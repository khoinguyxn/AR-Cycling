using UnityEngine;

public class SpawnSign : MonoBehaviour {
    //ATTRIBUTES
    public GameObject signObject;
    public Camera userCamera;
    public float distanceBase = 40;
    public float distanceOffset = 10;
    public float xDisplacement = 0;
    private float distanceUntilSpawnSign;
    private Vector2 userInitialPosition;
    private Vector3 signSpawnDisplacement = Vector3.forward*15; //Vector3 so it can be added to the user's position.
    private GameObject currentSign;
    private GameObject previousSign;


    
    //METHODS
    private float getUserDistance() {
        //x and z coordinates for Vector3, x and y coordinates for Vector2
        Vector2 displacementVector = new Vector2(userCamera.transform.position.x-userInitialPosition.x, userCamera.transform.position.z-userInitialPosition.y);
        return displacementVector.magnitude;
    }
    
    
    private float getRandomDistance() {
        return Random.Range(distanceBase-distanceOffset, distanceBase+distanceOffset);
    }


    private void initialiseSignDisplacement() {
        userInitialPosition = new Vector2(userCamera.transform.position.x, userCamera.transform.position.z);
        distanceUntilSpawnSign = getRandomDistance();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        signSpawnDisplacement = Vector3.right * xDisplacement + Vector3.forward * 15;
        initialiseSignDisplacement();
        currentSign = Instantiate(signObject, userCamera.transform.position + signSpawnDisplacement, transform.rotation);
    }


    // Update is called once per frame
    void Update() {
        if (getUserDistance() >= distanceUntilSpawnSign) {
            Destroy(previousSign);
            previousSign = currentSign;
            initialiseSignDisplacement();
            currentSign = Instantiate(signObject, userCamera.transform.position + signSpawnDisplacement, transform.rotation);
        }
    }
}
