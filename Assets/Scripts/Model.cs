using UnityEngine;


[System.Serializable]
public class Model : Notification
{
    //ATTRIBUTES
    public GameObject model;
    public RuntimeAnimatorController animatorController;
    public float spinningPeriod = 300;



    //METHODS
    public override GameObject spawnObject()
    {
        GameObject modelObject = Instantiate(model, position, getRotation());
        return modelObject;
    }
}
