using Fusion;
using UnityEngine;

/// <summary>
/// Marks StageObject_Halo as the single Host-authoritative scene NetworkObject for the SV prototype.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkObject))]
public class SUN_SV_StageObjectHaloAuthority : NetworkBehaviour
{
    [Header("Stage Object Identity")]
    [Tooltip("Networked stage object identifier used by rehearsal scripts. Unit: scene object id string.")]
    // Keeps the concert-space object identity explicit for the next transform sync task.
    [SerializeField] private string _stageObjectId = "StageObject_Halo";

    [Tooltip("Behaviours that may author StageObject_Halo state only on the Host/StateAuthority.")]
    // Host-only behaviours can drive local authoring, while clients keep the rendered object read-only.
    [SerializeField] private UnityEngine.Behaviour[] _hostOnlyBehaviours = new UnityEngine.Behaviour[0];

    [Tooltip("When enabled, clients disable host-only authoring behaviours after the scene object is attached by Fusion.")]
    // Clients should receive and display the shared stage object without running local authoring logic.
    [SerializeField] private bool _disableHostOnlyBehavioursOnClients = true;

    [Tooltip("When enabled, Spawned logs whether this peer owns or only observes StageObject_Halo.")]
    // One-time log helps testers verify the Host-authority split during prototype rehearsal.
    [SerializeField] private bool _logAuthorityOnSpawn = true;

    // Cached NetworkObject reference used by later transform-sync code to check local write authority.
    private NetworkObject _networkObject;

    /// <summary>
    /// Stable stage-space identifier for the shared halo object.
    /// </summary>
    public string StageObjectId => _stageObjectId;

    /// <summary>
    /// Fusion scene object that owns Host-authoritative state for StageObject_Halo.
    /// </summary>
    public NetworkObject StageNetworkObject => _networkObject;

    /// <summary>
    /// True only on the peer that may author shared stage-space changes.
    /// </summary>
    public bool CanApplyHostAuthoredChange => _networkObject != null && _networkObject.IsValid && _networkObject.HasStateAuthority;

    /// <summary>
    /// True after Fusion has registered this object from the loaded scene.
    /// </summary>
    public bool IsRegisteredSceneObject => _networkObject != null && _networkObject.NetworkTypeId.IsSceneObject;

    private void Awake()
    {
        _networkObject = GetComponent<NetworkObject>();

        if (_hostOnlyBehaviours == null)
        {
            _hostOnlyBehaviours = new UnityEngine.Behaviour[0];
        }
    }

    private void Start()
    {
        if (string.IsNullOrWhiteSpace(_stageObjectId))
        {
            Debug.LogWarning($"{nameof(SUN_SV_StageObjectHaloAuthority)} on {name} has an empty stage object id.", this);
        }
    }

    public override void Spawned()
    {
        _networkObject = Object != null ? Object : _networkObject;

        bool hasHostAuthority = CanApplyHostAuthoredChange;
        ApplyHostOnlyBehaviourState(hasHostAuthority);

        if (_logAuthorityOnSpawn)
        {
            string localMode = hasHostAuthority ? "Host/StateAuthority" : "Client read-only";
            string objectMode = IsRegisteredSceneObject ? "SceneObject" : "PendingSceneObject";
            Debug.Log($"[SUN_SV_StageObjectHaloAuthority] {StageObjectId} attached as {objectMode}. LocalMode={localMode}.", this);
        }
    }

    /// <summary>
    /// Future transform sync scripts should call this before changing stage-space position, rotation, or scale.
    /// </summary>
    public bool CanAuthorStageTransform()
    {
        return CanApplyHostAuthoredChange;
    }

    private void ApplyHostOnlyBehaviourState(bool shouldEnableHostOnlyBehaviours)
    {
        if (!_disableHostOnlyBehavioursOnClients && !shouldEnableHostOnlyBehaviours)
        {
            return;
        }

        for (int i = 0; i < _hostOnlyBehaviours.Length; i++)
        {
            UnityEngine.Behaviour hostOnlyBehaviour = _hostOnlyBehaviours[i];
            if (hostOnlyBehaviour == null || hostOnlyBehaviour == this)
            {
                continue;
            }

            hostOnlyBehaviour.enabled = shouldEnableHostOnlyBehaviours;
        }
    }
}
