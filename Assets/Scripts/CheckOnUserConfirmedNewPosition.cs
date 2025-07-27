using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckOnUserRepeatPrepare : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.name == "RightOVRControllerPrefab")
        {
            ExpController.userConfirmedRepeatPrepare = true;
        }
    }

}
