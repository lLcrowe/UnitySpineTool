using UnityEngine;
using SpineTool;

#if SPINE_UNITY
using Spine.Unity;
#endif

namespace SpineTool.Examples
{
    /// <summary>
    /// SpineEventInjector 사용 예제 1: 기본 사용법
    ///
    /// 사용 방법:
    /// 1. SkeletonAnimation 컴포넌트가 있는 GameObject에 추가
    /// 2. SpineEventInjector 컴포넌트 추가
    /// 3. 플레이 모드에서 자동으로 이벤트 주입
    /// </summary>
    [InjectSpineEvent("attack", "OnAttackStart", 0.0f)]
    [InjectSpineEvent("attack", "OnHitImpact", 0.5f, IntParameter = 50)]
    [InjectSpineEvent("attack", "OnAttackEnd", 1.0f)]
    public class SpineCharacterExample : MonoBehaviour
    {
        [Header("References")]
        public GameObject hitEffectPrefab;
        public Transform hitPoint;

#if SPINE_UNITY
        private SkeletonAnimation skeletonAnimation;

        private void Awake()
        {
            skeletonAnimation = GetComponent<SkeletonAnimation>();
        }

        // ===== 주입된 이벤트 핸들러 =====

        /// <summary>
        /// 공격 시작 (0.0초)
        /// </summary>
        private void OnAttackStart(SpineEventData data)
        {
            Debug.Log($"[{gameObject.name}] Attack started!");
        }

        /// <summary>
        /// 타격 순간 (0.5초, 50% 지점)
        /// IntParameter로 데미지 값 전달
        /// </summary>
        private void OnHitImpact(SpineEventData data)
        {
            int damage = data.IntParameter; // 50
            Debug.Log($"[{gameObject.name}] Hit impact! Damage: {damage}");

            // 타격 이펙트 생성
            if (hitEffectPrefab != null && hitPoint != null)
            {
                Instantiate(hitEffectPrefab, hitPoint.position, Quaternion.identity);
            }

            // 화면 쉐이크 등 추가 효과
            // CameraShake.Instance?.Shake(0.3f, 0.2f);
        }

        /// <summary>
        /// 공격 종료 (1.0초, 100% 지점)
        /// </summary>
        private void OnAttackEnd(SpineEventData data)
        {
            Debug.Log($"[{gameObject.name}] Attack finished!");
        }

        // ===== Spine 툴에서 설정한 이벤트 수신 =====

        /// <summary>
        /// SpineEventInjector가 processSpineToolEvents=true일 때
        /// Spine 에디터에서 설정한 이벤트를 받을 수 있습니다.
        /// </summary>
        private void OnSpineEvent(SpineEventData data)
        {
            Debug.Log($"[{gameObject.name}] Spine Tool Event: {data.EventName}");

            // 이벤트 이름으로 분기
            switch (data.EventName)
            {
                case "footstep":
                    PlayFootstepSound();
                    break;

                case "weapon_swoosh":
                    PlayWeaponSound();
                    break;
            }
        }

        // ===== 테스트 코드 =====

        private void Update()
        {
            // Space 키로 공격 애니메이션 테스트
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PlayAttack();
            }
        }

        private void PlayAttack()
        {
            if (skeletonAnimation != null)
            {
                skeletonAnimation.AnimationState.SetAnimation(0, "attack", false);
                Debug.Log("Playing attack animation...");
            }
        }

        private void PlayFootstepSound()
        {
            Debug.Log("*Footstep sound*");
            // AudioSource.PlayOneShot(footstepClip);
        }

        private void PlayWeaponSound()
        {
            Debug.Log("*Weapon swoosh sound*");
            // AudioSource.PlayOneShot(weaponSwooshClip);
        }
#endif
    }
}
