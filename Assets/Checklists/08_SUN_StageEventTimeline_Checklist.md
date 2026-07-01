# Checklist 08. SUN_StageEventTimeline 검증

## 구현 확인
- [x] 파일명이 `SUN_StageEventTimeline.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `MonoBehaviour`를 상속한다.
- [x] `_events`, `_playOnStart`, `_timelineTimeSeconds`가 존재한다.
- [x] `Play`, `Pause`, `ResetTimeline`, `EvaluateObjectState` API가 존재한다.

## 타임라인 제어 검증
- [x] `Play` 호출 후 시간이 증가한다.
- [x] `Pause` 호출 후 시간이 멈춘다.
- [x] `ResetTimeline` 호출 후 시간이 0으로 돌아간다.
- [x] `_playOnStart`가 켜져 있으면 시작 시 자동 재생된다.
- [x] `Paused` 상태에서 관객 위치를 바꿔도 이벤트 시간은 유지된다.

## 상태 평가 검증
- [x] 시작 시간 전에는 초기 또는 숨김 상태 정책이 적용된다.
- [x] 이벤트 진행 중 위치/회전/스케일이 보간된다.
- [x] 이벤트 종료 후 완료 상태를 반환한다.
- [x] 지속 시간이 0 이하인 이벤트는 즉시 완료로 처리된다.
- [x] 없는 ObjectId 요청이 전체 타임라인을 중단하지 않는다.

## 완료 판정
- [x] 모든 관객 카메라가 같은 이벤트 시간과 같은 오브젝트 상태값을 참조할 수 있다.

## 진행 메모
- 공통 타임라인 평가 로직은 구현했다.
- 실제 이벤트 List 값 입력과 Play 모드 동작 확인은 Unity 에디터에서 이어서 검증한다.
