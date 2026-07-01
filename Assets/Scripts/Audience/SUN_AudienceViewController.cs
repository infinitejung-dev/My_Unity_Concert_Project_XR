using UnityEngine;

/// <summary>
/// 관객 눈 위치, 캘리브레이션, 시선 입력, FOV를 렌더링 직전에 관객 카메라에 적용한다.
/// </summary>
public class SUN_AudienceViewController : MonoBehaviour
{
    private const float DefaultMinFovDegrees = 30.0f;
    private const float DefaultMaxFovDegrees = 80.0f;
    private const float DefaultFovDegrees = 45.0f;

    [Tooltip("관객 시점 렌더링에 사용할 Camera입니다.")]
    [SerializeField] private Camera _audienceCamera;

    [Tooltip("관객 눈 위치와 FOV 원본값을 제공하는 Rig입니다.")]
    [SerializeField] private SUN_AudienceRig _audienceRig;

    [Tooltip("중앙 마커 보정값을 카메라 위치와 회전에 적용하는 캘리브레이터입니다.")]
    [SerializeField] private SUN_CentralMarkerCalibrator _calibrator;

    [Tooltip("스마트 글래스 시야각 검증을 위한 최소 FOV입니다. 단위는 degree입니다.")]
    [SerializeField] private float _minFovDegrees = DefaultMinFovDegrees;

    [Tooltip("스마트 글래스 시야각 검증을 위한 최대 FOV입니다. 단위는 degree입니다.")]
    [SerializeField] private float _maxFovDegrees = DefaultMaxFovDegrees;

    [Tooltip("켜져 있으면 Inspector yaw/pitch 값을 매 프레임 시선 회전에 반영합니다.")]
    [SerializeField] private bool _useInspectorHeadInput;

    [Tooltip("프로토타입용 수동 머리 좌우 회전값입니다. 단위는 degree입니다.")]
    [SerializeField] private float _simulatedYawDegrees;

    [Tooltip("프로토타입용 수동 머리 상하 회전값입니다. 단위는 degree입니다.")]
    [SerializeField] private float _simulatedPitchDegrees;

    private Quaternion _lookRotation = Quaternion.identity;
    private bool _hasValidCamera;

    public Quaternion LookRotation => _lookRotation;

    private void Awake()
    {
        if (_audienceCamera == null)
        {
            _audienceCamera = GetComponent<Camera>();
        }

        _hasValidCamera = _audienceCamera != null;
    }

    private void Start()
    {
        ValidateFovRange();

        if (!_hasValidCamera)
        {
            Debug.LogWarning($"{nameof(SUN_AudienceViewController)} on {name} has no Camera and will disable itself.", this);
            enabled = false;
            return;
        }

        if (_audienceRig == null)
        {
            Debug.LogWarning($"{nameof(SUN_AudienceViewController)} on {name} has no audience rig assigned.", this);
        }
    }

    private void Update()
    {
        if (_useInspectorHeadInput)
        {
            SetSimulatedHeadYawPitch(_simulatedYawDegrees, _simulatedPitchDegrees);
        }
    }

    private void LateUpdate()
    {
        ApplyAudienceView();
    }

    public void SetLookRotation(Quaternion lookRotation)
    {
        _lookRotation = lookRotation;
    }

    public void SetSimulatedHeadYawPitch(float yawDegrees, float pitchDegrees)
    {
        _simulatedYawDegrees = yawDegrees;
        _simulatedPitchDegrees = pitchDegrees;
        _lookRotation = Quaternion.Euler(pitchDegrees, yawDegrees, 0.0f);
    }

    /// <summary>
    /// Stage Space 눈 위치와 시선 회전을 보정한 뒤 Unity World 카메라 Transform으로 변환한다.
    /// </summary>
    public void ApplyAudienceView()
    {
        if (!_hasValidCamera || _audienceRig == null)
        {
            return;
        }

        SUN_StageCoordinateSystem coordinateSystem = _audienceRig.CoordinateSystem;
        Vector3 eyeStagePositionMeters = _audienceRig.GetEyeStagePositionMeters();
        Quaternion stageLookRotation = _lookRotation;

        if (_calibrator != null && _calibrator.HasValidCalibration())
        {
            eyeStagePositionMeters = _calibrator.ApplyCalibrationToStagePosition(eyeStagePositionMeters);
            stageLookRotation = _calibrator.ApplyCalibrationToStageRotation(stageLookRotation);
        }

        if (coordinateSystem != null)
        {
            _audienceCamera.transform.position = coordinateSystem.StageToWorldPosition(eyeStagePositionMeters);
            _audienceCamera.transform.rotation = coordinateSystem.StageToWorldRotation(stageLookRotation);
        }
        else
        {
            _audienceCamera.transform.position = eyeStagePositionMeters;
            _audienceCamera.transform.rotation = stageLookRotation;
        }

        float requestedFovDegrees = _audienceRig.GetFieldOfViewDegrees();
        if (requestedFovDegrees <= 0.0f)
        {
            requestedFovDegrees = DefaultFovDegrees;
        }

        _audienceCamera.fieldOfView = Mathf.Clamp(requestedFovDegrees, _minFovDegrees, _maxFovDegrees);
    }

    private void ValidateFovRange()
    {
        if (_minFovDegrees <= 0.0f)
        {
            _minFovDegrees = DefaultMinFovDegrees;
        }

        if (_maxFovDegrees < _minFovDegrees)
        {
            _maxFovDegrees = _minFovDegrees;
        }
    }
}
