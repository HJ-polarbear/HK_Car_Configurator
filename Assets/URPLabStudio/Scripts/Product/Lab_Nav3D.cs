namespace URPLabStudio
{
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Lab_Nav3D : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] GameObject exitIconPrefab;
    [SerializeField] GameObject backIconPrefab;

    [Header("Controller")]
    [SerializeField] Lab_InteriorCameraController interiorController;

    GameObject exitInstance;
    GameObject backInstance;
    bool initialized;

    public GameObject ExitIconPrefab => exitIconPrefab;
    public GameObject BackIconPrefab => backIconPrefab;

    void Awake()
    {
        Initialize(interiorController);
    }

    void OnDestroy()
    {
        DestroyRuntimeInstance(exitInstance);
        DestroyRuntimeInstance(backInstance);
    }

    public void Configure(Lab_InteriorCameraController controller, GameObject exitPrefab, GameObject backPrefab)
    {
        interiorController = controller;
        exitIconPrefab = exitPrefab;
        backIconPrefab = backPrefab;
        initialized = false;
    }

    public void Initialize(Lab_InteriorCameraController controller)
    {
        if (controller != null)
            interiorController = controller;
        if (initialized)
            return;

        initialized = true;
        BuildInstances();
        ShowExterior();
    }

    public void ShowExterior()
    {
        if (!initialized)
            Initialize(interiorController);

        if (exitInstance != null)
            exitInstance.SetActive(true);
        if (backInstance != null)
            backInstance.SetActive(false);
    }

    public void ShowInterior()
    {
        if (!initialized)
            Initialize(interiorController);

        if (exitInstance != null)
            exitInstance.SetActive(false);
        if (backInstance != null)
            backInstance.SetActive(true);
    }

    void BuildInstances()
    {
        if (exitInstance == null)
            exitInstance = CreateIcon(exitIconPrefab, "Nav3D_Exit", Lab_3DNavigationButton.Action.Exit, "EXIT");
        if (backInstance == null)
            backInstance = CreateIcon(backIconPrefab, "Nav3D_Back", Lab_3DNavigationButton.Action.Back, "BACK");
    }

    GameObject CreateIcon(GameObject prefab, string instanceName, Lab_3DNavigationButton.Action action, string label)
    {
        if (prefab == null)
        {
            Debug.LogWarning($"Lab_Nav3D is missing the {instanceName} prefab.", this);
            return null;
        }

        GameObject instance = Instantiate(prefab);
        instance.name = instanceName;

        Lab_3DNavigationButton button = instance.GetComponent<Lab_3DNavigationButton>();
        if (button == null)
            button = instance.AddComponent<Lab_3DNavigationButton>();
        button.Configure(interiorController, action, label);
        return instance;
    }

    static void DestroyRuntimeInstance(GameObject instance)
    {
        if (instance != null)
            Destroy(instance);
    }
}
}
