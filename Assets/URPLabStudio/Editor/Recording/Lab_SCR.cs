using URPLabStudio;
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;

namespace URPLab.Screenshot.Editor
{
    public enum Lab_SCRTarget
    {
        GameView,
        SceneView
    }

    public sealed class Lab_SCR : EditorWindow
    {
        [SerializeField] Lab_SCRTarget captureTarget = Lab_SCRTarget.GameView;
        [SerializeField] string fileName = "URPLab_Screenshot";
        [SerializeField] string outputDirectory;
        [SerializeField, Range(1, 4)] int resolutionScale = 1;
        [SerializeField] bool openFolderAfterCapture;
        string lastOutput;

        [MenuItem("Tools/URPLab Studio/Screenshot/Lab SCR PNG")]
        public static void OpenWindow()
        {
            var window = GetWindow<Lab_SCR>("Lab SCR - PNG");
            window.titleContent = new GUIContent("Lab SCR - PNG");
            window.minSize = new Vector2(480f, 300f);
            window.Show();
            window.Focus();
        }

        void OnEnable()
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = DefaultOutputDirectory();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Lab Screenshot - PNG", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Capture Game View or Scene View as a lossless PNG image.", EditorStyles.miniLabel);
            EditorGUILayout.Space(10f);

            captureTarget = (Lab_SCRTarget)EditorGUILayout.EnumPopup("Capture Target", captureTarget);
            fileName = EditorGUILayout.TextField("File Name", fileName);
            resolutionScale = EditorGUILayout.IntSlider(
                new GUIContent("Resolution Scale", "1x to 4x. Game View uses Unity supersampling; Scene View renders at the scaled camera size."),
                resolutionScale, 1, 4);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Output Folder", GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                outputDirectory = EditorGUILayout.TextField(outputDirectory);
                if (GUILayout.Button("...", GUILayout.Width(34f)))
                {
                    var selected = EditorUtility.OpenFolderPanel(
                        "Select PNG Output Folder",
                        Directory.Exists(outputDirectory) ? outputDirectory : DefaultOutputDirectory(),
                        string.Empty);
                    if (!string.IsNullOrEmpty(selected))
                        outputDirectory = selected;
                    GUI.FocusControl(null);
                }
            }

            openFolderAfterCapture = EditorGUILayout.Toggle("Open Folder After Capture", openFolderAfterCapture);

            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Format", "PNG (lossless)");
                EditorGUILayout.LabelField("Target", captureTarget.ToString());
                EditorGUILayout.LabelField("Output", outputDirectory, EditorStyles.wordWrappedMiniLabel);
            }

            EditorGUILayout.Space(10f);
            var valid = !string.IsNullOrWhiteSpace(fileName) && !string.IsNullOrWhiteSpace(outputDirectory);
            using (new EditorGUI.DisabledScope(!valid))
            {
                GUI.backgroundColor = new Color(.25f, .65f, .95f);
                if (GUILayout.Button("Capture " + captureTarget + " PNG", GUILayout.Height(42f)))
                    Capture();
                GUI.backgroundColor = Color.white;
            }

            if (!string.IsNullOrEmpty(lastOutput))
            {
                EditorGUILayout.Space(8f);
                EditorGUILayout.HelpBox("Saved PNG: " + lastOutput, MessageType.Info);
            }
        }

        void Capture()
        {
            var folder = ResolveOutputDirectory(outputDirectory);
            Directory.CreateDirectory(folder);
            var safeName = MakeSafeFileName(fileName.Trim());
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var path = Path.Combine(folder, safeName + "_" + timestamp + ".png");

            try
            {
                if (captureTarget == Lab_SCRTarget.GameView)
                    CaptureGameView(path);
                else
                    CaptureSceneView(path);

                lastOutput = path;
                Debug.Log("[Lab SCR] PNG saved: " + path);
                ShowNotification(new GUIContent("PNG capture complete"));
                if (openFolderAfterCapture)
                    EditorUtility.RevealInFinder(path);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Lab SCR] PNG capture failed: " + exception);
                EditorUtility.DisplayDialog("Lab SCR", "PNG capture failed.\\n\\n" + exception.Message, "OK");
            }
        }

        void CaptureGameView(string path)
        {
            if (EditorApplication.isPlaying)
            {
                ScreenCapture.CaptureScreenshot(path, resolutionScale);
                return;
            }

            var camera = Camera.main ?? FindObjectsByType<Camera>(FindObjectsInactive.Exclude)
                .FirstOrDefault(item => item.enabled);
            if (camera == null)
                throw new InvalidOperationException("No active Game camera was found. Enter Play Mode or enable a Camera.");

            var width = Mathf.Max(1, camera.pixelWidth) * resolutionScale;
            var height = Mathf.Max(1, camera.pixelHeight) * resolutionScale;
            CaptureCameraToPng(camera, width, height, path);
        }

        void CaptureSceneView(string path)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
                throw new InvalidOperationException("No active Scene View was found. Click the Scene View once and try again.");

            var width = Mathf.Max(1, Mathf.RoundToInt(sceneView.position.width)) * resolutionScale;
            var height = Mathf.Max(1, Mathf.RoundToInt(sceneView.position.height)) * resolutionScale;
            CaptureCameraToPng(sceneView.camera, width, height, path);
        }

        static void CaptureCameraToPng(Camera camera, int width, int height, string path)
        {
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            var renderTexture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);

            try
            {
                camera.targetTexture = renderTexture;
                camera.Render();
                RenderTexture.active = renderTexture;
                texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0, false);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                DestroyImmediate(texture);
                RenderTexture.ReleaseTemporary(renderTexture);
            }
        }

        static string DefaultOutputDirectory()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Screenshots", "URPLab");
        }

        static string ResolveOutputDirectory(string requested)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var expanded = Environment.ExpandEnvironmentVariables(requested.Trim().Trim('"'));
            return Path.GetFullPath(Path.IsPathRooted(expanded) ? expanded : Path.Combine(projectRoot, expanded));
        }

        static string MakeSafeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return string.IsNullOrWhiteSpace(value) ? "URPLab_Screenshot" : value;
        }
    }

    [Overlay(typeof(SceneView), "Lab Screenshot PNG", true,
        defaultDockZone = DockZone.BottomToolbar,
        defaultDockPosition = DockPosition.Bottom,
        defaultDockIndex = 1)]
    public sealed class Lab_SCROverlay : ToolbarOverlay
    {
        public Lab_SCROverlay() : base(Lab_SCRButton.Id) { }
    }

    [EditorToolbarElement(Id)]
    public sealed class Lab_SCRButton : EditorToolbarButton
    {
        public const string Id = "URPLab/LabSCRButton";

        public Lab_SCRButton()
        {
            text = "SCR";
            tooltip = "Open Lab Game View / Scene View PNG Screenshot";
            style.color = new Color(.3f, .8f, 1f);
            style.backgroundColor = new Color(.18f, .18f, .18f);
            style.unityFontStyleAndWeight = FontStyle.Bold;
            style.minWidth = 42f;
            clicked += Lab_SCR.OpenWindow;
        }
    }
}
