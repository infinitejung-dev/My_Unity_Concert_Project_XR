using Fusion;
using UnityEngine;

/// <summary>
/// Synchronizes StageObject_Halo scale as one Host-authored concert-stage world value.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class SUN_SV_StageObjectHaloScaleSync : NetworkBehaviour
{
    [Header("Stage World Scale Sync")]
    [Tooltip("Scale target for StageObject_Halo. Unit: Unity local scale vector in the concert-stage world object.")]
    // StageObject_Halo is kept as one shared stage-world object; no audience-local scale correction is applied here.
    [SerializeField] private Transform _targetTransform;

    [Tooltip("When true, the Host seeds the networked scale from the scene object's current scale on spawn.")]
    // Keeps the prototype's authored scene size as the first Host-authoritative network value.
    [SerializeField] private bool _initializeFromSceneScaleOnHost = true;

    [Tooltip("Smallest scale delta that is worth reapplying to the Transform. Unit: Unity scale units.")]
    // Avoids redundant Transform writes while clients receive the same stage-world scale.
    [SerializeField] private float _scaleApplyEpsilon = 0.0001f;

    // Last stage-world scale applied to the Unity Transform for local render state.
    private Vector3 _lastAppliedStageWorldScale = Vector3.one;

    // Tracks whether a networked scale has already been applied at least once.
    private bool _hasAppliedScale;

    /// <summary>
    /// Host-authoritative scale value for the shared concert-stage world object.
    /// </summary>
    [Networked]
    [OnChangedRender(nameof(ApplyNetworkedStageWorldScale))]
    public Vector3 StageWorldScale { get; private set; }

    /// <summary>
    /// Current applied scale in the shared concert-stage world basis.
    /// </summary>
    public Vector3 CurrentStageWorldScale => _hasAppliedScale
        ? _lastAppliedStageWorldScale
        : (_targetTransform != null ? _targetTransform.localScale : Vector3.one);

    /// <summary>
    /// True only on the peer that may write the Host-authoritative scale value.
    /// </summary>
    public bool CanAuthorStageWorldScale => HasStateAuthority;

    private void Awake()
    {
        if (_targetTransform == null)
        {
            _targetTransform = transform;
        }

        if (_scaleApplyEpsilon < 0.0f)
        {
            _scaleApplyEpsilon = 0.0f;
        }
    }

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            Vector3 initialStageWorldScale = _initializeFromSceneScaleOnHost
                ? _targetTransform.localScale
                : Vector3.one;

            SetStageWorldScale(initialStageWorldScale);
            return;
        }

        ApplyStageWorldScale(StageWorldScale);
    }

    public override void Render()
    {
        if (!HasStateAuthority)
        {
            ApplyStageWorldScale(StageWorldScale);
        }
    }

    /// <summary>
    /// Writes a uniform concert-stage world scale from Host authority.
    /// </summary>
    public void SetUniformStageWorldScale(float uniformScale)
    {
        SetStageWorldScale(new Vector3(uniformScale, uniformScale, uniformScale));
    }

    /// <summary>
    /// Writes the shared concert-stage world scale. Audience-local correction is intentionally not part of this step.
    /// </summary>
    public void SetStageWorldScale(Vector3 stageWorldScale)
    {
        if (!CanAuthorStageWorldScale)
        {
            return;
        }

        if (!IsUsableScale(stageWorldScale))
        {
            return;
        }

        Vector3 sanitizedStageWorldScale = SanitizeScale(stageWorldScale);
        StageWorldScale = sanitizedStageWorldScale;
        ApplyStageWorldScale(sanitizedStageWorldScale);
    }

    private void ApplyNetworkedStageWorldScale()
    {
        ApplyStageWorldScale(StageWorldScale);
    }

    private void ApplyStageWorldScale(Vector3 stageWorldScale)
    {
        if (_targetTransform == null)
        {
            return;
        }

        if (!IsUsableScale(stageWorldScale))
        {
            return;
        }

        Vector3 sanitizedStageWorldScale = SanitizeScale(stageWorldScale);
        if (_hasAppliedScale && HasMeaningfullySameScale(sanitizedStageWorldScale, _lastAppliedStageWorldScale))
        {
            return;
        }

        // This is the single conversion point for scale in this task: network value -> shared stage-world Transform scale.
        _targetTransform.localScale = sanitizedStageWorldScale;
        _lastAppliedStageWorldScale = sanitizedStageWorldScale;
        _hasAppliedScale = true;
    }

    private bool HasMeaningfullySameScale(Vector3 leftScale, Vector3 rightScale)
    {
        return (leftScale - rightScale).sqrMagnitude <= _scaleApplyEpsilon * _scaleApplyEpsilon;
    }

    private static Vector3 SanitizeScale(Vector3 stageWorldScale)
    {
        if (!IsUsableScale(stageWorldScale))
        {
            return Vector3.one;
        }

        return stageWorldScale;
    }

    private static bool IsUsableScale(Vector3 stageWorldScale)
    {
        const float MinimumScaleComponent = 0.0001f;

        // A default network value of Vector3.zero should not collapse the object before the Host state arrives.
        return IsFinite(stageWorldScale.x)
            && IsFinite(stageWorldScale.y)
            && IsFinite(stageWorldScale.z)
            && stageWorldScale.x >= MinimumScaleComponent
            && stageWorldScale.y >= MinimumScaleComponent
            && stageWorldScale.z >= MinimumScaleComponent;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
