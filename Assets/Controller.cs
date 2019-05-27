using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.XR;
using Vector3 = UnityEngine.Vector3;


public class Controller : MonoBehaviour {
    public bool disableVR;

    private float speed = 1.0f;
    // Start is called before the first frame update
    void Start() {
        if (disableVR) {
            XRSettings.LoadDeviceByName("");
            XRSettings.enabled = false;
        }
        else {
            //
        }
    }

    // Update is called once per frame
    void Update() {
        if (OVRInput.Get(OVRInput.Button.One)) { //A button pressed, right controller:
            fly(speed * Time.deltaTime);
        } else if (OVRInput.Get(OVRInput.Button.Two)) {
            fly(-speed * Time.deltaTime);
        }
    }

    // "fly" meaning translate position in direction camera is facing
    void fly(float speed) {
        transform.localPosition += transform.GetChild(0).GetChild(0).forward * speed;
    }
}
