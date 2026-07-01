# Task 03. SUN_AudienceRig 구현

## 목적
관객 한 명의 몸 기준 위치와 눈 위치를 `Stage Space` 기준으로 계산하고, Unity 월드 카메라 루트 위치를 갱신할 수 있는 관객 Rig를 만든다.

## 구현 대상
- 클래스명: `SUN_AudienceRig`
- 파일명: `SUN_AudienceRig.cs`
- 부모 클래스: `MonoBehaviour`
- 권장 경로: `Assets/Scripts/Audience/SUN_AudienceRig.cs`

## 구현 범위
- `SUN_AudienceProfile`의 좌석 위치, 눈 높이, 디바이스 오프셋을 합산한다.
- 계산 결과는 `Stage Space` 기준 `Vector3`로 유지한다.
- 필요 시 `SUN_StageCoordinateSystem`을 통해 `_cameraRoot`의 Unity 월드 위치를 갱신한다.
- 프로필이 비어 있을 때는 기본 테스트 관객 위치 `(0, 0, -3)`과 눈 높이 `1.6m`를 사용한다.

## 필수 Inspector 필드
- `SUN_AudienceProfile _profile`
- `Transform _cameraRoot`
- `SUN_StageCoordinateSystem _coordinateSystem`
- `bool _applyToCameraRoot`

## public API
- `string AudienceId`
- `Vector3 GetSeatStagePositionMeters()`
- `Vector3 GetEyeStagePositionMeters()`
- `float GetFieldOfViewDegrees()`
- `void ApplyCameraRootPosition()`

## 생명주기 기준
- `Awake`: `_cameraRoot`가 비어 있으면 현재 Transform을 기본값으로 캐시한다.
- `Start`: 프로필과 좌표계 참조 상태를 검사한다.
- `Update`: 관객 위치 수동 변경 입력이 생기기 전까지 사용하지 않는다.
- `LateUpdate`: `_applyToCameraRoot`가 켜져 있으면 카메라 루트 위치를 최종 반영한다.

## 구현 메모
- 눈 위치 계산식은 `seatStagePositionMeters + Vector3.up * eyeHeightMeters + deviceOffsetMeters`로 분리한다.
- 관객 몸 기준 위치와 눈 기준 위치를 혼동하지 않도록 메서드명을 명확히 한다.
- 좌표계 참조가 없으면 Unity 월드 위치 반영만 건너뛰고, `Stage Space` 눈 위치 계산은 계속 가능하게 한다.

## 완료 기준
- 관객 프로필 A/B를 바꾸면 서로 다른 눈 위치가 계산된다.
- 눈 높이를 변경하면 카메라 루트 Y 위치가 달라진다.
- 좌표계 참조가 있을 때 카메라 루트가 Unity 월드 위치로 변환되어 배치된다.
