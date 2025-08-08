using System;
using UnityEngine;

public class ReactionTimeTracker : MonoBehaviour
{
    [SerializeField] private SpawnNotification spawnNotification;

    [Header("Export Settings")]
    [SerializeField]
    private string exportFileName = "reaction-time";

    [SerializeField] private float exportInterval = 1f;
    private CsvExporter _reactionTimeExporter;

    [SerializeField] private AudioSource audioSource;

    private string _inputBuffer = "";

    private void Awake()
    {
        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var reactionTimeFilePath = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";
        const string csvHeader = "AudioPlayedTime (s),ReactionTime (s)";

        _reactionTimeExporter = new CsvExporter(reactionTimeFilePath, exportInterval, csvHeader);

        Debug.Log($"Exporting reaction time data to {reactionTimeFilePath}");
    }

    private void Update()
    {
        foreach (var c in Input.inputString)
        {
            switch (c)
            {
                case '\n' or '\r':
                    // End of line - process the complete message
                    ProcessArduinoMessage(_inputBuffer.Trim());
                    _inputBuffer = "";
                    break;
                case '\b':
                    {
                        // Backspace
                        if (_inputBuffer.Length > 0)
                            _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                        break;
                    }
                // Only printable characters
                case >= ' ':
                    _inputBuffer += c;
                    break;
            }
        }

        _reactionTimeExporter.ExportRecentData();
    }

    private void ProcessArduinoMessage(string message)
    {
        switch (message)
        {
            case "READY":
                audioSource.Play();
                break;
            case "BUTTON_PRESSED":
                HandleButtonPress();
                break;
            default:
                Debug.Log($"Unknown message from Arduino: {message}");
                break;
        }
    }

    private void HandleButtonPress()
    {
        var now = Time.time;
        var reactionTime = now - spawnNotification.LastAudioSorurcePlayedTime;

        _reactionTimeExporter.AddData(new ReactionTimeDatum
        {
            AudioPlayedTime = spawnNotification.LastAudioSorurcePlayedTime,
            ReactionTime = reactionTime
        }.ToString());
    }
}

internal record ReactionTimeDatum
{
    public float AudioPlayedTime { get; set; }
    public float ReactionTime { get; set; }

    public override string ToString()
    {
        return $"{AudioPlayedTime},{ReactionTime}";
    }
}