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

### 🏷️ 3. Spine Symbol Data (메타데이터 관리)

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
│   │   └── SpineSymbolData.cs                      # 메타데이터 관리
│   └── Editor/
│       ├── SpineAnimationPreviewWindow.cs          # 애니메이션 프리뷰 윈도우
│       ├── SpineAnimationInspectorExtension.cs     # 인스펙터 확장
│       └── SpineEventInjectorEditor.cs             # 이벤트 편집기
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

### 에디터 모드 애니메이션 테스트

```
1. Scene에 Spine 캐릭터 배치
2. SkeletonAnimation 컴포넌트 설정
3. Inspector에서 애니메이션 목록 확인
4. ▶ 버튼으로 바로 재생!
5. 플레이 모드 불필요!
```

### 이벤트 추가하기

```
1. Spine Event Editor 열기
2. SkeletonDataAsset 선택
3. "attack" 애니메이션 선택
4. 0.5초 지점에 "hit_impact" 이벤트 추가
5. Int Parameter: 50 (데미지)
6. 저장!
```

### 런타임에서 이벤트 받기

```csharp
using Spine;
using Spine.Unity;
using UnityEngine;

public class MySpineCharacter : MonoBehaviour
{
    private SkeletonAnimation skeletonAnimation;

    void Start()
    {
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        skeletonAnimation.AnimationState.Event += OnSpineEvent;
    }

    void OnSpineEvent(TrackEntry trackEntry, Event e)
    {
        if (e.Data.Name == "hit_impact")
        {
            int damage = e.Int; // 50
            Debug.Log($"Hit with damage: {damage}");
        }
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
