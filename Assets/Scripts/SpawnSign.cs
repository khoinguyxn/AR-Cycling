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
    private Vector3 userPositionTracker;



    //METHODS
    private float dotProduct2(Vector2 a, Vector2 b)
    {
        return a.x * b.x + a.y * b.y;
    }


    private float crossProduct2(Vector2 a, Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }


    private Vector2 unitVector2(Vector2 v)
    {
        return v / v.magnitude;
    }


    private Vector2 getVector2(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
    }


    private float getUserDistance()
    {
        //x and z coordinates for Vector3, x and y coordinates for Vector2
        Vector2 displacementVector = new Vector2(userCamera.transform.position.x - userInitialPosition.x, userCamera.transform.position.z - userInitialPosition.y);
        return displacementVector.magnitude;
    }
    
    
    private float getRandomDistance() {
        return Random.Range(distanceBase-distanceOffset, distanceBase+distanceOffset);
    }


    private Vector3 getMovementVector()
    {
        Vector3 userPosition = userCamera.transform.position;
        Vector3 movementVector = new Vector3(userPosition.x - userPositionTracker.x, userPositionTracker.y - userPosition.y, userPosition.z - userPositionTracker.z);
        
        return movementVector;
    }


    private Vector2 getRelativeVector(Vector2 referenceVector, Vector2 relativeVector)
    {
        Vector2 forwardVector = new Vector2(0, 1);

        Vector2 a = forwardVector; //Absolute forward
        Vector2 b = relativeVector; //Absolute displacement
        Vector2 c = referenceVector; //Relative forward
        
        return new Vector2(
            dotProduct2(a, b) / (a.magnitude * b.magnitude) * c.x - crossProduct2(a, b) / (a.magnitude * b.magnitude) * c.y,
            crossProduct2(a, b) / (a.magnitude*b.magnitude) * c.x + dotProduct2(a, b) / (a.magnitude * b.magnitude) * c.y
        );
    }


    private void initialiseSignDisplacement()
    {
        userInitialPosition = new Vector2(userCamera.transform.position.x, userCamera.transform.position.z);
        distanceUntilSpawnSign = getRandomDistance();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() {
        signSpawnDisplacement = Vector3.right * xDisplacement + Vector3.forward * 15;
        initialiseSignDisplacement();
        currentSign = Instantiate(signObject, userCamera.transform.position + signSpawnDisplacement, transform.rotation);
    }


    // Update is called once per frame
    void Update()
    {
        if (getUserDistance() >= distanceUntilSpawnSign)
        {
            Vector2 relativeSpawnDisplacementUnit2 = getRelativeVector(unitVector2(getVector2(getMovementVector())), getVector2(signSpawnDisplacement));
            Vector3 relativeSpawnDisplacementUnit = new Vector3(relativeSpawnDisplacementUnit2.x, 0, relativeSpawnDisplacementUnit2.y);
            Vector3 relativeSpawnDisplacement = relativeSpawnDisplacementUnit * signSpawnDisplacement.magnitude;
            Destroy(previousSign);
            previousSign = currentSign;
            initialiseSignDisplacement();
            currentSign = Instantiate(signObject, userCamera.transform.position + relativeSpawnDisplacement, transform.rotation);
        }
        
        Vector3 userPosition = userCamera.transform.position;
        userPositionTracker.x = userPosition.x;
        userPositionTracker.y = userPosition.y;
        userPositionTracker.z = userPosition.z;
    }
}
