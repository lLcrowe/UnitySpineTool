# VAT + Selective IK Offset 구현 계획

**작성일:** 2026-02-24
**모듈 위치:** `Core/Modules/SpineVAT/`
**목표:** 기존 VAT 시스템에 선택적 IK 오프셋 기능 추가

---

## 배경

현재 VAT(Vertex Animation Texture)는 베이킹된 정점 위치를 텍스처에서 읽어 GPU에서 렌더링한다.
이벤트 시스템은 이미 구현되어 있으나, 런타임 본 제어(IK 등)는 불가능한 상태.

Compute Shader Skinning(6~8주)은 과도하므로,
**특정 본(발/손)에 한정된 IK 오프셋을 VAT 위에 얹는 경량 방식**을 채택한다.

---

## 핵심 개념

```
[VAT 텍스처 → 베이킹된 정점 위치]
              +
[IK Solver → 특정 본의 오프셋 계산]
              =
[최종 정점 위치]
```

- VAT의 대량 렌더링 성능은 그대로 유지
- IK가 필요한 유닛에만 선택적으로 적용
- 오프셋 대상: IK 본에 영향받는 정점들만

---

## 지원 범위

### 가능

- 발 IK (지면 고정, 경사면 보정)
- 손 IK (무기 그립, 오브젝트 잡기)
- 2~3개 본에 대한 위치 오프셋

### 불가능

- 풀 바디 IK
- 런타임 본 회전/스케일 조작
- 모든 본에 대한 자유 제어 (→ Compute Shader Skinning 필요)

---

## 구현 단계

### Phase 1: 베이커 확장 — IK 본-정점 매핑 추출 (2~3일)

**파일:** `Core/Modules/SpineVAT/Editor/SpineVatBaker.cs`

- 베이킹 시 `MeshAttachment`에서 정점별 주요 영향 본(dominant bone) 식별
- 지정된 IK 본 목록에 해당하는 정점 인덱스 + 가중치 추출
- `SpineVatData`에 메타데이터로 저장

```csharp
// 추출할 데이터 구조 (개념)
public struct IKVertexInfluence
{
    public int vertexIndex;      // VAT 메시 내 정점 인덱스
    public int boneIndex;        // 영향 본 인덱스
    public float weight;         // 가중치
}
```

**산출물:** 베이킹 결과에 IK 정점 매핑 데이터 포함

---

### Phase 2: SpineVatData 확장 (1일)

**파일:** `Core/Modules/SpineVAT/Runtime/SpineVatData.cs`

- IK 관련 메타데이터 필드 추가

```csharp
[Header("IK Offset Data (Optional)")]
public List<IKVertexInfluence> ikVertexInfluences;
public string[] ikBoneNames;           // IK 대상 본 이름 목록
public int ikBoneCount;
```

**산출물:** 확장된 ScriptableObject 구조

---

### Phase 3: 런타임 IK 오프셋 계산기 (2~3일)

**신규 파일:** `Core/Modules/SpineVAT/Runtime/SpineVatIKController.cs`

- 경량 Skeleton을 병렬로 유지하여 IK 본 위치만 계산
- 또는 외부 IK Target(Transform)으로부터 오프셋 직접 산출
- 계산된 오프셋을 ComputeBuffer 또는 MaterialPropertyBlock으로 셰이더에 전달

```
매 프레임:
  1. IK Target 위치 확인
  2. VAT 현재 프레임의 원본 본 위치 참조
  3. 오프셋 = IK Target - 원본 본 위치
  4. 영향 정점들에 가중치 적용한 오프셋 계산
  5. 셰이더로 전달
```

**산출물:** 런타임 IK 오프셋 컴포넌트

---

### Phase 4: 셰이더 수정 (1일)

**파일:** `Core/Modules/SpineVAT/Shader/SpineURPVat.shader`

- IK 오프셋 버퍼 입력 추가
- 기존 VAT 위치에 오프셋 합산

```hlsl
// 기존
float3 localPos = lerp(pos0.rgb, pos1.rgb, lerpFactor);

// 변경
float3 localPos = lerp(pos0.rgb, pos1.rgb, lerpFactor);
#ifdef _IK_OFFSET_ON
    float3 ikOffset = _IKOffsetBuffer[vertexIndex];
    localPos += ikOffset;
#endif
```

- IK 미사용 유닛에는 키워드 OFF로 성능 영향 없음

**산출물:** IK 오프셋 지원 셰이더 variant

---

### Phase 5: 통합 테스트 및 최적화 (2~3일)

- 발 IK 테스트 씬 구성 (경사면 + VAT 캐릭터)
- IK ON/OFF 성능 비교 프로파일링
- 대량 유닛 중 일부만 IK 적용하는 혼합 시나리오 검증
- SpineVatAnimModule과의 이벤트 연동 확인

**산출물:** 테스트 씬 + 프로파일 결과

---

## 예상 일정

| Phase | 내용 | 기간 |
|-------|------|------|
| 1 | 베이커 확장 (IK 본-정점 매핑) | 2~3일 |
| 2 | SpineVatData 확장 | 1일 |
| 3 | 런타임 IK 오프셋 계산기 | 2~3일 |
| 4 | 셰이더 수정 | 1일 |
| 5 | 통합 테스트 및 최적화 | 2~3일 |
| **합계** | | **8~11일 (약 2주)** |

---

## 기술적 리스크

| 리스크 | 수준 | 대응 |
|--------|------|------|
| MeshAttachment에서 본 가중치 접근 불가 | 중 | 리플렉션 또는 SetupPose 역산으로 우회 |
| 오프셋 적용 시 메시 찢어짐 (이웃 정점 불연속) | 중 | 감쇠(falloff) 함수로 주변 정점 부드럽게 블렌딩 |
| IK + VAT 인스턴싱 병행 시 성능 저하 | 저 | IK 유닛만 별도 드로우콜 분리, 나머지는 기존 인스턴싱 유지 |

---

## 향후 확장 가능성

- **Compute Shader Skinning**: 이 작업에서 축적한 본-정점 매핑 데이터가 기반이 됨
- **Look-At / Head Aiming**: 동일한 오프셋 방식으로 머리 방향 제어 추가 가능
- **Hit Reaction**: 피격 시 특정 본 오프셋으로 간단한 리액션 표현
