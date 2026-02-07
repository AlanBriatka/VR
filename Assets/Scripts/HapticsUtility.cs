using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public static class HapticsUtility
{
    private static readonly List<InputDevice> s_Devices = new List<InputDevice>();

    public enum Controller
    {
        Left,
        Right
    }

    public static void SendHapticImpulse(float intensity, float duration, Controller controller)
    {
        InputDeviceCharacteristics characteristics = InputDeviceCharacteristics.Controller |
            (controller == Controller.Left ? InputDeviceCharacteristics.Left : InputDeviceCharacteristics.Right);

        s_Devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(characteristics, s_Devices);

        foreach (var device in s_Devices)
        {
            if (device.TryGetHapticCapabilities(out HapticCapabilities capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0u, intensity, duration);
            }
        }
    }
}
