# Task 01. SUN_StageCoordinateSystem 구현

## 목적
공연장 전체를 하나의 `Stage Space` 좌표계로 정의하고, `Stage Space` 좌표와 Unity 월드 Transform 사이의 변환을 담당하는 단일 진입점을 만든다.

## 구현 대상
- 클래스명: `SUN_StageCoordinateSystem`
- 파일명: `SUN_StageCoordinateSystem.cs`
- 부모 클래스: `MonoBehaviour`
- 권장 경로: `Assets/Scripts/Core/SUN_StageCoordinateSystem.cs`

## 구현 범위
- 중앙 마커 중심을 `Stage Space` 원점 `(0, 0, 0)`으로 해석한다.
- `+Y`는 위쪽, `+Z`는 중앙 마커에서 무대 중심을 바라보는 방향, `+X`는 무대를 바라봤을 때 오른쪽 방향으로 둔다.
- Inspector 필드는 `[SerializeField] private`로 작성한다.
- 거리 단위는 미터 기준으로 관리하고, Unity 월드 스케일 변환은 `_metersToUnityScale`에서 처리한다.
- 좌표 변환 함수는 다른 클래스에서 직접 계산하지 않도록 이 클래스에 모은다.

## 필수 Inspector 필드
- `Transform _stageOrigin`
- `Vector3 _stageForward`
- `Vector3 _stageRight`
- `float _metersToUnityScale`

## public API
- `Vector3 StageToWorldPosition(Vector3 stagePositionMeters)`
- `Vector3 WorldToStagePosition(Vector3 worldPosition)`
- `Quaternion StageToWorldRotation(Quaternion stageRotation)`
- `Quaternion WorldToStageRotation(Quaternion worldRotation)`

## 생명주기 기준
- `Awake`: `_stageOrigin` 기본값과 축 벡터를 캐시 및 정규화한다.
- `Start`: 스케일 값이 0 이하인지 검사하고 기본값 1.0으로 보정한다.
- `Update`: 사용하지 않는 것을 기본으로 한다.
- `LateUpdate`: 사용하지 않는 것을 기본으로 한다.

## 구현 메모
- `_stageForward`와 `_stageRight`는 서로 직교하도록 보정하거나, 최소한 경고 상태를 남긴다.
- `Stage Space`의 권위 있는 좌표값과 Unity 월드 표현 계층이 섞이지 않도록 함수명과 변수명에 `Stage`, `World`, `Meters`를 명확히 사용한다.
- Gizmo에서 원점, X/Y/Z 축, 무대 방향을 확인할 수 있게 한다.

## 완료 기준
- 다른 컴포넌트가 좌표 변환을 직접 계산하지 않고 이 클래스를 참조할 수 있다.
- 서로 다른 `Stage Space` 좌표를 넣으면 Unity 월드에서 동일한 스케일과 축 기준으로 배치된다.
- Unity 씬 뷰에서 원점과 축 방향을 시각적으로 확인할 수 있다.
