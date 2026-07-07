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
- 2026-07-07 Android 실기 검정 배경 로그 대응으로 모바일 AR 경로는 Audience Rig 카메라가 아니라 XR Origin 하위 AR `Main Camera`만 실제 렌더링을 담당하도록 유지하고, Audience Rig는 관객 좌석/시점 기준으로만 사용한다.
- 2026-07-07 후속 사진 로그 대응 1차: `camera_c_api.cc:114 camera was passed NULL` 반복과 Development Console의 RenderPass 해상도 불일치(`1440 x 3120` vs `3120 x 1440`)를 막기 위해 Android AR 검증 경로를 Portrait 단일 방향, OpenGLES3, no HDR/MSAA/post processing, 빈 URP camera stack, 화면 크기 안정화 후 ARCameraManager enable 순서로 고정했다. 이후 Android 빌드 방향 요구가 Landscape로 확정되어 아래 조치로 대체했다.
- 2026-07-07 Android Landscape 추가 수정: 모바일 AR 경로의 실제 렌더링 담당 카메라는 계속 XR Origin 하위 AR `Main Camera`로 유지하되, Android PlayerSettings/런타임 방향/빌드 검증/씬 직렬화값을 `LandscapeLeft` 기준으로 맞췄다. `AR Session` 활성화 전에도 Landscape surface 안정화를 기다리게 해 관객 시점 카메라가 검정 배경에서 멈추는 원인을 줄였다. Android 재빌드 후 `orientationLock=True:LandscapeLeft`와 첫 렌더 가능 AR 카메라 배경 프레임 로그 확인이 필요하다.
