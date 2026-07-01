# Checklist 03. SUN_AudienceRig 검증

## 구현 확인
- [x] 파일명이 `SUN_AudienceRig.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `MonoBehaviour`를 상속한다.
- [x] Inspector 노출 필드는 `[SerializeField] private`로 선언되어 있다.
- [x] `_profile`, `_cameraRoot`, `_coordinateSystem` 참조가 존재한다.

## 눈 위치 계산 검증
- [x] 눈 위치 계산식이 `seatStagePositionMeters + Vector3.up * eyeHeightMeters + deviceOffsetMeters`로 분리되어 있다.
- [x] 관객 몸 기준 위치와 눈 위치를 각각 확인할 수 있다.
- [x] 프로필이 비어 있으면 기본 위치 `(0, 0, -3)`과 눈 높이 1.6m를 사용한다.
- [x] 눈 높이를 바꾸면 계산된 눈 위치의 Y값이 바뀐다.
- [x] 디바이스 오프셋을 바꾸면 계산된 눈 위치에 반영된다.

## 에디터 검증
- [ ] 관객 A/B Rig에 서로 다른 프로필을 넣으면 서로 다른 눈 위치가 나온다.
- [x] 좌표계 참조가 있을 때 `_cameraRoot`가 Unity 월드 위치로 이동한다.
- [x] 좌표계 참조가 없어도 눈 위치 계산 자체는 확인 가능하다.
- [x] 매 프레임 불필요한 `FindObjectOfType` 또는 문자열 기반 탐색을 사용하지 않는다.

## 완료 판정
- [x] `SUN_AudienceViewController`가 관객 눈 위치와 FOV를 안정적으로 참조할 수 있다.

## 진행 메모
- Rig 계산, 기본값 fallback, 카메라 루트 반영 API 구현은 완료했다.
- 실제 관객 A/B GameObject에 프로필을 넣고 눈 위치 차이를 보는 검증은 Unity 에디터에서 필요하다.
