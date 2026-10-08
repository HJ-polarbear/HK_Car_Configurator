namespace URPLabStudio
{
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class Lab_IconLoader
{
    private static readonly Dictionary<string, Texture2D> s_Cache = new();
    private sealed class StaticState { public string IconFolder; }
    private static readonly StaticState State = new StaticState();

    public static Texture2D Load(string fileName)
    {
        if (string.IsNullOrEmpty(fileName))
            return null;

        if (s_Cache.TryGetValue(fileName, out var cached) && cached != null)
            return cached;

        EnsureIconFolder();
        if (string.IsNullOrEmpty(State.IconFolder))
            return null;

        string path = $"{State.IconFolder}/{fileName}";
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        s_Cache[fileName] = tex;
        return tex;
    }

    private static void EnsureIconFolder()
    {
        if (!string.IsNullOrEmpty(State.IconFolder))
            return;

        string[] guids = AssetDatabase.FindAssets("Lab_PrimToolbarOverlay t:Script");
        if (guids.Length == 0)
            return;

        string overlayPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        if (string.IsNullOrEmpty(overlayPath))
            return;

        string normalizedPath = overlayPath.Replace("\\", "/");

        int idx = normalizedPath.LastIndexOf("/Toolbar/", StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            idx = normalizedPath.LastIndexOf("/Toolbars/", StringComparison.OrdinalIgnoreCase);

        if (idx < 0)
            return;

        string baseEditorPath = normalizedPath.Substring(0, idx);
        State.IconFolder = $"{baseEditorPath}/Icons";
    }

    public static void ClearCache()
    {
        s_Cache.Clear();
        State.IconFolder = null;
    }
}
}
