using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
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
	private AudioSource coinSound;
	void Start() {
		coins = new List<GameObject>();
		GameObject sockObject = GameObject.Find("SocketIO");
		socket = sockObject.GetComponent<SocketIOComponent>();
		buckets = GameObject.Find("Buckets").GetComponent<Buckets>();
		terrainScript = GameObject.Find("Terrain").GetComponent<TerrainScript>();
		socket.On("coins", HandleCoins);
		socket.On("tellCollect", HandleOtherCollect); //Somebody else got a coin
		socket.On("newCoin", HandleNewCoin);
        coinSound = GetComponentInChildren<AudioSource>();
	}

	[Obsolete("Only use this method in the tutorial")]
	public void AppendToList(GameObject coin) {
		coin.GetComponent<Coin>().index = coins.Count;
		coins.Add(coin);
	}
	

	[Obsolete("Only use this method in the tutorial")]
	public Vector3 GetGreenPosition() {
		//Find first (in tutorial only) green, return pos vec:
		foreach (GameObject c in coins) {
			if (!c) continue;
			if (Buckets.CompareRGB(c.GetComponent<Coin>().GetColor(), Color.green)) {
				return c.transform.localPosition;
			}
		}

		return Vector3.zero;
	}

	void HandleNewCoin(SocketIOEvent e) {
		string id = e.data["id"].str;
		Vector3 position = Interface.DeserializeVector3(e.data["position"]);
		int idx = (int) e.data["index"].n;
		GameObject inst = Instantiate(coinPrefab);
		position.y += terrainScript.transform.localPosition.y + 1.2f;
		inst.transform.localPosition = position + Vector3.up * terrainScript.GetHeightAt(position);
		inst.GetComponent<Collider>().enabled = false;
		inst.GetComponent<Collider>().enabled = true;
		Coin cs = inst.GetComponent<Coin>();
		Color c = id == Interface.MyId ? Color.green : Interface.GetFriendById(id).GetColor();
		cs.SetColor(c);
		cs.SetId(id);
		cs.SetParent(this);
		cs.index = idx;
		cs.SetAlbedo(0f);
		coins[idx] = inst;
	}

	void HandleOtherCollect(SocketIOEvent e) {
		Dictionary<string, string> res = e.data.ToDictionary();
		int idx = int.Parse(res["index"]);
		Destroy(coins[idx]);
		coins[idx] = null;
		Interface.GetFriendById(res["id"]).Score++;
		Interface.UpdateScore();
	}


	public void _destroyAll() {
		foreach (GameObject o in coins) {
			Destroy(o);
		}
		coins.Clear();
	}

	void HandleCoins(SocketIOEvent e) {
		/*
		 * Instantiate each, assign each Coin object
		 * an id (it will handle color internally)
		 */
		_destroyAll();
		foreach (string id in e.data.keys) {
			Color c = Interface.NullColor;
			if (!id.Equals(Interface.MyId)) {
				foreach (Friend friend in Interface.friends) {
					if (friend.GetId().Equals(id)) {
						c = friend.GetColor();
					}
				}
			}
			else {
				c = Color.green;
			}

			JSONObject arr = e.data[id];
			for (int i = 0; i < arr.Count; i++) {
				Vector3 pos = Interface.DeserializeVector3(arr[i]);
				GameObject inst = Instantiate(coinPrefab);
				pos.y += terrainScript.transform.localPosition.y + 1.2f;
				inst.transform.localPosition = pos + Vector3.up * terrainScript.GetHeightAt(pos);
				inst.GetComponent<Collider>().enabled = false;
				inst.GetComponent<Collider>().enabled = true;
				if (id.Equals(Interface.MyId)) {
					inst.layer = LayerMask.NameToLayer("My Coins");
				}
				Coin cs = inst.GetComponent<Coin>();
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
		send.AddField("position", Interface.SerializeVector3(Interface.GetMyPosition()));
		socket.Emit("collect", send);
		coinSound.transform.position = coins[index].transform.position;
		coinSound.Play();
		Destroy(coins[index]);
		coins[index] = null;
		buckets.Handle();
		Interface.MyScore++;
		Interface.UpdateScore();
	}

	// Update is called once per frame
	void Update() {
		
	}
}
