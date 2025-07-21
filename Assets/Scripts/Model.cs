using UnityEngine;
using MixedReality.Toolkit;


public class Model : Notification
{
    //ATTRIBUTES
    public GameObject model;
    public float spinningPeriod = 300;
    public RuntimeAnimatorController animatorController;



    //METHODS
    public Model(Vector3 _position, Vector3 _eulerRotation, Vector3 _localScale, GameObject _model, float _spinningPeriod, RuntimeAnimatorController _animatorController)
    {
        position = _position;
        eulerRotation = _eulerRotation;
        localScale = _localScale;
        model = _model;
        spinningPeriod = _spinningPeriod;
        animatorController = _animatorController;
    }


    private void addStatefulInteractable(GameObject modelObject)
    {
        if (modelObject.GetComponent<StatefulInteractable>() == null)
        {
            modelObject.AddComponent<StatefulInteractable>();
        }
    }

    
    private void addCollider(GameObject modelObject)
    {
        if (modelObject.GetComponent<Collider>() == null)
        {
            modelObject.AddComponent<SphereCollider>();
        }
    }


    private void addAnimation(GameObject modelObject)
    {
        Animator animator = modelObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = modelObject.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = animatorController;

        if (animatorController == null)
        {
            SpinningAnimation spinningAnimation = modelObject.GetComponent<SpinningAnimation>();
            if (spinningAnimation == null)
            {
                spinningAnimation = modelObject.AddComponent<SpinningAnimation>();
            }
            spinningAnimation.model = modelObject;
            spinningAnimation.setActive(true);
            spinningAnimation.setDuration(spinningPeriod);
        }
    }


    public override GameObject spawnObject()
    {
        GameObject modelObject = Instantiate(model, position, getRotation());
        modelObject.transform.localScale = localScale;
        addStatefulInteractable(modelObject);
        addCollider(modelObject);
        addAnimation(modelObject);
        return modelObject;
    }
}
