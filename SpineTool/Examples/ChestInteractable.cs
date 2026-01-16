using UnityEngine;
using InteractAnimation.Core;
using InteractAnimation.AnimationSystems.Spine;

namespace InteractAnimation.Examples
{
    /// <summary>
    /// Spine을 사용하는 상자 상호작용 예제
    /// 외부에서 OnInteractStart() 호출로 애니메이션 재생
    /// 애니메이션 이벤트를 사용하여 특정 프레임에서 보상 생성
    /// </summary>
    public class ChestInteractable : InteractableObjectBase
    {
        [Header("Chest Settings")]
        [SerializeField] private string chestId = "chest_01";
        [SerializeField] private bool isOpened = false;

        [Header("Spine Settings")]
        [SerializeField] private SpineSymbolCollection symbolCollection;
        [SerializeField] private string openSymbolId = "chest_open";
        [SerializeField] private string idleSymbolId = "chest_idle";

        [Header("Reward Settings")]
        [SerializeField] private GameObject rewardPrefab;
        [SerializeField] private Transform rewardSpawnPoint;
        [SerializeField] private ParticleSystem openParticle;

        private SpineAnimationSystem spineSystem;
        private bool rewardSpawned = false;

        protected override void Start()
        {
            // Spine 애니메이션 시스템 생성 및 설정
            spineSystem = gameObject.GetComponent<SpineAnimationSystem>();
            if (spineSystem == null)
            {
                spineSystem = gameObject.AddComponent<SpineAnimationSystem>();
            }

            SetAnimationSystem(spineSystem);

            // 매니저에 등록
            if (Managers.AnimationModuleManager.Instance != null)
            {
                Managers.AnimationModuleManager.Instance.RegisterInteractableObject(chestId, this);
            }

            // 애니메이션 이벤트 리스너 등록
            RegisterEventListener("ChestOpenStart", OnChestOpenStartEvent);
            RegisterEventListener("ChestSpawnReward", OnChestSpawnRewardEvent);
            RegisterEventListener("ChestOpenComplete", OnChestOpenCompleteEvent);

            base.Start();
        }

        protected override void OnInteractStartCustom()
        {
            if (isOpened)
            {
                Debug.Log("[ChestInteractable] Chest already opened");
                return;
            }

            isOpened = true;
            rewardSpawned = false;

            // Spine 심볼을 사용한 애니메이션 재생
            if (spineSystem != null)
            {
#if SPINE_UNITY
                spineSystem.PlayAnimationBySymbolId(openSymbolId, false);
#endif
            }

            Debug.Log("[ChestInteractable] Chest opening...");
        }

        protected override void OnInteractEndCustom()
        {
            Debug.Log("[ChestInteractable] Interaction ended");
        }

        private void SpawnReward()
        {
            if (rewardPrefab != null && rewardSpawnPoint != null)
            {
                Instantiate(rewardPrefab, rewardSpawnPoint.position, Quaternion.identity);
                Debug.Log("[ChestInteractable] Reward spawned");
            }
        }

        protected override float GetAnimationDuration(string animationName)
        {
#if SPINE_UNITY
            if (spineSystem != null)
            {
                return spineSystem.GetAnimationDuration(animationName);
            }
#endif
            return base.GetAnimationDuration(animationName);
        }

        #region Animation Event Handlers

        /// <summary>
        /// 애니메이션 이벤트: 상자 열기 시작
        /// Spine 애니메이션의 Event로 설정
        /// </summary>
        private void OnChestOpenStartEvent(Core.AnimationEventData eventData)
        {
            Debug.Log($"[ChestInteractable] Animation Event: Chest open started at {eventData.normalizedTime}");

            // 파티클 재생
            if (openParticle != null)
            {
                openParticle.Play();
            }
        }

        /// <summary>
        /// 애니메이션 이벤트: 보상 생성
        /// 애니메이션의 적절한 타이밍에 보상을 생성
        /// </summary>
        private void OnChestSpawnRewardEvent(Core.AnimationEventData eventData)
        {
            Debug.Log($"[ChestInteractable] Animation Event: Spawning reward at {eventData.normalizedTime}");

            if (!rewardSpawned)
            {
                SpawnReward();
                rewardSpawned = true;
            }
        }

        /// <summary>
        /// 애니메이션 이벤트: 상자 열기 완료
        /// </summary>
        private void OnChestOpenCompleteEvent(Core.AnimationEventData eventData)
        {
            Debug.Log($"[ChestInteractable] Animation Event: Chest open completed");
        }

        /// <summary>
        /// 커스텀 애니메이션 이벤트 처리 (오버라이드)
        /// </summary>
        protected override void OnAnimationEventCustom(Core.AnimationEventData eventData)
        {
            Debug.Log($"[ChestInteractable] Custom event: {eventData.eventName}");

            // Spine Event의 추가 파라미터 활용 예시
            if (eventData.intParameter > 0)
            {
                Debug.Log($"  - Int Parameter: {eventData.intParameter}");
            }
            if (!string.IsNullOrEmpty(eventData.stringParameter))
            {
                Debug.Log($"  - String Parameter: {eventData.stringParameter}");
            }
        }

        #endregion

        private void OnDestroy()
        {
            // 이벤트 리스너 해제
            UnregisterEventListener("ChestOpenStart", OnChestOpenStartEvent);
            UnregisterEventListener("ChestSpawnReward", OnChestSpawnRewardEvent);
            UnregisterEventListener("ChestOpenComplete", OnChestOpenCompleteEvent);

            // 매니저에서 등록 해제
            if (Managers.AnimationModuleManager.Instance != null)
            {
                Managers.AnimationModuleManager.Instance.UnregisterInteractableObject(chestId);
            }
        }

        // 심볼 컬렉션 설정 (런타임에서 사용 가능)
        public void SetSymbolCollection(SpineSymbolCollection collection)
        {
            symbolCollection = collection;
        }
    }
}
