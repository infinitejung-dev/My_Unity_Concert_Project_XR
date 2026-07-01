# Checklist 10. SUN_PrototypeSceneController 검증

## 구현 확인
- [x] 파일명이 `SUN_PrototypeSceneController.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `MonoBehaviour`를 상속한다.
- [x] 좌표계, 캘리브레이터, 타임라인, 관객 Rig, View Controller, Presenter 참조를 연결할 수 있다.
- [x] `PlayTimeline`, `PauseTimeline`, `ResetPrototype`, `CalibrateMarker`, `ResetCalibration` API가 존재한다.

## 씬 연결 검증
- [ ] 중앙 마커 원점 오브젝트가 연결되어 있다.
- [ ] 테스트 관객 Rig가 최소 2개 연결되어 있다.
- [ ] 관객별 카메라 2개 또는 화면 분할 뷰를 구성할 수 있다.
- [ ] 테스트용 3D 오브젝트 Presenter가 연결되어 있다.
- [ ] 공통 이벤트 타임라인이 연결되어 있다.
- [ ] 디버그 오버레이가 연결되어 있다.

## 수동 제어 검증
- [x] 타임라인 Play/Pause/Reset을 수동 실행할 수 있다.
- [x] 중앙 마커 Calibrate/Rescan/Manual Reset을 수동 실행할 수 있다.
- [x] 관객 A/B 위치 이동 흐름을 테스트할 수 있다.
- [x] 관객 A/B yaw/pitch 조정 흐름을 테스트할 수 있다.
- [x] FOV 조정 흐름을 테스트할 수 있다.
- [x] 디버그 표시 On/Off를 실행할 수 있다.

## 완료 판정
- [ ] 한 씬에서 좌표계, 관객 시점, 오브젝트 렌더링, 이벤트 동기화를 순서대로 검증할 수 있다.

## 진행 메모
- 씬 허브와 키보드/공개 API 기반 수동 제어는 구현했다.
- 중앙 마커, 관객 2명, 카메라, Presenter, Overlay를 실제 씬에 연결하는 검증은 남겨뒀다.
