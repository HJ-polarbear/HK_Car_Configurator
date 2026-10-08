namespace URPLabStudio
{
using UnityEngine;
using UnityEngine.InputSystem;

public class Lab_Light_Brake : MonoBehaviour
{
    public Light[] lights;
    [HideInInspector] public KeyCode keyboard;
    [HideInInspector] public KeyCode keyboard2;

    private InputAction brakeLightAction;

    private void OnEnable()
    {
        brakeLightAction = new InputAction("Vehicle Brake Lights", InputActionType.Button);
        brakeLightAction.AddBinding("<Keyboard>/space");
        brakeLightAction.AddBinding("<Keyboard>/s");
        brakeLightAction.AddBinding("<Keyboard>/downArrow");
        brakeLightAction.AddBinding("<Gamepad>/buttonSouth");
        brakeLightAction.Enable();
        SetLights(false);
    }

    private void Update()
    {
        SetLights(brakeLightAction != null && brakeLightAction.IsPressed());
    }

    private void OnDisable()
    {
        SetLights(false);
        if (brakeLightAction == null)
            return;

        brakeLightAction.Disable();
        brakeLightAction.Dispose();
        brakeLightAction = null;
    }

    private void SetLights(bool enabledState)
    {
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null && lights[i].enabled != enabledState)
                lights[i].enabled = enabledState;
        }
    }
}
}
