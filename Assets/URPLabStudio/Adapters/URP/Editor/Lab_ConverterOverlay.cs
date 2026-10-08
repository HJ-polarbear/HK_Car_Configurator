namespace URPLabStudio
{
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "URP Converter", true)]
public class Lab_ConverterOverlay : Overlay
{
    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.style.paddingLeft = 6;
        root.style.paddingRight = 6;

        var convertButton = new Button(ConvertSelectedMaterialsToURP)
        {
            text = "Convert to URP",
            tooltip = "Convert selected materials directly to URP shaders"
        };

        convertButton.style.paddingLeft = 6;
        convertButton.style.paddingRight = 6;
        convertButton.style.marginRight = 6;
        convertButton.style.backgroundColor = new Color(0.2f, 0.6f, 1f);
        convertButton.style.color = Color.white;

        root.Add(convertButton);
        return root;
    }

    private static void ConvertSelectedMaterialsToURP()
    {
        Material[] selectedMaterials = GetSelectedMaterials();

        if (selectedMaterials.Length == 0)
        {
            Debug.LogWarning("No materials selected. Select one or more materials in the Project window.");
            return;
        }

        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpUnlit = Shader.Find("Universal Render Pipeline/Unlit");

        if (urpLit == null)
        {
            Debug.LogError("URP Lit shader not found. Make sure URP is installed correctly.");
            return;
        }

        int convertedCount = 0;
        int skippedCount = 0;

        foreach (Material material in selectedMaterials)
        {
            if (material == null)
            {
                skippedCount++;
                continue;
            }

            if (ConvertMaterial(material, urpLit, urpUnlit))
            {
                convertedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"URP conversion finished. Converted: {convertedCount}, Skipped: {skippedCount}");
    }

    private static Material[] GetSelectedMaterials()
    {
        List<Material> materials = new List<Material>();
        Object[] selection = Selection.GetFiltered(typeof(Object), SelectionMode.DeepAssets);

        foreach (Object obj in selection)
        {
            if (obj is Material mat && !materials.Contains(mat))
            {
                materials.Add(mat);
            }
        }

        return materials.ToArray();
    }

    private static bool ConvertMaterial(Material material, Shader urpLit, Shader urpUnlit)
    {
        string oldShaderName = material.shader != null ? material.shader.name : string.Empty;

        if (string.IsNullOrEmpty(oldShaderName))
            return false;

        if (oldShaderName == "Universal Render Pipeline/Lit" || oldShaderName == "Universal Render Pipeline/Unlit")
        {
            return false;
        }

        Undo.RecordObject(material, "Convert Material To URP");

        bool isUnlit = IsUnlitShader(oldShaderName);
        bool isTransparent = DetectTransparency(material, oldShaderName);
        bool alphaClipping = DetectAlphaClipping(material, oldShaderName);

        Texture baseMap = GetFirstTexture(material,
            "_BaseMap",
            "_MainTex",
            "_BaseTex");

        Color baseColor = GetFirstColor(material,
            "_BaseColor",
            "_Color");

        Texture normalMap = GetFirstTexture(material,
            "_BumpMap");

        float normalScale = GetFirstFloat(material,
            1f,
            "_BumpScale");

        Texture metallicGlossMap = GetFirstTexture(material,
            "_MetallicGlossMap");

        float metallic = GetFirstFloat(material,
            0f,
            "_Metallic");

        float smoothness = GetFirstFloat(material,
            0.5f,
            "_Smoothness",
            "_Glossiness");

        Texture occlusionMap = GetFirstTexture(material,
            "_OcclusionMap");

        float occlusionStrength = GetFirstFloat(material,
            1f,
            "_OcclusionStrength");

        Texture emissionMap = GetFirstTexture(material,
            "_EmissionMap");

        Texture specGlossMap = GetFirstTexture(material,
            "_SpecGlossMap");

        float cutoff = GetFirstFloat(material,
            0.5f,
            "_Cutoff");

        Shader targetShader = isUnlit && urpUnlit != null ? urpUnlit : urpLit;
        material.shader = targetShader;

        SetTextureIfExists(material, "_BaseColorMap", baseMap);
        SetColorIfExists(material, "_BaseColor", baseColor);

        if (targetShader == urpLit)
        {
            SetTextureIfExists(material, "_BumpMap", normalMap);
            SetFloatIfExists(material, "_BumpScale", normalScale);

            SetTextureIfExists(material, "_MetallicGlossMap", metallicGlossMap);
            SetFloatIfExists(material, "_Metallic", metallic);
            SetFloatIfExists(material, "_Smoothness", smoothness);

            SetTextureIfExists(material, "_OcclusionMap", occlusionMap);
            SetFloatIfExists(material, "_OcclusionStrength", occlusionStrength);

            SetTextureIfExists(material, "_EmissionMap", emissionMap);
            SetColorIfExists(material, "_EmissionColor", Color.black);

            if (specGlossMap != null && metallicGlossMap == null)
            {
                Debug.LogWarning($"Specular workflow texture found on '{material.name}'. Manual check recommended.");
            }

            SetupSurfaceType(material, isTransparent, alphaClipping, cutoff);
            SetupBlackEmission(material);
        }

        EditorUtility.SetDirty(material);
        return true;
    }

    private static bool IsUnlitShader(string shaderName)
    {
        string lower = shaderName.ToLowerInvariant();
        return lower.Contains("unlit");
    }

    private static bool DetectTransparency(Material material, string shaderName)
    {
        if (material.HasProperty("_Surface"))
        {
            return Mathf.RoundToInt(material.GetFloat("_Surface")) == 1;
        }

        if (material.HasProperty("_Mode"))
        {
            float mode = material.GetFloat("_Mode");
            if (Mathf.Approximately(mode, 2f) || Mathf.Approximately(mode, 3f))
                return true;
        }

        if (material.HasProperty("_SrcBlend") && material.HasProperty("_DstBlend"))
        {
            int src = material.GetInt("_SrcBlend");
            int dst = material.GetInt("_DstBlend");

            if (src != (int)UnityEngine.Rendering.BlendMode.One ||
                dst != (int)UnityEngine.Rendering.BlendMode.Zero)
            {
                return true;
            }
        }

        string lower = shaderName.ToLowerInvariant();
        return lower.Contains("transparent") || lower.Contains("fade");
    }

    private static bool DetectAlphaClipping(Material material, string shaderName)
    {
        if (material.HasProperty("_AlphaClip"))
        {
            return material.GetFloat("_AlphaClip") > 0.5f;
        }

        if (material.HasProperty("_Cutoff"))
        {
            return material.GetFloat("_Cutoff") > 0f;
        }

        return material.IsKeywordEnabled("_ALPHATEST_ON") ||
               shaderName.ToLowerInvariant().Contains("cutout");
    }

    private static void SetupSurfaceType(Material material, bool isTransparent, bool alphaClipping, float cutoff)
    {
        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", isTransparent ? 1f : 0f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_AlphaClip"))
        {
            material.SetFloat("_AlphaClip", alphaClipping ? 1f : 0f);
        }

        if (material.HasProperty("_Cutoff"))
        {
            material.SetFloat("_Cutoff", cutoff);
        }

        if (isTransparent)
        {
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_SURFACE_TYPE_OPAQUE");
        }
        else
        {
            material.renderQueue = alphaClipping
                ? (int)UnityEngine.Rendering.RenderQueue.AlphaTest
                : (int)UnityEngine.Rendering.RenderQueue.Geometry;

            material.SetOverrideTag("RenderType", alphaClipping ? "TransparentCutout" : "Opaque");
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.EnableKeyword("_SURFACE_TYPE_OPAQUE");
        }

        if (alphaClipping)
            material.EnableKeyword("_ALPHATEST_ON");
        else
            material.DisableKeyword("_ALPHATEST_ON");
    }

    private static void SetupBlackEmission(Material material)
    {
        // Keep the URP Lit inspector's Emission checkbox enabled while forcing
        // zero emitted light. URP rebuilds _EMISSION from the GI flags.
        SetColorIfExists(material, "_EmissionColor", Color.black);
        material.globalIlluminationFlags =
            MaterialGlobalIlluminationFlags.BakedEmissive;
        material.EnableKeyword("_EMISSION");
    }

    private static Texture GetFirstTexture(Material material, params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
            {
                Texture value = material.GetTexture(name);
                if (value != null)
                    return value;
            }
        }

        return null;
    }

    private static Color GetFirstColor(Material material, params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetColor(name);
            }
        }

        return Color.white;
    }

    private static float GetFirstFloat(Material material, float defaultValue, params string[] names)
    {
        foreach (string name in names)
        {
            if (material.HasProperty(name))
            {
                return material.GetFloat(name);
            }
        }

        return defaultValue;
    }

    private static void SetTextureIfExists(Material material, string propertyName, Texture value)
    {
        if (value != null && material.HasProperty(propertyName))
        {
            material.SetTexture(propertyName, value);
        }
    }

    private static void SetColorIfExists(Material material, string propertyName, Color value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetColor(propertyName, value);
        }
    }

    private static void SetFloatIfExists(Material material, string propertyName, float value)
    {
        if (material.HasProperty(propertyName))
        {
            material.SetFloat(propertyName, value);
        }
    }
}
#endif
}
