using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.Management;

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

    [Tooltip("If Android reaches AR startup without an active XR loader, initialize XR Management before enabling AR components.")]
    [SerializeField] private bool _initializeXrLoaderIfMissing = true;

    [Tooltip("Check ARCore availability before enabling the AR Session so unsupported devices fail with a clear reason.")]
    [SerializeField] private bool _checkArAvailabilityBeforeSessionStart = true;

    [Tooltip("Keep AR Session disabled at scene start and enable it only after the startup guard completes.")]
    [SerializeField] private bool _enableArSessionAfterPermission = true;

    [Tooltip("Align XR Origin to Stage Origin when the scene starts, and keep it synced to the selected audience eye in Android AR camera runtime.")]
    [SerializeField] private bool _alignXrOriginToStageOriginOnStart = true;

    [Tooltip("Write AR camera startup state logs until the first camera frame arrives.")]
    [SerializeField] private bool _logArCameraStartupState = true;

    [Tooltip("Seconds between repeated AR camera startup status logs.")]
    [SerializeField] private float _startupStatusLogIntervalSeconds = 2.0f;

    [Tooltip("Require a camera frame to include camera textures and a concrete background render mode before treating AR background startup as complete.")]
    [SerializeField] private bool _requireRenderableCameraFrameBeforeReady = true;

    [Tooltip("Seconds after AR startup before writing a stronger warning if no renderable camera background frame has arrived.")]
    [SerializeField] private float _renderableFrameWarningDelaySeconds = 12.0f;

    [Tooltip("Render timing requested from ARCore. Before Opaques keeps the Android camera feed on the simpler background path while startup is validated.")]
    [SerializeField] private CameraBackgroundRenderingMode _requestedBackgroundRenderingMode = CameraBackgroundRenderingMode.BeforeOpaques;

    [Tooltip("Request ARCore electronic image stabilization after the camera feed is proven stable. It uses a provider-specific background mesh path.")]
    [SerializeField] private bool _requestImageStabilization;

    [Tooltip("Lock Android AR startup to one screen orientation so ARCore camera textures and URP render targets do not swap width/height during the first frames.")]
    [SerializeField] private bool _lockAndroidArScreenOrientation = true;

    [Tooltip("Prototype screen orientation used by the Android AR camera path. Unit: Unity screen orientation enum.")]
    [SerializeField] private ScreenOrientation _androidArScreenOrientation = ScreenOrientation.LandscapeLeft;

    [Tooltip("Wait for Screen.width and Screen.height to stop changing before ARCameraManager starts requesting ARCore camera frames.")]
    [SerializeField] private bool _waitForStableScreenDimensionsBeforeCameraEnable = true;

    [Tooltip("Consecutive stable frames required before enabling AR camera components. Unit: rendered frames.")]
    [SerializeField] private int _stableScreenDimensionFrameCount = 3;

    [Tooltip("Maximum time to wait for the Android surface size/orientation to settle before continuing startup. Unit: seconds.")]
    [SerializeField] private float _stableScreenDimensionTimeoutSeconds = 4.0f;

    [Tooltip("Extra frames to wait after AR Session is enabled before ARCameraBackground and ARCameraManager are enabled. Unit: rendered frames.")]
    [SerializeField] private int _arCameraEnableDelayFrames = 2;

    [Tooltip("Force the AR camera onto a fullscreen Base camera path without HDR, MSAA, post processing, or camera stacking.")]
    [SerializeField] private bool _forceSimpleArCameraRenderingPath = true;

    [Tooltip("Restart AR Session and camera components if the camera background stays non-renderable.")]
    [SerializeField] private bool _restartArCameraWhenBackgroundFrameMissing = true;

    [Tooltip("Maximum number of automatic AR camera restarts during startup.")]
    [SerializeField] private int _maxArCameraStartupRestartAttempts = 2;

    [Tooltip("Seconds to wait after disabling AR camera components before retrying startup.")]
    [SerializeField] private float _arCameraRestartCooldownSeconds = 0.5f;

    [Tooltip("Write the stage alignment and Halo hierarchy status when the scene starts.")]
    [SerializeField] private bool _logAlignmentOnStart = true;

    private Coroutine _startupRoutine;
    private bool _hasReceivedCameraFrame;
    private bool _isWaitingForPermissionRetry;
    private bool _hasLoggedInvalidCameraFrame;
    private bool _hasLoggedRenderableFrameTimeout;
    private float _nextStatusLogTime;
    private float _arStartupBeganTime = -1.0f;
    private int _invalidCameraFrameCount;
    private int _arCameraStartupRestartAttemptCount;
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

        if (!_hasLoggedRenderableFrameTimeout
            && _arStartupBeganTime > 0.0f
            && Time.unscaledTime - _arStartupBeganTime >= Mathf.Max(1.0f, _renderableFrameWarningDelaySeconds))
        {
            _hasLoggedRenderableFrameTimeout = true;
            LogArCameraStatus("no renderable AR camera background frame arrived within startup warning delay");
            TryRestartArCameraStartup("renderable AR camera background frame timeout");
        }
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
            ReapplyArCameraRuntimeRequests();
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
        ApplyAndroidArScreenOrientationRequest();
        ApplyArCameraRenderingSafetySettings();
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

        if (_initializeXrLoaderIfMissing)
        {
            yield return EnsureXrLoaderIsReady();
        }

        if (!HasLoadedArCoreSubsystems())
        {
            Debug.LogError($"{nameof(SUN_SV_MobileARStageAlignment)} cannot start AR camera feed because the active XR loader does not provide both XRSessionSubsystem and XRCameraSubsystem. Check Android XR Plug-in Management and rebuild the mobile client.", this);
            LogArCameraStatus("missing ARCore session or camera subsystem before AR Session enable");
            _startupRoutine = null;
            yield break;
        }

        if (_checkArAvailabilityBeforeSessionStart)
        {
            yield return CheckArAvailabilityBeforeSessionStart();

            if (!CanEnableArSessionAfterAvailabilityCheck())
            {
                Debug.LogError($"{nameof(SUN_SV_MobileARStageAlignment)} cannot start AR camera feed because AR availability is {ARSession.state}. Device must support ARCore and complete any required ARCore install/update.", this);
                LogArCameraStatus("AR availability check blocked AR Session startup");
                _startupRoutine = null;
                yield break;
            }
        }

        _hasReceivedCameraFrame = false;
        _hasLoggedInvalidCameraFrame = false;
        _hasLoggedRenderableFrameTimeout = false;
        _invalidCameraFrameCount = 0;
        _arStartupBeganTime = Time.unscaledTime;

        // Let Android finish swapping the app surface to the requested landscape frame before ARCore opens the camera path.
        yield return WaitForStableScreenDimensionsBeforeCameraEnable("before AR Session enable");

        _arSession.enabled = true;
        ReapplyArCameraRuntimeRequests();
        yield return WaitForArSessionReadyForCameraEnable();

        if (!IsArSessionReadyForCameraEnable())
        {
            Debug.LogError($"{nameof(SUN_SV_MobileARStageAlignment)} will keep AR camera components disabled because AR Session did not enter a running update state. ARSession.state={ARSession.state}. This prevents ARCore camera frame requests while the native camera is still null.", this);
            LogArCameraStatus("blocked AR camera component enable because AR Session is not running yet");
            _startupRoutine = null;
            yield break;
        }

        yield return WaitForStableScreenDimensionsBeforeCameraEnable("before AR camera component enable");
        yield return DelayArCameraComponentEnable();

        SetArCameraComponentsEnabled(true);
        ReapplyArCameraRuntimeRequests();
        LogArCameraStatus("enabled AR Session and AR camera background after camera permission check");

        _startupRoutine = null;
    }

    private IEnumerator EnsureXrLoaderIsReady()
    {
        XRManagerSettings xrManager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        if (xrManager == null)
        {
            Debug.LogError($"{nameof(SUN_SV_MobileARStageAlignment)} cannot initialize AR because XRGeneralSettings or XRManagerSettings is missing for Android.", this);
            yield break;
        }

        if (xrManager.activeLoader != null)
        {
            yield break;
        }

        // AR Foundation managers only start already-created subsystems. If automatic XR loading did not run,
        // initialize the configured Android ARCore loader here before enabling ARSession/ARCameraManager.
        Debug.LogWarning($"{nameof(SUN_SV_MobileARStageAlignment)} found no active XR loader at AR startup and will initialize XR Management manually.", this);
        yield return xrManager.InitializeLoader();

        if (xrManager.activeLoader == null)
        {
            Debug.LogError($"{nameof(SUN_SV_MobileARStageAlignment)} failed to initialize an Android XR loader. ARCoreLoader may be missing from Android XR Plug-in Management or incompatible with the current Graphics API.", this);
            yield break;
        }

        if (xrManager.automaticRunning)
        {
            xrManager.StartSubsystems();
        }

        LogArCameraStatus("initialized XR loader before AR Session startup");
    }

    private bool HasLoadedArCoreSubsystems()
    {
        XRLoader activeLoader = XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager != null
            ? XRGeneralSettings.Instance.Manager.activeLoader
            : null;

        if (activeLoader == null)
        {
            return false;
        }

        XRSessionSubsystem sessionSubsystem = activeLoader.GetLoadedSubsystem<XRSessionSubsystem>();
        XRCameraSubsystem cameraSubsystem = activeLoader.GetLoadedSubsystem<XRCameraSubsystem>();
        return sessionSubsystem != null && cameraSubsystem != null;
    }

    private IEnumerator CheckArAvailabilityBeforeSessionStart()
    {
        if (ARSession.state <= ARSessionState.CheckingAvailability)
        {
            LogArCameraStatus("checking ARCore availability before AR Session startup");
            yield return ARSession.CheckAvailability();
        }

        if ((ARSession.state == ARSessionState.NeedsInstall && _arSession != null && _arSession.attemptUpdate)
            || ARSession.state == ARSessionState.Installing)
        {
            LogArCameraStatus("installing or updating ARCore before AR Session startup");
            yield return ARSession.Install();
        }
    }

    private static bool CanEnableArSessionAfterAvailabilityCheck()
    {
        return ARSession.state == ARSessionState.Ready
            || ARSession.state == ARSessionState.SessionInitializing
            || ARSession.state == ARSessionState.SessionTracking;
    }

    private void SetArCameraComponentsEnabled(bool isEnabled)
    {
        _areArCameraSubsystemsRequestedEnabled = isEnabled;
        bool shouldUseArCameraView = ShouldUseArCameraAsLocalAudienceView();
        bool shouldEnableArSubsystems = shouldUseArCameraView && isEnabled;
        ApplyArCameraRenderingSafetySettings();

        if (_arCamera != null)
        {
            _arCamera.enabled = shouldEnableArSubsystems;
        }

        if (_arAudioListener != null)
        {
            _arAudioListener.enabled = shouldEnableArSubsystems;
        }

        if (!shouldEnableArSubsystems)
        {
            if (_arCameraBackground != null)
            {
                _arCameraBackground.enabled = false;
            }

            if (_arCameraManager != null)
            {
                _arCameraManager.enabled = false;
            }

            return;
        }

        if (_arCameraBackground != null)
        {
            _arCameraBackground.enabled = true;
        }

        if (_arCameraManager != null)
        {
            // Subscribe ARCameraBackground before ARCameraManager can emit the first camera texture frame.
            _arCameraManager.enabled = true;
        }
    }

    private void ReapplyArCameraRuntimeRequests()
    {
        ApplyAndroidArScreenOrientationRequest();
        ApplyArCameraRenderingSafetySettings();

        if (_arCameraManager == null)
        {
            return;
        }

        // The prototype requests a deterministic background path so black-feed diagnosis is not hidden behind provider choice.
        _arCameraManager.requestedBackgroundRenderingMode = _requestedBackgroundRenderingMode;
        _arCameraManager.requestedFacingDirection = CameraFacingDirection.World;
        _arCameraManager.autoFocusRequested = true;
        _arCameraManager.imageStabilizationRequested = ShouldRequestImageStabilization();
    }

    private bool ShouldRequestImageStabilization()
    {
        if (!_requestImageStabilization || _arCameraManager == null || _arCameraManager.descriptor == null)
        {
            return false;
        }

        // Image stabilization uses an ARCore-specific background mesh path, so request it only when explicitly enabled and supported.
        return _arCameraManager.descriptor.supportsImageStabilization == Supported.Supported;
    }

    private void ApplyAndroidArScreenOrientationRequest()
    {
        if (!ShouldUseArCameraAsLocalAudienceView() || !_lockAndroidArScreenOrientation)
        {
            return;
        }

        // Startup diagnostic from the device showed portrait and landscape targets in the same render pass.
        // Locking one orientation keeps ARCore camera params and URP target allocation in the same coordinate frame.
        ConfigureAllowedRuntimeOrientations(_androidArScreenOrientation);
        if (Screen.orientation != _androidArScreenOrientation)
        {
            Screen.orientation = _androidArScreenOrientation;
        }
    }

    private static void ConfigureAllowedRuntimeOrientations(ScreenOrientation orientation)
    {
        bool allowPortrait = orientation == ScreenOrientation.Portrait || orientation == ScreenOrientation.AutoRotation;
        bool allowPortraitUpsideDown = orientation == ScreenOrientation.PortraitUpsideDown || orientation == ScreenOrientation.AutoRotation;
        bool allowLandscapeLeft = orientation == ScreenOrientation.LandscapeLeft || orientation == ScreenOrientation.AutoRotation;
        bool allowLandscapeRight = orientation == ScreenOrientation.LandscapeRight || orientation == ScreenOrientation.AutoRotation;

        Screen.autorotateToPortrait = allowPortrait;
        Screen.autorotateToPortraitUpsideDown = allowPortraitUpsideDown;
        Screen.autorotateToLandscapeLeft = allowLandscapeLeft;
        Screen.autorotateToLandscapeRight = allowLandscapeRight;
    }

    private void ApplyArCameraRenderingSafetySettings()
    {
        if (!_forceSimpleArCameraRenderingPath || _arCamera == null)
        {
            return;
        }

        // The AR camera must stay a single fullscreen render target. Post effects and stacked overlays are
        // intentionally disabled while validating the phone camera feed on real Android hardware.
        _arCamera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
        _arCamera.targetTexture = null;
        _arCamera.clearFlags = CameraClearFlags.SolidColor;
        _arCamera.backgroundColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
        _arCamera.allowHDR = false;
        _arCamera.allowMSAA = false;
        _arCamera.allowDynamicResolution = false;
        _arCamera.forceIntoRenderTexture = false;

        UniversalAdditionalCameraData cameraData = _arCamera.GetUniversalAdditionalCameraData();
        cameraData.renderType = CameraRenderType.Base;
        List<Camera> cameraStack = cameraData.cameraStack;
        if (cameraStack != null)
        {
            cameraStack.Clear();
        }

        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.None;
        cameraData.allowHDROutput = false;
        cameraData.requiresDepthTexture = false;
        cameraData.requiresColorTexture = false;
        cameraData.SetRenderer(0);
    }

    private IEnumerator WaitForArSessionReadyForCameraEnable()
    {
        float timeoutAt = Time.unscaledTime + 2.0f;
        while (!IsArSessionReadyForCameraEnable() && Time.unscaledTime < timeoutAt)
        {
            ApplyArCameraRenderingSafetySettings();
            yield return null;
        }

        if (!IsArSessionReadyForCameraEnable())
        {
            LogArCameraStatus("continued AR camera startup after AR Session readiness wait timed out");
        }
    }

    private bool IsArSessionReadyForCameraEnable()
    {
        bool isSessionSubsystemRunning = _arSession != null
            && _arSession.subsystem != null
            && _arSession.subsystem.running;

        // Ready only means ARCore availability/install checks passed. Wait until ARSession.Update has
        // advanced the native session so ARCore has a camera frame before ARCameraManager asks for it.
        bool hasSessionUpdateFrame = ARSession.state == ARSessionState.SessionInitializing
            || ARSession.state == ARSessionState.SessionTracking;
        return isSessionSubsystemRunning && hasSessionUpdateFrame;
    }

    private IEnumerator WaitForStableScreenDimensionsBeforeCameraEnable(string startupPhase)
    {
        if (!_waitForStableScreenDimensionsBeforeCameraEnable)
        {
            yield break;
        }

        int requiredStableFrameCount = Mathf.Max(1, _stableScreenDimensionFrameCount);
        float timeoutAt = Time.unscaledTime + Mathf.Max(0.1f, _stableScreenDimensionTimeoutSeconds);
        int previousWidthPixels = Screen.width;
        int previousHeightPixels = Screen.height;
        int stableFrameCount = 0;

        while (Time.unscaledTime < timeoutAt)
        {
            ApplyArCameraRenderingSafetySettings();

            bool hasSameDimensions = Screen.width == previousWidthPixels && Screen.height == previousHeightPixels;
            bool hasRequestedOrientationDimensions = DoesScreenMatchRequestedOrientationDimensions();
            stableFrameCount = hasSameDimensions && hasRequestedOrientationDimensions ? stableFrameCount + 1 : 0;

            if (stableFrameCount >= requiredStableFrameCount)
            {
                yield break;
            }

            previousWidthPixels = Screen.width;
            previousHeightPixels = Screen.height;
            yield return null;
        }

        LogArCameraStatus($"continued AR camera startup after screen dimension stabilization wait timed out ({startupPhase})");
    }

    private bool DoesScreenMatchRequestedOrientationDimensions()
    {
        if (!_lockAndroidArScreenOrientation || _androidArScreenOrientation == ScreenOrientation.AutoRotation)
        {
            return true;
        }

        bool isPortraitRequest = _androidArScreenOrientation == ScreenOrientation.Portrait
            || _androidArScreenOrientation == ScreenOrientation.PortraitUpsideDown;
        bool isLandscapeRequest = _androidArScreenOrientation == ScreenOrientation.LandscapeLeft
            || _androidArScreenOrientation == ScreenOrientation.LandscapeRight;

        if (isPortraitRequest)
        {
            return Screen.height >= Screen.width;
        }

        return !isLandscapeRequest || Screen.width >= Screen.height;
    }

    private IEnumerator DelayArCameraComponentEnable()
    {
        int delayFrameCount = Mathf.Max(0, _arCameraEnableDelayFrames);
        for (int i = 0; i < delayFrameCount; i++)
        {
            ApplyArCameraRenderingSafetySettings();
            yield return null;
        }
    }

    private void TryRestartArCameraStartup(string reason)
    {
        if (!_restartArCameraWhenBackgroundFrameMissing || _startupRoutine != null || _hasReceivedCameraFrame)
        {
            return;
        }

        int maxRestartAttempts = Mathf.Max(0, _maxArCameraStartupRestartAttempts);
        if (_arCameraStartupRestartAttemptCount >= maxRestartAttempts)
        {
            Debug.LogError($"{nameof(SUN_SV_MobileARStageAlignment)} exhausted AR camera startup restarts after {_arCameraStartupRestartAttemptCount} attempt(s). Check Android logs for ARCore permission, graphics API, and camera subsystem errors.", this);
            return;
        }

        _arCameraStartupRestartAttemptCount++;
        _startupRoutine = StartCoroutine(RestartArCameraStartupAfterBackgroundFailure(reason, _arCameraStartupRestartAttemptCount));
    }

    private IEnumerator RestartArCameraStartupAfterBackgroundFailure(string reason, int attemptNumber)
    {
        Debug.LogWarning($"{nameof(SUN_SV_MobileARStageAlignment)} restarting AR camera startup because {reason}. Attempt {attemptNumber}/{Mathf.Max(0, _maxArCameraStartupRestartAttempts)}.", this);

        SetArCameraComponentsEnabled(false);

        if (_arSession != null)
        {
            _arSession.enabled = false;
            yield return null;
            _arSession.Reset();
        }

        yield return new WaitForSeconds(Mathf.Max(0.1f, _arCameraRestartCooldownSeconds));

        _hasReceivedCameraFrame = false;
        _hasLoggedInvalidCameraFrame = false;
        _hasLoggedRenderableFrameTimeout = false;
        _invalidCameraFrameCount = 0;
        _arStartupBeganTime = -1.0f;
        _startupRoutine = null;
        BeginArCameraStartup();
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

        int textureCount = eventArgs.textures != null ? eventArgs.textures.Count : 0;
        bool hasConcreteRenderMode = _arCameraManager != null
            && _arCameraManager.currentRenderingMode != XRCameraBackgroundRenderingMode.None;
        bool hasBackgroundMaterial = _arCameraBackground != null && _arCameraBackground.material != null;
        bool isRenderableCameraFrame = textureCount > 0 && hasConcreteRenderMode && hasBackgroundMaterial;

        if (_requireRenderableCameraFrameBeforeReady && !isRenderableCameraFrame)
        {
            _invalidCameraFrameCount++;
            if (!_hasLoggedInvalidCameraFrame)
            {
                _hasLoggedInvalidCameraFrame = true;
                LogArCameraStatus($"received non-renderable AR camera frame; textures={textureCount}, renderModeReady={hasConcreteRenderMode}, materialReady={hasBackgroundMaterial}");
            }

            return;
        }

        _hasReceivedCameraFrame = true;
        _arCameraStartupRestartAttemptCount = 0;
        LogArCameraStatus($"received first renderable AR camera background frame; textures={textureCount}, invalidFramesBeforeReady={_invalidCameraFrameCount}");
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
        string backgroundRenderingEnabled = _arCameraBackground != null ? _arCameraBackground.backgroundRenderingEnabled.ToString() : "missing-background";
        string backgroundMaterial = _arCameraBackground != null ? (_arCameraBackground.material != null ? _arCameraBackground.material.name : "missing-material") : "missing-background";
        string cameraEnabled = _arCamera != null ? _arCamera.enabled.ToString() : "missing-camera";
        string cameraClearFlags = _arCamera != null ? _arCamera.clearFlags.ToString() : "missing-camera";
        string renderMode = _arCameraManager != null ? _arCameraManager.currentRenderingMode.ToString() : "unknown";
        string requestedRenderMode = _arCameraManager != null ? _arCameraManager.requestedBackgroundRenderingMode.ToString() : "unknown";
        string supportedRenderMode = _arCameraManager != null && _arCameraManager.subsystem != null
            ? _arCameraManager.subsystem.supportedCameraBackgroundRenderingMode.ToString()
            : "missing-subsystem";
        string facingDirection = _arCameraManager != null ? _arCameraManager.currentFacingDirection.ToString() : "unknown";
        string imageStabilizationRequested = _arCameraManager != null ? _arCameraManager.imageStabilizationRequested.ToString() : "missing-manager";
        string imageStabilizationEnabled = _arCameraManager != null ? _arCameraManager.imageStabilizationEnabled.ToString() : "missing-manager";
        string imageStabilizationSupport = _arCameraManager != null && _arCameraManager.descriptor != null
            ? _arCameraManager.descriptor.supportsImageStabilization.ToString()
            : "missing-descriptor";
        string screenSummary = $"{Screen.width}x{Screen.height}@{Screen.orientation}";
        string arCameraRenderSettings = BuildArCameraRenderSettingsSummary();
        string trackingReason = ARSession.notTrackingReason.ToString();
        GraphicsDeviceType graphicsDeviceType = SystemInfo.graphicsDeviceType;
        string xrLoader = GetActiveXRLoaderName();
        string cameraSummary = BuildEnabledCameraSummary();

        Debug.Log(
            $"{nameof(SUN_SV_MobileARStageAlignment)} AR camera status ({reason}) | " +
            $"sessionEnabled={sessionEnabled}, sessionState={ARSession.state}, notTracking={trackingReason}, " +
            $"androidPermission={permissionStatus}, subsystemPermission={subsystemPermission}, " +
            $"cameraEnabled={cameraEnabled}, cameraManagerEnabled={cameraManagerEnabled}, backgroundEnabled={backgroundEnabled}, " +
            $"backgroundRenderingEnabled={backgroundRenderingEnabled}, backgroundMaterial={backgroundMaterial}, clearFlags={cameraClearFlags}, " +
            $"renderMode={renderMode}, requestedRenderMode={requestedRenderMode}, configuredRequest={_requestedBackgroundRenderingMode}, supportedRenderMode={supportedRenderMode}, facing={facingDirection}, " +
            $"imageStabilizationRequested={imageStabilizationRequested}, imageStabilizationEnabled={imageStabilizationEnabled}, imageStabilizationSupport={imageStabilizationSupport}, " +
            $"screen={screenSummary}, orientationLock={_lockAndroidArScreenOrientation}:{_androidArScreenOrientation}, arCameraRender={arCameraRenderSettings}, " +
            $"graphics={graphicsDeviceType}, graphicsMT={SystemInfo.graphicsMultiThreaded}, xrLoader={xrLoader}, " +
            $"firstRenderableFrame={_hasReceivedCameraFrame}, restartAttempts={_arCameraStartupRestartAttemptCount}, enabledCameras={cameraSummary}.",
            this);
    }

    private string BuildArCameraRenderSettingsSummary()
    {
        if (_arCamera == null)
        {
            return "missing-camera";
        }

        string postProcessing = "missing-urp-data";
        string cameraStack = "missing-urp-data";
        string allowHdrOutput = "missing-urp-data";

        if (_arCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
        {
            postProcessing = cameraData.renderPostProcessing.ToString();
            List<Camera> stackedCameras = cameraData.cameraStack;
            cameraStack = stackedCameras != null ? stackedCameras.Count.ToString() : "unsupported";
            allowHdrOutput = cameraData.allowHDROutput.ToString();
        }

        return $"hdr={_arCamera.allowHDR},msaa={_arCamera.allowMSAA},dynamicResolution={_arCamera.allowDynamicResolution},post={postProcessing},hdrOutput={allowHdrOutput},stack={cameraStack}";
    }

    private static string GetActiveXRLoaderName()
    {
        XRManagerSettings manager = XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;
        XRLoader activeLoader = manager != null ? manager.activeLoader : null;
        return activeLoader != null ? activeLoader.GetType().Name : "none";
    }

    private static string BuildEnabledCameraSummary()
    {
        int cameraCount = Camera.allCamerasCount;
        if (cameraCount == 0)
        {
            return "0/0";
        }

        Camera[] cameras = new Camera[cameraCount];
        int writtenCount = Camera.GetAllCameras(cameras);
        int enabledCount = 0;
        string summary = string.Empty;

        for (int i = 0; i < writtenCount; i++)
        {
            Camera camera = cameras[i];
            if (camera == null || !camera.enabled)
            {
                continue;
            }

            if (enabledCount < 4)
            {
                summary += enabledCount == 0 ? camera.name : $",{camera.name}";
            }

            enabledCount++;
        }

        if (enabledCount > 4)
        {
            summary += ",...";
        }

        return $"{enabledCount}/{writtenCount}[{summary}]";
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
