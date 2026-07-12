using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// 표준 AR Foundation 생명주기를 변경하지 않고 ARCameraTest 씬의 세션 상태와 첫 카메라 프레임만 기록한다.
/// </summary>
public sealed class SUN_ARCameraTestDiagnostics : MonoBehaviour
{
    [Tooltip("상태 변화를 관찰할 표준 AR Session입니다.")]
    [SerializeField] private ARSession _arSession;

    [Tooltip("첫 카메라 프레임 이벤트를 관찰할 표준 AR Camera Manager입니다.")]
    [SerializeField] private ARCameraManager _arCameraManager;

    [Tooltip("카메라 자식이 아닌 AR 월드에 배치된 테스트 오브젝트입니다.")]
    [SerializeField] private Transform _worldTestObject;

    [Tooltip("첫 카메라 프레임을 기다릴 시간입니다. 단위는 초입니다.")]
    [SerializeField] private float _firstFrameWarningDelaySeconds = 15.0f;

    private bool _hasReceivedCameraFrame;
    private bool _hasLoggedFrameWarning;
    private float _startedAtUnscaledTime;

    private void Awake()
    {
        _firstFrameWarningDelaySeconds = Mathf.Max(1.0f, _firstFrameWarningDelaySeconds);
    }

    private void OnEnable()
    {
        ARSession.stateChanged += OnArSessionStateChanged;

        if (_arCameraManager != null)
        {
            _arCameraManager.frameReceived += OnCameraFrameReceived;
        }
    }

    private void Start()
    {
        _startedAtUnscaledTime = Time.unscaledTime;

        if (_arSession == null || _arCameraManager == null)
        {
            Debug.LogError($"{nameof(SUN_ARCameraTestDiagnostics)} requires ARSession and ARCameraManager references.", this);
            return;
        }

        if (_worldTestObject == null)
        {
            Debug.LogWarning($"{nameof(SUN_ARCameraTestDiagnostics)} has no world test object reference.", this);
        }

        // Provider getter를 읽지 않고, Android surface와 렌더러 종류만 안전하게 남긴다.
        Debug.Log(
            $"{nameof(SUN_ARCameraTestDiagnostics)} started standard AR Foundation camera test. " +
            $"graphics={SystemInfo.graphicsDeviceType}, screen={Screen.width}x{Screen.height}@{Screen.orientation}, " +
            $"testObjectWorldPosition={(_worldTestObject != null ? _worldTestObject.position.ToString() : "missing")}.",
            this);
    }

    private void Update()
    {
        if (_hasReceivedCameraFrame || _hasLoggedFrameWarning)
        {
            return;
        }

        if (Time.unscaledTime - _startedAtUnscaledTime < _firstFrameWarningDelaySeconds)
        {
            return;
        }

        _hasLoggedFrameWarning = true;
        Debug.LogWarning(
            $"{nameof(SUN_ARCameraTestDiagnostics)} has not received an AR camera frame after {_firstFrameWarningDelaySeconds:F1}s. " +
            $"ARSession.state={ARSession.state}, notTrackingReason={ARSession.notTrackingReason}.",
            this);
    }

    private void OnDisable()
    {
        ARSession.stateChanged -= OnArSessionStateChanged;

        if (_arCameraManager != null)
        {
            _arCameraManager.frameReceived -= OnCameraFrameReceived;
        }
    }

    private void OnArSessionStateChanged(ARSessionStateChangedEventArgs eventArgs)
    {
        Debug.Log(
            $"{nameof(SUN_ARCameraTestDiagnostics)} ARSession.state={eventArgs.state}, notTrackingReason={ARSession.notTrackingReason}.",
            this);
    }

    private void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
    {
        if (_hasReceivedCameraFrame)
        {
            return;
        }

        _hasReceivedCameraFrame = true;
        int textureCount = eventArgs.textures != null ? eventArgs.textures.Count : 0;
        Debug.Log(
            $"{nameof(SUN_ARCameraTestDiagnostics)} received first AR camera frame. textures={textureCount}, " +
            $"ARSession.state={ARSession.state}.",
            this);
    }
}
