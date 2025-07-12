using UnityEngine;
using System.Collections.Generic;


public class SpawnSign : MonoBehaviour, IObjectSpawner
{
    //ATTRIBUTES
    public GameObject signObject;
    public Material signMaterial;
    public List<Texture> sideTextures;
    public List<Texture> topTextures;
    public List<Texture> bottomTextures;



    //METHODS
    public GameObject spawnObject(SpawnPosition spawnPosition, Vector3 position, Quaternion rotation)
    {
        List<Texture> textureList;

        switch (spawnPosition)
        {
            case SpawnPosition.side:
                textureList = sideTextures;
                break;
            case SpawnPosition.top:
                textureList = topTextures;
                break;
            case SpawnPosition.bottom:
                textureList = bottomTextures;
                break;
            default:
                textureList = sideTextures;
                break;
        }

        int textureIndex = Random.Range(0, textureList.Count);
        Texture texture = textureList[textureIndex];
        textureList.RemoveAt(textureIndex);

        GameObject signInstance = Instantiate(signObject, position, rotation);

        signMaterial.mainTexture = texture;

        MeshRenderer imageMeshRenderer = signInstance.GetComponent<MeshRenderer>();
        if (imageMeshRenderer != null)
        {
            imageMeshRenderer.material = signMaterial;
        }

        return signInstance;
    }
}
