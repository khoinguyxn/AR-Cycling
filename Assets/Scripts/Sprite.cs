using UnityEngine;


public class Sprite : Notification
{
    //ATTRIBUTES
    private Texture texture;
    private GameObject signObject;
    private Material signMaterial;



    //METHODS
    public Sprite(bool _playAudio, SpawnPosition _spawnPosition, Texture _texture, GameObject _signObject, Material _signMaterial)
    {
        playAudio = _playAudio;
        spawnPosition = _spawnPosition;
        texture = _texture;
        signObject = _signObject;
        signMaterial = _signMaterial;
    }


    public override GameObject SpawnObject(Vector3 position, Quaternion rotation, Vector3 localScale)
    {
        GameObject spriteObject = Instantiate(signObject, position + spawnPosition.GetYDisplacementVector(), rotation * spawnPosition.GetRotation());
        spriteObject.name = GetExportName();
        spriteObject.transform.localScale = GetLocalScale(localScale);

        MeshRenderer imageMeshRenderer = spriteObject.GetComponent<MeshRenderer>();
        if (imageMeshRenderer != null)
        {
            Material runtimeMaterial = new Material(signMaterial);
            runtimeMaterial.mainTexture = texture;
            imageMeshRenderer.material = runtimeMaterial;
        }

        return spriteObject;
    }


    public override string GetExportName()
    {
        return texture != null ? texture.name : signObject.name;
    }
}
