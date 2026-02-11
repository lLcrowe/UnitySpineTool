using UnityEngine;
using System;
using System.Collections.Generic;

namespace SpineVAT
{
    /// <summary>
    /// VAT 이벤트 데이터.
    /// SpineTool.SpineEventData와 동일한 구조이지만 Spine 의존성이 없다.
    /// </summary>
    public class VatAnimEventData
    {
        public string EventName { get; private set; }
        public string AnimationName { get; private set; }
        public float NormalizedTime { get; private set; }

        public VatAnimEventData(string eventName, string animationName, float normalizedTime)
        {
            EventName = eventName;
            AnimationName = animationName;
            NormalizedTime = normalizedTime;
        }

        public override string ToString()
        {
            return $"VatEvent[{EventName}] @{NormalizedTime:F2} (Anim: {AnimationName})";
        }
    }

    /// <summary>
    /// SpineAnimModule과 동일한 사용감으로 VAT 애니메이션을 제어하는 개별 유닛 컨트롤러.
    ///
    /// SpineAnimModule 대응 API:
    ///   PlayAnimation("walk", true)   → 클립 이름으로 재생
    ///   StopAnimation()               → 정지
    ///   PauseAnimation()              → 일시정지
    ///   ResumeAnimation()             → 재개
    ///   SetSpeed(2f)                  → 속도 조절
    ///   AddEventListener("hit", cb)   → 이벤트 리스너
    ///
    /// 사용법:
    ///   GameObject에 이 컴포넌트를 붙이고, SpineVatData와 SpineVatRenderer를 설정.
    ///   내부적으로 Renderer에 유닛을 등록하고, Transform 동기화 + 이벤트 중계를 처리한다.
    /// </summary>
    public class SpineVatAnimModule : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private SpineVatData vatData;

        [Header("Settings")]
        [SerializeField] private string defaultAnimation = "";
        [SerializeField] private bool defaultLoop = true;
        [SerializeField] private bool autoPlayOnStart = false;
        [SerializeField] private bool syncTransformEveryFrame = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = false;

        // Renderer 참조
        private SpineVatRenderer renderer;
        private int unitIndex = -1;

        // 현재 상태
        private string currentAnimationName = "";
        private int currentClipIndex = -1;
        private bool isPaused;
        private float savedSpeed = 1f;

        // 이벤트 리스너 (SpineAnimModule과 동일한 구조)
        private Dictionary<string, List<Action<VatAnimEventData>>> eventListeners
            = new Dictionary<string, List<Action<VatAnimEventData>>>();

        // 클립 이름 → 인덱스 캐시 (매번 검색 방지)
        private Dictionary<string, int> clipNameToIndex;

        // 프로퍼티
        public string CurrentAnimationName => currentAnimationName;
        public bool IsPlaying => unitIndex >= 0 && !isPaused && currentClipIndex >= 0;
        public bool IsRegistered => unitIndex >= 0;
        public int UnitIndex => unitIndex;

        #region Unity Lifecycle

        private void Awake()
        {
            BuildClipCache();
        }

        private void Start()
        {
            renderer = SpineVatRenderer.Instance;
            if (renderer == null)
            {
                LogError("SpineVatRenderer not found in scene!");
                enabled = false;
                return;
            }

            // Renderer에 유닛 등록
            int clipIndex = 0;
            if (!string.IsNullOrEmpty(defaultAnimation))
            {
                clipIndex = GetClipIndex(defaultAnimation);
                if (clipIndex < 0) clipIndex = 0;
            }

            unitIndex = renderer.AddUnit(
                transform.position,
                transform.rotation,
                transform.localScale,
                clipIndex,
                defaultLoop,
                1f
            );

            if (unitIndex < 0)
            {
                LogError("Failed to register unit with SpineVatRenderer!");
                enabled = false;
                return;
            }

            // 이벤트 구독
            renderer.OnVatEvent += OnRendererEvent;

            currentClipIndex = clipIndex;
            if (!string.IsNullOrEmpty(defaultAnimation))
            {
                currentAnimationName = defaultAnimation;
            }

            if (!autoPlayOnStart)
            {
                // 등록은 하되 일시정지 상태로
                PauseAnimation();
            }

            Log($"Unit registered: index={unitIndex}");
        }

        private void Update()
        {
            if (!syncTransformEveryFrame) return;
            if (renderer == null) return;
            if (unitIndex < 0) return;

            renderer.SetUnitTransform(unitIndex, transform.position, transform.rotation, transform.localScale);
        }

        private void OnDestroy()
        {
            if (renderer != null)
            {
                renderer.OnVatEvent -= OnRendererEvent;

                if (unitIndex >= 0)
                {
                    renderer.DeactivateUnit(unitIndex);
                }
            }

            eventListeners.Clear();
        }

        #endregion

        #region Animation Playback

        /// <summary>
        /// 애니메이션 재생 (SpineAnimModule.PlayAnimation 대응)
        /// </summary>
        public void PlayAnimation(string animationName, bool loop = true)
        {
            if (renderer == null) return;
            if (unitIndex < 0) return;

            int clipIndex = GetClipIndex(animationName);
            if (clipIndex < 0)
            {
                LogWarning($"Animation not found: {animationName}");
                return;
            }

            currentAnimationName = animationName;
            currentClipIndex = clipIndex;
            isPaused = false;

            renderer.SetUnitClip(unitIndex, clipIndex);

            Log($"Playing: {animationName} (loop: {loop})");
        }

        /// <summary>
        /// 애니메이션 정지
        /// </summary>
        public void StopAnimation()
        {
            if (renderer == null) return;
            if (unitIndex < 0) return;

            renderer.DeactivateUnit(unitIndex);
            currentAnimationName = "";
            currentClipIndex = -1;
            isPaused = false;

            Log("Animation stopped");
        }

        /// <summary>
        /// 일시정지 (속도를 0으로)
        /// </summary>
        public void PauseAnimation()
        {
            if (renderer == null) return;
            if (unitIndex < 0) return;
            if (isPaused) return;

            isPaused = true;
            // Renderer의 유닛 speed를 0으로 설정
            SetSpeedInternal(0f);

            Log("Animation paused");
        }

        /// <summary>
        /// 재개
        /// </summary>
        public void ResumeAnimation()
        {
            if (renderer == null) return;
            if (unitIndex < 0) return;
            if (!isPaused) return;

            isPaused = false;
            SetSpeedInternal(savedSpeed);

            Log("Animation resumed");
        }

        #endregion

        #region Animation Control

        /// <summary>
        /// 재생 속도 설정 (SpineAnimModule.SetSpeed 대응)
        /// </summary>
        public void SetSpeed(float speed)
        {
            savedSpeed = speed;
            if (isPaused) return;

            SetSpeedInternal(speed);
            Log($"Speed set to {speed}");
        }

        /// <summary>
        /// 루프 여부 변경
        /// </summary>
        public void SetLoop(bool loop)
        {
            if (renderer == null) return;
            if (unitIndex < 0) return;

            renderer.SetUnitLoop(unitIndex, loop);
            Log($"Loop set to {loop}");
        }

        /// <summary>
        /// 애니메이션 존재 여부 확인 (SpineAnimModule.HasAnimation 대응)
        /// </summary>
        public bool HasAnimation(string animationName)
        {
            return GetClipIndex(animationName) >= 0;
        }

        /// <summary>
        /// 애니메이션 길이 가져오기 (SpineAnimModule.GetAnimationDuration 대응)
        /// </summary>
        public float GetAnimationDuration(string animationName)
        {
            if (vatData == null) return 0f;

            int index = GetClipIndex(animationName);
            if (index < 0) return 0f;

            return vatData.clips[index].duration;
        }

        /// <summary>
        /// 사용 가능한 모든 애니메이션 이름 목록
        /// </summary>
        public List<string> GetAnimationNames()
        {
            var names = new List<string>();
            if (vatData == null) return names;

            for (int i = 0, count = vatData.clips.Count; i < count; i++)
            {
                names.Add(vatData.clips[i].clipName);
            }
            return names;
        }

        #endregion

        #region Event System

        /// <summary>
        /// 이벤트 리스너 추가 (SpineAnimModule.AddEventListener 대응)
        /// </summary>
        public void AddEventListener(string eventName, Action<VatAnimEventData> callback)
        {
            if (callback == null) return;

            if (!eventListeners.TryGetValue(eventName, out var list))
            {
                list = new List<Action<VatAnimEventData>>();
                eventListeners[eventName] = list;
            }

            if (!list.Contains(callback))
            {
                list.Add(callback);
                Log($"Event listener added: {eventName}");
            }
        }

        /// <summary>
        /// 이벤트 리스너 제거
        /// </summary>
        public void RemoveEventListener(string eventName, Action<VatAnimEventData> callback)
        {
            if (callback == null) return;

            if (eventListeners.TryGetValue(eventName, out var list))
            {
                list.Remove(callback);
                Log($"Event listener removed: {eventName}");
            }
        }

        /// <summary>
        /// 특정 이벤트의 모든 리스너 제거
        /// </summary>
        public void RemoveAllListeners(string eventName)
        {
            if (eventListeners.Remove(eventName))
            {
                Log($"All listeners removed for: {eventName}");
            }
        }

        /// <summary>
        /// 모든 이벤트 리스너 제거
        /// </summary>
        public void RemoveAllListeners()
        {
            eventListeners.Clear();
            Log("All event listeners removed");
        }

        /// <summary>
        /// SpineVatRenderer.OnVatEvent에서 호출됨.
        /// 자신의 unitIndex에 해당하는 이벤트만 필터링하여 리스너에 전달.
        /// </summary>
        private void OnRendererEvent(int eventUnitIndex, string eventName)
        {
            if (eventUnitIndex != unitIndex) return;

            if (!eventListeners.TryGetValue(eventName, out var list)) return;
            if (list.Count == 0) return;

            var eventData = new VatAnimEventData(eventName, currentAnimationName, 0f);

            for (int i = 0, count = list.Count; i < count; i++)
            {
                try
                {
                    list[i].Invoke(eventData);
                }
                catch (Exception ex)
                {
                    LogError($"Error invoking listener for '{eventName}': {ex.Message}");
                }
            }
        }

        #endregion

        #region Internal

        private void BuildClipCache()
        {
            clipNameToIndex = new Dictionary<string, int>();
            if (vatData == null) return;

            for (int i = 0, count = vatData.clips.Count; i < count; i++)
            {
                clipNameToIndex[vatData.clips[i].clipName] = i;
            }
        }

        private int GetClipIndex(string animationName)
        {
            if (clipNameToIndex == null) BuildClipCache();
            if (clipNameToIndex.TryGetValue(animationName, out int index)) return index;
            return -1;
        }

        private void SetSpeedInternal(float speed)
        {
            // SpineVatRenderer에 직접 speed를 변경하는 API를 호출
            renderer.SetUnitSpeed(unitIndex, speed);
        }

        #endregion

        #region Debug

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SpineVatAnimModule] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SpineVatAnimModule] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SpineVatAnimModule] {message}");
        }

        #endregion
    }
}
