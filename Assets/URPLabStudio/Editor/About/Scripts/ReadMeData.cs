using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "ReadMeData", menuName = "URPLab/ReadMe Data")]
public class ReadMeData : ScriptableObject
{
    [Serializable]
    public class SceneEntry
    {
        public string displayName;
        public SceneAsset scene;
    }

    [Header("Version")]
    public string requiredUnityVersion = "6000.4.0f1";

    [Header("Images")]
    public Texture2D headerImage;
    public Texture2D brandLogo;

    [Header("Brand")]
    public string windowTitle = "About";
    public string brandTitle = "URPLabStudio";
    public string projectSubtitle = "URP Production Toolkit / Migration Framework";
    public string versionLabel = "URPLabStudio URP Template / Version 1.0 / Unity 6 URP";

    [Header("Overview")]
    [TextArea(4, 10)]
    public string overviewText;

    [Header("Content - Main")]
    public string mainTitle = "URPLabStudio";

    [TextArea(4, 10)]
    public string description;

    [TextArea(3, 8)]
    public string environmentInfo = "Tested with Unity 6 and Universal Render Pipeline.";

    [Header("Features")]
    public string[] features;

    [Header("URPLab")]
    [TextArea(3, 8)]
    public string urpLabDescription;

    [Header("Scene Launcher")]
    public List<SceneEntry> sceneEntries = new List<SceneEntry>();
    public int primarySceneIndex = 0;
    public string mainSceneButtonLabel = "Open Main Scene";

    [Header("Asset Library")]
    public SceneAsset assetLibraryScene;
    public string assetLibraryButtonLabel = "Open Asset Library";

    [Header("Legacy Scene Reference")]
    public SceneAsset mainScene;

    [Header("Website")]
    public string websiteButtonLabel = "URPLabStudio Website";
    public string websiteUrl = "https://www.youtube.com/@yoon9718";

    [Header("Options")]
    public bool showOnStartByDefault = true;

    [Header("Markdown")]
    public TextAsset markdownFile;
}