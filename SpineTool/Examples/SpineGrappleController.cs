using UnityEngine;
using System.Collections;
using InteractAnimation.Core;
using InteractAnimation.AnimationSystems.Spine;

namespace InteractAnimation.Examples
{
    /// <summary>
    /// Spine 전용 그래플 시스템 예제
    /// SpineSymbolData를 사용한 동기화 애니메이션 데모
    ///
    /// 사용법:
    /// 1. 공격자 GameObject에 이 스크립트 추가
    /// 2. SpineAnimationSystem 컴포넌트 필수
    /// 3. Symbol Collection에 그래플 애니메이션 심볼 등록
    /// 4. Victim에 CombatCharacter (with Spine) 할당
    /// </summary>
    public class SpineGrappleController : InteractableObjectBase
    {
        [Header("Spine Grapple Settings")]
        [SerializeField] private string grapplerID = "spine_grappler_01";

        [Header("Target")]
        [SerializeField] private CombatCharacter victim;
        [SerializeField] private Transform grabPoint;

#if SPINE_UNITY
        [Header("Spine Settings")]
        [SerializeField] private SpineSymbolCollection symbolCollection;

        [Header("Grapple Symbol IDs")]
        [SerializeField] private string bodySlamAttackerSymbol = "grapple_bodyslam_attacker";
        [SerializeField] private string bodySlamVictimSymbol = "grapple_bodyslam_victim";
        [SerializeField] private string suplexAttackerSymbol = "grapple_suplex_attacker";
        [SerializeField] private string suplexVictimSymbol = "grapple_suplex_victim";
        [SerializeField] private string throwAttackerSymbol = "grapple_throw_attacker";
        [SerializeField] private string throwVictimSymbol = "grapple_throw_victim";
#endif

        [Header("Damage Settings")]
        [SerializeField] private int bodySlamDamage = 35;
        [SerializeField] private int suplexDamage = 45;
        [SerializeField] private int throwDamage = 25;

        [Header("Physics")]
        [SerializeField] private float throwForce = 12f;

        private bool isGrappling = false;
        private Coroutine syncCoroutine;
        private SpineAnimationSystem spineSystem;

        protected override void Start()
        {
            // 방향 제어 활성화
            enableDirectionControl = true;

#if SPINE_UNITY
            // Spine 애니메이션 시스템 설정
            spineSystem = gameObject.GetComponent<SpineAnimationSystem>();
            if (spineSystem == null)
            {
                spineSystem = gameObject.AddComponent<SpineAnimationSystem>();
            }
            SetAnimationSystem(spineSystem);

            // Symbol Collection 설정
            if (symbolCollection != null)
            {
                spineSystem.SetSymbolCollection(symbolCollection);
            }
#endif

            // Grab Point 자동 생성
            if (grabPoint == null)
            {
                GameObject grabPointObj = new GameObject("GrabPoint_Spine");
                grabPointObj.transform.SetParent(transform);
                grabPointObj.transform.localPosition = new Vector3(0.5f, 1.0f, 0);
                grabPoint = grabPointObj.transform;
            }

            // 매니저에 등록
            if (Managers.AnimationModuleManager.Instance != null)
            {
                Managers.AnimationModuleManager.Instance.RegisterInteractableObject(grapplerID, this);
            }

            // Spine 이벤트 등록
            // Spine 애니메이션에 설정한 Event 이름과 매칭
            RegisterEventListener("spine_grab_start", OnGrabStart);
            RegisterEventListener("spine_lift_up", OnLiftUp);
            RegisterEventListener("spine_slam_impact", OnSlamImpact);
            RegisterEventListener("spine_throw_release", OnThrowRelease);
            RegisterEventListener("spine_grapple_end", OnGrappleComplete);

            base.Start();
        }

        #region Spine Grapple Techniques

        /// <summary>
        /// 바디 슬램 (Spine Symbol 사용)
        /// </summary>
        public void PerformBodySlam()
        {
#if SPINE_UNITY
            if (!CanPerformGrapple()) return;

            StartSpineGrapple(
                bodySlamAttackerSymbol,
                bodySlamVictimSymbol,
                bodySlamDamage,
                "knockdown"
            );

            Debug.Log("[SpineGrappleController] Performing Spine Body Slam!");
#else
            Debug.LogWarning("[SpineGrappleController] Spine runtime not available!");
#endif
        }

        /// <summary>
        /// 백 드롭 (Spine Symbol 사용)
        /// </summary>
        public void PerformSuplex()
        {
#if SPINE_UNITY
            if (!CanPerformGrapple()) return;

            StartSpineGrapple(
                suplexAttackerSymbol,
                suplexVictimSymbol,
                suplexDamage,
                "knockdown"
            );

            Debug.Log("[SpineGrappleController] Performing Spine Suplex!");
#else
            Debug.LogWarning("[SpineGrappleController] Spine runtime not available!");
#endif
        }

        /// <summary>
        /// 던지기 (Spine Symbol 사용)
        /// </summary>
        public void PerformThrow()
        {
#if SPINE_UNITY
            if (!CanPerformGrapple()) return;

            StartSpineGrapple(
                throwAttackerSymbol,
                throwVictimSymbol,
                throwDamage,
                "heavy"
            );

            Debug.Log("[SpineGrappleController] Performing Spine Throw!");
#else
            Debug.LogWarning("[SpineGrappleController] Spine runtime not available!");
#endif
        }

        #endregion

        #region Spine Grapple System

        /// <summary>
        /// Spine 그래플 시작 (Symbol ID 사용)
        /// </summary>
        private void StartSpineGrapple(string attackerSymbol, string victimSymbol, int damage, string damageType)
        {
#if SPINE_UNITY
            if (victim == null)
            {
                Debug.LogWarning("[SpineGrappleController] No victim assigned!");
                return;
            }

            if (spineSystem == null)
            {
                Debug.LogError("[SpineGrappleController] SpineAnimationSystem not found!");
                return;
            }

            isGrappling = true;

            // 1. 피해자를 공격자 방향으로 향하게
            FacePosition(victim.transform.position);

            // 2. 피해자는 공격자 반대 방향
            victim.SetDirection(-GetCurrentDirection());

            // 3. 피해자를 Grab Point의 자식으로 (Master-Slave)
            victim.transform.SetParent(grabPoint);
            victim.transform.localPosition = Vector3.zero;
            victim.SetGrappled(true);

            // 4. Spine Symbol로 애니메이션 재생
            // 공격자 애니메이션
            spineSystem.PlayAnimationBySymbolId(attackerSymbol, false);

            // 피해자 애니메이션 (Spine 시스템이 있다면)
            SpineAnimationSystem victimSpineSystem = victim.GetComponent<SpineAnimationSystem>();
            if (victimSpineSystem != null)
            {
                victimSpineSystem.PlayAnimationBySymbolId(victimSymbol, false);
            }
            else
            {
                // Spine이 아니면 일반 애니메이션 이름으로
                victim.PlayGrappleAnimation(victimSymbol);
            }

            // 5. 애니메이션 동기화 (안전장치)
            float duration = GetSpineAnimationDuration(attackerSymbol);
            if (syncCoroutine != null)
            {
                StopCoroutine(syncCoroutine);
            }
            syncCoroutine = StartCoroutine(SyncGrappleAnimation(duration, damage, damageType));

            Debug.Log($"[SpineGrappleController] Spine Grapple started - Attacker: {attackerSymbol}, Victim: {victimSymbol}");
#endif
        }

        /// <summary>
        /// Spine 애니메이션 길이 가져오기
        /// </summary>
        private float GetSpineAnimationDuration(string symbolId)
        {
#if SPINE_UNITY
            if (spineSystem != null && symbolCollection != null)
            {
                SpineSymbolData symbol = symbolCollection.GetSymbolById(symbolId);
                if (symbol != null && symbol.IsValid())
                {
                    return spineSystem.GetAnimationDuration(symbol.animationName);
                }
            }
#endif
            return 3f; // 기본값
        }

        /// <summary>
        /// 그래플 애니메이션 동기화 코루틴
        /// </summary>
        private IEnumerator SyncGrappleAnimation(float duration, int damage, string damageType)
        {
            yield return new WaitForSeconds(duration);

            // 타임아웃 처리
            if (isGrappling)
            {
                Debug.LogWarning("[SpineGrappleController] Grapple animation timeout, forcing completion");
                CompleteGrapple(damage, damageType);
            }
        }

        /// <summary>
        /// 그래플 완료
        /// </summary>
        private void CompleteGrapple(int damage, string damageType)
        {
            if (victim == null || !isGrappling) return;

            // 1. 피해자 분리
            victim.transform.SetParent(null);

            // 2. 바닥 위치
            Vector3 dropPosition = transform.position + new Vector3(GetCurrentDirection() * 1.5f, 0, 0);
            victim.transform.position = dropPosition;

            // 3. 데미지 적용
            victim.OnReceiveAttack(transform.position, damageType, damage);

            // 4. 그래플 해제
            victim.SetGrappled(false);
            isGrappling = false;

            Debug.Log($"[SpineGrappleController] Spine Grapple completed - Damage: {damage}");
        }

        /// <summary>
        /// 그래플 가능 여부
        /// </summary>
        private bool CanPerformGrapple()
        {
            if (isGrappling)
            {
                Debug.LogWarning("[SpineGrappleController] Already grappling!");
                return false;
            }

            if (victim == null)
            {
                Debug.LogWarning("[SpineGrappleController] No victim assigned!");
                return false;
            }

#if SPINE_UNITY
            if (spineSystem == null)
            {
                Debug.LogError("[SpineGrappleController] SpineAnimationSystem not found!");
                return false;
            }
#endif

            return CanInteract();
        }

        #endregion

        #region Spine Animation Event Handlers

        /// <summary>
        /// Spine 이벤트: 붙잡기 시작
        /// Spine 에디터에서 Event 이름 "spine_grab_start"
        /// </summary>
        private void OnGrabStart(AnimationEventData eventData)
        {
            Debug.Log("[SpineGrappleController] Spine Event: Grab Start");

            // Spine Event의 파라미터 활용 가능
            if (!string.IsNullOrEmpty(eventData.stringParameter))
            {
                Debug.Log($"  → Event Param: {eventData.stringParameter}");
            }
        }

        /// <summary>
        /// Spine 이벤트: 들어올리기
        /// Spine Event 이름 "spine_lift_up"
        /// </summary>
        private void OnLiftUp(AnimationEventData eventData)
        {
            Debug.Log("[SpineGrappleController] Spine Event: Lift Up");

            // Spine Event의 Int/Float 파라미터 사용
            int liftHeight = eventData.intParameter;
            if (liftHeight > 0)
            {
                Debug.Log($"  → Lift Height: {liftHeight}");
            }
        }

        /// <summary>
        /// Spine 이벤트: 충격 순간
        /// Spine Event 이름 "spine_slam_impact"
        /// </summary>
        private void OnSlamImpact(AnimationEventData eventData)
        {
            Debug.Log("[SpineGrappleController] Spine Event: Slam Impact!");

            // 임팩트 파워 (Spine Event의 Float 파라미터)
            float impactPower = eventData.floatParameter;
            if (impactPower > 0)
            {
                Debug.Log($"  → Impact Power: {impactPower}");
                // 카메라 쉐이크 강도를 파라미터로 조절
                // CameraShake.Instance?.Shake(impactPower * 0.1f, 0.5f);
            }

            // 파티클 이펙트
            // PlayImpactEffect(victim.transform.position);
        }

        /// <summary>
        /// Spine 이벤트: 던지기 릴리즈
        /// Spine Event 이름 "spine_throw_release"
        /// </summary>
        private void OnThrowRelease(AnimationEventData eventData)
        {
            Debug.Log("[SpineGrappleController] Spine Event: Throw Release");

            if (victim == null) return;

            // 피해자 분리
            victim.transform.SetParent(null);

            // 던지기 힘 (Spine Event 파라미터로 조절 가능)
            float forceMultiplier = eventData.floatParameter > 0 ? eventData.floatParameter : 1f;

            Rigidbody2D victimRb = victim.GetComponent<Rigidbody2D>();
            if (victimRb != null)
            {
                Vector2 throwDirection = new Vector2(
                    GetCurrentDirection() * throwForce * forceMultiplier,
                    throwForce * 0.5f * forceMultiplier
                );
                victimRb.AddForce(throwDirection, ForceMode2D.Impulse);

                Debug.Log($"  → Throw Force Multiplier: {forceMultiplier}x");
            }
        }

        /// <summary>
        /// Spine 이벤트: 그래플 완료
        /// Spine Event 이름 "spine_grapple_end"
        /// </summary>
        private void OnGrappleComplete(AnimationEventData eventData)
        {
            Debug.Log("[SpineGrappleController] Spine Event: Grapple Complete");

            // Spine Event에서 데미지 오버라이드 가능
            int damage = eventData.intParameter > 0 ? eventData.intParameter : bodySlamDamage;
            string damageType = !string.IsNullOrEmpty(eventData.stringParameter)
                ? eventData.stringParameter
                : "knockdown";

            CompleteGrapple(damage, damageType);
        }

        #endregion

        /// <summary>
        /// 피해자 설정
        /// </summary>
        public void SetVictim(CombatCharacter newVictim)
        {
            victim = newVictim;
            Debug.Log($"[SpineGrappleController] Victim set to {(victim != null ? victim.name : "null")}");
        }

        /// <summary>
        /// Symbol Collection 설정 (런타임)
        /// </summary>
        public void SetSymbolCollection(SpineSymbolCollection collection)
        {
#if SPINE_UNITY
            symbolCollection = collection;
            if (spineSystem != null)
            {
                spineSystem.SetSymbolCollection(collection);
            }
            Debug.Log("[SpineGrappleController] Symbol Collection updated");
#endif
        }

        protected override void OnInteractStartCustom()
        {
            // SpineGrappleController는 그래플 기술 메서드를 직접 호출하므로 여기서는 처리 없음
        }

        protected override void OnInteractEndCustom()
        {
            // 그래플 종료는 CompleteGrapple()에서 처리됨
        }

        // 테스트용 입력
        private void Update()
        {
            if (!debugEvents) return;

            // Numpad 7: 바디슬램
            if (Input.GetKeyDown(KeyCode.Keypad7))
            {
                PerformBodySlam();
            }
            // Numpad 8: 백드롭
            else if (Input.GetKeyDown(KeyCode.Keypad8))
            {
                PerformSuplex();
            }
            // Numpad 9: 던지기
            else if (Input.GetKeyDown(KeyCode.Keypad9))
            {
                PerformThrow();
            }
        }

        private void OnDestroy()
        {
            // 이벤트 리스너 해제
            UnregisterEventListener("spine_grab_start", OnGrabStart);
            UnregisterEventListener("spine_lift_up", OnLiftUp);
            UnregisterEventListener("spine_slam_impact", OnSlamImpact);
            UnregisterEventListener("spine_throw_release", OnThrowRelease);
            UnregisterEventListener("spine_grapple_end", OnGrappleComplete);

            // 매니저 등록 해제
            if (Managers.AnimationModuleManager.Instance != null)
            {
                Managers.AnimationModuleManager.Instance.UnregisterInteractableObject(grapplerID);
            }

            // 코루틴 정리
            if (syncCoroutine != null)
            {
                StopCoroutine(syncCoroutine);
            }
        }

        // Gizmo
        private void OnDrawGizmosSelected()
        {
            if (grabPoint != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(grabPoint.position, 0.3f);
                Gizmos.DrawLine(transform.position, grabPoint.position);
            }

            if (victim != null)
            {
                Gizmos.color = isGrappling ? Color.red : Color.cyan;
                Gizmos.DrawLine(transform.position, victim.transform.position);
            }
        }
    }
}
