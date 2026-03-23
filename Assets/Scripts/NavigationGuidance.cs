using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;


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
    [Tooltip("Optional label used in debug logs and the inspector.")]
    public string label;

    [Tooltip("Used to generate a default label when Label is empty.")]
    public NavigationDirection direction;

    [Tooltip("Scene anchor that defines the world-space sign position and forward direction.")]
    public Transform anchor;

    [Tooltip("Optional prefab override for this cue. Falls back to the component default prefab.")]
    public GameObject prefabOverride;

    [Tooltip("Optional material override for this cue. Falls back to the component default material.")]
    public Material materialOverride;

    [Tooltip("Optional resource texture path, e.g. SignImages/turn_left")]
    public string textureResourcePath;

    [Tooltip("Local position offset from the anchor transform.")]
    public Vector3 localPositionOffset = Vector3.zero;

    [Tooltip("Local euler rotation offset from the anchor transform.")]
    public Vector3 localEulerOffset = Vector3.zero;

    [Tooltip("Local scale applied to the spawned sign.")]
    public Vector3 localScale = Vector3.one;

    [Tooltip("Cue becomes visible when the rider is within this planar distance of the anchor.")]
    public float showWithinDistanceMeters = 30f;

    [Tooltip("Cue is considered passed once the rider has moved this far beyond the anchor forward axis.")]
    public float hideAfterPassingMeters = 6f;

    [Tooltip("Keep the cue active after it becomes visible until the rider passes it.")]
    public bool persistUntilPassed = true;

    [Tooltip("Fade in duration in seconds.")]
    public float fadeInDurationSeconds = 0.35f;

    [Tooltip("Fade out duration in seconds.")]
    public float fadeOutDurationSeconds = 0.25f;

    [NonSerialized] public bool HasBeenShown;
    [NonSerialized] public bool IsCompleted;
    [NonSerialized] public GameObject ActiveInstance;
    [NonSerialized] public Renderer[] ActiveRenderers;
    [NonSerialized] public float CurrentAlpha;
    [NonSerialized] public bool IsFadingOut;

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
    [SerializeField] private Transform spawnedCueContainer;

    [Header("Cue Authoring")]
    [SerializeField] private List<NavigationCue> cues = new List<NavigationCue>();


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

        ResetProgress();
    }


    private void Update()
    {
        if (userCamera == null) return;

        for (int i = 0; i < cues.Count; i++)
        {
            UpdateCue(cues[i]);
        }
    }


    [ContextMenu("Reset Navigation Progress")]
    public void ResetProgress()
    {
        for (int i = 0; i < cues.Count; i++)
        {
            NavigationCue cue = cues[i];
            cue.HasBeenShown = false;
            cue.IsCompleted = false;
            cue.IsFadingOut = false;
            cue.CurrentAlpha = 0f;
            cue.ActiveRenderers = null;

            if (cue.ActiveInstance != null)
            {
                Destroy(cue.ActiveInstance);
                cue.ActiveInstance = null;
            }
        }
    }


    private void UpdateCue(NavigationCue cue)
    {
        if (cue == null || cue.IsCompleted || cue.anchor == null)
        {
            return;
        }

        Vector3 riderPosition = userCamera.transform.position;
        Vector3 anchorPosition = cue.anchor.position;

        Vector3 planarOffset = riderPosition - anchorPosition;
        planarOffset.y = 0f;

        float planarDistance = planarOffset.magnitude;
        float passedDistance = Vector3.Dot(planarOffset, cue.anchor.forward);

        if (!cue.HasBeenShown)
        {
            if (planarDistance <= Mathf.Max(0.01f, cue.showWithinDistanceMeters))
            {
                ShowCue(cue);
            }

            return;
        }

        if (cue.ActiveInstance == null)
        {
            cue.IsCompleted = true;
            return;
        }

        UpdateCueFade(cue);

        bool hasPassedCue = passedDistance >= Mathf.Max(0f, cue.hideAfterPassingMeters);
        bool shouldHideBeforePassing = !cue.persistUntilPassed && planarDistance > cue.showWithinDistanceMeters;

        if (hasPassedCue || shouldHideBeforePassing)
        {
            BeginHideCue(cue);
        }
    }


    private void ShowCue(NavigationCue cue)
    {
        GameObject prefabToSpawn = cue.prefabOverride != null ? cue.prefabOverride : guidancePrefab;
        if (prefabToSpawn == null)
        {
            Debug.LogWarning($"NavigationGuidance could not show cue '{cue.GetResolvedLabel()}' because no prefab is assigned.");
            cue.IsCompleted = true;
            return;
        }

        GameObject cueObject = Instantiate(prefabToSpawn);
        Transform cueTransform = cueObject.transform;
        cueObject.name = cue.anchor != null ? cue.anchor.name : cue.GetResolvedLabel();

        if (spawnedCueContainer != null)
        {
            cueTransform.SetParent(spawnedCueContainer, worldPositionStays: true);
        }

        cueTransform.SetPositionAndRotation(
            cue.anchor.TransformPoint(cue.localPositionOffset),
            cue.anchor.rotation * Quaternion.Euler(cue.localEulerOffset)
        );
        cueTransform.localScale = cue.localScale;

        TryApplyCueTexture(cueObject, cue);

        cue.ActiveInstance = cueObject;
        cue.ActiveRenderers = cueObject.GetComponentsInChildren<Renderer>(includeInactive: true);
        cue.HasBeenShown = true;
        cue.IsFadingOut = false;
        cue.CurrentAlpha = 0f;
        SetCueAlpha(cue, 0f);

        Debug.Log($"Navigation cue shown: {cue.GetResolvedLabel()}");
    }


    private void BeginHideCue(NavigationCue cue)
    {
        if (cue.ActiveInstance == null)
        {
            cue.IsCompleted = true;
            return;
        }

        cue.IsFadingOut = true;
    }


    private void HideCueImmediately(NavigationCue cue, bool markCompleted)
    {
        if (cue.ActiveInstance != null)
        {
            Destroy(cue.ActiveInstance);
            cue.ActiveInstance = null;
        }

        cue.ActiveRenderers = null;
        cue.IsFadingOut = false;
        cue.IsCompleted = markCompleted;
    }


    private void TryApplyCueTexture(GameObject cueObject, NavigationCue cue)
    {
        Material baseMaterial = cue.materialOverride != null ? cue.materialOverride : guidanceMaterial;

        MeshRenderer meshRenderer = cueObject.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = cueObject.GetComponentInChildren<MeshRenderer>();
        }

        if (meshRenderer == null)
        {
            Debug.LogWarning($"NavigationGuidance cue '{cue.GetResolvedLabel()}' has no MeshRenderer. Texture was not applied.");
            return;
        }

        Material runtimeMaterial = baseMaterial != null
            ? new Material(baseMaterial)
            : new Material(meshRenderer.material);

        ConfigureMaterialForFade(runtimeMaterial);

        if (!string.IsNullOrWhiteSpace(cue.textureResourcePath))
        {
            Texture texture = Resources.Load<Texture>(cue.textureResourcePath);
            if (texture == null)
            {
                Debug.LogWarning($"NavigationGuidance could not find texture at Resources/{cue.textureResourcePath}");
            }
            else
            {
                runtimeMaterial.mainTexture = texture;
            }
        }

        meshRenderer.material = runtimeMaterial;
    }


    private void ConfigureMaterialForFade(Material material)
    {
        if (material == null) return;

        // Standard shader style transparency setup.
        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 2f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        // URP/HDRP style transparency setup.
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", 0f);
        }
    }


    private void UpdateCueFade(NavigationCue cue)
    {
        if (cue.ActiveInstance == null || cue.ActiveRenderers == null || cue.ActiveRenderers.Length == 0)
        {
            return;
        }

        float fadeDuration = cue.IsFadingOut
            ? Mathf.Max(0.01f, cue.fadeOutDurationSeconds)
            : Mathf.Max(0.01f, cue.fadeInDurationSeconds);

        float alphaDelta = Time.deltaTime / fadeDuration;
        cue.CurrentAlpha = cue.IsFadingOut
            ? Mathf.Max(0f, cue.CurrentAlpha - alphaDelta)
            : Mathf.Min(1f, cue.CurrentAlpha + alphaDelta);

        SetCueAlpha(cue, cue.CurrentAlpha);

        if (cue.IsFadingOut && cue.CurrentAlpha <= 0f)
        {
            HideCueImmediately(cue, markCompleted: true);
        }
    }


    private void SetCueAlpha(NavigationCue cue, float alpha)
    {
        if (cue.ActiveRenderers == null) return;

        for (int i = 0; i < cue.ActiveRenderers.Length; i++)
        {
            Renderer rendererComponent = cue.ActiveRenderers[i];
            if (rendererComponent == null) continue;

            Material[] materials = rendererComponent.materials;
            for (int j = 0; j < materials.Length; j++)
            {
                Material material = materials[j];
                if (material == null) continue;

                if (material.HasProperty("_Color"))
                {
                    Color color = material.color;
                    color.a = alpha;
                    material.color = color;
                }

                if (material.HasProperty("_BaseColor"))
                {
                    Color baseColor = material.GetColor("_BaseColor");
                    baseColor.a = alpha;
                    material.SetColor("_BaseColor", baseColor);
                }
            }
        }
    }


    private void OnDestroy()
    {
        ResetProgress();
    }
}
