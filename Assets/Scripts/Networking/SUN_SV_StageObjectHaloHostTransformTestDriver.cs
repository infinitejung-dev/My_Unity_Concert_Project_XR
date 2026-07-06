using UnityEngine;
using NetworkTransform = Fusion.NetworkTransform;

/// <summary>
/// Provides Host-only keyboard controls for testing StageObject_Halo world transform synchronization.
/// </summary>
[DisallowMultipleComponent]
public class SUN_SV_StageObjectHaloHostTransformTestDriver : MonoBehaviour
{
    [Header("Host Authority References")]
    [Tooltip("Authority marker that confirms only the Host/StateAuthority can author StageObject_Halo. Unit: component reference.")]
    // This keeps client peers read-only even if the test driver component exists in their scene.
    [SerializeField] private SUN_SV_StageObjectHaloAuthority _authority;

    [Tooltip("Scale synchronizer that publishes Host-authored concert-stage world scale. Unit: component reference.")]
    // Scale is synchronized as one stage-world value, not as an audience-local correction.
    [SerializeField] private SUN_SV_StageObjectHaloScaleSync _scaleSync;

    [Tooltip("Transform moved and rotated in the concert-stage world coordinate basis. Unit: Unity Transform.")]
    // Position and rotation are authored directly in stage/world axes for this prototype task.
    [SerializeField] private Transform _stageWorldTransform;

    [Tooltip("Fusion transform synchronizer that receives Host-authored stage-world position and rotation writes. Unit: component reference.")]
    // Position and rotation must enter NetworkTransform state, otherwise Render can restore the previous network pose.
    [SerializeField] private NetworkTransform _networkTransform;

    [Header("Test Isolation")]
    [Tooltip("Behaviours disabled on Host while this keyboard test owns the stage-world Transform. Unit: component references.")]
    // Prevents local timeline/presenter scripts from overwriting the Host-authored transform test values.
    [SerializeField] private Behaviour[] _behavioursToDisableOnHostWhileTesting = new Behaviour[0];

    [Header("Step Settings")]
    [Tooltip("Distance moved per key press on the concert-stage world X/Z axes. Unit: meters.")]
    [SerializeField] private float _moveStepMeters = 0.25f;

    [Tooltip("Rotation applied per key press around the concert-stage world Y axis. Unit: degrees.")]
    [SerializeField] private float _rotateStepDegrees = 15.0f;

    [Tooltip("Uniform scale multiplier delta per key press. Unit: scalar multiplier.")]
    [SerializeField] private float _scaleStep = 0.1f;

    [Tooltip("Minimum uniform scale multiplier applied to the authored scene scale. Unit: scalar multiplier.")]
    [SerializeField] private float _minScale = 0.5f;

    [Tooltip("Maximum uniform scale multiplier applied to the authored scene scale. Unit: scalar multiplier.")]
    [SerializeField] private float _maxScale = 2.0f;

    // Scene-authored scale used as the base shape for uniform prototype scaling.
    private Vector3 _baseStageWorldScale = Vector3.one;

    // Current uniform multiplier applied on top of the authored scene scale.
    private float _currentUniformScale = 1.0f;

    // Ensures the prototype-only behaviour disabling runs once after Host authority is available.
    private bool _hasAppliedHostTestIsolation;

    private void Awake()
    {
        if (_authority == null)
        {
            _authority = GetComponent<SUN_SV_StageObjectHaloAuthority>();
        }

        if (_scaleSync == null)
        {
            _scaleSync = GetComponent<SUN_SV_StageObjectHaloScaleSync>();
        }

        if (_stageWorldTransform == null)
        {
            _stageWorldTransform = transform;
        }

        if (_networkTransform == null && _stageWorldTransform != null)
        {
            _networkTransform = _stageWorldTransform.GetComponent<NetworkTransform>();
        }

        if (_networkTransform == null)
        {
            _networkTransform = GetComponent<NetworkTransform>();
        }

        if (_behavioursToDisableOnHostWhileTesting == null)
        {
            _behavioursToDisableOnHostWhileTesting = new Behaviour[0];
        }

        NormalizeStepSettings();
    }

    private void Start()
    {
        if (_stageWorldTransform == null)
        {
            Debug.LogWarning($"{nameof(SUN_SV_StageObjectHaloHostTransformTestDriver)} on {name} has no target Transform.", this);
            enabled = false;
            return;
        }

        _baseStageWorldScale = _stageWorldTransform.localScale;
        _currentUniformScale = 1.0f;

        if (_authority == null)
        {
            Debug.LogWarning($"{nameof(SUN_SV_StageObjectHaloHostTransformTestDriver)} on {name} has no Host authority reference.", this);
        }

        if (_scaleSync == null)
        {
            Debug.LogWarning($"{nameof(SUN_SV_StageObjectHaloHostTransformTestDriver)} on {name} has no scale sync reference.", this);
        }
    }

    private void Update()
    {
        if (!CanApplyHostAuthoredTransform())
        {
            return;
        }

        ApplyHostTestIsolation();
        ApplyStageWorldPoseInput();
        ApplyScaleInput();
    }

    private bool CanApplyHostAuthoredTransform()
    {
        if (_authority != null)
        {
            return _authority.CanAuthorStageTransform();
        }

        return _scaleSync != null && _scaleSync.CanAuthorStageWorldScale;
    }

    private void ApplyHostTestIsolation()
    {
        if (_hasAppliedHostTestIsolation)
        {
            return;
        }

        for (int i = 0; i < _behavioursToDisableOnHostWhileTesting.Length; i++)
        {
            Behaviour behaviour = _behavioursToDisableOnHostWhileTesting[i];
            if (behaviour == null || behaviour == this)
            {
                continue;
            }

            behaviour.enabled = false;
        }

        _hasAppliedHostTestIsolation = true;
    }

    private void ApplyStageWorldPoseInput()
    {
        Vector3 stageWorldMoveMeters = ReadStageWorldMoveInputMeters();
        float stageWorldYawDegrees = ReadStageWorldYawInputDegrees();
        bool hasMoveInput = stageWorldMoveMeters != Vector3.zero;
        bool hasRotateInput = !Mathf.Approximately(stageWorldYawDegrees, 0.0f);

        if (!hasMoveInput && !hasRotateInput)
        {
            return;
        }

        Vector3 nextStageWorldPositionMeters = _stageWorldTransform.position + stageWorldMoveMeters;
        Quaternion nextStageWorldRotation = _stageWorldTransform.rotation;
        if (hasRotateInput)
        {
            // Rotation is authored around the shared stage-world Y axis, not each audience device's local up axis.
            nextStageWorldRotation = Quaternion.AngleAxis(stageWorldYawDegrees, Vector3.up) * nextStageWorldRotation;
        }

        ApplyStageWorldPose(nextStageWorldPositionMeters, nextStageWorldRotation);
    }

    private Vector3 ReadStageWorldMoveInputMeters()
    {
        float stageWorldXDirection = ReadSignedKeyPair(KeyCode.J, KeyCode.L);
        float stageWorldZDirection = ReadSignedKeyPair(KeyCode.K, KeyCode.I);

        if (Mathf.Approximately(stageWorldXDirection, 0.0f) && Mathf.Approximately(stageWorldZDirection, 0.0f))
        {
            return Vector3.zero;
        }

        // Movement is applied in one shared concert-stage world basis; audience-local offsets are intentionally excluded.
        return new Vector3(stageWorldXDirection, 0.0f, stageWorldZDirection) * _moveStepMeters;
    }

    private float ReadStageWorldYawInputDegrees()
    {
        return ReadSignedKeyPair(KeyCode.U, KeyCode.O) * _rotateStepDegrees;
    }

    private void ApplyStageWorldPose(Vector3 stageWorldPositionMeters, Quaternion stageWorldRotation)
    {
        if (_networkTransform != null)
        {
            // This is the conversion point for Host keyboard input: stage-world pose -> Fusion NetworkTransform TRSP state.
            _networkTransform.Teleport(stageWorldPositionMeters, stageWorldRotation);
            return;
        }

        // Fallback keeps local Host testing usable before Fusion NetworkTransform is present in the scene.
        _stageWorldTransform.SetPositionAndRotation(stageWorldPositionMeters, stageWorldRotation);
    }

    private static float ReadSignedKeyPair(KeyCode negativeKey, KeyCode positiveKey)
    {
        float direction = 0.0f;
        if (Input.GetKeyDown(negativeKey))
        {
            direction -= 1.0f;
        }

        if (Input.GetKeyDown(positiveKey))
        {
            direction += 1.0f;
        }

        return direction;
    }

    private void ApplyScaleInput()
    {
        float scaleDelta = 0.0f;

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            scaleDelta -= _scaleStep;
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            scaleDelta += _scaleStep;
        }

        if (Mathf.Approximately(scaleDelta, 0.0f))
        {
            return;
        }

        _currentUniformScale = Mathf.Clamp(_currentUniformScale + scaleDelta, _minScale, _maxScale);
        Vector3 nextStageWorldScale = _baseStageWorldScale * _currentUniformScale;

        if (_scaleSync != null)
        {
            _scaleSync.SetStageWorldScale(nextStageWorldScale);
            return;
        }

        // Fallback keeps local Host testing usable before Fusion is running; clients still cannot reach this branch.
        _stageWorldTransform.localScale = nextStageWorldScale;
    }

    private void OnValidate()
    {
        NormalizeStepSettings();
    }

    private void NormalizeStepSettings()
    {
        _moveStepMeters = Mathf.Max(0.0f, _moveStepMeters);
        _rotateStepDegrees = Mathf.Max(0.0f, _rotateStepDegrees);
        _scaleStep = Mathf.Max(0.0f, _scaleStep);
        _minScale = Mathf.Max(0.0f, _minScale);
        _maxScale = Mathf.Max(_minScale, _maxScale);
    }
}
