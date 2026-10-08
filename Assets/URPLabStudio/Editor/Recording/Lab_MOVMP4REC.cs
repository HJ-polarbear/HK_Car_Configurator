using URPLabStudio;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEditor.SceneManagement;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace URPLab.Recording.Editor
{
    public enum LabRecordingMode
    {
        TimelineCinematic,
        GameplayManual
    }

    public sealed class Lab_MOVMP4REC : EditorWindow
    {
        const string DefaultScenePath = "Assets/URPLab/Scenes/URPLabController/Scenes/URPLabEnv.unity";

        [SerializeField] SceneAsset sceneToRecord;
        [SerializeField] string fileNamePrefix = "URPLab_Cinematic";
        [SerializeField] int outputWidth = 1920;
        [SerializeField] int outputHeight = 800;
        [SerializeField] string outputDirectory;
        [SerializeField] LabRecordingMode recordingMode = LabRecordingMode.TimelineCinematic;
        [SerializeField] bool stopTimelineDirectorsInGameplay = true;
        [SerializeField] bool createHighQualityMp4 = true;
        [SerializeField, Range(12, 23)] int mp4Crf = 16;
        [SerializeField] bool keepMovAfterConversion = true;
        [SerializeField] string ffmpegPath;

        [MenuItem("Tools/URPLab Studio/Recorder/Lab MOV + MP4")]
        internal static void OpenWindow()
        {
            var window = GetWindow<Lab_MOVMP4REC>("URP MOV / MP4 Recorder");
            window.titleContent = new GUIContent("URP MOV / MP4 Recorder");
            window.minSize = new Vector2(560f, 570f);
            window.Show();
            window.Focus();
        }

        [MenuItem("Tools/URPLab Studio/Recorder/Stop Active Recording _F9")]
        static void StopActiveRecording()
        {
            LabMovMp4RecorderService.StopRecording();
        }

        [MenuItem("Tools/URPLab Studio/Recorder/Stop Active Recording _F9", true)]
        static bool ValidateStopActiveRecording()
        {
            return LabMovMp4RecorderService.CanStopRecording;
        }

        void OnEnable()
        {
            if (sceneToRecord == null)
            {
                var activePath = SceneManager.GetActiveScene().path;
                var candidate = string.IsNullOrEmpty(activePath) ? DefaultScenePath : activePath;
                sceneToRecord = AssetDatabase.LoadAssetAtPath<SceneAsset>(candidate);
            }
            if (string.IsNullOrWhiteSpace(outputDirectory))
                outputDirectory = GetDefaultOutputDirectory();
            if (string.IsNullOrWhiteSpace(ffmpegPath))
                ffmpegPath = LabMovMp4RecorderService.FindFfmpegExecutable();
            EditorApplication.update -= RepaintWhileBusy;
            EditorApplication.update += RepaintWhileBusy;
        }

        void OnDisable()
        {
            EditorApplication.update -= RepaintWhileBusy;
        }

        void RepaintWhileBusy()
        {
            if (LabMovMp4RecorderService.IsBusy)
                Repaint();
        }

        void OnGUI()
        {
            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("URP MOV / MP4 Recorder", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("ProRes 422 HQ MOV master -> optional high-quality H.264 MP4", EditorStyles.miniLabel);
            EditorGUILayout.Space(8f);

            using (new EditorGUI.DisabledScope(LabMovMp4RecorderService.IsBusy))
            {
                var previousRecordingMode = recordingMode;
                recordingMode = (LabRecordingMode)EditorGUILayout.EnumPopup(
                    new GUIContent("Recording Mode",
                        "Timeline Cinematic records the configured 50-second range. Gameplay Manual records until you click Stop or press F9."),
                    recordingMode);
                if (recordingMode != previousRecordingMode &&
                    recordingMode == LabRecordingMode.GameplayManual)
                {
                    createHighQualityMp4 = true;
                    mp4Crf = 12;
                    keepMovAfterConversion = false;
                }
                sceneToRecord = (SceneAsset)EditorGUILayout.ObjectField(
                    new GUIContent("Recording Scene", "Assign a .unity SceneAsset from the Project window."),
                    sceneToRecord, typeof(SceneAsset), false);
                fileNamePrefix = EditorGUILayout.TextField("File Name", fileNamePrefix);

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        new GUIContent("Resolution", "Enter the exact output width and height in pixels."),
                        GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                    outputWidth = Mathf.Max(1, EditorGUILayout.IntField(outputWidth, GUILayout.MinWidth(90f)));
                    EditorGUILayout.LabelField("x", GUILayout.Width(14f));
                    outputHeight = Mathf.Max(1, EditorGUILayout.IntField(outputHeight, GUILayout.MinWidth(90f)));
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(
                        new GUIContent("Output Folder", "Enter an absolute path, or choose a folder with the browse button."),
                        GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                    outputDirectory = EditorGUILayout.TextField(outputDirectory);
                    if (GUILayout.Button("...", GUILayout.Width(34f)))
                    {
                        var selectedDirectory = EditorUtility.OpenFolderPanel(
                            "Select Recording Output Folder",
                            Directory.Exists(outputDirectory) ? outputDirectory : GetDefaultOutputDirectory(),
                            string.Empty);
                        if (!string.IsNullOrEmpty(selectedDirectory))
                            outputDirectory = selectedDirectory;
                        GUI.FocusControl(null);
                    }
                }

                if (recordingMode == LabRecordingMode.GameplayManual)
                {
                    stopTimelineDirectorsInGameplay = EditorGUILayout.Toggle(
                        new GUIContent("Stop Scene Timelines",
                            "Stops PlayableDirector playback when Gameplay recording begins, so FPS control is not overridden."),
                        stopTimelineDirectorsInGameplay);
                }

                EditorGUILayout.Space(5f);
                createHighQualityMp4 = EditorGUILayout.ToggleLeft(
                    new GUIContent("Create high-quality MP4 after MOV",
                        "Record a ProRes 422 HQ master first, then convert it to H.264 MP4 with FFmpeg."),
                    createHighQualityMp4);

                if (createHighQualityMp4)
                {
                    EditorGUI.indentLevel++;
                    mp4Crf = EditorGUILayout.IntSlider(
                        new GUIContent("MP4 Quality (CRF)",
                            "Lower is higher quality. CRF 12 is visually near-lossless; CRF 16 is high quality with a smaller file."),
                        mp4Crf, 12, 23);
                    keepMovAfterConversion = EditorGUILayout.Toggle(
                        new GUIContent("Keep MOV Master", "Keep the original ProRes MOV after MP4 conversion succeeds."),
                        keepMovAfterConversion);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            new GUIContent("FFmpeg", "Path to ffmpeg.exe. The tool searches common locations automatically."),
                            GUILayout.Width(EditorGUIUtility.labelWidth - 4f));
                        ffmpegPath = EditorGUILayout.TextField(ffmpegPath);
                        if (GUILayout.Button("...", GUILayout.Width(34f)))
                        {
                            var selectedFfmpeg = EditorUtility.OpenFilePanel(
                                "Select FFmpeg Executable",
                                File.Exists(ffmpegPath) ? Path.GetDirectoryName(ffmpegPath) : string.Empty,
                                "exe");
                            if (!string.IsNullOrEmpty(selectedFfmpeg))
                                ffmpegPath = selectedFfmpeg;
                            GUI.FocusControl(null);
                        }
                    }

                    if (GUILayout.Button("Auto Detect FFmpeg", GUILayout.Width(160f)))
                        ffmpegPath = LabMovMp4RecorderService.FindFfmpegExecutable();
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space(10f);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                DrawSetting("Mode", recordingMode == LabRecordingMode.TimelineCinematic
                    ? "Timeline Cinematic - automatic 50-second capture"
                    : "Gameplay / FPS - manual stop (button or F9)");
                DrawSetting("Format", createHighQualityMp4
                    ? "MOV ProRes 422 HQ -> MP4 H.264 (CRF " + mp4Crf + ", slow)"
                    : "MOV - Apple ProRes 422 HQ");
                DrawSetting("Resolution", outputWidth + " x " + outputHeight + " (Custom)");
                DrawSetting("Duration", recordingMode == LabRecordingMode.TimelineCinematic
                    ? "00:50 (minutes : seconds)"
                    : "Manual stop - timer shown as minutes : seconds");
                DrawSetting("Audio", recordingMode == LabRecordingMode.TimelineCinematic
                    ? "Include Timeline music"
                    : "Include Game audio");
                if (createHighQualityMp4)
                    DrawSetting("Files", keepMovAfterConversion
                        ? "Keep both MOV master and MP4"
                        : "MP4 only - delete temporary MOV after successful conversion");
                DrawSetting("Output", outputDirectory);
            }

            EditorGUILayout.Space(10f);
            var scenePath = sceneToRecord != null ? AssetDatabase.GetAssetPath(sceneToRecord) : string.Empty;
            var validScene = !string.IsNullOrEmpty(scenePath) &&
                             scenePath.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
            var validResolution = outputWidth > 0 && outputHeight > 0;
            var validOutput = !string.IsNullOrWhiteSpace(outputDirectory);
            var validFfmpeg = !createHighQualityMp4 || File.Exists(ffmpegPath);
            var valid = validScene && validResolution && validOutput && validFfmpeg;

            if (LabMovMp4RecorderService.IsBusy)
            {
                EditorGUILayout.HelpBox(LabMovMp4RecorderService.Status, MessageType.Info);
                if (LabMovMp4RecorderService.IsActivelyRecording)
                    DrawRecordingTimer();
                if (LabMovMp4RecorderService.CanStopRecording)
                {
                    GUI.backgroundColor = new Color(1f, .45f, .35f);
                    if (GUILayout.Button("Stop Recording", GUILayout.Height(38f)))
                        LabMovMp4RecorderService.StopRecording();
                    GUI.backgroundColor = Color.white;
                }
            }
            else
            {
                if (!validScene)
                    EditorGUILayout.HelpBox("Assign a SceneAsset to the recording slot.", MessageType.Warning);
                else if (!validResolution)
                    EditorGUILayout.HelpBox("Resolution width and height must be greater than zero.", MessageType.Warning);
                else if (!validOutput)
                    EditorGUILayout.HelpBox("Enter or select an output folder.", MessageType.Warning);
                else if (!validFfmpeg)
                    EditorGUILayout.HelpBox(
                        "FFmpeg was not found. Click Auto Detect FFmpeg or select ffmpeg.exe.",
                        MessageType.Warning);
                else
                    EditorGUILayout.HelpBox(recordingMode == LabRecordingMode.TimelineCinematic
                        ? "Timeline mode opens the scene, plays its longest Timeline, and stops automatically after 50 seconds."
                            : (createHighQualityMp4 && !keepMovAfterConversion
                                ? "Gameplay mode records while you control the FPS player. Stop with F9; after MP4 succeeds, the temporary MOV is removed."
                                : "Gameplay mode opens the scene and records the Game View while you control the FPS player. Click Stop Recording or press F9 when finished."),
                        MessageType.None);

                using (new EditorGUI.DisabledScope(!valid))
                {
                    var previousBackgroundColor = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(.32f, .34f, .37f);
                    var recordButtonLabel = recordingMode == LabRecordingMode.TimelineCinematic
                        ? (createHighQualityMp4
                            ? "Record Timeline MOV + Create High-Quality MP4"
                            : "Record Timeline to MOV")
                        : (createHighQualityMp4
                            ? "Start Gameplay Recording + Create MP4"
                            : "Start Gameplay Recording to MOV");
                    if (GUILayout.Button(recordButtonLabel, GUILayout.Height(44f)))
                        LabMovMp4RecorderService.RequestRecording(
                            scenePath, fileNamePrefix, outputWidth, outputHeight, outputDirectory,
                            recordingMode, stopTimelineDirectorsInGameplay,
                            createHighQualityMp4, ffmpegPath, mp4Crf, keepMovAfterConversion);
                    GUI.backgroundColor = previousBackgroundColor;
                }
            }

            EditorGUILayout.Space(7f);
            if (!string.IsNullOrEmpty(LabMovMp4RecorderService.LastOutput))
                EditorGUILayout.SelectableLabel("Latest Output: " + LabMovMp4RecorderService.LastOutput,
                    EditorStyles.wordWrappedMiniLabel, GUILayout.Height(34f));
        }

        static void DrawSetting(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(72f));
                EditorGUILayout.LabelField(value, EditorStyles.miniLabel);
            }
        }

        static void DrawRecordingTimer()
        {
            var elapsed = LabMovMp4RecorderService.FormatMinutesSeconds(
                LabMovMp4RecorderService.RecordedSeconds);
            var timerText = "\u25CF  REC  " + elapsed;
            if (LabMovMp4RecorderService.ActiveRecordingMode == LabRecordingMode.TimelineCinematic)
                timerText += "  /  00:50";

            var timerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 22,
                fixedHeight = 40f
            };
            timerStyle.normal.textColor = new Color(1f, .12f, .12f);

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                EditorGUILayout.LabelField(timerText, timerStyle, GUILayout.Height(40f));
        }

        static string GetDefaultOutputDirectory()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, "Recordings", "URPLab");
        }
    }

    [Overlay(typeof(SceneView), "URP MOV / MP4 Recorder", true,
        defaultDockZone = DockZone.BottomToolbar,
        defaultDockPosition = DockPosition.Bottom,
        defaultDockIndex = 0)]
    public sealed class LabMovRecorderOverlay : ToolbarOverlay
    {
        public LabMovRecorderOverlay() : base(LabMovRecorderButton.Id)
        {
        }
    }

    [EditorToolbarElement(Id)]
    public sealed class LabMovRecorderButton : EditorToolbarButton
    {
        public const string Id = "URPLab/LabMovRecorderButton";

        public LabMovRecorderButton()
        {
            text = "RC";
            tooltip = "Open URP MOV / MP4 Recorder";
            style.color = new Color(1f, .2f, .2f);
            style.backgroundColor = new Color(.18f, .18f, .18f);
            style.unityFontStyleAndWeight = FontStyle.Bold;
            style.minWidth = 34f;
            clicked += Lab_MOVMP4REC.OpenWindow;
        }
    }

    [InitializeOnLoad]
    public static class LabMovMp4RecorderService
    {
        const string RequestKey = "URPLab.URPLabRecorder.Requested";
        const string SceneKey = "URPLab.URPLabRecorder.Scene";
        const string OutputKey = "URPLab.URPLabRecorder.Output";
        const string StatusKey = "URPLab.URPLabRecorder.Status";
        const string WidthKey = "URPLab.URPLabRecorder.Width";
        const string HeightKey = "URPLab.URPLabRecorder.Height";
        const string RecordingModeKey = "URPLab.URPLabRecorder.RecordingMode";
        const string StopTimelinesKey = "URPLab.URPLabRecorder.StopTimelines";
        const string ConvertMp4Key = "URPLab.URPLabRecorder.ConvertMp4";
        const string FfmpegPathKey = "URPLab.URPLabRecorder.FfmpegPath";
        const string Mp4CrfKey = "URPLab.URPLabRecorder.Mp4Crf";
        const string KeepMovKey = "URPLab.URPLabRecorder.KeepMov";
        const int Fps = 30;
        const int LastFrame = 1499;

        sealed class StaticState
        {
            public RecorderController Controller;
            public PlayableDirector Director;
            public bool Started;
            public double RecordingStartUnscaledTime;
            public bool ConversionPending;
            public double ConversionReadyTime;
            public Process ConversionProcess;
            public string ConversionMovPath;
            public string ConversionMp4Path;
            public bool KeepMovAfterConversion;
        }
        static readonly StaticState State = new StaticState();

        public static bool IsBusy => SessionState.GetBool(RequestKey, false) || State.Started ||
                                     State.ConversionPending || State.ConversionProcess != null;
        public static bool CanStopRecording => SessionState.GetBool(RequestKey, false) || State.Started;
        public static bool IsActivelyRecording => State.Started && State.Controller != null && State.Controller.IsRecording();
        public static double RecordedSeconds => IsActivelyRecording
            ? Math.Max(0d, Time.unscaledTimeAsDouble - State.RecordingStartUnscaledTime)
            : 0d;
        public static LabRecordingMode ActiveRecordingMode =>
            (LabRecordingMode)SessionState.GetInt(
                RecordingModeKey, (int)LabRecordingMode.TimelineCinematic);
        public static string Status => SessionState.GetString(StatusKey, "Ready");
        public static string LastOutput => SessionState.GetString(OutputKey, string.Empty);

        static LabMovMp4RecorderService()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorApplication.update -= MonitorRecording;
            EditorApplication.update += MonitorRecording;
        }

        public static void RequestRecording(
            string scenePath,
            string fileNamePrefix,
            int outputWidth,
            int outputHeight,
            string requestedOutputDirectory,
            LabRecordingMode recordingMode,
            bool stopTimelineDirectors,
            bool createHighQualityMp4,
            string requestedFfmpegPath,
            int mp4Crf,
            bool keepMovMaster)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || IsBusy)
            {
                Debug.LogWarning("[Lab MOV/MP4 Recorder] Play Mode or recording preparation is already active.");
                return;
            }
            if (string.IsNullOrEmpty(scenePath) || AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                Debug.LogError("[Lab MOV/MP4 Recorder] Select a valid scene to record.");
                return;
            }
            if (outputWidth <= 0 || outputHeight <= 0)
            {
                Debug.LogError("[Lab MOV/MP4 Recorder] Output width and height must be greater than zero.");
                SessionState.SetString(StatusKey, "Invalid output resolution.");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                SessionState.SetString(StatusKey, "Scene switch cancelled by user.");
                return;
            }

            if (!string.Equals(SceneManager.GetActiveScene().path, scenePath, StringComparison.OrdinalIgnoreCase))
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            if (recordingMode == LabRecordingMode.TimelineCinematic && FindTimelineDirector() == null)
            {
                Debug.LogError("[Lab MOV/MP4 Recorder] No PlayableDirector with a Timeline was found in the selected scene.");
                SessionState.SetString(StatusKey, "No PlayableDirector found.");
                return;
            }

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            string outputDirectory;
            try
            {
                outputDirectory = ResolveOutputDirectory(requestedOutputDirectory);
                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception exception)
            {
                SessionState.SetString(StatusKey, "Invalid or inaccessible output folder.");
                Debug.LogError("[Lab MOV/MP4 Recorder] Could not use output folder: " + exception.Message);
                return;
            }
            var safePrefix = MakeSafeFileName(string.IsNullOrWhiteSpace(fileNamePrefix)
                ? Path.GetFileNameWithoutExtension(scenePath)
                : fileNamePrefix.Trim());
            var outputWithoutExtension = Path.Combine(outputDirectory,
                safePrefix + "_" + outputWidth + "x" + outputHeight +
                (recordingMode == LabRecordingMode.TimelineCinematic ? "_Timeline" : "_Gameplay") +
                "_ProRes422HQ_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));

            SessionState.SetString(SceneKey, scenePath);
            SessionState.SetString(OutputKey, outputWithoutExtension + ".mov");
            SessionState.SetInt(WidthKey, outputWidth);
            SessionState.SetInt(HeightKey, outputHeight);
            SessionState.SetInt(RecordingModeKey, (int)recordingMode);
            SessionState.SetBool(StopTimelinesKey, stopTimelineDirectors);
            SessionState.SetBool(ConvertMp4Key, createHighQualityMp4);
            SessionState.SetString(FfmpegPathKey, requestedFfmpegPath ?? string.Empty);
            SessionState.SetInt(Mp4CrfKey, Mathf.Clamp(mp4Crf, 12, 23));
            SessionState.SetBool(KeepMovKey, keepMovMaster);
            SessionState.SetString(StatusKey, "Entering Play Mode and preparing Recorder...");
            SessionState.SetBool(RequestKey, true);
            Debug.Log("[Lab MOV/MP4 Recorder] Preparing: " + outputWithoutExtension + ".mov");
            EditorApplication.EnterPlaymode();
        }

        public static void StopRecording()
        {
            SessionState.SetString(StatusKey, "Stopping recording and finalizing the file...");
            if (State.Controller != null && State.Controller.IsRecording())
                State.Controller.StopRecording();
            State.Started = false;
            State.Director?.Stop();
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
            else
                SessionState.SetBool(RequestKey, false);
        }

        static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(RequestKey, false))
                StartInPlayMode();
            else if (state == PlayModeStateChange.ExitingPlayMode && SessionState.GetBool(RequestKey, false))
            {
                if (State.Controller != null && State.Controller.IsRecording())
                    State.Controller.StopRecording();
                State.Started = false;
                State.Director?.Stop();
                SessionState.SetString(StatusKey, "Finalizing MOV file...");
            }
            else if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(RequestKey, false))
            {
                SessionState.SetBool(RequestKey, false);
                State.Started = false;
                var output = SessionState.GetString(OutputKey, string.Empty);
                if (!string.IsNullOrEmpty(output) && File.Exists(output))
                {
                    Debug.Log("[Lab MOV/MP4 Recorder] COMPLETE - " + output);
                    if (SessionState.GetBool(ConvertMp4Key, false))
                    {
                        State.ConversionMovPath = output;
                        State.ConversionMp4Path = Path.ChangeExtension(output, ".mp4");
                        State.KeepMovAfterConversion = SessionState.GetBool(KeepMovKey, true);
                        State.ConversionReadyTime = EditorApplication.timeSinceStartup + 1d;
                        State.ConversionPending = true;
                        SessionState.SetString(StatusKey, "MOV complete - preparing high-quality MP4 conversion...");
                    }
                    else
                    {
                        SessionState.SetString(StatusKey, "Complete: " + output);
                    }
                }
                else
                {
                    SessionState.SetString(StatusKey, "Stopped - check the output file.");
                    Debug.LogWarning("[Lab MOV/MP4 Recorder] MOV output was not found yet: " + output);
                }
            }
        }

        static void StartInPlayMode()
        {
            var recordingMode = (LabRecordingMode)SessionState.GetInt(
                RecordingModeKey, (int)LabRecordingMode.TimelineCinematic);
            var isTimelineMode = recordingMode == LabRecordingMode.TimelineCinematic;

            State.Director = isTimelineMode ? FindTimelineDirector() : null;
            if (isTimelineMode && State.Director == null)
            {
                FailAndExit("No PlayableDirector was found in Play Mode.");
                return;
            }

            if (!isTimelineMode && SessionState.GetBool(StopTimelinesKey, true))
                StopAllTimelineDirectors();

            var output = SessionState.GetString(OutputKey, string.Empty);
            var outputWithoutExtension = Path.Combine(
                Path.GetDirectoryName(output) ?? string.Empty,
                Path.GetFileNameWithoutExtension(output));
            var outputWidth = Mathf.Max(1, SessionState.GetInt(WidthKey, 1920));
            var outputHeight = Mathf.Max(1, SessionState.GetInt(HeightKey, 800));

            var controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
            controllerSettings.name = "HDRP Lab " + outputWidth + "x" + outputHeight +
                                      (isTimelineMode ? " Timeline" : " Gameplay") + " ProRes Controller";
            if (isTimelineMode)
                controllerSettings.SetRecordModeToFrameInterval(0, LastFrame);
            else
                controllerSettings.SetRecordModeToManual();
            controllerSettings.FrameRate = Fps;
            controllerSettings.FrameRatePlayback = FrameRatePlayback.Constant;
            controllerSettings.CapFrameRate = true;
            controllerSettings.ExitPlayMode = isTimelineMode;

            var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
            movie.name = "HDRP Lab ProRes 422 HQ MOV";
            movie.Enabled = true;
            movie.FrameRate = Fps;
            movie.EncoderSettings = new ProResEncoderSettings
            {
                Format = ProResEncoderSettings.OutputFormat.ProRes422HQ
            };
            movie.CaptureAlpha = false;
            movie.CaptureAudio = true;
            movie.ImageInputSettings = new GameViewInputSettings
            {
                OutputWidth = outputWidth,
                OutputHeight = outputHeight
            };
            movie.OutputFile = outputWithoutExtension;
            controllerSettings.AddRecorderSettings(movie);

            RecorderOptions.VerboseMode = false;
            State.Controller = new RecorderController(controllerSettings);
            if (isTimelineMode)
            {
                State.Director.Stop();
                State.Director.time = 0d;
                State.Director.Evaluate();
            }
            State.Controller.PrepareRecording();
            State.Started = State.Controller.StartRecording();
            if (!State.Started)
            {
                FailAndExit("Recorder failed to start.");
                return;
            }
            State.RecordingStartUnscaledTime = Time.unscaledTimeAsDouble;

            if (isTimelineMode)
            {
                State.Director.Play();
                SessionState.SetString(StatusKey, "Recording Timeline - " + outputWidth + "x" + outputHeight +
                                                         " / total 00:50 / ProRes 422 HQ / Audio");
                Debug.Log("[Lab MOV/MP4 Recorder] RECORDING TIMELINE - " + outputWidth + "x" + outputHeight +
                          ", duration 00:50, audio enabled.");
            }
            else
            {
                SessionState.SetString(StatusKey, "Recording Gameplay - click Stop Recording or press F9 when finished.");
                Debug.Log("[Lab MOV/MP4 Recorder] RECORDING GAMEPLAY - " + outputWidth + "x" + outputHeight +
                          ", manual stop with minutes:seconds timer, audio enabled.");
                EditorApplication.delayCall += FocusGameView;
            }
        }

        static void MonitorRecording()
        {
            MonitorMp4Conversion();

            if (!EditorApplication.isPlaying || !State.Started || State.Controller == null || State.Controller.IsRecording())
                return;
            State.Started = false;
            State.Director?.Stop();
            State.Controller.StopRecording();
            SessionState.SetString(StatusKey, "Finalizing MOV file...");
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }

        static void MonitorMp4Conversion()
        {
            if (State.ConversionPending && EditorApplication.timeSinceStartup >= State.ConversionReadyTime)
            {
                State.ConversionPending = false;
                StartMp4Conversion();
            }

            if (State.ConversionProcess == null)
                return;

            try
            {
                if (!State.ConversionProcess.HasExited)
                    return;

                var exitCode = State.ConversionProcess.ExitCode;
                State.ConversionProcess.Dispose();
                State.ConversionProcess = null;

                if (exitCode == 0 && File.Exists(State.ConversionMp4Path))
                {
                    SessionState.SetString(OutputKey, State.ConversionMp4Path);
                    SessionState.SetString(StatusKey, "Complete: " + State.ConversionMp4Path);
                    Debug.Log("[Lab MOV/MP4 Recorder] HIGH-QUALITY MP4 COMPLETE - " + State.ConversionMp4Path);

                    if (!State.KeepMovAfterConversion && File.Exists(State.ConversionMovPath))
                    {
                        try
                        {
                            File.Delete(State.ConversionMovPath);
                            Debug.Log("[Lab MOV/MP4 Recorder] Removed MOV master after successful MP4 conversion: " +
                                      State.ConversionMovPath);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning("[Lab MOV/MP4 Recorder] MP4 succeeded, but the MOV master could not be removed: " +
                                             exception.Message);
                        }
                    }
                }
                else
                {
                    SessionState.SetString(StatusKey,
                        "MP4 conversion failed (FFmpeg exit code " + exitCode + "). MOV master was kept.");
                    Debug.LogError("[Lab MOV/MP4 Recorder] MP4 conversion failed with FFmpeg exit code " + exitCode +
                                   ". MOV master: " + State.ConversionMovPath);
                }
            }
            catch (Exception exception)
            {
                State.ConversionProcess?.Dispose();
                State.ConversionProcess = null;
                SessionState.SetString(StatusKey, "MP4 conversion monitoring failed. MOV master was kept.");
                Debug.LogError("[Lab MOV/MP4 Recorder] MP4 conversion monitoring failed: " + exception);
            }
        }

        static void StartMp4Conversion()
        {
            var ffmpeg = ResolveFfmpegExecutable(SessionState.GetString(FfmpegPathKey, string.Empty));
            if (string.IsNullOrEmpty(ffmpeg))
            {
                SessionState.SetString(StatusKey, "FFmpeg was not found. MOV master was kept.");
                Debug.LogError("[Lab MOV/MP4 Recorder] FFmpeg was not found. Select ffmpeg.exe in the recorder window.");
                return;
            }

            if (string.IsNullOrEmpty(State.ConversionMovPath) || !File.Exists(State.ConversionMovPath))
            {
                SessionState.SetString(StatusKey, "MOV master was not found; MP4 conversion was skipped.");
                Debug.LogError("[Lab MOV/MP4 Recorder] MOV master was not found: " + State.ConversionMovPath);
                return;
            }

            var crf = Mathf.Clamp(SessionState.GetInt(Mp4CrfKey, 16), 12, 23);
            var arguments = "-y -hide_banner -loglevel warning " +
                            "-i " + QuoteArgument(State.ConversionMovPath) + " " +
                            "-map 0:v:0 -map 0:a? " +
                            "-c:v libx264 -preset slow -crf " + crf + " " +
                            "-vf \"pad=ceil(iw/2)*2:ceil(ih/2)*2,format=yuv420p\" " +
                            "-profile:v high -tag:v avc1 " +
                            "-colorspace bt709 -color_primaries bt709 -color_trc bt709 -color_range tv " +
                            "-c:a aac -b:a 256k -movflags +faststart -max_muxing_queue_size 4096 " +
                            QuoteArgument(State.ConversionMp4Path);

            try
            {
                State.ConversionProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = ffmpeg,
                        Arguments = arguments,
                        WorkingDirectory = Path.GetDirectoryName(State.ConversionMovPath) ?? string.Empty,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                if (!State.ConversionProcess.Start())
                    throw new InvalidOperationException("FFmpeg did not start.");

                SessionState.SetString(StatusKey,
                    "Converting MP4 - H.264 CRF " + crf + " / preset slow / Rec.709...");
                Debug.Log("[Lab MOV/MP4 Recorder] CONVERTING TO MP4 - " + State.ConversionMp4Path +
                          "\n[Lab MOV/MP4 Recorder] FFmpeg: " + ffmpeg);
            }
            catch (Exception exception)
            {
                State.ConversionProcess?.Dispose();
                State.ConversionProcess = null;
                SessionState.SetString(StatusKey, "Could not start FFmpeg. MOV master was kept.");
                Debug.LogError("[Lab MOV/MP4 Recorder] Could not start FFmpeg: " + exception);
            }
        }

        static PlayableDirector FindTimelineDirector()
        {
            return UnityEngine.Object.FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include)
                .Where(item => item.playableAsset != null)
                .OrderByDescending(item => item.playableAsset.duration)
                .FirstOrDefault();
        }

        static void StopAllTimelineDirectors()
        {
            var directors = UnityEngine.Object.FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include);
            foreach (var timelineDirector in directors)
                timelineDirector.Stop();

            if (directors.Length > 0)
                Debug.Log("[Lab MOV/MP4 Recorder] Gameplay mode stopped " + directors.Length +
                          " Timeline State.Director(s) so player control can take priority.");
        }

        static void FocusGameView()
        {
            var gameViewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if (gameViewType == null)
                return;

            var gameView = EditorWindow.GetWindow(gameViewType);
            gameView.Show();
            gameView.Focus();
        }

        static string MakeSafeFileName(string value)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }

        public static string FormatMinutesSeconds(double seconds)
        {
            var wholeSeconds = Math.Max(0, (long)Math.Floor(seconds));
            var minutes = wholeSeconds / 60;
            var remainingSeconds = wholeSeconds % 60;
            return minutes.ToString("00") + ":" + remainingSeconds.ToString("00");
        }

        static string ResolveOutputDirectory(string requestedOutputDirectory)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            if (string.IsNullOrWhiteSpace(requestedOutputDirectory))
                return Path.Combine(projectRoot, "Recordings", "URPLab");

            var expandedPath = Environment.ExpandEnvironmentVariables(
                requestedOutputDirectory.Trim().Trim('"'));
            return Path.GetFullPath(Path.IsPathRooted(expandedPath)
                ? expandedPath
                : Path.Combine(projectRoot, expandedPath));
        }

        public static string FindFfmpegExecutable()
        {
            return ResolveFfmpegExecutable(string.Empty);
        }

        static string ResolveFfmpegExecutable(string requestedPath)
        {
            if (!string.IsNullOrWhiteSpace(requestedPath))
            {
                var expandedRequestedPath = Environment.ExpandEnvironmentVariables(
                    requestedPath.Trim().Trim('"'));
                if (File.Exists(expandedRequestedPath))
                    return Path.GetFullPath(expandedRequestedPath);
            }

            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var localCandidates = new[]
            {
                Path.Combine(projectRoot, "Tools", "FFmpeg", "ffmpeg.exe"),
                Path.Combine(projectRoot, "Tools", "ffmpeg.exe"),
                Path.Combine(projectRoot, "ffmpeg.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Microsoft", "WinGet", "Links", "ffmpeg.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Virtual Desktop Streamer", "ffmpeg.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "youplayer", "ffmpeg.exe")
            };

            foreach (var candidate in localCandidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            var pathValue = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var directory in pathValue.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(directory))
                    continue;

                try
                {
                    var candidate = Path.Combine(directory.Trim().Trim('"'), "ffmpeg.exe");
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch (Exception)
                {
                    // Ignore malformed PATH entries and continue searching.
                }
            }

            return string.Empty;
        }

        static string QuoteArgument(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }

        static void FailAndExit(string message)
        {
            State.Started = false;
            SessionState.SetBool(RequestKey, false);
            SessionState.SetString(StatusKey, message);
            Debug.LogError("[Lab MOV/MP4 Recorder] " + message);
            if (EditorApplication.isPlaying)
                EditorApplication.ExitPlaymode();
        }
    }
}

