using UnityEngine;


public class SpawnSign : MonoBehaviour, IObjectSpawner
{
    //ATTRIBUTES
    public GameObject signObject;
    public Material signMaterial;
    public TextureList[] textures;
    public int textureListIndex;



    //METHODS
    public GameObject spawnObject(Vector3 position, Quaternion rotation)
    {
        TextureList textureList = textures[textureListIndex];
        int textureIndex = Random.Range(0, textureList.getLength());
        Texture texture = textureList.getTexture(textureIndex);
        textureList.popTexture(textureIndex);

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
