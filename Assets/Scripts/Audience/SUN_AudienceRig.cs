using UnityEngine;

/// <summary>
/// 한 관객의 몸 기준 위치와 눈 위치를 Stage Space에서 계산하고 카메라 루트에 반영한다.
/// </summary>
public class SUN_AudienceRig : MonoBehaviour
{
    private const string DefaultAudienceId = "Audience_Default";
    private static readonly Vector3 DefaultSeatStagePositionMeters = new Vector3(0.0f, 0.0f, -3.0f);
    private const float DefaultEyeHeightMeters = 1.6f;
    private const float DefaultFieldOfViewDegrees = 45.0f;

    [Tooltip("관객별 좌석, 눈 높이, 디바이스 오프셋, FOV를 제공하는 ScriptableObject입니다.")]
    [SerializeField] private SUN_AudienceProfile _profile;

    [Tooltip("Unity World에서 관객 카메라의 부모 또는 카메라 자체로 사용할 Transform입니다.")]
    [SerializeField] private Transform _cameraRoot;

    [Tooltip("Stage Space 좌표를 Unity World 좌표로 변환하는 기준 좌표계입니다.")]
    [SerializeField] private SUN_StageCoordinateSystem _coordinateSystem;

    [Tooltip("켜져 있으면 LateUpdate에서 계산된 눈 위치를 카메라 루트에 반영합니다.")]
    [SerializeField] private bool _applyToCameraRoot = true;

    [Tooltip("프로토타입 수동 제어로 더해지는 좌석 위치 오프셋입니다. 단위는 Stage Space 미터입니다.")]
    [SerializeField] private Vector3 _runtimeSeatOffsetMeters = Vector3.zero;

    [Tooltip("프로토타입 수동 제어로 더해지는 FOV 오프셋입니다. 단위는 degree입니다.")]
    [SerializeField] private float _runtimeFieldOfViewOffsetDegrees;

    public string AudienceId => _profile != null ? _profile.AudienceId : DefaultAudienceId;
    public SUN_StageCoordinateSystem CoordinateSystem => _coordinateSystem;
    public Vector3 RuntimeSeatOffsetMeters => _runtimeSeatOffsetMeters;
    public float RuntimeFieldOfViewOffsetDegrees => _runtimeFieldOfViewOffsetDegrees;

    private void Awake()
    {
        if (_cameraRoot == null)
        {
            _cameraRoot = transform;
        }
    }

    private void Start()
    {
        if (_profile == null)
        {
            Debug.LogWarning($"{nameof(SUN_AudienceRig)} on {name} is using the default audience profile values.", this);
        }

        if (_coordinateSystem == null)
        {
            Debug.LogWarning($"{nameof(SUN_AudienceRig)} on {name} can calculate Stage Space positions, but no coordinate system is assigned.", this);
        }
    }

    private void LateUpdate()
    {
        if (_applyToCameraRoot)
        {
            ApplyCameraRootPosition();
        }
    }

    /// <summary>
    /// 관객 몸 기준 위치를 Stage Space 미터 좌표로 반환한다.
    /// </summary>
    public Vector3 GetSeatStagePositionMeters()
    {
        return GetBaseSeatStagePositionMeters() + _runtimeSeatOffsetMeters;
    }

    /// <summary>
    /// 좌석 위치, 눈 높이, 디바이스 오프셋을 합산해 관객 눈 위치를 Stage Space 미터 좌표로 반환한다.
    /// </summary>
    public Vector3 GetEyeStagePositionMeters()
    {
        Vector3 seatStagePositionMeters = GetSeatStagePositionMeters();
        float eyeHeightMeters = _profile != null ? _profile.EyeHeightMeters : DefaultEyeHeightMeters;
        Vector3 deviceOffsetMeters = _profile != null ? _profile.DeviceOffsetMeters : Vector3.zero;

        return seatStagePositionMeters + (Vector3.up * eyeHeightMeters) + deviceOffsetMeters;
    }

    public float GetFieldOfViewDegrees()
    {
        return GetBaseFieldOfViewDegrees() + _runtimeFieldOfViewOffsetDegrees;
    }

    public void SetRuntimeSeatOffsetMeters(Vector3 runtimeSeatOffsetMeters)
    {
        _runtimeSeatOffsetMeters = runtimeSeatOffsetMeters;
    }

    public void MoveRuntimeSeatOffsetMeters(Vector3 deltaStageMeters)
    {
        _runtimeSeatOffsetMeters += deltaStageMeters;
    }

    public void SetRuntimeFieldOfViewDegrees(float fieldOfViewDegrees)
    {
        _runtimeFieldOfViewOffsetDegrees = fieldOfViewDegrees - GetBaseFieldOfViewDegrees();
    }

    public void AdjustRuntimeFieldOfViewDegrees(float deltaDegrees)
    {
        _runtimeFieldOfViewOffsetDegrees += deltaDegrees;
    }

    public void ResetRuntimeOverrides()
    {
        _runtimeSeatOffsetMeters = Vector3.zero;
        _runtimeFieldOfViewOffsetDegrees = 0.0f;
    }

    /// <summary>
    /// Stage Space 눈 위치를 Unity World 위치로 변환해 카메라 루트에 적용한다.
    /// </summary>
    public void ApplyCameraRootPosition()
    {
        if (_cameraRoot == null || _coordinateSystem == null)
        {
            return;
        }

        _cameraRoot.position = _coordinateSystem.StageToWorldPosition(GetEyeStagePositionMeters());
    }

    private Vector3 GetBaseSeatStagePositionMeters()
    {
        return _profile != null ? _profile.SeatStagePositionMeters : DefaultSeatStagePositionMeters;
    }

    private float GetBaseFieldOfViewDegrees()
    {
        return _profile != null ? _profile.FieldOfViewDegrees : DefaultFieldOfViewDegrees;
    }
}
