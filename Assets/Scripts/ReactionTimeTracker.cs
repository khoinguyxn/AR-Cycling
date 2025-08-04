using UnityEngine;

class ReactionTimeTracker : MonoBehaviour
{
  [SerializeField] private SpawnNotification spawnNotification;
  private CsvExporter _reactionTimeExporter;
  private string inputBuffer = "";

  private void Update()
  {
    foreach (char c in Input.inputString)
    {
      if (c == '\n' || c == '\r')
      {
        // End of line - process the complete message
        ProcessArduinoMessage(inputBuffer.Trim());
        inputBuffer = "";
      }
      else if (c == '\b')
      {
        // Backspace
        if (inputBuffer.Length > 0)
          inputBuffer = inputBuffer.Substring(0, inputBuffer.Length - 1);
      }
      else if (c >= ' ') // Only printable characters
      {
        inputBuffer += c;
      }
    }
  }

  private void ProcessArduinoMessage(string message)
  {
    switch (message)
    {
      case "READY":
        // Instead of loogging, we could play a beep
        Debug.Log("Arduino is ready.");
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
}