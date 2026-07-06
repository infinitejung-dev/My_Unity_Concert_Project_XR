# Checklist 04. `StageObject_Halo` 위치/회전/크기 동기화 검증

## 구현 확인
- [x] 위치 동기화가 구현되어 있다.
  - `StageObject_Halo`에 Fusion `NetworkTransform`을 붙이는 씬 구성 로직과 씬 참조를 반영했다.
  - Host 테스트 입력은 직접 Transform 쓰기가 아니라 `NetworkTransform.Teleport()`를 통해 Fusion TRSP 상태에 위치를 기록한다.
- [x] 회전 동기화가 구현되어 있다.
  - `NetworkTransform`이 Host 권위 Transform의 위치/회전을 동기화하도록 구성했다.
  - Host 테스트 입력은 `NetworkTransform.Teleport()`를 통해 공연장 월드 Y축 회전을 Fusion TRSP 상태에 기록한다.
- [x] 크기 동기화가 구현되어 있다.
  - `SUN_SV_StageObjectHaloScaleSync`에서 Host 권위 `StageWorldScale` 네트워크 값을 유지하고 클라이언트가 같은 공연장 월드 기준 scale을 적용한다.
- [x] Host만 값 변경 권한을 가진다.
  - `SUN_SV_StageObjectHaloAuthority.CanAuthorStageTransform()` 및 `SUN_SV_StageObjectHaloScaleSync.CanAuthorStageWorldScale` 확인 뒤에만 테스트 조작을 적용한다.
- [x] 테스트용 이동/회전/스케일 입력 컨트롤이 있다.
  - `SUN_SV_StageObjectHaloHostTransformTestDriver` 기본값: Move Step Meters `0.25`, Rotate Step Degrees `15`, Scale Step `0.1`, Min Scale `0.5`, Max Scale `2.0`.
  - 입력: `I/K` Z축 전후, `J/L` X축 좌우, `U/O` Y축 회전, `[`/`]` 균일 스케일 축소/확대.

## 동작 검증
- [X] Host에서 오브젝트를 이동하면 모든 Client에 같은 월드 위치로 반영된다.
- [X] Host에서 오브젝트를 회전하면 모든 Client에 같은 월드 회전으로 반영된다.
- [X] Host에서 오브젝트 크기를 바꾸면 모든 Client에 같은 공연장 월드 기준 크기로 반영된다.
- [X] 늦게 접속한 Client도 최신 위치/회전/크기를 받는다.
- [ ] 접속 중 지연이나 네트워크 지터가 있어도 상태가 최종적으로 수렴한다.

## 프로토타입 맥락 검증
- [x] 동기화 값은 관객 로컬 기준이 아니라 공연장 월드 기준이다.
  - 이번 단계에서는 관객 로컬 좌표 보정값을 넣지 않았다.
  - scale 동기화 코드와 Host 테스트 조작 코드 모두 하나의 공유 공연장 월드 Transform을 기준으로 한다.
- [x] 오브젝트 권위 값 1개를 관객별 카메라가 다르게 보는 구조를 유지한다.
  - `StageObject_Halo`는 단일 `NetworkObject`/Host 권위 오브젝트로 유지하고, 관객별 시점 보정은 다음 AR 표시 단계로 분리한다.

## 진행 메모
- 2026-07-07 수정: `StageObject_Halo`의 `NetworkTransform`이 stale script fileID로 저장되어 Fusion `NetworkedBehaviours`에 bake되지 않던 문제를 수정했다. 씬 YAML을 정상 `NetworkTransform` fileID/필드 구성으로 맞추고, 구성 메뉴에서 `SyncScale` 필드가 없는 invalid `NetworkTransform`을 제거 후 재생성하도록 보강했다.
- 2026-07-07 수정: Host에서 위치/회전 키 입력이 Transform에만 직접 적용되어 Fusion `NetworkTransform` 렌더 단계에서 이전 네트워크 pose로 되돌아갈 수 있던 문제를 수정했다. `SUN_SV_StageObjectHaloHostTransformTestDriver`가 위치/회전 변경을 `NetworkTransform.Teleport()` 경유로 기록하고, 구성 메뉴가 테스트 드라이버의 `NetworkTransform` 참조를 채우도록 보강했다.
- Unity batchmode 검증은 프로젝트가 이미 다른 Unity 인스턴스에서 열려 있어 중단되었다.
- `dotnet build PersonalTestProject.slnx`는 통과했다. 단, 기존 `System.IO.Compression` 버전 충돌 경고 1건은 남아 있다.
- 위 동작 검증 항목은 실제 Unity Play 및 Host + 다중 Client 수동 검증이 필요하므로 아직 체크하지 않는다.
- Unity에서 프로젝트를 다시 import한 뒤 `SUN/SV/Configure StageObject_Halo Network Authority` 메뉴를 실행하면 씬 구성과 Fusion scene object bake를 재확인할 수 있다.

## 완료 판정
- [x] `StageObject_Halo` 단일 오브젝트 동기화 요구의 구현 구성이 준비되었다.
- [X] 실제 Play 모드와 다중 Client에서 값 일치가 확인되었다.
