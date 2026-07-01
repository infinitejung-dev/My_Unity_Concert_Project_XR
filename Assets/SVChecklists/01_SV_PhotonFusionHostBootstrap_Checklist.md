# Checklist 01. Photon Fusion 호스트 부트스트랩 검증

## 구현 확인
- [ ] Fusion 관련 패키지와 설정 파일 존재 여부를 확인했다.
- [ ] Host 시작용 `NetworkRunner` 진입점이 분리되어 있다.
- [ ] 프로토타입용 세션 시작 코드가 준비되어 있다.
- [ ] 플레이어 수 기본 설정이 3명 기준으로 정리되어 있다.

## 동작 검증
- [ ] 개인 PC에서 Play 또는 빌드 실행 시 Host 세션이 시작된다.
- [ ] 세션 시작 성공 로그와 실패 로그를 구분해 볼 수 있다.
- [ ] 중복 Runner 생성이나 씬 전환 충돌 없이 세션이 유지된다.

## 프로토타입 맥락 검증
- [ ] Host가 공연장 월드 기준 권위를 가진다는 구조가 문서/코드에 반영되어 있다.
- [ ] 아직 `StageObject_Halo` 동기화 없이도 다음 접속 Task로 넘어갈 수 있는 상태다.

## 완료 판정
- [ ] 개인 PC를 프로토타입 Host로 재사용할 수 있다.

## 진행 메모
- [x] 테스트용 씬 후보로 `Assets/Scenes/SampleScene.unity`가 빌드 설정에 등록되어 있다.
- [x] `Assets/Photon/Fusion/Resources/NetworkProjectConfig.fusion` 파일이 존재한다.
- [x] 현재 `NetworkProjectConfig`의 `PlayerCount`는 `10`으로 설정되어 있어, 프로토타입 기준 `Client 3명` 정책으로 후속 조정이 필요하다.
- [x] 현재 씬에는 기존 좌표계 오브젝트 `Sun_StageRoot`, `CentralMarker`, 기존 `NetworkRunner`가 이미 있다.
- [ ] Unity Editor Console 기준 컴파일 오류 없음은 수동 확인이 필요하다.
- [ ] `AR Foundation` 및 모바일 Build Support 설치 여부는 수동 확인이 필요하다.
