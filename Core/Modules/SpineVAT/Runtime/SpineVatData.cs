using UnityEngine;
using System.Collections.Generic;

namespace SpineVAT
{
    /// <summary>
    /// 구워진 VAT 애니메이션 내에서 발동하는 이벤트 메타데이터.
    /// Baker가 Spine EventTimeline을 분석하여 채운다.
    /// </summary>
    [System.Serializable]
    public struct VatEventData
    {
        public string eventName;
        [Range(0f, 1f)] public float normalizedTime; // 0.0 ~ 1.0
    }

    /// <summary>
    /// 하나의 애니메이션 클립에 대한 VAT 베이크 결과.
    /// positionTexture의 row 범위(frameOffset ~ frameOffset+frameCount-1)를 사용한다.
    /// </summary>
    [System.Serializable]
    public struct VatClipData
    {
        public string clipName;
        public int frameOffset;  // 텍스처 내 시작 row
        public int frameCount;   // 이 클립의 프레임 수
        public float duration;   // 원본 애니메이션 길이(초)
        public List<VatEventData> events;
    }

    /// <summary>
    /// 구워진 VAT 텍스처, 공유 메쉬, 그리고 클립별 메타데이터를 저장하는 ScriptableObject.
    /// Runtime 코드는 이 SO만 참조하며, Spine 네임스페이스에 의존하지 않는다.
    /// </summary>
    [CreateAssetMenu(fileName = "NewSpineVatData", menuName = "SpineVAT/Vat Data")]
    public class SpineVatData : ScriptableObject
    {
        [Header("Baked Textures")]
        [Tooltip("각 row = 1프레임, 각 pixel = 1버텍스의 로컬 좌표 (RGB=XYZ)")]
        public Texture2D positionTexture;

        [Header("Shared Mesh")]
        [Tooltip("첫 프레임 기준으로 구워진 메쉬 (버텍스 순서 보장)")]
        public Mesh sharedMesh;

        [Header("Animation Clips")]
        public List<VatClipData> clips = new List<VatClipData>();

        [Header("Global Info")]
        [Tooltip("positionTexture 전체의 row 수 (모든 클립 프레임 합산)")]
        public int totalFrames;

        [Tooltip("positionTexture의 가로 픽셀 수 = 버텍스 개수")]
        public int vertexCount;

        /// <summary>
        /// 클립 이름으로 VatClipData를 검색한다.
        /// </summary>
        public bool TryGetClip(string clipName, out VatClipData clip)
        {
            for (int i = 0, count = clips.Count; i < count; i++)
            {
                if (clips[i].clipName == clipName)
                {
                    clip = clips[i];
                    return true;
                }
            }
            clip = default;
            return false;
        }

        /// <summary>
        /// 클립 인덱스로 직접 접근.
        /// </summary>
        public VatClipData GetClip(int index)
        {
            return clips[index];
        }
    }
}
