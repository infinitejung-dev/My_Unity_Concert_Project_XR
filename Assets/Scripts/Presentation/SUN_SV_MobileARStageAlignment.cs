using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

/// <summary>
/// Aligns the mobile AR origin with the concert stage origin and guards Android AR camera startup.
/// </summary>
public class SUN_SV_MobileARStageAlignment : MonoBehaviour
{
    [Tooltip("XR Origin that receives the mobile device pose in Unity World space.")]
    [SerializeField] private XROrigin _xrOrigin;

    [Tooltip("Stage-space origin used as the shared start point for the concert coordinate system and mobile AR world.")]
    [SerializeField] private Transform _stageOrigin;

    [Tooltip("Top-level root that groups stage-space world objects.")]
    [SerializeField] private Transform _stageWorldRoot;

    [Tooltip("Root for network-synchronized stage objects.")]
    [SerializeField] private Transform _networkObjectsRoot;

    [Tooltip("Halo object used to verify that the networked stage transform remains independent from the AR camera.")]
    [SerializeField] private Transform _stageObjectHalo;

    [Tooltip("Mobile AR camera under the XR Origin. It should only follow the device pose.")]
    [SerializeField] private Camera _arCamera;

    [Tooltip("AR Session that is enabled after Android camera permission is ready.")]
    [SerializeField] private ARSession _arSession;

    [Tooltip("AR camera manager that provides camera frames and camera permission state.")]
    [SerializeField] private ARCameraManager _arCameraManager;

    [Tooltip("AR camera background component that draws the device camera feed behind stage objects.")]
    [SerializeField] private ARCameraBackground _arCameraBackground;

    [Header("Audience Seat View Gate")]
    [Tooltip("Fusion runners that can provide the local PlayerRef used to pick the audience seat index.")]
    [SerializeField] private NetworkRunner[] _networkRunners;

    [Tooltip("Prototype audience rigs available in the scene. Index 0 is Audience A, index 1 is Audience B, and so on.")]
    [SerializeField] private SUN_AudienceRig[] _audienceRigs;

    [Tooltip("Seat index used before Fusion has a valid local player. Unit: audience rig array index.")]
    [SerializeField] private int _fallbackAudienceSeatIndex;

    [Tooltip("When enabled, the lowest active Fusion PlayerId is treated as the Host/director and is not assigned an audience seat.")]
    [SerializeField] private bool _excludeLowestActivePlayerIdAsHost = true;

    [Tooltip("Fallback PlayerId used for Audience_A when the active player list is unavailable. Host/director commonly occupies PlayerId 1, so Audience_A starts at PlayerId 2.")]
    [SerializeField] private int _firstAudiencePlayerIdFallback = 2;

    [Tooltip("When enabled in an Android Player, mobile AR uses the XR Origin Main Camera and keeps virtual audience cameras disabled. Editor keeps the selected Audience Rig camera for GameView fallback.")]
    [SerializeField] private bool _useArCameraAsLocalAudienceView = true;

    [Tooltip("Write a log when the local client selects a single audience rig.")]
    [SerializeField] private bool _logAudienceRigSelection = true;

    [Tooltip("Request Android camera permission before enabling the AR Session.")]
    [SerializeField] private bool _requestAndroidCameraPermissionBeforeSessionStart = true;

    [Tooltip("Keep AR Session disabled at scene start and enable it only after the startup guard completes.")]
    [SerializeField] private bool _enableArSessionAfterPermission = true;

    [Tooltip("Align XR Origin to Stage Origin when the scene starts, and keep it synced to the selected audience eye in Android AR camera runtime.")]
    [SerializeField] private bool _alignXrOriginToStageOriginOnStart = true;

    [Tooltip("Write AR camera startup state logs until the first camera frame arrives.")]
    [SerializeField] private bool _logArCameraStartupState = true;

    [Tooltip("Seconds between repeated AR camera startup status logs.")]
    [SerializeField] private float _startupStatusLogIntervalSeconds = 2.0f;

    [Tooltip("Write the stage alignment and Halo hierarchy status when the scene starts.")]
    [SerializeField] private bool _logAlignmentOnStart = true;

    private Coroutine _startupRoutine;
    private bool _hasReceivedCameraFrame;
    private bool _isWaitingForPermissionRetry;
    private float _nextStatusLogTime;
    private ARSessionState _lastLoggedSessionState;
    private Camera[] _audienceCameras;
    private AudioListener[] _audienceAudioListeners;
    private SUN_AudienceViewController[] _audienceViewControllers;
    private AudioListener _arAudioListener;
    private SUN_AudienceRig _selectedAudienceRig;
    private int _selectedAudienceSeatIndex = -1;
    private bool _hasAppliedRunningPlayerSeat;
    private bool _areArCameraSubsystemsRequestedEnabled;
    private readonly List<int> _activePlayerIds = new List<int>(8);

    public Transform StageOrigin => _stageOrigin;
    public Transform StageObjectHalo => _stageObjectHalo;
    public Camera ArCamera => _arCamera;
    public SUN_AudienceRig SelectedAudienceRig => _selectedAudienceRig;
    public int SelectedAudienceSeatIndex => _selectedAudienceSeatIndex;

    private void Awake()
    {
        // Inspector references are the contract. Runtime lookup is limited to local objects and camera components.
        if (_xrOrigin == null)
        {
            _xrOrigin = GetComponent<XROrigin>();
        }

        if (_arCamera == null && _xrOrigin != null)
        {
            _arCamera = _xrOrigin.Camera;
        }

        if (_arCameraManager == null && _arCamera != null)
        {
            _arCameraManager = _arCamera.GetComponent<ARCameraManager>();
        }

        if (_arCameraBackground == null && _arCamera != null)
        {
            _arCameraBackground = _arCamera.GetComponent<ARCameraBackground>();
        }

        if (_arCamera != null)
        {
            _arAudioListener = _arCamera.GetComponent<AudioListener>();
        }

        CacheAudienceRigComponents();
        CacheNetworkRunners();
        ApplyAudienceSeatSelection(false);

        // ARSession has the earliest AR Foundation execution order, so the scene should serialize it disabled.
        // This backup keeps manually edited scenes from starting ARCore before Android camera permission is checked.
        if (_enableArSessionAfterPermission && _arSession != null)
        {
            _arSession.enabled = false;
        }
    }

    private void OnEnable()
    {
        ARSession.stateChanged += OnArSessionStateChanged;

        if (_arCameraManager != null)
        {
            _arCameraManager.frameReceived += OnArCameraFrameReceived;
        }
    }

    private void Start()
    {
        ApplyAudienceSeatSelection(false);

        // Align the AR world start point with the concert stage origin before the first camera pose is applied.
        if (_alignXrOriginToStageOriginOnStart)
        {
            AlignXrOriginToStageOrigin();
        }

        ValidateStageObjectHierarchy();
        BeginArCameraStartup();
    }

    private void Update()
    {
        UpdateAudienceSeatFromRunningPlayer();

        if (!ShouldUseArCameraAsLocalAudienceView() || !_logArCameraStartupState || _hasReceivedCameraFrame)
        {
            return;
        }

        if (Time.unscaledTime < _nextStatusLogTime)
        {
            return;
        }

        _nextStatusLogTime = Time.unscaledTime + Mathf.Max(0.5f, _startupStatusLogIntervalSeconds);
        LogArCameraStatus("waiting for first AR camera frame");
    }

    private void LateUpdate()
    {
        if (!_alignXrOriginToStageOriginOnStart || !ShouldUseArCameraAsLocalAudienceView())
        {
            return;
        }

        // Android AR pose belongs to the Main Camera child. Only the XR Origin stage baseline is moved here.
        TryAlignXrOriginToStageOrigin(false);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!ShouldUseArCameraAsLocalAudienceView() || !hasFocus || !_isWaitingForPermissionRetry)
        {
            return;
        }

        if (!HasAndroidCameraPermission())
        {
            LogArCameraStatus("camera permission is still missing after focus return");
            return;
        }

        Debug.Log($"{nameof(SUN_SV_MobileARStageAlignment)} detected Android camera permission on focus return and will retry AR Session startup.", this);
        _isWaitingForPermissionRetry = false;
        BeginArCameraStartup();
    }

    private void OnDisable()
    {
        ARSession.stateChanged -= OnArSessionStateChanged;

        if (_arCameraManager != null)
        {
            _arCameraManager.frameReceived -= OnArCameraFrameReceived;
        }
    }

    /// <summary>
    /// Sets the XR Origin start transform to the selected audience eye position, or to the stage origin as a fallback.
    /// </summary>
    [ContextMenu("Align XR Origin To Stage Origin")]
    public void AlignXrOriginToStageOrigin()
    {
        TryAlignXrOriginToStageOrigin(true);
    }

    private bool TryAlignXrOriginToStageOrigin(bool shouldLogWarning)
    {
        if (_xrOrigin == null || _stageOrigin == null)
        {
            if (shouldLogWarning)
            {
                Debug.LogWarning($"{nameof(SUN_SV_MobileARStageAlignment)} cannot align because XR Origin or Stage Origin is missing.", this);
            }

            return false;
        }

        if (_selectedAudienceRig != null)
        {
            SUN_StageCoordinateSystem coordinateSystem = _selectedAudienceRig.CoordinateSystem;
            Vector3 eyeStagePositionMeters = _selectedAudienceRig.GetEyeStagePositionMeters();
            Quaternion stageRotation = Quaternion.identity;

            // Conversion boundary: selected audience eye pose moves from stage-space meters into Unity world space.
            Vector3 worldPosition = coordinateSystem != null
                ? coordinateSystem.StageToWorldPosition(eyeStagePositionMeters)
                : eyeStagePositionMeters;
            Quaternion worldRotation = coordinateSystem != null
                ? coordinateSystem.StageToWorldRotation(stageRotation)
                : stageRotation;

            _xrOrigin.transform.SetPositionAndRotation(worldPosition, worldRotation);
            return true;
        }

        _xrOrigin.transform.SetPositionAndRotation(_stageOrigin.position, _stageOrigin.rotation);
        return true;
    }

    /// <summary>
    /// Checks whether the Halo remains outside the AR Camera hierarchy.
    /// </summary>
    public bool IsStageObjectIndependentFromCamera()
    {
        if (_stageObjectHalo == null || _arCamera == null)
        {
            return false;
        }

        return !_stageObjectHalo.IsChildOf(_arCamera.transform);
    }

    private void BeginArCameraStartup()
    {
        if (!ShouldUseArCameraAsLocalAudienceView())
        {
            if (_arSession != null)
            {
                _arSession.enabled = false;
            }

            SetArCameraComponentsEnabled(false);
            LogArCameraStatus("using selected Audience Rig camera fallback outside Android Player");
            return;
        }

        if (!_enableArSessionAfterPermission)
        {
            SetArCameraComponentsEnabled(true);
            LogArCameraStatus("AR Session startup guard is disabled");
            return;
        }

        if (_startupRoutine != null)
        {
            return;
        }

        _startupRoutine = StartCoroutine(StartArSessionWhenCameraPermissionIsReady());
    }

    private IEnumerator StartArSessionWhenCameraPermissionIsReady()
    {
        if (!ShouldUseArCameraAsLocalAudienceView())
        {
            if (_arSession != null)
            {
                _arSession.enabled = false;
            }

            SetArCameraComponentsEnabled(false);
            _startupRoutine = null;
            yield break;
        }

        if (_arSession == null)
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARStageAlignment)} cannot start AR because AR Session reference is missing.", this);
            _startupRoutine = null;
            yield break;
        }

        SetArCameraComponentsEnabled(false);
        LogArCameraStatus("checking AR camera startup prerequisites");

        if (_requestAndroidCameraPermissionBeforeSessionStart)
        {
            yield return RequestAndroidCameraPermissionIfNeeded();
        }

        if (_requestAndroidCameraPermissionBeforeSessionStart && !HasAndroidCameraPermission())
        {
            _isWaitingForPermissionRetry = true;
            Debug.LogError($"{nameof(SUN_SV_MobileARStageAlignment)} cannot start AR Session because Android camera permission is missing. Grant Camera permission in Android app settings and return to the app to retry.", this);
            _startupRoutine = null;
            yield break;
        }

        SetArCameraComponentsEnabled(true);
        _arSession.enabled = true;
        LogArCameraStatus("enabled AR Session after camera permission check");

        _startupRoutine = null;
    }

    private void SetArCameraComponentsEnabled(bool isEnabled)
    {
        _areArCameraSubsystemsRequestedEnabled = isEnabled;
        bool shouldUseArCameraView = ShouldUseArCameraAsLocalAudienceView();
        bool shouldEnableArSubsystems = shouldUseArCameraView && isEnabled;

        if (_arCamera != null)
        {
            _arCamera.enabled = shouldUseArCameraView;
        }

        if (_arAudioListener != null)
        {
            _arAudioListener.enabled = shouldUseArCameraView;
        }

        if (_arCameraManager != null)
        {
            _arCameraManager.enabled = shouldEnableArSubsystems;
        }

        if (_arCameraBackground != null)
        {
            _arCameraBackground.enabled = shouldEnableArSubsystems;
        }
    }

    private void CacheAudienceRigComponents()
    {
        if (_audienceRigs == null || _audienceRigs.Length == 0)
        {
            // Scene configurator should serialize these references. This fallback keeps hand-edited scenes usable.
            _audienceRigs = FindObjectsByType<SUN_AudienceRig>(FindObjectsInactive.Include);
        }

        Array.Sort(_audienceRigs, CompareAudienceRigsById);

        int rigCount = _audienceRigs != null ? _audienceRigs.Length : 0;
        _audienceCameras = new Camera[rigCount];
        _audienceAudioListeners = new AudioListener[rigCount];
        _audienceViewControllers = new SUN_AudienceViewController[rigCount];

        for (int i = 0; i < rigCount; i++)
        {
            SUN_AudienceRig rig = _audienceRigs[i];
            if (rig == null)
            {
                continue;
            }

            _audienceCameras[i] = rig.GetComponentInChildren<Camera>(true);
            _audienceAudioListeners[i] = rig.GetComponentInChildren<AudioListener>(true);
            _audienceViewControllers[i] = rig.GetComponentInChildren<SUN_AudienceViewController>(true);
        }
    }

    private void CacheNetworkRunners()
    {
        if (_networkRunners != null && _networkRunners.Length > 0)
        {
            return;
        }

        // Runners are scene-level session entry points. Cache once so seat assignment can react after Fusion starts.
        _networkRunners = FindObjectsByType<NetworkRunner>(FindObjectsInactive.Include);
    }

    private static int CompareAudienceRigsById(SUN_AudienceRig left, SUN_AudienceRig right)
    {
        if (left == right)
        {
            return 0;
        }

        if (left == null)
        {
            return 1;
        }

        if (right == null)
        {
            return -1;
        }

        return string.CompareOrdinal(left.AudienceId, right.AudienceId);
    }

    private void UpdateAudienceSeatFromRunningPlayer()
    {
        if (_hasAppliedRunningPlayerSeat)
        {
            return;
        }

        NetworkRunner runningRunner = GetRunningRunner();
        if (runningRunner == null || !runningRunner.LocalPlayer.IsRealPlayer)
        {
            return;
        }

        ApplyAudienceSeatSelection(true);
        _hasAppliedRunningPlayerSeat = true;

        if (_alignXrOriginToStageOriginOnStart)
        {
            AlignXrOriginToStageOrigin();
        }
    }

    private void ApplyAudienceSeatSelection(bool preferRunningPlayer)
    {
        int rigCount = _audienceRigs != null ? _audienceRigs.Length : 0;
        if (rigCount == 0)
        {
            SetArCameraComponentsEnabled(_areArCameraSubsystemsRequestedEnabled);
            return;
        }

        int seatIndex = Mathf.Clamp(_fallbackAudienceSeatIndex, 0, rigCount - 1);
        NetworkRunner runningRunner = preferRunningPlayer ? GetRunningRunner() : null;
        if (runningRunner != null && runningRunner.LocalPlayer.IsRealPlayer)
        {
            seatIndex = ResolveAudienceSeatIndex(runningRunner, rigCount);
        }

        _selectedAudienceSeatIndex = seatIndex;
        _selectedAudienceRig = _audienceRigs[seatIndex];

        for (int i = 0; i < rigCount; i++)
        {
            bool isSelectedRig = i == seatIndex;
            SetAudienceRigViewEnabled(i, isSelectedRig);
        }

        SetArCameraComponentsEnabled(_areArCameraSubsystemsRequestedEnabled);

        if (_logAudienceRigSelection && _selectedAudienceRig != null)
        {
            string runnerPlayer = runningRunner != null ? runningRunner.LocalPlayer.PlayerId.ToString() : "fallback";
            Debug.Log($"{nameof(SUN_SV_MobileARStageAlignment)} selected audience seat index {seatIndex} ({_selectedAudienceRig.AudienceId}) for local player {runnerPlayer}. ARCameraView={ShouldUseArCameraAsLocalAudienceView()}.", this);
        }
    }

    private int ResolveAudienceSeatIndex(NetworkRunner runner, int rigCount)
    {
        if (TryResolveAudienceSeatIndexFromActivePlayers(runner, rigCount, out int activePlayerSeatIndex))
        {
            return activePlayerSeatIndex;
        }

        int zeroBasedSeatIndex = runner.LocalPlayer.PlayerId - Mathf.Max(0, _firstAudiencePlayerIdFallback);

        if (zeroBasedSeatIndex < 0)
        {
            zeroBasedSeatIndex = 0;
        }

        return zeroBasedSeatIndex % rigCount;
    }

    private bool TryResolveAudienceSeatIndexFromActivePlayers(NetworkRunner runner, int rigCount, out int seatIndex)
    {
        seatIndex = 0;
        _activePlayerIds.Clear();

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            if (player.IsRealPlayer)
            {
                _activePlayerIds.Add(player.PlayerId);
            }
        }

        if (_activePlayerIds.Count == 0)
        {
            return false;
        }

        _activePlayerIds.Sort();
        int firstAudiencePlayerListIndex = 0;

        // In the Host + remote Client prototype, the lowest active PlayerId is the director/Host slot.
        // Audience_A starts at the first remote client after that Host slot.
        if (_excludeLowestActivePlayerIdAsHost
            && runner.GameMode == GameMode.Client
            && _activePlayerIds.Count > 1
            && _activePlayerIds[0] != runner.LocalPlayer.PlayerId)
        {
            firstAudiencePlayerListIndex = 1;
        }

        for (int i = firstAudiencePlayerListIndex; i < _activePlayerIds.Count; i++)
        {
            if (_activePlayerIds[i] != runner.LocalPlayer.PlayerId)
            {
                continue;
            }

            seatIndex = (i - firstAudiencePlayerListIndex) % rigCount;
            return true;
        }

        return false;
    }

    private void SetAudienceRigViewEnabled(int rigIndex, bool isSelectedRig)
    {
        bool shouldRenderVirtualAudienceCamera = isSelectedRig && !ShouldUseArCameraAsLocalAudienceView();

        if (_audienceCameras != null && rigIndex < _audienceCameras.Length && _audienceCameras[rigIndex] != null)
        {
            _audienceCameras[rigIndex].enabled = shouldRenderVirtualAudienceCamera;
            _audienceCameras[rigIndex].rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        }

        if (_audienceAudioListeners != null && rigIndex < _audienceAudioListeners.Length && _audienceAudioListeners[rigIndex] != null)
        {
            _audienceAudioListeners[rigIndex].enabled = shouldRenderVirtualAudienceCamera;
        }

        if (_audienceViewControllers != null && rigIndex < _audienceViewControllers.Length && _audienceViewControllers[rigIndex] != null)
        {
            _audienceViewControllers[rigIndex].enabled = shouldRenderVirtualAudienceCamera;
        }
    }

    private NetworkRunner GetRunningRunner()
    {
        if (_networkRunners == null)
        {
            return null;
        }

        for (int i = 0; i < _networkRunners.Length; i++)
        {
            NetworkRunner runner = _networkRunners[i];
            if (runner != null && runner.IsRunning)
            {
                return runner;
            }
        }

        return null;
    }

    private IEnumerator RequestAndroidCameraPermissionIfNeeded()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            Debug.Log($"{nameof(SUN_SV_MobileARStageAlignment)} Android camera permission is already granted.", this);
            yield break;
        }

        bool? isGranted = null;
        PermissionCallbacks callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += permissionName => isGranted = true;
        callbacks.PermissionDenied += permissionName => isGranted = false;
        callbacks.PermissionDeniedAndDontAskAgain += permissionName => isGranted = false;
        callbacks.PermissionRequestDismissed += permissionName => isGranted = false;

        Debug.Log($"{nameof(SUN_SV_MobileARStageAlignment)} requesting Android camera permission before AR Session startup.", this);
        Permission.RequestUserPermission(Permission.Camera, callbacks);

        while (!isGranted.HasValue)
        {
            yield return null;
        }

        Debug.Log($"{nameof(SUN_SV_MobileARStageAlignment)} Android camera permission result: {(isGranted.Value ? "granted" : "denied")}.", this);
#else
        yield break;
#endif
    }

    private bool HasAndroidCameraPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return Permission.HasUserAuthorizedPermission(Permission.Camera);
#else
        return true;
#endif
    }

    private void OnArSessionStateChanged(ARSessionStateChangedEventArgs eventArgs)
    {
        if (!ShouldUseArCameraAsLocalAudienceView() || !_logArCameraStartupState || eventArgs.state == _lastLoggedSessionState)
        {
            return;
        }

        _lastLoggedSessionState = eventArgs.state;
        LogArCameraStatus($"AR Session state changed to {eventArgs.state}");
    }

    private void OnArCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (!ShouldUseArCameraAsLocalAudienceView() || _hasReceivedCameraFrame)
        {
            return;
        }

        _hasReceivedCameraFrame = true;
        LogArCameraStatus("received first AR camera frame");
    }

    private void LogArCameraStatus(string reason)
    {
        if (!_logArCameraStartupState)
        {
            return;
        }

        string permissionStatus = HasAndroidCameraPermission() ? "granted" : "missing";
        string subsystemPermission = _arCameraManager != null ? _arCameraManager.permissionGranted.ToString() : "missing-manager";
        string sessionEnabled = _arSession != null ? _arSession.enabled.ToString() : "missing-session";
        string cameraManagerEnabled = _arCameraManager != null ? _arCameraManager.enabled.ToString() : "missing-manager";
        string backgroundEnabled = _arCameraBackground != null ? _arCameraBackground.enabled.ToString() : "missing-background";
        string renderMode = _arCameraManager != null ? _arCameraManager.currentRenderingMode.ToString() : "unknown";
        string requestedRenderMode = _arCameraManager != null ? _arCameraManager.requestedBackgroundRenderingMode.ToString() : "unknown";
        string facingDirection = _arCameraManager != null ? _arCameraManager.currentFacingDirection.ToString() : "unknown";
        string trackingReason = ARSession.notTrackingReason.ToString();
        GraphicsDeviceType graphicsDeviceType = SystemInfo.graphicsDeviceType;

        Debug.Log(
            $"{nameof(SUN_SV_MobileARStageAlignment)} AR camera status ({reason}) | " +
            $"sessionEnabled={sessionEnabled}, sessionState={ARSession.state}, notTracking={trackingReason}, " +
            $"androidPermission={permissionStatus}, subsystemPermission={subsystemPermission}, " +
            $"cameraManagerEnabled={cameraManagerEnabled}, backgroundEnabled={backgroundEnabled}, " +
            $"renderMode={renderMode}, requestedRenderMode={requestedRenderMode}, facing={facingDirection}, " +
            $"graphics={graphicsDeviceType}, graphicsMT={SystemInfo.graphicsMultiThreaded}, firstFrame={_hasReceivedCameraFrame}.",
            this);
    }

    private bool ShouldUseArCameraAsLocalAudienceView()
    {
        return _useArCameraAsLocalAudienceView
            && Application.platform == RuntimePlatform.Android
            && !Application.isEditor;
    }

    private void ValidateStageObjectHierarchy()
    {
        bool hasIndependentHalo = IsStageObjectIndependentFromCamera();
        if (!hasIndependentHalo)
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARStageAlignment)} expects StageObject_Halo to stay outside the AR Camera hierarchy.", this);
        }

        if (_stageObjectHalo != null && _networkObjectsRoot != null && !_stageObjectHalo.IsChildOf(_networkObjectsRoot))
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARStageAlignment)} expects StageObject_Halo to remain under the network world root.", this);
        }

        if (_networkObjectsRoot != null && _stageWorldRoot != null && !_networkObjectsRoot.IsChildOf(_stageWorldRoot))
        {
            Debug.LogWarning($"{nameof(SUN_SV_MobileARStageAlignment)} expects the network root to remain under the stage world root.", this);
        }

        if (_logAlignmentOnStart)
        {
            string stageOriginName = _stageOrigin != null ? _stageOrigin.name : "Missing";
            string cameraName = _arCamera != null ? _arCamera.name : "Missing";
            Debug.Log($"{nameof(SUN_SV_MobileARStageAlignment)} uses '{stageOriginName}' as the AR world start point. Camera '{cameraName}' moves by AR pose while StageObject_Halo keeps its network world Transform.", this);
        }
    }
}
