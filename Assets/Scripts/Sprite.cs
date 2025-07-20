using UnityEngine;


[System.Serializable]
public class Sprite : Notification
{
    //ATTRIBUTES
    public Texture texture;
    public GameObject signObject;



    //METHODS
    public override GameObject spawnObject()
    {
        GameObject spriteObject = Instantiate(signObject, position, getRotation());
        spriteObject.transform.localScale = localScale;
        return spriteObject;
    }
}
