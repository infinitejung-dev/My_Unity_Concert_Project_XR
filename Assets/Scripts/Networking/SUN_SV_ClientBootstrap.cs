using System.Threading.Tasks;
using Fusion;
using UnityEngine;

/// <summary>
/// Joins the prototype Photon Fusion room as an audience Client for PC/mobile rehearsal builds.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkRunner))]
public class SUN_SV_ClientBootstrap : MonoBehaviour
{
    [Header("Fusion Client Session")]
    [Tooltip("Prototype room name shared with the Host bootstrap. Unit: Photon Fusion session name.")]
    // Shared room identifier that places every audience device into the same concert stage session.
    [SerializeField] private string _sessionName = "SV_Prototype_Room";

    [Tooltip("Fusion start mode for audience devices. This prototype task is fixed to Client.")]
    // Client mode joins the authoritative Host instead of owning the shared stage-space timeline.
    [SerializeField] private GameMode _gameMode = GameMode.Client;

    [Tooltip("When enabled, player builds automatically join the configured Host room.")]
    // PC and mobile builds should enter as audience clients for the three-client session test.
    [SerializeField] private bool _autoStartInPlayerBuild = true;

    [Tooltip("When enabled, the Unity Editor also starts this object as a Client.")]
    // Disabled by default so Editor Play remains the Host path for rehearsal setup.
    [SerializeField] private bool _autoStartInEditor = false;

    [Tooltip("Existing NetworkRunner reused for the client connection. If empty, the component on this object is used.")]
    // Runner owns the Client session lifecycle for one audience device.
    [SerializeField] private NetworkRunner _runner;

    [Tooltip("Scene manager used by Fusion when receiving synchronized scene objects from the Host.")]
    // Scene manager keeps client-side stage objects aligned with the Host-owned scene state.
    [SerializeField] private NetworkSceneManagerDefault _sceneManager;

    [Tooltip("Object provider used for later networked stage object prefab reception.")]
    // Object provider prepares this client for later StageObject_Halo network spawning tasks.
    [SerializeField] private NetworkObjectProviderDefault _objectProvider;

    [Tooltip("HUD that shows Client role, session room, and connection state.")]
    // Session HUD gives testers immediate feedback about connection progress and failure reasons.
    [SerializeField] private SUN_SV_SessionStatusView _statusView;

    // Prevents duplicate client StartGame calls while a connection attempt is in progress.
    private bool _isStartRequested;

    private void Awake()
    {
        _runner = _runner != null ? _runner : GetComponent<NetworkRunner>();
        _sceneManager = _sceneManager != null ? _sceneManager : GetComponent<NetworkSceneManagerDefault>();
        _objectProvider = _objectProvider != null ? _objectProvider : GetComponent<NetworkObjectProviderDefault>();
        _statusView = _statusView != null ? _statusView : FindAnyObjectByType<SUN_SV_SessionStatusView>();

        if (_sceneManager == null)
        {
            _sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        if (_objectProvider == null)
        {
            _objectProvider = gameObject.AddComponent<NetworkObjectProviderDefault>();
        }
    }

    private void Start()
    {
        bool shouldAutoStart = Application.isEditor ? _autoStartInEditor : _autoStartInPlayerBuild;
        if (shouldAutoStart)
        {
            _ = StartClientAsync();
            return;
        }

        Debug.Log("[SUN_SV_ClientBootstrap] Client auto start is disabled for this runtime path. Waiting for an explicit StartClientAsync call.", this);
    }

    /// <summary>
    /// Joins the Host-owned session so this device can observe the shared stage-space state.
    /// </summary>
    public async Task StartClientAsync()
    {
        if (!CanStartClient())
        {
            return;
        }

        _isStartRequested = true;
        _statusView?.SetConnecting("Client", _sessionName);
        Debug.Log($"[SUN_SV_ClientBootstrap] Client connection requested. Session='{_sessionName}', GameMode={_gameMode}.", this);

        StartGameResult result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = _gameMode,
            SessionName = _sessionName,
            SceneManager = _sceneManager,
            ObjectProvider = _objectProvider,
            EnableClientSessionCreation = false,
            OnGameStarted = OnClientGameStarted
        });

        if (result.Ok)
        {
            _statusView?.SetConnected("Client", _sessionName, $"LocalPlayer={_runner.LocalPlayer}");
            Debug.Log($"[SUN_SV_ClientBootstrap] Client joined session successfully. Session='{_sessionName}', LocalPlayer={_runner.LocalPlayer}.", this);
            return;
        }

        _isStartRequested = false;
        _statusView?.SetFailed("Client", _sessionName, $"{result.ShutdownReason}: {result.ErrorMessage}");
        Debug.LogError($"[SUN_SV_ClientBootstrap] Client connection failed. Session='{_sessionName}', ShutdownReason={result.ShutdownReason}, Error='{result.ErrorMessage}'.", this);
    }

    private bool CanStartClient()
    {
        if (_runner == null)
        {
            _statusView?.SetFailed("Client", _sessionName, "NetworkRunner reference is missing.");
            Debug.LogError("[SUN_SV_ClientBootstrap] Client start failed before StartGame: NetworkRunner reference is missing.", this);
            return false;
        }

        if (_runner.IsRunning)
        {
            _statusView?.SetConnected("Client", _sessionName, "Existing runner already running");
            Debug.Log("[SUN_SV_ClientBootstrap] Existing NetworkRunner is already running. Client start skipped to avoid duplicate runners.", this);
            return false;
        }

        if (_isStartRequested)
        {
            _statusView?.SetConnecting("Client", _sessionName);
            Debug.Log("[SUN_SV_ClientBootstrap] Client start is already in progress. Duplicate request skipped.", this);
            return false;
        }

        if (_gameMode != GameMode.Client)
        {
            Debug.LogWarning($"[SUN_SV_ClientBootstrap] GameMode was '{_gameMode}'. Prototype Client bootstrap will use Client mode.", this);
            _gameMode = GameMode.Client;
        }

        return true;
    }

    private void OnClientGameStarted(NetworkRunner startedRunner)
    {
        Debug.Log($"[SUN_SV_ClientBootstrap] Fusion OnGameStarted received for audience Client. Runner='{startedRunner.name}'.", this);
    }

    private void OnValidate()
    {
        if (_gameMode != GameMode.Client)
        {
            _gameMode = GameMode.Client;
        }
    }
}
