using UnityEngine;

public class ObjectPosition
{
    //ATTRIBUTES
    private Vector2 displacement;
    private Vector3 eulerRotation;



    //METHODS
    public ObjectPosition(Vector2 _displacement, Vector3 _eulerRotation)
    {
        displacement = _displacement;
        eulerRotation = _eulerRotation;
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
}
