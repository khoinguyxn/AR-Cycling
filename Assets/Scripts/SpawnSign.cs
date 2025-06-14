using UnityEngine;


public class SpawnSign : SpawnObject
{
    //ATTRIBUTES
    public GameObject signObject;



    //METHODS
    protected override GameObject spawnObject(Vector3 position, Quaternion rotation)
    {
        return Instantiate(signObject, position, rotation);
    }
}
