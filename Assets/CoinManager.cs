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
    void Start() {
        coins = new List<GameObject>();
        GameObject sockObject = GameObject.Find("SocketIO");
        socket = sockObject.GetComponent<SocketIOComponent>();
        socket.On("coins", HandleCoins);
        socket.On("tellCollect", HandleOtherCollect); //Somebody else got a coin
    }

    void HandleOtherCollect(SocketIOEvent e) {
        //TODO maybe store 2 other players' coins
        Dictionary<string, string> res = e.data.ToDictionary();
        int idx = int.Parse(res["index"]);
        Destroy(coins[idx]);
        coins[idx] = null;
    }

    void HandleCoins(SocketIOEvent e) {
        /*
         * Instantiate each, assign each Coin object
         * an id (it will handle color internally)
         */
        coins.Clear(); // Could be 2nd or 3rd round
        foreach (string id in e.data.keys) {
            Color c = Color.magenta;
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
                inst.transform.localPosition = pos;
                inst.GetComponent<Collider>().enabled = false;
                inst.GetComponent<Collider>().enabled = true;
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
        socket.Emit("collect", send);
        Destroy(coins[index]);
        coins[index] = null;
        Controller.MyCoinsOwned++;
    }

    // Update is called once per frame
    void Update() {
        
    }
}
