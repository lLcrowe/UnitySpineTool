using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using InteractAnimation.Core;

#if SPINE_UNITY
using Spine;
using Spine.Unity;
#endif

namespace InteractAnimation.AnimationSystems.Spine
{
    /// <summary>
    /// Spine 애니메이션에 이벤트를 동적으로 주입하는 시스템
    /// InjectSpineEvent Attribute를 스캔하여 런타임에 Spine 이벤트를 등록합니다.
    /// </summary>
    public class SpineEventInjector : MonoBehaviour
    {
#if SPINE_UNITY
        private SkeletonAnimation skeletonAnimation;
        private Dictionary<string, List<EventInjectionInfo>> injectionInfoMap = new Dictionary<string, List<EventInjectionInfo>>();
        private bool isInitialized = false;

        private class EventInjectionInfo
        {
            public string AnimationName;
            public string FunctionName;
            public float NormalizedTime;
            public string StringParameter;
            public int IntParameter;
            public float FloatParameter;
            public MethodInfo Method;
            public object Target;
        }

        private void Awake()
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
            if (skeletonAnimation == null)
            {
                Debug.LogError($"[SpineEventInjector] SkeletonAnimation not found on {gameObject.name}!");
                return;
            }

            InitializeInjection();
        }

        /// <summary>
        /// Attribute를 스캔하여 이벤트 주입 정보 수집
        /// </summary>
        public void InitializeInjection()
        {
            if (isInitialized) return;

            injectionInfoMap.Clear();

            // InteractableObjectBase를 상속한 컴포넌트 찾기
            var interactableComponents = GetComponents<InteractableObjectBase>();

            foreach (var component in interactableComponents)
            {
                ScanAttributes(component);
            }

            // TrackEntry 이벤트 등록
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Start += OnAnimationStart;
                skeletonAnimation.AnimationState.Event += OnSpineEvent;
            }

            isInitialized = true;

            Debug.Log($"[SpineEventInjector] Initialized with {injectionInfoMap.Count} animation event mappings");
        }

        /// <summary>
        /// Attribute 스캔
        /// </summary>
        private void ScanAttributes(object target)
        {
            Type targetType = target.GetType();

            // InjectSpineEvent Attribute 스캔
            var attributes = targetType.GetCustomAttributes<InjectSpineEventAttribute>(true);

            foreach (var attr in attributes)
            {
                // 메서드 찾기
                MethodInfo method = targetType.GetMethod(
                    attr.FunctionName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (method == null)
                {
                    Debug.LogWarning($"[SpineEventInjector] Method '{attr.FunctionName}' not found in {targetType.Name}");
                    continue;
                }

                var info = new EventInjectionInfo
                {
                    AnimationName = attr.AnimationName,
                    FunctionName = attr.FunctionName,
                    NormalizedTime = attr.NormalizedTime,
                    StringParameter = attr.StringParameter,
                    IntParameter = attr.IntParameter,
                    FloatParameter = attr.FloatParameter,
                    Method = method,
                    Target = target
                };

                if (!injectionInfoMap.ContainsKey(attr.AnimationName))
                {
                    injectionInfoMap[attr.AnimationName] = new List<EventInjectionInfo>();
                }

                injectionInfoMap[attr.AnimationName].Add(info);

                Debug.Log($"[SpineEventInjector] Registered event '{attr.FunctionName}' for animation '{attr.AnimationName}' at {attr.NormalizedTime:F2}");
            }
        }

        /// <summary>
        /// 애니메이션 시작 시 호출
        /// </summary>
        private void OnAnimationStart(TrackEntry trackEntry)
        {
            if (trackEntry == null || trackEntry.Animation == null) return;

            string animationName = trackEntry.Animation.Name;

            if (injectionInfoMap.ContainsKey(animationName))
            {
                // 해당 애니메이션의 이벤트들을 시간 기반으로 실행하도록 Coroutine 시작
                StartCoroutine(TriggerEventsAtTime(trackEntry, injectionInfoMap[animationName]));
            }
        }

        /// <summary>
        /// 특정 시간에 이벤트 트리거
        /// </summary>
        private System.Collections.IEnumerator TriggerEventsAtTime(TrackEntry trackEntry, List<EventInjectionInfo> events)
        {
            // 시간순 정렬
            var sortedEvents = events.OrderBy(e => e.NormalizedTime).ToList();

            foreach (var info in sortedEvents)
            {
                // 이벤트가 발생할 시간까지 대기
                float targetTime = trackEntry.Animation.Duration * info.NormalizedTime;

                while (trackEntry.TrackTime < targetTime && trackEntry.TrackTime >= 0)
                {
                    yield return null;
                }

                // TrackEntry가 여전히 유효한지 확인
                if (trackEntry.TrackTime < 0) yield break;

                // 이벤트 호출
                try
                {
                    // 파라미터가 있는 경우
                    ParameterInfo[] parameters = info.Method.GetParameters();

                    if (parameters.Length == 0)
                    {
                        // 파라미터 없음
                        info.Method.Invoke(info.Target, null);
                    }
                    else if (parameters.Length == 1)
                    {
                        // AnimationEventData 파라미터
                        var eventData = new AnimationEventData(info.FunctionName, info.NormalizedTime)
                        {
                            stringParameter = info.StringParameter,
                            intParameter = info.IntParameter,
                            floatParameter = info.FloatParameter
                        };

                        info.Method.Invoke(info.Target, new object[] { eventData });
                    }

                    Debug.Log($"[SpineEventInjector] Triggered '{info.FunctionName}' at {trackEntry.TrackTime:F2}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SpineEventInjector] Failed to invoke '{info.FunctionName}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Spine 자체 이벤트 처리 (Spine 애니메이션 툴에서 설정한 이벤트)
        /// </summary>
        private void OnSpineEvent(TrackEntry trackEntry, global::Spine.Event e)
        {
            if (e == null) return;

            Debug.Log($"[SpineEventInjector] Spine Event: {e.Data.Name} (Int: {e.Int}, Float: {e.Float}, String: {e.String})");

            // Spine 이벤트를 AnimationEventData로 변환하여 전달
            var eventData = new AnimationEventData(e.Data.Name, trackEntry.TrackTime / trackEntry.Animation.Duration)
            {
                stringParameter = e.String,
                intParameter = e.Int,
                floatParameter = e.Float
            };

            // 등록된 모든 InteractableObjectBase에 이벤트 전달
            var interactableComponents = GetComponents<InteractableObjectBase>();
            foreach (var component in interactableComponents)
            {
                component.OnAnimationEvent(eventData);
            }
        }

        private void OnDestroy()
        {
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Start -= OnAnimationStart;
                skeletonAnimation.AnimationState.Event -= OnSpineEvent;
            }
        }

        /// <summary>
        /// 특정 오브젝트의 이벤트 수동 등록
        /// </summary>
        public void RegisterEvents(object target)
        {
            ScanAttributes(target);
        }

        /// <summary>
        /// 디버그: 등록된 이벤트 목록 출력
        /// </summary>
        [ContextMenu("Debug: Print Registered Events")]
        public void PrintRegisteredEvents()
        {
            Debug.Log($"[SpineEventInjector] Registered Events:");

            foreach (var kvp in injectionInfoMap)
            {
                Debug.Log($"  Animation: {kvp.Key}");
                foreach (var info in kvp.Value)
                {
                    Debug.Log($"    - {info.FunctionName} at {info.NormalizedTime:F2}");
                }
            }
        }
#else
        private void Awake()
        {
            Debug.LogWarning("[SpineEventInjector] Spine-Unity is not installed. This component will be disabled.");
            enabled = false;
        }
#endif
    }
}
