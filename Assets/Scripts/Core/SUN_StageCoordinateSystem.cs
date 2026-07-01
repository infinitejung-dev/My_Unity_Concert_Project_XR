using UnityEngine;

/// <summary>
/// 공연장 중앙 마커를 원점으로 하는 Stage Space와 Unity World Space 사이의 좌표/회전 변환을 담당한다.
/// </summary>
public class SUN_StageCoordinateSystem : MonoBehaviour
{
    private const float MinimumAxisMagnitude = 0.0001f;
    private const float DefaultMetersToUnityScale = 1.0f;
    private const float GizmoAxisLengthMeters = 2.0f;
    private const float GizmoStageForwardLengthMeters = 5.0f;

    [Header("Stage Space Basis")]
    [Tooltip("Stage Space 원점으로 사용할 중앙 마커 Transform입니다.")]
    [SerializeField] private Transform _stageOrigin;

    [Tooltip("중앙 마커에서 무대 중심을 바라보는 Stage Space +Z 방향입니다.")]
    [SerializeField] private Vector3 _stageForward = Vector3.forward;

    [Tooltip("무대를 바라봤을 때 오른쪽을 가리키는 Stage Space +X 방향입니다.")]
    [SerializeField] private Vector3 _stageRight = Vector3.right;

    [Tooltip("Stage Space 1미터를 Unity World 몇 단위로 표현할지 결정하는 스케일입니다.")]
    [SerializeField] private float _metersToUnityScale = DefaultMetersToUnityScale;

    // 보정된 축 기저는 모든 변환 API에서 같은 기준을 공유하기 위해 Awake에서 캐시한다.
    private Vector3 _normalizedStageRight = Vector3.right;
    private Vector3 _normalizedStageUp = Vector3.up;
    private Vector3 _normalizedStageForward = Vector3.forward;
    private Quaternion _stageToWorldRotation = Quaternion.identity;

    private void Awake()
    {
        if (_stageOrigin == null)
        {
            _stageOrigin = transform;
        }

        ValidateMetersToUnityScale();
        RefreshStageBasis();
    }

    private void Start()
    {
        ValidateMetersToUnityScale();
    }

    /// <summary>
    /// Stage Space 미터 좌표를 Unity World 좌표로 변환한다.
    /// </summary>
    public Vector3 StageToWorldPosition(Vector3 stagePositionMeters)
    {
        Vector3 scaledStageOffset = stagePositionMeters * _metersToUnityScale;
        return GetStageOriginPosition() + StageOffsetToWorldOffset(scaledStageOffset);
    }

    /// <summary>
    /// Unity World 좌표를 Stage Space 미터 좌표로 변환한다.
    /// </summary>
    public Vector3 WorldToStagePosition(Vector3 worldPosition)
    {
        Vector3 worldOffset = worldPosition - GetStageOriginPosition();
        Vector3 stageOffsetUnityUnits = WorldOffsetToStageOffset(worldOffset);
        return stageOffsetUnityUnits / _metersToUnityScale;
    }

    /// <summary>
    /// Stage Space 기준 회전을 Unity World 기준 회전으로 변환한다.
    /// </summary>
    public Quaternion StageToWorldRotation(Quaternion stageRotation)
    {
        return _stageToWorldRotation * stageRotation;
    }

    /// <summary>
    /// Unity World 기준 회전을 Stage Space 기준 회전으로 변환한다.
    /// </summary>
    public Quaternion WorldToStageRotation(Quaternion worldRotation)
    {
        return Quaternion.Inverse(_stageToWorldRotation) * worldRotation;
    }

    private void OnValidate()
    {
        ValidateMetersToUnityScale();
        RefreshStageBasis();
    }

    private void OnDrawGizmos()
    {
        ValidateMetersToUnityScale();
        RefreshStageBasis();

        Vector3 originPosition = GetStageOriginPosition();
        float axisLength = GizmoAxisLengthMeters * _metersToUnityScale;
        float stageForwardLength = GizmoStageForwardLengthMeters * _metersToUnityScale;

        Gizmos.color = Color.white;
        Gizmos.DrawSphere(originPosition, 0.08f * _metersToUnityScale);

        DrawGizmoAxis(originPosition, _normalizedStageRight, axisLength, Color.red);
        DrawGizmoAxis(originPosition, _normalizedStageUp, axisLength, Color.green);
        DrawGizmoAxis(originPosition, _normalizedStageForward, axisLength, Color.blue);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(originPosition, originPosition + _normalizedStageForward * stageForwardLength);
    }

    private void RefreshStageBasis()
    {
        Vector3 up = Vector3.up;
        Vector3 rightInput = ProjectAxisOnUpPlane(_stageRight, Vector3.right, up);
        Vector3 forward = Vector3.ProjectOnPlane(_stageForward, up);
        if (forward.sqrMagnitude > MinimumAxisMagnitude)
        {
            forward.Normalize();
        }
        else
        {
            forward = Vector3.Cross(rightInput, up).normalized;
        }

        // Stage Space의 +Y는 공연장 수직 방향으로 고정하고, +X/+Z는 수평면에서 서로 직교하도록 보정한다.
        Vector3 right = Vector3.Cross(up, forward).normalized;
        _normalizedStageUp = up;
        _normalizedStageRight = right;
        _normalizedStageForward = Vector3.Cross(_normalizedStageRight, _normalizedStageUp).normalized;
        _stageToWorldRotation = Quaternion.LookRotation(_normalizedStageForward, _normalizedStageUp);
    }

    private void ValidateMetersToUnityScale()
    {
        if (_metersToUnityScale <= 0.0f)
        {
            Debug.LogWarning($"{nameof(SUN_StageCoordinateSystem)} requires a positive meters-to-Unity scale. Resetting to 1.0.", this);
            _metersToUnityScale = DefaultMetersToUnityScale;
        }
    }

    private Vector3 StageOffsetToWorldOffset(Vector3 scaledStageOffset)
    {
        return (_normalizedStageRight * scaledStageOffset.x)
            + (_normalizedStageUp * scaledStageOffset.y)
            + (_normalizedStageForward * scaledStageOffset.z);
    }

    private Vector3 WorldOffsetToStageOffset(Vector3 worldOffset)
    {
        return new Vector3(
            Vector3.Dot(worldOffset, _normalizedStageRight),
            Vector3.Dot(worldOffset, _normalizedStageUp),
            Vector3.Dot(worldOffset, _normalizedStageForward));
    }

    private Vector3 GetStageOriginPosition()
    {
        return _stageOrigin != null ? _stageOrigin.position : transform.position;
    }

    private static Vector3 ProjectAxisOnUpPlane(Vector3 axis, Vector3 fallbackAxis, Vector3 up)
    {
        Vector3 projectedAxis = Vector3.ProjectOnPlane(axis, up);
        if (projectedAxis.sqrMagnitude > MinimumAxisMagnitude)
        {
            return projectedAxis.normalized;
        }

        Vector3 projectedFallback = Vector3.ProjectOnPlane(fallbackAxis, up);
        if (projectedFallback.sqrMagnitude > MinimumAxisMagnitude)
        {
            return projectedFallback.normalized;
        }

        return Vector3.forward;
    }

    private static void DrawGizmoAxis(Vector3 originPosition, Vector3 axisDirection, float length, Color color)
    {
        Gizmos.color = color;
        Gizmos.DrawLine(originPosition, originPosition + axisDirection * length);
    }
}
