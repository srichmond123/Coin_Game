using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using UnityEngine;
using UnityEngine.XR;
using Vector3 = UnityEngine.Vector3;
using SocketIO;
using Quaternion = UnityEngine.Quaternion;


public class Controller : MonoBehaviour {
    public static Color NULL_COLOR = Color.magenta; //Placeholder for color I probably won't have uses for
    //public const float MYCOIN_BOOST_FACTOR = 5.0f; //2 * speed
    //public const float OTHERCOIN_BOOST_FACTOR = 10.0f;
    //public const float BOOST_TIME = 4.0f;
    //private const float SLOWDOWN_INTERVAL = 1.0f;
    private const float MIN_RANGE = 10f;
    private const float MAX_RANGE = 200f; //Maybe infinite
    private const float OWN_RANGE_INCREASE = 20f;
    private const float OTHER_RANGE_INCREASE = 40f;
    private const float CONST_DECREASE = 2.0f; // CONST_DECREASE * Time.deltaTime (per frame)
    private const float INITIAL_RANGE = 80f;
    
    public bool disableVR;

    private float speed = 4f;
    public static SocketIOComponent socket;
    public static string myId;
    private bool setInitialPositions = false;
    public static List<Opponent> opponents;
    public static List<string> permissibleIndividuals;
    public static int MyCoinsOwned = 0, OtherCoinsOwned = 0;

    public GameObject arrowOfVirtue;
    public int _MyCoins, _OtherCoins;
    private bool flying = true;
    private float currBoost = 1f;
    //private float boostTime = 0f;
    private float timeSpentSlowingDown = 0f;
    //private float oldBoost = -1f;
    private Buckets buckets;
    private float luminosity = 0.2f;
    private Light light; //TODO other lights don't light up your own coins
    public static GameObject controllerGameObject;

    void Start() {
        if (disableVR) {
            XRSettings.LoadDeviceByName("");
            XRSettings.enabled = false;
        }
        else {
            //
        }
        buckets = GameObject.Find("Buckets").GetComponent<Buckets>();
        opponents = new List<Opponent>();
        permissibleIndividuals = new List<string>();
        opponents.Add(GameObject.Find("Player_1").GetComponent<Opponent>());
        opponents.Add(GameObject.Find("Player_2").GetComponent<Opponent>());
        
        GameObject sockObject = GameObject.Find("SocketIO");
        socket = sockObject.GetComponent<SocketIOComponent>();
        
        socket.On("start", HandleStart);
        socket.On("update", OnSocketUpdate);
        socket.On("give", HandleGenerosity);
        socket.On("getOut", HandleRejection);
        light = GameObject.Find("My Light").GetComponent<Light>();
        controllerGameObject = gameObject;
    }

    void HandleRejection(SocketIOEvent e) {
        //TODO Thank you screen, etc.
    }

    void _displayCoins() {
        _MyCoins = MyCoinsOwned;
        _OtherCoins = OtherCoinsOwned;
    }

    void HandleStart(SocketIOEvent e) {
        Dictionary<string, string> res = e.data.ToDictionary();
        myId = res["id"];
        transform.localPosition = DeserializeVector3(e.data["position"]);
        //TODO Initial rotation vectors
        Opponent.UPDATE_INTERVAL = float.Parse(res["interval"]);
        JSONObject topologyArray = e.data["topology"][myId];
        permissibleIndividuals.Clear();
        for (int i = 0; i < topologyArray.Count; i++) {
            permissibleIndividuals.Add(topologyArray[i].str);
        }

        Endzone.Finished = false; //TODO Delete all this endzone code
        Endzone.OthersFinished = 0;
        MyCoinsOwned = 0;
        OtherCoinsOwned = 0;

        //TODO initial rotation (random or facing forward always)
        light.range = INITIAL_RANGE;
    }

    void ShowGenerosity(Opponent opponent) {
        //TODO maybe don't allow sharing by clicking on person
        string to = opponent.GetId();
        if (MyCoinsOwned == 0 || !permissibleIndividuals.Contains(to)) return;

        MyCoinsOwned--;
        Dictionary<string, string> dict = new Dictionary<string, string>();
        dict["id"] = to;
        socket.Emit("give", new JSONObject(dict));
    }

    void HandleGenerosity(SocketIOEvent e) {
        /*
         * Animate from opponent obj with id e.data[from] to e.data[to],
         * or if e.data[to] == myId, animate from e.data[from] to me:
         */
        Dictionary<string, string> data = e.data.ToDictionary();
        if (data["to"].Equals(myId)) {
            OtherCoinsOwned++;
            //TODO Animation of receiving a coin
        }
        else {
            VirtueSignal(GetOpponentById(data["from"]), GetOpponentById(data["to"]));
        }
    }

    void VirtueSignal(Opponent from, Opponent to) {
        Vector3 fromVec = from.transform.localPosition;
        Vector3 toVec = to.transform.localPosition;
        GameObject inst = Instantiate(arrowOfVirtue); 
        inst.transform.LookAt(toVec - fromVec);
        Vector3 currScale = inst.transform.localScale;
        currScale.z = Vector3.Distance(fromVec, toVec) - to.transform.localScale.x * 1.5f; // Width of player objects
        inst.transform.localScale = currScale;
        inst.transform.localPosition = toVec;
        Destroy(inst, 1.0f);
        //TODO Show better animation, shuffle around opponents' coin values maybe 
    }
    
    
    public static Opponent GetOpponentById(string id) {
        foreach (Opponent o in opponents) {
            if (o.GetId().Equals(id)) {
                return o;
            }
        }

        throw new Exception("Opponent " + id + " not found.");
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
        send.AddField("range", light.range);
        send.AddField("flying", flying);
        socket.Emit("update", send);
    }

    public static Vector3 GetMyPosition() {
        return controllerGameObject.transform.localPosition;
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

    public void AdjustMyLight() {
        if (light.range > MIN_RANGE) {
            light.range -= CONST_DECREASE * Time.deltaTime;
        }

        if (MyCoinsOwned > 0) {
            MyCoinsOwned--;
            light.range += OWN_RANGE_INCREASE;
        }
        if (OtherCoinsOwned > 0) {
            OtherCoinsOwned--;
            light.range += OTHER_RANGE_INCREASE;
        }
        //TODO Max range
    }

    // Update is called once per frame
    void Update() {
        AdjustMyLight();
        if (!disableVR) {
            if (OVRInput.GetDown(OVRInput.Button.One)) {
                //A button pressed, right controller:
                //Fly(speed * Time.deltaTime);
                flying = true;
            }
            else if (OVRInput.GetUp(OVRInput.Button.One)) {
                flying = false;
            }

            if (OVRInput.GetUp(OVRInput.Button.Two)) {
                //Stop boosting, don't stop flying though
            }
            
            if (OVRInput.GetDown(OVRInput.Button.SecondaryIndexTrigger)) {
                //Raycast, check tag, call ShowGenerosity
            }
        }
        else {
            _displayCoins();
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
                flying = true;
            }
            if (Input.GetKeyDown(KeyCode.S)) {
                flying = !flying;
            }

            if (Input.GetMouseButtonDown(0)) {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit)) {
                    if (hit.transform.tag.Equals("Not Me")) {
                        //ShowGenerosity(hit.transform.GetComponent<Opponent>());
                    } else if (hit.transform.tag.Equals("Bucket")) {
                        buckets.HandleClick(hit.transform);
                    }
                }
            }
        }

        if (flying) {
            Fly(speed * Time.deltaTime); // * currBoost);
            /*if (boostTime > 0f) {
                //TODO show progress bar of how much boost left
                boostTime -= Time.deltaTime;
            } else {
                //Start slowing down
                if (oldBoost < 0f) {
                    oldBoost = currBoost;
                }
                boostTime = 0f;
                if (currBoost > 1f) {
                    currBoost -= ((oldBoost - 1f) / SLOWDOWN_INTERVAL) * Time.deltaTime;
                } else {
                    currBoost = 1f;
                    oldBoost = -1f;
                }
            }*/
        }
    }
    
    public static void LogMessage(string s) {
        JSONObject msg = new JSONObject(JSONObject.Type.OBJECT);
        msg.AddField("Message", s);
        socket.Emit("log", msg);
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

    /*void Boost(float boostFactor) {
        if (boostTime <= 0f) {
            //User is pressing B and not currently boosting, use coin:
            if (boostFactor.Equals(MYCOIN_BOOST_FACTOR)) MyCoinsOwned--;
            else OtherCoinsOwned--;
            boostTime = BOOST_TIME;
            currBoost = boostFactor;
        }
    }*/
}
