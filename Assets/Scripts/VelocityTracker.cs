using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class VelocityTracker : MonoBehaviour
{
    [Header("Tracking Settings")] [SerializeField]
    private Transform headTransform;

    [Header("Smoothing Settings")] [SerializeField]
    private float smoothingFactor = 0.1f;

    [SerializeField] private float maxVelocityChange = 10f;

    [Header("Export Settings")] [SerializeField]
    private string exportFileName = "velocity_data";

    [SerializeField] private float exportInterval = 1f;

    private Vector3 _previousPosition;
    private Vector3 _rawVelocity;
    private Vector3 _smoothedVelocity;

    private readonly List<VelocityDatum> _velocityData = new();
    private float _nextExportTime;
    private string _csvFilePath;

    private bool _isTracking;

    private void Start()
    {
        _previousPosition = transform.position;
        _nextExportTime = Time.time + exportInterval;
        _smoothedVelocity = Vector3.zero;

        var timeStamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        _csvFilePath = Application.persistentDataPath + $"/{exportFileName}_{timeStamp}.csv";

        Debug.Log($"Exporting velocity data to {_csvFilePath}");
    }

    private void FixedUpdate()
    {
        if (!_isTracking) return;

        TrackVelocity();

        _velocityData.Add(new VelocityDatum
                          {
                              TimeStamp = Time.time,
                              Position = transform.position,
                              Speed = _smoothedVelocity.sqrMagnitude
                          });

        if (!(Time.time >= _nextExportTime)) return;

        ExportRecentData();
        _nextExportTime = Time.time + exportInterval;
    }

    public void StartTracking()
    {
        _isTracking = true;
    }

    private void ExportRecentData()
    {
        if (_velocityData.Count == 0) return;

        var cutoffTime   = Time.time - exportInterval;
        var dataToExport = _velocityData.FindAll(datum => datum.TimeStamp >= cutoffTime);

        WriteToCsv(dataToExport, true);
    }

    private void WriteToCsv(List<VelocityDatum> velocityData, bool isAppended)
    {
        var isFileExisting = File.Exists(_csvFilePath);

        using var writer = new StreamWriter(_csvFilePath, isAppended);

        if (!isFileExisting)
        {
            writer.WriteLine("Time (s),Position_X (m),Position_Y (m),Position_Z (m),Speed (m/s)");
        }
        else
        {
            foreach (var datum in velocityData)
            {
                writer.WriteLine($"{datum.TimeStamp},{datum.Position.x},{datum.Position.y},{datum.Position.z},{datum.Speed}");
            }
        }
    }

    private void TrackVelocity()
    {
        var currentPosition = headTransform.position;

        _rawVelocity = (currentPosition - _previousPosition) / Time.fixedDeltaTime;

        // Outlier detection
        if (Vector3.Distance(_rawVelocity, _smoothedVelocity) >= maxVelocityChange)
        {
            _rawVelocity = _smoothedVelocity;
        }

        _smoothedVelocity = Vector3.Lerp(_smoothedVelocity, _rawVelocity, smoothingFactor);
        _previousPosition = currentPosition;
    }
}

internal record VelocityDatum
{
    public float TimeStamp { get; set; }
    public Vector3 Position { get; set; }
    public float Speed { get; set; }
}