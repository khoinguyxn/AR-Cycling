using UnityEngine;


[System.Serializable]
public class Model
{
    //ATTRIBUTES
    public GameObject model;
    public RuntimeAnimatorController animatorController;
    public Quaternion rotation;
    public Vector3 localScale;
    public float spinningPeriod = 300;
}
