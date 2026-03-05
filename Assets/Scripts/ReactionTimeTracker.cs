using System;
using UnityEngine;

public class ReactionTimeTracker : MonoBehaviour
{
    //ATTRIBUTES
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private float reactionTimeoutWindow = 6f; // 6 seconds window for valid reactions

    private CsvExporter _reactionTimeExporter;
    private bool _waitingForResponse = false;
    private float _currentAudioPlayRealtime = -1f;
    private string _currentAudioPlayTimestampUtc = "";

    [Header("Export Settings")]
    [SerializeField] private string exportFileName = "reaction-time";
    [SerializeField] private float exportInterval = 1f;

    //METHODS
    private void Awake()
    {
        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var reactionTimeFilePath = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";
        const string csvHeader = "AudioPlayTimestampUtc,ButtonPressTimestampUtc,ReactionTimeMs,AudioType,ResponseType";

        _reactionTimeExporter = new CsvExporter(reactionTimeFilePath, exportInterval, csvHeader);

        Debug.Log($"Exporting reaction time data to {reactionTimeFilePath}");
    }

    private void Update()
    {
        CheckForAudioPlay();
        CheckForButtonPress();
        CheckForMissedResponse();

        _reactionTimeExporter.ExportRecentData();
    }

    private void CheckForAudioPlay()
    {
        if (audioManager == null) return;

        float audioPlayRealtime = audioManager.LastAudioPlayRealtime;

        // New audio has been played
        if (audioPlayRealtime > 0 && !Mathf.Approximately(audioPlayRealtime, _currentAudioPlayRealtime))
        {
            _currentAudioPlayRealtime = audioPlayRealtime;
            _currentAudioPlayTimestampUtc = audioManager.LastAudioPlayTimestampUtc;
            _waitingForResponse = true;
        }
    }

    private void CheckForButtonPress()
    {
        // Detect 'A' key press from Bluetooth button
        if (Input.GetKeyDown(KeyCode.A))
        {
            float buttonPressRealtime = Time.realtimeSinceStartup;
            string buttonPressTimestampUtc = DateTimeOffset.UtcNow.ToString("O");

            // Check if this is a response to recent audio
            if (_waitingForResponse && (buttonPressRealtime - _currentAudioPlayRealtime) <= reactionTimeoutWindow)
            {
                // Valid reaction
                float reactionTime = (buttonPressRealtime - _currentAudioPlayRealtime) * 1000; // Convert to milliseconds

                _reactionTimeExporter.AddData(new ReactionTimeDatum
                {
                    AudioPlayTimestampUtc = _currentAudioPlayTimestampUtc,
                    ButtonPressTimestampUtc = buttonPressTimestampUtc,
                    ReactionTimeMs = reactionTime,
                    AudioType = "Audio",
                    ResponseType = "ValidResponse"
                }.ToString());

                _waitingForResponse = false;

                Debug.Log($"Valid reaction: {reactionTime:F2}ms");
            }
            else if (_currentAudioPlayRealtime > 0 && (buttonPressRealtime - _currentAudioPlayRealtime) > reactionTimeoutWindow)
            {
                // Late response (outside timeout window)
                float reactionTime = (buttonPressRealtime - _currentAudioPlayRealtime) * 1000;

                _reactionTimeExporter.AddData(new ReactionTimeDatum
                {
                    AudioPlayTimestampUtc = _currentAudioPlayTimestampUtc,
                    ButtonPressTimestampUtc = buttonPressTimestampUtc,
                    ReactionTimeMs = reactionTime,
                    AudioType = "Audio",
                    ResponseType = "LateResponse"
                }.ToString());

                Debug.Log($"Late response: {reactionTime:F2}ms (outside {reactionTimeoutWindow}s window)");
            }
            else
            {
                // False positive (button pressed without recent audio)
                _reactionTimeExporter.AddData(new ReactionTimeDatum
                {
                    AudioPlayTimestampUtc = _currentAudioPlayRealtime > 0 ? _currentAudioPlayTimestampUtc : "",
                    ButtonPressTimestampUtc = buttonPressTimestampUtc,
                    ReactionTimeMs = -1,
                    AudioType = "None",
                    ResponseType = "FalsePositive"
                }.ToString());

                Debug.Log("False positive: Button pressed without audio");
            }
        }
    }

    private void CheckForMissedResponse()
    {
        // Check if audio was played but no response within timeout window
        if (_waitingForResponse && (Time.realtimeSinceStartup - _currentAudioPlayRealtime) > reactionTimeoutWindow)
        {
            _reactionTimeExporter.AddData(new ReactionTimeDatum
            {
                AudioPlayTimestampUtc = _currentAudioPlayTimestampUtc,
                ButtonPressTimestampUtc = "",
                ReactionTimeMs = -1,
                AudioType = "Audio",
                ResponseType = "MissedResponse"
            }.ToString());

            _waitingForResponse = false;

            Debug.Log($"Missed response: No button press within {reactionTimeoutWindow}s");
        }
    }

    private void OnDestroy()
    {
        if (_reactionTimeExporter != null && _reactionTimeExporter.BufferCount > 0)
        {
            _reactionTimeExporter.ForceFlush();
        }
    }
}


internal record ReactionTimeDatum
{
    public string AudioPlayTimestampUtc { get; set; }
    public string ButtonPressTimestampUtc { get; set; }
    public float ReactionTimeMs { get; set; }
    public string AudioType { get; set; }
    public string ResponseType { get; set; }

    public override string ToString()
    {
        return $"{AudioPlayTimestampUtc ?? ""},{ButtonPressTimestampUtc ?? ""},{ReactionTimeMs:F2},{AudioType},{ResponseType}";
    }
}
