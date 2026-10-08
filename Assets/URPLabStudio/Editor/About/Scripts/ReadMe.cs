using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReadMe : EditorWindow
{
    public static readonly string kShowOnStart = "URPLab.ReadMe.ShowOnStart";
    public static readonly int kShowOnStartCookie = 1;
    public static readonly string kShownThisSession = "URPLab.ReadMe.ShownThisSession";

    private static string _salt;

    private GUIStyle _textAreaStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _subtitleStyle;
    private GUIStyle _infoBoxStyle;
    private GUIStyle _versionStyle;
    private GUIStyle _buttonStyle;
    private GUIStyle _sectionTitleStyle;
    private GUIStyle _bulletStyle;

    private Vector2 _scroll;
    private string _text;
    private ReadMeData _data;

    private const string kDataFolder = "Assets/URPLab/Editor/About/Data";
    private const string kDataAssetPath = "Assets/URPLab/Editor/About/Data/ReadMeData.asset";
    private const string kDefaultHeaderPath = "Assets/URPLab/Editor/About/Icons/AboutHeader.png";
    private const string kDefaultLogoPath = "Assets/URPLab/Editor/About/Icons/URPLabStudioLogo.png";
    private const string kDefaultMarkdownPath = "Assets/URPLab/Editor/About/Data/ReadMe.md";

    private static string Salt
    {
        get
        {
            if (string.IsNullOrEmpty(_salt))
            {
                _salt = Application.dataPath.GetHashCode().ToString("X8");
            }

            return _salt;
        }
    }

    [MenuItem("Help/About URPLabStudio...", false, 1)]
    public static void ShowWindow()
    {
        ReadMe window = GetWindow<ReadMe>(true, "About");
        window.minSize = new Vector2(640f, 480f);
        window.maxSize = new Vector2(1280f, 960f);
        window.Show();

        SessionState.SetBool(kShownThisSession, true);
    }

    [InitializeOnLoadMethod]
    private static void InitializeProjectSettings()
    {
        EditorApplication.delayCall += () =>
        {
            CheckPrefsAndShow();
            EnsureLinearProject();
        };
    }

    private static void CheckPrefsAndShow()
    {
        ReadMeData data = LoadData();

        bool defaultShowOnStart = data == null || data.showOnStartByDefault;
        int defaultCookie = defaultShowOnStart ? 0 : kShowOnStartCookie;

        int cookie = EditorPrefs.GetInt(kShowOnStart + Salt, defaultCookie);
        bool showOnStart = cookie < kShowOnStartCookie;

        if (showOnStart && !SessionState.GetBool(kShownThisSession, false))
        {
            ShowWindow();
        }
    }

    private void OnEnable()
    {
        _data = LoadData();

        titleContent = new GUIContent(_data != null && !string.IsNullOrEmpty(_data.windowTitle) ? _data.windowTitle : "About");
        minSize = new Vector2(640f, 480f);
        maxSize = new Vector2(1280f, 960f);

        try
        {
            _text = ParseMarkdown(_data);
        }
        catch (Exception e)
        {
            _text = "Failed to load ReadMe.\n\n" + e.Message;
        }

        string requiredVersion = _data != null ? _data.requiredUnityVersion : string.Empty;

        if (!string.IsNullOrEmpty(requiredVersion) && CompareVersions(Application.unityVersion, requiredVersion) < 0)
        {
            _text =
                "\n<color=red><size=18><b>This project requires at least Unity version " +
                requiredVersion +
                ", current version is " +
                Application.unityVersion +
                "</b></size></color>\n\n" +
                _text;
        }
    }

    private void OnGUI()
    {
        InitStyles();

        if (_data == null)
        {
            DrawMissingDataUI();
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        GUILayout.Space(8f);

        DrawHeaderImage();
        DrawBrandLogo();
        DrawBrandSection();

        GUILayout.Space(14f);

        DrawBodyContent();

        EditorGUILayout.EndScrollView();

        DrawFooterControls();
    }

    private void DrawMissingDataUI()
    {
        EditorGUILayout.HelpBox("ReadMeData asset was not found.", MessageType.Warning);

        GUILayout.Space(6f);

        if (GUILayout.Button("Create ReadMeData Asset"))
        {
            CreateDefaultDataAsset();
        }
    }

    private void InitStyles()
    {
        if (_textAreaStyle == null)
        {
            _textAreaStyle = new GUIStyle(EditorStyles.textArea)
            {
                richText = true,
                wordWrap = true,
                fontSize = 13
            };
        }

        if (_titleStyle == null)
        {
            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                wordWrap = true
            };
        }

        if (_subtitleStyle == null)
        {
            _subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 12
            };
        }

        if (_infoBoxStyle == null)
        {
            _infoBoxStyle = new GUIStyle(EditorStyles.helpBox)
            {
                wordWrap = true,
                richText = true,
                fontSize = 13,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(12, 12, 6, 6)
            };

            _infoBoxStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
        }

        if (_versionStyle == null)
        {
            _versionStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                fontSize = 11
            };
        }

        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fixedHeight = 28f,
                fontSize = 12,
                alignment = TextAnchor.MiddleCenter
            };
        }

        if (_sectionTitleStyle == null)
        {
            _sectionTitleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                margin = new RectOffset(12, 12, 10, 6)
            };
        }

        if (_bulletStyle == null)
        {
            _bulletStyle = new GUIStyle(EditorStyles.label)
            {
                wordWrap = true,
                fontSize = 13,
                margin = new RectOffset(18, 12, 2, 2)
            };
        }
    }

    private void DrawHeaderImage()
    {
        if (_data.headerImage == null)
        {
            return;
        }

        float width = Mathf.Max(100f, position.width - 12f);
        float aspect = (float)_data.headerImage.width / _data.headerImage.height;
        float height = width / aspect;

        Rect rect = GUILayoutUtility.GetRect(width, height, GUILayout.ExpandWidth(true));
        GUI.DrawTexture(rect, _data.headerImage, ScaleMode.ScaleToFit);

        GUILayout.Space(16f);
    }

    private void DrawBrandLogo()
    {
        if (_data.brandLogo == null)
        {
            return;
        }

        float width = Mathf.Min(position.width * 0.35f, 260f);

        if (width <= 0f)
        {
            return;
        }

        float aspect = (float)_data.brandLogo.width / _data.brandLogo.height;
        float height = width / aspect;

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(_data.brandLogo, GUILayout.Width(width), GUILayout.Height(height));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.Space(10f);
    }

    private void DrawBrandSection()
    {
        if (!string.IsNullOrEmpty(_data.brandTitle))
        {
            GUILayout.Label(_data.brandTitle, _titleStyle);
        }

        if (!string.IsNullOrEmpty(_data.projectSubtitle))
        {
            GUILayout.Label(_data.projectSubtitle, _subtitleStyle);
        }

        if (!string.IsNullOrEmpty(_data.versionLabel))
        {
            GUILayout.Label(_data.versionLabel, _versionStyle);
        }

        GUILayout.Space(10f);

        if (!string.IsNullOrEmpty(_data.overviewText))
        {
            GUILayout.Label(_data.overviewText, _infoBoxStyle);
        }

        GUILayout.Space(4f);
    }

    private void DrawBodyContent()
    {
        bool hasCustomContent =
            !string.IsNullOrEmpty(_data.mainTitle) ||
            !string.IsNullOrEmpty(_data.description) ||
            !string.IsNullOrEmpty(_data.environmentInfo) ||
            (_data.features != null && _data.features.Length > 0) ||
            !string.IsNullOrEmpty(_data.urpLabDescription);

        if (hasCustomContent)
        {
            DrawCustomContent();
            return;
        }

        GUILayout.TextArea(_text, _textAreaStyle, GUILayout.ExpandHeight(true));
    }

    private void DrawCustomContent()
    {
        GUILayout.Space(6f);

        if (!string.IsNullOrEmpty(_data.mainTitle))
        {
            GUILayout.Label(_data.mainTitle, _sectionTitleStyle);
            GUILayout.Space(4f);
        }

        if (!string.IsNullOrEmpty(_data.description))
        {
            GUILayout.Label(_data.description, _infoBoxStyle);
            GUILayout.Space(8f);
        }

        if (!string.IsNullOrEmpty(_data.environmentInfo))
        {
            GUILayout.Label("Environment", _sectionTitleStyle);
            GUILayout.Label(_data.environmentInfo, _infoBoxStyle);
            GUILayout.Space(8f);
        }

        if (_data.features != null && _data.features.Length > 0)
        {
            bool hasFeatureText = false;

            for (int i = 0; i < _data.features.Length; i++)
            {
                if (!string.IsNullOrEmpty(_data.features[i]))
                {
                    hasFeatureText = true;
                    break;
                }
            }

            if (hasFeatureText)
            {
                GUILayout.Label("Features", _sectionTitleStyle);

                GUILayout.BeginVertical(_infoBoxStyle);

                for (int i = 0; i < _data.features.Length; i++)
                {
                    if (string.IsNullOrEmpty(_data.features[i]))
                    {
                        continue;
                    }

                    GUILayout.Label("• " + _data.features[i], _bulletStyle);
                }

                GUILayout.EndVertical();

                GUILayout.Space(8f);
            }
        }

        if (!string.IsNullOrEmpty(_data.urpLabDescription))
        {
            GUILayout.Label("URPLabStudio", _sectionTitleStyle);
            GUILayout.Label(_data.urpLabDescription, _infoBoxStyle);
            GUILayout.Space(8f);
        }
    }

    private void DrawFooterControls()
    {
        GUILayout.Space(4f);

        DrawActionButtons();

        GUILayout.Space(8f);

        bool defaultShowOnStart = _data.showOnStartByDefault;
        int defaultCookie = defaultShowOnStart ? 0 : kShowOnStartCookie;

        int cookie = EditorPrefs.GetInt(kShowOnStart + Salt, defaultCookie);
        bool showOnStart = cookie < kShowOnStartCookie;

        bool newValue = EditorGUILayout.ToggleLeft("Show On Start", showOnStart);

        if (newValue != showOnStart)
        {
            EditorPrefs.SetInt(kShowOnStart + Salt, newValue ? 0 : kShowOnStartCookie);
        }
    }

    private void DrawActionButtons()
    {
        bool hasMainSceneSet = HasSceneEntries() || _data.mainScene != null;

        GUILayout.BeginHorizontal();

        if (hasMainSceneSet)
        {
            string mainSceneLabel = !string.IsNullOrEmpty(_data.mainSceneButtonLabel)
                ? _data.mainSceneButtonLabel
                : "Open Main Scene";

            if (GUILayout.Button(mainSceneLabel, _buttonStyle))
            {
                QueueMainSceneSetOpen();
            }
        }

        if (_data.assetLibraryScene != null)
        {
            string assetLibraryLabel = !string.IsNullOrEmpty(_data.assetLibraryButtonLabel)
                ? _data.assetLibraryButtonLabel
                : "Open Asset Library";

            if (GUILayout.Button(assetLibraryLabel, _buttonStyle))
            {
                QueueSceneOpen(_data.assetLibraryScene, OpenSceneMode.Single);
            }
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(4f);

        string websiteLabel = !string.IsNullOrEmpty(_data.websiteButtonLabel)
            ? _data.websiteButtonLabel
            : "URPLabStudio Website";

        if (GUILayout.Button(websiteLabel, _buttonStyle))
        {
            OpenWebsite();
        }
    }

    // A scene change invalidates Unity's current IMGUI layout stack in 6.6.
    // Queue it after the button's horizontal group has closed.
    private void QueueMainSceneSetOpen()
    {
        EditorApplication.delayCall += OpenMainSceneSet;
    }

    private void QueueSceneOpen(SceneAsset sceneAsset, OpenSceneMode mode)
    {
        EditorApplication.delayCall += () => OpenScene(sceneAsset, mode);
    }

    private bool HasSceneEntries()
    {
        if (_data.sceneEntries == null || _data.sceneEntries.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < _data.sceneEntries.Count; i++)
        {
            ReadMeData.SceneEntry entry = _data.sceneEntries[i];

            if (entry != null && entry.scene != null)
            {
                return true;
            }
        }

        return false;
    }

    private void OpenMainSceneSet()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        if (!HasSceneEntries())
        {
            OpenSceneInternal(_data.mainScene, OpenSceneMode.Single);
            return;
        }

        int primaryIndex = Mathf.Clamp(_data.primarySceneIndex, 0, _data.sceneEntries.Count - 1);
        ReadMeData.SceneEntry primaryEntry = _data.sceneEntries[primaryIndex];

        if (primaryEntry == null || primaryEntry.scene == null)
        {
            primaryEntry = FindFirstValidSceneEntry();

            if (primaryEntry == null || primaryEntry.scene == null)
            {
                EditorUtility.DisplayDialog("Scene", "No valid main scene entries were found.", "OK");
                return;
            }
        }

        Scene openedPrimaryScene = OpenSceneInternal(primaryEntry.scene, OpenSceneMode.Single);

        if (openedPrimaryScene.IsValid())
        {
            EditorSceneManager.SetActiveScene(openedPrimaryScene);
        }

        for (int i = 0; i < _data.sceneEntries.Count; i++)
        {
            ReadMeData.SceneEntry entry = _data.sceneEntries[i];

            if (entry == null || entry.scene == null || entry.scene == primaryEntry.scene)
            {
                continue;
            }

            OpenSceneInternal(entry.scene, OpenSceneMode.Additive);
        }
    }

    private ReadMeData.SceneEntry FindFirstValidSceneEntry()
    {
        if (_data.sceneEntries == null)
        {
            return null;
        }

        for (int i = 0; i < _data.sceneEntries.Count; i++)
        {
            ReadMeData.SceneEntry entry = _data.sceneEntries[i];

            if (entry != null && entry.scene != null)
            {
                return entry;
            }
        }

        return null;
    }

    private void OpenScene(SceneAsset sceneAsset, OpenSceneMode mode)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        OpenSceneInternal(sceneAsset, mode);
    }

    private Scene OpenSceneInternal(SceneAsset sceneAsset, OpenSceneMode mode)
    {
        if (sceneAsset == null)
        {
            return default;
        }

        string path = AssetDatabase.GetAssetPath(sceneAsset);

        if (string.IsNullOrEmpty(path))
        {
            EditorUtility.DisplayDialog("Scene", "Scene path was not found.", "OK");
            return default;
        }

        return EditorSceneManager.OpenScene(path, mode);
    }

    private void OpenWebsite()
    {
        if (string.IsNullOrEmpty(_data.websiteUrl))
        {
            EditorUtility.DisplayDialog("Website", "Website URL is empty.", "OK");
            return;
        }

        Application.OpenURL(_data.websiteUrl);
    }

    private static ReadMeData LoadData()
    {
        string[] guids = AssetDatabase.FindAssets("t:ReadMeData");

        if (guids == null || guids.Length == 0)
        {
            return null;
        }

        string preferredPath = kDataAssetPath;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (path == preferredPath)
            {
                return AssetDatabase.LoadAssetAtPath<ReadMeData>(path);
            }
        }

        string fallbackPath = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<ReadMeData>(fallbackPath);
    }

    private static void CreateDefaultDataAsset()
    {
        EnsureFolder("Assets/URPLab");
        EnsureFolder("Assets/URPLab/Editor");
        EnsureFolder("Assets/URPLab/Editor/About");
        EnsureFolder(kDataFolder);

        ReadMeData data = CreateInstance<ReadMeData>();

        data.headerImage = AssetDatabase.LoadAssetAtPath<Texture2D>(kDefaultHeaderPath);
        data.brandLogo = AssetDatabase.LoadAssetAtPath<Texture2D>(kDefaultLogoPath);
        data.markdownFile = AssetDatabase.LoadAssetAtPath<TextAsset>(kDefaultMarkdownPath);

        AssetDatabase.CreateAsset(data, kDataAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorGUIUtility.PingObject(data);
        Selection.activeObject = data;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(assetPath)?.Replace("\\", "/");
        string folderName = Path.GetFileName(assetPath);

        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }

        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(folderName))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static int CompareVersions(string version1, string version2)
    {
        Regex regex = new Regex(@"^(?<major>\d+)(\.(?<minor>\d+))*((?<early>[a-z])(?<number>\d+))?");
        Match match1 = regex.Match(version1);
        Match match2 = regex.Match(version2);

        Func<string, int> compareInteger = group =>
        {
            CaptureCollection captures1 = match1.Groups[group].Captures;
            CaptureCollection captures2 = match2.Groups[group].Captures;
            int count1 = captures1.Count;
            int count2 = captures2.Count;

            for (int i = 0; i < Mathf.Min(count1, count2); ++i)
            {
                int value1 = int.Parse(captures1[i].Value);
                int value2 = int.Parse(captures2[i].Value);

                if (value1 != value2)
                {
                    return value1 - value2;
                }
            }

            for (int i = count2; i < count1; ++i)
            {
                int value1 = int.Parse(captures1[i].Value);

                if (value1 != 0)
                {
                    return value1;
                }
            }

            for (int i = count1; i < count2; ++i)
            {
                int value2 = int.Parse(captures2[i].Value);

                if (value2 != 0)
                {
                    return -value2;
                }
            }

            return 0;
        };

        Func<string, int> compareString = group =>
        {
            CaptureCollection captures1 = match1.Groups[group].Captures;
            CaptureCollection captures2 = match2.Groups[group].Captures;
            int count1 = captures1.Count;
            int count2 = captures2.Count;

            for (int i = 0; i < Mathf.Min(count1, count2); ++i)
            {
                string value1 = captures1[i].Value;
                string value2 = captures2[i].Value;

                int cmp = string.Compare(value1, value2, StringComparison.Ordinal);

                if (cmp != 0)
                {
                    return cmp;
                }
            }

            return count2 - count1;
        };

        int diff;

        if ((diff = compareInteger("major")) != 0) return diff;
        if ((diff = compareInteger("minor")) != 0) return diff;
        if ((diff = compareString("early")) != 0) return diff;
        if ((diff = compareInteger("number")) != 0) return diff;

        return 0;
    }

    private static string ParseMarkdown(ReadMeData data)
    {
        if (data == null || data.markdownFile == null)
        {
            return "ReadMe markdown file not found.";
        }

        Regex h1 = new Regex(@"^\=+\s*$");
        Regex h2 = new Regex(@"^\-+\s*$");
        Regex hr = new Regex(@"^\*+\s*$");
        Regex li = new Regex(@"^\-\s+(.*)$");

        Queue<string> lines = new Queue<string>(data.markdownFile.text.Replace("\r\n", "\n").Split('\n'));
        StringBuilder builder = new StringBuilder();

        bool list = true;

        Action endList = () =>
        {
            if (list)
            {
                builder.Append('\n');
            }

            list = false;
        };

        while (lines.Count > 0)
        {
            string line = lines.Dequeue();
            Match match;

            if (string.IsNullOrEmpty(line))
            {
                endList();
                continue;
            }

            if (hr.IsMatch(line))
            {
                endList();
                builder.Append('\n');

                for (int i = 0; i < 20; ++i)
                {
                    builder.Append("\u2e3b");
                }

                builder.Append("\n\n");
                continue;
            }

            if ((match = li.Match(line)).Success)
            {
                builder.AppendFormat(" \u2022 {0}\n", match.Groups[1].Captures[0].Value);
                list = true;
                continue;
            }

            if (lines.Count > 0)
            {
                if (h1.IsMatch(lines.Peek()))
                {
                    lines.Dequeue();
                    endList();
                    builder.AppendFormat("\n<size=24><b>{0}</b></size>\n\n", line);
                    continue;
                }

                if (h2.IsMatch(lines.Peek()))
                {
                    lines.Dequeue();
                    endList();
                    builder.AppendFormat("\n<size=18><b>{0}</b></size>\n\n", line);
                    continue;
                }
            }

            builder.AppendFormat("{0}\n\n", line);
        }

        return builder.ToString();
    }

    private static void EnsureLinearProject()
    {
        if (PlayerSettings.colorSpace != ColorSpace.Linear)
        {
            PlayerSettings.colorSpace = ColorSpace.Linear;
            AssetDatabase.SaveAssets();
        }
    }
}
