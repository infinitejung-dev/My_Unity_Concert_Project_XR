using UnityEngine;

/// <summary>
/// 공통 타임라인의 Stage Space 오브젝트 상태를 실제 Unity Transform과 Renderer에 적용한다.
/// </summary>
public class SUN_ARObjectPresenter : MonoBehaviour
{
    [Tooltip("타임라인 이벤트의 ObjectId와 매칭되는 AR 오브젝트 ID입니다.")]
    [SerializeField] private string _objectId = "Object_01";

    [Tooltip("상태값을 적용할 실제 3D 오브젝트 Transform입니다.")]
    [SerializeField] private Transform _targetObject;

    [Tooltip("표시 상태를 제어할 Renderer 목록입니다.")]
    [SerializeField] private Renderer[] _renderers = new Renderer[0];

    [Tooltip("Stage Space 위치와 회전을 Unity World Transform으로 변환하는 좌표계입니다.")]
    [SerializeField] private SUN_StageCoordinateSystem _coordinateSystem;

    [Tooltip("카메라와 같은 중앙 마커 보정값을 오브젝트 Transform에도 적용하기 위한 캘리브레이터입니다.")]
    [SerializeField] private SUN_CentralMarkerCalibrator _calibrator;

    [Tooltip("현재 ObjectId의 공통 상태를 조회할 타임라인입니다.")]
    [SerializeField] private SUN_StageEventTimeline _timeline;

    [Tooltip("타임라인에서 상태를 찾지 못했을 때 Renderer를 숨길지 결정합니다.")]
    [SerializeField] private bool _hideWhenNoState = true;

    private SUN_StageObjectState _lastAppliedState = new SUN_StageObjectState();
    private bool _hasLastAppliedState;

    public string ObjectId => _objectId;
    public Transform TargetObject => _targetObject;
    public SUN_StageObjectState LastAppliedState => _lastAppliedState;
    public bool HasLastAppliedState => _hasLastAppliedState;

    private void Awake()
    {
        if (_targetObject == null)
        {
            _targetObject = transform;
        }

        if (_renderers == null || _renderers.Length == 0)
        {
            _renderers = _targetObject.GetComponentsInChildren<Renderer>(true);
        }
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(_objectId))
        {
            Debug.LogWarning($"{nameof(SUN_ARObjectPresenter)} on {name} has an empty ObjectId.", this);
        }

        if (_targetObject == null)
        {
            Debug.LogWarning($"{nameof(SUN_ARObjectPresenter)} on {name} has no target object and will disable itself.", this);
            enabled = false;
        }

        if (_coordinateSystem == null)
        {
            Debug.LogWarning($"{nameof(SUN_ARObjectPresenter)} on {name} will apply Stage Space values directly because no coordinate system is assigned.", this);
        }

        if (_timeline == null)
        {
            Debug.LogWarning($"{nameof(SUN_ARObjectPresenter)} on {name} has no timeline assigned.", this);
        }
    }

    private void LateUpdate()
    {
        if (_timeline == null)
        {
            return;
        }

        if (_timeline.EvaluateObjectState(_objectId, out SUN_StageObjectState state))
        {
            ApplyStageObjectState(state);
        }
        else if (_hideWhenNoState)
        {
            SetVisible(false);
        }
    }

    public void ApplyStageObjectState(SUN_StageObjectState state)
    {
        if (state == null)
        {
            return;
        }

        SetStageTransform(state.StagePositionMeters, state.StageRotation, state.Scale);
        SetVisible(state.IsVisible);
        _lastAppliedState = state;
        _hasLastAppliedState = true;
    }

    public void SetVisible(bool isVisible)
    {
        if (_renderers == null)
        {
            return;
        }

        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null)
            {
                _renderers[i].enabled = isVisible;
            }
        }
    }

    /// <summary>
    /// Stage Space Transform 값을 Unity World Transform으로 변환해 적용한다.
    /// </summary>
    public void SetStageTransform(Vector3 stagePositionMeters, Quaternion stageRotation, Vector3 scale)
    {
        if (_targetObject == null)
        {
            return;
        }

        Vector3 calibratedStagePositionMeters = stagePositionMeters;
        Quaternion calibratedStageRotation = stageRotation;
        if (_calibrator != null && _calibrator.HasValidCalibration())
        {
            calibratedStagePositionMeters = _calibrator.ApplyCalibrationToStagePosition(stagePositionMeters);
            calibratedStageRotation = _calibrator.ApplyCalibrationToStageRotation(stageRotation);
        }

        if (_coordinateSystem != null)
        {
            _targetObject.position = _coordinateSystem.StageToWorldPosition(calibratedStagePositionMeters);
            _targetObject.rotation = _coordinateSystem.StageToWorldRotation(calibratedStageRotation);
        }
        else
        {
            _targetObject.position = calibratedStagePositionMeters;
            _targetObject.rotation = calibratedStageRotation;
        }

        _targetObject.localScale = scale;
    }
}
