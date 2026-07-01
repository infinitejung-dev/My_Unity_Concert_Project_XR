# Task 04. SUN_CentralMarkerCalibrator 구현

## 목적
중앙 마커 인식값 또는 수동 입력값을 기준으로 디바이스 로컬 좌표계를 `Stage Space`에 정렬하는 캘리브레이션 흐름을 만든다.

## 구현 대상
- 클래스명: `SUN_CentralMarkerCalibrator`
- 파일명: `SUN_CentralMarkerCalibrator.cs`
- 부모 클래스: `MonoBehaviour`
- 권장 경로: `Assets/Scripts/Calibration/SUN_CentralMarkerCalibrator.cs`

## 구현 범위
- 체험 시작 전 상태는 `Uncalibrated`로 둔다.
- `BeginCalibration` 호출 시 수동 또는 외부 마커 Pose 입력을 받을 준비를 한다.
- `ApplyMarkerPose`에서 마커 위치/회전 기준 정렬값을 저장한다.
- `ResetCalibration`은 보정 상태를 초기화한다.
- 추적 손실 시 마지막 유효 보정값을 유지하되 상태는 `TrackingLost`로 표시할 수 있게 한다.

## 필수 Inspector 필드
- `Transform _markerVisual`
- `SUN_StageCoordinateSystem _coordinateSystem`
- `bool _useManualCalibration`
- `Vector3 _manualMarkerPositionMeters`
- `Vector3 _manualMarkerEulerDegrees`

## public API
- `void BeginCalibration()`
- `void ApplyMarkerPose(Vector3 markerPositionMeters, Quaternion markerRotation)`
- `void ResetCalibration()`
- `bool HasValidCalibration()`
- `Vector3 ApplyCalibrationToStagePosition(Vector3 stagePositionMeters)`
- `Quaternion ApplyCalibrationToStageRotation(Quaternion stageRotation)`

## 생명주기 기준
- `Awake`: 상태값과 마지막 유효 Pose를 초기화한다.
- `Start`: 중앙 마커 시각 오브젝트와 좌표계 참조를 검사한다.
- `Update`: 수동 캘리브레이션 트리거가 필요한 경우에만 처리한다.
- `LateUpdate`: 마커 시각 오브젝트의 방향 표시를 갱신할 수 있다.

## 구현 메모
- 프로토타입에서는 실제 AR SDK 대신 `_manualMarkerPositionMeters`, `_manualMarkerEulerDegrees`로 동일 흐름을 시뮬레이션한다.
- 보정값은 관객별로 달라도, 보정 후 참조하는 원본 `Stage Space` 데이터는 동일해야 한다.
- 비정상적으로 큰 이동/회전 입력은 즉시 적용하지 않고 경고 상태를 남길 수 있게 한다.

## 완료 기준
- 수동 마커 Pose를 적용하면 `HasValidCalibration()`이 true를 반환한다.
- 리셋 후 상태가 `Uncalibrated`로 돌아간다.
- 보정값 변경 후 관객 카메라와 오브젝트 변환이 같은 기준으로 갱신될 수 있다.
