# Task 05. SUN_AudienceViewController 구현

## 목적
관객 눈 위치와 시선 방향을 기반으로 관객 카메라의 위치, 회전, FOV를 렌더링 직전에 확정한다.

## 구현 대상
- 클래스명: `SUN_AudienceViewController`
- 파일명: `SUN_AudienceViewController.cs`
- 부모 클래스: `MonoBehaviour`
- 권장 경로: `Assets/Scripts/Audience/SUN_AudienceViewController.cs`

## 구현 범위
- `SUN_AudienceRig`에서 `Stage Space` 눈 위치를 가져온다.
- `SUN_StageCoordinateSystem`과 `SUN_CentralMarkerCalibrator`를 통해 카메라 월드 위치를 계산한다.
- 관객 시선 회전 입력을 카메라 회전에 반영한다.
- FOV는 기본 45도에서 시작하고 Inspector의 최소/최대값으로 clamp한다.
- 카메라가 오브젝트를 강제로 바라보게 하지 않는다.

## 필수 Inspector 필드
- `Camera _audienceCamera`
- `SUN_AudienceRig _audienceRig`
- `SUN_CentralMarkerCalibrator _calibrator`
- `float _minFovDegrees`
- `float _maxFovDegrees`

## public API
- `void SetLookRotation(Quaternion lookRotation)`
- `void SetSimulatedHeadYawPitch(float yawDegrees, float pitchDegrees)`
- `void ApplyAudienceView()`

## 생명주기 기준
- `Awake`: Camera 참조를 캐시한다.
- `Start`: Rig, Calibrator, FOV 범위를 검사한다.
- `Update`: 마우스/키보드 또는 Inspector 기반 시선 입력값을 갱신한다.
- `LateUpdate`: 카메라 Transform과 FOV를 최종 반영한다.

## 구현 메모
- 관객 로컬 회전과 `Stage Space` 회전을 구분한다.
- 캘리브레이션이 유효하지 않으면 마지막 유효 카메라 위치 유지 또는 기본 테스트 위치 전환 정책을 명시한다.
- 카메라 참조가 없으면 해당 View Controller만 비활성화하고 다른 관객 시뮬레이션은 계속되게 한다.

## 완료 기준
- 관객 A/B의 카메라 위치가 서로 다른 눈 위치로 배치된다.
- yaw/pitch 입력 변경 시 카메라 회전이 바뀐다.
- FOV 값이 최소/최대 범위를 벗어나면 clamp된다.
- 오브젝트가 화면 중앙에 강제 고정되지 않는다.
