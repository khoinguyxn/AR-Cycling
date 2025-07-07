using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TextureList
{
    //ATTRIBUTES
    public List<Texture> textures;



    //METHODS
    public Texture getTexture(int index)
    {
        return textures[index];
    }


    public int getLength()
    {
        return textures.Count;
    }


    public void popTexture(int index)
    {
        textures.RemoveAt(index);
    }
}
