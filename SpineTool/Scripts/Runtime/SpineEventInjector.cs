using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

#if SPINE_UNITY
using Spine;
using Spine.Unity;
#endif

namespace SpineTool
{
    /// <summary>
    /// Spine 애니메이션에 이벤트를 동적으로 주입하는 시스템
    ///
    /// 사용 방법:
    /// 1. SkeletonAnimation이 있는 GameObject에 이 컴포넌트 추가
    /// 2. 같은 GameObject의 MonoBehaviour에 [InjectSpineEvent] Attribute 사용
    /// 3. 런타임에 자동으로 이벤트가 주입됨
    ///
    /// 예시:
    /// [InjectSpineEvent("attack", "OnHitImpact", 0.5f, IntParameter = 50)]
    /// public class MyCharacter : MonoBehaviour
    /// {
    ///     void OnHitImpact(SpineEventData data)
    ///     {
    ///         Debug.Log($"Hit with power: {data.IntParameter}");
    ///     }
    /// }
    /// </summary>
    public class SpineEventInjector : MonoBehaviour
    {
#if SPINE_UNITY
        [Header("Settings")]
        [Tooltip("디버그 로그 활성화")]
        public bool enableDebugLog = true;

        [Tooltip("Spine 툴에서 설정한 이벤트도 처리")]
        public bool processSpineToolEvents = true;

        private SkeletonAnimation skeletonAnimation;
        private Dictionary<string, List<InjectionInfo>> injectionMap = new Dictionary<string, List<InjectionInfo>>();
        private Dictionary<int, Coroutine> activeCoroutines = new Dictionary<int, Coroutine>();
        private bool isInitialized = false;

        private class InjectionInfo
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
                LogError($"SkeletonAnimation not found on {gameObject.name}!");
                enabled = false;
                return;
            }

            InitializeInjection();
        }

        private void OnEnable()
        {
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Start += OnAnimationStart;
                skeletonAnimation.AnimationState.End += OnAnimationEnd;

                if (processSpineToolEvents)
                {
                    skeletonAnimation.AnimationState.Event += OnSpineToolEvent;
                }
            }
        }

        private void OnDisable()
        {
            if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
            {
                skeletonAnimation.AnimationState.Start -= OnAnimationStart;
                skeletonAnimation.AnimationState.End -= OnAnimationEnd;

                if (processSpineToolEvents)
                {
                    skeletonAnimation.AnimationState.Event -= OnSpineToolEvent;
                }
            }

            // 모든 코루틴 중지
            StopAllInjectionCoroutines();
        }

        /// <summary>
        /// Attribute를 스캔하여 이벤트 주입 정보 수집
        /// </summary>
        public void InitializeInjection()
        {
            if (isInitialized) return;

            injectionMap.Clear();

            // 같은 GameObject의 모든 MonoBehaviour 스캔
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();

            foreach (var component in components)
            {
                if (component == null || component == this) continue;

                ScanAttributesFromComponent(component);
            }

            isInitialized = true;

            Log($"Initialized with {injectionMap.Count} animation(s), total {CountTotalInjections()} event(s)");
        }

        /// <summary>
        /// 컴포넌트에서 Attribute 스캔
        /// </summary>
        private void ScanAttributesFromComponent(object target)
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
                    LogWarning($"Method '{attr.FunctionName}' not found in {targetType.Name}");
                    continue;
                }

                // 파라미터 검증
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length > 1)
                {
                    LogWarning($"Method '{attr.FunctionName}' should have 0 or 1 parameter (SpineEventData)");
                    continue;
                }

                if (parameters.Length == 1 && parameters[0].ParameterType != typeof(SpineEventData))
                {
                    LogWarning($"Method '{attr.FunctionName}' parameter should be SpineEventData, not {parameters[0].ParameterType.Name}");
                    continue;
                }

                var info = new InjectionInfo
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

                if (!injectionMap.ContainsKey(attr.AnimationName))
                {
                    injectionMap[attr.AnimationName] = new List<InjectionInfo>();
                }

                injectionMap[attr.AnimationName].Add(info);

                Log($"Registered: '{attr.FunctionName}' for '{attr.AnimationName}' @{attr.NormalizedTime:F2}");
            }
        }

        /// <summary>
        /// 애니메이션 시작 시 호출
        /// </summary>
        private void OnAnimationStart(TrackEntry trackEntry)
        {
            if (trackEntry == null || trackEntry.Animation == null) return;

            string animationName = trackEntry.Animation.Name;

            if (injectionMap.ContainsKey(animationName))
            {
                // 기존 코루틴이 있으면 중지
                int trackIndex = trackEntry.TrackIndex;
                if (activeCoroutines.ContainsKey(trackIndex))
                {
                    StopCoroutine(activeCoroutines[trackIndex]);
                    activeCoroutines.Remove(trackIndex);
                }

                // 새로운 코루틴 시작
                Coroutine coroutine = StartCoroutine(ProcessInjectionEvents(trackEntry, injectionMap[animationName]));
                activeCoroutines[trackIndex] = coroutine;

                Log($"Started injection for '{animationName}' ({injectionMap[animationName].Count} events)");
            }
        }

        /// <summary>
        /// 애니메이션 종료 시 호출
        /// </summary>
        private void OnAnimationEnd(TrackEntry trackEntry)
        {
            if (trackEntry == null) return;

            int trackIndex = trackEntry.TrackIndex;
            if (activeCoroutines.ContainsKey(trackIndex))
            {
                StopCoroutine(activeCoroutines[trackIndex]);
                activeCoroutines.Remove(trackIndex);
            }
        }

        /// <summary>
        /// 특정 시간에 이벤트 트리거
        /// </summary>
        private IEnumerator ProcessInjectionEvents(TrackEntry trackEntry, List<InjectionInfo> events)
        {
            // 시간순 정렬
            var sortedEvents = events.OrderBy(e => e.NormalizedTime).ToList();
            float animationDuration = trackEntry.Animation.Duration;

            foreach (var info in sortedEvents)
            {
                // 이벤트가 발생할 시간 계산
                float targetTime = animationDuration * info.NormalizedTime;

                // 목표 시간까지 대기
                while (trackEntry.TrackTime < targetTime)
                {
                    // TrackEntry가 유효한지 확인
                    if (trackEntry.Animation == null || trackEntry.TrackTime < 0)
                    {
                        yield break;
                    }

                    yield return null;
                }

                // 이벤트 실행
                InvokeInjectedEvent(info, trackEntry);
            }

            // 코루틴 완료 후 제거
            int trackIndex = trackEntry.TrackIndex;
            if (activeCoroutines.ContainsKey(trackIndex))
            {
                activeCoroutines.Remove(trackIndex);
            }
        }

        /// <summary>
        /// 주입된 이벤트 호출
        /// </summary>
        private void InvokeInjectedEvent(InjectionInfo info, TrackEntry trackEntry)
        {
            try
            {
                ParameterInfo[] parameters = info.Method.GetParameters();

                if (parameters.Length == 0)
                {
                    // 파라미터 없음
                    info.Method.Invoke(info.Target, null);
                }
                else
                {
                    // SpineEventData 파라미터
                    var eventData = new SpineEventData(
                        info.FunctionName,
                        trackEntry.Animation.Name,
                        info.NormalizedTime,
                        trackEntry.TrackTime
                    )
                    {
                        StringParameter = info.StringParameter,
                        IntParameter = info.IntParameter,
                        FloatParameter = info.FloatParameter
                    };

                    info.Method.Invoke(info.Target, new object[] { eventData });
                }

                Log($"Triggered: '{info.FunctionName}' @{trackEntry.TrackTime:F2}s");
            }
            catch (Exception ex)
            {
                LogError($"Failed to invoke '{info.FunctionName}': {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Spine 툴에서 설정한 이벤트 처리
        /// </summary>
        private void OnSpineToolEvent(TrackEntry trackEntry, Event e)
        {
            if (e == null || e.Data == null) return;

            Log($"Spine Tool Event: {e.Data.Name} (Int: {e.Int}, Float: {e.Float}, String: '{e.String}')");

            // SpineEventData로 변환
            var eventData = new SpineEventData(
                e.Data.Name,
                trackEntry.Animation.Name,
                trackEntry.TrackTime / trackEntry.Animation.Duration,
                trackEntry.TrackTime
            )
            {
                StringParameter = e.String ?? "",
                IntParameter = e.Int,
                FloatParameter = e.Float
            };

            // 같은 GameObject의 모든 컴포넌트에 브로드캐스트
            BroadcastEventToComponents(eventData);
        }

        /// <summary>
        /// 모든 컴포넌트에 이벤트 브로드캐스트
        /// </summary>
        private void BroadcastEventToComponents(SpineEventData eventData)
        {
            MonoBehaviour[] components = GetComponents<MonoBehaviour>();

            foreach (var component in components)
            {
                if (component == null || component == this) continue;

                // OnSpineEvent 메서드 찾기
                MethodInfo method = component.GetType().GetMethod(
                    "OnSpineEvent",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                );

                if (method != null)
                {
                    try
                    {
                        method.Invoke(component, new object[] { eventData });
                    }
                    catch (Exception ex)
                    {
                        LogError($"Failed to broadcast event to {component.GetType().Name}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// 모든 주입 코루틴 중지
        /// </summary>
        private void StopAllInjectionCoroutines()
        {
            foreach (var coroutine in activeCoroutines.Values)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
            activeCoroutines.Clear();
        }

        /// <summary>
        /// 특정 오브젝트의 이벤트 수동 등록
        /// </summary>
        public void RegisterComponent(MonoBehaviour component)
        {
            if (component == null) return;

            ScanAttributesFromComponent(component);
            Log($"Manually registered component: {component.GetType().Name}");
        }

        /// <summary>
        /// 재초기화 (런타임에 컴포넌트 추가된 경우)
        /// </summary>
        [ContextMenu("Re-Initialize")]
        public void ReInitialize()
        {
            isInitialized = false;
            InitializeInjection();
        }

        /// <summary>
        /// 디버그: 등록된 이벤트 목록 출력
        /// </summary>
        [ContextMenu("Debug: Print Registered Events")]
        public void PrintRegisteredEvents()
        {
            Debug.Log($"===== SpineEventInjector: Registered Events =====");

            if (injectionMap.Count == 0)
            {
                Debug.Log("  No events registered.");
                return;
            }

            foreach (var kvp in injectionMap)
            {
                Debug.Log($"  Animation: '{kvp.Key}' ({kvp.Value.Count} events)");
                foreach (var info in kvp.Value.OrderBy(i => i.NormalizedTime))
                {
                    Debug.Log($"    - {info.NormalizedTime:F2}: {info.FunctionName}()" +
                        $" [Int:{info.IntParameter}, Float:{info.FloatParameter:F2}, String:'{info.StringParameter}']");
                }
            }

            Debug.Log($"================================================");
        }

        private int CountTotalInjections()
        {
            return injectionMap.Values.Sum(list => list.Count);
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SpineEventInjector] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SpineEventInjector] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SpineEventInjector] {message}");
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
