using UnityEngine;
using SpineTool;

namespace SpineTool.Examples
{
    /// <summary>
    /// SpineAnimModule 사용 예제
    ///
    /// 구조: 샘플코드(설정) → 모듈 → 기능 작동
    ///
    /// 1. 샘플 코드에서 설정
    /// 2. SpineAnimModule이 처리
    /// 3. 결과 출력
    /// </summary>
    public class SpineAnimModuleExample : MonoBehaviour
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 1단계: 샘플 코드 (설정)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        [Header("모듈 참조")]
        [SerializeField] private SpineAnimModule animModule; // ← 모듈

        [Header("애니메이션 설정")]
        [SerializeField] private string idleAnimation = "idle";
        [SerializeField] private string walkAnimation = "walk";
        [SerializeField] private string attackAnimation = "attack";

        void Awake()
        {
            // 모듈 가져오기
            if (animModule == null)
            {
                animModule = GetComponent<SpineAnimModule>();
            }
        }

        void Start()
        {
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // 샘플 코드 1: 이벤트 리스너 등록
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

            animModule.AddEventListener("footstep", OnFootstep);
            animModule.AddEventListener("hit_impact", OnHitImpact);
            animModule.AddEventListener("weapon_swoosh", OnWeaponSwoosh);

            // ↓ SpineAnimModule이 처리
            // ↓ 이벤트 발생 시 콜백 호출


            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // 샘플 코드 2: 기본 애니메이션 재생
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

            animModule.PlayAnimation(idleAnimation, true);

            // ↓ SpineAnimModule이 처리
            // ↓ Idle 애니메이션 재생됨
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 3단계: 기능 작동 (콜백)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void OnFootstep(SpineEventData data)
        {
            // ✅ 결과: 발소리 이벤트 수신
            Debug.Log($"👟 발소리! (애니메이션: {data.AnimationName})");

            // 여기서 실제 기능 구현
            // PlayFootstepSound();
        }

        private void OnHitImpact(SpineEventData data)
        {
            // ✅ 결과: 타격 이벤트 수신
            int damage = data.IntParameter;
            Debug.Log($"💥 타격! 데미지: {damage}");

            // 여기서 실제 기능 구현
            // ApplyDamage(damage);
            // SpawnHitEffect();
        }

        private void OnWeaponSwoosh(SpineEventData data)
        {
            // ✅ 결과: 무기 휘두르기 이벤트 수신
            Debug.Log("⚔️ 무기 휘두르기!");

            // 여기서 실제 기능 구현
            // PlayWeaponSound();
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 추가 예제: 애니메이션 제어
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        void Update()
        {
            // Space 키로 공격 테스트
            if (Input.GetKeyDown(KeyCode.Space))
            {
                PerformAttack();
            }
        }

        void PerformAttack()
        {
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // 샘플 코드: 공격 애니메이션 재생
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

            animModule.PlayAnimation(attackAnimation, false); // 한 번만
            animModule.AddAnimation(idleAnimation, true);     // 공격 후 Idle

            // ↓ SpineAnimModule이 처리
            // ↓ Attack → Idle 순서로 재생됨
            // ↓ "hit_impact" 이벤트 발생 시 OnHitImpact() 호출

            Debug.Log("공격 실행!");
        }


        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // GUI (테스트용)
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 500, 400));

            GUILayout.Box("SpineAnimModule 사용 예제");
            GUILayout.Label("구조: 샘플코드(설정) → 모듈 → 기능작동");

            GUILayout.Space(10);
            GUILayout.Label("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

            // 예제 1: 애니메이션 재생
            GUILayout.Label("▼ 예제 1: 애니메이션 재생");
            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("PlayAnimation(\"idle\", true)", GUILayout.Width(200)))
            {
                animModule.PlayAnimation(idleAnimation, true);
                Debug.Log("→ SpineAnimModule이 처리 → Idle 재생됨");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("PlayAnimation(\"walk\", true)", GUILayout.Width(200)))
            {
                animModule.PlayAnimation(walkAnimation, true);
                Debug.Log("→ SpineAnimModule이 처리 → Walk 재생됨");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 예제 2: 순차 재생
            GUILayout.Label("▼ 예제 2: 순차 재생 (Attack → Idle)");
            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("Attack + Idle 순차 재생", GUILayout.Width(200)))
            {
                PerformAttack();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 예제 3: 속도 제어
            GUILayout.Label("▼ 예제 3: 속도 제어");
            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("SetSpeed(0.5f) - 느리게", GUILayout.Width(200)))
            {
                animModule.SetSpeed(0.5f);
                Debug.Log("→ SpineAnimModule이 처리 → 0.5배속으로 재생");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("샘플 코드:");
            if (GUILayout.Button("SetSpeed(2f) - 빠르게", GUILayout.Width(200)))
            {
                animModule.SetSpeed(2.0f);
                Debug.Log("→ SpineAnimModule이 처리 → 2배속으로 재생");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // 현재 상태
            GUILayout.Label("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            GUILayout.Label($"현재 애니메이션: {animModule.CurrentAnimationName}");
            GUILayout.Label($"재생 중: {animModule.IsPlaying}");

            GUILayout.Space(10);
            GUILayout.Label("💡 Spine 툴에서 이벤트를 추가하면");
            GUILayout.Label("   자동으로 콜백 함수가 호출됩니다!");

            GUILayout.EndArea();
        }
    }
}
