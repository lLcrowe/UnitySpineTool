using UnityEngine;
using System;
using System.Collections.Generic;

namespace SpineVAT
{
    /// <summary>
    /// VAT 유닛 하나의 런타임 상태.
    /// class 대신 struct로 GC 부하를 제거한다.
    /// </summary>
    public struct VatUnitState
    {
        public bool active;
        public int clipIndex;

        // Transform
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        // Animation time
        public float prevNormTime;
        public float currNormTime;
        public float speed; // 1.0 = 정상 속도
        public bool loop;
    }

    /// <summary>
    /// 수백~수천 개의 VAT 유닛을 관리하고 Graphics.DrawMeshInstanced로 1-draw 렌더링하는 중앙 매니저.
    /// Runtime에서 Spine 네임스페이스를 참조하지 않는다.
    /// </summary>
    public class SpineVatRenderer : MonoBehaviour
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 싱글톤
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private static SpineVatRenderer instance;
        public static SpineVatRenderer Instance
        {
            get
            {
                if (instance != null) return instance;
                instance = FindFirstObjectByType<SpineVatRenderer>();
                return instance;
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Inspector
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        [Header("Data")]
        [SerializeField] private SpineVatData vatData;
        [SerializeField] private Material vatMaterial; // SpineURPVat 셰이더 사용

        [Header("Settings")]
        [SerializeField] private int defaultClipIndex;
        [SerializeField] private bool defaultLoop = true;
        [SerializeField] private float defaultSpeed = 1f;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 내부 상태
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private List<VatUnitState> units = new List<VatUnitState>(256);

        // DrawMeshInstanced 한 번에 최대 1023개
        private const int BATCH_SIZE = 1023;
        private Matrix4x4[] batchMatrices = new Matrix4x4[BATCH_SIZE];
        private MaterialPropertyBlock propertyBlock;

        // 인스턴스별 프로퍼티 배열 (재사용)
        private float[] batchAnimTime = new float[BATCH_SIZE];
        private float[] batchFrameOffset = new float[BATCH_SIZE];
        private float[] batchFrameCount = new float[BATCH_SIZE];

        // 셰이더 프로퍼티 ID 캐시
        private static readonly int PropAnimTime = Shader.PropertyToID("_AnimTime");
        private static readonly int PropFrameOffset = Shader.PropertyToID("_FrameOffset");
        private static readonly int PropFrameCount = Shader.PropertyToID("_FrameCount");
        private static readonly int PropTotalFrames = Shader.PropertyToID("_TotalFrames");
        private static readonly int PropVertexCount = Shader.PropertyToID("_VertexCount");
        private static readonly int PropVatPositionTex = Shader.PropertyToID("_VatPositionTex");

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 이벤트
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        /// <summary>
        /// VAT 이벤트가 발동될 때 호출된다.
        /// 파라미터: (유닛 인덱스, 이벤트 이름)
        /// </summary>
        public event Action<int, string> OnVatEvent;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 초기화
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;

            propertyBlock = new MaterialPropertyBlock();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 유닛 관리 API
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// 유닛 추가. 반환값은 유닛 인덱스.
        /// </summary>
        public int AddUnit(Vector3 position)
        {
            return AddUnit(position, Quaternion.identity, Vector3.one, defaultClipIndex, defaultLoop, defaultSpeed);
        }

        public int AddUnit(Vector3 position, Quaternion rotation, Vector3 scale,
                           int clipIndex, bool loop, float speed)
        {
            if (vatData == null) return -1;
            if (clipIndex < 0 || clipIndex >= vatData.clips.Count) return -1;

            var unit = new VatUnitState
            {
                active = true,
                clipIndex = clipIndex,
                position = position,
                rotation = rotation,
                scale = scale,
                prevNormTime = 0f,
                currNormTime = 0f,
                speed = speed,
                loop = loop
            };

            units.Add(unit);
            return units.Count - 1;
        }

        /// <summary>
        /// 유닛 비활성화 (풀링 용도, 리스트에서 제거하지 않음)
        /// </summary>
        public void DeactivateUnit(int index)
        {
            if (index < 0 || index >= units.Count) return;
            var u = units[index];
            u.active = false;
            units[index] = u;
        }

        /// <summary>
        /// 유닛 재활성화
        /// </summary>
        public void ActivateUnit(int index, Vector3 position, int clipIndex)
        {
            if (index < 0 || index >= units.Count) return;
            if (clipIndex < 0 || clipIndex >= vatData.clips.Count) return;

            var u = units[index];
            u.active = true;
            u.position = position;
            u.clipIndex = clipIndex;
            u.prevNormTime = 0f;
            u.currNormTime = 0f;
            units[index] = u;
        }

        /// <summary>
        /// 유닛의 애니메이션 클립을 교체한다.
        /// </summary>
        public void SetUnitClip(int index, int clipIndex)
        {
            if (index < 0 || index >= units.Count) return;
            if (clipIndex < 0 || clipIndex >= vatData.clips.Count) return;

            var u = units[index];
            u.clipIndex = clipIndex;
            u.prevNormTime = 0f;
            u.currNormTime = 0f;
            units[index] = u;
        }

        /// <summary>
        /// 유닛 위치 갱신
        /// </summary>
        public void SetUnitPosition(int index, Vector3 position)
        {
            if (index < 0 || index >= units.Count) return;
            var u = units[index];
            u.position = position;
            units[index] = u;
        }

        /// <summary>
        /// 유닛 Transform 일괄 갱신
        /// </summary>
        public void SetUnitTransform(int index, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (index < 0 || index >= units.Count) return;
            var u = units[index];
            u.position = position;
            u.rotation = rotation;
            u.scale = scale;
            units[index] = u;
        }

        /// <summary>
        /// 유닛 재생 속도 변경
        /// </summary>
        public void SetUnitSpeed(int index, float speed)
        {
            if (index < 0 || index >= units.Count) return;
            var u = units[index];
            u.speed = speed;
            units[index] = u;
        }

        /// <summary>
        /// 유닛 루프 여부 변경
        /// </summary>
        public void SetUnitLoop(int index, bool loop)
        {
            if (index < 0 || index >= units.Count) return;
            var u = units[index];
            u.loop = loop;
            units[index] = u;
        }

        /// <summary>
        /// 활성 유닛 수
        /// </summary>
        public int ActiveUnitCount
        {
            get
            {
                int count = 0;
                for (int i = 0, len = units.Count; i < len; i++)
                {
                    if (units[i].active) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// 전체 유닛 수 (비활성 포함)
        /// </summary>
        public int TotalUnitCount => units.Count;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Update Loop
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        private void Update()
        {
            if (vatData == null) return;
            if (vatMaterial == null) return;
            if (units.Count == 0) return;

            float dt = Time.deltaTime;

            UpdateAnimationTime(dt);
            DrawAllUnits();
        }

        /// <summary>
        /// 모든 활성 유닛의 시간을 갱신하고 이벤트를 체크한다.
        /// </summary>
        private void UpdateAnimationTime(float dt)
        {
            for (int i = 0, count = units.Count; i < count; i++)
            {
                var u = units[i];
                if (!u.active) continue;

                var clip = vatData.clips[u.clipIndex];
                if (clip.duration <= 0f) continue;

                u.prevNormTime = u.currNormTime;
                u.currNormTime += (dt * u.speed) / clip.duration;

                if (u.loop)
                {
                    // 루프 시 랩 처리
                    if (u.currNormTime >= 1f)
                    {
                        // 이벤트 체크 (prevTime ~ 1.0 구간)
                        CheckEvents(i, clip, u.prevNormTime, 1f);
                        // 랩
                        u.currNormTime -= 1f;
                        u.prevNormTime = 0f;
                        // 이벤트 체크 (0.0 ~ 랩 후 currTime 구간)
                        CheckEvents(i, clip, 0f, u.currNormTime);
                    }
                    else
                    {
                        CheckEvents(i, clip, u.prevNormTime, u.currNormTime);
                    }
                }
                else
                {
                    if (u.currNormTime > 1f) u.currNormTime = 1f;
                    CheckEvents(i, clip, u.prevNormTime, u.currNormTime);
                }

                units[i] = u;
            }
        }

        /// <summary>
        /// prevTime과 currTime 사이에 이벤트가 있으면 발송한다.
        /// </summary>
        private void CheckEvents(int unitIndex, VatClipData clip, float prevTime, float currTime)
        {
            if (OnVatEvent == null) return;
            if (clip.events == null) return;

            for (int e = 0, eCount = clip.events.Count; e < eCount; e++)
            {
                float t = clip.events[e].normalizedTime;
                if (t > prevTime && t <= currTime)
                {
                    OnVatEvent.Invoke(unitIndex, clip.events[e].eventName);
                }
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 렌더링
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void DrawAllUnits()
        {
            // 공통 머티리얼 프로퍼티 설정
            vatMaterial.SetFloat(PropTotalFrames, vatData.totalFrames);
            vatMaterial.SetFloat(PropVertexCount, vatData.vertexCount);
            vatMaterial.SetTexture(PropVatPositionTex, vatData.positionTexture);

            int batchIndex = 0;

            for (int i = 0, count = units.Count; i < count; i++)
            {
                var u = units[i];
                if (!u.active) continue;

                var clip = vatData.clips[u.clipIndex];

                batchMatrices[batchIndex] = Matrix4x4.TRS(u.position, u.rotation, u.scale);
                batchAnimTime[batchIndex] = Mathf.Clamp01(u.currNormTime);
                batchFrameOffset[batchIndex] = clip.frameOffset;
                batchFrameCount[batchIndex] = clip.frameCount;
                batchIndex++;

                // 배치가 가득 차면 드로우
                if (batchIndex >= BATCH_SIZE)
                {
                    FlushBatch(batchIndex);
                    batchIndex = 0;
                }
            }

            // 남은 유닛 드로우
            if (batchIndex > 0)
            {
                FlushBatch(batchIndex);
            }
        }

        private void FlushBatch(int count)
        {
            propertyBlock.Clear();
            propertyBlock.SetFloatArray(PropAnimTime, CopySubArray(batchAnimTime, count));
            propertyBlock.SetFloatArray(PropFrameOffset, CopySubArray(batchFrameOffset, count));
            propertyBlock.SetFloatArray(PropFrameCount, CopySubArray(batchFrameCount, count));

            Graphics.DrawMeshInstanced(
                vatData.sharedMesh,
                0,
                vatMaterial,
                batchMatrices,
                count,
                propertyBlock
            );
        }

        /// <summary>
        /// 배열의 앞 count개만 복사한다. DrawMeshInstanced에 정확한 크기를 전달하기 위함.
        /// </summary>
        private float[] CopySubArray(float[] source, int count)
        {
            // count == BATCH_SIZE이면 그대로 반환 (할당 최소화)
            if (count == BATCH_SIZE) return source;

            var result = new float[count];
            Array.Copy(source, result, count);
            return result;
        }
    }
}
