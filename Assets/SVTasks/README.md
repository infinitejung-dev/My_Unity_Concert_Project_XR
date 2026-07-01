# Photon Fusion 서버-동기화 Task 맵

## 목적
`StageObject_Halo` 1개 오브젝트를 기준으로, 개인 PC 호스트와 스마트폰 클라이언트 3명이 같은 공연장 좌표계 오브젝트를 각자 시점에 맞게 보도록 프로토타입 구현 순서를 정리한다.

## Task / Checklist 대응표
| 순서 | Task 파일 | Checklist 파일 | 핵심 범위 |
| --- | --- | --- | --- |
| 01 | `01_SV_PhotonFusionHostBootstrap_Task.md` | `01_SV_PhotonFusionHostBootstrap_Checklist.md` | Photon Fusion 설치 상태 점검, 개인 PC 호스트 부트스트랩 |
| 02 | `02_SV_ThreeClientSession_Task.md` | `02_SV_ThreeClientSession_Checklist.md` | 3클라이언트 동시 접속, 세션 진입 흐름, 최대 접속 수 제어 |
| 03 | `03_SV_StageObjectHaloNetworkAuthority_Task.md` | `03_SV_StageObjectHaloNetworkAuthority_Checklist.md` | `StageObject_Halo` 단일 네트워크 오브젝트화, 권한 구조 정리 |
| 04 | `04_SV_StageObjectHaloTransformSync_Task.md` | `04_SV_StageObjectHaloTransformSync_Checklist.md` | 위치/회전/크기 동기화 구현과 검증 |
| 05 | `05_SV_MobileARWorldObjectView_Task.md` | `05_SV_MobileARWorldObjectView_Checklist.md` | 스마트폰 카메라 기반 월드 오브젝트 표시 |
| 06 | `06_SV_PrototypeRehearsalValidation_Task.md` | `06_SV_PrototypeRehearsalValidation_Checklist.md` | 3클라이언트 통합 리허설, 디버그 HUD, 최종 검증 |

## 운영 원칙
- 한 번에 하나의 Task만 구현한다.
- 각 Task 구현 직후 대응 Checklist를 사용해 Unity Editor 또는 기기에서 검증한다.
- 이번 범위는 프로토타입 검증용이므로 서버 이중화, 계정 체계, 운영 자동화는 제외한다.
- 공연장 기준 월드 오브젝트는 1개만 동기화하고, 관객별 차이는 카메라 시점에서만 발생하도록 유지한다.
