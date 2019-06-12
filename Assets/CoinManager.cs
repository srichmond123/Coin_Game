using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OVRSimpleJSON;
using SocketIO;
using UnityEngine;

public class CoinManager : MonoBehaviour {
    // Start is called before the first frame update
    public GameObject coinPrefab;
    private List<GameObject> coins;
    private static SocketIOComponent socket;
    private Buckets buckets;
    private TerrainScript terrainScript;
    void Start() {
        coins = new List<GameObject>();
        GameObject sockObject = GameObject.Find("SocketIO");
        socket = sockObject.GetComponent<SocketIOComponent>();
        buckets = GameObject.Find("Buckets").GetComponent<Buckets>();
        terrainScript = GameObject.Find("Terrain").GetComponent<TerrainScript>();
        socket.On("coins", HandleCoins);
        socket.On("tellCollect", HandleOtherCollect); //Somebody else got a coin
        socket.On("newCoin", HandleNewCoin);
    }

    void HandleNewCoin(SocketIOEvent e) {
        string id = e.data["id"].str;
        Vector3 position = Controller.DeserializeVector3(e.data["position"]);
        int idx = (int) e.data["index"].n;
        GameObject inst = Instantiate(coinPrefab);
        position.y = terrainScript.transform.localPosition.y + 1.2f;
        inst.transform.localPosition = position + Vector3.up * terrainScript.GetHeightAt(position);;
        inst.GetComponent<Collider>().enabled = false;
        inst.GetComponent<Collider>().enabled = true;
        CoinScript cs = inst.GetComponent<CoinScript>();
        Color c = id == Controller.myId ? Color.green : Controller.GetOpponentById(id).GetColor();
        cs.SetColor(c);
        cs.SetId(id);
        cs.SetParent(this);
        cs.index = idx;
        cs.SetAlbedo(0f);
        coins[idx] = inst;
    }

    void HandleOtherCollect(SocketIOEvent e) {
        //TODO maybe store 2 other players' coins
        Dictionary<string, string> res = e.data.ToDictionary();
        int idx = int.Parse(res["index"]);
        Destroy(coins[idx]);
        coins[idx] = null;
        Controller.GetOpponentById(res["id"]).Score++;
        Controller.UpdateScore();
    }

    void HandleCoins(SocketIOEvent e) {
        /*
         * Instantiate each, assign each Coin object
         * an id (it will handle color internally)
         */
        foreach (GameObject o in coins) {
            Destroy(o);
        }
        coins.Clear(); // Could be 2nd or 3rd round
        foreach (string id in e.data.keys) {
            Color c = Controller.NULL_COLOR;
            if (!id.Equals(Controller.myId)) {
                foreach (Opponent opp in Controller.opponents) {
                    if (opp.GetId().Equals(id)) {
                        c = opp.GetColor();
                    }
                }
            }
            else {
                c = Color.green;
            }

            JSONObject arr = e.data[id];
            for (int i = 0; i < arr.Count; i++) {
                Vector3 pos = Controller.DeserializeVector3(arr[i]);
                GameObject inst = Instantiate(coinPrefab);
                pos.y = terrainScript.transform.localPosition.y + 1.2f;
                inst.transform.localPosition = pos + Vector3.up * terrainScript.GetHeightAt(pos);;
                inst.GetComponent<Collider>().enabled = false;
                inst.GetComponent<Collider>().enabled = true;
                if (id.Equals(Controller.myId)) {
                    inst.layer = LayerMask.NameToLayer("My Coins");
                }
                CoinScript cs = inst.GetComponent<CoinScript>();
                cs.SetColor(c);
                cs.SetId(id);
                cs.SetParent(this);
                cs.index = coins.Count; //For easy access on collision with prefab
                coins.Add(inst);
            }
        }
    }

    public void Collect(int index) { //User collects a coin
        JSONObject send = new JSONObject(JSONObject.Type.OBJECT);
        send.AddField("index", index);
        send.AddField("position", Controller.SerializeVector3(Controller.GetMyPosition()));
        socket.Emit("collect", send);
        Destroy(coins[index]);
        coins[index] = null;
        buckets.Handle();
        Controller.MyScore++;
        Controller.UpdateScore();
    }

    // Update is called once per frame
    void Update() {
        
    }
}
