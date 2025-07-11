using UnityEngine;

public interface IObjectSpawner
{
    GameObject spawnObject(Vector3 position, Quaternion rotation);
}
