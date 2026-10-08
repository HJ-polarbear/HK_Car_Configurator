namespace URPLabStudio
{
#if UNITY_EDITOR && URPLAB_HAS_RECORDER
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using UnityEngine;

[InitializeOnLoad]
public static class Lab_RecordingToolbar
{
    private static bool isRecording;
    private static bool pendingStartRecording;
    private static RecorderController recorderController;

    private const string OutputFolder = "Recordings";
    private const string BaseFileName = "MyRecording_";

    static Lab_RecordingToolbar()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        SceneView.duringSceneGui += OnSceneGUI;
    }

    public static bool IsRecording => isRecording;
    public static bool IsWaitingForPlayMode => pendingStartRecording;

    private static void OnSceneGUI(SceneView sceneView)
    {
    }

    [MenuItem("Tools/Recorder/Start Recording %#r")]
    public static void StartRecording()
    {
        if (isRecording || pendingStartRecording)
            return;

        if (!EditorApplication.isPlaying)
        {
            pendingStartRecording = true;
            Debug.Log("Recorder waiting for Play Mode...");
            EditorApplication.isPlaying = true;
            return;
        }

        StartRecordingInternal();
    }

    [MenuItem("Tools/Recorder/Stop Recording %#e")]
    public static void StopRecording()
    {
        pendingStartRecording = false;

        try
        {
            if (recorderController != null && recorderController.IsRecording())
            {
                recorderController.StopRecording();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Recording stop failed: {ex}");
        }
        finally
        {
            isRecording = false;
            recorderController = null;
            Debug.Log("Recording stopped");
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && pendingStartRecording)
        {
            pendingStartRecording = false;
            StartRecordingInternal();
        }

        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            pendingStartRecording = false;
            isRecording = false;
            recorderController = null;
        }
    }

    private static void StartRecordingInternal()
    {
        try
        {
            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = 30.0f;
            controllerSettings.CapFrameRate = true;

            var movieRecorder = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movieRecorder.name = "Movie Recorder";
            movieRecorder.Enabled = true;

            string date = DateTime.Now.ToString("yyyyMMdd");
            int nextTake = GetNextTakeNumber(date);
            movieRecorder.OutputFile = $"{OutputFolder}/{BaseFileName}{date}_{nextTake:0000}";

            movieRecorder.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = 1920,
                OutputHeight = 1080
            };

            controllerSettings.AddRecorderSettings(movieRecorder);

            recorderController = new RecorderController(controllerSettings);
            recorderController.PrepareRecording();
            recorderController.StartRecording();

            isRecording = true;
            Debug.Log("Recording started");
        }
        catch (Exception ex)
        {
            isRecording = false;
            recorderController = null;
            Debug.LogError($"Recording start failed: {ex}");
        }
    }

    private static int GetNextTakeNumber(string date)
    {
        string projectRoot = Directory.GetCurrentDirectory();
        string folderAbs = Path.Combine(projectRoot, OutputFolder);

        if (!Directory.Exists(folderAbs))
            Directory.CreateDirectory(folderAbs);

        int max = 0;
        string prefix = $"{BaseFileName}{date}_";

        foreach (string file in Directory.EnumerateFiles(folderAbs, prefix + "*", SearchOption.TopDirectoryOnly))
        {
            string name = Path.GetFileNameWithoutExtension(file);
            if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                continue;

            string tail = name.Substring(prefix.Length);
            string digits = new string(tail.TakeWhile(char.IsDigit).ToArray());

            if (digits.Length == 0)
                continue;

            if (int.TryParse(digits, out int n))
                max = Mathf.Max(max, n);
        }

        return max + 1;
    }
}
#endif
}
