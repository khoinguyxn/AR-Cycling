using UnityEngine;
using System.Collections.Generic;

public class SpawnNotification : MonoBehaviour
{
    //ATTRIBUTES
    public List<List<Notification>> notificationLists;
    private List<Notification> notifications;
    public int notificationListIndex = 0;
    public float spawnDistance = 20;
    public GameObject notificationControl;
    public Camera userCamera;
    public GameObject signObject;
    public Material signMaterial;
    public AudioSource audioSource;
    private GameObject currentObject;
    private GameObject previousObject;



    //METHODS
    public Sprite CreateSprite(Vector3 position, Vector3 eulerRotation, Vector3 localScale, string textureLocation)
    {
        Texture texture = Resources.Load<Texture>(textureLocation);
        return new Sprite(position, eulerRotation, localScale, texture, signObject, signMaterial);
    }


    public Model CreateModel(Vector3 position, Vector3 eulerRotation, Vector3 localScale, string modelLocation, float spinningPeriod, string animatorLocation = null)
    {
        GameObject model = Resources.Load<GameObject>(modelLocation);
        RuntimeAnimatorController animator = Resources.Load<RuntimeAnimatorController>(animatorLocation);
        return new Model(position, eulerRotation, localScale, model, spinningPeriod, animator);
    }


    private void SpawnNotificationInstance(Notification notification)
    {
        Destroy(previousObject);
        previousObject = currentObject;
        currentObject = notification.SpawnObject();
        audioSource.Play();
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        notificationLists = new List<List<Notification>>
        {
            new List<Notification>
            {
                CreateSprite(new Vector3(-2, 1.5f, 30), new Vector3(0, 0, 0), new Vector3(1, 1, 1), "SignImages/40_zone"),
                CreateModel(new Vector3(0, 6, 70), new Vector3(0, 0, 0), new Vector3(50, 50, 50), "Models/Cafe/Cafe", 5),
            }
        };

        notifications = notificationLists[notificationListIndex];
        notifications.Reverse(); //Reverse notification list items so that items are popped from the end (O(1) time as opposed to O(n)).
    }

    // Update is called once per frame
    void Update()
    {
        if (notifications.Count > 0)
        {
            Notification notification = notifications[notifications.Count - 1];
            if (notification.CheckSpawn(userCamera.transform.position, spawnDistance))
            {
                SpawnNotificationInstance(notification);
                notifications.RemoveAt(notifications.Count - 1);
            }
        }
    }
}
