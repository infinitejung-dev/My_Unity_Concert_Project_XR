# Checklist 01. SUN_StageCoordinateSystem 검증

## 구현 확인
- [x] 파일명이 `SUN_StageCoordinateSystem.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `MonoBehaviour`를 상속한다.
- [x] Inspector 노출 필드는 `[SerializeField] private`로 선언되어 있다.
- [x] `_stageOrigin`, `_stageForward`, `_stageRight`, `_metersToUnityScale`가 존재한다.
- [x] `StageToWorldPosition`, `WorldToStagePosition`, `StageToWorldRotation`, `WorldToStageRotation` API가 존재한다.

## 좌표계 검증
- [x] 중앙 마커 중심이 `Stage Space` 원점 `(0, 0, 0)`으로 해석된다.
- [x] `+Y`가 위쪽, `+Z`가 무대 방향, `+X`가 무대를 바라봤을 때 오른쪽으로 표시된다.
- [x] `_metersToUnityScale`이 1일 때 1m가 Unity 월드 1 단위로 변환된다.
- [x] `_metersToUnityScale`이 바뀌면 위치 변환 결과가 같은 비율로 바뀐다.
- [x] `StageToWorldPosition` 후 `WorldToStagePosition`을 적용하면 원래 좌표로 돌아온다.

## 에디터 검증
- [x] 씬 뷰 Gizmo에서 원점과 X/Y/Z 축을 확인할 수 있다.
- [x] 무대 중심 예시 `(0, 0, 5)`가 `+Z` 방향에 배치된다.
- [x] 왼쪽 뒤 관객 예시 `(-2, 0, -4)`가 기대 위치에 배치된다.
- [x] 스케일 값이 0 이하일 때 기본값 1.0으로 보정되거나 경고가 표시된다.

## 완료 판정
- [x] 이후 Task의 관객 Rig와 오브젝트 Presenter가 이 클래스만 통해 좌표 변환을 수행할 준비가 됐다.
