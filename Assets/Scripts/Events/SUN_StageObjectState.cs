using UnityEngine;

/// <summary>
/// 특정 프레임의 공통 Stage Space 오브젝트 상태를 Presenter로 전달하는 데이터 계약이다.
/// </summary>
[System.Serializable]
public class SUN_StageObjectState
{
    [Tooltip("상태값이 적용될 AR 오브젝트 ID입니다.")]
    public string ObjectId;

    [Tooltip("이 상태를 만든 타임라인 이벤트 ID입니다.")]
    public string EventId;

    [Tooltip("Renderer 표시 여부입니다. false여도 Transform 상태는 유지됩니다.")]
    public bool IsVisible;

    [Tooltip("Stage Space 기준 위치입니다. 단위는 미터입니다.")]
    public Vector3 StagePositionMeters;

    [Tooltip("Stage Space 기준 회전입니다.")]
    public Quaternion StageRotation;

    [Tooltip("Unity Transform에 적용할 로컬 스케일입니다.")]
    public Vector3 Scale;

    [Tooltip("이벤트 진행률입니다. 기본 범위는 0에서 1입니다.")]
    public float NormalizedProgress;

    public SUN_StageObjectState()
    {
        ObjectId = string.Empty;
        EventId = string.Empty;
        IsVisible = false;
        StagePositionMeters = Vector3.zero;
        StageRotation = Quaternion.identity;
        Scale = Vector3.one;
        NormalizedProgress = 0.0f;
    }

    public SUN_StageObjectState(
        string objectId,
        bool isVisible,
        Vector3 stagePositionMeters,
        Quaternion stageRotation,
        Vector3 scale,
        float normalizedProgress)
    {
        ObjectId = objectId;
        EventId = string.Empty;
        IsVisible = isVisible;
        StagePositionMeters = stagePositionMeters;
        StageRotation = stageRotation;
        Scale = scale;
        NormalizedProgress = Mathf.Clamp01(normalizedProgress);
    }

    public static SUN_StageObjectState Hidden(string objectId)
    {
        return new SUN_StageObjectState(
            objectId,
            false,
            Vector3.zero,
            Quaternion.identity,
            Vector3.one,
            0.0f);
    }
}
