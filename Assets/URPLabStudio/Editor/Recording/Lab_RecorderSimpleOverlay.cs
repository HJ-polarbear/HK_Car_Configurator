namespace URPLabStudio
{
#if UNITY_EDITOR && URPLAB_HAS_RECORDER
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "Recorder UI", true)]
public class Lab_RecorderSimpleOverlay : Overlay
{
    private double startTime;

    public override VisualElement CreatePanelContent()
    {
        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.style.paddingLeft = 6;
        root.style.paddingRight = 6;

        var recButton = new Button();
        recButton.text = "REC";
        recButton.style.color = Color.white;
        recButton.style.unityFontStyleAndWeight = FontStyle.Bold;
        recButton.style.marginRight = 6;

        var timeLabel = new Label("⏱ 00.0");
        timeLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        timeLabel.style.fontSize = 12;
        timeLabel.style.color = Color.white;

        recButton.clicked += () =>
        {
            if (!Lab_RecordingToolbar.IsRecording && !Lab_RecordingToolbar.IsWaitingForPlayMode)
            {
                Lab_RecordingToolbar.StartRecording();

                if (EditorApplication.isPlaying)
                {
                    startTime = EditorApplication.timeSinceStartup;
                }
            }
            else
            {
                Lab_RecordingToolbar.StopRecording();
            }

            RefreshButtonState(recButton);
        };

        root.Add(recButton);
        root.Add(timeLabel);

        EditorApplication.update += () =>
        {
            RefreshButtonState(recButton);

            if (Lab_RecordingToolbar.IsRecording)
            {
                double elapsed = EditorApplication.timeSinceStartup - startTime;
                timeLabel.text = $"⏱ {elapsed:00.0}";
            }
            else if (Lab_RecordingToolbar.IsWaitingForPlayMode)
            {
                timeLabel.text = "⏱ WAIT";
            }
            else
            {
                timeLabel.text = "⏱ 00.0";
            }
        };

        RefreshButtonState(recButton);
        return root;
    }

    private static void RefreshButtonState(Button button)
    {
        if (Lab_RecordingToolbar.IsRecording)
        {
            button.style.backgroundColor = Color.red;
        }
        else if (Lab_RecordingToolbar.IsWaitingForPlayMode)
        {
            button.style.backgroundColor = new Color(0.8f, 0.5f, 0.0f);
        }
        else
        {
            button.style.backgroundColor = new Color(0.25f, 0.25f, 0.25f);
        }
    }
}
#endif
}
