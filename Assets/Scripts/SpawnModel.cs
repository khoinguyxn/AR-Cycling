using System.Collections.Generic;
using MixedReality.Toolkit;
using UnityEngine;

public class SpawnModel : MonoBehaviour, IObjectSpawner
{
    //ATTRIBUTES
    public List<Model> sideModels;
    public List<Model> topModels;
    public List<Model> bottomModels;



    //METHODS
    private void addStatefulInteractable(GameObject modelObject)
    {
        StatefulInteractable statefulInteractable = modelObject.AddComponent<StatefulInteractable>();
        GazeControl gazeControl = modelObject.AddComponent<GazeControl>();
        statefulInteractable.IsGazeHovered.OnEntered.AddListener(_ => gazeControl.startGaze());
        statefulInteractable.IsGazeHovered.OnExited.AddListener(_ => gazeControl.endGaze());
    }


    private void addCollider(GameObject modelObject)
    {
        if (modelObject.GetComponent<Collider>() == null)
        {
            modelObject.AddComponent<SphereCollider>();
        }
    }


    private void addAnimation(GameObject modelObject, Model model)
    {
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
    }


    public GameObject spawnObject(SpawnPosition spawnPosition, Vector3 position, Quaternion rotation)
    {
        List<Model> modelList;

        switch (spawnPosition)
        {
            case SpawnPosition.side:
                modelList = sideModels;
                break;
            case SpawnPosition.top:
                modelList = topModels;
                break;
            case SpawnPosition.bottom:
                modelList = bottomModels;
                break;
            default:
                modelList = sideModels;
                break;
        }

        int modelIndex = Random.Range(0, modelList.Count);
        Model model = modelList[modelIndex];
        modelList.RemoveAt(modelIndex);

        GameObject modelObject = Instantiate(model.model, position, rotation);
        addStatefulInteractable(modelObject);
        addCollider(modelObject);
        addAnimation(modelObject, model);

        //Set transforms
        modelObject.transform.rotation = Quaternion.Euler(model.eulerRotation);
        modelObject.transform.localScale = model.localScale;

        return modelObject;
    }
}
