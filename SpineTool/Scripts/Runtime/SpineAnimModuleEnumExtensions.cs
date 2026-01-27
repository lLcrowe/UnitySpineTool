#if SPINE_UNITY
using Spine;

namespace SpineTool
{
    /// <summary>
    /// SpineAnimModule에 Enum 지원 확장 메서드
    ///
    /// 사용법:
    /// <code>
    /// // Enum 정의 (SpineAnimationEnumGenerator로 자동 생성 가능)
    /// public enum PlayerAnimations
    /// {
    ///     Idle,
    ///     Run,
    ///     Jump,
    ///     Attack
    /// }
    ///
    /// // 사용
    /// animModule.PlayAnimation(PlayerAnimations.Idle, loop: true);
    /// animModule.AddAnimation(PlayerAnimations.Attack, loop: false);
    /// </code>
    /// </summary>
    public static class SpineAnimModuleEnumExtensions
    {
        /// <summary>
        /// Enum을 사용하여 애니메이션 재생
        /// </summary>
        /// <typeparam name="T">애니메이션 Enum 타입</typeparam>
        /// <param name="module">SpineAnimModule 인스턴스</param>
        /// <param name="animationEnum">재생할 애니메이션 Enum 값</param>
        /// <param name="loop">루프 여부</param>
        /// <param name="trackIndex">트랙 인덱스</param>
        /// <returns>TrackEntry</returns>
        public static TrackEntry PlayAnimation<T>(this SpineAnimModule module, T animationEnum, bool loop = true, int trackIndex = 0) where T : System.Enum
        {
            string animationName = animationEnum.ToString();
            return module.PlayAnimation(animationName, loop, trackIndex);
        }

        /// <summary>
        /// Enum을 사용하여 애니메이션 추가 (큐잉)
        /// </summary>
        /// <typeparam name="T">애니메이션 Enum 타입</typeparam>
        /// <param name="module">SpineAnimModule 인스턴스</param>
        /// <param name="animationEnum">추가할 애니메이션 Enum 값</param>
        /// <param name="loop">루프 여부</param>
        /// <param name="delay">지연 시간</param>
        /// <param name="trackIndex">트랙 인덱스</param>
        /// <returns>TrackEntry</returns>
        public static TrackEntry AddAnimation<T>(this SpineAnimModule module, T animationEnum, bool loop = true, float delay = 0f, int trackIndex = 0) where T : System.Enum
        {
            string animationName = animationEnum.ToString();
            return module.AddAnimation(animationName, loop, delay, trackIndex);
        }

        /// <summary>
        /// Enum을 사용하여 애니메이션 즉시 설정 (페이드 없이)
        /// </summary>
        /// <typeparam name="T">애니메이션 Enum 타입</typeparam>
        /// <param name="module">SpineAnimModule 인스턴스</param>
        /// <param name="animationEnum">설정할 애니메이션 Enum 값</param>
        /// <param name="loop">루프 여부</param>
        /// <param name="trackIndex">트랙 인덱스</param>
        /// <returns>TrackEntry</returns>
        public static TrackEntry SetAnimation<T>(this SpineAnimModule module, T animationEnum, bool loop = true, int trackIndex = 0) where T : System.Enum
        {
            string animationName = animationEnum.ToString();

            if (module.SkeletonAnimation == null)
            {
                UnityEngine.Debug.LogError("SkeletonAnimation이 없습니다.");
                return null;
            }

            return module.SkeletonAnimation.AnimationState.SetAnimation(trackIndex, animationName, loop);
        }
    }
}
#endif
