# SpineTool

Unity에서 Spine2D 애니메이션 작업을 위한 강력한 에디터 도구 모음입니다.

## 호환 버전

- Unity 6000.3 이상
- Spine C# Runtime 4.3.39
- Spine Unity Runtime 4.3.98

`com.esotericsoftware.spine.spine-unity` 패키지를 설치하면 `SPINE_UNITY` 심볼은 asmdef의 `versionDefines`에서 자동으로 활성화됩니다.

## ✨ 주요 기능

### 🎬 1. Animation Preview (에디터 모드 애니메이션 재생)

**플레이 모드 없이** 에디터에서 바로 Spine 애니메이션을 재생하고 확인할 수 있습니다!

#### 특징:
- ✅ 에디터 모드에서 실시간 애니메이션 재생
- ✅ 여러 오브젝트 동시 선택 및 제어
- ✅ 재생/일시정지/정지 컨트롤
- ✅ Spine 공식 인스펙터와 함께 동작

#### 사용 방법:

**방법 1: Inspector 통합 (추천)**
1. `SkeletonAnimation` 컴포넌트가 있는 GameObject 선택
2. Inspector 하단에 "🎬 Animation Preview (Editor Mode)" 섹션 확인
3. 애니메이션 목록에서 ▶ 버튼 클릭
4. 씬 뷰에서 실시간 재생 확인!

**방법 2: 별도 윈도우**
1. 메뉴: `Tools → SpineTool → Animation Preview Window`
2. `SkeletonAnimation`이 있는 GameObject 선택
3. 창에서 애니메이션 제어

### 🔍 2. Skeleton Inspector (파라미터 뷰어) ⭐ 신규!

**SkeletonAnimation의 모든 정보를 한눈에!** IK, Bone, Slot, Animation 등 모든 파라미터 확인!

#### 특징:
- ✅ 실시간 파라미터 확인
- ✅ IK Constraints (이름, Active, Weight, Target)
- ✅ Transform/Path Constraints
- ✅ Bones, Slots 정보
- ✅ Skins, Animations, Events 목록
- ✅ 검색 필터 지원
- ✅ 원클릭 스킨/애니메이션 변경

#### 사용 방법:
1. 메뉴: `Tools → SpineTool → Skeleton Inspector`
2. SkeletonAnimation 선택
3. 모든 정보 확인!

**표시 정보:**
- 🎨 Skins (스킨 목록 + 변경 버튼)
- 🎬 Animations (애니메이션 목록 + 재생 버튼)
- 🦴 **IK Constraints** (Active, Weight, Target, Toggle 버튼) ← 핵심!
- ↔️ Transform Constraints
- 🛤️ Path Constraints
- ⚡ Events (이벤트 정의)
- 💀 Bones (위치, 회전, 스케일)
- 📌 Slots (Attachment, Color)

### 🔢 3. Animation Enum Generator (애니메이션 Enum 생성기) ⭐ 신규!

**문자열 대신 Enum으로 타입 안전하게 애니메이션 제어!** SkeletonDataAsset에서 자동으로 Enum 코드 생성!

#### 특징:
- ✅ 자동 Enum 코드 생성 (SkeletonDataAsset → Enum)
- ✅ **3가지 생성 모드** 지원 (Individual, Combined, Smart Combined)
- ✅ 여러 Skeleton 동시 선택 가능
- ✅ 타입 안전성 (컴파일 타임 체크)
- ✅ IDE 자동완성 지원
- ✅ 오타 방지 (컴파일 에러로 감지)
- ✅ 리팩토링 용이
- ✅ Namespace, 경로 커스터마이징

#### 3가지 생성 모드:

**1️⃣ Individual (각각 생성)** - 기본, 권장
```
Player.asset → PlayerAnimations.cs
Enemy.asset  → EnemyAnimations.cs
Boss.asset   → BossAnimations.cs
```
- 장점: 명확한 분리, 타입 안전
- 단점: 공통 애니메이션 중복

**2️⃣ Combined (통합 생성)**
```
3개 합쳐서 → AllCharacterAnimations.cs
  - Player_Idle, Player_Run
  - Enemy_Idle, Enemy_Walk
  - Boss_Idle, Boss_Ultimate
```
- 장점: 한 파일로 관리
- 단점: Enum 값 많아짐, 타입 안전성 낮음

**3️⃣ Smart Combined (똑똑한 통합)** ⭐
```
공통 감지 → CommonAnimations.cs (Idle, Attack, Death)
Player 전용 → PlayerAnimations.cs (Shoot, Dash)
Enemy 전용 → EnemyAnimations.cs (Patrol, Rage)
```
- 장점: 중복 없음, 재사용성 최고
- 단점: 파일이 여러 개

#### 사용 방법:
1. 메뉴: `Tools → SpineTool → Animation Enum Generator`
2. **SkeletonDataAsset 추가** (여러 개 가능)
3. **Generation Mode 선택** (Individual / Combined / Smart Combined)
4. Enum 이름, Namespace 설정
5. "Enum 코드 생성" 클릭
6. 생성된 Enum 사용!

**생성 예시:**
```csharp
// 자동 생성된 코드 (PlayerAnimations.cs)
namespace Game.Animations
{
    /// <summary>
    /// Hero 애니메이션 목록
    /// 자동 생성됨 - SpineAnimationEnumGenerator
    /// </summary>
    public enum PlayerAnimations
    {
        /// <summary>idle</summary>
        Idle,

        /// <summary>run</summary>
        Run,

        /// <summary>jump</summary>
        Jump,

        /// <summary>attack_01</summary>
        Attack_01,

        /// <summary>attack_02</summary>
        Attack_02
    }
}
```

**사용 예시:**
```csharp
using SpineTool;

public class Player : MonoBehaviour
{
    private SpineAnimModule animModule;

    void Start()
    {
        animModule = GetComponent<SpineAnimModule>();

        // ✅ Enum 사용 (타입 안전, 자동완성)
        animModule.PlayAnimation(PlayerAnimations.Idle, loop: true);

        // ❌ 문자열 사용 (오타 위험)
        // animModule.PlayAnimation("idel", loop: true); // 버그!
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // IDE에서 자동완성으로 선택 가능!
            animModule.PlayAnimation(PlayerAnimations.Jump, loop: false);
            animModule.AddAnimation(PlayerAnimations.Idle, loop: true);
        }
    }
}
```

**장점:**
- **자동완성**: `PlayerAnimations.` 입력 시 모든 애니메이션 목록 표시
- **컴파일 체크**: 잘못된 애니메이션 이름 사용 시 컴파일 에러
- **리팩토링**: Enum 값 변경 시 IDE의 Rename 기능으로 일괄 변경
- **타입 안전**: 다른 캐릭터의 애니메이션 Enum 사용 시 컴파일 에러

### 📝 4. Spine Event Editor (이벤트 편집기)

Spine JSON 파일에 이벤트를 **Unity 에디터에서 직접** 추가/수정/삭제할 수 있습니다.

#### 특징:
- ✅ Spine Editor 없이도 이벤트 관리
- ✅ 실시간 애니메이션 프리뷰
- ✅ 비주얼 타임라인 (이벤트 위치 시각화)
- ✅ 마우스 컨트롤 (우클릭 패닝, 휠 줌)
- ✅ JSON 파일 직접 수정
- ✅ 변경사항 추적 및 저장

#### 사용 방법:
1. 메뉴: `Tools → InteractAnimation → Spine Event Editor`
2. `SkeletonDataAsset` 선택
3. 애니메이션 선택
4. `Add New Event` 버튼으로 이벤트 추가
5. 이벤트 이름, 시간, 파라미터 설정
6. `Save to JSON` 클릭

### ⚡ 5. Spine Event Injector (런타임 이벤트 주입)

**코드만으로** Spine 애니메이션에 이벤트를 주입! Attribute 기반의 강력한 이벤트 시스템입니다.

#### 특징:
- ✅ Attribute 하나로 이벤트 자동 등록
- ✅ 정확한 타이밍 제어 (정규화된 시간 0.0 ~ 1.0)
- ✅ 파라미터 전달 지원 (Int, Float, String)
- ✅ Spine 툴 이벤트와 통합 가능
- ✅ 여러 애니메이션에 여러 이벤트 등록 가능

#### 사용 방법:

```csharp
using SpineTool;

// Attribute로 이벤트 등록
[InjectSpineEvent("attack", "OnHitImpact", 0.5f, IntParameter = 50)]
public class MyCharacter : MonoBehaviour
{
    // 1. SpineEventInjector 컴포넌트 추가 필요
    // 2. SkeletonAnimation 컴포넌트 필요

    // 이벤트 핸들러 (자동 호출됨)
    void OnHitImpact(SpineEventData data)
    {
        int damage = data.IntParameter; // 50
        Debug.Log($"Hit! Damage: {damage}");
    }
}
```

**여러 이벤트 등록:**
```csharp
[InjectSpineEvent("attack", "OnAttackStart", 0.0f)]
[InjectSpineEvent("attack", "OnHitImpact", 0.5f, IntParameter = 50)]
[InjectSpineEvent("attack", "OnAttackEnd", 1.0f)]
public class CombatCharacter : MonoBehaviour { ... }
```

### 🎮 6. Spine Anim Module (통합 애니메이션 모듈) ⭐ 신규!

**런타임에서 애니메이션을 쉽게 재생하고 이벤트를 등록**할 수 있는 통합 컨트롤러!

#### 특징:
- ✅ 간편한 애니메이션 재생 API (Play, Stop, Pause, Resume)
- ✅ 이벤트 리스너 등록 (문자열 이벤트 이름 기반)
- ✅ SpineSymbolData 지원
- ✅ 속도, 루프, 스킨 제어
- ✅ 블렌딩 시간 설정

#### 사용 방법 (설정 → 모듈 → 기능 구조):

```csharp
using SpineTool;

public class MyCharacter : MonoBehaviour
{
    // ━━━━━ 1단계: 샘플 코드 (설정) ━━━━━
    private SpineAnimModule animModule;

    void Start()
    {
        animModule = GetComponent<SpineAnimModule>();

        // 이벤트 리스너 등록
        animModule.AddEventListener("hit_impact", OnHit);
        // ↓ SpineAnimModule이 처리

        // 애니메이션 재생
        animModule.PlayAnimation("attack", false);
        // ↓ SpineAnimModule이 처리
        // ↓ Attack 애니메이션 재생됨
    }

    // ━━━━━ 3단계: 기능 작동 (콜백) ━━━━━
    void OnHit(SpineEventData data)
    {
        // ✅ 결과: hit_impact 이벤트 수신
        int damage = data.IntParameter;
        Debug.Log($"Hit! {damage} damage");

        // 실제 기능 구현
        ApplyDamage(damage);
    }
}
```

**고급 기능:**
```csharp
// 속도 조절 (슬로우 모션)
controller.SetSpeed(0.5f);

// 스킨 변경
controller.SetSkin("red_costume");

// 블렌딩 시간 설정
controller.SetMixDuration("walk", "run", 0.2f);

// 일시정지/재개
controller.PauseAnimation();
controller.ResumeAnimation();
```

### 🔗 7. Spine Anim Sync Module (애니메이션 동기화) ⭐ 신규!

**두 캐릭터의 애니메이션을 동기화**하는 모듈! 처형, 그래플, 상호작용에 필수!

#### 특징:
- ✅ Master-Slave 애니메이션 동기화
- ✅ 위치/방향 자동 매칭
- ✅ Transform 부모-자식 관계 설정
- ✅ 특정 본(Bone)에 부착 가능
- ✅ 동기화 완료 콜백

#### 사용 방법:
```csharp
using SpineTool;

public class ExecutionSystem : MonoBehaviour
{
    // ━━━━━ 샘플 코드 ━━━━━
    private SpineAnimSyncModule syncModule;

    void PerformExecution()
    {
        // 동기화 시작
        syncModule.StartSync(
            "execute_attack",  // Master 애니메이션
            "execute_victim",  // Slave 애니메이션
            false
        );
        // ↓ SpineAnimSyncModule이 처리
        // ↓ 두 캐릭터 애니메이션 동기화됨
    }

    // ✅ 결과: 처형 모션 완벽 싱크
}
```

**사용 예시:**
- 처형 모션 (공격자 + 피해자)
- 보물상자 열기 (캐릭터 + 상자)
- 그래플 기술 (캐릭터 + 적)

### 🦴 8. Spine IK Module (IK 제어) ⭐ 신규!

**IK (Inverse Kinematics) 제어**로 자연스러운 움직임 구현!

#### 특징:
- ✅ IK Constraint 온/오프
- ✅ IK 가중치 (Weight) 조절 (0.0 ~ 1.0)
- ✅ 부드러운 전환 지원
- ✅ 여러 IK 동시 제어
- ✅ 실시간 조작

#### 사용 방법:
```csharp
using SpineTool;

public class CharacterIK : MonoBehaviour
{
    // ━━━━━ 샘플 코드 ━━━━━
    private SpineIKControl ikModule;

    void GrabObject()
    {
        // IK 활성화
        ikModule.SetIKActive("hand_IK", true);

        // 가중치 부드럽게 변경
        ikModule.SetIKWeightSmooth("hand_IK", 1.0f, 0.3f);

        // ↓ SpineIKControl이 처리
        // ↓ 0.3초 동안 부드럽게 IK 활성화됨
    }

    // ✅ 결과: 손이 타겟을 향해 자연스럽게 뻗음
}
```

**사용 예시:**
- 손으로 오브젝트 잡기
- 발이 지면에 붙도록
- 시선 추적

### 🏷️ 9. Spine Symbol Data (메타데이터 관리)

ScriptableObject 기반 애니메이션 메타데이터 관리 시스템

#### 특징:
- ✅ 애니메이션 설정 중앙 관리
- ✅ 태그 기반 필터링
- ✅ 우선순위 시스템
- ✅ 재사용 가능한 설정

#### 사용 방법:
```csharp
// SpineSymbolData 생성
[CreateAssetMenu(menuName = "SpineTool/Symbol Data")]
public class MySymbolData : SpineSymbolData
{
    // 자동으로 설정 필드 제공
}
```

## 📁 구조

```
SpineTool/
├── Scripts/
│   ├── Runtime/
│   │   ├── SpineAnimModule.cs                      # 통합 애니메이션 모듈
│   │   ├── SpineAnimModuleEnumExtensions.cs        # ⭐ Enum 확장 메서드
│   │   ├── SpineAnimSyncModule.cs                  # ⭐ 애니메이션 동기화 모듈
│   │   ├── SpineIKControl.cs                        # ⭐ IK 제어 모듈
│   │   ├── SpineEventInjector.cs                   # 이벤트 주입 시스템
│   │   ├── SpineEventInjectionAttribute.cs         # Attribute & EventData
│   │   └── SpineSymbolData.cs                      # 메타데이터 관리
│   └── Editor/
│       ├── SpineAnimationPreviewWindow.cs          # 애니메이션 프리뷰 윈도우
│       ├── SpineAnimationInspectorExtension.cs     # 인스펙터 확장
│       ├── SpineAnimationEnumGenerator.cs          # ⭐ Enum 코드 자동 생성기
│       ├── SpineSkeletonInspectorWindow.cs         # ⭐ Skeleton 파라미터 뷰어
│       └── SpineEventInjectorEditor.cs             # 이벤트 편집기
├── Examples/
│   ├── SpineAnimModuleExample.cs                   # AnimModule 사용 예제
│   ├── SpineEnumAnimationExample.cs                # ⭐ Enum 사용 예제
│   ├── SpineAnimSyncExample.cs                     # ⭐ Sync 사용 예제
│   ├── SpineIKExample.cs                           # ⭐ IK 사용 예제
│   ├── SpineCharacterExample.cs                    # Injector 기본 예제
│   └── SpineComboSystemExample.cs                  # Injector 콤보 예제
└── README.md
```

## 🚀 설치

### 1. Spine-Unity Runtime 설치
먼저 [Spine-Unity Runtime](http://esotericsoftware.com/spine-unity-download)을 프로젝트에 임포트하세요.

### 2. SpineTool 설치
이 레포지토리를 Unity 프로젝트에 복사하거나 Git submodule로 추가하세요.

```bash
# Git submodule로 추가
git submodule add https://github.com/yourusername/UnitySpineTool.git Assets/SpineTool
```

### 3. 스크립팅 심볼 확인
`Project Settings → Player → Scripting Define Symbols`에 **SPINE_UNITY**가 있는지 확인하세요.

## 📦 의존성

- **Unity 2020.3 이상**
- **Spine-Unity Runtime** (필수)
- **Newtonsoft.Json** (이벤트 에디터용, Unity 2020+는 기본 포함)

### 외부 의존성 없음!
이전 버전과 달리 **InteractAnimation.Core 의존성이 완전히 제거**되었습니다. 순수 Spine 도구로 독립 사용 가능합니다.

## 🎯 사용 예시

### 1. 에디터 모드 애니메이션 테스트

```
1. Scene에 Spine 캐릭터 배치
2. SkeletonAnimation 컴포넌트 설정
3. Inspector에서 애니메이션 목록 확인
4. ▶ 버튼으로 바로 재생!
5. 플레이 모드 불필요!
```

### 2. Enum 생성 및 사용 (타입 안전!) ⭐⭐ 강력 추천

#### 🔧 셋업 (한번만)

**Step 1: SkeletonDataAsset 준비**
```
Project 창에서 Spine 캐릭터들의 SkeletonDataAsset 확인
예: Assets/Spine/Player.asset
    Assets/Spine/Enemy.asset
    Assets/Spine/Boss.asset
```

**Step 2: Enum Generator 열기**
```
메뉴: Tools → SpineTool → Animation Enum Generator
```

**Step 3: Skeleton 추가**
```
1. [+ Add Skeleton Data] 버튼 클릭
2. Project 창에서 SkeletonDataAsset 드래그 앤 드롭
3. 여러 개 추가 가능 (Player, Enemy, Boss 등)
```

**Step 4: 생성 모드 선택**
```
Individual      : 각각 따로 (권장) → PlayerAnimations.cs, EnemyAnimations.cs
Combined        : 하나로 통합 → AllCharacterAnimations.cs
Smart Combined  : 공통/개별 분리 (최적) → CommonAnimations.cs + 각 전용
```

**Step 5: 설정**
```
Namespace: Game.Animations (선택)
경로: Assets/Scripts/Animations (자동 생성됨)
```

**Step 6: 생성**
```
[Enum 코드 생성] 버튼 클릭!
→ .cs 파일 자동 생성됨
```

---

#### 💻 사용법 (생성 후)

**🔹 Individual 모드 사용 예시:**
```csharp
using SpineTool;

public class Player : MonoBehaviour
{
    private SpineAnimModule animModule;

    void Start()
    {
        animModule = GetComponent<SpineAnimModule>();

        // ✅ Enum 사용 (자동완성, 오타 방지)
        animModule.PlayAnimation(PlayerAnimations.Idle, loop: true);
        animModule.PlayAnimation(PlayerAnimations.Run, loop: true);
        animModule.PlayAnimation(PlayerAnimations.Jump, loop: false);
    }
}

public class Enemy : MonoBehaviour
{
    private SpineAnimModule animModule;

    void Start()
    {
        animModule = GetComponent<SpineAnimModule>();

        // ✅ Enemy 전용 Enum
        animModule.PlayAnimation(EnemyAnimations.Idle, loop: true);
        animModule.PlayAnimation(EnemyAnimations.Attack, loop: false);
    }
}
```

**🔹 Combined 모드 사용 예시:**
```csharp
using SpineTool;

public class CharacterController : MonoBehaviour
{
    private SpineAnimModule animModule;
    public bool isPlayer;

    void Start()
    {
        animModule = GetComponent<SpineAnimModule>();

        if (isPlayer)
        {
            // 모든 캐릭터가 같은 Enum 사용 (Prefix로 구분)
            animModule.PlayAnimation(AllCharacterAnimations.Player_Idle);
            animModule.PlayAnimation(AllCharacterAnimations.Player_Shoot);
        }
        else
        {
            animModule.PlayAnimation(AllCharacterAnimations.Enemy_Idle);
            animModule.PlayAnimation(AllCharacterAnimations.Enemy_Attack);
        }
    }
}
```

**🔹 Smart Combined 모드 사용 예시 (최고!):**
```csharp
using SpineTool;

public class Player : MonoBehaviour
{
    private SpineAnimModule animModule;

    void Update()
    {
        float input = Input.GetAxis("Horizontal");

        if (input != 0)
        {
            // 공통 애니메이션 (모든 캐릭터가 가짐)
            animModule.PlayAnimation(CommonAnimations.Run, loop: true);
        }
        else
        {
            animModule.PlayAnimation(CommonAnimations.Idle, loop: true);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            // Player 전용 애니메이션
            animModule.PlayAnimation(PlayerAnimations.Shoot, loop: false);
            animModule.AddAnimation(CommonAnimations.Idle, loop: true);
        }

        if (Input.GetKeyDown(KeyCode.X))
        {
            // Player 전용
            animModule.PlayAnimation(PlayerAnimations.DoubleJump, loop: false);
        }
    }
}

public class Enemy : MonoBehaviour
{
    private SpineAnimModule animModule;

    void AI()
    {
        // 공통 애니메이션 사용 (Player와 동일)
        animModule.PlayAnimation(CommonAnimations.Idle, loop: true);
        animModule.PlayAnimation(CommonAnimations.Attack, loop: false);

        // Enemy 전용 애니메이션
        animModule.PlayAnimation(EnemyAnimations.Patrol, loop: true);
        animModule.PlayAnimation(EnemyAnimations.Rage, loop: false);
    }
}
```

---

#### 🎯 모드 선택 가이드

| 프로젝트 상황 | 권장 모드 | 이유 |
|--------------|-----------|------|
| 캐릭터마다 애니메이션이 완전히 다름 | **Individual** | 명확한 분리, 타입 안전 |
| 모든 애니메이션 한 곳에서 관리 | **Combined** | 통합 관리 용이 |
| 공통 애니메이션 많음 (idle, run 등) | **Smart Combined** ⭐ | 중복 제거, 최적 |
| 메탈슬러그/액션 게임 | **Smart Combined** ⭐ | 공통 동작 재사용 |
| 프로토타입/빠른 개발 | **Individual** | 가장 심플 |

---

#### ✨ Enum 사용의 장점

```csharp
// ❌ 문자열 방식 (위험)
animModule.PlayAnimation("idel", loop: true);  // 오타! 런타임 에러!
animModule.PlayAnimation("runn", loop: true);  // 오타! 런타임 에러!

// ✅ Enum 방식 (안전)
animModule.PlayAnimation(PlayerAnimations.Idle, loop: true);  // 컴파일 체크!
animModule.PlayAnimation(PlayerAnimations.Run, loop: true);   // 자동완성!
// animModule.PlayAnimation(PlayerAnimations.Idel);  // 컴파일 에러! 즉시 발견!
```

**결과:**
- 🔍 오타 즉시 발견 (컴파일 타임)
- 💡 IDE 자동완성 지원
- 🔄 리팩토링 안전 (Rename 일괄 변경)
- 📝 코드 가독성 향상
- 🛡️ 타입 안전성 보장

---

### 3. 통합 모듈 사용 (가장 간편!) ⭐⭐ 최고 추천

**구조: 샘플 코드(설정) → SpineAnimModule → 기능 작동**

```csharp
using SpineTool;
using UnityEngine;

public class MyCharacter : MonoBehaviour
{
    // ━━━━━ 1단계: 샘플 코드 (설정) ━━━━━
    private SpineAnimModule animModule;

    void Start()
    {
        animModule = GetComponent<SpineAnimModule>();

        // 이벤트 리스너 등록 (Spine Editor에서 추가한 이벤트)
        animModule.AddEventListener("footstep", OnFootstep);
        animModule.AddEventListener("hit_impact", OnHitImpact);
        // ↓ SpineAnimModule이 처리

        // 애니메이션 재생
        animModule.PlayAnimation("walk", true); // 반복 재생
        // ↓ SpineAnimModule이 처리
        // ↓ Walk 애니메이션 재생됨
    }

    // ━━━━━ 3단계: 기능 작동 (콜백) ━━━━━

    void OnFootstep(SpineEventData data)
    {
        // ✅ 결과: 발소리 이벤트 수신
        Debug.Log("발소리!");
        PlaySound(footstepClip);
    }

    void OnHitImpact(SpineEventData data)
    {
        // ✅ 결과: 타격 이벤트 수신
        int damage = data.IntParameter;
        Debug.Log($"타격! 데미지: {damage}");
    }

    // 공격 버튼
    void Attack()
    {
        // ━━━━━ 샘플 코드 ━━━━━
        animModule.PlayAnimation("attack", false); // 한 번만
        animModule.AddAnimation("idle", true);     // 이후 idle
        // ↓ SpineAnimModule이 처리
        // ↓ Attack → Idle 순차 재생됨
    }
}
```

### 4. 이벤트 주입 (Attribute 방식) ⭐ 추천

```csharp
using SpineTool;
using UnityEngine;

// Attribute로 이벤트 자동 등록!
[InjectSpineEvent("attack", "OnAttackStart", 0.0f)]
[InjectSpineEvent("attack", "OnHitImpact", 0.5f, IntParameter = 50)]
[InjectSpineEvent("attack", "OnAttackEnd", 1.0f)]
public class MyCharacter : MonoBehaviour
{
    // SpineEventInjector 컴포넌트 추가 필요!

    void OnAttackStart(SpineEventData data)
    {
        Debug.Log("Attack started!");
    }

    void OnHitImpact(SpineEventData data)
    {
        int damage = data.IntParameter; // 50
        Debug.Log($"Hit! Damage: {damage}");
        // 파티클 생성, 데미지 적용 등
    }

    void OnAttackEnd(SpineEventData data)
    {
        Debug.Log("Attack finished!");
    }
}
```

### 5. Spine 툴 이벤트 받기 (Injector 사용)

Spine Event Editor로 추가한 이벤트를 받으려면:

```csharp
using SpineTool;
using UnityEngine;

public class MyCharacter : MonoBehaviour
{
    // SpineEventInjector의 processSpineToolEvents = true 설정 필요

    // 이 메서드가 자동으로 호출됩니다
    void OnSpineEvent(SpineEventData data)
    {
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
}
```

### 6. 콤보 시스템 예제 (Injector 사용)

```csharp
[InjectSpineEvent("attack1", "OnHit", 0.6f, IntParameter = 10)]
[InjectSpineEvent("attack2", "OnHit", 0.5f, IntParameter = 15)]
[InjectSpineEvent("attack3", "OnHit", 0.7f, IntParameter = 30)]
public class ComboSystem : MonoBehaviour
{
    void OnHit(SpineEventData data)
    {
        int damage = data.IntParameter;
        string animName = data.AnimationName; // "attack1", "attack2", etc.

        Debug.Log($"{animName} hit for {damage} damage!");
        ApplyDamage(damage);
    }
}
```

## 🔧 트러블슈팅

### "SPINE_UNITY 심볼이 정의되지 않았습니다" 오류
**해결:** `com.esotericsoftware.spine.spine-csharp`와 `com.esotericsoftware.spine.spine-unity` 4.3 패키지가 함께 설치됐는지 확인하세요. 심볼을 Player Settings에 수동 추가할 필요는 없습니다.

### 애니메이션이 에디터에서 재생되지 않음
**해결:**
1. SkeletonDataAsset이 올바르게 설정되었는지 확인
2. Spine JSON 파일이 올바른지 확인
3. Inspector를 다시 열어보세요

### Spine 기본 인스펙터가 보이지 않음
**해결:** `SpineAnimationInspectorExtension.cs`가 Spine의 `SkeletonAnimationInspector`를 상속받으므로 모든 기능이 유지됩니다. 만약 문제가 있다면 해당 파일을 삭제하고 EditorWindow 버전만 사용하세요.

## 🎨 스크린샷

### Animation Preview (Inspector)
```
┌─────────────────────────────────────┐
│ 🎬 Animation Preview (Editor Mode)  │
├─────────────────────────────────────┤
│ [🔄 Setup Pose]                     │
│                                     │
│ Animations: 10개                    │
│                                     │
│ ▶  idle        1.50s  (5 timelines)│
│ ■  walk        0.80s  (8 timelines)│ ← 재생 중
│ ▶  run         0.60s  (8 timelines)│
│ ▶  attack      1.20s  (12 timelines)│
└─────────────────────────────────────┘
```

### Spine Event Editor
```
┌─────────────────────────────────────┐
│ Spine Event Editor                  │
├─────────────────────────────────────┤
│ Skeleton Data Asset: [Hero.asset]   │
│                                     │
│ Animations:                         │
│ [attack] ← Selected                 │
│                                     │
│ [Add New Event] [Save to JSON]      │
│                                     │
│ Event 1: hit_impact @ 0.50s        │
│   ├─ String: ""                     │
│   ├─ Int: 50                        │
│   └─ Float: 0                       │
│                                     │
│ ┌─────────────────────────────┐    │
│ │   Animation Preview          │    │
│ │   [Play] [Pause] [Stop]     │    │
│ │   Timeline: ●────────        │    │
│ └─────────────────────────────┘    │
└─────────────────────────────────────┘
```

## 🤝 기여

이슈 리포트 및 Pull Request 환영합니다!

## 📝 라이선스

MIT License

Copyright (c) 2026 lLcrowe

## 📞 문의

이슈 페이지를 통해 문의해주세요.

---

**Made with ❤️ for Spine2D Users**
