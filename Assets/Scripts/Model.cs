using UnityEngine;
using MixedReality.Toolkit;


[System.Serializable]
public class Model : Notification
{
    //ATTRIBUTES
    public GameObject model;
    public RuntimeAnimatorController animatorController;
    public float spinningPeriod = 300;



    //METHODS
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
