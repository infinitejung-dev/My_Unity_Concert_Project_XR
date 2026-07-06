# Checklist 03. `StageObject_Halo` 네트워크 오브젝트 권한 검증

## 구현 확인
- [x] `StageObject_Halo`가 네트워크 오브젝트로 식별된다.
- [x] Host만 생성 또는 소유하도록 권한 정책이 정리되어 있다.
- [x] 씬 배치형인지 런타임 Spawn형인지 한 가지 방식으로 고정되어 있다.
- [x] 클라이언트는 수정 권한 없이 수신 전용 구조를 가진다.

## 동작 검증
- [ ] Host 실행 시 `StageObject_Halo`가 네트워크 상에 한 번만 존재한다.
- [ ] 클라이언트 접속 후 동일 오브젝트를 참조한다.
- [ ] 중복 Spawn, 중복 활성화, 참조 유실이 발생하지 않는다.

## 프로토타입 맥락 검증
- [x] `StageObject_Halo` 1개만 동기화 대상이라는 요구가 유지된다.
- [x] 공연장 월드 기준 오브젝트 하나를 관객이 함께 본다는 구조가 유지된다.

## 완료 판정
- [ ] 다음 Transform 동기화 Task를 붙일 수 있는 안정적인 권한 구조가 됐다.

## 진행 메모
- 2026-07-06: `StageObject_Halo`를 Scene Object 방식으로 유지하고, `SUN_SV_NetworkObjects` 하위에 배치했다.
- 2026-07-06: `NetworkObject`와 `SUN_SV_StageObjectHaloAuthority`를 연결해 Host/StateAuthority만 연출 상태를 작성하는 정책을 명시했다.
- 2026-07-06: 클라이언트에서는 Host 전용 작성 컴포넌트를 비활성화해 수신/표시 전용 구조로 정리했다.
- 2026-07-06: 위 동작 검증 항목은 Unity Play 모드에서 Host와 Client를 함께 실행해 확인해야 한다.
