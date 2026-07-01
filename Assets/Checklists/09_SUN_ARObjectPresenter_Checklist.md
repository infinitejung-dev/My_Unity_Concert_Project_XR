# Checklist 09. SUN_ARObjectPresenter 검증

## 구현 확인
- [x] 파일명이 `SUN_ARObjectPresenter.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `MonoBehaviour`를 상속한다.
- [x] `_objectId`, `_targetObject`, `_renderers`, `_coordinateSystem`, `_timeline`이 존재한다.
- [x] `ApplyStageObjectState`, `SetVisible`, `SetStageTransform` API가 존재한다.

## Transform 적용 검증
- [x] `SUN_StageObjectState.StagePositionMeters`가 좌표계 변환을 거쳐 월드 위치로 적용된다.
- [x] `SUN_StageObjectState.StageRotation`이 월드 회전으로 적용된다.
- [x] `SUN_StageObjectState.Scale`이 Transform scale에 적용된다.
- [x] 표시 상태가 false여도 Transform 값은 유지된다.

## Renderer 검증
- [x] `IsVisible` true일 때 Renderer가 켜진다.
- [x] `IsVisible` false일 때 Renderer가 꺼진다.
- [x] Renderer 참조가 비어 있어도 Transform 갱신은 계속된다.
- [x] `_targetObject`가 없으면 해당 Presenter만 비활성화되거나 경고된다.

## 원근 렌더링 검증
- [x] 같은 오브젝트를 관객별로 복제하지 않는다.
- [x] 오브젝트가 카메라를 향해 자동 회전하지 않는다.
- [ ] 관객 A/B 카메라 위치 차이로 서로 다른 원근과 각도가 보인다.

## 완료 판정
- [x] 공통 타임라인 상태가 하나의 실제 3D 오브젝트 렌더링에 적용된다.

## 진행 메모
- Presenter는 타임라인 상태 객체를 재사용해 매 프레임 할당을 줄이도록 구현했다.
- 실제 관객 A/B 카메라 화면에서 원근 차이를 보는 검증은 씬 구성 후 필요하다.
