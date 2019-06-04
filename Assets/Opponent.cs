using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;

public class Opponent : MonoBehaviour {
    // Start is called before the first frame update
    public static float UPDATE_INTERVAL = 0.05f;
    public static bool colorTaken = false; //Static global allows Opponent object to handle diff opponent colors internally
    
    private float realInterval = 0f;
    private string id = "";
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float timeSinceUpdate = -1f;
    private Color color = Color.white;
    private Light light;

    void Start() {
        light = transform.GetChild(0).GetComponent<Light>();
    }

    // Update is called once per frame
    void Update() {
        if (timeSinceUpdate > -1f) {
            float lerpFactor = Time.deltaTime / (UPDATE_INTERVAL);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, lerpFactor);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, lerpFactor);
        }
    }

    public void _setColor(Color c) {
        color = c;
        transform.GetComponent<Renderer>().material.color = c;
    }
    
    public void SetId(string id) {
        this.id = id;
        if (color == Color.white) {
            _setColor(colorTaken ? Color.red : Color.blue);
            colorTaken = true;
        }
    }

    public Color GetColor() {
        return color;
    }
    
    public string GetId() {
        return id;
    }

    //Turn raw server data into position, rotation for specific this.id:
    public void AdjustTransform(JSONObject data, bool hardSet) {
        JSONObject myData = data[id];
        JSONObject pos = myData["position"];
        JSONObject rot = myData["rotation"];
        light.range = myData["range"].f; //TODO Lerp?
        //Controller.socket.Emit("log", pos);

        targetPosition = Controller.DeserializeVector3(pos); //+ new Vector3(4, 0, 0);
        targetRotation = Controller.DeserializeQuaternion(rot);

        if (hardSet) {
            Transform t = transform;
            t.localPosition = targetPosition;
            t.localRotation = targetRotation;
        }
        realInterval = 0f;
		timeSinceUpdate = 0f;
    }
}
