# Checklist 01. Photon Fusion Host 부트스트랩 검증

## 구현 확인
- [x] Fusion 관련 패키지와 설정 파일 존재 여부를 확인했다.
- [x] Host 시작용 `NetworkRunner` 진입점이 `SUN_SV_HostBootstrap`으로 분리되어 있다.
- [x] 프로토타입용 세션 시작 코드가 `GameMode.Host` 기준으로 준비되어 있다.
- [x] 플레이어 수 기본 설정이 Host 1명 + 원격 Client 3명 기준으로 정리되어 있다.

## 동작 검증
- [X] 개인 PC에서 Play 또는 빌드 실행 후 Host 세션이 시작된다.
- [X] 세션 시작 성공 로그와 실패 로그를 구분해서 볼 수 있다.
- [X] 중복 Runner 생성이나 세션 전환 충돌 없이 세션이 유지된다.

## 프로토타입 맥락 검증
- [x] Host가 공연장 월드 기준 권위를 가진다는 구조가 코드 주석과 로그에 반영되어 있다.
- [x] 아직 `StageObject_Halo` 동기화 없이 다음 접속 Task로 넘어갈 수 있는 상태다.

## 완료 판정
- [ ] 개인 PC를 프로토타입 Host로 사용할 수 있다.

## 진행 메모
- [x] 테스트용 씬 후보로 `Assets/Scenes/SampleScene.unity`가 빌드 설정에 등록되어 있다.
- [x] `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` 파일이 존재한다.
- [x] `NetworkProjectConfig`의 `PlayerCount`를 `10`에서 `4`로 조정했다. 기준은 Host 1명 + 원격 Client 3명이다.
- [x] 현재 씬의 기존 `NetworkRunner` 오브젝트를 재사용하도록 `SUN_SV_HostBootstrap` 컴포넌트를 연결했다.
- [x] Inspector 기본값을 Session Name `SV_Prototype_Room`, Game Mode `Host`, Auto Start On Play `true`, Max Remote Clients `3`으로 구성했다.
- [x] Host 시작 성공, 실패, 중복 시작 방지 로그를 구분해서 출력하도록 구현했다.
- [X] Unity Editor Console 기준 컴파일 오류 없음은 수동 확인이 필요하다.
- [X] Play 모드에서 Photon Cloud/App 설정과 실제 Host 세션 시작 성공 여부는 수동 확인이 필요하다.
- [ ] `AR Foundation` 및 모바일 Build Support 설치 여부는 이후 AR 단계에서 수동 확인이 필요하다.
