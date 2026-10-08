namespace URPLabStudio
{
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;

[Overlay(typeof(SceneView), "URPLab PrimTools", true)]
public class Lab_PrimToolbarOverlay : ToolbarOverlay
{
    public Lab_PrimToolbarOverlay() : base(
        "URPLab/Lab_CreateGrounded",
        "URPLab/Lab_CreateSphere",
        "URPLab/Lab_CreateCapsule",
        "URPLab/Lab_CreateCylinder",
        "URPLab/Lab_CreatePlane",
        "URPLab/Lab_CreateEmpty",
        "URPLab/Lab_ResetTransform",
        "URPLab/Lab_CreateDirectional",
        "URPLab/Lab_CreatePoint",
        "URPLab/Lab_CreateSpot",
        "URPLab/Lab_CreateArea",
        "URPLab/URPSet"
    )
    {
    }
}
}
