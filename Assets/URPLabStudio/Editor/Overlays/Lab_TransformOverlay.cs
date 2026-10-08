namespace URPLabStudio
{
using UnityEditor;
using UnityEngine;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.UIElements;

[Overlay(typeof(SceneView), "Transform", true)]
public class Lab_TransformOverlay : Overlay
{
    private static readonly Color kPanelBg = new Color(0.12f, 0.12f, 0.12f, 0.96f);
    private static readonly Color kFieldBg = new Color(0.16f, 0.16f, 0.16f, 1f);
    private static readonly Color kFieldBorder = new Color(0.24f, 0.24f, 0.24f, 1f);

    private static readonly Color kButtonNormal = new Color(0.23f, 0.23f, 0.23f, 1f);
    private static readonly Color kButtonHover = new Color(0.30f, 0.30f, 0.30f, 1f);

    private static readonly Color kAxisDark = new Color(0.18f, 0.18f, 0.18f, 1f);
    private static readonly Color kAxisDarkHover = new Color(0.24f, 0.24f, 0.24f, 1f);

    private static readonly Color kAxisLight = new Color(0.78f, 0.78f, 0.78f, 1f);
    private static readonly Color kAxisLightHover = new Color(0.88f, 0.88f, 0.88f, 1f);

    private static readonly Color kResetButton = new Color(0.30f, 0.30f, 0.30f, 1f);
    private static readonly Color kResetButtonHover = new Color(0.38f, 0.38f, 0.38f, 1f);

    private Vector3 copiedPosition;
    private Vector3 copiedRotation;
    private Vector3 copiedScale;

    public override VisualElement CreatePanelContent()
    {
        var container = new VisualElement();
        container.style.paddingTop = 6;
        container.style.paddingBottom = 6;
        container.style.paddingLeft = 6;
        container.style.paddingRight = 6;
        container.style.width = 208;
        container.style.backgroundColor = kPanelBg;
        container.style.borderTopLeftRadius = 6;
        container.style.borderTopRightRadius = 6;
        container.style.borderBottomLeftRadius = 6;
        container.style.borderBottomRightRadius = 6;
        container.style.borderLeftWidth = 1;
        container.style.borderRightWidth = 1;
        container.style.borderTopWidth = 1;
        container.style.borderBottomWidth = 1;
        container.style.borderLeftColor = kFieldBorder;
        container.style.borderRightColor = kFieldBorder;
        container.style.borderTopColor = kFieldBorder;
        container.style.borderBottomColor = kFieldBorder;

        var positionField = new Vector3Field("P");
        var rotationField = new Vector3Field("R");
        var scaleField = new Vector3Field("S");

        StyleVector3Field(positionField);
        StyleVector3Field(rotationField);
        StyleVector3Field(scaleField);

        positionField.RegisterValueChangedCallback(evt => ApplyToSelected(t => t.position = evt.newValue));
        rotationField.RegisterValueChangedCallback(evt => ApplyToSelected(t => t.eulerAngles = evt.newValue));
        scaleField.RegisterValueChangedCallback(evt => ApplyToSelected(t => t.localScale = evt.newValue));

        var buttonContainer = new VisualElement();
        buttonContainer.style.flexDirection = FlexDirection.Row;
        buttonContainer.style.marginTop = 6;
        buttonContainer.style.justifyContent = Justify.SpaceBetween;

        var copyButton = new Button(() =>
        {
            if (Selection.activeTransform != null)
            {
                copiedPosition = Selection.activeTransform.position;
                copiedRotation = Selection.activeTransform.eulerAngles;
                copiedScale = Selection.activeTransform.localScale;
            }
        })
        { text = "Copy" };

        var pasteButton = new Button(() =>
        {
            ApplyToSelected(t =>
            {
                t.position = copiedPosition;
                t.eulerAngles = copiedRotation;
                t.localScale = copiedScale;
            });
            UpdateFields();
        })
        { text = "Paste" };

        var resetButton = new Button(() =>
        {
            ApplyToSelected(t =>
            {
                t.position = Vector3.zero;
                t.eulerAngles = Vector3.zero;
                t.localScale = Vector3.one;
            });
            UpdateFields();
        })
        { text = "Reset" };

        StyleActionButton(copyButton, kButtonNormal, kButtonHover, Color.white);
        StyleActionButton(pasteButton, kAxisLight, kAxisLightHover, Color.black);
        StyleActionButton(resetButton, kResetButton, kResetButtonHover, Color.white);

        copyButton.style.flexGrow = 1;
        pasteButton.style.flexGrow = 1;
        resetButton.style.flexGrow = 1;

        copyButton.style.marginRight = 3;
        pasteButton.style.marginRight = 3;

        buttonContainer.Add(copyButton);
        buttonContainer.Add(pasteButton);
        buttonContainer.Add(resetButton);

        container.Add(positionField);
        container.Add(rotationField);
        container.Add(scaleField);
        container.Add(CreateMinimalRotationControls(rotationField));
        container.Add(buttonContainer);

        void UpdateFields()
        {
            if (Selection.activeTransform != null)
            {
                positionField.SetValueWithoutNotify(Selection.activeTransform.position);
                rotationField.SetValueWithoutNotify(Selection.activeTransform.eulerAngles);
                scaleField.SetValueWithoutNotify(Selection.activeTransform.localScale);
            }
        }

        Selection.selectionChanged += UpdateFields;
        EditorApplication.update += UpdateFields;
        UpdateFields();

        return container;
    }

    private void ApplyToSelected(System.Action<Transform> action)
    {
        if (Selection.activeTransform != null)
        {
            Undo.RecordObject(Selection.activeTransform, "Transform Change");
            action(Selection.activeTransform);
        }
    }

    private VisualElement CreateMinimalRotationControls(Vector3Field rotationField)
    {
        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.marginTop = 6;

        string[] axes = { "X", "Y", "Z" };

        for (int i = 0; i < 3; i++)
        {
            int axisIndex = i;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 3;

            var label = new Label($"R{axes[i]}");
            label.style.minWidth = 26;
            label.style.color = new Color(0.82f, 0.82f, 0.82f, 1f);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(label);

            var minusBtn = new Button();
            minusBtn.text = "-90";
            StyleAxisButton(minusBtn, kAxisDark, kAxisDarkHover);

            minusBtn.clicked += () =>
            {
                AnimateButton(minusBtn);
                ApplyToSelected(t =>
                {
                    var rot = t.eulerAngles;
                    rot[axisIndex] -= 90f;
                    t.eulerAngles = rot;
                    rotationField.SetValueWithoutNotify(t.eulerAngles);
                });
            };

            var plusBtn = new Button();
            plusBtn.text = "+90";
            StyleAxisButton(plusBtn, kAxisLight, kAxisLightHover);

            plusBtn.clicked += () =>
            {
                AnimateButton(plusBtn);
                ApplyToSelected(t =>
                {
                    var rot = t.eulerAngles;
                    rot[axisIndex] += 90f;
                    t.eulerAngles = rot;
                    rotationField.SetValueWithoutNotify(t.eulerAngles);
                });
            };

            row.Add(minusBtn);
            row.Add(plusBtn);
            container.Add(row);
        }

        return container;
    }

    private void StyleVector3Field(Vector3Field field)
    {
        field.style.marginBottom = 3;
        if (field.labelElement != null)
        {
            field.labelElement.style.minWidth = 18;
            field.labelElement.style.color = new Color(0.82f, 0.82f, 0.82f, 1f);
        }

        foreach (var input in field.Query<TextField>().ToList())
        {
            input.style.backgroundColor = kFieldBg;
            input.style.color = Color.white;
            input.style.borderLeftWidth = 1;
            input.style.borderRightWidth = 1;
            input.style.borderTopWidth = 1;
            input.style.borderBottomWidth = 1;
            input.style.borderLeftColor = kFieldBorder;
            input.style.borderRightColor = kFieldBorder;
            input.style.borderTopColor = kFieldBorder;
            input.style.borderBottomColor = kFieldBorder;
        }
    }

    private void StyleAxisButton(Button button, Color baseColor, Color hoverColor)
    {
        button.style.width = 36;
        button.style.height = 20;
        button.style.marginRight = 4;
        button.style.backgroundColor = baseColor;
        button.style.color = baseColor.r > 0.6f ? Color.black : Color.white;

        button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = hoverColor);
        button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = baseColor);
    }

    private void StyleActionButton(Button button, Color baseColor, Color hoverColor, Color textColor)
    {
        button.style.height = 22;
        button.style.backgroundColor = baseColor;
        button.style.color = textColor;

        button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = hoverColor);
        button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = baseColor);
    }

    private void AnimateButton(Button button)
    {
        var up = new StyleScale(new Scale(new Vector3(1.06f, 1.06f, 1)));
        var normal = new StyleScale(new Scale(Vector3.one));
        button.style.scale = up;
        EditorApplication.delayCall += () => button.style.scale = normal;
    }
}

[EditorToolbarElement(ID)]
public class OpenTransformOverlayButton : EditorToolbarButton
{
    public const string ID = "CustomTools/TransformOverlayButton";

    public OpenTransformOverlayButton()
    {
        text = "Transform";
        icon = (Texture2D)EditorGUIUtility.IconContent("d_Transform Icon").image;
    }
}
}
