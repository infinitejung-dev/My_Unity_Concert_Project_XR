using System.Collections.Generic;
using System.Text;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.UI;

/// <summary>
/// 프로토타입 검증 중 좌표, 캘리브레이션, 관객, 타임라인, Presenter 상태를 표시하는 디버그 계층이다.
/// </summary>
public class SUN_PrototypeDebugOverlay : MonoBehaviour
{
    private const float GizmoAudienceRadius = 0.12f;
    private const float GizmoObjectRadius = 0.16f;

    [Tooltip("디버그 UI 전체를 켜고 끌 루트 오브젝트입니다.")]
    [SerializeField] private GameObject _displayRoot;

    [Tooltip("Ready, Playing, Uncalibrated 같은 현재 상태 메시지를 표시할 텍스트입니다.")]
    [SerializeField] private Text _statusText;

    [Tooltip("누락 참조와 비정상 입력 경고를 표시할 텍스트입니다.")]
    [SerializeField] private Text _warningText;

    [Tooltip("좌표와 타임라인 상세 값을 표시할 텍스트입니다.")]
    [SerializeField] private Text _detailsText;

    [Tooltip("Stage Space 원점과 축을 표시할 좌표계입니다.")]
    [SerializeField] private SUN_StageCoordinateSystem _coordinateSystem;

    [Tooltip("캘리브레이션 상태를 표시할 캘리브레이터입니다.")]
    [SerializeField] private SUN_CentralMarkerCalibrator _calibrator;

    [Tooltip("공통 이벤트 시간과 재생 상태를 표시할 타임라인입니다.")]
    [SerializeField] private SUN_StageEventTimeline _timeline;

    [Tooltip("관객별 몸 위치와 눈 위치를 표시할 Rig 목록입니다.")]
    [SerializeField] private SUN_AudienceRig[] _audienceRigs = new SUN_AudienceRig[0];

    [Tooltip("관객별 시선 방향을 표시할 View Controller 목록입니다.")]
    [SerializeField] private SUN_AudienceViewController[] _viewControllers = new SUN_AudienceViewController[0];

    [Tooltip("오브젝트 Stage Space 및 Unity World 상태를 표시할 Presenter 목록입니다.")]
    [SerializeField] private SUN_ARObjectPresenter[] _objectPresenters = new SUN_ARObjectPresenter[0];

    [Tooltip("켜져 있으면 BackQuote 키로 디버그 표시를 토글합니다.")]
    [SerializeField] private bool _allowKeyboardToggle = true;

    private readonly List<string> _missingReferences = new List<string>();
    private readonly StringBuilder _detailsBuilder = new StringBuilder(1024);
    private bool _isVisible = true;
    private string _statusMessage = "Uncalibrated";
    private string _warningMessage = string.Empty;

    private void Awake()
    {
        if (_displayRoot == null)
        {
            _displayRoot = gameObject;
        }

        CacheTextFieldsIfNeeded();
        ApplyVisibility();
    }

    private void Start()
    {
        SetStatus("Ready");
        RefreshText();
    }

    private void Update()
    {
        if (_allowKeyboardToggle && WasDebugTogglePressedThisFrame())
        {
            SetVisible(!_isVisible);
        }
    }

    private void LateUpdate()
    {
        RefreshText();
    }

    public void SetVisible(bool isVisible)
    {
        _isVisible = isVisible;
        ApplyVisibility();
    }

    public void SetStatus(string statusMessage)
    {
        _statusMessage = string.IsNullOrWhiteSpace(statusMessage) ? "Ready" : statusMessage;
        RefreshText();
    }

    public void SetWarning(string warningMessage)
    {
        _warningMessage = warningMessage ?? string.Empty;
        RefreshText();
    }

    public void ClearWarning()
    {
        _warningMessage = string.Empty;
        RefreshText();
    }

    public void RegisterMissingReference(string referenceName)
    {
        if (string.IsNullOrWhiteSpace(referenceName) || _missingReferences.Contains(referenceName))
        {
            return;
        }

        _missingReferences.Add(referenceName);
        RefreshText();
    }

    public void SetPrototypeReferences(
        SUN_StageCoordinateSystem coordinateSystem,
        SUN_CentralMarkerCalibrator calibrator,
        SUN_StageEventTimeline timeline,
        SUN_AudienceRig[] audienceRigs,
        SUN_AudienceViewController[] viewControllers,
        SUN_ARObjectPresenter[] objectPresenters)
    {
        _coordinateSystem = coordinateSystem;
        _calibrator = calibrator;
        _timeline = timeline;
        _audienceRigs = audienceRigs ?? new SUN_AudienceRig[0];
        _viewControllers = viewControllers ?? new SUN_AudienceViewController[0];
        _objectPresenters = objectPresenters ?? new SUN_ARObjectPresenter[0];
    }

    private void OnDrawGizmos()
    {
        if (!_isVisible)
        {
            return;
        }

        DrawStageAxes();
        DrawAudienceGizmos();
        DrawObjectGizmos();
    }

    private void RefreshText()
    {
        if (!_isVisible)
        {
            return;
        }

        if (_statusText != null)
        {
            _statusText.text = _statusMessage;
        }

        if (_warningText != null)
        {
            _warningText.text = BuildWarningText();
        }

        if (_detailsText != null)
        {
            _detailsText.text = BuildDetailsText();
        }
    }

    private string BuildWarningText()
    {
        _detailsBuilder.Clear();

        if (!string.IsNullOrWhiteSpace(_warningMessage))
        {
            _detailsBuilder.AppendLine(_warningMessage);
        }

        for (int i = 0; i < _missingReferences.Count; i++)
        {
            _detailsBuilder.Append("Missing: ");
            _detailsBuilder.AppendLine(_missingReferences[i]);
        }

        return _detailsBuilder.ToString();
    }

    private string BuildDetailsText()
    {
        _detailsBuilder.Clear();

        if (_coordinateSystem != null)
        {
            Vector3 originWorld = _coordinateSystem.StageToWorldPosition(Vector3.zero);
            _detailsBuilder.Append("Stage Origin World: ");
            _detailsBuilder.AppendLine(originWorld.ToString("F2"));
        }

        if (_calibrator != null)
        {
            _detailsBuilder.Append("Calibration: ");
            _detailsBuilder.Append(_calibrator.Status);
            _detailsBuilder.Append(", Last Time: ");
            _detailsBuilder.AppendLine(_calibrator.LastValidCalibrationTimeSeconds.ToString("F2"));
        }

        if (_timeline != null)
        {
            _detailsBuilder.Append("Timeline: ");
            _detailsBuilder.Append(_timeline.IsPlaying ? "Playing" : "Paused");
            _detailsBuilder.Append(", Time: ");
            _detailsBuilder.AppendLine(_timeline.TimelineTimeSeconds.ToString("F2"));
        }

        AppendAudienceDetails();
        AppendPresenterDetails();
        return _detailsBuilder.ToString();
    }

    private void AppendAudienceDetails()
    {
        if (_audienceRigs == null)
        {
            return;
        }

        for (int i = 0; i < _audienceRigs.Length; i++)
        {
            SUN_AudienceRig rig = _audienceRigs[i];
            if (rig == null)
            {
                continue;
            }

            _detailsBuilder.Append("Audience ");
            _detailsBuilder.Append(rig.AudienceId);
            _detailsBuilder.Append(" Seat: ");
            _detailsBuilder.Append(rig.GetSeatStagePositionMeters().ToString("F2"));
            _detailsBuilder.Append(", Eye: ");
            _detailsBuilder.AppendLine(rig.GetEyeStagePositionMeters().ToString("F2"));

            if (_viewControllers != null && i < _viewControllers.Length && _viewControllers[i] != null)
            {
                _detailsBuilder.Append("Look: ");
                _detailsBuilder.AppendLine(_viewControllers[i].LookRotation.eulerAngles.ToString("F1"));
            }
        }
    }

    private void AppendPresenterDetails()
    {
        if (_objectPresenters == null)
        {
            return;
        }

        for (int i = 0; i < _objectPresenters.Length; i++)
        {
            SUN_ARObjectPresenter presenter = _objectPresenters[i];
            if (presenter == null)
            {
                continue;
            }

            _detailsBuilder.Append("Object ");
            _detailsBuilder.Append(presenter.ObjectId);

            if (presenter.HasLastAppliedState)
            {
                SUN_StageObjectState state = presenter.LastAppliedState;
                _detailsBuilder.Append(" Event: ");
                _detailsBuilder.Append(state.EventId);
                _detailsBuilder.Append(" Stage: ");
                _detailsBuilder.Append(state.StagePositionMeters.ToString("F2"));
                _detailsBuilder.Append(", Progress: ");
                _detailsBuilder.Append(state.NormalizedProgress.ToString("F2"));
                _detailsBuilder.Append(", Visible: ");
                _detailsBuilder.Append(state.IsVisible);
            }

            if (presenter.TargetObject != null)
            {
                _detailsBuilder.Append(", World: ");
                _detailsBuilder.Append(presenter.TargetObject.position.ToString("F2"));
            }

            _detailsBuilder.AppendLine();
        }
    }

    private void ApplyVisibility()
    {
        if (_displayRoot != null)
        {
            _displayRoot.SetActive(_isVisible);
        }
    }

    private void CacheTextFieldsIfNeeded()
    {
        if (_statusText != null && _warningText != null && _detailsText != null)
        {
            return;
        }

        Text[] texts = GetComponentsInChildren<Text>(true);
        if (texts.Length > 0 && _statusText == null)
        {
            _statusText = texts[0];
        }

        if (texts.Length > 1 && _warningText == null)
        {
            _warningText = texts[1];
        }

        if (texts.Length > 2 && _detailsText == null)
        {
            _detailsText = texts[2];
        }
    }

    private void DrawStageAxes()
    {
        if (_coordinateSystem == null)
        {
            return;
        }

        Vector3 origin = _coordinateSystem.StageToWorldPosition(Vector3.zero);
        Vector3 right = _coordinateSystem.StageToWorldPosition(Vector3.right) - origin;
        Vector3 up = _coordinateSystem.StageToWorldPosition(Vector3.up) - origin;
        Vector3 forward = _coordinateSystem.StageToWorldPosition(Vector3.forward) - origin;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + right);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + up);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(origin, origin + forward);
    }

    private void DrawAudienceGizmos()
    {
        if (_audienceRigs == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        for (int i = 0; i < _audienceRigs.Length; i++)
        {
            SUN_AudienceRig rig = _audienceRigs[i];
            if (rig == null)
            {
                continue;
            }

            Vector3 eyeStagePositionMeters = rig.GetEyeStagePositionMeters();
            Vector3 worldPosition = _coordinateSystem != null
                ? _coordinateSystem.StageToWorldPosition(eyeStagePositionMeters)
                : eyeStagePositionMeters;

            Gizmos.DrawSphere(worldPosition, GizmoAudienceRadius);
        }
    }

    private void DrawObjectGizmos()
    {
        if (_objectPresenters == null)
        {
            return;
        }

        Gizmos.color = Color.magenta;
        for (int i = 0; i < _objectPresenters.Length; i++)
        {
            SUN_ARObjectPresenter presenter = _objectPresenters[i];
            if (presenter == null || presenter.TargetObject == null)
            {
                continue;
            }

            Gizmos.DrawWireSphere(presenter.TargetObject.position, GizmoObjectRadius);
        }
    }

    private bool WasDebugTogglePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.backquoteKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.BackQuote);
#endif
    }
}
