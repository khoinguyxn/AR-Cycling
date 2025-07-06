using UnityEngine;

[System.Serializable]
public class TextureList
{
    //ATTRIBUTES
    public Texture[] textures;



    //METHODS
    public Texture getTexture(int index)
    {
        return textures[index];
    }


    public int getLength()
    {
        return textures.Length;
    }
}
