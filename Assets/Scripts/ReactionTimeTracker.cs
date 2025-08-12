using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class ReactionTimeTracker : MonoBehaviour
{
    [SerializeField] private SpawnNotification spawnNotification;

    [Header("Export Settings")]
    [SerializeField] private string exportFileName = "reaction-time";
    [SerializeField] private float exportInterval = 1f;
    private CsvExporter _reactionTimeExporter;

    [Header("Arduino Connection")]
    [SerializeField] private string arduinoIp = "192.168.1.100";
    [SerializeField] private int arduinoPort = 8888;
    [SerializeField] private float reconnectInterval = 5f;
    [SerializeField] private float connectionTimeout = 10f;

    // TCP Connection
    private TcpClient tcpClient;
    private NetworkStream networkStream;
    private Thread receiveThread;
    private bool isConnected;
    private bool shouldReconnect = true;
    private MainThreadDispatcher dispatcher;

    // Study session management
    private bool isStudyActive = false;
    private bool pendingStudyStart = false;

    private void Awake()
    {
        InitializeExporter();
    }

    private void Start()
    {
        dispatcher = MainThreadDispatcher.Instance;
        ConnectToArduino();
    }

    public void StartStudySession()
    {
        if (isStudyActive)
        {
            Debug.LogWarning("Study session already active");
            return;
        }

        if (isConnected)
        {
            isStudyActive = true;
            pendingStudyStart = false;
            SendTcpMessage("START");
            Debug.Log("Study session started");
        }
        else
        {
            pendingStudyStart = true;
            Debug.Log("Study session queued - waiting for Arduino connection");
        }
    }

    public void EndStudySession()
    {
        if (isConnected && isStudyActive)
        {
            isStudyActive = false;
            SendTcpMessage("END");
            Debug.Log("Study session ended");
        }
        else if (!isConnected)
        {
            Debug.LogWarning("Cannot end study - not connected to Arduino");
        }
        else
        {
            Debug.LogWarning("No active study session to end");
        }
    }

    private void Update()
    {
        _reactionTimeExporter.ExportRecentData();
    }

    #region CSV Export Setup

    private void InitializeExporter()
    {
        var timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var filePath = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";
        const string csvHeader = "AudioPlayedTime (s),ReactionTime (s)";

        _reactionTimeExporter = new CsvExporter(filePath, exportInterval, csvHeader);
        Debug.Log($"Exporting reaction time data to {filePath}");
    }

    #endregion

    #region TCP Connection

    private void ConnectToArduino()
    {
        if (isConnected) return;

        try
        {
            Debug.Log($"Connecting to Arduino at {arduinoIp}:{arduinoPort}");
            tcpClient = new TcpClient();
            StartCoroutine(ConnectWithTimeout(tcpClient.ConnectAsync(arduinoIp, arduinoPort)));
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection failed: {e.Message}");
            ScheduleReconnect();
        }
    }

    private IEnumerator ConnectWithTimeout(Task connectTask)
    {
        float timer = 0f;
        while (!connectTask.IsCompleted && timer < connectionTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (connectTask.IsCompleted && !connectTask.IsFaulted)
        {
            OnConnectionEstablished();
        }
        else
        {
            Debug.LogError("Connection timeout or failed");

            tcpClient?.Close();
            ScheduleReconnect();
        }
    }

    private void OnConnectionEstablished()
    {
        try
        {
            networkStream = tcpClient.GetStream();
            isConnected = true;

            receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            receiveThread.Start();

            Debug.Log("Connected to Arduino!");

            // If there's a pending study start request, execute it now
            if (pendingStudyStart && !isStudyActive)
            {
                StartStudySession();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to establish stream: {e.Message}");
            Disconnect();
        }
    }

    private void ReceiveLoop()
    {
        var buffer = new byte[1024];

        while (isConnected)
        {
            try
            {
                if (networkStream.DataAvailable)
                {
                    var bytesRead = networkStream.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                        var messages = receivedData.Split('\n');

                        foreach (var message in messages)
                        {
                            if (!string.IsNullOrEmpty(message))
                            {
                                dispatcher.Enqueue(() => ProcessMessage(message));
                            }
                        }
                    }
                }
                else
                {
                    Thread.Sleep(10);
                }
            }
            catch (Exception e)
            {
                if (isConnected)
                {
                    Debug.LogError($"Receive error: {e.Message}");

                    dispatcher.Enqueue(HandleConnectionLoss);
                }
                break;
            }
        }
    }

    private void HandleConnectionLoss()
    {
        if (isConnected)
        {
            Debug.LogWarning("Connection lost, attempting to reconnect...");
            isConnected = false;

            // If study was active, queue it to restart after reconnection
            if (isStudyActive)
            {
                pendingStudyStart = true;
                Debug.Log("Study session will restart after reconnection");
            }
            isStudyActive = false;

            try
            {
                networkStream?.Close();
                tcpClient?.Close();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to handle connection loss: {e.Message}");
            }

            ScheduleReconnect();
        }
    }

    private void ScheduleReconnect()
    {
        if (shouldReconnect)
        {
            Debug.Log($"Scheduling reconnect in {reconnectInterval} seconds");

            Invoke(nameof(ConnectToArduino), reconnectInterval);
        }
    }

    public void Disconnect()
    {
        isConnected = false;
        shouldReconnect = false;

        try
        {
            networkStream?.Close();
            tcpClient?.Close();
            receiveThread?.Join(1000);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during disconnect: {e.Message}");
        }

        Debug.Log("Disconnected from Arduino");
    }

    #endregion

    #region Message Processing

    private void ProcessMessage(string message)
    {
        Debug.Log($"Received: {message}");

        switch (message)
        {
            case "BUTTON_PRESSED":
                HandleButtonPress();
                break;
            case "PING":
                SendTcpMessage("PONG");
                break;
            case "READY":
                Debug.Log("Arduino is ready for study session");
                break;
            case "ENDED":
                Debug.Log("Arduino confirmed study session ended");
                break;
            default:
                Debug.Log($"Unknown message: {message}");
                break;
        }
    }

    private void SendTcpMessage(string message)
    {
        if (!isConnected || networkStream == null)
        {
            Debug.LogWarning($"Cannot send message '{message}' - not connected");
            return;
        }

        try
        {
            var data = Encoding.UTF8.GetBytes(message + "\n");

            networkStream.Write(data, 0, data.Length);
            networkStream.Flush();

            Debug.Log($"Sent: {message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send message: {e.Message}");

            HandleConnectionLoss();
        }
    }

    private void HandleButtonPress()
    {
        if (!isStudyActive)
        {
            Debug.LogWarning("Button pressed but study session is not active");
            return;
        }

        var currentTime = Time.time;
        var reactionTime = currentTime - spawnNotification.LastAudioSorurcePlayedTime;

        var reactionData = new ReactionTimeDatum
        {
            AudioPlayedTime = spawnNotification.LastAudioSorurcePlayedTime,
            ReactionTime = reactionTime
        };

        _reactionTimeExporter.AddData(reactionData.ToString());

        Debug.Log($"Reaction time: {reactionTime:F3}s");
    }

    #endregion

    #region Unity Lifecycle

    private void OnDestroy()
    {
        Disconnect();
        EndStudySession();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Disconnect();
        }
        else
        {
            shouldReconnect = true;
            ConnectToArduino();
        }
    }

    #endregion
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

internal class MainThreadDispatcher : MonoBehaviour
{
    private static MainThreadDispatcher _instance;
    private readonly Queue<Action> _executionQueue = new();

    public static MainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<MainThreadDispatcher>();
                if (_instance == null)
                {
                    var go = new GameObject("MainThreadDispatcher");
                    _instance = go.AddComponent<MainThreadDispatcher>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }

    private void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}