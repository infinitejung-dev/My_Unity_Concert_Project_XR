# Checklist 11. SUN_PrototypeDebugOverlay 검증

## 구현 확인
- [x] 파일명이 `SUN_PrototypeDebugOverlay.cs`이고 public 클래스명과 일치한다.
- [x] 클래스명이 `SUN_` prefix를 사용한다.
- [x] `MonoBehaviour`를 상속한다.
- [x] `SetVisible`, `SetStatus`, `SetWarning`, `ClearWarning`, `RegisterMissingReference` API가 존재한다.

## 표시 항목 검증
- [x] `Stage Space` 원점과 X/Y/Z 축이 표시된다.
- [x] 관객별 몸 위치와 눈 위치가 표시된다.
- [x] 관객별 시선 방향이 표시된다.
- [x] 중앙 마커 캘리브레이션 상태가 표시된다.
- [x] 마지막 유효 보정 시간이 표시된다.
- [x] 이벤트 ID, ObjectId, 타임라인 진행 시간, 진행률이 표시된다.
- [x] 오브젝트 `Stage Space` 좌표와 Unity 월드 좌표가 표시된다.

## 경고 검증
- [x] 미정렬 상태가 표시된다.
- [x] 추적 손실 상태가 표시된다.
- [x] 누락된 참조 목록이 표시된다.
- [ ] 시야 밖 상태가 표시된다.
- [ ] 이벤트 데이터 오류가 표시된다.
- [x] 상태명이 `Ready`, `Uncalibrated`, `Calibrating`, `TrackingLost`, `Playing`, `Paused`, `Reset` 중 하나로 표현될 수 있다.

## 완료 판정
- [x] 디버그 표시를 꺼도 핵심 카메라/오브젝트/타임라인 흐름은 정상 동작한다.
- [ ] 개발자가 Unity 에디터에서 한 Task씩 구현 결과를 확인할 수 있다.

## 진행 메모
- 화면 텍스트와 Gizmo 기반 관찰 기능은 구현했다.
- 시야 밖 판정과 이벤트 데이터 오류의 오버레이 직접 표시는 아직 수동/추가 구현 검증 항목으로 남겨뒀다.
