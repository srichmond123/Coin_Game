using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;

public class Opponent : MonoBehaviour {
    // Start is called before the first frame update
    private string id = "";
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float timeSinceUpdate = 0f;
    public static float UPDATE_INTERVAL = 0.05f;
    private float updateTimestamp = -1f;
    void Start() {
        
    }

    // Update is called once per frame
    void Update() {
        if (updateTimestamp > -1f) {
            float lerpFactor = (Time.time - updateTimestamp) / UPDATE_INTERVAL;
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetPosition, lerpFactor);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, lerpFactor);
        }
    }
    
    public void SetId(string id) {
        this.id = id;
    }
    
    public string GetId() {
        return id;
    }

    //Turn raw server data into position, rotation for specific this.id:
    public void AdjustTransform(JSONObject data, bool hardSet) {
        JSONObject pos = data["position"];
        JSONObject rot = data["rotation"];
        
        targetPosition = Controller.DeserializeVector3(pos);
        targetRotation = Controller.DeserializeQuaternion(rot);

        if (hardSet) {
            transform.localPosition = targetPosition;
            transform.localRotation = targetRotation;
        }

        updateTimestamp = Time.time;
    }
}
