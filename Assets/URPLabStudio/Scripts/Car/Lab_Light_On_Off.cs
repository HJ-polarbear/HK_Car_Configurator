namespace URPLabStudio
{
using UnityEngine;
using UnityEngine.InputSystem;

public class Lab_Light_On_Off : MonoBehaviour
{
    public Light[] lights;
    public KeyCode keyboard = KeyCode.L;

    private InputAction toggleAction;

    private void OnEnable()
    {
        toggleAction = new InputAction("Toggle Vehicle Lights", InputActionType.Button);
        toggleAction.AddBinding("<Keyboard>/l");
        toggleAction.AddBinding("<Gamepad>/rightShoulder");
        toggleAction.performed += ToggleLights;
        toggleAction.Enable();
    }

    private void OnDisable()
    {
        if (toggleAction == null)
            return;

        toggleAction.performed -= ToggleLights;
        toggleAction.Disable();
        toggleAction.Dispose();
        toggleAction = null;
    }

    private void ToggleLights(InputAction.CallbackContext context)
    {
        ToggleLights();
    }

    public void ToggleLights()
    {
        bool nextState = false;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                nextState = !lights[i].enabled;
                break;
            }
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = nextState;
        }
    }
}
}
