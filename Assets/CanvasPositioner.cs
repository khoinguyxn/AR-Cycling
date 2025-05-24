using JetBrains.Annotations;
using Unity.XR.CoreUtils;
using UnityEngine;

public class CanvasPositioner : MonoBehaviour
{
    [SerializeField] [CanBeNull] private XROrigin xrOrigin;
    [SerializeField] private float verticalOffset = 1.0f;
    [SerializeField] private float horizontalOffset = -1.0f;
    [SerializeField] private float distance = 2.0f;

    private void Start()
    {
        if (xrOrigin == null)
        {
            xrOrigin = FindFirstObjectByType<XROrigin>();
        }

        var cam = xrOrigin?.Camera;

        if (cam == null) return;
        
        var forward = cam.transform.forward;
        var right = cam.transform.right;
        var up = cam.transform.up;

        var initialPosition = cam.transform.position + forward * distance + right * horizontalOffset +
                              up * verticalOffset;

        transform.position = initialPosition;
        transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);
    }
}