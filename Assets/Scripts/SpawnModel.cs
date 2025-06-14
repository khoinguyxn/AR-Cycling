using UnityEngine;

public class SpawnModel : SpawnObject
{
    //ATTRIBUTES
    public GameObject[] models;
    public RuntimeAnimatorController[] animatorControllers;



    //METHODS
    protected override GameObject spawnObject(Vector3 position, Quaternion rotation)
    {
        int modelIndex = Random.Range(0, models.Length - 1);
        GameObject model = models[modelIndex];
        RuntimeAnimatorController animatorController = animatorControllers[modelIndex];

        Animator animator = model.GetComponent<Animator>();
        if (animator == null)
        {
            animator = model.AddComponent<Animator>();
        }
        animator.runtimeAnimatorController = animatorController;

        return Instantiate(model, position, rotation);
    }
}
