# Task 08. SUN_StageEventTimeline 구현

## 목적
모든 관객 렌더링이 같은 이벤트 시간과 상태값을 참조하도록 공통 타임라인을 구현한다.

## 구현 대상
- 클래스명: `SUN_StageEventTimeline`
- 파일명: `SUN_StageEventTimeline.cs`
- 부모 클래스: `MonoBehaviour`
- 권장 경로: `Assets/Scripts/Events/SUN_StageEventTimeline.cs`

## 구현 범위
- 이벤트 목록을 Inspector에서 편집할 수 있게 한다.
- `Play`, `Pause`, `ResetTimeline`으로 공통 시간을 제어한다.
- 현재 시간 기준으로 특정 ObjectId의 `SUN_StageObjectState`를 평가한다.
- 네트워크 동기화는 구현하지 않고 로컬 에디터 공통 타임라인 검증에 집중한다.

## 필수 Inspector 필드
- `List<SUN_StageEventDefinition> _events`
- `bool _playOnStart`
- `float _timelineTimeSeconds`

## public API
- `void Play()`
- `void Pause()`
- `void ResetTimeline()`
- `bool EvaluateObjectState(string objectId, out SUN_StageObjectState state)`
- `float TimelineTimeSeconds`
- `bool IsPlaying`

## 생명주기 기준
- `Awake`: 이벤트 목록과 상태값을 초기화한다.
- `Start`: `_playOnStart`가 켜져 있으면 `Play`를 호출한다.
- `Update`: 재생 중일 때 `Time.deltaTime`으로 타임라인 시간을 증가시킨다.
- `LateUpdate`: 사용하지 않는 것을 기본으로 한다.

## 구현 메모
- 위치와 스케일은 `Vector3.Lerp`, 회전은 `Quaternion.Slerp` 또는 Euler 보간 후 Quaternion 변환으로 처리한다.
- 지속 시간이 0 이하이면 즉시 완료 이벤트로 처리하고 경고 상태를 남긴다.
- 대상 ObjectId가 없는 이벤트는 전체 타임라인을 중단하지 않고 건너뛰게 한다.
- 중복 EventId는 실행 가능하되 경고로 표시할 수 있게 한다.

## 완료 기준
- Play 중 시간이 증가하고 Pause 중 시간이 멈춘다.
- Reset 후 시간이 0으로 돌아간다.
- 같은 ObjectId에 대해 같은 시간에는 항상 같은 상태값이 반환된다.
- 관객 위치와 무관하게 이벤트 상태는 하나의 공통 값으로 유지된다.
