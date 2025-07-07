using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ModelList
{
    //ATTRIBUTES
    public List<Model> models;



    //METHODS
    public Model getModel(int index)
    {
        return models[index];
    }


    public int getLength()
    {
        return models.Count;
    }


    public void popModel(int index)
    {
        models.RemoveAt(index);
    }
}
