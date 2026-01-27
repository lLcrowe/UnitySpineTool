#if SPINE_UNITY
using UnityEngine;
using SpineTool;

/// <summary>
/// Enum을 사용한 Spine 애니메이션 제어 예제
///
/// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
/// 사용 흐름:
/// 1단계: SpineAnimationEnumGenerator로 Enum 생성 (Tools/SpineTool/Animation Enum Generator)
/// 2단계: 생성된 Enum을 사용하여 애니메이션 재생 (타입 안전, 자동완성 지원)
/// 3단계: 컴파일 타임에 오타 체크 가능
/// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
/// </summary>
public class SpineEnumAnimationExample : MonoBehaviour
{
    // ━━━━━ 예제용 Enum 정의 ━━━━━
    // 실제로는 SpineAnimationEnumGenerator로 자동 생성합니다!
    public enum PlayerAnimations
    {
        Idle,
        Run,
        Jump,
        Attack,
        Shoot,
        Death
    }

    public enum EnemyAnimations
    {
        Idle,
        Walk,
        Attack,
        Hit,
        Death
    }

    // ━━━━━ 컴포넌트 ━━━━━
    private SpineAnimModule animModule;

    void Awake()
    {
        animModule = GetComponent<SpineAnimModule>();
    }

    void Start()
    {
        // ━━━━━ 1단계: 샘플 코드 (설정) ━━━━━
        // Enum 사용 - 문자열 대신 타입 안전한 방식

        // ✅ Enum 사용 (권장)
        animModule.PlayAnimation(PlayerAnimations.Idle, loop: true);

        // ❌ 문자열 사용 (오타 가능)
        // animModule.PlayAnimation("idel", loop: true); // 컴파일러가 체크 못함!

        // ↓ SpineAnimModule이 처리
        // ↓ Enum을 문자열로 변환하여 애니메이션 재생

        // ━━━━━ 3단계: 기능 작동 ━━━━━
        // ✅ 결과: 타입 안전하게 애니메이션 재생
    }

    void Update()
    {
        // ━━━━━ 메탈슬러그 스타일 캐릭터 제어 ━━━━━

        float input = Input.GetAxis("Horizontal");

        // 이동
        if (Mathf.Abs(input) > 0.1f)
        {
            animModule.PlayAnimation(PlayerAnimations.Run, loop: true);
            transform.localScale = new Vector3(input > 0 ? 1 : -1, 1, 1);
        }
        else
        {
            animModule.PlayAnimation(PlayerAnimations.Idle, loop: true);
        }

        // 점프
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animModule.PlayAnimation(PlayerAnimations.Jump, loop: false);
            animModule.AddAnimation(PlayerAnimations.Idle, loop: true); // 점프 끝나면 Idle
        }

        // 공격
        if (Input.GetKeyDown(KeyCode.Z))
        {
            animModule.PlayAnimation(PlayerAnimations.Attack, loop: false);
            animModule.AddAnimation(PlayerAnimations.Idle, loop: true);
        }

        // 슈팅
        if (Input.GetKeyDown(KeyCode.X))
        {
            animModule.PlayAnimation(PlayerAnimations.Shoot, loop: false);
            animModule.AddAnimation(PlayerAnimations.Idle, loop: true);
        }
    }

    // ━━━━━ Enum 사용의 장점 ━━━━━

    /// <summary>
    /// 1. 자동완성 지원
    /// animModule.PlayAnimation(PlayerAnimations. ← 여기서 Ctrl+Space 누르면 목록 표시
    /// </summary>
    public void AdvantageAutoComplete()
    {
        // IDE에서 자동완성으로 애니메이션 선택 가능
        animModule.PlayAnimation(PlayerAnimations.Idle, loop: true);
    }

    /// <summary>
    /// 2. 컴파일 타임 체크
    /// 오타나 잘못된 이름을 사용하면 컴파일 에러 발생
    /// </summary>
    public void AdvantageCompileTimeCheck()
    {
        // ✅ 컴파일 성공
        animModule.PlayAnimation(PlayerAnimations.Run, loop: true);

        // ❌ 컴파일 에러 (PlayerAnimations에 없는 값)
        // animModule.PlayAnimation(PlayerAnimations.Fly, loop: true);
    }

    /// <summary>
    /// 3. 리팩토링 용이
    /// Enum 이름 변경 시 IDE의 Rename 기능으로 일괄 변경 가능
    /// </summary>
    public void AdvantageRefactoring()
    {
        // Enum 값을 변경하면 모든 사용처가 자동으로 변경됨
        animModule.PlayAnimation(PlayerAnimations.Attack, loop: false);
    }

    /// <summary>
    /// 4. 타입 안전성
    /// 잘못된 타입의 Enum을 사용하면 컴파일 에러
    /// </summary>
    public void AdvantageTypeSafety()
    {
        // ✅ PlayerAnimations 사용
        animModule.PlayAnimation(PlayerAnimations.Idle, loop: true);

        // ❌ 다른 Enum 사용 불가 (컴파일 에러)
        // animModule.PlayAnimation(EnemyAnimations.Walk, loop: true); // 타입 불일치!
    }
}

/// <summary>
/// 메탈슬러그 스타일 적 AI 예제
/// </summary>
public class MetalSlugEnemyAI : MonoBehaviour
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Death
    }

    public enum EnemyAnimations
    {
        Idle,
        Walk,
        Run,
        Attack,
        Hit,
        Death
    }

    private SpineAnimModule animModule;
    private EnemyState currentState;

    void Awake()
    {
        animModule = GetComponent<SpineAnimModule>();
    }

    void Start()
    {
        ChangeState(EnemyState.Patrol);
    }

    void ChangeState(EnemyState newState)
    {
        currentState = newState;

        // 상태에 따른 애니메이션 재생 (Enum 매핑)
        switch (currentState)
        {
            case EnemyState.Idle:
                animModule.PlayAnimation(EnemyAnimations.Idle, loop: true);
                break;

            case EnemyState.Patrol:
                animModule.PlayAnimation(EnemyAnimations.Walk, loop: true);
                break;

            case EnemyState.Chase:
                animModule.PlayAnimation(EnemyAnimations.Run, loop: true);
                break;

            case EnemyState.Attack:
                animModule.PlayAnimation(EnemyAnimations.Attack, loop: false);
                animModule.AddAnimation(EnemyAnimations.Idle, loop: true);
                break;

            case EnemyState.Death:
                animModule.PlayAnimation(EnemyAnimations.Death, loop: false);
                break;
        }
    }

    public void TakeDamage(int damage)
    {
        // 피격 애니메이션 (즉시 재생)
        animModule.SetAnimation(EnemyAnimations.Hit, loop: false);
        animModule.AddAnimation(EnemyAnimations.Idle, loop: true);
    }
}
#endif
