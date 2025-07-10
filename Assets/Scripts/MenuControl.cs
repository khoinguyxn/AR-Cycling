using UnityEngine;

public class MenuController : MonoBehaviour
{
    [SerializeField] private GameObject topSignControl;
    [SerializeField] private GameObject sideSignControl;
    [SerializeField] private GameObject modelControl;
    [SerializeField] private GameObject signSelectorOptions;
    [SerializeField] private GameObject signSelector;
    [SerializeField] private GameObject gazeControl;

    public void Activate3DModels(bool state)
    {
        SetGameObjectActive(modelControl, state);
        ToggleGameObjectActive(signSelector);
    }

    public void ActivateTopSign(bool state)
    {
        SetGameObjectActive(topSignControl, state);
    }

    public void ActivateSideSign(bool state)
    {
        SetGameObjectActive(sideSignControl, state);
    }

    public void HandleSelect2DModelPositions(string position)
    {
        switch (position.ToLower())
        {
            case "top":
                ActivateTopSign(true);
                break;
            case "side":
                ActivateSideSign(true);
                break;
            default:
                Debug.LogWarning($"Unknown position: {position}");
                break;
        }
    }

    public void ShowSignSelectorOptions()
    {
        ToggleGameObjectActive(signSelectorOptions);
    }

    public void QuitApplication()
    {
        Application.Quit();
    }

    public void StartApplication()
    {
        StartEyeTracking();
        CloseMenu();
    }

    private static void SetGameObjectActive(GameObject target, bool state)
    {
        if (target != null)
        {
            target.SetActive(state);
        }
        else
        {
            Debug.LogWarning("Target GameObject is null");
        }
    }

    private static void ToggleGameObjectActive(GameObject target)
    {
        if (target != null)
        {
            target.SetActive(!target.activeSelf);
        }
        else
        {
            Debug.LogWarning("Target GameObject is null");
        }
    }

    private void StartEyeTracking()
    {
        SetGameObjectActive(gazeControl, true);
    }

    private void CloseMenu()
    {
        SetGameObjectActive(gameObject, false);
    }
}