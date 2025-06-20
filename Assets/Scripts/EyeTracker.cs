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
    private void WriteTrackingPoint(Vector3 hitPoint)
    {
        // var relativePoint = objectOfInterest.transform.position - hitPoint;
        // trackerData.WriteLine(FormattableString.Invariant($"{relativePoint.x},{relativePoint.y},{relativePoint.z}"));
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
            // if (hit.collider.gameObject == objectOfInterest)
            // {
            //     gazePointer.transform.position = hit.point;
            //     WriteTrackingPoint(hit.point);
            // }
        }
        else
        {
            gazePointer.transform.position = userCamera.transform.position;
        }
    }
    

    private void OnDestroy()
    {
        trackerData.Close();
    }
}
