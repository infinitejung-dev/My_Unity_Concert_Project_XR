# Task 09. SUN_ARObjectPresenter 구현

## 목적
공통 이벤트 상태값인 `SUN_StageObjectState`를 실제 3D 오브젝트의 Transform과 Renderer 상태에 적용한다.

## 구현 대상
- 클래스명: `SUN_ARObjectPresenter`
- 파일명: `SUN_ARObjectPresenter.cs`
- 부모 클래스: `MonoBehaviour`
- 권장 경로: `Assets/Scripts/Presentation/SUN_ARObjectPresenter.cs`

## 구현 범위
- 오브젝트 ID를 기준으로 타임라인 상태를 조회한다.
- `Stage Space` 위치와 회전을 `SUN_StageCoordinateSystem`으로 Unity 월드 Transform에 변환한다.
- Renderer 표시 상태를 이벤트의 IsVisible 값에 맞춘다.
- Transform 정보는 표시 상태가 false여도 유지한다.
- 오브젝트를 카메라 방향으로 자동 회전시키는 빌보드 처리는 하지 않는다.

## 필수 Inspector 필드
- `string _objectId`
- `Transform _targetObject`
- `Renderer[] _renderers`
- `SUN_StageCoordinateSystem _coordinateSystem`
- `SUN_StageEventTimeline _timeline`

## public API
- `void ApplyStageObjectState(SUN_StageObjectState state)`
- `void SetVisible(bool isVisible)`
- `void SetStageTransform(Vector3 stagePositionMeters, Quaternion stageRotation, Vector3 scale)`

## 생명주기 기준
- `Awake`: `_targetObject`와 Renderer 참조를 캐시한다.
- `Start`: ObjectId, 좌표계, 타임라인 참조를 검사한다.
- `Update`: 사용하지 않는 것을 기본으로 한다.
- `LateUpdate`: 타임라인 상태를 조회해 Transform과 표시 상태를 최종 반영한다.

## 구현 메모
- `_targetObject`가 없으면 해당 Presenter만 비활성화하고 ObjectId와 오류 상태를 남긴다.
- Renderer 참조가 비어 있어도 Transform 갱신은 계속할 수 있게 한다.
- 이벤트 상태가 없을 때 마지막 유효 상태 유지 또는 초기 상태 복귀 중 하나를 명확히 선택한다.

## 완료 기준
- 이벤트 상태의 위치, 회전, 스케일이 오브젝트 Transform에 반영된다.
- 표시 상태 false에서 Renderer만 꺼지고 Transform 값은 유지된다.
- 같은 오브젝트를 관객별로 복제하지 않아도 카메라 위치 차이로 원근이 달라진다.
