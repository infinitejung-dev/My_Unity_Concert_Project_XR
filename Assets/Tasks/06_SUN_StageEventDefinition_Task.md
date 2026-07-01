# Task 06. SUN_StageEventDefinition 구현

## 목적
공통 타임라인에서 평가할 수 있는 하나의 연출 이벤트 데이터를 정의한다.

## 구현 대상
- 클래스명: `SUN_StageEventDefinition`
- 파일명: `SUN_StageEventDefinition.cs`
- 성격: `[System.Serializable]` 데이터 클래스
- 권장 경로: `Assets/Scripts/Events/SUN_StageEventDefinition.cs`

## 구현 범위
- 이벤트 ID, 대상 오브젝트 ID, 시작 시간, 지속 시간, 표시 상태를 저장한다.
- 시작/종료 위치, 회전, 스케일을 `Stage Space` 기준으로 저장한다.
- 프로토타입에서는 선형 보간을 전제로 데이터만 정의한다.

## 필수 필드
- `string EventId`
- `string ObjectId`
- `float StartTimeSeconds`
- `float DurationSeconds`
- `Vector3 StartPositionMeters`
- `Vector3 EndPositionMeters`
- `Vector3 StartEulerDegrees`
- `Vector3 EndEulerDegrees`
- `Vector3 StartScale`
- `Vector3 EndScale`
- `bool IsVisible`

## 구현 메모
- 부모 클래스는 두지 않는다.
- Unity Inspector에서 `SUN_StageEventTimeline`의 List 안에 표시될 수 있도록 직렬화 가능해야 한다.
- 지속 시간이 0 이하인 경우 타임라인에서 즉시 완료 이벤트로 처리할 수 있도록 원본 값을 유지한다.
- 이벤트 데이터는 관객별로 복제하지 않는다.

## 완료 기준
- `SUN_StageEventTimeline`의 Inspector List에 이벤트 데이터가 펼쳐져 보인다.
- 위치/회전/스케일 값이 모두 `Stage Space` 기준으로 입력 가능하다.
- 최소 1개의 테스트 이벤트를 데이터로 표현할 수 있다.
