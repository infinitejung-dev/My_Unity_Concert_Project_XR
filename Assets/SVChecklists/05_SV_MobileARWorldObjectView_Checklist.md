# Checklist 05. 모바일 AR 카메라 월드 오브젝트 표시 검증

## 구현 확인
- [ ] 스마트폰 카메라 기반 AR 뷰가 동작한다.  
  - 진행 상태: 씬의 AR Camera 구성은 존재했으나 Android XR Management의 Loader 목록이 비어 있어 AR Foundation이 기기 카메라/포즈 서브시스템을 시작하지 못하는 상태였다. `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`의 Android Providers에 `ARCoreLoader`를 등록했고, 구성 메뉴에서도 ARCore Loader를 첫 번째 Android XR Loader로 보정하도록 수정했다. Android 재빌드 후 실기 카메라 피드 확인이 필요하다.
- [x] Android 빌드 타깃에 ARCore Loader가 등록되어 있다.
  - 진행 상태: Android Providers에 `Assets/XR/Loaders/ARCoreLoader.asset`을 연결하고 자동 Loading/Running을 켰다.
- [x] Android ARCore 빌드 검증에 맞는 그래픽 API 우선순위가 적용되어 있다.
  - 진행 상태: ARCore Required + Android Minimum API Level 29 기준으로 Android 그래픽 API 순서를 Vulkan 우선, OpenGLES3 fallback으로 정리했다.
- [ ] 공연장 기준 보정 지점과 AR 월드 좌표 연결 방식이 정리되어 있다.  
  - 진행 상태: `SUN_SV_StageOrigin`과 `XR Origin` 시작 Transform을 같은 기준점으로 맞추는 방식으로 코드 정리 완료. 씬 반영 후 Inspector 연결 확인 필요.
- [ ] `StageObject_Halo` 네트워크 Transform이 AR 월드에 반영된다.  
  - 진행 상태: `StageObject_Halo`를 AR Camera 자식으로 두지 않고 `SUN_SV_NetworkObjects`/공연장 월드 루트 아래 유지하는 구성 코드 추가. Host/Client Play 검증 필요.
- [ ] 네트워크 동기화 로직과 AR 표시 로직이 분리되어 있다.  
  - 진행 상태: 네트워크 Transform은 기존 Halo 동기화 컴포넌트가 담당하고, AR 원점 정렬/계층 검증은 `SUN_SV_MobileARStageAlignment`가 담당하도록 분리했다.

## 기기 검증
- [ ] 스마트폰 화면에서 카메라 피드 위에 `StageObject_Halo`가 표시된다.
- [ ] 사용자가 움직여도 오브젝트는 월드에 고정된 것처럼 보인다.
- [ ] 서로 다른 관객 위치에서 같은 오브젝트를 다른 각도로 본다.
- [ ] 카메라 추적이 잠시 흔들려도 오브젝트가 완전히 붕괴하지 않는다.

## 프로토타입 맥락 검증
- [ ] 관객마다 다른 오브젝트를 받는 것이 아니라 같은 월드 오브젝트를 본다.
- [ ] 공연장 좌표계와 관객 시점 기반 렌더링 관계가 체험 가능하다.

## 완료 판정
- [ ] 스마트폰 카메라로 월드 오브젝트를 띄워주는 요구가 충족됐다.

## 2026-07-07 구현 메모
- `AR Session`, `XR Origin`, `Camera Offset`, AR용 `Main Camera`, `ARCameraManager`, `ARCameraBackground`, `TrackedPoseDriver`를 자동 구성하는 Editor 메뉴를 추가했다.
- 모바일 URP 렌더러(`Assets/Settings/Mobile_Renderer.asset`)에 `ARBackgroundRendererFeature`가 없으면 자동으로 추가하도록 구성 메뉴를 보강했다.
- AR 카메라는 `XR Origin` 하위에서 디바이스 포즈만 반영하고, `StageObject_Halo`는 카메라 자식이 아닌 공연장 기준 월드 오브젝트로 유지하도록 런타임 검증 스크립트를 추가했다.
- 현재 프로젝트가 Unity Editor에서 열려 있어 batchmode로 `SampleScene.unity`에 자동 적용하지 못했다. 메뉴 실행과 모바일 기기 검증 후 체크박스를 완료 처리한다.
- Android XR Management에 `ARCoreLoader`가 등록되어 있지 않아 Android 빌드에서 AR Session이 실제 스마트폰 카메라 기반 세션으로 올라오지 않는 문제를 확인했다.
- `SUN_SV_MobileARWorldObjectViewSceneConfigurator`가 Android Providers를 보정해 `ARCoreLoader`를 첫 번째 Loader로 유지하도록 수정했다.
- ARCore Required 상태에서 Vulkan 우선 조합을 사용하기 위해 Android Minimum API Level을 29로 올리고, OpenGLES3는 fallback으로만 남겼다.
- `AR Session`에 `ARInputManager`를 추가해 `TrackedPoseDriver`가 스마트폰 AR 포즈 입력을 받을 수 있도록 씬과 구성 메뉴를 보강했다.
- 모바일 URP 렌더러에 `ARCommandBufferSupportRendererFeature`를 추가해 Android ARCore/Vulkan 조합에서도 카메라 배경 렌더링 이벤트가 누락되지 않도록 보강했다.

## 2026-07-07 Android 카메라 기동 보강 메모
- [x] Android 빌드 로그의 `OPENGL NATIVE PLUG-IN ERROR: GL_INVALID_ENUM`은 OpenGLES3 카메라 텍스처/ARCore 배경 렌더링 경로 문제로 보고, Android Graphics API를 `Vulkan -> OpenGLES3` 순서로 전환했다.
- [x] ARCore Required + Vulkan 우선 조합에 맞춰 Android Minimum API Level을 29로 올리고, Android Multithreaded Rendering을 꺼서 OpenGLES 멀티스레드 렌더링 계열 충돌 가능성을 줄였다.
- [x] `SUN_SV_MobileARStageAlignment`가 Android 카메라 권한을 먼저 확인한 뒤 `AR Session`, `ARCameraManager`, `ARCameraBackground`를 켜도록 보강했다.
- [x] 첫 AR 카메라 프레임 수신 전까지 `ARSession.state`, `notTrackingReason`, Android 카메라 권한, 카메라 서브시스템 권한, 배경 렌더링 모드, 실제 Graphics API를 로그로 남기도록 했다.
- [ ] Android 실기에서 카메라 권한 팝업, 첫 카메라 프레임 로그, `StageObject_Halo`의 카메라 배경 위 표시 여부를 재검증해야 한다.

## 2026-07-07 Audience Rig 카메라 게이트 보강 메모
- [x] `Audience_A_Rig`, `Audience_B_Rig`의 가상 Camera가 동시에 렌더링되어 AR `Main Camera` 화면을 덮을 수 있는 구조를 확인했다.
- [x] `SUN_SV_MobileARStageAlignment`가 실행 시 Audience Rig들을 `AudienceId` 기준으로 정렬하고, Fusion `LocalPlayer.PlayerId` 기준으로 로컬 좌석 인덱스를 1개만 선택하도록 보강했다.
- [x] `AudienceProfile_B`의 내부 ID를 `Audience_B`로 수정해 A/B 리그 정렬과 선택 로그가 중복되지 않도록 보정했다.
- [x] Fusion 활성 PlayerId 목록을 우선 사용해 Host/Director 슬롯을 제외한 원격 Client 순서대로 Audience_A, Audience_B, Audience_C... 역할을 배정하도록 보강했다.
- [x] 모바일 AR 경로에서는 선택된 리그를 좌석/시점 역할로만 사용하고, 실제 렌더링은 XR Origin 하위 `Main Camera`/`ARCameraManager`/`ARCameraBackground`만 담당하도록 Audience Rig Camera, AudioListener, `SUN_AudienceViewController`를 비활성화한다.
- [x] 선택된 Audience Rig의 eye stage position을 Unity world position으로 변환해 XR Origin 시작 위치에 반영하도록 보강했다. 공연장 좌표계와 관객 로컬 시점의 변환 지점은 `SUN_SV_MobileARStageAlignment.AlignXrOriginToStageOrigin()`에 명시했다.
- [x] `SUN_SV_MobileARWorldObjectViewSceneConfigurator`가 Audience Rig와 NetworkRunner 참조를 Alignment 컴포넌트에 직렬화하도록 보강했다.
- [ ] Android 실기에서 첫 번째 Client가 Audience_A, 두 번째 Client가 Audience_B 역할 로그를 받는지 확인해야 한다.
- [ ] Android 실기에서 Audience Rig 가상 카메라 viewport가 더 이상 화면을 덮지 않고, 실제 카메라 feed 위에 `StageObject_Halo`가 보이는지 재검증해야 한다.

## 2026-07-07 Editor fallback / Host slot 보강 메모
- [x] Editor와 비Android 런타임에서는 `_useArCameraAsLocalAudienceView=true`여도 선택된 Audience Rig의 Camera, AudioListener, `SUN_AudienceViewController`를 로컬 GameView 렌더링 경로로 사용하도록 보강했다.
- [x] Android Player에서만 AR `Main Camera`, `ARCameraManager`, `ARCameraBackground`, AR Camera AudioListener가 로컬 AR view를 담당하도록 분기했다.
- [x] AR `Main Camera`를 사용하는 Android 런타임에서는 선택된 Audience Rig의 eye stage position을 기준으로 `LateUpdate`에서 `XR Origin` Transform만 재동기화하도록 보강했다. Main Camera child Transform은 AR device pose 담당으로 유지한다.
- [x] AR Main Camera AudioListener와 Audience Rig AudioListener가 동시에 켜지지 않도록 로컬 view 모드별 enable 처리를 분리했다.
- [x] Host가 `PlayerId 1`을 차지하는 Host+remote Client prototype 기준에 맞춰 fallback 기준을 `PlayerId 2 == Audience_A`로 변경하고 Configurator 기본 설정에도 반영했다.
