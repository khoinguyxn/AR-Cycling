using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_WSA && !UNITY_EDITOR
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
#endif

public class ReactionTimeTracker : MonoBehaviour
{
    [SerializeField] private SpawnNotification spawnNotification;

    [Header("Export Settings")] [SerializeField]
    private string exportFileName = "reaction-time";

    [SerializeField] private float exportInterval = 1f;
    private CsvExporter _reactionTimeExporter;

    [Header("Arduino Connection")] [SerializeField]
    private string arduinoIp = "192.168.1.100";

    [SerializeField] private int arduinoPort = 8888;
    [SerializeField] private float reconnectInterval = 5f;
    [SerializeField] private float connectionTimeout = 10f;

    // TCP Connection
    #if UNITY_WSA && !UNITY_EDITOR
    private StreamSocket _streamSocket;
    private DataWriter _dataWriter;
    private DataReader _dataReader;
    #else
    private TcpClient _tcpClient;
    private NetworkStream _networkStream;
    #endif
    private Thread _receiveThread;
    private bool _isConnected;
    private bool _shouldReconnect = true;
    private MainThreadDispatcher _dispatcher;

    // Study session management
    private bool _isStudyActive;
    private bool _pendingStudyStart;

    private void Awake()
    {
        InitializeExporter();
    }

    private void Start()
    {
        _dispatcher = MainThreadDispatcher.Instance;
        ConnectToArduino();
    }

    public void StartStudySession()
    {
        if (_isStudyActive)
        {
            Debug.LogWarning("Study session already active");
            return;
        }

        if (_isConnected)
        {
            _isStudyActive = true;
            _pendingStudyStart = false;
            SendTcpMessage("START");
            Debug.Log("Study session started");
        }
        else
        {
            _pendingStudyStart = true;
            Debug.Log("Study session queued - waiting for Arduino connection");
        }
    }

    public void EndStudySession()
    {
        switch (_isConnected)
        {
            case true when _isStudyActive:
                _isStudyActive = false;
                SendTcpMessage("END");
                Debug.Log("Study session ended");
                break;
            case false:
                Debug.LogWarning("Cannot end study - not connected to Arduino");
                break;
            default:
                Debug.LogWarning("No active study session to end");
                break;
        }
    }

    private void Update()
    {
        _reactionTimeExporter.ExportRecentData();
    }

    #region CSV Export Setup

    private void InitializeExporter()
    {
        var          timeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var          filePath  = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";
        const string csvHeader = "AudioPlayedTime (s),ReactionTime (s)";

        _reactionTimeExporter = new CsvExporter(filePath, exportInterval, csvHeader);
        Debug.Log($"Exporting reaction time data to {filePath}");
    }

    #endregion

    #region TCP Connection

    private void ConnectToArduino()
    {
        if (_isConnected) return;

        try
        {
            Debug.Log($"Connecting to Arduino at {arduinoIp}:{arduinoPort}");

            #if UNITY_WSA && !UNITY_EDITOR
            StartCoroutine(ConnectWithStreamSocket());
            #else
            _tcpClient = new TcpClient();
            StartCoroutine(ConnectWithTimeout(_tcpClient.ConnectAsync(arduinoIp, arduinoPort)));
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Connection failed: {e.Message}");
            ScheduleReconnect();
        }
    }

    private IEnumerator ConnectWithTimeout(Task connectTask)
    {
        var timer = 0f;

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
            #if !UNITY_WSA || UNITY_EDITOR
            _tcpClient?.Close();
            #endif
            ScheduleReconnect();
        }
    }

    #if UNITY_WSA && !UNITY_EDITOR
    private IEnumerator ConnectWithStreamSocket()
    {
        bool connectionCompleted = false;
        bool connectionSuccessful = false;
        
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                _streamSocket = new StreamSocket();
                var hostName = new HostName(arduinoIp);
                var serviceName = arduinoPort.ToString();
                
                await _streamSocket.ConnectAsync(hostName, serviceName);
                
                _dataWriter = new DataWriter(_streamSocket.OutputStream);
                _dataReader = new DataReader(_streamSocket.InputStream);
                _dataReader.InputStreamOptions = InputStreamOptions.Partial;
                
                connectionSuccessful = true;
                connectionCompleted = true;
                
                Debug.Log("StreamSocket connected successfully!");
            }
            catch (Exception e)
            {
                Debug.LogError($"StreamSocket connection failed: {e.Message}");
                connectionSuccessful = false;
                connectionCompleted = true;
            }
        });
        
        var timer = 0f;
        
        while (!connectionCompleted && timer < connectionTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (connectionSuccessful)
        {
            OnConnectionEstablished();
        }
        else
        {
            Debug.LogError("StreamSocket connection timeout or failed");
            CleanupConnection();
            ScheduleReconnect();
        }
    }
    #endif

    private void CleanupConnection()
    {
        try
        {
            #if UNITY_WSA && !UNITY_EDITOR
            _dataWriter?.Dispose();
            _dataReader?.Dispose();
            _streamSocket?.Dispose();
            _dataWriter = null;
            _dataReader = null;
            _streamSocket = null;
            #else
            _networkStream?.Close();
            _tcpClient?.Close();
            #endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cleaning up connection: {e.Message}");
        }
    }

    private void OnConnectionEstablished()
    {
        try
        {
            #if !UNITY_WSA || UNITY_EDITOR
            _networkStream = _tcpClient.GetStream();
            #endif
            _isConnected = true;

            _receiveThread = new Thread(ReceiveLoop) { IsBackground = true };
            _receiveThread.Start();

            Debug.Log("Connected to Arduino!");

            // If there's a pending study start request, execute it now
            if (_pendingStudyStart && !_isStudyActive)
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
        while (_isConnected)
        {
            try
            {
                #if UNITY_WSA && !UNITY_EDITOR
                ReceiveDataStreamSocket();
                #else
                ReceiveDataTcpClient();
                #endif
                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                if (_isConnected)
                {
                    Debug.LogError($"Receive loop error: {e.Message}");
                    _dispatcher.Enqueue(HandleConnectionLoss);
                }

                break;
            }
        }
    }

    #if !UNITY_WSA || UNITY_EDITOR
    private void ReceiveDataTcpClient()
    {
        if (_networkStream.DataAvailable)
        {
            var buffer    = new byte[1024];
            var bytesRead = _networkStream.Read(buffer, 0, buffer.Length);
            if (bytesRead > 0)
            {
                var receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                ProcessReceivedData(receivedData);
            }
        }
    }
    #endif

    #if UNITY_WSA && !UNITY_EDITOR
    private void ReceiveDataStreamSocket()
    {
        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                var availableBytes = await _dataReader.LoadAsync(1024);
                if (availableBytes > 0)
                {
                    var receivedData = _dataReader.ReadString(availableBytes).Trim();
                    ProcessReceivedData(receivedData);
                }
            }
            catch (Exception e)
            {
                if (_isConnected)
                {
                    Debug.LogError($"StreamSocket receive error: {e.Message}");
                    _dispatcher.Enqueue(HandleConnectionLoss);
                }
            }
        });
    }
    #endif

    private void ProcessReceivedData(string receivedData)
    {
        var messages = receivedData.Split('\n');
        foreach (var message in messages)
        {
            if (!string.IsNullOrEmpty(message))
            {
                _dispatcher.Enqueue(() => ProcessMessage(message));
            }
        }
    }

    private void HandleConnectionLoss()
    {
        if (!_isConnected) return;

        Debug.LogWarning("Connection lost, attempting to reconnect...");
        _isConnected = false;

        // If study was active, queue it to restart after reconnection
        if (_isStudyActive)
        {
            _pendingStudyStart = true;
            Debug.Log("Study session will restart after reconnection");
        }

        _isStudyActive = false;

        CleanupConnection();

        ScheduleReconnect();
    }

    private void ScheduleReconnect()
    {
        if (!_shouldReconnect) return;

        Debug.Log($"Scheduling reconnect in {reconnectInterval} seconds");

        Invoke(nameof(ConnectToArduino), reconnectInterval);
    }

    private void Disconnect()
    {
        _isConnected = false;
        _shouldReconnect = false;

        try
        {
            CleanupConnection();
            _receiveThread?.Join(1000);
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
        if (!_isConnected)
        {
            Debug.LogWarning($"Cannot send message '{message}' - not connected");
            return;
        }

        try
        {
            #if UNITY_WSA && !UNITY_EDITOR
            SendMessageStreamSocket(message);
            #else
            SendMessageTcpClient(message);
            #endif
            Debug.Log($"Sent: {message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to send message: {e.Message}");
            HandleConnectionLoss();
        }
    }

    #if !UNITY_WSA || UNITY_EDITOR
    private void SendMessageTcpClient(string message)
    {
        if (_networkStream == null)
            throw new InvalidOperationException("NetworkStream is null");

        var data = Encoding.UTF8.GetBytes(message + "\n");
        _networkStream.Write(data, 0, data.Length);
        _networkStream.Flush();
    }
    #endif

    #if UNITY_WSA && !UNITY_EDITOR
    private void SendMessageStreamSocket(string message)
    {
        if (_dataWriter == null)
            throw new InvalidOperationException("DataWriter is null");

        System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                _dataWriter.WriteString(message + "\n");
                await _dataWriter.StoreAsync();
                await _dataWriter.FlushAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to send StreamSocket message: {e.Message}");
                _dispatcher.Enqueue(HandleConnectionLoss);
            }
        });
    }
    #endif

    private void HandleButtonPress()
    {
        if (!_isStudyActive)
        {
            Debug.LogWarning("Button pressed but study session is not active");
            return;
        }

        var currentTime  = Time.time;
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
            if (_instance != null) return _instance;

            _instance = FindFirstObjectByType<MainThreadDispatcher>();

            if (_instance != null) return _instance;

            var go = new GameObject("MainThreadDispatcher");
            _instance = go.AddComponent<MainThreadDispatcher>();
            DontDestroyOnLoad(go);

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