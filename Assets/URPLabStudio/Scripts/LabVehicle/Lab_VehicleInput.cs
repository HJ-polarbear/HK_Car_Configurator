using URPLabStudio;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Lab.Vehicle
{
    public static class Lab_VehicleInput
    {
        public static Vector2 Drive
        {
            get
            {
                Vector2 value = Vector2.zero;
                Keyboard keyboard = Keyboard.current;
                if (keyboard != null)
                {
                    value.x = ReadAxis(keyboard.aKey, keyboard.leftArrowKey, keyboard.dKey, keyboard.rightArrowKey);
                    value.y = ReadAxis(keyboard.sKey, keyboard.downArrowKey, keyboard.wKey, keyboard.upArrowKey);
                }
                Gamepad pad = Gamepad.current;
                if (pad != null && pad.leftStick.ReadValue().sqrMagnitude > value.sqrMagnitude) value = pad.leftStick.ReadValue();
                return Vector2.ClampMagnitude(value, 1f);
            }
        }
        public static bool Handbrake => (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) || (Gamepad.current != null && Gamepad.current.buttonSouth.isPressed);
        public static bool CameraCyclePressed => (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame);
        public static bool BrakePressed => Drive.y < -0.05f || Handbrake;
        public static bool WasLegacyKeyPressed(KeyCode key)
        {
            Keyboard k = Keyboard.current;
            if (k == null) return false;
            return key switch
            {
                KeyCode.Alpha1 or KeyCode.Keypad1 => k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame,
                KeyCode.Alpha2 or KeyCode.Keypad2 => k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame,
                KeyCode.Alpha3 or KeyCode.Keypad3 => k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame,
                KeyCode.Alpha4 or KeyCode.Keypad4 => k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame,
                KeyCode.L => k.lKey.wasPressedThisFrame,
                _ => false
            };
        }
        public static bool Pressed(Key key) => Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        private static float ReadAxis(ButtonControl negativeA, ButtonControl negativeB, ButtonControl positiveA, ButtonControl positiveB)
        {
            float negative = negativeA.isPressed || negativeB.isPressed ? 1f : 0f;
            float positive = positiveA.isPressed || positiveB.isPressed ? 1f : 0f;
            return positive - negative;
        }
    }
}
