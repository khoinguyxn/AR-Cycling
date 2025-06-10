using UnityEngine;

public class GazeControl : MonoBehaviour
{
    //ATTRIBUTES
    private float timeActivated = float.MinValue;


    //METHODS
    public void startGaze()
    {
        Debug.Log("gazing");
        timeActivated = Time.time;
    }


    public void endGaze()
    {
        Debug.Log(Time.time - timeActivated);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
