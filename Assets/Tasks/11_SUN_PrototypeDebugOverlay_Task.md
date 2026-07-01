# Task 11. SUN_PrototypeDebugOverlay 구현

## 목적
좌표값, 캘리브레이션 상태, 관객별 카메라 상태, 이벤트 진행 시간, 오브젝트 상태, 경고를 개발자가 즉시 확인할 수 있는 디버그 표시 계층을 만든다.

## 구현 대상
- 클래스명: `SUN_PrototypeDebugOverlay`
- 파일명: `SUN_PrototypeDebugOverlay.cs`
- 부모 클래스: `MonoBehaviour`
- 권장 경로: `Assets/Scripts/Prototype/SUN_PrototypeDebugOverlay.cs`

## 구현 범위
- 화면 텍스트 또는 간단한 UI로 주요 상태를 표시한다.
- 씬 뷰 Gizmo로 `Stage Space` 원점, X/Y/Z 축, 관객 위치, 오브젝트 위치를 표시한다.
- 경고 상태를 `Ready`, `Uncalibrated`, `Calibrating`, `TrackingLost`, `Playing`, `Paused`, `Reset` 등으로 표현한다.
- 디버그 표시 On/Off 토글을 제공한다.

## 권장 표시 항목
- `Stage Space` 원점과 축 방향
- 관객별 몸 위치, 눈 위치, 시선 방향
- 중앙 마커 캘리브레이션 상태와 마지막 유효 보정 시간
- 이벤트 ID, ObjectId, 타임라인 진행 시간, 진행률
- 오브젝트 `Stage Space` 좌표와 Unity 월드 좌표
- 경고 상태: 미정렬, 추적 손실, 누락된 참조, 시야 밖, 이벤트 데이터 오류

## public API
- `void SetVisible(bool isVisible)`
- `void SetStatus(string statusMessage)`
- `void SetWarning(string warningMessage)`
- `void ClearWarning()`
- `void RegisterMissingReference(string referenceName)`

## 생명주기 기준
- `Awake`: UI 텍스트와 표시 루트를 캐시한다.
- `Start`: 초기 상태를 표시한다.
- `Update`: 디버그 토글 입력이 있으면 처리한다.
- `LateUpdate`: 좌표/시간처럼 프레임 최종 상태가 필요한 값을 갱신한다.

## 구현 메모
- 디버그 오버레이가 없어도 핵심 연출 흐름은 동작해야 한다.
- 문자열 생성은 매 프레임 과도하게 할당하지 않도록 필요한 범위로 제한한다.
- 프로토타입 검증 도구이므로 최종 연출물과 분리해 켜고 끌 수 있게 한다.

## 완료 기준
- 좌표계, 관객 Rig, 캘리브레이션, 타임라인, Presenter 상태를 한 화면에서 확인할 수 있다.
- 누락 참조와 잘못된 입력이 조용히 실패하지 않고 경고로 보인다.
- 디버그 표시를 꺼도 카메라와 오브젝트 렌더링 흐름은 유지된다.
