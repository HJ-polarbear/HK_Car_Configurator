namespace URPLabStudio
{
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class Lab_OrbitPanelController : MonoBehaviour
{
    [Serializable]
    public sealed class CategoryPanel
    {
        public string categoryId;
        public GameObject panel;
    }

    [Header("Orbit Ring")]
    [SerializeField] Transform orbitRoot;
    [SerializeField] float rotationSpeed = -3.5f;
    [SerializeField, Min(.05f)] float speedBlendDuration = .55f;
    [SerializeField, Min(0f)] float resumeDelay = .7f;
    [SerializeField, Min(0f)] float hoverLift = .035f;
    [SerializeField, Min(.05f)] float liftSpeed = 7f;
    [SerializeField] Color selectedTint = new Color(1f, .22f, .035f, 1f);

    [Header("Context Panels")]
    [SerializeField] Transform contextRoot;
    [SerializeField] List<CategoryPanel> categoryPanels = new List<CategoryPanel>();
    [SerializeField, Min(.05f)] float panelMotionDuration = .32f;
    [SerializeField] float panelRiseDistance = 18f;

    [Header("Automatic Panel Close")]
    [SerializeField] bool closeWhenAnotherSlotFacesCamera = true;
    [SerializeField, Range(1f, 30f)] float frontFacingAngle = 8f;
    [SerializeField, Min(0f)] float frontFacingHoldDuration = .12f;

    [Header("Interior Camera")]
    [SerializeField] Lab_InteriorCameraController interiorCameraController;

    readonly List<Lab_OrbitControlSlot> categorySlots = new List<Lab_OrbitControlSlot>();
    readonly Dictionary<Lab_OrbitControlSlot, Vector3> basePositions = new Dictionary<Lab_OrbitControlSlot, Vector3>();
    readonly Dictionary<RectTransform, Vector2> panelPositions = new Dictionary<RectTransform, Vector2>();
    Lab_OrbitControlSlot hovered;
    Lab_OrbitControlSlot selected;
    Lab_OrbitControlSlot frontCandidate;
    Coroutine panelMotion;
    float currentSpeed;
    float resumeTimer;
    float frontCandidateTimer;

    public Transform OrbitRoot => orbitRoot;
    public Transform ContextRoot => contextRoot;
    public IReadOnlyList<CategoryPanel> CategoryPanels => categoryPanels;
    public string SelectedCategory => selected != null ? selected.CategoryId : string.Empty;
    public Lab_InteriorCameraController InteriorCameraController => interiorCameraController;

    void Reset()
    {
        orbitRoot = transform;
    }

    void Awake()
    {
        if (orbitRoot == null)
            orbitRoot = transform;

        RebuildSlotCache();
        currentSpeed = rotationSpeed;
    }

    void Start()
    {
        selected = null;
        hovered = null;
        SetContextVisible(false);
        RefreshCategoryVisuals();
    }

    void Update()
    {
        UpdatePointerInput();
        UpdateOrbitMotion();
        UpdateSlotLift();
    }

    void LateUpdate()
    {
        UpdateAutomaticPanelClose();
    }

    public void Register(Lab_OrbitControlSlot slot)
    {
        if (slot == null || !slot.IsCategory || categorySlots.Contains(slot))
            return;

        categorySlots.Add(slot);
        basePositions[slot] = slot.transform.localPosition;
    }

    public void Unregister(Lab_OrbitControlSlot slot)
    {
        categorySlots.Remove(slot);
        basePositions.Remove(slot);
        if (hovered == slot)
            hovered = null;
        if (selected == slot)
            selected = null;
    }

    public void RebuildSlotCache()
    {
        categorySlots.Clear();
        basePositions.Clear();
        Transform searchRoot = orbitRoot != null ? orbitRoot : transform;
        foreach (Lab_OrbitControlSlot slot in searchRoot.GetComponentsInChildren<Lab_OrbitControlSlot>(true))
        {
            if (!slot.IsCategory)
                continue;
            slot.AssignOwner(this);
            Register(slot);
        }
    }

    public void Configure(Transform ringRoot, Transform panelRoot, IEnumerable<CategoryPanel> panels)
    {
        orbitRoot = ringRoot != null ? ringRoot : transform;
        contextRoot = panelRoot;
        categoryPanels.Clear();
        if (panels != null)
            categoryPanels.AddRange(panels.Where(binding => binding != null && binding.panel != null));
        RebuildSlotCache();
    }

    public void SetInteriorCameraController(Lab_InteriorCameraController controller)
    {
        interiorCameraController = controller;
    }

    public void AutoDiscoverPanels()
    {
        categoryPanels.Clear();
        if (contextRoot == null)
            return;

        foreach (Transform child in contextRoot)
        {
            string category = ResolveCategory(child.name);
            if (string.IsNullOrEmpty(category))
                continue;
            categoryPanels.Add(new CategoryPanel { categoryId = category, panel = child.gameObject });
        }
    }

    public string ValidateConfiguration()
    {
        List<string> issues = new List<string>();
        if (orbitRoot == null)
            issues.Add("Orbit Root is not assigned.");
        Lab_OrbitControlSlot[] slots = orbitRoot != null
            ? orbitRoot.GetComponentsInChildren<Lab_OrbitControlSlot>(true)
                .Where(slot => slot != null && slot.IsCategory).ToArray()
            : Array.Empty<Lab_OrbitControlSlot>();
        if (slots.Length == 0)
            issues.Add("No category slots were found under Orbit Root.");
        if (contextRoot == null)
            issues.Add("Context Root is not assigned.");
        if (categoryPanels.Count == 0)
            issues.Add("No context panels are mapped.");
        foreach (Lab_OrbitControlSlot slot in slots)
        {
            if (string.IsNullOrWhiteSpace(slot.CategoryId))
                issues.Add($"Category ID is empty on {slot.name}.");
        }
        return issues.Count == 0 ? "Setup is valid." : string.Join("\n", issues);
    }

    public void SetHovered(Lab_OrbitControlSlot slot)
    {
        if (slot != null && !slot.IsCategory)
            return;
        hovered = slot;
        RefreshCategoryVisuals();
    }

    public void ClearHovered(Lab_OrbitControlSlot slot)
    {
        if (hovered != slot)
            return;
        hovered = null;
        resumeTimer = resumeDelay;
        RefreshCategoryVisuals();
    }

    public void SelectCategory(Lab_OrbitControlSlot slot)
    {
        if (slot == null || !slot.IsCategory)
            return;

        if (selected == slot)
        {
            selected = null;
            hovered = null;
            ResetFrontCandidate();
            resumeTimer = resumeDelay;
            SetContextVisible(false);
            RefreshCategoryVisuals();
            return;
        }

        selected = slot;
        ResetFrontCandidate();
        resumeTimer = resumeDelay;

        if (string.Equals(slot.CategoryId, "INTERIOR", StringComparison.OrdinalIgnoreCase) &&
            interiorCameraController != null)
        {
            SetContextVisible(false);
            RefreshCategoryVisuals();
            interiorCameraController.EnterInterior();
            return;
        }

        ShowPanel(slot.CategoryId);
        RefreshCategoryVisuals();
    }

    public void ClearSelection()
    {
        selected = null;
        hovered = null;
        ResetFrontCandidate();
        resumeTimer = resumeDelay;
        SetContextVisible(false);
        RefreshCategoryVisuals();
    }

    public void InvokeAction(Lab_OrbitControlSlot slot)
    {
        if (slot == null || slot.IsCategory)
            return;

        Lab_OrbitControlSlot[] siblings = slot.transform.parent != null
            ? slot.transform.parent.GetComponentsInChildren<Lab_OrbitControlSlot>(true)
            : Array.Empty<Lab_OrbitControlSlot>();
        foreach (Lab_OrbitControlSlot sibling in siblings)
        {
            if (!sibling.IsCategory)
                sibling.SetActionSelected(sibling == slot);
        }

        switch (slot.Action)
        {
            case Lab_OrbitControlSlot.ActionType.Wheel:
                Lab_WheelSelection wheel = FindObjectsByType<Lab_WheelSelection>(FindObjectsInactive.Include).FirstOrDefault();
                if (wheel != null)
                    wheel.Select(slot.OptionIndex);
                else
                    FindObjectsByType<Lab_WheelSelector>(FindObjectsInactive.Include).FirstOrDefault()?.SelectWheel(slot.OptionIndex);
                break;
            case Lab_OrbitControlSlot.ActionType.Paint:
                Lab_WavePaintController paint = FindObjectsByType<Lab_WavePaintController>(FindObjectsInactive.Include).FirstOrDefault();
                if (paint != null)
                {
                    if (!paint.ApplyPreset(slot.OptionIndex))
                        paint.ApplyColorWithWave(slot.PaintColor);
                }
                break;
            case Lab_OrbitControlSlot.ActionType.Camera:
                SelectCamera(slot.OptionIndex);
                break;
            case Lab_OrbitControlSlot.ActionType.Door:
                ToggleDoor(slot.OptionIndex);
                break;
            case Lab_OrbitControlSlot.ActionType.Lights:
                ToggleVehicleLights();
                break;
            case Lab_OrbitControlSlot.ActionType.Explode:
                ToggleVehicleExplode();
                break;
        }

        slot.InvokeCustomAction();
    }

    void UpdatePointerInput()
    {
        if (Mouse.current == null || Camera.main == null)
            return;

        if (IsPointerOverSelectable())
        {
            if (hovered != null)
                ClearHovered(hovered);
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit[] hits = Physics.RaycastAll(ray, Mathf.Infinity, ~0, QueryTriggerInteraction.Collide);
        Lab_OrbitControlSlot candidate = null;
        float nearest = float.PositiveInfinity;
        foreach (RaycastHit hit in hits)
        {
            Lab_OrbitControlSlot slot = hit.collider.GetComponentInParent<Lab_OrbitControlSlot>();
            if (slot == null || !slot.IsCategory || slot.Owner != this || hit.distance >= nearest)
                continue;
            candidate = slot;
            nearest = hit.distance;
        }

        if (candidate != hovered)
        {
            if (hovered != null)
                ClearHovered(hovered);
            if (candidate != null)
                SetHovered(candidate);
        }

        if (candidate != null && Mouse.current.leftButton.wasPressedThisFrame)
            SelectCategory(candidate);
    }

    void UpdateOrbitMotion()
    {
        bool paused = hovered != null || selected != null;
        if (paused)
            resumeTimer = resumeDelay;
        else if (resumeTimer > 0f)
            resumeTimer -= Time.unscaledDeltaTime;

        float targetSpeed = paused || resumeTimer > 0f ? 0f : rotationSpeed;
        float acceleration = Mathf.Abs(rotationSpeed) / Mathf.Max(.05f, speedBlendDuration);
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.unscaledDeltaTime);
        if (orbitRoot != null)
            orbitRoot.Rotate(Vector3.up, -currentSpeed * Time.unscaledDeltaTime, Space.Self);
    }

    void UpdateSlotLift()
    {
        foreach (Lab_OrbitControlSlot slot in categorySlots)
        {
            if (slot == null || !basePositions.TryGetValue(slot, out Vector3 origin))
                continue;
            Vector3 target = origin + Vector3.up * (slot == hovered ? hoverLift : 0f);
            slot.transform.localPosition = Vector3.Lerp(slot.transform.localPosition, target,
                1f - Mathf.Exp(-liftSpeed * Time.unscaledDeltaTime));
        }
    }

    void UpdateAutomaticPanelClose()
    {
        if (!closeWhenAnotherSlotFacesCamera || selected == null || orbitRoot == null || Camera.main == null)
        {
            ResetFrontCandidate();
            return;
        }

        Vector3 center = orbitRoot.position;
        Vector3 up = orbitRoot.up;
        Vector3 cameraDirection = Vector3.ProjectOnPlane(Camera.main.transform.position - center, up);
        if (cameraDirection.sqrMagnitude < .0001f)
        {
            ResetFrontCandidate();
            return;
        }
        cameraDirection.Normalize();

        Lab_OrbitControlSlot nearestSlot = null;
        float nearestAngle = float.PositiveInfinity;
        foreach (Lab_OrbitControlSlot slot in categorySlots)
        {
            if (slot == null)
                continue;
            Vector3 slotDirection = Vector3.ProjectOnPlane(slot.transform.position - center, up);
            if (slotDirection.sqrMagnitude < .0001f)
                continue;
            float angle = Vector3.Angle(cameraDirection, slotDirection.normalized);
            if (angle >= nearestAngle)
                continue;
            nearestAngle = angle;
            nearestSlot = slot;
        }

        if (nearestSlot == null || nearestSlot == selected || nearestAngle > frontFacingAngle)
        {
            ResetFrontCandidate();
            return;
        }

        if (frontCandidate != nearestSlot)
        {
            frontCandidate = nearestSlot;
            frontCandidateTimer = 0f;
            return;
        }

        frontCandidateTimer += Time.unscaledDeltaTime;
        if (frontCandidateTimer < frontFacingHoldDuration)
            return;

        selected = null;
        ResetFrontCandidate();
        resumeTimer = resumeDelay;
        SetContextVisible(false);
        RefreshCategoryVisuals();
    }

    void ResetFrontCandidate()
    {
        frontCandidate = null;
        frontCandidateTimer = 0f;
    }

    void RefreshCategoryVisuals()
    {
        foreach (Lab_OrbitControlSlot slot in categorySlots)
            if (slot != null)
                slot.SetCategoryState(slot == hovered, slot == selected, selectedTint);
    }

    void ShowPanel(string categoryId)
    {
        SetContextVisible(true);
        GameObject activePanel = null;
        foreach (CategoryPanel binding in categoryPanels)
        {
            if (binding == null || binding.panel == null)
                continue;
            bool active = string.Equals(binding.categoryId, categoryId, StringComparison.OrdinalIgnoreCase);
            binding.panel.SetActive(active);
            if (active)
                activePanel = binding.panel;
        }

        if (activePanel == null)
            return;
        if (panelMotion != null)
            StopCoroutine(panelMotion);
        panelMotion = StartCoroutine(AnimatePanel(activePanel));
    }

    IEnumerator AnimatePanel(GameObject panel)
    {
        CanvasGroup group = panel.GetComponent<CanvasGroup>();
        if (group == null)
            group = panel.AddComponent<CanvasGroup>();
        RectTransform rect = panel.transform as RectTransform;
        Vector2 targetPosition = Vector2.zero;
        if (rect != null)
        {
            if (!panelPositions.TryGetValue(rect, out targetPosition))
            {
                targetPosition = rect.anchoredPosition;
                panelPositions[rect] = targetPosition;
            }
            rect.anchoredPosition = targetPosition - Vector2.up * panelRiseDistance;
        }

        group.alpha = 0f;
        group.interactable = false;
        float elapsed = 0f;
        while (elapsed < panelMotionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / panelMotionDuration);
            t = 1f - Mathf.Pow(1f - t, 3f);
            group.alpha = t;
            if (rect != null)
                rect.anchoredPosition = Vector2.Lerp(targetPosition - Vector2.up * panelRiseDistance, targetPosition, t);
            yield return null;
        }

        group.alpha = 1f;
        group.interactable = true;
        if (rect != null)
            rect.anchoredPosition = targetPosition;
        panelMotion = null;
    }

    void SetContextVisible(bool visible)
    {
        if (contextRoot != null && contextRoot.gameObject.activeSelf != visible)
            contextRoot.gameObject.SetActive(visible);

        if (contextRoot != null)
        {
            Transform background = contextRoot.Find("RoundedCardBackground");
            if (background != null && background.gameObject.activeSelf != visible)
                background.gameObject.SetActive(visible);
        }
    }

    static bool IsPointerOverSelectable()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || Mouse.current == null)
            return false;
        PointerEventData pointer = new PointerEventData(eventSystem)
        {
            position = Mouse.current.position.ReadValue()
        };
        List<RaycastResult> results = new List<RaycastResult>();
        eventSystem.RaycastAll(pointer, results);
        return results.Any(result => result.gameObject.GetComponentInParent<Selectable>() != null);
    }

    static string ResolveCategory(string value)
    {
        string upper = value.ToUpperInvariant();
        string[] categories = { "WHEELS", "EXTERIOR", "INTERIOR", "CAMERA", "DOORS", "LIGHTS", "EXPLODE" };
        return categories.FirstOrDefault(upper.Contains) ?? string.Empty;
    }

    static void SelectCamera(int index)
    {
        Lab_CameraDirector camera = FindObjectsByType<Lab_CameraDirector>(FindObjectsInactive.Include).FirstOrDefault();
        if (camera == null)
            return;
        if (index == 0) camera.SelectClose();
        else if (index == 1) camera.SelectMid();
        else if (index == 2) camera.SelectFull();
        else if (index == 3) camera.SelectTurntable();
        else camera.SelectOrbit();
    }

    static void ToggleDoor(int index)
    {
        Lab_OpenElement[] elements = FindObjectsByType<Lab_OpenElement>(FindObjectsInactive.Include);
        string token = index == 0 ? "FL" : index == 1 ? "FR" : "Trunk";
        Lab_OpenElement element = elements.FirstOrDefault(candidate =>
            candidate.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        if (element == null && index >= 0 && index < elements.Length)
            element = elements[index];
        element?.Toggle();
    }

    static void ToggleVehicleLights()
    {
        Lab_Light_On_Off[] controllers = FindObjectsByType<Lab_Light_On_Off>(FindObjectsInactive.Include);
        Light[] lights = controllers
            .Where(controller => controller != null && controller.gameObject.activeInHierarchy && controller.lights != null)
            .SelectMany(controller => controller.lights)
            .Where(light => light != null)
            .Distinct()
            .ToArray();
        if (lights.Length == 0)
        {
            Debug.LogWarning("URP Orbit: no connected vehicle lights were found.");
            return;
        }
        bool nextState = !lights.Any(light => light.enabled);
        foreach (Light light in lights)
            light.enabled = nextState;
        foreach (Lab_LightFlare flare in FindObjectsByType<Lab_LightFlare>(FindObjectsInactive.Include))
            if (flare != null && flare.gameObject.activeInHierarchy)
                flare.SetLights(nextState);
        Lab_ConfiguratorAudio.Instance?.PlayLights(nextState);
    }

    static void ToggleVehicleExplode()
    {
        Lab_ExplodeController controller = FindObjectsByType<Lab_ExplodeController>(FindObjectsInactive.Include)
            .FirstOrDefault(candidate => candidate != null && candidate.gameObject.activeInHierarchy);
        if (controller == null)
        {
            Debug.LogWarning("URP Orbit: no active vehicle explode controller was found.");
            return;
        }
        controller.ToggleExplode();
    }
}
}
