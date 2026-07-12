# Checklist 07. ARCameraTest 최소 카메라 피드 검증

## 구현 확인
- [x] `ARCameraTest` 씬에 활성 `AR Session`과 `AR Input Manager`가 있다.
- [x] `XR Origin > Camera Offset > Main Camera` 표준 계층이 있다.
- [x] `Main Camera`에 `AR Camera Manager`, `AR Camera Background`, `Tracked Pose Driver`가 활성 상태로 연결되어 있다.
- [x] Fusion, 네트워크, 타임라인, `SUN_SV_MobileARStageAlignment`를 포함하지 않는다.
- [x] 테스트 3D 오브젝트는 카메라 자식이 아니라 XR Origin의 AR 월드 하위 `(0, 0, 2)m`에 `0.25m` 청록색 URP Unlit Cube로 배치되어 있다.
- [x] 진단 스크립트는 AR Foundation 컴포넌트의 enable/disable 또는 provider 상태를 변경하지 않고 세션 상태와 첫 frame 이벤트만 기록한다.
- [x] `ARCameraTest`가 Build Settings의 첫 번째이자 유일한 활성 씬이며, `SampleScene`은 에셋을 보존한 채 비활성화되어 있다.
- [x] Android 카메라 권한은 별도 `Permission.RequestUserPermission` 없이 ARCore/AR Foundation 표준 흐름에 맡긴다.
- [x] Android Application Entry Point를 휴대폰 ARCore용 `Activity`로 고정한다.
- [x] 빌드 전 검증기가 `GameActivity`로 되돌아간 설정을 `Activity`로 정규화하고 다시 검증한다.
- [x] `Mobile_Renderer`의 두 Renderer Feature가 서로 다른 Local File ID(`...9544`, `...9543`)로 매핑되어 있다.

## 2026-07-13 실기기 로그 원인 확인
- [x] 연결된 `SM-S947N`에서 카메라 권한이 승인된 상태임을 확인했다.
- [x] ARCore가 World Facing Camera 구성을 선택하지만 `SessionInitializing`에 머무르는 것을 확인했다.
- [x] ARCore 네이티브 로그에서 `Failed to register sensor to queue 0` 이후 `camera was passed NULL`이 매 프레임 반복되는 것을 확인했다.
- [x] 당시 앱 진입점이 Android XR/OpenXR용 `UnityPlayerGameActivity`였음을 확인했다.
- [x] 휴대폰 ARCore 테스트 빌드의 진입점을 표준 `UnityPlayerActivity`로 변경했다.

## Android 실기 검증
- [ ] 앱 시작 시 Android 카메라 권한 요청을 승인할 수 있다.
- [ ] Logcat에 `SUN_ARCameraTestDiagnostics received first AR camera frame`이 출력된다.
- [ ] 검정/스카이박스 대신 실제 후면 카메라 영상이 전체 배경에 표시된다.
- [ ] 카메라 영상 위에 청록색 테스트 Cube가 보인다.
- [ ] 스마트폰을 움직이면 Cube는 카메라에 붙지 않고 AR 월드의 같은 위치에 남아 보인다.
- [ ] `camera was passed NULL` 오류가 발생하지 않는다.

## 프로토타입 범위
- 이 씬은 카메라 피드와 AR 월드 렌더링만 분리 검증한다.
- Fusion 연결, 공연장 캘리브레이션, 관객 좌석 배정, 네트워크 오브젝트 동기화는 의도적으로 제외한다.

## 완료 판정
- [ ] Android 실기에서 카메라 피드와 월드 고정 테스트 Cube를 동시에 확인했다.

> 재빌드/재설치 후 실기기 항목은 다시 확인해야 한다. 기존 설치본 로그는 수정 전 `GameActivity` 빌드 결과다.
