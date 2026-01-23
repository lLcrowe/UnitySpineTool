using UnityEngine;
using System;
using System.Collections.Generic;

#if SPINE_UNITY
using Spine;
using Spine.Unity;
#endif

namespace SpineTool
{
    /// <summary>
    /// Spine 애니메이션을 쉽게 제어하고 이벤트를 등록할 수 있는 통합 컨트롤러
    ///
    /// 주요 기능:
    /// - 애니메이션 재생/정지/일시정지
    /// - 이벤트 리스너 등록 (문자열 이벤트 이름 기반)
    /// - SpineSymbolData 지원
    /// - 속도, 루프, 스킨 제어
    ///
    /// 사용 예:
    /// controller.PlayAnimation("attack", false);
    /// controller.AddEventListener("hit_impact", OnHitImpact);
    /// </summary>
    [RequireComponent(typeof(SkeletonAnimation))]
    public class SpineAnimationController : MonoBehaviour
    {
#if SPINE_UNITY
        [Header("References")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("Symbol Data (Optional)")]
        [SerializeField] private SpineSymbolCollection symbolCollection;

        [Header("Settings")]
        [SerializeField] private bool autoPlayOnStart = false;
        [SerializeField] private string defaultAnimationName = "idle";
        [SerializeField] private bool defaultLoop = true;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = false;

        // 이벤트 리스너 저장소
        private Dictionary<string, List<Action<SpineEventData>>> eventListeners = new Dictionary<string, List<Action<SpineEventData>>>();

        // 현재 재생 중인 애니메이션 정보
        private string currentAnimationName = "";
        private TrackEntry currentTrackEntry;

        // 프로퍼티
        public SkeletonAnimation SkeletonAnimation => skeletonAnimation;
        public Skeleton Skeleton => skeletonAnimation?.Skeleton;
        public AnimationState AnimationState => skeletonAnimation?.AnimationState;
        public string CurrentAnimationName => currentAnimationName;
        public bool IsPlaying => currentTrackEntry != null && currentTrackEntry.TimeScale > 0;

        #region Unity Lifecycle

        private void Awake()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            if (skeletonAnimation == null)
            {
                LogError("SkeletonAnimation component not found!");
                enabled = false;
                return;
            }

            // Spine 이벤트 리스너 등록
            if (skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Event += OnSpineEvent;
                skeletonAnimation.AnimationState.Complete += OnAnimationComplete;
                skeletonAnimation.AnimationState.Start += OnAnimationStart;
            }
        }

        private void Start()
        {
            if (autoPlayOnStart && !string.IsNullOrEmpty(defaultAnimationName))
            {
                PlayAnimation(defaultAnimationName, defaultLoop);
            }
        }

        private void OnDestroy()
        {
            // 이벤트 리스너 해제
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Event -= OnSpineEvent;
                skeletonAnimation.AnimationState.Complete -= OnAnimationComplete;
                skeletonAnimation.AnimationState.Start -= OnAnimationStart;
            }

            // 모든 커스텀 리스너 제거
            eventListeners.Clear();
        }

        #endregion

        #region Animation Playback

        /// <summary>
        /// 애니메이션 재생
        /// </summary>
        public TrackEntry PlayAnimation(string animationName, bool loop = true, int trackIndex = 0)
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
            {
                LogError("SkeletonAnimation not initialized!");
                return null;
            }

            currentAnimationName = animationName;
            currentTrackEntry = skeletonAnimation.AnimationState.SetAnimation(trackIndex, animationName, loop);

            Log($"Playing animation: {animationName} (loop: {loop})");

            return currentTrackEntry;
        }

        /// <summary>
        /// 애니메이션 추가 (큐에 넣기)
        /// </summary>
        public TrackEntry AddAnimation(string animationName, bool loop = true, float delay = 0f, int trackIndex = 0)
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null)
            {
                LogError("SkeletonAnimation not initialized!");
                return null;
            }

            TrackEntry entry = skeletonAnimation.AnimationState.AddAnimation(trackIndex, animationName, loop, delay);

            Log($"Added animation to queue: {animationName} (delay: {delay}s)");

            return entry;
        }

        /// <summary>
        /// Symbol ID로 애니메이션 재생 (SpineSymbolData 사용)
        /// </summary>
        public TrackEntry PlayAnimationBySymbolId(string symbolId, bool loop = true)
        {
            if (symbolCollection == null)
            {
                LogError("SymbolCollection is not assigned!");
                return null;
            }

            SpineSymbolData symbol = symbolCollection.GetSymbolById(symbolId);
            if (symbol == null || !symbol.IsValid())
            {
                LogError($"Symbol not found: {symbolId}");
                return null;
            }

            return PlayAnimation(symbol.animationName, loop);
        }

        /// <summary>
        /// 애니메이션 정지 (Clear)
        /// </summary>
        public void StopAnimation(int trackIndex = 0)
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null) return;

            skeletonAnimation.AnimationState.ClearTrack(trackIndex);
            currentAnimationName = "";
            currentTrackEntry = null;

            Log("Animation stopped");
        }

        /// <summary>
        /// 모든 트랙 정지
        /// </summary>
        public void StopAllAnimations()
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null) return;

            skeletonAnimation.AnimationState.ClearTracks();
            currentAnimationName = "";
            currentTrackEntry = null;

            Log("All animations stopped");
        }

        /// <summary>
        /// 애니메이션 일시정지
        /// </summary>
        public void PauseAnimation(int trackIndex = 0)
        {
            TrackEntry entry = GetTrackEntry(trackIndex);
            if (entry != null)
            {
                entry.TimeScale = 0f;
                Log("Animation paused");
            }
        }

        /// <summary>
        /// 애니메이션 재개
        /// </summary>
        public void ResumeAnimation(int trackIndex = 0)
        {
            TrackEntry entry = GetTrackEntry(trackIndex);
            if (entry != null)
            {
                entry.TimeScale = 1f;
                Log("Animation resumed");
            }
        }

        /// <summary>
        /// Setup Pose로 리셋
        /// </summary>
        public void SetToSetupPose()
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return;

            skeletonAnimation.Skeleton.SetToSetupPose();
            Log("Set to setup pose");
        }

        #endregion

        #region Animation Control

        /// <summary>
        /// 애니메이션 속도 설정
        /// </summary>
        public void SetSpeed(float speed, int trackIndex = 0)
        {
            TrackEntry entry = GetTrackEntry(trackIndex);
            if (entry != null)
            {
                entry.TimeScale = speed;
                Log($"Speed set to {speed}");
            }
        }

        /// <summary>
        /// 스킨 변경
        /// </summary>
        public void SetSkin(string skinName)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return;

            Skin skin = skeletonAnimation.Skeleton.Data.FindSkin(skinName);
            if (skin != null)
            {
                skeletonAnimation.Skeleton.SetSkin(skin);
                skeletonAnimation.Skeleton.SetSlotsToSetupPose();
                Log($"Skin changed to: {skinName}");
            }
            else
            {
                LogWarning($"Skin not found: {skinName}");
            }
        }

        /// <summary>
        /// 애니메이션 블렌딩 시간 설정
        /// </summary>
        public void SetMixDuration(string fromAnimation, string toAnimation, float duration)
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null) return;

            skeletonAnimation.AnimationState.Data.SetMix(fromAnimation, toAnimation, duration);
            Log($"Mix duration set: {fromAnimation} -> {toAnimation} ({duration}s)");
        }

        #endregion

        #region Event System

        /// <summary>
        /// 이벤트 리스너 추가
        /// </summary>
        public void AddEventListener(string eventName, Action<SpineEventData> callback)
        {
            if (callback == null) return;

            if (!eventListeners.ContainsKey(eventName))
            {
                eventListeners[eventName] = new List<Action<SpineEventData>>();
            }

            if (!eventListeners[eventName].Contains(callback))
            {
                eventListeners[eventName].Add(callback);
                Log($"Event listener added: {eventName}");
            }
        }

        /// <summary>
        /// 이벤트 리스너 제거
        /// </summary>
        public void RemoveEventListener(string eventName, Action<SpineEventData> callback)
        {
            if (callback == null) return;

            if (eventListeners.ContainsKey(eventName))
            {
                eventListeners[eventName].Remove(callback);
                Log($"Event listener removed: {eventName}");
            }
        }

        /// <summary>
        /// 특정 이벤트의 모든 리스너 제거
        /// </summary>
        public void RemoveAllListeners(string eventName)
        {
            if (eventListeners.ContainsKey(eventName))
            {
                eventListeners.Remove(eventName);
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
        /// Spine 이벤트 발생 시 호출
        /// </summary>
        private void OnSpineEvent(TrackEntry trackEntry, Event e)
        {
            if (e == null || e.Data == null) return;

            string eventName = e.Data.Name;

            // SpineEventData 생성
            var eventData = new SpineEventData(
                eventName,
                trackEntry.Animation.Name,
                trackEntry.TrackTime / trackEntry.Animation.Duration,
                trackEntry.TrackTime
            )
            {
                StringParameter = e.String ?? "",
                IntParameter = e.Int,
                FloatParameter = e.Float
            };

            Log($"Spine Event: {eventName}");

            // 등록된 리스너 호출
            if (eventListeners.ContainsKey(eventName))
            {
                foreach (var listener in eventListeners[eventName])
                {
                    try
                    {
                        listener?.Invoke(eventData);
                    }
                    catch (Exception ex)
                    {
                        LogError($"Error invoking listener for '{eventName}': {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 애니메이션 시작 시 호출
        /// </summary>
        private void OnAnimationStart(TrackEntry trackEntry)
        {
            Log($"Animation started: {trackEntry.Animation.Name}");
        }

        /// <summary>
        /// 애니메이션 완료 시 호출
        /// </summary>
        private void OnAnimationComplete(TrackEntry trackEntry)
        {
            Log($"Animation completed: {trackEntry.Animation.Name}");
        }

        #endregion

        #region Utility

        /// <summary>
        /// 애니메이션 존재 여부 확인
        /// </summary>
        public bool HasAnimation(string animationName)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return false;

            SkeletonData skeletonData = skeletonAnimation.Skeleton.Data;
            return skeletonData.FindAnimation(animationName) != null;
        }

        /// <summary>
        /// 애니메이션 길이 가져오기
        /// </summary>
        public float GetAnimationDuration(string animationName)
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null) return 0f;

            SkeletonData skeletonData = skeletonAnimation.Skeleton.Data;
            Animation animation = skeletonData.FindAnimation(animationName);

            return animation != null ? animation.Duration : 0f;
        }

        /// <summary>
        /// 현재 트랙 엔트리 가져오기
        /// </summary>
        public TrackEntry GetTrackEntry(int trackIndex = 0)
        {
            if (skeletonAnimation == null || skeletonAnimation.AnimationState == null) return null;

            return skeletonAnimation.AnimationState.GetCurrent(trackIndex);
        }

        /// <summary>
        /// SymbolCollection 설정
        /// </summary>
        public void SetSymbolCollection(SpineSymbolCollection collection)
        {
            symbolCollection = collection;
            Log("SymbolCollection updated");
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Print Animation List")]
        private void PrintAnimationList()
        {
            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
            {
                Debug.Log("SkeletonAnimation not initialized");
                return;
            }

            SkeletonData skeletonData = skeletonAnimation.Skeleton.Data;
            Debug.Log($"===== Animation List ({skeletonData.Animations.Count}) =====");

            foreach (var animation in skeletonData.Animations)
            {
                Debug.Log($"  - {animation.Name} ({animation.Duration:F2}s)");
            }
        }

        [ContextMenu("Debug: Print Event Listeners")]
        private void PrintEventListeners()
        {
            Debug.Log($"===== Event Listeners ({eventListeners.Count}) =====");

            foreach (var kvp in eventListeners)
            {
                Debug.Log($"  Event: {kvp.Key} ({kvp.Value.Count} listeners)");
            }
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SpineAnimationController] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SpineAnimationController] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SpineAnimationController] {message}");
        }

        #endregion

#else
        private void Awake()
        {
            Debug.LogWarning("[SpineAnimationController] Spine-Unity is not installed. This component will be disabled.");
            enabled = false;
        }
#endif
    }
}
