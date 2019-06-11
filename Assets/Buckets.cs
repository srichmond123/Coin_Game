using System.Collections;
using System.Collections.Generic;
using OculusSampleFramework;
using OVR.OpenVR;
using SocketIO;
using UnityEngine;

public class Buckets : MonoBehaviour {
    private static int coins = 0;
    public static SocketIOComponent socket;
    public GameObject crossPrefab;

    private bool colorsInitialized = false;
    private GameObject crossInstance;
    void Start() {
        Hide();
        GameObject sockObject = GameObject.Find("SocketIO");
        socket = sockObject.GetComponent<SocketIOComponent>();
    }

    public static int GetCoinsHeld() {
        return coins;
    }

    public void Handle() {
        if (coins == 0) {
            Show();
        }

        coins += 1;
    }

    private void Hide() {
        foreach (Transform child in transform) {
            child.gameObject.SetActive(false);
        }
        Destroy(crossInstance);
        //crossInstance = null;
    }

    private void SetChildColor(Transform t, Color c) {
        foreach (Transform child in t) {
            if (child.name.Equals("Tube01")) {
                child.GetComponent<MeshRenderer>().materials[0].color = c;
            }
        }
    }

    private void Show() {
        int idx = 0;
        foreach (Transform child in transform) {
            child.gameObject.SetActive(true);
            if (idx == 1) {
                if (!colorsInitialized) {
                    SetChildColor(child, Color.green);
                }
            }
            else {
                Opponent oppSet = Controller.opponents[idx == 0 ? 0 : 1];
                if (!Controller.permissibleIndividuals.Contains(oppSet.GetId())) {
                    GameObject inst = Instantiate(crossPrefab, GameObject.Find("TrackingSpace").transform);
                    inst.transform.position = child.position;
                    inst.transform.Translate(new Vector3(0, 0.203f, -0.134f));
                    //if (crossInstance != null) Destroy(crossInstance);
                    crossInstance = inst;
                }

                if (!colorsInitialized) {
                    SetChildColor(child, oppSet.GetColor());
                }
            }
            idx++;
        }

        colorsInitialized = true;
    }

    private Color GetBucketColor(Transform t) {
        foreach (Transform child in t) {
            if (child.name.Equals("Tube01")) {
                return child.GetComponent<MeshRenderer>().materials[0].color;
            }
        }

        return Controller.NULL_COLOR;
    }

    public void HandleClick(Transform t) {
        Color c = GetBucketColor(t);
        
        if (coins > 0) {
            if (c.Equals(Color.green)) {
                Controller.MyCoinsOwned++;
                socket.Emit("claim");
            }
            else {
                foreach (Opponent opp in Controller.opponents) {
                    if (opp.GetColor().Equals(c)) {
                        if (Controller.permissibleIndividuals.Contains(opp.GetId())) {
                            Dictionary<string, string> dict = new Dictionary<string, string>();
                            dict["id"] = opp.GetId();
                            socket.Emit("give", new JSONObject(dict));
                        }
                        else {
                            return;
                        }
                    }
                }
            }
            if (--coins == 0) {
                Hide();
            }
        }
    }

    void Update() {
        
    }
}
