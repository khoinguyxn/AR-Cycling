using UnityEngine;

public interface IObjectSpawner
{
    GameObject spawnObject(SpawnPosition spawnPosition, Vector3 position, Quaternion rotation);
}
