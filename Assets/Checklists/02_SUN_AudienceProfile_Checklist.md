# Checklist 02. SUN_AudienceProfile 검증

## 구현 확인
- [x] 파일명이 `SUN_AudienceProfile.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `ScriptableObject`를 상속한다.
- [x] ScriptableObject 생성 메뉴가 제공된다.
- [x] Inspector 노출 필드는 `[SerializeField] private`로 선언되어 있다.

## 데이터 필드 검증
- [x] `_audienceId`가 존재한다.
- [x] `_seatStagePositionMeters`가 존재하고 미터 단위 의도가 드러난다.
- [x] `_eyeHeightMeters`가 존재하고 기본값 1.6m를 사용할 수 있다.
- [x] `_deviceOffsetMeters`가 존재한다.
- [x] `_fieldOfViewDegrees`가 존재하고 기본값 45도를 사용할 수 있다.

## 에셋 검증
- [ ] 관객 A 프로필을 생성할 수 있다.
- [ ] 관객 B 프로필을 생성할 수 있다.
- [ ] 관객 A/B의 좌석 위치를 서로 다르게 입력할 수 있다.
- [ ] 관객별 눈 높이와 FOV를 다르게 입력할 수 있다.

## 완료 판정
- [x] `SUN_AudienceRig`가 이 프로필을 참조해 관객별 눈 위치와 FOV를 가져올 수 있다.

## 진행 메모
- 코드 구현과 컴파일 확인은 완료했다.
- 실제 관객 A/B ScriptableObject 에셋 생성과 값 입력은 Unity 에디터에서 수동 검증이 필요하다.
