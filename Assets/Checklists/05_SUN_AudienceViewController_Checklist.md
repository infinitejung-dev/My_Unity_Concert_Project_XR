# Checklist 05. SUN_AudienceViewController 검증

## 구현 확인
- [x] 파일명이 `SUN_AudienceViewController.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `MonoBehaviour`를 상속한다.
- [x] `_audienceCamera`, `_audienceRig`, `_calibrator`, `_minFovDegrees`, `_maxFovDegrees`가 존재한다.
- [x] `SetLookRotation`, `SetSimulatedHeadYawPitch`, `ApplyAudienceView` API가 존재한다.

## 카메라 위치 검증
- [x] `SUN_AudienceRig`의 눈 위치를 카메라 위치 계산에 사용한다.
- [x] `SUN_StageCoordinateSystem` 변환을 거쳐 Unity 월드 위치가 적용된다.
- [x] 캘리브레이션이 유효할 때 보정값이 카메라 위치/회전에 반영된다.
- [ ] 관객 A/B의 카메라가 서로 다른 위치에 배치된다.

## 시선 및 FOV 검증
- [x] yaw/pitch 입력으로 카메라 회전을 변경할 수 있다.
- [x] 카메라가 특정 오브젝트를 강제 LookAt하지 않는다.
- [x] FOV 기본값 45도를 적용할 수 있다.
- [x] FOV가 최소/최대 범위를 벗어나면 clamp된다.
- [x] 오브젝트가 시야 밖으로 나가도 화면에 억지로 붙이지 않는다.

## 완료 판정
- [ ] 같은 3D 오브젝트가 관객 A/B 카메라에서 서로 다른 각도와 거리감으로 보인다.

## 진행 메모
- 카메라 pose/FOV 적용 로직은 구현했다.
- 관객 A/B 카메라를 실제 씬에 배치하고 동일 오브젝트 원근 차이를 확인하는 검증은 남아 있다.
