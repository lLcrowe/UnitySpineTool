# Changelog

## [2.0.0] - 2026-07-27

### Changed

- 기본 `SkeletonAnimation`·`SkeletonRenderer` 제어 패키지로 역할을 축소했습니다.
- VAT runtime, baker, preview, shader와 examples를 패키지에서 제거했습니다.

## [1.1.1] - 2026-07-27

### Fixed

- Unity의 UPM `versionDefines` 형식에 맞는 Spine 4.3 버전 범위를 지정해 `SPINE_UNITY` 심볼과 UnitySpineTool 어셈블리가 실제로 활성화되도록 수정했습니다.

## [1.1.0] - 2026-07-16

### Changed

- Spine C# Runtime 4.3.39와 Spine Unity Runtime 4.3.98 API에 맞게 런타임·에디터·VAT 코드를 마이그레이션했습니다.
- Spine 4.3의 분리된 `spine-csharp` 어셈블리를 명시적으로 참조합니다.
- `versionDefines`가 Spine Unity 패키지 ID를 감지하도록 수정해 `SPINE_UNITY` 심볼을 자동 활성화합니다.
- Spine 4.3의 통합 Constraint, Pose/AppliedPose, 분리된 SkeletonRenderer 구조를 반영했습니다.

### Added

- Spine 4.3 API 계약과 패키지 어셈블리 로드를 확인하는 EditMode 테스트를 추가했습니다.
