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
    private List<Model> getModelList(SpawnPosition spawnPosition)
    {
        switch (spawnPosition)
        {
            case SpawnPosition.side:
                return sideModels;
            case SpawnPosition.top:
                return topModels;
            case SpawnPosition.bottom:
                return bottomModels;
            default:
                return sideModels;
        }
    }


    public bool getListEmpty(SpawnPosition spawnPosition)
    {
        List<Model> modelList = getModelList(spawnPosition);
        return modelList.Count == 0;
    }


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


    public GameObject spawnObject(SpawnPosition spawnPosition, Vector3 position, Quaternion rotation, Vector3 localScale)
    {
        List<Model> modelList = getModelList(spawnPosition);

        int modelIndex = Random.Range(0, modelList.Count);
        Model model = modelList[modelIndex];
        modelList.RemoveAt(modelIndex);

        GameObject modelObject = Instantiate(model.model, position, rotation);
        modelObject.transform.localScale = localScale;
        addStatefulInteractable(modelObject);
        addCollider(modelObject);
        addAnimation(modelObject, model);

        //Set transforms
        modelObject.transform.rotation = Quaternion.Euler(model.eulerRotation);
        modelObject.transform.localScale = model.localScale;

        return modelObject;
    }
}
