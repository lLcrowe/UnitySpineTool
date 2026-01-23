using System;
using UnityEngine;

namespace SpineTool
{
    /// <summary>
    /// Spine 애니메이션에 이벤트를 주입하기 위한 Attribute
    /// 클래스에 이 Attribute를 붙이면 런타임에 자동으로 이벤트가 등록됩니다.
    ///
    /// 사용 예시:
    /// [InjectSpineEvent("attack", "OnHitImpact", 0.5f, IntParameter = 50)]
    /// public class MyCharacter : MonoBehaviour { ... }
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class InjectSpineEventAttribute : Attribute
    {
        /// <summary>애니메이션 이름</summary>
        public string AnimationName { get; private set; }

        /// <summary>호출될 함수 이름</summary>
        public string FunctionName { get; private set; }

        /// <summary>정규화된 시간 (0.0 ~ 1.0)</summary>
        public float NormalizedTime { get; private set; }

        /// <summary>String 파라미터 (선택사항)</summary>
        public string StringParameter { get; set; } = "";

        /// <summary>Int 파라미터 (선택사항)</summary>
        public int IntParameter { get; set; } = 0;

        /// <summary>Float 파라미터 (선택사항)</summary>
        public float FloatParameter { get; set; } = 0f;

        public InjectSpineEventAttribute(string animationName, string functionName, float normalizedTime)
        {
            AnimationName = animationName;
            FunctionName = functionName;
            NormalizedTime = Mathf.Clamp01(normalizedTime);
        }
    }

    /// <summary>
    /// Spine 이벤트 데이터 구조
    /// </summary>
    public class SpineEventData
    {
        /// <summary>이벤트 이름</summary>
        public string EventName { get; private set; }

        /// <summary>정규화된 시간 (0.0 ~ 1.0)</summary>
        public float NormalizedTime { get; private set; }

        /// <summary>애니메이션 이름</summary>
        public string AnimationName { get; private set; }

        /// <summary>현재 트랙 시간 (초)</summary>
        public float TrackTime { get; private set; }

        /// <summary>String 파라미터</summary>
        public string StringParameter { get; set; } = "";

        /// <summary>Int 파라미터</summary>
        public int IntParameter { get; set; } = 0;

        /// <summary>Float 파라미터</summary>
        public float FloatParameter { get; set; } = 0f;

        public SpineEventData(string eventName, string animationName, float normalizedTime, float trackTime = 0f)
        {
            EventName = eventName;
            AnimationName = animationName;
            NormalizedTime = normalizedTime;
            TrackTime = trackTime;
        }

        public override string ToString()
        {
            return $"SpineEvent[{EventName}] @{NormalizedTime:F2} (Anim: {AnimationName})";
        }
    }
}
