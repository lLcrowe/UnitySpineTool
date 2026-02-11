# SpineVAT - Vertex Animation Texture 기반 대규모 렌더링

Spine 애니메이션을 GPU 인스턴싱으로 처리하여 수백~수천 개의 유닛을 **1 Draw Call**로 렌더링하는 모듈입니다.

## 왜 필요한가?

| 항목 | 기존 Spine Runtime | SpineVAT |
|------|-------------------|----------|
| 연산 | CPU (SkeletonAnimation) | GPU (텍스처 Fetch) |
| 드로우 콜 | 유닛당 1회 | 1023개당 1회 |
| 100마리 | CPU 과부하 | 거의 무부하 |
| 이벤트 | Spine EventTimeline | normalizedTime 기반 재현 |

## 구조

```
Core/Modules/SpineVAT/
├── Runtime/
│   ├── SpineVatData.cs          # SO 데이터 컨테이너
│   ├── SpineVatAnimModule.cs    # SpineAnimModule 호환 API
│   └── SpineVatRenderer.cs      # 중앙 매니저 (DrawMeshInstanced)
├── Editor/
│   ├── SpineVatBaker.cs         # VAT 베이커 (Spine → Texture)
│   └── SpineVatPreviewWindow.cs # 에디터 프리뷰 윈도우
├── Shader/
│   └── SpineURPVat.shader       # URP GPU 인스턴싱 셰이더
└── Examples/
    └── VatEnemySpawnerExample.cs
```

## 의존성 분리

```
Editor (Baker)          Runtime (Renderer, Data, AnimModule)
━━━━━━━━━━━━━           ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Spine.Unity 참조 O       Spine.Unity 참조 X (순수 Mesh/Texture)
SkeletonData 분석        SpineVatData SO만 참조
EventTimeline 추출       VatEventData로 이벤트 재생
```

## 사용법

### 1단계: VAT 베이크 (에디터)

1. 메뉴: `Tools → SpineVAT → VAT Baker`
2. SkeletonDataAsset 할당
3. 베이크할 애니메이션 선택
4. Sample Rate, 출력 경로 설정
5. **Bake VAT** 클릭

결과물:
- `*_PositionTex.asset` — 버텍스 위치 텍스처 (Width=버텍스수, Height=프레임수)
- `*_Mesh.asset` — 공유 메쉬 (UV2에 버텍스 인덱스 기록)
- `*.asset` — SpineVatData SO (클립 메타데이터 + 이벤트)

### 2단계: 씬 셋업

```
[씬 구조]
├── VatRenderer (GameObject)
│   └── SpineVatRenderer (컴포넌트)
│       ├── Vat Data: 베이크된 SO
│       └── Vat Material: SpineVAT/URPVat 셰이더 머티리얼
│
├── Enemy_01 (GameObject)
│   └── SpineVatAnimModule
│       └── Vat Data: 동일한 SO
│
├── Enemy_02 ...
```

### 3단계: 코드 사용

#### SpineAnimModule과 동일한 API

```csharp
using SpineVAT;

public class Enemy : MonoBehaviour
{
    private SpineVatAnimModule anim;

    void Start()
    {
        anim = GetComponent<SpineVatAnimModule>();

        // SpineAnimModule과 동일한 사용법
        anim.PlayAnimation("walk", true);
        anim.SetSpeed(1.5f);

        // 이벤트 리스너 등록
        anim.AddEventListener("footstep", OnFootstep);
        anim.AddEventListener("attack_hit", OnHit);
    }

    void OnFootstep(VatAnimEventData evt)
    {
        AudioManager.Play("footstep");
    }

    void OnHit(VatAnimEventData evt)
    {
        DamageSystem.Apply(evt.EventName);
    }

    void Attack()
    {
        anim.PlayAnimation("attack", false);
    }
}
```

#### 대량 스폰 (Renderer 직접 사용)

```csharp
using SpineVAT;

public class Spawner : MonoBehaviour
{
    void SpawnSwarm(int count)
    {
        var renderer = SpineVatRenderer.Instance;

        // 이벤트 구독
        renderer.OnVatEvent += (unitIndex, eventName) => {
            // 사운드, 이펙트 등
        };

        // 대량 스폰 — 전부 struct 추가, Draw Call 1회
        for (int i = 0; i < count; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 20f;
            renderer.AddUnit(pos);
        }
    }
}
```

## API 대응표

| SpineAnimModule | SpineVatAnimModule | 비고 |
|---|---|---|
| `PlayAnimation("walk", true)` | `PlayAnimation("walk", true)` | 동일 |
| `StopAnimation()` | `StopAnimation()` | 동일 |
| `PauseAnimation()` | `PauseAnimation()` | 동일 |
| `ResumeAnimation()` | `ResumeAnimation()` | 동일 |
| `SetSpeed(2f)` | `SetSpeed(2f)` | 동일 |
| `HasAnimation("attack")` | `HasAnimation("attack")` | 동일 |
| `GetAnimationDuration("idle")` | `GetAnimationDuration("idle")` | 동일 |
| `AddEventListener("hit", cb)` | `AddEventListener("hit", cb)` | 콜백 타입 다름 |
| `RemoveEventListener("hit", cb)` | `RemoveEventListener("hit", cb)` | 동일 |
| `SetSkin("red")` | — | VAT는 스킨별로 별도 베이크 필요 |
| `AddAnimation("idle", true)` | — | VAT는 큐잉 미지원 |

## 이벤트 시스템 흐름

```
[에디터 베이크]
Spine EventTimeline → VatEventData { eventName, normalizedTime }

[런타임]
SpineVatRenderer.Update()
  → 유닛별 prevTime~currTime 구간에 이벤트 있는지 체크
    → 있으면 OnVatEvent(unitIndex, "hit") 발송
      → SpineVatAnimModule.OnRendererEvent()
        → unitIndex 필터링 (자기 것만)
        → eventListeners["hit"] 콜백 호출
```

## 셰이더 동작 원리

```hlsl
// 1. 인스턴스별 _AnimTime (0~1) 수신
// 2. 현재 프레임 계산: frame = AnimTime * (FrameCount - 1)
// 3. frame0, frame1 사이 보간 비율 계산
// 4. UV 계산: X = 버텍스인덱스/총버텍스, Y = (FrameOffset+frame)/총프레임
// 5. 텍스처에서 위치 Fetch → lerp → 최종 위치
```

## 에디터 프리뷰

메뉴: `Tools → SpineVAT → VAT Animation Preview`

- SpineVatAnimModule이 있는 GameObject 여러 개 선택
- 클립별 재생/정지 버튼
- 에디터 모드에서 실시간 프리뷰
- 이벤트 타이밍 표시

## 제약 사항

- 스킨 변경: 스킨별로 별도 VAT 베이크 필요
- 애니메이션 블렌딩: 클립 간 크로스페이드 미지원 (프레임 단위 전환)
- 버텍스 수 고정: 모든 프레임에서 동일한 버텍스 수 필요 (Spine 기본 동작과 호환)
- 큐잉 미지원: AddAnimation 대신 PlayAnimation으로 즉시 전환
