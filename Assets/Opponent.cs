using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;

public class Opponent : MonoBehaviour {
    // Start is called before the first frame update
    public static float UPDATE_INTERVAL = 0.05f;
    public static bool colorTaken = false; //Static global allows Opponent object to handle diff opponent colors internally
    
    private string id = "";
    private Vector3 targetPosition = Vector3.zero, oldPosition = Vector3.zero;
    private Quaternion targetRotation = Quaternion.identity, oldRotation = Quaternion.identity;
    private float targetTime = 0f, oldTime = 0f;
    private float timeSinceUpdate = -1f;
    private Color color = Color.white;
    private Light light;
    private bool flying = false;
    private List<Vector3> positionQueue;
    private List<Quaternion> rotationQueue;
    private List<float> timestampQueue;
    private bool startQueue = false;
    private float interval = 0f;
    public int Score = 0, MyCoins = 0, OtherCoins = 0;

    void Start() {
        light = transform.GetComponentsInChildren<Light>()[0];
        positionQueue = new List<Vector3>();
        rotationQueue = new List<Quaternion>();
        timestampQueue = new List<float>();
    }

    // Update is called once per frame
    void Update() {
        if (startQueue) {
            //Take interval time to go from queue[0] to queue[1]:
            if (timeSinceUpdate >= interval || oldPosition.Equals(Vector3.zero)) {
                if (oldPosition.Equals(Vector3.zero)) {
                    oldPosition = Pop(positionQueue, 0);
                    oldRotation = Pop(rotationQueue, 0);
                    oldTime = Pop(timestampQueue, 0);
                }
                else {
                    Transform t = transform;
                    oldPosition = t.localPosition;
                    oldRotation = t.localRotation;
                    oldTime = targetTime;
                }
                targetPosition = Pop(positionQueue, 0);
                targetRotation = Pop(rotationQueue, 0);
                targetTime = Pop(timestampQueue, 0);
                timeSinceUpdate = Time.deltaTime;
                interval = targetTime - oldTime;
            }
            transform.localPosition = Vector3.Lerp(oldPosition, targetPosition, timeSinceUpdate / interval);
            transform.localRotation = Quaternion.Lerp(oldRotation, targetRotation, timeSinceUpdate / interval);
            timeSinceUpdate += Time.deltaTime;
        }
    }

    private T Pop<T>(List<T> v, int i) {
        T elem = v[i];
        v.RemoveAt(i);
        return elem;
    }

    public void _SetColor(Color c) {
        color = c;
        transform.GetComponentsInChildren<Light>()[1].color = c;
    }
    
    public void SetId(string id) {
        this.id = id;
        if (color == Color.white) {
            _SetColor(colorTaken ? Color.red : Color.blue);
            colorTaken = true;
        }
    }

    public float GetRange() {
        return light.range;
    }

    public Color GetColor() {
        return color;
    }
    
    public string GetId() {
        return id;
    }

    //Turn raw server data into position, rotation for specific this.id:
    public void AdjustTransform(JSONObject data, bool hardSet) {
        //JSONObject myData = data[Controller.myId];
        JSONObject myData = data[id];
        JSONObject pos = myData["position"];
        JSONObject rot = myData["rotation"];
        light.range = myData["range"].f;
        if (myData["flying"].b != flying) {
            flying = myData["flying"].b;
            if (flying) {
                transform.GetComponentInChildren<Animation>().Play();
            }
            else {
                transform.GetComponentInChildren<Animation>().Stop();
            }
        }
        //Controller.socket.Emit("log", pos);

        Vector3 tPosition = Controller.DeserializeVector3(pos);
        Quaternion tRotation = Controller.DeserializeQuaternion(rot);

		Transform t = transform;
        if (hardSet) {
            t.localPosition = tPosition;
            t.localRotation = tRotation;
        }
		oldPosition = t.localPosition;
		oldRotation = t.localRotation;
        positionQueue.Add(tPosition);
        rotationQueue.Add(tRotation);
        timestampQueue.Add(Time.time);
        if (positionQueue.Count > 2) {
            startQueue = true;
        } else if (positionQueue.Count < 1) {
            startQueue = false;
        }
    }
}
