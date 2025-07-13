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
    private List<Texture> getTextureList(SpawnPosition spawnPosition)
    {
        switch (spawnPosition)
        {
            case SpawnPosition.side:
                return sideTextures;
            case SpawnPosition.top:
                return topTextures;
            case SpawnPosition.bottom:
                return bottomTextures;
            default:
                return sideTextures;
        }
    }


    public bool getListEmpty(SpawnPosition spawnPosition)
    {
        List<Texture> textureList = getTextureList(spawnPosition);
        return textureList.Count == 0;
    }


    public GameObject spawnObject(SpawnPosition spawnPosition, Vector3 position, Quaternion rotation)
    {
        List<Texture> textureList = getTextureList(spawnPosition);

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
