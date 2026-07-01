# Task 02. SUN_AudienceProfile 구현

## 목적
관객별 테스트 입력값을 ScriptableObject로 분리해, 좌석/스탠딩 위치, 눈 높이, 디바이스 착용 오프셋, 스마트 글래스 FOV 가정값을 쉽게 교체할 수 있게 한다.

## 구현 대상
- 클래스명: `SUN_AudienceProfile`
- 파일명: `SUN_AudienceProfile.cs`
- 부모 클래스: `ScriptableObject`
- 권장 경로: `Assets/Scripts/Audience/SUN_AudienceProfile.cs`

## 구현 범위
- 관객 ID와 `Stage Space` 기준 좌석 위치를 저장한다.
- 관객 눈 위치 계산에 필요한 눈 높이와 디바이스 착용 오프셋을 저장한다.
- 관객 카메라 FOV 기본값을 저장한다.
- ScriptableObject 생성 메뉴를 제공해 관객 A/B/C 테스트 프로필을 만들 수 있게 한다.

## 필수 Inspector 필드
- `string _audienceId`
- `Vector3 _seatStagePositionMeters`
- `float _eyeHeightMeters`
- `Vector3 _deviceOffsetMeters`
- `float _fieldOfViewDegrees`

## public API
- `string AudienceId`
- `Vector3 SeatStagePositionMeters`
- `float EyeHeightMeters`
- `Vector3 DeviceOffsetMeters`
- `float FieldOfViewDegrees`

## 입력 기본값
- 관객 ID: `Audience_A`
- 좌석 위치: `(0, 0, -3)`
- 눈 높이: `1.6`
- 디바이스 오프셋: `(0, 0, 0)`
- FOV: `45`

## 구현 메모
- FOV는 과도한 값이 들어오더라도 이후 View Controller에서 clamp할 수 있게 원본 값을 제공한다.
- 눈 높이와 좌표값은 미터 단위임을 변수명에 유지한다.
- 프로토타입에서는 런타임 수정보다 Inspector 기반 테스트 편의성을 우선한다.

## 완료 기준
- Unity Project 창에서 관객 프로필 에셋을 생성할 수 있다.
- 두 개 이상의 관객 프로필에 서로 다른 좌석 위치와 FOV를 지정할 수 있다.
- `SUN_AudienceRig`가 프로필 데이터를 읽어 눈 위치 계산에 사용할 수 있다.
