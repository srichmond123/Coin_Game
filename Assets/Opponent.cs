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
    void Start() { }

    // Update is called once per frame
    void Update() {
        if (timeSinceUpdate > -1f) {
            //timeSinceUpdate += Time.deltaTime;
            //float lerpFactor = timeSinceUpdate / UPDATE_INTERVAL;
            float lerpFactor = Time.deltaTime / (UPDATE_INTERVAL);
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, lerpFactor);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, lerpFactor);
            //realInterval += Time.deltaTime;
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
        //Controller.socket.Emit("log", pos);

        targetPosition = Controller.DeserializeVector3(pos); 
        targetRotation = Controller.DeserializeQuaternion(rot);

        if (hardSet) {
            transform.localPosition = targetPosition;
            transform.localRotation = targetRotation;
        }
        /*Dictionary<string, string> dict = new Dictionary<string, string>();
        dict["interval"] = realInterval.ToString();
        Controller.socket.Emit("log", new JSONObject(dict));*/
        realInterval = 0f;
		timeSinceUpdate = 0f;
    }
}
