using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Starts the prototype Photon Fusion Host that owns the shared stage-space session authority.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NetworkRunner))]
public class SUN_SV_HostBootstrap : MonoBehaviour
{
    private const int HostPlayerSlotCount = 1;

    [Header("Fusion Host Session")]
    [Tooltip("Prototype room name used by host and later client bootstrap flows.")]
    // Shared room identifier for all prototype devices joining the same stage-space session.
    [SerializeField] private string _sessionName = "SV_Prototype_Room";

    [Tooltip("Fusion start mode for the authority PC. This prototype task is fixed to Host.")]
    // Host mode gives this PC both server authority and a local player slot for rehearsal control.
    [SerializeField] private GameMode _gameMode = GameMode.Host;

    [Tooltip("When enabled, the Host session starts automatically from Play mode Start().")]
    // Enables one-click Play mode validation without a separate debug UI.
    [SerializeField] private bool _autoStartOnPlay = true;

    [Tooltip("When enabled, automatic Host startup only runs inside the Unity Editor.")]
    // Separates the rehearsal Host path from PC/mobile player builds that should join as clients.
    [SerializeField] private bool _autoStartOnlyInEditor = true;

    [Tooltip("Maximum remote audience client devices allowed after the Host PC joins. Unit: clients.")]
    // Prototype audience capacity: three remote clients plus the one local Host slot.
    [SerializeField] private int _maxRemoteClients = 3;

    [Tooltip("Existing NetworkRunner reused for the stage session. If empty, the component on this object is used.")]
    // Runner that owns the Fusion session lifecycle for the concert stage prototype.
    [SerializeField] private NetworkRunner _runner;

    [Tooltip("Scene manager used by Fusion when keeping scene objects synchronized.")]
    // Scene manager keeps later network scene objects aligned across Host and clients.
    [SerializeField] private NetworkSceneManagerDefault _sceneManager;

    [Tooltip("Object provider used for later networked stage object prefab spawning.")]
    // Object provider is prepared now so later StageObject_Halo spawning can reuse the same runner.
    [SerializeField] private NetworkObjectProviderDefault _objectProvider;

    [Tooltip("Optional HUD that shows Host role, session room, and connection state.")]
    // Shared session HUD lets directors verify whether the current device owns or joins the stage session.
    [SerializeField] private SUN_SV_SessionStatusView _statusView;

    // Prevents duplicate StartGame calls when Play is restarted or another script requests host startup.
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
        if (_autoStartOnPlay)
        {
            if (_autoStartOnlyInEditor && !Application.isEditor)
            {
                Debug.Log("[SUN_SV_HostBootstrap] Host auto start skipped outside the Unity Editor so this build can use the Client path.", this);
                return;
            }

            _ = StartHostAsync();
        }
    }

    /// <summary>
    /// Starts the Host session used as the authoritative stage-space source for the prototype.
    /// </summary>
    public async Task StartHostAsync()
    {
        if (!CanStartHost())
        {
            return;
        }

        _isStartRequested = true;

        int totalPlayerSlots = Mathf.Max(HostPlayerSlotCount, _maxRemoteClients + HostPlayerSlotCount);
        _statusView?.SetConnecting("Host", _sessionName);
        Debug.Log($"[SUN_SV_HostBootstrap] Host start requested. Session='{_sessionName}', GameMode={_gameMode}, TotalPlayerSlots={totalPlayerSlots}, RemoteClientSlots={_maxRemoteClients}.", this);

        StartGameResult result = await _runner.StartGame(new StartGameArgs
        {
            GameMode = _gameMode,
            SessionName = _sessionName,
            PlayerCount = totalPlayerSlots,
            Scene = BuildActiveSceneInfo(),
            SceneManager = _sceneManager,
            ObjectProvider = _objectProvider,
            IsOpen = true,
            IsVisible = true,
            OnGameStarted = OnHostGameStarted
        });

        if (result.Ok)
        {
            _statusView?.SetConnected("Host", _sessionName, $"LocalPlayer={_runner.LocalPlayer}");
            Debug.Log($"[SUN_SV_HostBootstrap] Host session started successfully. Session='{_sessionName}', LocalPlayer={_runner.LocalPlayer}.", this);
            return;
        }

        _isStartRequested = false;
        _statusView?.SetFailed("Host", _sessionName, $"{result.ShutdownReason}: {result.ErrorMessage}");
        Debug.LogError($"[SUN_SV_HostBootstrap] Host session failed. Session='{_sessionName}', ShutdownReason={result.ShutdownReason}, Error='{result.ErrorMessage}'.", this);
    }

    private bool CanStartHost()
    {
        if (_runner == null)
        {
            _statusView?.SetFailed("Host", _sessionName, "NetworkRunner reference is missing.");
            Debug.LogError("[SUN_SV_HostBootstrap] Host start failed before StartGame: NetworkRunner reference is missing.", this);
            return false;
        }

        if (_runner.IsRunning)
        {
            _statusView?.SetConnected("Host", _sessionName, "Existing runner already running");
            Debug.Log($"[SUN_SV_HostBootstrap] Existing NetworkRunner is already running. Session start skipped to avoid duplicate Host runners.", this);
            return false;
        }

        if (_isStartRequested)
        {
            _statusView?.SetConnecting("Host", _sessionName);
            Debug.Log("[SUN_SV_HostBootstrap] Host start is already in progress. Duplicate request skipped.", this);
            return false;
        }

        if (_gameMode != GameMode.Host)
        {
            Debug.LogWarning($"[SUN_SV_HostBootstrap] GameMode was '{_gameMode}'. Prototype Host bootstrap will use Host mode.", this);
            _gameMode = GameMode.Host;
        }

        if (_maxRemoteClients < 0)
        {
            Debug.LogWarning("[SUN_SV_HostBootstrap] Max Remote Clients was below zero. Resetting to 3 for the prototype audience limit.", this);
            _maxRemoteClients = 3;
        }

        return true;
    }

    private NetworkSceneInfo BuildActiveSceneInfo()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex < 0)
        {
            Debug.LogWarning("[SUN_SV_HostBootstrap] Active scene is not registered in Build Settings. Host will start without an initial network scene.", this);
            return default;
        }

        // The Host owns the loaded concert stage scene so later scene NetworkObjects can share one world basis.
        NetworkSceneInfo sceneInfo = new NetworkSceneInfo();
        sceneInfo.AddSceneRef(SceneRef.FromIndex(activeScene.buildIndex), LoadSceneMode.Additive);
        return sceneInfo;
    }

    private void OnHostGameStarted(NetworkRunner startedRunner)
    {
        Debug.Log($"[SUN_SV_HostBootstrap] Fusion OnGameStarted received for authoritative stage Host. Runner='{startedRunner.name}'.", this);
    }

    private void OnValidate()
    {
        if (_gameMode != GameMode.Host)
        {
            _gameMode = GameMode.Host;
        }

        if (_maxRemoteClients < 0)
        {
            _maxRemoteClients = 0;
        }
    }
}
