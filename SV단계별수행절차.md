# SV 단계별 수행 절차

## 목적
이 문서는 `Assets/SVTasks`의 01~06 Task를 기준으로, Unity에서 Photon Fusion 개인 PC Host와 클라이언트 3명, `StageObject_Halo` 1개, 모바일 AR 표시까지 순차적으로 구현하고 검증하는 절차를 정리한다.  
핵심은 공연장 전체를 하나의 기준 좌표계로 보고, Host가 동일한 월드 상태를 배포하며, 각 관객 디바이스는 자신의 카메라 위치와 시선에 따라 같은 오브젝트를 다른 각도로 보게 만드는 프로토타입 경험을 빠르게 검증하는 것이다.

## 1. 사전 확인
1. Unity 프로젝트를 열고 Console 창에 컴파일 에러가 없는지 먼저 확인한다.
2. `Assets/SVTasks/README.md`, `Assets/SVTasks/01~06`, `Assets/SVChecklists/01~06`을 열어 두고 이번 작업 순서를 다시 확인한다.
3. 루트의 `단계별수행절차.md`를 함께 열어 문서 톤과 검증 방식 기준을 맞춘다.
4. 테스트용 씬을 하나 정해 작업 시작 지점으로 고정한다.
5. Hierarchy에 공연장 기준 루트를 만들고 이름을 `SUN_SV_StageRoot`로 지정한다.
6. `SUN_SV_StageRoot`의 Transform을 Position `(0, 0, 0)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`로 맞춘다.
7. `SUN_SV_StageRoot` 하위에 빈 오브젝트 `SUN_SV_StageOrigin`을 만들고 공연장 원점으로 사용한다.
8. `SUN_SV_StageOrigin` 하위에 Cube 또는 Empty + Gizmo용 메쉬를 두어 Scene 뷰에서 기준점을 눈으로 확인할 수 있게 한다.
9. Photon Fusion 패키지와 `NetworkProjectConfig`가 프로젝트에 들어 있는지 확인한다.
10. 모바일 AR 검증을 할 예정이면 `AR Foundation`, 플랫폼용 XR 패키지, 모바일 Build Support가 설치되어 있는지 확인한다.
11. 이번 문서는 한 번에 하나의 Task만 구현하는 흐름을 전제로 하므로, 각 단계가 끝날 때마다 바로 Play 또는 디바이스 테스트를 수행한다.

## 2. Host 부트스트랩 구성
1. 현재 테스트 씬에 빈 오브젝트 `SUN_SV_HostBootstrap`을 만든다.
2. `SUN_SV_HostBootstrap`에 Host 시작을 담당할 스크립트를 붙인다.
   예시 역할: `NetworkRunner` 생성, GameMode를 `Host`로 시작, 세션명 고정, 시작 성공/실패 로그 출력.
3. `SUN_SV_HostBootstrap` 오브젝트에 `NetworkRunner` 컴포넌트를 붙이거나, Play 시작 시 스크립트가 자동 생성하도록 한 가지 방식으로 통일한다.
4. Fusion 설정에서 이번 프로토타입용 세션명을 정한다.
   권장 값: `SV_Prototype_Room`
5. 최대 참여 기준을 `Host 1 + Client 3`으로 이해하기 쉬운 상수나 Inspector 값으로 정리한다.
6. Host 시작 스크립트의 Inspector에서 아래 값을 확인한다.
   - Session Name: `SV_Prototype_Room`
   - Game Mode: `Host`
   - Auto Start On Play: `true`
   - Max Remote Clients: `3`
7. 씬에 중복 `NetworkRunner`가 생기지 않도록, 기존 Runner가 있으면 재사용하거나 새 생성 전 검사하도록 정리한다.
8. Play를 눌러 Host가 정상적으로 시작되는지 확인한다.
9. Console에서 세션 시작 성공 로그와 실패 로그가 구분되어 보이는지 확인한다.
10. Play를 멈췄다가 다시 실행해도 Runner 중복 에러 없이 Host가 재시작되는지 확인한다.
11. `Assets/SVChecklists/01_SV_PhotonFusionHostBootstrap_Checklist.md` 기준으로 구현 확인 항목을 직접 점검한다.

## 3. 3클라이언트 세션 검증
1. 씬에 빈 오브젝트 `SUN_SV_ClientBootstrap`을 만든다.
2. `SUN_SV_ClientBootstrap`에 Client 접속용 스크립트를 붙인다.
   예시 역할: 같은 세션명으로 접속, 현재 접속 상태 표시, 실패 사유 출력.
3. Host와 Client 시작 경로를 분리한다.
   권장 방식: Editor는 Host, PC 빌드와 모바일 빌드는 Client로 구동하거나, Inspector 토글로 역할을 바꾸는 방식.
4. 접속 상태를 볼 수 있도록 Canvas를 만들고 이름을 `SUN_SV_SessionCanvas`로 지정한다.
5. `SUN_SV_SessionCanvas` 아래에 Text 또는 TMP 오브젝트 3개를 만든다.
   권장 이름: `RoleText`, `RoomText`, `ConnectionText`
6. 세션 상태 표시 스크립트를 Canvas 또는 별도 오브젝트에 붙이고, 각 Text 참조를 Inspector에 연결한다.
7. Editor에서 Host를 Play로 실행한 뒤, PC 빌드 1대 또는 2대를 실행해 같은 세션에 접속한다.
8. 모바일 빌드가 준비되어 있으면 스마트폰에서도 같은 세션명으로 접속한다.
9. 접속이 성공하면 각 기기에서 최소한 아래 정보가 보이게 한다.
   - 내 역할: Host 또는 Client
   - 세션명: `SV_Prototype_Room`
   - 현재 상태: Connecting / Connected / Failed
10. 3대가 모두 접속한 상태를 확인한다.
11. 4번째 Client 접속을 시도해 접속 제한이 명확하게 동작하는지 확인한다.
12. 접속 실패 시 사용자에게 방이 없거나 인원이 초과됐다는 메시지가 구분되어 보이도록 정리한다.
13. 이 단계에서는 관객별 아바타를 만들지 않고, 같은 세션에 안정적으로 붙는지만 검증한다.
14. `Assets/SVChecklists/02_SV_ThreeClientSession_Checklist.md` 기준으로 세션 흐름을 점검한다.

## 4. StageObject_Halo 네트워크 권한화
1. Hierarchy에서 3D 오브젝트를 하나 만들고 이름을 `StageObject_Halo`로 지정한다.
2. 초기 형태는 Sphere 또는 얇은 Cylinder처럼 무대에서 잘 보이는 단순 메쉬를 사용한다.
3. `StageObject_Halo`의 Transform을 공연장 기준 좌표로 먼저 맞춘다.
   권장 시작값: Position `(0, 1.8, 4)`, Rotation `(0, 0, 0)`, Scale `(1, 1, 1)`
4. `StageObject_Halo`에 `NetworkObject`를 붙인다.
5. 이 오브젝트를 Scene Object로 둘지, Host가 런타임에 Spawn할 Prefab으로 둘지 결정한다.
6. 프로토타입 1차 검증은 Scene에 미리 배치한 뒤 Host가 활성 권한만 갖는 방식이 가장 단순하다.
7. Prefab 기반으로 가면 `StageObject_Halo`를 Prefab으로 저장하고, Host Bootstrap 또는 별도 Spawn 스크립트에서 한 번만 생성하도록 연결한다.
8. Host에서만 `StageObject_Halo`를 생성 또는 활성화하도록 조건을 둔다.
9. Client에서 직접 위치를 바꾸거나 활성 상태를 변경하지 못하도록 조작 스크립트를 Host 권한 체크 뒤에만 실행되게 한다.
10. Hierarchy에 빈 오브젝트 `SUN_SV_NetworkObjects`를 만들고 `StageObject_Halo`를 그 하위에 두어 씬 구성을 명확히 한다.
11. Play 후 Host와 Client 모두에서 `StageObject_Halo`가 동일 이름의 네트워크 오브젝트로 보이는지 확인한다.
12. Client에서 오브젝트가 중복 생성되지 않는지 확인한다.
13. 이 단계에서는 움직이지 않아도 괜찮다. 중요한 것은 `StageObject_Halo`가 하나의 권위 오브젝트로 정리되는 것이다.
14. `Assets/SVChecklists/03_SV_StageObjectHaloNetworkAuthority_Checklist.md` 기준으로 권한 구조를 점검한다.

## 5. 위치/회전/크기 동기화 검증
1. `StageObject_Halo`에 위치와 회전 동기화용 컴포넌트를 붙인다.
   권장 후보: `NetworkTransform`
2. `StageObject_Halo`에 크기 동기화용 스크립트를 추가한다.
   이유: Scale은 기본 설정만으로 충분하지 않을 수 있어 별도 `Networked` 값이나 커스텀 동기화가 더 안전할 수 있다.
3. Host에서만 `StageObject_Halo`를 조작하는 테스트용 스크립트를 붙인다.
4. Inspector에서 테스트용 이동 폭과 회전 속도, 스케일 증감 폭을 정한다.
   권장 값:
   - Move Step Meters: `0.25`
   - Rotate Step Degrees: `15`
   - Scale Step: `0.1`
   - Min Scale: `0.5`
   - Max Scale: `2.0`
5. 테스트 입력을 간단히 정한다.
   - `I/K`: Z축 전후 이동
   - `J/L`: X축 좌우 이동
   - `U/O`: Y축 회전
   - `[` / `]`: 균일 스케일 축소/확대
6. Play 후 Host에서만 입력이 먹는지 확인한다.
7. Client에서는 입력이 먹지 않고 수신만 되는지 확인한다.
8. Host에서 `StageObject_Halo`를 이동시키고 각 Client에서 같은 위치로 수렴하는지 확인한다.
9. Host에서 회전을 바꾸고 각 Client가 같은 월드 회전을 받는지 확인한다.
10. Host에서 스케일을 바꾸고 각 Client가 같은 크기로 보이는지 확인한다.
11. 접속 후반에 새로운 Client가 들어와도 최신 위치, 회전, 크기를 바로 받는지 확인한다.
12. 이 단계에서는 관객 로컬 좌표 보정값을 넣지 않는다. 동기화 기준은 공연장 월드 좌표 하나로 유지한다.
13. `Assets/SVChecklists/04_SV_StageObjectHaloTransformSync_Checklist.md` 기준으로 값 일치를 점검한다.

## 6. 모바일 AR 월드 오브젝트 표시
1. 모바일 AR 테스트용 씬 오브젝트 구성을 확인한다.
2. Hierarchy에 `AR Session`과 `XR Origin` 또는 `AR Session Origin`을 만든다.
3. `XR Origin` 하위의 Main Camera를 모바일 AR 카메라로 사용한다.
4. 모바일 카메라 배경이 보이도록 AR Camera Background 관련 설정을 확인한다.
5. 공연장 기준 좌표와 모바일 AR 월드 좌표를 잇는 기준점을 정한다.
6. 가장 단순한 방식은 `SUN_SV_StageOrigin`과 AR Origin의 시작 정렬을 같은 기준점으로 잡는 것이다.
7. 필요하면 기준 마커 또는 수동 보정 버튼을 둬서 공연장 원점과 모바일 원점을 맞춘다.
8. `StageObject_Halo`가 네트워크에서 받은 월드 Transform을 그대로 쓰되, AR 카메라는 자기 포즈만 바꾸도록 구성한다.
9. `StageObject_Halo`를 AR Camera의 자식으로 두지 않는다.
10. `StageObject_Halo`는 공연장 기준 월드의 자식으로 두고, 카메라만 움직이게 유지한다.
11. Android 또는 iOS 빌드를 생성해 최소 1대 스마트폰에서 실행한다.
12. 스마트폰으로 세션에 접속한 뒤 카메라 화면 위에 `StageObject_Halo`가 보이는지 확인한다.
13. 사용자가 좌우로 이동했을 때 오브젝트가 사용자를 따라오지 않고 같은 월드 지점에 남는지 확인한다.
14. 서로 다른 위치의 스마트폰 2대에서 같은 오브젝트를 각기 다른 각도로 보는지 확인한다.
15. 공연장 좌표계 검증 관점에서 중요한 것은 오브젝트가 카메라에 종속되지 않고, 같은 월드 지점에 존재하는 감각이 유지되는지다.
16. `Assets/SVChecklists/05_SV_MobileARWorldObjectView_Checklist.md` 기준으로 AR 표시 상태를 점검한다.

## 7. 통합 리허설 순서
1. Host PC에서 Unity Editor 또는 PC 빌드로 Host를 실행한다.
2. `RoleText`, `RoomText`, `ConnectionText`에서 Host 상태를 먼저 확인한다.
3. `StageObject_Halo`가 씬에 1개만 존재하는지 확인한다.
4. Client 1대를 접속시켜 기본 세션 진입과 오브젝트 수신을 확인한다.
5. Client 2대, Client 3대를 순서대로 접속시킨다.
6. 세 기기가 모두 같은 세션명과 Connected 상태를 보여주는지 확인한다.
7. Host에서 `StageObject_Halo`를 이동, 회전, 스케일 변경한다.
8. 세 Client에서 동일한 월드 상태로 반영되는지 확인한다.
9. 모바일 Client에서는 카메라를 움직이며 월드 고정 감각이 유지되는지 확인한다.
10. 관객 A 위치, 관객 B 위치처럼 서로 다른 자리에서 동일 오브젝트를 보는 상황을 직접 만들어 각도 차이를 확인한다.
11. Client 1대를 중간에 종료하고 다시 접속시켜 최신 상태 복구 여부를 확인한다.
12. 마지막으로 4번째 Client 접속을 시도해 인원 제한 동작도 함께 확인한다.
13. 리허설 중 문제 발생 시 아래 순서로 원인을 좁힌다.
    - 세션 문제인지 확인
    - `StageObject_Halo` 권한 문제인지 확인
    - Transform 동기화 문제인지 확인
    - AR 카메라 보정 문제인지 확인
14. 리허설 종료 후 `Assets/SVChecklists/06_SV_PrototypeRehearsalValidation_Checklist.md`를 기준으로 수동 판정을 기록한다.

## 8. 이번 프로토타입에서 코드로 지원되는 항목
1. Host와 Client의 시작 경로 분리
2. Host PC 1대 기준의 Fusion 세션 시작
3. Client 3명까지의 동일 세션 접속
4. `StageObject_Halo` 1개에 대한 Host 권위 네트워크 오브젝트 구조
5. `StageObject_Halo`의 위치, 회전, 크기 동기화
6. 세션 상태와 역할을 보여주는 최소 HUD 또는 로그
7. 모바일 AR 카메라에서 같은 월드 오브젝트를 보는 기본 표시 흐름
8. 늦게 접속한 Client의 최신 상태 수신
9. 리허설 시 반복 검증 가능한 수동 테스트 순서

## 9. Unity/디바이스에서 남는 수동 검증 항목
1. 공연장 조명 조건에서 모바일 AR 추적 안정성이 충분한지 확인
2. Wi-Fi 품질이 나빠졌을 때 위치, 회전, 크기 동기화 오차가 허용 가능한지 확인
3. 스마트폰 3대 동시 접속 시 발열과 프레임 저하가 심하지 않은지 확인
4. 공연장 원점과 모바일 AR 원점 보정 절차가 작업자 입장에서 과도하게 번거롭지 않은지 확인
5. `StageObject_Halo`의 크기 변화가 실제 공연 시야각에서 자연스럽게 느껴지는지 확인
6. Host 재시작, Client 재접속, 모바일 앱 백그라운드 복귀 상황에서 세션이 어디까지 복구되는지 확인
7. 관객 위치 차이가 커졌을 때도 같은 오브젝트를 보고 있다는 인지가 유지되는지 확인
8. 이번 범위 밖인 다중 오브젝트, 연출 타이밍 시퀀스, 정밀한 앵커 보정은 다음 단계 검증 항목으로 분리한다.
