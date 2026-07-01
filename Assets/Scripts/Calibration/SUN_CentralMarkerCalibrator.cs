using UnityEngine;

/// <summary>
/// 중앙 마커를 기준으로 디바이스 로컬 기준과 Stage Space 기준을 정렬하는 프로토타입 캘리브레이터이다.
/// </summary>
public class SUN_CentralMarkerCalibrator : MonoBehaviour
{
    private const float MaximumReasonableMarkerDistanceMeters = 100.0f;

    public enum CalibrationStatus
    {
        Uncalibrated,
        Calibrating,
        Ready,
        TrackingLost
    }

    [Tooltip("현재 캘리브레이션 기준 마커의 위치와 방향을 보여줄 시각 오브젝트입니다.")]
    [SerializeField] private Transform _markerVisual;

    [Tooltip("보정된 Stage Space 값을 Unity World로 표시할 때 사용할 좌표계입니다.")]
    [SerializeField] private SUN_StageCoordinateSystem _coordinateSystem;

    [Tooltip("켜져 있으면 실제 AR SDK 대신 Inspector 입력값으로 보정을 시뮬레이션합니다.")]
    [SerializeField] private bool _useManualCalibration = true;

    [Tooltip("수동 보정에서 중앙 마커가 감지된 위치입니다. 단위는 Stage Space 미터입니다.")]
    [SerializeField] private Vector3 _manualMarkerPositionMeters = Vector3.zero;

    [Tooltip("수동 보정에서 중앙 마커가 감지된 회전입니다. 단위는 Euler degree입니다.")]
    [SerializeField] private Vector3 _manualMarkerEulerDegrees = Vector3.zero;

    private CalibrationStatus _status = CalibrationStatus.Uncalibrated;
    private Vector3 _lastValidMarkerPositionMeters = Vector3.zero;
    private Quaternion _lastValidMarkerRotation = Quaternion.identity;
    private float _lastValidCalibrationTimeSeconds = -1.0f;

    public CalibrationStatus Status => _status;
    public float LastValidCalibrationTimeSeconds => _lastValidCalibrationTimeSeconds;
    public Vector3 LastValidMarkerPositionMeters => _lastValidMarkerPositionMeters;
    public Quaternion LastValidMarkerRotation => _lastValidMarkerRotation;

    private void Awake()
    {
        ResetCalibration();
    }

    private void Start()
    {
        if (_markerVisual == null)
        {
            Debug.LogWarning($"{nameof(SUN_CentralMarkerCalibrator)} has no marker visual assigned.", this);
        }

        if (_coordinateSystem == null)
        {
            Debug.LogWarning($"{nameof(SUN_CentralMarkerCalibrator)} can store calibration values, but no coordinate system is assigned.", this);
        }
    }

    private void LateUpdate()
    {
        UpdateMarkerVisual();
    }

    public void BeginCalibration()
    {
        _status = CalibrationStatus.Calibrating;

        if (_useManualCalibration)
        {
            ApplyMarkerPose(_manualMarkerPositionMeters, Quaternion.Euler(_manualMarkerEulerDegrees));
        }
    }

    public void ApplyMarkerPose(Vector3 markerPositionMeters, Quaternion markerRotation)
    {
        if (markerPositionMeters.magnitude > MaximumReasonableMarkerDistanceMeters)
        {
            Debug.LogWarning($"{nameof(SUN_CentralMarkerCalibrator)} received an unusually distant marker pose: {markerPositionMeters}.", this);
        }

        _lastValidMarkerPositionMeters = markerPositionMeters;
        _lastValidMarkerRotation = markerRotation;
        _lastValidCalibrationTimeSeconds = Time.time;
        _status = CalibrationStatus.Ready;
    }

    public void ResetCalibration()
    {
        _status = CalibrationStatus.Uncalibrated;
        _lastValidMarkerPositionMeters = Vector3.zero;
        _lastValidMarkerRotation = Quaternion.identity;
        _lastValidCalibrationTimeSeconds = -1.0f;
    }

    public void MarkTrackingLost()
    {
        if (HasValidCalibration())
        {
            _status = CalibrationStatus.TrackingLost;
        }
    }

    public bool HasValidCalibration()
    {
        return _status == CalibrationStatus.Ready || _status == CalibrationStatus.TrackingLost;
    }

    /// <summary>
    /// 원본 Stage Space 위치에 마지막 유효 마커 Pose를 적용해 보정된 Stage Space 위치를 만든다.
    /// </summary>
    public Vector3 ApplyCalibrationToStagePosition(Vector3 stagePositionMeters)
    {
        if (!HasValidCalibration())
        {
            return stagePositionMeters;
        }

        return _lastValidMarkerPositionMeters + (_lastValidMarkerRotation * stagePositionMeters);
    }

    /// <summary>
    /// 원본 Stage Space 회전에 마지막 유효 마커 회전을 합성한다.
    /// </summary>
    public Quaternion ApplyCalibrationToStageRotation(Quaternion stageRotation)
    {
        if (!HasValidCalibration())
        {
            return stageRotation;
        }

        return _lastValidMarkerRotation * stageRotation;
    }

    private void UpdateMarkerVisual()
    {
        if (_markerVisual == null || _coordinateSystem == null)
        {
            return;
        }

        _markerVisual.position = _coordinateSystem.StageToWorldPosition(_lastValidMarkerPositionMeters);
        _markerVisual.rotation = _coordinateSystem.StageToWorldRotation(_lastValidMarkerRotation);
    }
}
