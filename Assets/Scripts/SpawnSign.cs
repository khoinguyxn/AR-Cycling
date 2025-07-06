using UnityEngine;


public class SpawnSign : SpawnObject
{
    //ATTRIBUTES
    public GameObject signObject;
    public Material signMaterial;
    public TextureList[] textures;
    public int textureListIndex;



    //METHODS
    protected override GameObject spawnObject(Vector3 position, Quaternion rotation)
    {
        TextureList textureList = textures[textureListIndex];
        int textureIndex = Random.Range(0, textureList.getLength());
        Texture texture = textureList.getTexture(textureIndex);

        GameObject signInstance = Instantiate(signObject, position, rotation);

        signMaterial.mainTexture = texture;

        GameObject signQuad = signInstance.transform.Find("SignBoard/SignQuad").gameObject;
        MeshRenderer imageMeshRenderer = signQuad.GetComponent<MeshRenderer>();
        if (imageMeshRenderer != null)
        {
            imageMeshRenderer.material = signMaterial;
        }

        return signInstance;
    }
}
