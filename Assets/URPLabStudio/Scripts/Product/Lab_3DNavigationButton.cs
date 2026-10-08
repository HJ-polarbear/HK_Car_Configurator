namespace URPLabStudio
{
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class Lab_3DNavigationButton : MonoBehaviour
{
    public enum Action { Exit, Back }

    [SerializeField] Action action;
    [SerializeField] Lab_InteriorCameraController controller;
    [SerializeField] Vector2 viewportPosition = new Vector2(.945f, .9f);
    [SerializeField, Min(.2f)] float cameraDistance = 1f;
    [SerializeField, Range(.01f, .2f)] float viewportHeight = .055f;
    [SerializeField] Color textColor = Color.white;

    MeshFilter meshFilter;
    Collider hitCollider;
    TextMesh label;

    public void Configure(Lab_InteriorCameraController owner, Action navigationAction, string labelText)
    {
        controller = owner;
        action = navigationAction;
        viewportPosition = action == Action.Exit ? new Vector2(.952f, .935f) : new Vector2(.952f, .53f);
        EnsureSetup(labelText.ToUpperInvariant());
    }

    void Awake()
    {
        EnsureSetup(action == Action.Exit ? "EXIT" : "BACK");
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        Camera activeCamera = Camera.main;
        if (mouse == null || activeCamera == null || hitCollider == null || !mouse.leftButton.wasReleasedThisFrame)
            return;

        Ray ray = activeCamera.ScreenPointToRay(mouse.position.ReadValue());
        if (!hitCollider.Raycast(ray, out _, activeCamera.farClipPlane))
            return;

        if (action == Action.Back)
            controller?.ExitInterior();
        else
            controller?.InvokeExit();
    }

    void LateUpdate()
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null || meshFilter == null || meshFilter.sharedMesh == null)
            return;

        if (transform.parent != activeCamera.transform)
            transform.SetParent(activeCamera.transform, false);

        transform.position = activeCamera.ViewportToWorldPoint(new Vector3(viewportPosition.x, viewportPosition.y, cameraDistance));
        transform.rotation = activeCamera.transform.rotation;

        float visibleHeight = activeCamera.orthographic
            ? activeCamera.orthographicSize * 2f
            : 2f * cameraDistance * Mathf.Tan(activeCamera.fieldOfView * .5f * Mathf.Deg2Rad);
        float meshHeight = Mathf.Max(.0001f, meshFilter.sharedMesh.bounds.size.y);
        float uniformScale = visibleHeight * viewportHeight / meshHeight;
        transform.localScale = Vector3.one * uniformScale;
    }

    void EnsureSetup(string labelText)
    {
        meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        hitCollider = GetComponent<Collider>();
        if (hitCollider == null)
        {
            BoxCollider box = gameObject.AddComponent<BoxCollider>();
            box.center = meshFilter.sharedMesh.bounds.center;
            box.size = meshFilter.sharedMesh.bounds.size * 1.25f;
            hitCollider = box;
        }

        Transform labelTransform = transform.Find("Navigation_Label");
        if (labelTransform == null)
        {
            GameObject labelObject = new GameObject("Navigation_Label");
            labelTransform = labelObject.transform;
            labelTransform.SetParent(transform, false);
            label = labelObject.AddComponent<TextMesh>();
        }
        else
        {
            label = labelTransform.GetComponent<TextMesh>();
            if (label == null)
                label = labelTransform.gameObject.AddComponent<TextMesh>();
        }

        Bounds bounds = meshFilter.sharedMesh.bounds;
        label.text = labelText;
        label.anchor = TextAnchor.UpperCenter;
        label.alignment = TextAlignment.Center;
        label.fontSize = 48;
        label.characterSize = Mathf.Max(.01f, bounds.size.y * .09f);
        label.color = textColor;
        labelTransform.localPosition = new Vector3(bounds.center.x, bounds.min.y - bounds.size.y * .2f, bounds.min.z - .01f);
        labelTransform.localRotation = Quaternion.identity;
    }
}
}
