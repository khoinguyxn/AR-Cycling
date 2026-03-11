using System;
using System.Collections.Generic;
using UnityEngine;


public enum NavigationDirection
{
    Left,
    Right,
    Straight,
    Custom
}


[Serializable]
public class NavigationCue
{
    [Tooltip("Trigger distance in meters from ride start.")]
    public float triggerDistanceMeters;

    [Tooltip("Used to auto-generate a default label.")]
    public NavigationDirection direction;

    [Tooltip("Optional custom label shown in debug logs.")]
    public string label;

    [Tooltip("Optional resource texture path, e.g. SignImages/keep_left")]
    public string textureResourcePath;

    [Tooltip("Per-cue local offset relative to the default anchor.")]
    public Vector3 localPositionOffset = Vector3.zero;

    [Tooltip("Per-cue local euler rotation in degrees.")]
    public Vector3 localEulerOffset = Vector3.zero;

    [Tooltip("Per-cue local scale multiplier.")]
    public Vector3 localScaleMultiplier = Vector3.one;

    [Tooltip("How long this cue stays visible (seconds).")]
    public float visibleDurationSeconds = 4f;

    public string GetResolvedLabel()
    {
        if (!string.IsNullOrWhiteSpace(label)) return label;

        return direction switch
        {
            NavigationDirection.Left => "Turn left",
            NavigationDirection.Right => "Turn right",
            NavigationDirection.Straight => "Go straight",
            _ => "Navigation cue"
        };
    }
}


public class NavigationGuidance : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera userCamera;
    [SerializeField] private GameObject guidancePrefab;
    [SerializeField] private Material guidanceMaterial;

    [Header("Screen-Locked Placement")]
    [SerializeField] private Vector3 defaultLocalPosition = new Vector3(0f, 0.22f, 1.2f);
    [SerializeField] private Vector3 defaultLocalEuler = Vector3.zero;
    [SerializeField] private Vector3 defaultLocalScale = new Vector3(0.5f, 0.35f, 0.5f);

    [Header("Distance Tracking")]
    [SerializeField] private bool ignoreVerticalMovement = true;
    [SerializeField] private float maxFrameDistanceMeters = 3f;

    [Header("Navigation Cues")]
    [SerializeField] private List<NavigationCue> cues = new List<NavigationCue>
    {
        new NavigationCue
        {
            triggerDistanceMeters = 270f,
            direction = NavigationDirection.Left,
            label = "Turn left",
            textureResourcePath = "SignImages/turn_left",
            visibleDurationSeconds = 4f
        },
        new NavigationCue
        {
            triggerDistanceMeters = 330f,
            direction = NavigationDirection.Left,
            label = "Turn left",
            textureResourcePath = "SignImages/turn_left",
            visibleDurationSeconds = 4f
        },
        new NavigationCue
        {
            triggerDistanceMeters = 345f,
            direction = NavigationDirection.Right,
            label = "Turn right",
            textureResourcePath = "SignImages/turn_right",
            visibleDurationSeconds = 4f
        }
    };

    private readonly List<NavigationCue> _sortedCues = new List<NavigationCue>();
    private Vector3 _previousUserPosition;
    private int _nextCueIndex;
    private float _totalDistanceTravelledMeters;
    private GameObject _activeCueObject;


    public float TotalDistanceTravelledMeters => _totalDistanceTravelledMeters;


    private void Start()
    {
        if (userCamera == null)
        {
            userCamera = Camera.main;
        }

        if (userCamera == null)
        {
            Debug.LogError("NavigationGuidance requires a user camera reference.");
            enabled = false;
            return;
        }

        if (guidancePrefab == null)
        {
            Debug.LogError("NavigationGuidance requires a guidance prefab reference.");
            enabled = false;
            return;
        }

        _previousUserPosition = userCamera.transform.position;

        _sortedCues.Clear();
        _sortedCues.AddRange(cues);
        _sortedCues.Sort((a, b) => a.triggerDistanceMeters.CompareTo(b.triggerDistanceMeters));
    }


    private void Update()
    {
        TrackDistance();
        TriggerDueCue();
    }


    [ContextMenu("Reset Distance Progress")]
    public void ResetProgress()
    {
        _totalDistanceTravelledMeters = 0f;
        _nextCueIndex = 0;
        _previousUserPosition = userCamera != null ? userCamera.transform.position : Vector3.zero;

        if (_activeCueObject != null)
        {
            Destroy(_activeCueObject);
            _activeCueObject = null;
        }
    }


    private void TrackDistance()
    {
        Vector3 currentPosition = userCamera.transform.position;
        Vector3 movement = currentPosition - _previousUserPosition;
        if (ignoreVerticalMovement)
        {
            movement.y = 0f;
        }

        float frameDistance = movement.magnitude;

        // Ignore very large single-frame jumps (teleport spikes).
        if (frameDistance <= maxFrameDistanceMeters)
        {
            _totalDistanceTravelledMeters += frameDistance;
        }

        _previousUserPosition = currentPosition;
    }


    private void TriggerDueCue()
    {
        if (_nextCueIndex >= _sortedCues.Count)
        {
            return;
        }

        NavigationCue cue = _sortedCues[_nextCueIndex];
        if (_totalDistanceTravelledMeters < cue.triggerDistanceMeters)
        {
            return;
        }

        ShowCue(cue);
        _nextCueIndex++;
    }


    private void ShowCue(NavigationCue cue)
    {
        if (_activeCueObject != null)
        {
            Destroy(_activeCueObject);
            _activeCueObject = null;
        }

        _activeCueObject = Instantiate(guidancePrefab, userCamera.transform);
        _activeCueObject.transform.localPosition = defaultLocalPosition + cue.localPositionOffset;
        _activeCueObject.transform.localRotation = Quaternion.Euler(defaultLocalEuler + cue.localEulerOffset);

        Vector3 scale = new Vector3(
            defaultLocalScale.x * cue.localScaleMultiplier.x,
            defaultLocalScale.y * cue.localScaleMultiplier.y,
            defaultLocalScale.z * cue.localScaleMultiplier.z
        );
        _activeCueObject.transform.localScale = scale;

        TryApplyCueTexture(_activeCueObject, cue.textureResourcePath);
        Debug.Log($"Navigation cue: {cue.GetResolvedLabel()} at {_totalDistanceTravelledMeters:F1}m");

        float duration = Mathf.Max(0f, cue.visibleDurationSeconds);
        if (duration > 0f)
        {
            Destroy(_activeCueObject, duration);
        }
    }


    private void TryApplyCueTexture(GameObject cueObject, string texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return;
        }

        Texture texture = Resources.Load<Texture>(texturePath);
        if (texture == null)
        {
            Debug.LogWarning($"NavigationGuidance could not find texture at Resources/{texturePath}");
            return;
        }

        MeshRenderer meshRenderer = cueObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogWarning("NavigationGuidance cue prefab has no MeshRenderer. Texture was not applied.");
            return;
        }

        Material runtimeMaterial;
        if (guidanceMaterial != null)
        {
            runtimeMaterial = new Material(guidanceMaterial);
        }
        else
        {
            runtimeMaterial = new Material(meshRenderer.material);
        }

        runtimeMaterial.mainTexture = texture;
        meshRenderer.material = runtimeMaterial;
    }
}
