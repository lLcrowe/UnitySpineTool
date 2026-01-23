# SpineTool

Unity에서 Spine2D 애니메이션 작업을 위한 강력한 에디터 도구 모음입니다.

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

### 📝 2. Spine Event Editor (이벤트 편집기)

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

### ⚡ 3. Spine Event Injector (런타임 이벤트 주입)

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

### 🎮 4. Spine Animation Controller (통합 애니메이션 제어) ⭐ 신규!

**런타임에서 애니메이션을 쉽게 재생하고 이벤트를 등록**할 수 있는 통합 컨트롤러!

#### 특징:
- ✅ 간편한 애니메이션 재생 API (Play, Stop, Pause, Resume)
- ✅ 이벤트 리스너 등록 (문자열 이벤트 이름 기반)
- ✅ SpineSymbolData 지원
- ✅ 속도, 루프, 스킨 제어
- ✅ 블렌딩 시간 설정

#### 사용 방법:

```csharp
using SpineTool;

public class MyCharacter : MonoBehaviour
{
    private SpineAnimationController controller;

    void Start()
    {
        controller = GetComponent<SpineAnimationController>();

        // 이벤트 리스너 등록
        controller.AddEventListener("hit_impact", OnHit);

        // 애니메이션 재생
        controller.PlayAnimation("attack", false);

        // 공격 후 idle로 자동 전환
        controller.AddAnimation("idle", true);
    }

    void OnHit(SpineEventData data)
    {
        int damage = data.IntParameter;
        Debug.Log($"Hit! {damage} damage");
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

### 🏷️ 5. Spine Symbol Data (메타데이터 관리)

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
│   │   ├── SpineAnimationController.cs             # ⭐ 통합 애니메이션 컨트롤러
│   │   ├── SpineEventInjector.cs                   # 이벤트 주입 시스템
│   │   ├── SpineEventInjectionAttribute.cs         # Attribute & EventData
│   │   └── SpineSymbolData.cs                      # 메타데이터 관리
│   └── Editor/
│       ├── SpineAnimationPreviewWindow.cs          # 애니메이션 프리뷰 윈도우
│       ├── SpineAnimationInspectorExtension.cs     # 인스펙터 확장
│       └── SpineEventInjectorEditor.cs             # 이벤트 편집기
├── Examples/
│   ├── SpineControllerExample.cs                   # ⭐ Controller 사용 예제
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

### 2. 통합 컨트롤러 사용 (가장 간편!) ⭐⭐ 최고 추천

```csharp
using SpineTool;
using UnityEngine;

public class MyCharacter : MonoBehaviour
{
    private SpineAnimationController controller;

    void Start()
    {
        controller = GetComponent<SpineAnimationController>();

        // 이벤트 리스너 등록 (Spine Editor에서 추가한 이벤트)
        controller.AddEventListener("footstep", OnFootstep);
        controller.AddEventListener("hit_impact", OnHitImpact);

        // 애니메이션 재생
        controller.PlayAnimation("walk", true); // 반복 재생
    }

    void OnFootstep(SpineEventData data)
    {
        Debug.Log("발소리!");
        // PlaySound(footstepClip);
    }

    void OnHitImpact(SpineEventData data)
    {
        int damage = data.IntParameter;
        Debug.Log($"타격! 데미지: {damage}");
    }

    // 공격 버튼
    void Attack()
    {
        controller.PlayAnimation("attack", false); // 한 번만
        controller.AddAnimation("idle", true);     // 이후 idle
    }
}
```

### 3. 이벤트 주입 (Attribute 방식) ⭐ 추천

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

### 4. Spine 툴 이벤트 받기 (Injector 사용)

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

### 5. 콤보 시스템 예제 (Injector 사용)

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
**해결:** `Project Settings → Player → Scripting Define Symbols`에 `SPINE_UNITY` 추가

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
