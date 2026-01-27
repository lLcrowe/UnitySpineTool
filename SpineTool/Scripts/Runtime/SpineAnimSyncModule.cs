using UnityEngine;
using System;

#if SPINE_UNITY
using Spine;
using Spine.Unity;
#endif

namespace SpineTool
{
    /// <summary>
    /// 두 개의 Spine 애니메이션을 동기화하는 모듈
    ///
    /// 사용 예:
    /// - 처형 모션 (공격자 + 피해자)
    /// - 보물상자 열기 (캐릭터 + 상자)
    /// - 그래플 기술 (캐릭터 + 적)
    ///
    /// 기능:
    /// - Master-Slave 애니메이션 동기화
    /// - 위치/방향 자동 매칭
    /// - Transform 부모-자식 관계 설정
    /// - 동기화 완료 콜백
    /// </summary>
    public class SpineAnimSyncModule : MonoBehaviour
    {
#if SPINE_UNITY
        [Header("Master (주 캐릭터)")]
        [SerializeField] private SpineAnimModule masterAnimModule;
        [SerializeField] private Transform masterTransform;

        [Header("Slave (종속 캐릭터)")]
        [SerializeField] private SpineAnimModule slaveAnimModule;
        [SerializeField] private Transform slaveTransform;

        [Header("Sync Settings")]
        [Tooltip("Slave를 Master의 자식으로 설정")]
        [SerializeField] private bool parentSlaveToMaster = true;

        [Tooltip("Slave의 로컬 위치 오프셋")]
        [SerializeField] private Vector3 slaveLocalOffset = Vector3.zero;

        [Tooltip("Slave의 방향을 Master와 반대로 설정")]
        [SerializeField] private bool flipSlaveDirection = true;

        [Header("Attach Point (Optional)")]
        [Tooltip("Master의 특정 본에 Slave를 부착 (예: 손)")]
        [SerializeField] private string attachBoneName = "";
        private Transform attachPoint;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLog = false;

        // 동기화 상태
        private bool isSyncing = false;
        private string currentMasterAnimation = "";
        private string currentSlaveAnimation = "";

        // 콜백
        public event Action OnSyncStarted;
        public event Action OnSyncCompleted;

        // 프로퍼티
        public bool IsSyncing => isSyncing;
        public string CurrentMasterAnimation => currentMasterAnimation;
        public string CurrentSlaveAnimation => currentSlaveAnimation;

        #region Setup

        private void Awake()
        {
            // Master 자동 설정
            if (masterAnimModule == null)
            {
                masterAnimModule = GetComponent<SpineAnimModule>();
            }

            if (masterTransform == null && masterAnimModule != null)
            {
                masterTransform = masterAnimModule.transform;
            }

            // Attach Point 설정
            if (!string.IsNullOrEmpty(attachBoneName) && masterAnimModule != null)
            {
                SetupAttachPoint();
            }
        }

        private void SetupAttachPoint()
        {
            if (masterAnimModule.SkeletonAnimation == null) return;

            Bone bone = masterAnimModule.SkeletonAnimation.Skeleton.FindBone(attachBoneName);
            if (bone != null)
            {
                // Bone Transform 생성
                GameObject attachPointObj = new GameObject($"AttachPoint_{attachBoneName}");
                attachPointObj.transform.SetParent(masterTransform);
                attachPoint = attachPointObj.transform;

                // BoneFollower 추가 (Spine의 본을 따라가도록)
                var follower = attachPointObj.AddComponent<BoneFollower>();
                follower.skeletonRenderer = masterAnimModule.SkeletonAnimation;
                follower.SetBone(attachBoneName);

                Log($"Attach point created: {attachBoneName}");
            }
            else
            {
                LogWarning($"Bone not found: {attachBoneName}");
            }
        }

        #endregion

        #region Sync Control

        /// <summary>
        /// 두 애니메이션 동기화 시작
        /// </summary>
        public void StartSync(string masterAnimName, string slaveAnimName, bool loop = false)
        {
            if (masterAnimModule == null || slaveAnimModule == null)
            {
                LogError("Master or Slave AnimModule is null!");
                return;
            }

            isSyncing = true;
            currentMasterAnimation = masterAnimName;
            currentSlaveAnimation = slaveAnimName;

            // Slave 위치/방향 설정
            SetupSlaveTransform();

            // 애니메이션 재생
            masterAnimModule.PlayAnimation(masterAnimName, loop);
            slaveAnimModule.PlayAnimation(slaveAnimName, loop);

            // 완료 콜백 등록
            RegisterCompletionCallbacks();

            OnSyncStarted?.Invoke();

            Log($"Sync started: Master[{masterAnimName}] + Slave[{slaveAnimName}]");
        }

        /// <summary>
        /// Symbol ID로 동기화 시작
        /// </summary>
        public void StartSyncBySymbol(string masterSymbolId, string slaveSymbolId, bool loop = false)
        {
            if (masterAnimModule == null || slaveAnimModule == null)
            {
                LogError("Master or Slave AnimModule is null!");
                return;
            }

            isSyncing = true;

            // Slave 위치/방향 설정
            SetupSlaveTransform();

            // Symbol ID로 재생
            masterAnimModule.PlayAnimationBySymbolId(masterSymbolId, loop);
            slaveAnimModule.PlayAnimationBySymbolId(slaveSymbolId, loop);

            // 완료 콜백 등록
            RegisterCompletionCallbacks();

            OnSyncStarted?.Invoke();

            Log($"Sync started by symbol: Master[{masterSymbolId}] + Slave[{slaveSymbolId}]");
        }

        /// <summary>
        /// 동기화 중지
        /// </summary>
        public void StopSync()
        {
            if (!isSyncing) return;

            // 애니메이션 정지
            if (masterAnimModule != null)
                masterAnimModule.StopAnimation();

            if (slaveAnimModule != null)
                slaveAnimModule.StopAnimation();

            // Slave 분리
            ReleaseSlave();

            isSyncing = false;
            currentMasterAnimation = "";
            currentSlaveAnimation = "";

            Log("Sync stopped");
        }

        #endregion

        #region Transform Control

        /// <summary>
        /// Slave Transform 설정
        /// </summary>
        private void SetupSlaveTransform()
        {
            if (slaveTransform == null) return;

            if (parentSlaveToMaster)
            {
                // Attach Point가 있으면 그것에 부착, 없으면 Master에 부착
                Transform parent = attachPoint != null ? attachPoint : masterTransform;
                slaveTransform.SetParent(parent);
                slaveTransform.localPosition = slaveLocalOffset;

                // 방향 설정
                if (flipSlaveDirection)
                {
                    // Master와 반대 방향
                    slaveTransform.localScale = new Vector3(
                        -Mathf.Abs(slaveTransform.localScale.x),
                        slaveTransform.localScale.y,
                        slaveTransform.localScale.z
                    );
                }
            }
            else
            {
                // 부모 없이 절대 위치 설정
                Vector3 targetPos = masterTransform.position + slaveLocalOffset;
                slaveTransform.position = targetPos;

                if (flipSlaveDirection)
                {
                    // Master와 반대 방향
                    float masterScaleX = masterTransform.localScale.x;
                    slaveTransform.localScale = new Vector3(
                        -Mathf.Sign(masterScaleX) * Mathf.Abs(slaveTransform.localScale.x),
                        slaveTransform.localScale.y,
                        slaveTransform.localScale.z
                    );
                }
            }

            Log("Slave transform setup completed");
        }

        /// <summary>
        /// Slave 분리
        /// </summary>
        private void ReleaseSlave()
        {
            if (slaveTransform == null) return;

            if (parentSlaveToMaster)
            {
                slaveTransform.SetParent(null);
            }

            Log("Slave released");
        }

        /// <summary>
        /// Slave 위치 수동 설정 (런타임)
        /// </summary>
        public void SetSlaveOffset(Vector3 offset)
        {
            slaveLocalOffset = offset;
            if (isSyncing && slaveTransform != null)
            {
                slaveTransform.localPosition = offset;
            }
        }

        #endregion

        #region Callbacks

        private void RegisterCompletionCallbacks()
        {
            if (masterAnimModule == null) return;

            // Master 애니메이션 완료 시 호출
            TrackEntry masterEntry = masterAnimModule.GetTrackEntry(0);
            if (masterEntry != null)
            {
                masterEntry.Complete += OnMasterAnimationComplete;
            }
        }

        private void OnMasterAnimationComplete(TrackEntry trackEntry)
        {
            if (!isSyncing) return;

            Log("Sync animation completed");

            // 자동으로 분리
            ReleaseSlave();

            isSyncing = false;
            OnSyncCompleted?.Invoke();
        }

        #endregion

        #region Public Helpers

        /// <summary>
        /// Master 설정 (런타임)
        /// </summary>
        public void SetMaster(SpineAnimModule master)
        {
            masterAnimModule = master;
            masterTransform = master != null ? master.transform : null;
        }

        /// <summary>
        /// Slave 설정 (런타임)
        /// </summary>
        public void SetSlave(SpineAnimModule slave)
        {
            slaveAnimModule = slave;
            slaveTransform = slave != null ? slave.transform : null;
        }

        /// <summary>
        /// Attach Bone 설정 (런타임)
        /// </summary>
        public void SetAttachBone(string boneName)
        {
            attachBoneName = boneName;
            SetupAttachPoint();
        }

        #endregion

        #region Debug

        [ContextMenu("Test: Start Sync")]
        private void TestStartSync()
        {
            StartSync("attack", "hit_react", false);
        }

        [ContextMenu("Test: Stop Sync")]
        private void TestStopSync()
        {
            StopSync();
        }

        private void Log(string message)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[SpineAnimSyncModule] {message}");
            }
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SpineAnimSyncModule] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SpineAnimSyncModule] {message}");
        }

        private void OnDrawGizmosSelected()
        {
            if (masterTransform == null || slaveTransform == null) return;

            // Master → Slave 연결선
            Gizmos.color = isSyncing ? Color.green : Color.gray;
            Gizmos.DrawLine(masterTransform.position, slaveTransform.position);

            // Attach Point
            if (attachPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(attachPoint.position, 0.1f);
            }

            // Slave Offset
            if (masterTransform != null)
            {
                Vector3 offsetPos = masterTransform.position + slaveLocalOffset;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(offsetPos, 0.05f);
            }
        }

        #endregion

#else
        private void Awake()
        {
            Debug.LogWarning("[SpineAnimSyncModule] Spine-Unity is not installed. This component will be disabled.");
            enabled = false;
        }
#endif
    }
}
