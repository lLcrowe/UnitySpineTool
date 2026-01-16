using UnityEngine;
using InteractAnimation.Core;
using InteractAnimation.AnimationSystems.Spine;

namespace InteractAnimation.Examples
{
    /// <summary>
    /// 애니메이션 이벤트 자동 주입을 사용하는 상자 예제 (Spine)
    ///
    /// 사용 방법:
    /// 1. 이 스크립트를 GameObject에 부착
    /// 2. SpineEventInjector 컴포넌트도 자동으로 추가됨
    /// 3. 런타임에 자동으로 이벤트가 시간 기반으로 트리거됨
    /// 4. Spine 애니메이션 툴에서 설정한 이벤트도 자동으로 처리됨
    /// </summary>
    [InjectSpineEvent("chest_open", "OnChestOpenStart", 0.1f)]
    [InjectSpineEvent("chest_open", "OnChestCrack", 0.4f)]
    [InjectSpineEvent("chest_open", "OnChestSpawnReward", 0.6f, IntParameter = 100)]
    [InjectSpineEvent("chest_open", "OnChestFullyOpen", 0.9f)]
    [InjectSpineEvent("chest_shake", "OnChestShake", 0.5f)]
    public class ChestWithAutoInjection : InteractableObjectBase
    {
        [Header("Chest Settings")]
        [SerializeField] private bool isOpened = false;

        [Header("Reward Settings")]
        [SerializeField] private GameObject rewardPrefab;
        [SerializeField] private Transform rewardSpawnPoint;

        [Header("Audio")]
        [SerializeField] private AudioClip openSound;
        [SerializeField] private AudioClip crackSound;
        [SerializeField] private AudioClip rewardSound;
        [SerializeField] private AudioSource audioSource;

        [Header("Visual Effects")]
        [SerializeField] private ParticleSystem openParticle;
        [SerializeField] private ParticleSystem glowParticle;

        private SpineEventInjector spineInjector;
        private bool rewardSpawned = false;

        protected override void Start()
        {
#if SPINE_UNITY
            // Spine 시스템 설정
            var spineSystem = gameObject.GetComponent<SpineAnimationSystem>();
            if (spineSystem == null)
            {
                spineSystem = gameObject.AddComponent<SpineAnimationSystem>();
            }

            SetAnimationSystem(spineSystem);

            // SpineEventInjector 추가 및 초기화
            spineInjector = gameObject.GetComponent<SpineEventInjector>();
            if (spineInjector == null)
            {
                spineInjector = gameObject.AddComponent<SpineEventInjector>();
            }
#endif

            // AudioSource 설정
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }

            base.Start();

            Debug.Log("[ChestWithAutoInjection] Initialized - Spine events will be triggered automatically");
        }

        protected override void OnInteractStartCustom()
        {
            if (isOpened)
            {
                // 이미 열린 상자는 흔들기
                PlayShakeAnimation();
                Debug.Log("[ChestWithAutoInjection] Chest already opened - shaking");
                return;
            }

            isOpened = true;
            rewardSpawned = false;

#if SPINE_UNITY
            var spineSystem = animationSystem as SpineAnimationSystem;
            if (spineSystem != null)
            {
                spineSystem.PlayAnimationBySymbolId("chest_open", false);
            }
#endif

            Debug.Log("[ChestWithAutoInjection] Opening chest...");
        }

        private void PlayShakeAnimation()
        {
#if SPINE_UNITY
            var spineSystem = animationSystem as SpineAnimationSystem;
            if (spineSystem != null)
            {
                spineSystem.PlayAnimationBySymbolId("chest_shake", false);
            }
#endif
        }

        protected override void OnInteractEndCustom()
        {
            // 상호작용 종료 로직
        }

        #region Animation Event Callbacks
        // ⭐ 이 메서드들은 SpineEventInjector에 의해 자동으로 호출됩니다!

        /// <summary>
        /// chest_open 애니메이션의 0.1 지점에서 호출됨
        /// </summary>
        private void OnChestOpenStart()
        {
            Debug.Log("[ChestWithAutoInjection] ✨ Chest opening started");

            if (openParticle != null)
            {
                openParticle.Play();
            }

            if (glowParticle != null)
            {
                glowParticle.Play();
            }

            if (openSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(openSound);
            }
        }

        /// <summary>
        /// chest_open 애니메이션의 0.4 지점에서 호출됨
        /// </summary>
        private void OnChestCrack()
        {
            Debug.Log("[ChestWithAutoInjection] 🔊 Chest cracking");

            if (crackSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(crackSound);
            }
        }

        /// <summary>
        /// chest_open 애니메이션의 0.6 지점에서 호출됨
        /// IntParameter로 보상 점수를 받음
        /// </summary>
        private void OnChestSpawnReward(AnimationEventData data)
        {
            Debug.Log($"[ChestWithAutoInjection] 🎁 Spawning reward (Score: {data.intParameter})");

            if (!rewardSpawned && rewardPrefab != null && rewardSpawnPoint != null)
            {
                GameObject reward = Instantiate(rewardPrefab, rewardSpawnPoint.position, Quaternion.identity);

                // 보상 점수 적용 (예시)
                var rewardComponent = reward.GetComponent<Reward>();
                if (rewardComponent != null)
                {
                    rewardComponent.SetScore(data.intParameter);
                }

                rewardSpawned = true;

                if (rewardSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(rewardSound);
                }
            }
        }

        /// <summary>
        /// chest_open 애니메이션의 0.9 지점에서 호출됨
        /// </summary>
        private void OnChestFullyOpen()
        {
            Debug.Log("[ChestWithAutoInjection] ✅ Chest fully opened");
        }

        /// <summary>
        /// chest_shake 애니메이션의 0.5 지점에서 호출됨
        /// </summary>
        private void OnChestShake()
        {
            Debug.Log("[ChestWithAutoInjection] 📦 Chest shaking");
        }

        #endregion

        // 보상 컴포넌트 예시
        private class Reward : MonoBehaviour
        {
            private int score;

            public void SetScore(int value)
            {
                score = value;
                Debug.Log($"Reward score set to {score}");
            }
        }
    }
}
