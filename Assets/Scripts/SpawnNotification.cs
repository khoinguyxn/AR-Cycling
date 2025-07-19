using UnityEngine;
using System.Collections.Generic;

public class SpawnNotification : MonoBehaviour
{
    //ATTRIBUTES
    public SpawnPosition spawnPosition;
    public float spawnDistance = 20;
    public GameObject notificationControl;
    public Camera userCamera;
    public float distanceBetweenObjectsBase = 40;
    public float distanceBetweenObjectsOffset = 10;
    private ObjectPosition objectPosition;
    private float distanceUntilSpawnObject;
    private Vector2 userInitialPosition;
    private Vector3 objectSpawnDisplacement; //Vector3 so it can be added to the user's position.
    private GameObject currentObject;
    private GameObject previousObject;
    private Vector3 userPositionTracker;

    private SpawnSign spawnSign;
    private SpawnModel spawnModel;
    private readonly Dictionary<SpawnPosition, ObjectPosition> objectPositionMap = new Dictionary<SpawnPosition, ObjectPosition>
    {
        { SpawnPosition.side, new ObjectPosition(new Vector2(-3, 0), new Vector3(0, 0, 0), new Vector3(1, 1, 1)) },
        { SpawnPosition.top, new ObjectPosition(new Vector2(0, 6), new Vector3(0, 0, 0), new Vector3(1, 1, 1)) },
        { SpawnPosition.bottom, new ObjectPosition(new Vector2(0, -1.5f), new Vector3(90, 0, 0), new Vector3(2, 2, 1)) },
    };



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
        if (v.magnitude == 0)
        {
            return new Vector2(0, 0);
        }
        return v / v.magnitude;
    }


    private Vector2 getVector2(Vector3 vector)
    {
        return new Vector2(vector.x, vector.z);
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
            crossProduct2(a, b) / (a.magnitude * b.magnitude) * c.x + dotProduct2(a, b) / (a.magnitude * b.magnitude) * c.y
        );
    }


    private float getFacingAngle(Vector2 referenceVector)
    {
        return Mathf.Atan2(referenceVector.x, referenceVector.y) * 180 / Mathf.PI;
    }


    private Vector3 getRelativeSpawnDisplacement(Vector2 referenceVector)
    {
        Vector2 relativeSpawnDisplacementUnit2 = getRelativeVector(referenceVector, getVector2(objectSpawnDisplacement));
        Vector3 relativeSpawnDisplacementUnit = new Vector3(relativeSpawnDisplacementUnit2.x, 0, relativeSpawnDisplacementUnit2.y);
        Vector3 relativeSpawnDisplacement = relativeSpawnDisplacementUnit * objectSpawnDisplacement.magnitude;

        return relativeSpawnDisplacement;
    }


    private float getUserDistance()
    {
        //x and z coordinates for Vector3, x and y coordinates for Vector2
        Vector2 displacementVector = new Vector2(userCamera.transform.position.x - userInitialPosition.x, userCamera.transform.position.z - userInitialPosition.y);
        return displacementVector.magnitude;
    }


    private float getRandomDistance()
    {
        return Random.Range(distanceBetweenObjectsBase - distanceBetweenObjectsOffset, distanceBetweenObjectsBase + distanceBetweenObjectsOffset);
    }

    private void initialiseSignDisplacement()
    {
        userInitialPosition = new Vector2(userCamera.transform.position.x, userCamera.transform.position.z);
        distanceUntilSpawnObject = getRandomDistance();
    }


    private GameObject spawnSignInstance(Vector3 instancePosition, Vector2 referenceVector)
    {
        Quaternion instanceRotation = Quaternion.Euler(objectPosition.getXRotation(), getFacingAngle(referenceVector) + objectPosition.getYRotation(), objectPosition.getZRotation());
        return spawnSign.spawnObject(spawnPosition, instancePosition, instanceRotation, objectPosition.getLocalScale());
    }


    private GameObject spawnModelInstance(Vector3 instancePosition)
    {
        Quaternion instanceRotation = Quaternion.Euler(0, 0, objectPosition.getZRotation());
        return spawnModel.spawnObject(spawnPosition, instancePosition, instanceRotation, new Vector3(1, 1, 1));
    }


    private void spawnNotificationInstance(Vector3 instancePosition, Vector2 referenceVector)
    {
        Destroy(previousObject);
        previousObject = currentObject;
        initialiseSignDisplacement();

        if (spawnSign.getListEmpty(spawnPosition))
        {
            currentObject = spawnModelInstance(instancePosition);
        }
        else if (spawnModel.getListEmpty(spawnPosition))
        {
            currentObject = spawnSignInstance(instancePosition, referenceVector);
        }
        else
        {
            if (Random.value < 0.5f)
            {
                currentObject = spawnSignInstance(instancePosition, referenceVector);
            }
            else
            {

                currentObject = spawnModelInstance(instancePosition);
            }
        }
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        objectPosition = objectPositionMap[spawnPosition];

        spawnSign = notificationControl.GetComponent<SpawnSign>();
        spawnModel = notificationControl.GetComponent<SpawnModel>();

        initialiseSignDisplacement();

        objectSpawnDisplacement = Vector3.right * objectPosition.getXDisplacement() + Vector3.forward * spawnDistance;
        Vector2 referenceVector = unitVector2(getVector2(getMovementVector()));
        spawnNotificationInstance(userCamera.transform.position + objectSpawnDisplacement + Vector3.up * objectPosition.getYDisplacement(), referenceVector);
    }

    // Update is called once per frame
    void Update()
    {
        if (getUserDistance() >= distanceUntilSpawnObject)
        {
            Vector2 referenceVector = unitVector2(getVector2(getMovementVector()));
            Vector3 relativeSpawnDisplacement = getRelativeSpawnDisplacement(referenceVector);
            Vector3 instancePosition = userCamera.transform.position + relativeSpawnDisplacement + Vector3.up * objectPosition.getYDisplacement();
            spawnNotificationInstance(instancePosition, referenceVector);
        }

        Vector3 userPosition = userCamera.transform.position;
        userPositionTracker.x = userPosition.x;
        userPositionTracker.y = userPosition.y;
        userPositionTracker.z = userPosition.z;
    }
}
