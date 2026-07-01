# Checklist 04. SUN_CentralMarkerCalibrator 검증

## 구현 확인
- [x] 파일명이 `SUN_CentralMarkerCalibrator.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `MonoBehaviour`를 상속한다.
- [x] `_markerVisual`, `_coordinateSystem`, `_useManualCalibration`, `_manualMarkerPositionMeters`, `_manualMarkerEulerDegrees`가 존재한다.
- [x] `BeginCalibration`, `ApplyMarkerPose`, `ResetCalibration`, `HasValidCalibration` API가 존재한다.

## 캘리브레이션 상태 검증
- [x] 시작 상태가 `Uncalibrated`다.
- [x] `BeginCalibration` 호출 시 `Calibrating` 상태로 전환된다.
- [x] 유효한 마커 Pose를 적용하면 `HasValidCalibration()`이 true가 된다.
- [x] `ResetCalibration` 호출 시 상태가 `Uncalibrated`로 돌아간다.
- [x] 추적 손실 상태에서 마지막 유효 보정값을 유지할 수 있다.

## 마커 기준 검증
- [x] 마커 중심이 `Stage Space` 원점과 일치한다.
- [x] 마커 전방 방향이 무대 방향 `+Z`와 일치한다.
- [x] 수동 입력값으로 실제 AR SDK 없이 캘리브레이션 흐름을 테스트할 수 있다.
- [x] 마커 회전을 일부러 틀었을 때 변환 결과에도 영향이 나타난다.

## 완료 판정
- [x] 관객 카메라와 오브젝트 Presenter가 같은 보정값을 통해 정렬될 준비가 됐다.

## 진행 메모
- 수동 마커 Pose와 TrackingLost 상태 유지 API를 구현했다.
- 실제 AR SDK 마커 인식 연동은 이번 프로토타입 범위에서 제외했다.
