using MixedReality.Toolkit;
using UnityEngine;

public class SpawnModel : SpawnObject
{
    //ATTRIBUTES
    public GameObject[] models;
    public RuntimeAnimatorController[] animatorControllers;



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
        int modelIndex = Random.Range(0, models.Length - 1);
        GameObject model = models[modelIndex];

        GameObject modelObject = Instantiate(model, position, rotation);
        addStatefulInteractable(modelObject);

        if (modelObject.GetComponent<Collider>() == null)
        {
            modelObject.AddComponent<BoxCollider>();
        }

        RuntimeAnimatorController animatorController = animatorControllers[modelIndex];
        Animator animator = modelObject.GetComponent<Animator>();
        if (animator == null)
        {
            animator = modelObject.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = animatorController;

        return modelObject;
    }
}
