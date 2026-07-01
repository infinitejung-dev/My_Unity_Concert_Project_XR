# Task 10. SUN_PrototypeSceneController 구현

## 목적
테스트 씬의 관객 프리셋, 중앙 마커, 이벤트 타임라인, 오브젝트 Presenter, 수동 제어 입력을 연결하는 프로토타입 허브를 만든다.

## 구현 대상
- 클래스명: `SUN_PrototypeSceneController`
- 파일명: `SUN_PrototypeSceneController.cs`
- 부모 클래스: `MonoBehaviour`
- 권장 경로: `Assets/Scripts/Prototype/SUN_PrototypeSceneController.cs`

## 구현 범위
- 테스트 씬 시작 시 필수 참조를 검사한다.
- 타임라인 `Play`, `Pause`, `Reset` 제어를 제공한다.
- 중앙 마커 `Calibrate`, `Rescan`, `Manual Reset` 제어를 제공한다.
- 관객 A/B 위치, 시선 yaw/pitch, FOV 조정 흐름을 연결한다.
- 일부 관객 Rig가 실패해도 나머지 관객과 타임라인 검증은 계속 진행되게 한다.

## 권장 Inspector 필드
- `SUN_StageCoordinateSystem _coordinateSystem`
- `SUN_CentralMarkerCalibrator _calibrator`
- `SUN_StageEventTimeline _timeline`
- `SUN_AudienceRig[] _audienceRigs`
- `SUN_AudienceViewController[] _viewControllers`
- `SUN_ARObjectPresenter[] _objectPresenters`
- `SUN_PrototypeDebugOverlay _debugOverlay`

## public API
- `void PlayTimeline()`
- `void PauseTimeline()`
- `void ResetPrototype()`
- `void CalibrateMarker()`
- `void ResetCalibration()`
- `void SetDebugVisible(bool isVisible)`

## 생명주기 기준
- `Awake`: 배열과 내부 참조를 캐시한다.
- `Start`: 필수 참조 누락 목록을 생성하고 디버그 오버레이에 전달한다.
- `Update`: 키보드 또는 UI 버튼 입력을 처리한다.
- `LateUpdate`: 전체 상태 요약을 디버그 오버레이로 전달한다.

## 구현 메모
- UI는 프로토타입 수동 제어 수준으로 제한한다.
- 상용 서비스용 계정, 권한, 서버 동기화, 운영 자동화는 구현하지 않는다.
- 미정렬 상태에서 Play를 눌러도 타임라인은 재생 가능하되 AR 표시 쪽은 미정렬 경고를 유지한다.

## 완료 기준
- 한 씬에서 좌표계, 관객 시점, 오브젝트 렌더링, 이벤트 동기화 흐름을 연결할 수 있다.
- 수동 리셋 후 같은 입력값으로 같은 결과가 반복된다.
- 일부 참조 누락이 있어도 크래시하지 않고 누락 목록을 확인할 수 있다.
