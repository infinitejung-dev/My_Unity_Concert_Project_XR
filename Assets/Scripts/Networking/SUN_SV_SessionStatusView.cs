using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the current Fusion rehearsal role, room name, and connection state on a small session HUD.
/// </summary>
[DisallowMultipleComponent]
public class SUN_SV_SessionStatusView : MonoBehaviour
{
    [Tooltip("Text field that shows whether this device is Host or Client.")]
    // Role label helps testers confirm whether a device owns or joins the concert stage session.
    [SerializeField] private Text _roleText;

    [Tooltip("Text field that shows the shared Photon Fusion session name.")]
    // Room label confirms every device is targeting the same prototype room.
    [SerializeField] private Text _roomText;

    [Tooltip("Text field that shows Connecting, Connected, Failed, or Idle plus a short reason.")]
    // Connection label exposes the current join state and useful failure reason during rehearsal.
    [SerializeField] private Text _connectionText;

    private void Awake()
    {
        CacheTextReferencesIfNeeded();
    }

    private void Start()
    {
        if (_roleText != null && string.IsNullOrWhiteSpace(_roleText.text))
        {
            _roleText.text = "Role: Unknown";
        }

        if (_roomText != null && string.IsNullOrWhiteSpace(_roomText.text))
        {
            _roomText.text = "Room: SV_Prototype_Room";
        }

        if (_connectionText != null && string.IsNullOrWhiteSpace(_connectionText.text))
        {
            _connectionText.text = "Connection: Idle";
        }
    }

    public void SetIdle(string role, string roomName, string reason)
    {
        SetStatus(role, roomName, string.IsNullOrWhiteSpace(reason) ? "Idle" : $"Idle - {reason}");
    }

    public void SetConnecting(string role, string roomName)
    {
        SetStatus(role, roomName, "Connecting");
    }

    public void SetConnected(string role, string roomName, string detail)
    {
        SetStatus(role, roomName, string.IsNullOrWhiteSpace(detail) ? "Connected" : $"Connected - {detail}");
    }

    public void SetFailed(string role, string roomName, string reason)
    {
        SetStatus(role, roomName, string.IsNullOrWhiteSpace(reason) ? "Failed" : $"Failed - {reason}");
    }

    private void SetStatus(string role, string roomName, string connectionState)
    {
        if (_roleText != null)
        {
            _roleText.text = $"Role: {role}";
        }

        if (_roomText != null)
        {
            _roomText.text = $"Room: {roomName}";
        }

        if (_connectionText != null)
        {
            _connectionText.text = $"Connection: {connectionState}";
        }
    }

    private void CacheTextReferencesIfNeeded()
    {
        if (_roleText != null && _roomText != null && _connectionText != null)
        {
            return;
        }

        Text[] childTexts = GetComponentsInChildren<Text>(true);
        for (int i = 0; i < childTexts.Length; i++)
        {
            Text childText = childTexts[i];
            if (childText == null)
            {
                continue;
            }

            if (_roleText == null && childText.name == "RoleText")
            {
                _roleText = childText;
            }
            else if (_roomText == null && childText.name == "RoomText")
            {
                _roomText = childText;
            }
            else if (_connectionText == null && childText.name == "ConnectionText")
            {
                _connectionText = childText;
            }
        }
    }
}
