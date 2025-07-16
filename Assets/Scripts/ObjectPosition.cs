using UnityEngine;

public class ObjectPosition
{
    //ATTRIBUTES
    private Vector2 displacement;
    private Vector3 eulerRotation;
    private Vector3 localScale;



    //METHODS
    public ObjectPosition(Vector2 _displacement, Vector3 _eulerRotation, Vector3 _localScale)
    {
        displacement = _displacement;
        eulerRotation = _eulerRotation;
        localScale = _localScale;
    }


    public float getXDisplacement()
    {
        return displacement.x;
    }


    public float getYDisplacement()
    {
        return displacement.y;
    }


    public float getXRotation()
    {
        return eulerRotation.x;
    }


    public float getYRotation()
    {
        return eulerRotation.y;
    }


    public float getZRotation()
    {
        return eulerRotation.z;
    }


    public Vector3 getLocalScale()
    {
        return localScale;
    }
}
