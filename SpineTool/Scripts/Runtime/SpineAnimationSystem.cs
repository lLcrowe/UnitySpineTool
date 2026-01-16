using UnityEngine;
using InteractAnimation.Core;

// Spine Runtime이 없을 경우를 대비한 조건부 컴파일
#if SPINE_UNITY
using Spine.Unity;
using Spine;
#endif

namespace InteractAnimation.AnimationSystems.Spine
{
    /// <summary>
    /// Spine2D 애니메이션 시스템 구현
    /// AnimationSystemBase 상속, 이벤트 기반 통신
    /// </summary>
    public class SpineAnimationSystem : AnimationSystemBase
    {
#if SPINE_UNITY
        [Header("Spine Components")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Symbol Settings")]
        [SerializeField] private SpineSymbolCollection symbolCollection;
        [SerializeField] private bool useSymbolSystem = true;

        [Header("Animation Settings")]
        [SerializeField] private int defaultTrackIndex = 0;
        [SerializeField] private bool clearPreviousAnimation = true;

        private string currentAnimationName;
        private float currentSpeed = 1f;
        private bool isPaused = false;
        private SpineSymbolData currentSymbol;

        public override void Initialize(GameObject targetObject)
        {
            if (isInitialized) return;

            // SkeletonAnimation 컴포넌트 찾기
            if (skeletonAnimation == null)
            {
                skeletonAnimation = targetObject.GetComponent<SkeletonAnimation>();

                if (skeletonAnimation == null)
                {
                    Debug.LogError($"[SpineAnimationSystem] SkeletonAnimation component not found on {targetObject.name}");
                    return;
                }
            }

            // 심볼 컬렉션 초기화
            if (symbolCollection != null)
            {
                symbolCollection.Initialize();
            }

            // Spine 애니메이션 이벤트 등록
            if (skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Complete += OnSpineAnimationComplete;
                skeletonAnimation.AnimationState.Start += OnSpineAnimationStart;
                skeletonAnimation.AnimationState.Event += OnSpineAnimationEvent;
            }

            isInitialized = true;
        }

        public override void PlayAnimation(string animationName, bool loop = false)
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
            {
                Debug.LogWarning("[SpineAnimationSystem] SkeletonAnimation not initialized");
                return;
            }

            // 심볼 시스템 사용 시 심볼 데이터 가져오기
            if (useSymbolSystem && symbolCollection != null)
            {
                currentSymbol = symbolCollection.GetSymbolByAnimationName(animationName);

                if (currentSymbol != null)
                {
                    // 심볼 데이터의 설정 적용
                    animationName = currentSymbol.animationName;
                    loop = currentSymbol.isLooping;
                    currentSpeed = currentSymbol.customSpeed;

                    // 스킨 변경 (지정되어 있는 경우)
                    if (!string.IsNullOrEmpty(currentSymbol.skinName))
                    {
                        skeletonAnimation.Skeleton.SetSkin(currentSymbol.skinName);
                        skeletonAnimation.Skeleton.SetSlotsToSetupPose();
                    }
                }
            }

            currentAnimationName = animationName;

            // 애니메이션 재생
            TrackEntry trackEntry;
            if (currentSymbol != null && currentSymbol.blendDuration > 0)
            {
                trackEntry = skeletonAnimation.AnimationState.SetAnimation(defaultTrackIndex, animationName, loop);
                trackEntry.MixDuration = currentSymbol.blendDuration;
            }
            else
            {
                trackEntry = skeletonAnimation.AnimationState.SetAnimation(defaultTrackIndex, animationName, loop);
            }

            if (trackEntry != null)
            {
                trackEntry.TimeScale = currentSpeed;
            }

            isPaused = false;
        }

        public override void StopAnimation()
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
                return;

            skeletonAnimation.AnimationState.ClearTrack(defaultTrackIndex);
            currentAnimationName = null;
            currentSymbol = null;
            isPaused = false;
        }

        public override void PauseAnimation()
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
                return;

            TrackEntry trackEntry = skeletonAnimation.AnimationState.GetCurrent(defaultTrackIndex);
            if (trackEntry != null)
            {
                trackEntry.TimeScale = 0f;
                isPaused = true;
            }
        }

        public override void ResumeAnimation()
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
                return;

            TrackEntry trackEntry = skeletonAnimation.AnimationState.GetCurrent(defaultTrackIndex);
            if (trackEntry != null)
            {
                trackEntry.TimeScale = currentSpeed;
                isPaused = false;
            }
        }

        public override string GetCurrentAnimationName()
        {
            return currentAnimationName;
        }

        public override bool IsPlaying()
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
                return false;

            TrackEntry trackEntry = skeletonAnimation.AnimationState.GetCurrent(defaultTrackIndex);
            return trackEntry != null && !isPaused;
        }

        public override void SetAnimationSpeed(float speed)
        {
            currentSpeed = speed;

            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
                return;

            TrackEntry trackEntry = skeletonAnimation.AnimationState.GetCurrent(defaultTrackIndex);
            if (trackEntry != null && !isPaused)
            {
                trackEntry.TimeScale = speed;
            }
        }

        /// <summary>
        /// 심볼 ID로 애니메이션 재생
        /// </summary>
        public void PlayAnimationBySymbolId(string symbolId, bool overrideLoop = false)
        {
            if (symbolCollection == null)
            {
                Debug.LogWarning("[SpineAnimationSystem] Symbol collection not assigned");
                return;
            }

            SpineSymbolData symbol = symbolCollection.GetSymbolById(symbolId);
            if (symbol != null && symbol.IsValid())
            {
                bool loop = overrideLoop ? overrideLoop : symbol.isLooping;
                PlayAnimation(symbol.animationName, loop);
            }
            else
            {
                Debug.LogWarning($"[SpineAnimationSystem] Symbol with ID '{symbolId}' not found");
            }
        }

        /// <summary>
        /// 애니메이션 재생 시간 가져오기
        /// </summary>
        public float GetAnimationDuration(string animationName)
        {
            if (skeletonAnimation == null || skeletonAnimation.SkeletonDataAsset == null)
                return 0f;

            var animation = skeletonAnimation.SkeletonDataAsset.GetSkeletonData(false).FindAnimation(animationName);
            return animation != null ? animation.Duration : 0f;
        }

        /// <summary>
        /// 현재 심볼 데이터 가져오기
        /// </summary>
        public SpineSymbolData GetCurrentSymbol()
        {
            return currentSymbol;
        }

        /// <summary>
        /// 심볼 컬렉션 설정
        /// </summary>
        public void SetSymbolCollection(SpineSymbolCollection collection)
        {
            symbolCollection = collection;
            if (symbolCollection != null)
            {
                symbolCollection.Initialize();
            }
        }

        #region Spine Animation Event Handlers

        private void OnSpineAnimationStart(TrackEntry trackEntry)
        {
            Debug.Log($"[SpineAnimationSystem] Animation started: {trackEntry.Animation.Name}");

            // 이벤트 발생
            TriggerAnimationStarted(trackEntry.Animation.Name);
        }

        private void OnSpineAnimationComplete(TrackEntry trackEntry)
        {
            Debug.Log($"[SpineAnimationSystem] Animation completed: {trackEntry.Animation.Name}");

            // 이벤트 발생
            TriggerAnimationCompleted(trackEntry.Animation.Name);
        }

        private void OnSpineAnimationEvent(TrackEntry trackEntry, global::Spine.Event e)
        {
            Debug.Log($"[SpineAnimationSystem] Animation event: {e.Data.Name}");

            // AnimationEventData 생성 및 이벤트 발생
            AnimationEventData eventData = new AnimationEventData(e.Data.Name)
            {
                normalizedTime = trackEntry.TrackTime / trackEntry.TrackEnd,
                intParameter = e.Int,
                floatParameter = e.Float,
                stringParameter = e.String
            };

            TriggerAnimationEvent(eventData);
        }

        #endregion

        protected virtual void OnDestroy()
        {
            // Spine 이벤트 해제
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Complete -= OnSpineAnimationComplete;
                skeletonAnimation.AnimationState.Start -= OnSpineAnimationStart;
                skeletonAnimation.AnimationState.Event -= OnSpineAnimationEvent;
            }
        }

#else
        // Spine Runtime이 없을 경우의 더미 구현
        public override void Initialize(GameObject targetObject)
        {
            Debug.LogWarning("[SpineAnimationSystem] Spine Runtime not found. Please import Spine-Unity runtime.");
            isInitialized = true;
        }

        public override void PlayAnimation(string animationName, bool loop = false)
        {
            Debug.LogWarning("[SpineAnimationSystem] Spine Runtime not found.");
        }

        public override void StopAnimation() { }
        public override void PauseAnimation() { }
        public override void ResumeAnimation() { }
        public override string GetCurrentAnimationName() { return null; }
        public override bool IsPlaying() { return false; }
        public override void SetAnimationSpeed(float speed) { }
#endif
    }
}
