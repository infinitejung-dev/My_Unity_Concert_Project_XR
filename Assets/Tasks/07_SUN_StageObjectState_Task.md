# Task 07. SUN_StageObjectState 구현

## 목적
타임라인 평가 결과와 실제 렌더링 적용 사이에서 사용할 오브젝트 상태 전달 객체를 만든다.

## 구현 대상
- 클래스명: `SUN_StageObjectState`
- 파일명: `SUN_StageObjectState.cs`
- 성격: `[System.Serializable]` 데이터 클래스
- 권장 경로: `Assets/Scripts/Events/SUN_StageObjectState.cs`

## 구현 범위
- 특정 프레임의 오브젝트 ID, 표시 여부, `Stage Space` 위치, 회전, 스케일, 진행률을 저장한다.
- `SUN_StageEventTimeline`이 값을 만들고 `SUN_ARObjectPresenter`가 값을 적용한다.
- 데이터 객체는 관객별 시점 차이를 만들기 위한 복제본이 아니라 공통 이벤트 상태값이다.

## 필수 필드
- `string ObjectId`
- `bool IsVisible`
- `Vector3 StagePositionMeters`
- `Quaternion StageRotation`
- `Vector3 Scale`
- `float NormalizedProgress`

## 권장 API
- 기본 생성자
- 전체 필드 초기화 생성자
- 초기 상태 또는 숨김 상태를 만들 수 있는 정적 헬퍼

## 구현 메모
- `NormalizedProgress`는 0에서 1 사이를 기본 범위로 한다.
- 회전은 내부 계산 결과인 `Quaternion`으로 저장한다.
- 표시 여부가 false여도 위치, 회전, 스케일 값은 유지한다.

## 완료 기준
- 타임라인이 특정 ObjectId에 대한 상태값을 만들 수 있다.
- Presenter가 같은 상태 객체를 받아 Transform과 Renderer 표시 상태에 적용할 수 있다.
- 진행률 0, 중간, 1 상태를 명확히 표현할 수 있다.
