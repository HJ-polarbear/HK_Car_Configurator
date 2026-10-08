namespace URPLabStudio
{
using System.Collections.Generic;
using URPLabStudio.LabMotion;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Lab_ExplodeController : MonoBehaviour, ISerializationCallbackReceiver
{
    [SerializeField] private Transform visualRoot;
    [SerializeField, Min(0f)] private float distance = 1.25f;
    [SerializeField, Min(0.05f)] private float duration = 0.45f;
    [SerializeField, Min(0f)] private float stagger = 0.2f;
    [SerializeField] private Lab_Ease ease = Lab_Ease.InOutCubic;
    [Header("Part name filter")]
    [Tooltip("Only transforms whose names begin with this prefix are exploded.")]
    [SerializeField] private string partNamePrefix = "Lab_";
    [Tooltip("Only matching renderer objects below these named hierarchy roots are exploded.")]
    [SerializeField] private string[] targetRootNames = { "Animation_MD", "Exterior_GT" };

    private readonly List<Transform> parts = new List<Transform>();
    private readonly List<Vector3> originalPositions = new List<Vector3>();
    private readonly List<Vector3> originalScales = new List<Vector3>();
    private readonly List<string> partGroups = new List<string>();
    private bool exploded;

    public bool IsExploded => exploded;

    private void Awake() => CacheParts();

    public void ToggleExplode() => SetExploded(!exploded);

    public void SetExploded(bool value)
    {
        if (parts.Count == 0) CacheParts();
        exploded = value;
        Bounds vehicleBounds = default;
        bool hasVehicleBounds = false;
        Dictionary<string, Bounds> groupBounds = new Dictionary<string, Bounds>();
        Dictionary<string, int> groupOrder = new Dictionary<string, int>();
        for (int i = 0; i < parts.Count; i++)
        {
            Renderer renderer = parts[i] != null ? parts[i].GetComponent<Renderer>() : null;
            if (renderer == null) continue;
            string group = partGroups[i];
            Bounds rendererBounds = renderer.bounds;
            if (!hasVehicleBounds)
            {
                vehicleBounds = rendererBounds;
                hasVehicleBounds = true;
            }
            else vehicleBounds.Encapsulate(rendererBounds);

            if (groupBounds.TryGetValue(group, out Bounds combined))
            {
                combined.Encapsulate(rendererBounds);
                groupBounds[group] = combined;
            }
            else groupBounds[group] = rendererBounds;
            if (!groupOrder.ContainsKey(group)) groupOrder[group] = groupOrder.Count;
        }

        for (int i = 0; i < parts.Count; i++)
        {
            Transform part = parts[i];
            if (part == null) continue;
            Lab_MotionScheduler.Current.Cancel(part, MotionChannel.LocalPosition);
            Lab_MotionScheduler.Current.Cancel(part, MotionChannel.Scale);
            Vector3 target = originalPositions[i];
            float groupDelay = stagger <= 0f ? 0f : (groupOrder[partGroups[i]] % 5) * stagger / 5f;
            if (value)
            {
                string group = partGroups[i];
                Vector3 groupCenter = groupBounds.TryGetValue(group, out Bounds bounds)
                    ? bounds.center
                    : part.position;
                Vector3 center = hasVehicleBounds ? vehicleBounds.center : visualRoot.position;
                Vector3 direction = groupCenter - center;
                if (direction.sqrMagnitude < 0.0001f)
                    direction = visualRoot.TransformDirection(FallbackDirection(group));

                // Convert one world-space displacement vector directly into each
                // mesh parent's local space. InverseTransformVector handles the
                // model's 0.1 scale and any rotated/non-uniform parent correctly.
                Vector3 worldOffset = direction.normalized * distance;
                target += part.parent.InverseTransformVector(worldOffset);

                Lab_TransformMotions.LocalPosition(
                    part, target, duration,
                    new MotionOptions(groupDelay, ease));
                Lab_TransformMotions.Scale(
                    part, Vector3.zero, duration,
                    new MotionOptions(groupDelay + duration, Lab_Ease.InCubic));
            }
            else
            {
                // Match the URP sequence: restore scale first, then return the
                // assembly to its cached position.
                Lab_TransformMotions.Scale(
                    part, originalScales[i], duration,
                    new MotionOptions(groupDelay, Lab_Ease.OutCubic));
                Lab_TransformMotions.LocalPosition(
                    part, target, duration,
                    new MotionOptions(groupDelay + duration, ease));
            }
        }

        Lab_ConfiguratorAudio.Instance?.PlayExplode();
    }

    private void CacheParts()
    {
        parts.Clear();
        originalPositions.Clear();
        originalScales.Clear();
        partGroups.Clear();
        if (visualRoot == null) visualRoot = transform;

        List<Transform> targetRoots = new List<Transform>();
        foreach (Transform candidate in visualRoot.GetComponentsInChildren<Transform>(true))
        {
            foreach (string rootName in targetRootNames)
            {
                if (candidate.name.Equals(rootName, System.StringComparison.OrdinalIgnoreCase))
                {
                    targetRoots.Add(candidate);
                    break;
                }
            }
        }

        foreach (Transform root in targetRoots)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child == root || !child.gameObject.activeInHierarchy) continue;
                if (!child.name.StartsWith(partNamePrefix, System.StringComparison.OrdinalIgnoreCase)) continue;
                // Select actual mesh objects only; container transforms must remain fixed.
                if (child.GetComponent<Renderer>() == null) continue;
                parts.Add(child);
                originalPositions.Add(child.localPosition);
                originalScales.Add(child.localScale);
                partGroups.Add(GetPartGroup(child.name));
            }
        }

        if (parts.Count == 0)
            Debug.LogWarning($"HDRP Explode: no active renderer transforms beginning with '{partNamePrefix}' were found below Animation_MD or Exterior_GT.", this);
    }

    private static string GetPartGroup(string objectName)
    {
        string name = objectName.ToLowerInvariant();
        // Door glass, handles, seams and speakers must follow their door instead
        // of flying away as individual renderer objects.
        if (name.Contains("door_l") || name.Contains("l_door")) return "door_left";
        if (name.Contains("door_r") || name.Contains("r_door")) return "door_right";
        if (name.Contains("bonnet") || name.Contains("hood") || name.Contains("wiper")) return "hood";
        if (name.Contains("trunk") || name.Contains("boot")) return "trunk";
        if (name.Contains("f_bumper") || name.Contains("front_bumper") || name.Contains("f_numberplate")) return "bumper_front";
        if (name.Contains("r_bumper") || name.Contains("rear_bumper") || name.Contains("rear_numberplate")) return "bumper_rear";
        if (name.Contains("l_backmirror")) return "mirror_left";
        if (name.Contains("r_backmirror")) return "mirror_right";
        if (name.Contains("window") || name.Contains("glass")) return "glass";
        if (name.Contains("side_air") || name.Contains("fender")) return "front_side";
        if (name.Contains("engine")) return "engine";
        if (name.Contains("seat") || name.Contains("interior")) return "interior";
        if (name.Contains("undercarriage")) return "undercarriage";
        return "body_shell";
    }

    private static Vector3 FallbackDirection(string group)
    {
        switch (group)
        {
            case "hood": return new Vector3(0f, 1f, 0.35f);
            case "trunk": return new Vector3(0f, 0.55f, -1f);
            case "door_left": return Vector3.left;
            case "door_right": return Vector3.right;
            case "bumper_front": return Vector3.forward;
            case "bumper_rear": return Vector3.back;
            case "undercarriage": return Vector3.down;
            default: return Vector3.up;
        }
    }

    private void OnDestroy()
    {
        if (!Lab_MotionScheduler.TryGetCurrent(out Lab_MotionScheduler scheduler)) return;
        foreach (Transform part in parts)
        {
            if (part == null) continue;
            scheduler.Cancel(part, MotionChannel.LocalPosition);
            scheduler.Cancel(part, MotionChannel.Scale);
        }
    }

    public void OnBeforeSerialize() { }

    public void OnAfterDeserialize()
    {
        // Existing prefabs serialized a different easing enum. Preserve the
        // established artistic default without modifying prefab data in Phase 3.
        int serializedEase = (int)ease;
        if (serializedEase < (int)Lab_Ease.Linear || serializedEase > (int)Lab_Ease.OutSine)
            ease = Lab_Ease.InOutCubic;
    }
}
}
