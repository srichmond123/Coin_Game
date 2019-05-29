using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.XR;
using Vector3 = UnityEngine.Vector3;
using SocketIO;
using Quaternion = UnityEngine.Quaternion;


public class Controller : MonoBehaviour {
    public bool disableVR;

    private float speed = 1.0f;
    public static SocketIOComponent socket;
    public static string myId;
    private bool setInitialPositions = false;
    public static List<Opponent> opponents;
    public static int COINS_OWNED = 0;
    
    void Start() {
        if (disableVR) {
            XRSettings.LoadDeviceByName("");
            XRSettings.enabled = false;
        }
        else {
            //
        }
        opponents = new List<Opponent>();
        opponents.Add(GameObject.Find("Player_1").GetComponent<Opponent>());
        opponents.Add(GameObject.Find("Player_2").GetComponent<Opponent>());
        
        GameObject sockObject = GameObject.Find("SocketIO");
        socket = sockObject.GetComponent<SocketIOComponent>();
        
        socket.On("start", (SocketIOEvent e) => {
            Dictionary<string, string> res = e.data.ToDictionary();
            myId = res["id"];
            transform.localPosition = DeserializeVector3(e.data["position"]);
            Opponent.UPDATE_INTERVAL = float.Parse(res["interval"]);
        });
        
        socket.On("update", OnSocketUpdate);
        socket.On("tellGive", HandleJealousy);
    }

    void HandleJealousy(SocketIOEvent e) {
        //TODO animate from opponent obj with id e.data[from] to e.data[to]
    }


    void OnSocketUpdate(SocketIOEvent e) {
        if (opponents[0].GetId().Equals("")) {
            //If this is the first update, assign ids:
            int ind = 0;
            foreach (string key in e.data.keys) { // res.Keys) {
                if (!key.Equals(myId)) {
                    opponents[ind++].SetId(key);
                }
            }
        }
        else {
            foreach (Opponent o in opponents) {
                o.AdjustTransform(e.data, !setInitialPositions);
            }
            setInitialPositions = true;
        }
        
        JSONObject send = new JSONObject(JSONObject.Type.OBJECT);
        send.AddField("position", SerializeVector3(transform.localPosition));
        send.AddField("rotation", SerializeQuaternion(GetMyRotation()));
        socket.Emit("update", send);
    }
    
    

    public static JSONObject SerializeVector3(Vector3 v) {
        JSONObject res = new JSONObject(JSONObject.Type.OBJECT);
        res.AddField("x", v.x);
        res.AddField("y", v.y);
        res.AddField("z", v.z);
        return res;
    }

    public static JSONObject SerializeQuaternion(Quaternion q) {
        JSONObject res = new JSONObject(JSONObject.Type.OBJECT);
        res.AddField("x", q.x);
        res.AddField("y", q.y);
        res.AddField("z", q.z);
        res.AddField("w", q.w);
        return res;
    }

    public static Vector3 DeserializeVector3(JSONObject v) {
        Dictionary<string, string> pos_d = v.ToDictionary();
        return new Vector3(
            float.Parse(pos_d["x"]), 
            float.Parse(pos_d["y"]),
            float.Parse(pos_d["z"])
        );
    }

    public static Quaternion DeserializeQuaternion(JSONObject q) {
        Dictionary<string, string> rot_d = q.ToDictionary();  
        return new Quaternion(
            float.Parse(rot_d["x"]),
            float.Parse(rot_d["y"]),
            float.Parse(rot_d["z"]),
            float.Parse(rot_d["w"])
        );
    }

    // Update is called once per frame
    void Update() {
        if (!disableVR) {
            if (OVRInput.Get(OVRInput.Button.One)) {
                //A button pressed, right controller:
                Fly(speed * Time.deltaTime);
            }
            else if (OVRInput.Get(OVRInput.Button.Two)) {
                Fly(-speed * Time.deltaTime);
            }
            else if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) {
                //Raycast, check tag, get ID, Emit event
            }
        }
        else {
            if (Input.GetKey(KeyCode.RightArrow)) {
                transform.localEulerAngles += Vector3.up;
            }
            if (Input.GetKey(KeyCode.LeftArrow)) {
                transform.localEulerAngles -= Vector3.up;
            } 
            if (Input.GetKey(KeyCode.UpArrow)) {
                transform.localEulerAngles += Vector3.left;
            }
            if (Input.GetKey(KeyCode.DownArrow)) {
                transform.localEulerAngles -= Vector3.left;
            }

            if (Input.GetKey(KeyCode.W)) {
                Fly(speed * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.S)) {
                Fly(-speed * Time.deltaTime);
            }
        }
    }

    // "fly" meaning translate position in direction camera is facing
    void Fly(float speed) {
        transform.localPosition += transform.GetChild(0).GetChild(0).forward * speed;
    }

    Quaternion GetMyRotation() { //Left eye camera if in VR, otherwise, whole camera rig's rotation:
        if (!disableVR) {
            return transform.GetChild(0).GetChild(0).localRotation;
        }
        return transform.localRotation;
    }
}
