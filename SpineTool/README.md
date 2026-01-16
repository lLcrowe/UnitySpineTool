# SpineTool

Spine2D 애니메이션 시스템을 위한 독립적인 도구 모듈입니다.

## 📁 구조

```
SpineTool/
├── Scripts/
│   ├── Runtime/
│   │   ├── SpineAnimationSystem.cs      # Spine 애니메이션 시스템 구현
│   │   ├── SpineSymbolData.cs           # 심볼 기반 메타데이터 관리
│   │   └── SpineEventInjector.cs        # 런타임 이벤트 주입
│   └── Editor/
│       └── SpineEventInjectorEditor.cs  # GUI 기반 이벤트 편집기
└── Examples/
    ├── ChestInteractable.cs             # Spine 상자 예제
    ├── ChestWithAutoInjection.cs        # 자동 주입 예제
    └── SpineGrappleController.cs        # Spine 그래플 시스템 예제
```

## ✨ 주요 기능

### 1. SpineAnimationSystem
- Spine-Unity 런타임 통합
- 심볼 ID 기반 애니메이션 관리
- 스킨 변경, 블렌딩 지원
- Spine 이벤트 자동 처리

### 2. SpineSymbolData
- ScriptableObject 기반 메타데이터
- 태그 기반 필터링
- 우선순위 시스템
- 애니메이션 설정 중앙 관리

### 3. SpineEventInjector
- Attribute 기반 런타임 이벤트 주입
- Spine 툴 이벤트 자동 통합
- Coroutine 기반 정확한 타이밍 제어

### 4. SpineEventInjectorEditor (⭐ 핵심 기능)
- **실시간 애니메이션 프리뷰**
- **마우스 컨트롤** (우클릭 패닝, 휠 줌)
- **Visual Timeline** (이벤트 위치 시각화)
- **Spine JSON 직접 편집**
- 저장되지 않은 변경사항 추적

## 🚀 빠른 시작

### SpineEventInjectorEditor 사용하기

```
1. Unity 메뉴 → Tools → InteractAnimation → Spine Event Editor
2. SkeletonDataAsset을 Inspector에 드래그
3. 애니메이션 목록에서 선택
4. 실시간 프리뷰로 확인
5. Add New Event로 이벤트 추가
6. Save to JSON
```

### Attribute 기반 자동 주입

```csharp
using InterectAnimationModule.Core;
using SpineTool;

[InjectSpineEvent("chest_open", "OnRewardSpawn", 0.6f, IntParameter = 100)]
public class Chest : InteractableObjectBase
{
    protected override void Start()
    {
        var spineSystem = gameObject.AddComponent<SpineAnimationSystem>();
        SetAnimationSystem(spineSystem);

        // SpineEventInjector 추가 (자동 주입)
        gameObject.AddComponent<SpineEventInjector>();

        base.Start();
    }

    private void OnRewardSpawn(AnimationEventData data)
    {
        int score = data.intParameter; // 100
        Debug.Log($"Reward spawned with score: {score}");
    }
}
```

## 📦 의존성

- **Spine-Unity Runtime** (필수)
- **InterectAnimationModule Core** (AnimationSystemBase, InteractableObjectBase, AnimationEventData)

## 🔧 독립 레포지토리로 분리 준비

이 폴더는 독립적인 SpineTool 레포지토리로 분리될 수 있도록 구조화되어 있습니다.

### 분리 시 필요한 작업
1. `SpineTool/` 폴더를 새로운 Git 레포지토리로 이동
2. Core 모듈에 대한 의존성 설정 (Unity Package 또는 Git submodule)
3. Assembly Definition 파일 생성 (선택사항)

## 📝 라이선스

InterectAnimationModule과 동일한 라이선스 적용
