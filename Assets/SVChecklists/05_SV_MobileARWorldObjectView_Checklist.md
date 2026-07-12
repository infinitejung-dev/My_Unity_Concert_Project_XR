# Checklist 05. 모바일 AR 카메라 월드 오브젝트 표시 검증

## 구현 확인
- [ ] 스마트폰 카메라 기반 AR 뷰가 동작한다.  
  - 진행 상태: ARCore Loader/URP 배경 구성에 더해, 네이티브 ARCore camera가 준비되기 전에 `ARCameraManager`가 켜지는 우회 경로를 제거했다. 카메라 프레임 timeout은 `ARCameraManager` 활성화 시점부터 계산하며, Android 재빌드 후 실기 카메라 피드 확인이 필요하다.
- [x] Android 빌드 타깃에 ARCore Loader가 등록되어 있다.
  - 진행 상태: Android Providers에 `Assets/XR/Loaders/ARCoreLoader.asset`을 연결하고 자동 Loading/Running을 켰다.
- [x] Android ARCore 빌드 검증에 맞는 그래픽 API 우선순위가 적용되어 있다.
  - 진행 상태: 사진 로그에서 ARCore Vulkan hardware buffer 경로(`ArFrame_getHardwareBuffer`, texture buffer handle null)가 반복 실패하는 것을 확인해 Android 그래픽 API를 OpenGLES3 단독으로 재조정했다.
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
- 당시 ARCore Required 상태에서 Vulkan 우선 조합을 사용하기 위해 Android Minimum API Level을 29로 올리고 OpenGLES3를 fallback으로 남겼으나, 이번 hardware buffer 로그 대응에서 OpenGLES3 단독으로 재조정했다.
- `AR Session`에 `ARInputManager`를 추가해 `TrackedPoseDriver`가 스마트폰 AR 포즈 입력을 받을 수 있도록 씬과 구성 메뉴를 보강했다.
- 모바일 URP 렌더러에 `ARCommandBufferSupportRendererFeature`를 추가해 Android ARCore/Vulkan 조합에서도 카메라 배경 렌더링 이벤트가 누락되지 않도록 보강했다.

## 2026-07-07 Android 카메라 기동 보강 메모
- [x] Android 빌드 로그의 `OPENGL NATIVE PLUG-IN ERROR: GL_INVALID_ENUM`은 당시 OpenGLES3 카메라 텍스처/ARCore 배경 렌더링 경로 문제로 보고 `Vulkan -> OpenGLES3` 순서로 전환했으나, 사진의 hardware buffer 로그 후 아래 후속 보강에서 OpenGLES3 단독으로 다시 조정했다.
- [x] Android Multithreaded Rendering을 꺼서 OpenGLES 멀티스레드 렌더링 계열 충돌 가능성을 줄였다.
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

## 2026-07-07 AR 배경 렌더링/Manifest 검증 보강 메모
- [x] `ARCoreSettings.Requirement.Required`의 직렬화 값이 `0`임을 PackageCache에서 확인했다. `Assets/XR/Settings/ARCoreSettings.asset`의 `m_Requirement: 0`은 이미 Required 상태이므로 변경하지 않았다.
- [x] Android 권한 확인 뒤 `AR Session`을 먼저 켜고, 화면 방향/크기 안정화 프레임을 기다린 뒤 `ARCameraBackground`가 frame 이벤트를 먼저 구독하고 `ARCameraManager`를 켜도록 순서를 보강했다. AR 배경 요청 모드는 현재 `BeforeOpaques`로 고정해 단순한 배경 렌더링 경로를 검증한다.
- [x] `ARCameraManager.frameReceived`만으로 성공 처리하지 않고, 카메라 텍스처 수, `currentRenderingMode != None`, 배경 Material 존재 여부를 모두 만족하는 "렌더 가능한 AR 카메라 배경 프레임"을 첫 성공 기준으로 바꿨다.
- [x] Android 런타임 로그에 ARCore Loader 활성 여부, 카메라/배경 컴포넌트 enable 상태, `ARCameraBackground.backgroundRenderingEnabled`, 배경 Material, Camera clear flags, 활성 Camera 목록을 추가해 검정 솔리드 배경 원인을 분리할 수 있게 했다.
- [x] `SUN_SV_AndroidARBuildValidator`를 추가해 Android 빌드 전 ARCore Loader/Required/Graphics API/minSdk/Multithreaded Rendering/URP AR Renderer Feature를 검증하고, Gradle 프로젝트 생성 후 실제 `AndroidManifest.xml`의 `CAMERA` 권한, `android.hardware.camera.ar`, `com.google.ar.core=required`를 확인하도록 했다.
- [x] `SampleScene.unity`의 AR `Main Camera`에 연결된 `ARCameraManager` 요청 모드를 `BeforeOpaques`로 직렬화하고, 새 렌더 가능 프레임 진단 필드를 `SUN_SV_MobileARStageAlignment`에 반영했다.
- [x] Unity batchmode 구성 메뉴 실행을 시도했으나, 동일 프로젝트가 Unity Editor에서 이미 열려 있어 `Multiple Unity instances cannot open the same project` 오류로 중단됐다.
- [ ] Android 실기에서 카메라 권한 승인 후 `received first renderable AR camera background frame` 로그가 찍히고, 검정 배경 대신 실제 카메라 feed 위에 `StageObject_Halo`가 보이는지 재검증해야 한다.

## 2026-07-07 AR 시작 Guard 추가 보강 메모
- [x] Android 런타임에서 AR 시작 시점에 active XR loader가 없으면 `XRManagerSettings.InitializeLoader()`를 실행해 ARCore loader/subsystem을 먼저 준비하도록 보강했다.
- [x] `XRSessionSubsystem`과 `XRCameraSubsystem`이 모두 로드되어 있는지 확인한 뒤에만 `AR Session`, `ARCameraManager`, `ARCameraBackground`를 켜도록 막았다.
- [x] ARCore availability/install 상태를 AR Session 활성화 전에 확인하고, 지원 불가/설치 필요 실패 상태에서는 검정 화면으로 계속 진행하지 않고 명확한 오류 로그를 남기도록 했다.
- [x] 렌더 가능한 AR 카메라 배경 프레임이 timeout 안에 오지 않으면 AR view 컴포넌트와 `AR Session`을 자동 재시작하도록 보강했다.
- [x] `SUN_SV_MobileARWorldObjectViewSceneConfigurator`와 `SampleScene.unity`에 새 시작 guard 옵션을 반영했다.
- [ ] Android 실기에서 `initialized XR loader before AR Session startup`, `checking ARCore availability before AR Session startup`, `received first renderable AR camera background frame` 로그 흐름을 확인해야 한다.

## 2026-07-07 Android ARCore hardware buffer 검정 배경 후속 보강 메모
- [x] 사진 로그의 `Failed to get the hardware buffer. ArFrame_getHardwareBuffer returned an error: -12.`와 `The texture buffer handle is null.`가 Vulkan 카메라 hardware buffer 경로에서 반복되는 것으로 보고, Android Graphics API를 OpenGLES3 단독으로 변경했다.
- [x] Android 실기 검증 범위를 줄이기 위해 `ProjectSettings.asset`, 구성 메뉴, 빌드 검증기 모두 OpenGLES3 단독 계약으로 맞췄다.
- [x] ARCameraManager의 Image Stabilization 기본 요청을 껐다. 필요 시 `_requestImageStabilization`을 켜더라도 `supportsImageStabilization == Supported`일 때만 요청하도록 런타임 guard를 추가했다.
- [x] AR 카메라 배경 Render Mode를 `Before Opaques`로 명시해 provider 선택값에 숨지 않는 단순한 배경 렌더링 경로로 고정했다.
- [x] 모바일 URP 렌더러의 Native Render Pass를 꺼서 AR background renderer feature/command buffer 경로와의 기기별 충돌 가능성을 줄였고, 빌드 검증기에서도 이를 검사하도록 했다.
- [x] 권한 승인 직후 ARCore cold start를 방해하지 않도록 렌더 가능 프레임 timeout을 12초로 늘리고, 화면 방향/크기 안정화 대기 후에도 실패할 때만 자동 AR Session restart를 2회까지 제한했다.
- [ ] Android 실기에서 `graphics=OpenGLES3`, `imageStabilizationRequested=False`, `renderMode=BeforeOpaques`, `received first renderable AR camera background frame` 로그가 찍히고 검정 배경 대신 카메라 feed가 보이는지 재검증해야 한다.

## 2026-07-07 Android RenderPass 해상도 불일치 / Camera NULL 후속 보강 메모
- [x] 1차 조치: 사진의 Development Console 오류 `RenderPass specifications (1440 x 3120 1AA) vs (3120 x 1440 1AA)`를 Android portrait/landscape surface 불일치 가설로 보고, Android 기본 방향을 Portrait 단일 방향으로 고정했다. 이후 Android 빌드 방향 요구가 Landscape로 확정되어 아래 Landscape AR 카메라 수정 메모로 대체했다.
- [x] AR `Main Camera`와 Mobile RPAsset에서 HDR, MSAA, Render Scale 0.8, Post Processing, URP camera stack, Depth/Opaque texture 요구를 꺼서 AR camera background가 단일 fullscreen render target을 쓰도록 단순화했다.
- [x] `SUN_SV_MobileARStageAlignment`가 AR Session을 먼저 켠 뒤, `Screen.width/height`가 안정화된 프레임을 기다리고 나서 `ARCameraBackground`와 `ARCameraManager`를 켜도록 순서를 늦췄다.
- [x] Android 런타임 로그에 `screen=`, `orientationLock=`, `arCameraRender=hdr/msaa/post/stack` 정보를 추가해 다음 Logcat에서 방향/렌더 경로가 실제 적용됐는지 확인할 수 있게 했다.
- [x] 1차 조치에서는 `SUN_SV_AndroidARBuildValidator`가 Portrait 단일 방향, Mobile RPAsset no HDR/no MSAA/renderScale 1.0, AR Main Camera no post processing/no stack 설정을 Android 빌드 전에 검사하도록 확장했다. 현재 검증기는 LandscapeLeft 단일 방향 기준으로 변경됐다.
- [x] 1차 조치에서는 Unity Editor에 열린 씬/PlayerSettings 메모리 상태가 저장본과 달라도 같은 BuildFailedException이 반복되지 않도록, `SUN_SV_AndroidARBuildValidator`가 Android 빌드 직전에 Portrait only 계약을 정규화했다. 현재 정규화 기준은 LandscapeLeft only 계약이다.
- [x] 기존 Portrait Logcat 재검증 항목은 현재 요구사항과 맞지 않아 아래 `orientationLock=True:LandscapeLeft` 재검증 항목으로 대체했다.

## 2026-07-07 Android Landscape AR 카메라 수정 메모
- [x] 첨부 스크린샷의 Development Console 오류 `SUN_SV_MobileARStageAlignment exhausted AR camera startup restarts after 1 attempt(s)`는 렌더 가능한 AR 카메라 배경 프레임이 timeout 안에 오지 않아 자동 재시작 감시가 종료된 상태로 확인했다.
- [x] 현재 요구사항에 맞춰 이전 Portrait 단일 방향 가설을 대체하고, `ProjectSettings.asset`, `SUN_SV_AndroidARBuildValidator`, `SUN_SV_MobileARWorldObjectViewSceneConfigurator`, `SampleScene.unity`의 Android AR 방향 계약을 `LandscapeLeft` 단일 방향으로 변경했다.
- [x] `SUN_SV_MobileARStageAlignment`가 Android에서 `AR Session`을 켜기 전에도 `Screen.width >= Screen.height`인 Landscape surface 안정화 프레임을 기다리도록 보강했다. 공연장 좌표계와 관객 로컬 시점 정렬은 기존 `XR Origin` 기준을 유지한다.
- [x] 씬의 `_maxArCameraStartupRestartAttempts`를 코드/Configurator 기본값과 같은 2회로 맞춰 빌드에 저장된 값이 1회로 남아 있던 문제를 수정했다.
- [x] `camera_c_api.cc:114 camera was passed NULL` 반복 원인은 `ARSession.state == Ready`를 카메라 활성화 가능 상태로 취급해 `ARCameraManager`가 ARCore native camera frame 생성 전 `TryGetLatestFrame`/texture descriptor 경로를 호출한 것으로 판단했다. `XRSessionSubsystem.running == true`이고 `ARSession.state`가 `SessionInitializing` 또는 `SessionTracking`에 들어온 뒤에만 `ARCameraManager`/`ARCameraBackground`를 켜도록 guard를 보강했다.
- [ ] Android 재빌드 후 Logcat에서 `orientationLock=True:LandscapeLeft`, `screen=<가로>x<세로>@LandscapeLeft`, `graphics=OpenGLES3`, `received first renderable AR camera background frame` 순서가 찍히고 검정 배경 대신 실제 카메라 feed가 보이는지 재검증해야 한다.

## 2026-07-12 AR 카메라 시작 복구 흐름 수정 메모
- [x] 화면 방향/AR Session 준비 대기 시간이 12초 카메라 프레임 timeout에 포함되어, 실제 `ARCameraManager` 활성화 직후 재시작될 수 있던 타이머 기준을 수정했다. 이제 실제 카메라 텍스처 요청이 가능한 시점부터 timeout을 계산한다.
- [x] 당시에는 AR Session 준비 timeout 뒤에도 `ARCameraManager`와 `ARCameraBackground` 초기화를 계속하도록 했으나, NULL camera 호출을 허용하는 흐름으로 확인되어 2026-07-13 수정에서 이 우회 처리를 폐기했다.
- [x] 권한/세션 준비 중에는 AR Manager만 비활성화하고 XR `Main Camera` 자체는 Android 로컬 뷰 렌더 경로에 유지하도록 수정했다.
- [x] 상세 시작 로그를 끈 빌드에서도 카메라 프레임 timeout 및 자동 복구가 동작하도록 진단 로그와 복구 조건을 분리했다.
- [x] 자동 재시작 cooldown을 `WaitForSecondsRealtime`로 변경해 타임 스케일과 무관하게 복구가 진행되도록 수정했다.
- [ ] Android 실기에서 권한 승인 뒤 12초의 실제 카메라 활성 구간 안에 `received first renderable AR camera background frame` 로그가 출력되고 검정 배경이 카메라 feed로 교체되는지 확인해야 한다.

## 2026-07-13 ARCore NULL camera 호출 제거 메모
- [x] AR Foundation 6.4.3의 `ARCameraManager.Update()`가 활성화된 동안 매 프레임 `XRCameraSubsystem.TryGetLatestFrame()`을 호출하는 생명주기를 기준으로 시작 순서를 재검토했다.
- [x] `ARSession.state`가 `SessionInitializing` 또는 `SessionTracking`에 도달하지 못했는데도 timeout 뒤 `ARCameraManager`/`ARCameraBackground`를 강제로 켜던 2026-07-12 우회 처리를 제거했다.
- [x] 세션 준비 timeout 시 AR 카메라 manager는 계속 끈 상태로 유지하고, 네이티브 상태가 불완전한 동안 자동 `Stop/Reset/Start`를 반복하지 않도록 이번 검증의 자동 재시작 기본값과 씬 설정을 껐다.
- [x] `ARCameraBackground` 구독 후 `ARCameraManager`를 켜는 정상 순서는 유지하고, 실제 manager 활성화 뒤부터만 렌더 가능 프레임 timeout을 계산하도록 유지했다.
- [x] 첫 렌더 가능 프레임 전 진단 로그가 `currentFacingDirection`, `currentRenderingMode`, `imageStabilizationEnabled`, background material 등 카메라 provider 상태 getter를 읽지 않도록 지연했다.
- [x] `descriptor.supportsImageStabilization`도 ARCore native 지원 여부 delegate를 호출하므로 같은 첫 프레임 gate 뒤로 이동했다.
- [x] manager 활성 직후 runtime request를 다시 쓰는 호출을 제거했다. 요청값은 manager가 꺼져 있을 때 저장하고 `ARCameraManager.OnBeforeStart()`가 적용하며, Image Stabilization 지원 조회/후속 요청만 첫 실제 프레임 뒤 수행한다.
- [x] readiness는 manager/background를 끈 상태와 동일 AR Session을 유지하면서 10초 단위 최대 3회 확인한다. 30초 뒤에도 준비되지 않을 때만 manager를 켜지 않은 채 명시적으로 종료한다.
- [ ] Android 재빌드 후 Logcat에서 `camera_c_api.cc:114] operator(): camera was passed NULL.`이 더 이상 발생하지 않는지 확인해야 한다.
- [ ] 이어서 `AR Session state changed to SessionInitializing` 또는 `SessionTracking` 이후 `received first renderable AR camera background frame` 로그와 실제 카메라 feed를 확인해야 한다.
