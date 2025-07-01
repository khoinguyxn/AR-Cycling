using UnityEngine;

[System.Serializable]
public class ModelList
{
    //ATTRIBUTES
    public Model[] models;



    //METHODS
    public Model getModel(int index)
    {
        return models[index];
    }


    public int getLength()
    {
        return models.Length;
    }
}
