# Checklist 07. SUN_StageObjectState 검증

## 구현 확인
- [x] 파일명이 `SUN_StageObjectState.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `[System.Serializable]` 데이터 클래스로 작성되어 있다.
- [x] 부모 클래스를 상속하지 않는다.

## 필드 검증
- [x] `ObjectId`가 존재한다.
- [x] `IsVisible`이 존재한다.
- [x] `StagePositionMeters`가 존재한다.
- [x] `StageRotation`이 `Quaternion`으로 존재한다.
- [x] `Scale`이 존재한다.
- [x] `NormalizedProgress`가 존재한다.

## 상태 전달 검증
- [x] 진행률 0 상태를 표현할 수 있다.
- [x] 진행률 0.5 상태를 표현할 수 있다.
- [x] 진행률 1 상태를 표현할 수 있다.
- [x] `IsVisible`이 false여도 위치/회전/스케일 값은 유지된다.
- [x] 동일 상태 객체를 Presenter에 전달할 수 있다.

## 완료 판정
- [x] 타임라인 평가 결과와 오브젝트 렌더링 적용 사이의 데이터 계약이 명확하다.
