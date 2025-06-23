using UnityEngine;


[System.Serializable]
public class Model
{
    //ATTRIBUTES
    public GameObject model;
    public RuntimeAnimatorController animatorController;
    public Vector3 eulerRotation;
    public Vector3 localScale;
    public float spinningPeriod = 300;
}
