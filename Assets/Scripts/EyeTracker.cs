using System;
using System.IO;
using MixedReality.Toolkit.Input;
using UnityEngine;



public class EyeTracker : MonoBehaviour
{
    //ATTRIBUTES
    private StreamWriter trackerData;
    public GazeInteractor gazeInteractor;
    public GameObject gazeIndicator;
    private GameObject gazePointer;
    public GameObject userCamera;



    //METHODS
    private void WriteTrackingPoint(RaycastHit hit)
    {
        // var relativePoint = objectOfInterest.transform.position - hitPoint;
        // trackerData.WriteLine(FormattableString.Invariant($"{relativePoint.x},{relativePoint.y},{relativePoint.z}"));
        Transform currentObject = hit.collider.gameObject.transform;
        while (currentObject.parent != null)
        {
            currentObject = currentObject.parent;
        }
        Debug.Log($"{currentObject.gameObject}, {Time.time}");
        trackerData.WriteLine(FormattableString.Invariant($"{currentObject.gameObject}, {Time.time}"));
    }


    private void Awake()
    {
        string trackerDataPath = Path.Combine(Application.persistentDataPath, "eyeTracking.csv");
        trackerData = new StreamWriter(trackerDataPath);
        trackerData.AutoFlush = true;
    }


    private void Start()
    {
        gazePointer = Instantiate(gazeIndicator);
    }


    private void Update()
    {
        var ray = new Ray(gazeInteractor.rayOriginTransform.position, gazeInteractor.rayOriginTransform.forward * 3);
        if (Physics.Raycast(ray, out var hit))
        {
            gazePointer.transform.position = hit.point;
            WriteTrackingPoint(hit);
        }
        else
        {
            gazePointer.transform.position = userCamera.transform.position - Vector3.down * 2;
        }
    }
    

    private void OnDestroy()
    {
        trackerData.Close();
    }
}
