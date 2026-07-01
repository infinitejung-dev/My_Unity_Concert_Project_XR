# Checklist 06. SUN_StageEventDefinition 검증

## 구현 확인
- [x] 파일명이 `SUN_StageEventDefinition.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `[System.Serializable]` 데이터 클래스로 작성되어 있다.
- [x] 부모 클래스를 상속하지 않는다.

## 필드 검증
- [x] `EventId`가 존재한다.
- [x] `ObjectId`가 존재한다.
- [x] `StartTimeSeconds`와 `DurationSeconds`가 존재한다.
- [x] `StartPositionMeters`와 `EndPositionMeters`가 존재한다.
- [x] `StartEulerDegrees`와 `EndEulerDegrees`가 존재한다.
- [x] `StartScale`과 `EndScale`이 존재한다.
- [x] `IsVisible`이 존재한다.

## Inspector 검증
- [x] `SUN_StageEventTimeline`의 이벤트 List 안에서 필드가 편집 가능하다.
- [x] 위치값이 `Stage Space` 미터 기준임을 확인할 수 있다.
- [x] 회전 입력은 Euler degree 기준으로 입력 가능하다.
- [x] 최소 1개의 테스트 이벤트를 정의할 수 있다.

## 완료 판정
- [x] `SUN_StageEventTimeline`이 이 데이터를 평가해 오브젝트 상태를 만들 수 있다.

## 진행 메모
- 직렬화 데이터 정의는 완료했다.
- 실제 테스트 이벤트 값 입력은 Unity 에디터의 Timeline 컴포넌트에서 수행해야 한다.
