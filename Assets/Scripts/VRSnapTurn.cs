using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine.SceneManagement;
using System.Security;
using JetBrains.Annotations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using System;

public class VRSnapTurnOVR : MonoBehaviour
{
    public GameObject WorldRoot; // what you rotate (WorldRoot, Player Rig, etc.)
    public float snapAngle = 20f;
    public float inputThreshold = 0.7f;
    public float snapCooldown = 0.5f;

    public float hapticStrength = 0.5f; // vibration strength (0..1)
    public float hapticDuration = 0.1f; // seconds

    private float lastSnapTime = 0f;

    public GameObject laser;

    public ExpController expController;



    void Update()
    {
        if (expController.isSnapTurn && expController.isNotPassThrough) //only include feature of turning by joystick, if applicable
        {


            Vector2 rightStick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
            float horizontalInput = rightStick.x;

            if (Time.time > lastSnapTime + snapCooldown)
            {
                if (laser != null && laser.activeSelf)
                {
                    if (horizontalInput > inputThreshold || horizontalInput < -inputThreshold)
                    {
                        // rotation not allowed while laser is active, haptic feedback
                        StartCoroutine(TriggerDoubleErrorHaptics(OVRInput.Controller.RTouch));
                    }

                    return;
                }

                if (horizontalInput > inputThreshold)
                {
                    SnapTurn(-snapAngle);
                    TriggerHaptics(OVRInput.Controller.RTouch);
                }
                else if (horizontalInput < -inputThreshold)
                {
                    SnapTurn(snapAngle);
                    TriggerHaptics(OVRInput.Controller.RTouch);
                }
            }
        }
    }

    void SnapTurn(float angle)
    {
        WorldRoot.transform.rotation *=
            Quaternion.AngleAxis(angle, Vector3.up);
        lastSnapTime = Time.time;
    }

    void TriggerHaptics(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(1f, hapticStrength, controller);
        Invoke(nameof(StopHaptics), hapticDuration);
    }

    void StopHaptics()
    {
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.LTouch); // just to be safe
    }

    IEnumerator TriggerDoubleErrorHaptics(OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(1f, 1f, controller);
        yield return new WaitForSeconds(0.1f);
        OVRInput.SetControllerVibration(0, 0, controller);
        yield return new WaitForSeconds(0.05f);
        OVRInput.SetControllerVibration(1f, 1f, controller);
        yield return new WaitForSeconds(0.1f);
        OVRInput.SetControllerVibration(0, 0, controller);
    }
}

