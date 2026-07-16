# Changelog

## [1.1.0] - 2026-07-16

### Changed

- Spine C# Runtime 4.3.39와 Spine Unity Runtime 4.3.98 API에 맞게 런타임·에디터·VAT 코드를 마이그레이션했습니다.
- Spine 4.3의 분리된 `spine-csharp` 어셈블리를 명시적으로 참조합니다.
- `versionDefines`가 Spine Unity 패키지 ID를 감지하도록 수정해 `SPINE_UNITY` 심볼을 자동 활성화합니다.
- Spine 4.3의 통합 Constraint, Pose/AppliedPose, 분리된 SkeletonRenderer 구조를 반영했습니다.

### Added

- Spine 4.3 API 계약과 패키지 어셈블리 로드를 확인하는 EditMode 테스트를 추가했습니다.
