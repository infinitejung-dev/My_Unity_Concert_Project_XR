using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 관객이 공유하는 연출 시간을 관리하고 ObjectId별 Stage Space 상태를 평가한다.
/// </summary>
public class SUN_StageEventTimeline : MonoBehaviour
{
    [Tooltip("Inspector에서 편집하는 공통 연출 이벤트 목록입니다.")]
    [SerializeField] private List<SUN_StageEventDefinition> _events = new List<SUN_StageEventDefinition>();

    [Tooltip("켜져 있으면 Start에서 타임라인을 자동 재생합니다.")]
    [SerializeField] private bool _playOnStart;

    [Tooltip("현재 공통 타임라인 시간입니다. 단위는 초입니다.")]
    [SerializeField] private float _timelineTimeSeconds;

    private bool _isPlaying;

    public float TimelineTimeSeconds => _timelineTimeSeconds;
    public bool IsPlaying => _isPlaying;

    private void Awake()
    {
        if (_events == null)
        {
            _events = new List<SUN_StageEventDefinition>();
        }

        _timelineTimeSeconds = Mathf.Max(0.0f, _timelineTimeSeconds);
    }

    private void Start()
    {
        ValidateEvents();

        if (_playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (_isPlaying)
        {
            _timelineTimeSeconds += Time.deltaTime;
        }
    }

    public void Play()
    {
        _isPlaying = true;
    }

    public void Pause()
    {
        _isPlaying = false;
    }

    public void ResetTimeline()
    {
        _timelineTimeSeconds = 0.0f;
        _isPlaying = false;
    }

    /// <summary>
    /// 현재 시간에서 특정 ObjectId의 공통 상태를 계산한다. 관객 위치는 이 계산에 개입하지 않는다.
    /// </summary>
    public bool EvaluateObjectState(string objectId, out SUN_StageObjectState state)
    {
        state = SUN_StageObjectState.Hidden(objectId);

        if (string.IsNullOrWhiteSpace(objectId) || _events == null || _events.Count == 0)
        {
            return false;
        }

        SUN_StageEventDefinition selectedEvent = null;
        SUN_StageEventDefinition earliestFutureEvent = null;
        float selectedStartTimeSeconds = float.MinValue;
        float earliestFutureStartTimeSeconds = float.MaxValue;

        for (int i = 0; i < _events.Count; i++)
        {
            SUN_StageEventDefinition candidate = _events[i];
            if (candidate == null || candidate.ObjectId != objectId)
            {
                continue;
            }

            if (candidate.StartTimeSeconds <= _timelineTimeSeconds && candidate.StartTimeSeconds >= selectedStartTimeSeconds)
            {
                selectedEvent = candidate;
                selectedStartTimeSeconds = candidate.StartTimeSeconds;
            }
            else if (candidate.StartTimeSeconds > _timelineTimeSeconds && candidate.StartTimeSeconds < earliestFutureStartTimeSeconds)
            {
                earliestFutureEvent = candidate;
                earliestFutureStartTimeSeconds = candidate.StartTimeSeconds;
            }
        }

        if (selectedEvent == null)
        {
            if (earliestFutureEvent == null)
            {
                return false;
            }

            // 시작 전 정책: 대상 이벤트가 존재하면 시작 위치를 유지하되 Renderer는 숨긴다.
            state = new SUN_StageObjectState(
                objectId,
                false,
                earliestFutureEvent.StartPositionMeters,
                Quaternion.Euler(earliestFutureEvent.StartEulerDegrees),
                earliestFutureEvent.StartScale,
                0.0f);
            state.EventId = earliestFutureEvent.EventId;
            return true;
        }

        float normalizedProgress = CalculateNormalizedProgress(selectedEvent);
        Vector3 stagePositionMeters = Vector3.Lerp(selectedEvent.StartPositionMeters, selectedEvent.EndPositionMeters, normalizedProgress);
        Quaternion startRotation = Quaternion.Euler(selectedEvent.StartEulerDegrees);
        Quaternion endRotation = Quaternion.Euler(selectedEvent.EndEulerDegrees);
        Quaternion stageRotation = Quaternion.Slerp(startRotation, endRotation, normalizedProgress);
        Vector3 scale = Vector3.Lerp(selectedEvent.StartScale, selectedEvent.EndScale, normalizedProgress);

        state = new SUN_StageObjectState(
            objectId,
            selectedEvent.IsVisible,
            stagePositionMeters,
            stageRotation,
            scale,
            normalizedProgress);
        state.EventId = selectedEvent.EventId;
        return true;
    }

    private float CalculateNormalizedProgress(SUN_StageEventDefinition eventDefinition)
    {
        if (eventDefinition.DurationSeconds <= 0.0f)
        {
            return 1.0f;
        }

        float elapsedSeconds = _timelineTimeSeconds - eventDefinition.StartTimeSeconds;
        return Mathf.Clamp01(elapsedSeconds / eventDefinition.DurationSeconds);
    }

    private void ValidateEvents()
    {
        for (int i = 0; i < _events.Count; i++)
        {
            SUN_StageEventDefinition current = _events[i];
            if (current == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(current.ObjectId))
            {
                Debug.LogWarning($"{nameof(SUN_StageEventTimeline)} has an event with an empty ObjectId at index {i}.", this);
            }

            if (current.DurationSeconds <= 0.0f)
            {
                Debug.LogWarning($"{nameof(SUN_StageEventTimeline)} event {current.EventId} will be treated as an instant event.", this);
            }

            for (int j = i + 1; j < _events.Count; j++)
            {
                SUN_StageEventDefinition other = _events[j];
                if (other != null && current.EventId == other.EventId && !string.IsNullOrWhiteSpace(current.EventId))
                {
                    Debug.LogWarning($"{nameof(SUN_StageEventTimeline)} has duplicate EventId: {current.EventId}.", this);
                    break;
                }
            }
        }
    }
}
