using UnityEngine;

/// <summary>
/// 공통 타임라인에서 평가할 하나의 Stage Space 연출 이벤트 데이터이다.
/// </summary>
[System.Serializable]
public class SUN_StageEventDefinition
{
    [Tooltip("타임라인 안에서 이벤트를 식별하기 위한 ID입니다.")]
    public string EventId = "Event_01";

    [Tooltip("이 이벤트가 제어할 AR 오브젝트 ID입니다.")]
    public string ObjectId = "Object_01";

    [Tooltip("이벤트 시작 시간입니다. 단위는 초입니다.")]
    public float StartTimeSeconds;

    [Tooltip("이벤트 지속 시간입니다. 0 이하는 즉시 완료 이벤트로 해석합니다. 단위는 초입니다.")]
    public float DurationSeconds = 1.0f;

    [Tooltip("이벤트 시작 위치입니다. Stage Space 미터 좌표입니다.")]
    public Vector3 StartPositionMeters = Vector3.zero;

    [Tooltip("이벤트 종료 위치입니다. Stage Space 미터 좌표입니다.")]
    public Vector3 EndPositionMeters = new Vector3(0.0f, 0.0f, 3.0f);

    [Tooltip("이벤트 시작 회전입니다. Euler degree 값입니다.")]
    public Vector3 StartEulerDegrees = Vector3.zero;

    [Tooltip("이벤트 종료 회전입니다. Euler degree 값입니다.")]
    public Vector3 EndEulerDegrees = Vector3.zero;

    [Tooltip("이벤트 시작 스케일입니다.")]
    public Vector3 StartScale = Vector3.one;

    [Tooltip("이벤트 종료 스케일입니다.")]
    public Vector3 EndScale = Vector3.one;

    [Tooltip("이벤트 평가 결과에서 Renderer를 표시할지 결정합니다.")]
    public bool IsVisible = true;
}
