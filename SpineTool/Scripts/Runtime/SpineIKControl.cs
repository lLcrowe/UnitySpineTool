using UnityEngine;
using System.Collections.Generic;

#if SPINE_UNITY
using Spine;
using Spine.Unity;
#endif

namespace SpineTool
{
    /// <summary>
    /// Spine IK (Inverse Kinematics) 제어 모듈
    ///
    /// 기능:
    /// - IK Constraint 온/오프
    /// - IK 가중치 (Weight) 조절
    /// - IK 타겟 설정
    /// - 여러 IK Constraint 동시 제어
    ///
    /// 사용 예:
    /// - 손으로 오브젝트 잡기
    /// - 발이 지면에 붙도록
    /// - 시선 추적
    /// </summary>
    public class SpineIKControl : MonoBehaviour
    {
#if SPINE_UNITY
        [Header("References")]
        [SerializeField] private SkeletonAnimation skeletonAnimation;

        [Header("IK Settings")]
        [Tooltip("제어할 IK Constraint 이름들")]
        [SerializeField] private List<string> ikConstraintNames = new List<string>();

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = false;

        // IK Constraint 캐시
        private Dictionary<string, IkConstraint> ikConstraints = new Dictionary<string, IkConstraint>();
        private bool isInitialized = false;

        // 프로퍼티
        public SkeletonAnimation SkeletonAnimation => skeletonAnimation;

        #region Initialization

        private void Awake()
        {
            if (skeletonAnimation == null)
            {
                skeletonAnimation = GetComponent<SkeletonAnimation>();
            }

            if (skeletonAnimation == null)
            {
                LogError("SkeletonAnimation not found!");
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            InitializeIKConstraints();
        }

        /// <summary>
        /// IK Constraint 초기화
        /// </summary>
        private void InitializeIKConstraints()
        {
            if (isInitialized) return;

            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
            {
                LogError("Skeleton not ready!");
                return;
            }

            ikConstraints.Clear();

            // IK Constraint 찾기
            foreach (var ikName in ikConstraintNames)
            {
                if (string.IsNullOrEmpty(ikName)) continue;

                IkConstraint ikConstraint = skeletonAnimation.Skeleton.FindConstraint<IkConstraint>(ikName);
                if (ikConstraint != null)
                {
                    ikConstraints[ikName] = ikConstraint;
                    Log($"IK Constraint found: {ikName}");
                }
                else
                {
                    LogWarning($"IK Constraint not found: {ikName}");
                }
            }

            isInitialized = true;
            Log($"IK Module initialized with {ikConstraints.Count} constraints");
        }

        /// <summary>
        /// 재초기화 (런타임에 Skeleton이 바뀐 경우)
        /// </summary>
        [ContextMenu("Re-Initialize IK Constraints")]
        public void ReInitialize()
        {
            isInitialized = false;
            InitializeIKConstraints();
        }

        #endregion

        #region IK Control

        /// <summary>
        /// IK 활성화/비활성화
        /// </summary>
        public void SetIKActive(string ikName, bool active)
        {
            if (!ikConstraints.ContainsKey(ikName))
            {
                LogWarning($"IK Constraint not found: {ikName}");
                return;
            }

            IkConstraint ik = ikConstraints[ikName];
            ik.Pose.Mix = active ? 1f : 0f;

            Log($"IK '{ikName}' {(active ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// IK 가중치 설정 (0.0 ~ 1.0)
        /// </summary>
        public void SetIKWeight(string ikName, float weight)
        {
            if (!ikConstraints.ContainsKey(ikName))
            {
                LogWarning($"IK Constraint not found: {ikName}");
                return;
            }

            IkConstraint ik = ikConstraints[ikName];
            ik.Pose.Mix = Mathf.Clamp01(weight);

            Log($"IK '{ikName}' weight set to {weight:F2}");
        }

        /// <summary>
        /// IK 가중치 부드럽게 변경 (Coroutine)
        /// </summary>
        public void SetIKWeightSmooth(string ikName, float targetWeight, float duration)
        {
            if (!ikConstraints.ContainsKey(ikName))
            {
                LogWarning($"IK Constraint not found: {ikName}");
                return;
            }

            StartCoroutine(SmoothIKWeight(ikName, targetWeight, duration));
        }

        private System.Collections.IEnumerator SmoothIKWeight(string ikName, float targetWeight, float duration)
        {
            IkConstraint ik = ikConstraints[ikName];
            float startWeight = ik.Pose.Mix;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                ik.Pose.Mix = Mathf.Lerp(startWeight, targetWeight, t);
                yield return null;
            }

            ik.Pose.Mix = targetWeight;
            Log($"IK '{ikName}' smoothly changed to {targetWeight:F2}");
        }

        /// <summary>
        /// IK Bend Direction 설정 (양수/음수)
        /// </summary>
        public void SetIKBendDirection(string ikName, int direction)
        {
            if (!ikConstraints.ContainsKey(ikName))
            {
                LogWarning($"IK Constraint not found: {ikName}");
                return;
            }

            IkConstraint ik = ikConstraints[ikName];
            ik.Pose.BendDirection = direction > 0 ? 1 : -1;

            Log($"IK '{ikName}' bend direction set to {ik.Pose.BendDirection}");
        }

        /// <summary>
        /// IK Compress 설정
        /// </summary>
        public void SetIKCompress(string ikName, bool compress)
        {
            if (!ikConstraints.ContainsKey(ikName))
            {
                LogWarning($"IK Constraint not found: {ikName}");
                return;
            }

            IkConstraint ik = ikConstraints[ikName];
            ik.Pose.Compress = compress;

            Log($"IK '{ikName}' compress set to {compress}");
        }

        /// <summary>
        /// IK Stretch 설정
        /// </summary>
        public void SetIKStretch(string ikName, bool stretch)
        {
            if (!ikConstraints.ContainsKey(ikName))
            {
                LogWarning($"IK Constraint not found: {ikName}");
                return;
            }

            IkConstraint ik = ikConstraints[ikName];
            ik.Pose.Stretch = stretch;

            Log($"IK '{ikName}' stretch set to {stretch}");
        }

        #endregion

        #region Batch Control

        /// <summary>
        /// 모든 IK 활성화/비활성화
        /// </summary>
        public void SetAllIKActive(bool active)
        {
            foreach (var kvp in ikConstraints)
            {
                kvp.Value.Pose.Mix = active ? 1f : 0f;
            }

            Log($"All IK {(active ? "enabled" : "disabled")}");
        }

        /// <summary>
        /// 모든 IK 가중치 설정
        /// </summary>
        public void SetAllIKWeight(float weight)
        {
            float clampedWeight = Mathf.Clamp01(weight);
            foreach (var kvp in ikConstraints)
            {
                kvp.Value.Pose.Mix = clampedWeight;
            }

            Log($"All IK weight set to {clampedWeight:F2}");
        }

        /// <summary>
        /// 모든 IK 부드럽게 변경
        /// </summary>
        public void SetAllIKWeightSmooth(float targetWeight, float duration)
        {
            foreach (var ikName in ikConstraints.Keys)
            {
                SetIKWeightSmooth(ikName, targetWeight, duration);
            }
        }

        #endregion

        #region Query

        /// <summary>
        /// IK가 활성화되어 있는지 확인
        /// </summary>
        public bool IsIKActive(string ikName)
        {
            if (!ikConstraints.ContainsKey(ikName)) return false;
            return ikConstraints[ikName].Pose.Mix > 0f;
        }

        /// <summary>
        /// IK 가중치 가져오기
        /// </summary>
        public float GetIKWeight(string ikName)
        {
            if (!ikConstraints.ContainsKey(ikName)) return 0f;
            return ikConstraints[ikName].Pose.Mix;
        }

        /// <summary>
        /// IK Constraint 가져오기
        /// </summary>
        public IkConstraint GetIKConstraint(string ikName)
        {
            if (!ikConstraints.ContainsKey(ikName)) return null;
            return ikConstraints[ikName];
        }

        /// <summary>
        /// 등록된 모든 IK 이름 가져오기
        /// </summary>
        public List<string> GetAllIKNames()
        {
            return new List<string>(ikConstraints.Keys);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// IK Constraint 추가 (런타임)
        /// </summary>
        public void AddIKConstraint(string ikName)
        {
            if (ikConstraints.ContainsKey(ikName))
            {
                LogWarning($"IK Constraint already exists: {ikName}");
                return;
            }

            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
            {
                LogError("Skeleton not ready!");
                return;
            }

            IkConstraint ikConstraint = skeletonAnimation.Skeleton.FindConstraint<IkConstraint>(ikName);
            if (ikConstraint != null)
            {
                ikConstraints[ikName] = ikConstraint;
                ikConstraintNames.Add(ikName);
                Log($"IK Constraint added: {ikName}");
            }
            else
            {
                LogWarning($"IK Constraint not found: {ikName}");
            }
        }

        /// <summary>
        /// IK Constraint 제거
        /// </summary>
        public void RemoveIKConstraint(string ikName)
        {
            if (ikConstraints.Remove(ikName))
            {
                ikConstraintNames.Remove(ikName);
                Log($"IK Constraint removed: {ikName}");
            }
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Print All IK Constraints")]
        private void PrintAllIKConstraints()
        {
            Debug.Log("===== IK Constraints =====");

            if (skeletonAnimation == null || skeletonAnimation.Skeleton == null)
            {
                Debug.Log("Skeleton not ready!");
                return;
            }

            var constraints = skeletonAnimation.Skeleton.Constraints;
            int ikCount = 0;
            foreach (var constraint in constraints)
            {
                if (constraint is not IkConstraint ik) continue;

                ikCount++;
                Debug.Log($"  - {ik.Data.Name} (Active: {ik.Pose.Mix > 0f}, Weight: {ik.Pose.Mix:F2})");
            }

            Debug.Log($"Total IK Constraints: {ikCount}");
        }

        [ContextMenu("Test: Toggle All IK")]
        private void TestToggleAllIK()
        {
            bool anyActive = false;
            foreach (var ik in ikConstraints.Values)
            {
                if (ik.Pose.Mix > 0f)
                {
                    anyActive = true;
                    break;
                }
            }

            SetAllIKActive(!anyActive);
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SpineIKControl] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SpineIKControl] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SpineIKControl] {message}");
        }

        #endregion

#else
        private void Awake()
        {
            Debug.LogWarning("[SpineIKControl] Spine-Unity is not installed. This component will be disabled.");
            enabled = false;
        }
#endif
    }
}
