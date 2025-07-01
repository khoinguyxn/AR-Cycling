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
    private GameObject currentGazingObject = null;
    private float currentGazingTimer = 0;



    //METHODS
    private GameObject getGazingObject(RaycastHit hit)
    {
        Transform currentObject = hit.collider.gameObject.transform;
        while (currentObject.parent != null)
        {
            currentObject = currentObject.parent;
        }
        return currentObject.gameObject;
    }


    private void WriteTrackingData()
    {
        // var relativePoint = objectOfInterest.transform.position - hitPoint;
        // trackerData.WriteLine(FormattableString.Invariant($"{relativePoint.x},{relativePoint.y},{relativePoint.z}"));
        Debug.Log($"{currentGazingObject}, {currentGazingTimer}");
        trackerData.WriteLine(FormattableString.Invariant($"{currentGazingObject}, {currentGazingTimer}"));
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
            //Update gaze indicator position
            gazePointer.transform.position = hit.point;

            //Get what the user is currently gazing at this frame
            GameObject gazingObject = getGazingObject(hit);

            //If still gazing at the same object, increment timer
            if (gazingObject == currentGazingObject)
            {
                currentGazingTimer += Time.deltaTime * 1000f;
            }

            //Otherwise, record data (if applicable, i.e. was looking at something), reset timer and set to look at new object
            else
            {
                if (currentGazingObject != null)
                {
                    WriteTrackingData();
                }
                currentGazingObject = gazingObject;
                currentGazingTimer = 0;   
            }
        }
        else
        {
            //Reset gaze indicator position
            gazePointer.transform.position = userCamera.transform.position - Vector3.down * 2;

            //Check if the user was gazing at something and if so, record data and reset timer
            if (currentGazingObject != null)
            {
                WriteTrackingData();
                currentGazingObject = null;
                currentGazingTimer = 0;   
            }
        }
    }
    

    private void OnDestroy()
    {
        trackerData.Close();
    }
}
