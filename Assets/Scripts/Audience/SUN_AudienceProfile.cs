using UnityEngine;

/// <summary>
/// 관객별 좌석, 눈 높이, 디바이스 착용 오프셋, FOV 가정값을 저장하는 테스트 프로필이다.
/// </summary>
[CreateAssetMenu(fileName = "SUN_AudienceProfile", menuName = "SUN/Prototype/Audience Profile")]
public class SUN_AudienceProfile : ScriptableObject
{
    [Tooltip("프로토타입에서 관객 Rig를 구분하기 위한 식별자입니다.")]
    [SerializeField] private string _audienceId = "Audience_A";

    [Tooltip("Stage Space 기준 관객 좌석 또는 스탠딩 기준 위치입니다. 단위는 미터입니다.")]
    [SerializeField] private Vector3 _seatStagePositionMeters = new Vector3(0.0f, 0.0f, -3.0f);

    [Tooltip("좌석 기준 위치에서 관객 눈까지의 수직 높이입니다. 단위는 미터입니다.")]
    [SerializeField] private float _eyeHeightMeters = 1.6f;

    [Tooltip("스마트 글래스 착용으로 생기는 눈 위치 보정값입니다. 단위는 미터입니다.")]
    [SerializeField] private Vector3 _deviceOffsetMeters = Vector3.zero;

    [Tooltip("관객 카메라에 적용할 스마트 글래스 FOV 가정값입니다. 단위는 degree입니다.")]
    [SerializeField] private float _fieldOfViewDegrees = 45.0f;

    public string AudienceId => _audienceId;
    public Vector3 SeatStagePositionMeters => _seatStagePositionMeters;
    public float EyeHeightMeters => _eyeHeightMeters;
    public Vector3 DeviceOffsetMeters => _deviceOffsetMeters;
    public float FieldOfViewDegrees => _fieldOfViewDegrees;
}
