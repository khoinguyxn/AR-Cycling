using System;
using UnityEngine;

public class ReactionTimeTracker : MonoBehaviour
{
    //ATTRIBUTES
    [SerializeField] private AudioManager audioManager;

    private CsvExporter _reactionTimeExporter;
    private bool _waitingForResponse = false;
    private float _currentAudioPlayRealtime = -1f;
    private string _currentAudioPlayTimestampLocal = "";

    [Header("Export Settings")]
    [SerializeField] private string exportFileName = "reaction-time";
    [SerializeField] private float exportInterval = 1f;

    //METHODS
    private void Awake()
    {
        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var reactionTimeFilePath = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";
        const string csvHeader = "AudioPlayTimestampLocal,ButtonPressTimestampLocal,ReactionTimeMs,AudioType,ResponseType";

        _reactionTimeExporter = new CsvExporter(reactionTimeFilePath, exportInterval, csvHeader);

        Debug.Log($"Exporting reaction time data to {reactionTimeFilePath}");
    }

    private void Update()
    {
        CheckForAudioPlay();
        CheckForButtonPress();

        _reactionTimeExporter.ExportRecentData();
    }

    private void CheckForAudioPlay()
    {
        if (audioManager == null) return;

        float audioPlayRealtime = audioManager.LastAudioPlayRealtime;

        // New audio has been played
        if (audioPlayRealtime > 0 && !Mathf.Approximately(audioPlayRealtime, _currentAudioPlayRealtime))
        {
            if (_waitingForResponse)
            {
                RecordMissedResponse();
            }

            _currentAudioPlayRealtime = audioPlayRealtime;
            _currentAudioPlayTimestampLocal = audioManager.LastAudioPlayTimestampLocal;
            _waitingForResponse = true;
        }
    }

    private void CheckForButtonPress()
    {
        // Detect 'A' key press from Bluetooth button
        if (Input.GetKeyDown(KeyCode.A))
        {
            float buttonPressRealtime = Time.realtimeSinceStartup;
            string buttonPressTimestampLocal = DateTimeOffset.Now.ToString("O");

            // The first button press after an audio consumes that audio event.
            if (_waitingForResponse)
            {
                float reactionTime = (buttonPressRealtime - _currentAudioPlayRealtime) * 1000; // Convert to milliseconds

                _reactionTimeExporter.AddData(new ReactionTimeDatum
                {
                    AudioPlayTimestampLocal = _currentAudioPlayTimestampLocal,
                    ButtonPressTimestampLocal = buttonPressTimestampLocal,
                    ReactionTimeMs = reactionTime,
                    AudioType = "Audio",
                    ResponseType = "ValidResponse"
                }.ToString());

                ClearCurrentAudioState();

                Debug.Log($"Valid reaction: {reactionTime:F2}ms");
            }
            else
            {
                // False positive (button pressed without a pending audio event)
                _reactionTimeExporter.AddData(new ReactionTimeDatum
                {
                    AudioPlayTimestampLocal = "",
                    ButtonPressTimestampLocal = buttonPressTimestampLocal,
                    ReactionTimeMs = -1,
                    AudioType = "None",
                    ResponseType = "FalsePositive"
                }.ToString());

                Debug.Log("False positive: Button pressed without audio");
            }
        }
    }

    private void RecordMissedResponse()
    {
        _reactionTimeExporter.AddData(new ReactionTimeDatum
        {
            AudioPlayTimestampLocal = _currentAudioPlayTimestampLocal,
            ButtonPressTimestampLocal = "",
            ReactionTimeMs = -1,
            AudioType = "Audio",
            ResponseType = "MissedResponse"
        }.ToString());

        ClearCurrentAudioState();

        Debug.Log("Missed response: New audio played before any button press");
    }

    private void ClearCurrentAudioState()
    {
        _waitingForResponse = false;
        _currentAudioPlayRealtime = -1f;
        _currentAudioPlayTimestampLocal = "";
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
    public string AudioPlayTimestampLocal { get; set; }
    public string ButtonPressTimestampLocal { get; set; }
    public float ReactionTimeMs { get; set; }
    public string AudioType { get; set; }
    public string ResponseType { get; set; }

    public override string ToString()
    {
        return $"{AudioPlayTimestampLocal ?? ""},{ButtonPressTimestampLocal ?? ""},{ReactionTimeMs:F2},{AudioType},{ResponseType}";
    }
}
