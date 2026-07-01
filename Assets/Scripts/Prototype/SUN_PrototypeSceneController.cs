using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 프로토타입 씬에서 좌표계, 캘리브레이션, 관객 시점, 타임라인, Presenter, 디버그 표시를 연결하는 허브이다.
/// </summary>
public class SUN_PrototypeSceneController : MonoBehaviour
{
    private const float MinimumRuntimeFovDegrees = 20.0f;
    private const float MaximumRuntimeFovDegrees = 100.0f;

    [Tooltip("공연장 전체 Stage Space를 Unity World로 변환하는 좌표계입니다.")]
    [SerializeField] private SUN_StageCoordinateSystem _coordinateSystem;

    [Tooltip("중앙 마커 기준 보정을 담당하는 캘리브레이터입니다.")]
    [SerializeField] private SUN_CentralMarkerCalibrator _calibrator;

    [Tooltip("모든 관객이 공유하는 공통 이벤트 타임라인입니다.")]
    [SerializeField] private SUN_StageEventTimeline _timeline;

    [Tooltip("테스트할 관객 Rig 목록입니다.")]
    [SerializeField] private SUN_AudienceRig[] _audienceRigs = new SUN_AudienceRig[0];

    [Tooltip("관객별 카메라 시점을 제어할 View Controller 목록입니다.")]
    [SerializeField] private SUN_AudienceViewController[] _viewControllers = new SUN_AudienceViewController[0];

    [Tooltip("공통 타임라인 상태를 실제 3D 오브젝트에 적용할 Presenter 목록입니다.")]
    [SerializeField] private SUN_ARObjectPresenter[] _objectPresenters = new SUN_ARObjectPresenter[0];

    [Tooltip("프로토타입 상태와 경고를 표시할 디버그 오버레이입니다.")]
    [SerializeField] private SUN_PrototypeDebugOverlay _debugOverlay;

    [Tooltip("켜져 있으면 키보드로 Play/Pause/Reset/Calibrate를 제어합니다.")]
    [SerializeField] private bool _enableKeyboardShortcuts = true;

    [Tooltip("W/A/S/D 입력으로 선택 관객 좌석을 이동하는 속도입니다. 단위는 Stage Space m/s입니다.")]
    [SerializeField] private float _audienceMoveMetersPerSecond = 1.0f;

    [Tooltip("Q/E/Z/X 입력으로 선택 관객 시선을 회전하는 속도입니다. 단위는 degree/s입니다.")]
    [SerializeField] private float _headRotationDegreesPerSecond = 60.0f;

    [Tooltip("-/= 입력으로 선택 관객 FOV를 조정하는 단위입니다. 단위는 degree입니다.")]
    [SerializeField] private float _fieldOfViewStepDegrees = 5.0f;

    private bool _isDebugVisible = true;
    private int _selectedAudienceIndex;
    private float[] _runtimeYawDegreesByAudience = new float[0];
    private float[] _runtimePitchDegreesByAudience = new float[0];

    private void Awake()
    {
        if (_audienceRigs == null)
        {
            _audienceRigs = new SUN_AudienceRig[0];
        }

        if (_viewControllers == null)
        {
            _viewControllers = new SUN_AudienceViewController[0];
        }

        if (_objectPresenters == null)
        {
            _objectPresenters = new SUN_ARObjectPresenter[0];
        }

        EnsureAudienceControlArrays();
    }

    private void Start()
    {
        RegisterMissingReferences();

        if (_debugOverlay != null)
        {
            _debugOverlay.SetPrototypeReferences(
                _coordinateSystem,
                _calibrator,
                _timeline,
                _audienceRigs,
                _viewControllers,
                _objectPresenters);
            _debugOverlay.SetStatus("Ready");
        }
    }

    private void Update()
    {
        if (!_enableKeyboardShortcuts)
        {
            return;
        }

        HandleKeyboardShortcuts();
        HandleSelectedAudienceInput();
    }

    private void LateUpdate()
    {
        if (_debugOverlay == null)
        {
            return;
        }

        if (_calibrator != null && !_calibrator.HasValidCalibration())
        {
            _debugOverlay.SetWarning("Uncalibrated");
        }
        else
        {
            _debugOverlay.ClearWarning();
        }

        if (_timeline != null && _timeline.IsPlaying)
        {
            _debugOverlay.SetStatus("Playing");
        }
        else if (_timeline != null)
        {
            _debugOverlay.SetStatus("Paused");
        }
    }

    public void PlayTimeline()
    {
        if (_timeline == null)
        {
            return;
        }

        _timeline.Play();
        SetOverlayStatus("Playing");
    }

    public void PauseTimeline()
    {
        if (_timeline == null)
        {
            return;
        }

        _timeline.Pause();
        SetOverlayStatus("Paused");
    }

    public void ResetPrototype()
    {
        if (_timeline != null)
        {
            _timeline.ResetTimeline();
        }

        if (_calibrator != null)
        {
            _calibrator.ResetCalibration();
        }

        ResetAudienceRuntimeControls();
        SetOverlayStatus("Reset");
    }

    public void RescanMarker()
    {
        ResetCalibration();
        CalibrateMarker();
    }

    public void CalibrateMarker()
    {
        if (_calibrator == null)
        {
            return;
        }

        _calibrator.BeginCalibration();
        SetOverlayStatus(_calibrator.HasValidCalibration() ? "Ready" : "Calibrating");
    }

    public void ResetCalibration()
    {
        if (_calibrator == null)
        {
            return;
        }

        _calibrator.ResetCalibration();
        SetOverlayStatus("Uncalibrated");
    }

    public void SetDebugVisible(bool isVisible)
    {
        _isDebugVisible = isVisible;

        if (_debugOverlay != null)
        {
            _debugOverlay.SetVisible(isVisible);
        }
    }

    private void RegisterMissingReferences()
    {
        if (_debugOverlay == null)
        {
            return;
        }

        RegisterMissingReferenceIfNeeded(_coordinateSystem == null, "SUN_StageCoordinateSystem");
        RegisterMissingReferenceIfNeeded(_calibrator == null, "SUN_CentralMarkerCalibrator");
        RegisterMissingReferenceIfNeeded(_timeline == null, "SUN_StageEventTimeline");
        RegisterMissingReferenceIfNeeded(_audienceRigs == null || _audienceRigs.Length == 0, "Audience Rigs");
        RegisterMissingReferenceIfNeeded(_viewControllers == null || _viewControllers.Length == 0, "Audience View Controllers");
        RegisterMissingReferenceIfNeeded(_objectPresenters == null || _objectPresenters.Length == 0, "AR Object Presenters");
    }

    private void RegisterMissingReferenceIfNeeded(bool isMissing, string referenceName)
    {
        if (isMissing && _debugOverlay != null)
        {
            _debugOverlay.RegisterMissingReference(referenceName);
        }
    }

    private void SetOverlayStatus(string status)
    {
        if (_debugOverlay != null)
        {
            _debugOverlay.SetStatus(status);
        }
    }

    private void HandleKeyboardShortcuts()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            ToggleTimeline();
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            ResetPrototype();
        }

        if (keyboard.cKey.wasPressedThisFrame)
        {
            CalibrateMarker();
        }

        if (keyboard.tKey.wasPressedThisFrame)
        {
            RescanMarker();
        }

        if (keyboard.backspaceKey.wasPressedThisFrame)
        {
            ResetCalibration();
        }

        if (keyboard.vKey.wasPressedThisFrame)
        {
            SetDebugVisible(!_isDebugVisible);
        }

        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            SelectAudience(0);
        }

        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            SelectAudience(1);
        }

        if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame)
        {
            AdjustSelectedAudienceFieldOfView(-_fieldOfViewStepDegrees);
        }

        if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame)
        {
            AdjustSelectedAudienceFieldOfView(_fieldOfViewStepDegrees);
        }
#else
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleTimeline();
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetPrototype();
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            CalibrateMarker();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            RescanMarker();
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            ResetCalibration();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            SetDebugVisible(!_isDebugVisible);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SelectAudience(0);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SelectAudience(1);
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            AdjustSelectedAudienceFieldOfView(-_fieldOfViewStepDegrees);
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            AdjustSelectedAudienceFieldOfView(_fieldOfViewStepDegrees);
        }
#endif
    }

    private void HandleSelectedAudienceInput()
    {
        if (!TryGetSelectedAudienceRig(out SUN_AudienceRig selectedRig))
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        Vector3 seatDeltaMeters = Vector3.zero;
        float yawDeltaDegrees = 0.0f;
        float pitchDeltaDegrees = 0.0f;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.wKey.isPressed)
        {
            seatDeltaMeters.z += _audienceMoveMetersPerSecond * deltaTime;
        }

        if (keyboard.sKey.isPressed)
        {
            seatDeltaMeters.z -= _audienceMoveMetersPerSecond * deltaTime;
        }

        if (keyboard.aKey.isPressed)
        {
            seatDeltaMeters.x -= _audienceMoveMetersPerSecond * deltaTime;
        }

        if (keyboard.dKey.isPressed)
        {
            seatDeltaMeters.x += _audienceMoveMetersPerSecond * deltaTime;
        }

        if (keyboard.qKey.isPressed)
        {
            yawDeltaDegrees -= _headRotationDegreesPerSecond * deltaTime;
        }

        if (keyboard.eKey.isPressed)
        {
            yawDeltaDegrees += _headRotationDegreesPerSecond * deltaTime;
        }

        if (keyboard.zKey.isPressed)
        {
            pitchDeltaDegrees += _headRotationDegreesPerSecond * deltaTime;
        }

        if (keyboard.xKey.isPressed)
        {
            pitchDeltaDegrees -= _headRotationDegreesPerSecond * deltaTime;
        }
#else
        if (Input.GetKey(KeyCode.W))
        {
            seatDeltaMeters.z += _audienceMoveMetersPerSecond * deltaTime;
        }

        if (Input.GetKey(KeyCode.S))
        {
            seatDeltaMeters.z -= _audienceMoveMetersPerSecond * deltaTime;
        }

        if (Input.GetKey(KeyCode.A))
        {
            seatDeltaMeters.x -= _audienceMoveMetersPerSecond * deltaTime;
        }

        if (Input.GetKey(KeyCode.D))
        {
            seatDeltaMeters.x += _audienceMoveMetersPerSecond * deltaTime;
        }

        if (Input.GetKey(KeyCode.Q))
        {
            yawDeltaDegrees -= _headRotationDegreesPerSecond * deltaTime;
        }

        if (Input.GetKey(KeyCode.E))
        {
            yawDeltaDegrees += _headRotationDegreesPerSecond * deltaTime;
        }

        if (Input.GetKey(KeyCode.Z))
        {
            pitchDeltaDegrees += _headRotationDegreesPerSecond * deltaTime;
        }

        if (Input.GetKey(KeyCode.X))
        {
            pitchDeltaDegrees -= _headRotationDegreesPerSecond * deltaTime;
        }
#endif

        if (seatDeltaMeters != Vector3.zero)
        {
            selectedRig.MoveRuntimeSeatOffsetMeters(seatDeltaMeters);
        }

        if (!Mathf.Approximately(yawDeltaDegrees, 0.0f) || !Mathf.Approximately(pitchDeltaDegrees, 0.0f))
        {
            ApplySelectedAudienceLookDelta(yawDeltaDegrees, pitchDeltaDegrees);
        }
    }

    private void ToggleTimeline()
    {
        if (_timeline != null && _timeline.IsPlaying)
        {
            PauseTimeline();
        }
        else
        {
            PlayTimeline();
        }
    }

    private void SelectAudience(int audienceIndex)
    {
        if (_audienceRigs == null || audienceIndex < 0 || audienceIndex >= _audienceRigs.Length)
        {
            return;
        }

        if (_audienceRigs[audienceIndex] == null)
        {
            return;
        }

        _selectedAudienceIndex = audienceIndex;
        SetOverlayStatus($"Selected {_audienceRigs[audienceIndex].AudienceId}");
    }

    private bool TryGetSelectedAudienceRig(out SUN_AudienceRig selectedRig)
    {
        selectedRig = null;

        if (_audienceRigs == null || _audienceRigs.Length == 0)
        {
            return false;
        }

        _selectedAudienceIndex = Mathf.Clamp(_selectedAudienceIndex, 0, _audienceRigs.Length - 1);
        selectedRig = _audienceRigs[_selectedAudienceIndex];
        return selectedRig != null;
    }

    private void ApplySelectedAudienceLookDelta(float yawDeltaDegrees, float pitchDeltaDegrees)
    {
        EnsureAudienceControlArrays();

        _runtimeYawDegreesByAudience[_selectedAudienceIndex] += yawDeltaDegrees;
        _runtimePitchDegreesByAudience[_selectedAudienceIndex] = Mathf.Clamp(
            _runtimePitchDegreesByAudience[_selectedAudienceIndex] + pitchDeltaDegrees,
            -80.0f,
            80.0f);

        if (_viewControllers != null
            && _selectedAudienceIndex < _viewControllers.Length
            && _viewControllers[_selectedAudienceIndex] != null)
        {
            _viewControllers[_selectedAudienceIndex].SetSimulatedHeadYawPitch(
                _runtimeYawDegreesByAudience[_selectedAudienceIndex],
                _runtimePitchDegreesByAudience[_selectedAudienceIndex]);
        }
    }

    private void AdjustSelectedAudienceFieldOfView(float deltaDegrees)
    {
        if (!TryGetSelectedAudienceRig(out SUN_AudienceRig selectedRig))
        {
            return;
        }

        float nextFovDegrees = Mathf.Clamp(
            selectedRig.GetFieldOfViewDegrees() + deltaDegrees,
            MinimumRuntimeFovDegrees,
            MaximumRuntimeFovDegrees);
        selectedRig.SetRuntimeFieldOfViewDegrees(nextFovDegrees);
    }

    private void EnsureAudienceControlArrays()
    {
        int count = _audienceRigs != null ? _audienceRigs.Length : 0;
        if (_runtimeYawDegreesByAudience.Length == count && _runtimePitchDegreesByAudience.Length == count)
        {
            return;
        }

        _runtimeYawDegreesByAudience = new float[count];
        _runtimePitchDegreesByAudience = new float[count];
    }

    private void ResetAudienceRuntimeControls()
    {
        EnsureAudienceControlArrays();

        for (int i = 0; i < _audienceRigs.Length; i++)
        {
            if (_audienceRigs[i] != null)
            {
                _audienceRigs[i].ResetRuntimeOverrides();
            }

            _runtimeYawDegreesByAudience[i] = 0.0f;
            _runtimePitchDegreesByAudience[i] = 0.0f;

            if (_viewControllers != null && i < _viewControllers.Length && _viewControllers[i] != null)
            {
                _viewControllers[i].SetSimulatedHeadYawPitch(0.0f, 0.0f);
            }
        }

        _selectedAudienceIndex = 0;
    }
}
