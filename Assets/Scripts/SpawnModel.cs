using MixedReality.Toolkit;
using UnityEditor;
using UnityEngine;

public class SpawnModel : SpawnObject
{
    //ATTRIBUTES
    public Model[] models;



    //METHODS
    private void addStatefulInteractable(GameObject modelObject)
    {
        StatefulInteractable statefulInteractable = modelObject.AddComponent<StatefulInteractable>();
        GazeControl gazeControl = modelObject.AddComponent<GazeControl>();
        statefulInteractable.IsGazeHovered.OnEntered.AddListener(_ => gazeControl.startGaze());
        statefulInteractable.IsGazeHovered.OnExited.AddListener(_ => gazeControl.endGaze());
    }


    protected override GameObject spawnObject(Vector3 position, Quaternion rotation)
    {
        int modelIndex = Random.Range(0, models.Length);
        Model model = models[modelIndex];

        GameObject modelObject = Instantiate(model.model, position, rotation);
        addStatefulInteractable(modelObject);

        if (modelObject.GetComponent<Collider>() == null)
        {
            modelObject.AddComponent<SphereCollider>();
        }

        RuntimeAnimatorController animatorController = model.animatorController;
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
            spinningAnimation.setDuration(model.spinningPeriod);
        }

        //Set transforms
        modelObject.transform.rotation = Quaternion.Euler(model.eulerRotation);
        modelObject.transform.localScale = model.localScale;

        return modelObject;
    }
}
