using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.UI;
using Vector3 = UnityEngine.Vector3;
using SocketIO;
using TMPro;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;


public class Interface : MonoBehaviour {
	public static Color NullColor => Color.magenta;

	public static float MinRange => 10f;
	public static float MaxRange => 200f; //Maybe infinite
	public static float OwnRangeIncrease => 32f;
	public static float OtherRangeIncrease => 32f;
	public static float ConstDecrease => 2.0f; // ConstDecrease * Time.deltaTime (per frame)
	public static float InitialRange => 80f;
	public static float MaxSpeed => 4f;
	public static float SpeedIncrement => 2.5f;
	public static float SpeedDecrement => 4f; //Stop faster
	public static int Goal = 30; //Default value (referenced in tutorial before server tells clients goal)
	public static bool MulticolorBar => true;
	static float heightThreshold => 0.8f; //How high user can be above terrain Y
	public static Vector3 BarOrigin = Vector3.zero;
	
	//private const int ZeroScoreRight = 347;
	//private const int MaxScoreRight = -200;
	private const float MaxScale = 0.18f;

	public bool disableVR;

	private float speed = 4f;
	public static SocketIOComponent socket;
	private const string BlankId = "BLANK";
	public static string MyId = BlankId;
	private bool setInitialPositions = false;
	public static List<Friend> friends;
	public static List<string> permissibleIndividuals;
	public static int MyCoinsOwned = 0, OtherCoinsOwned = 0;
	public static int MyScore = 0; //Score is directly related to bumping into a coin, doesn't have to do with sharing.
	private static Vector3 MapScale, MapOrigin;

	public GameObject arrowOfVirtue;
	public int _MyCoins, _OtherCoins;
	public static bool flying = false;
	private static bool slowingDown = false;
	private float currBoost = 1f;
	//private float boostTime = 0f;
	private float timeSpentSlowingDown = 0f;
	//private float oldBoost = -1f;
	public static Buckets buckets;
	private float luminosity = 0.2f;
	public static Light light;
	public static Transform interfaceTransform;
	public static TextMeshPro scoreText, timeText;
	private static Transform scoreBar, redBar, greenBar, blueBar, emptyBar;
	public static bool LightDecreasing = false;
	private TerrainScript terrainScript;
	private Boundaries boundaries;

	void Start() {
		if (disableVR) {
			XRSettings.LoadDeviceByName("");
			XRSettings.enabled = false;
		}
		else {
			//
		}
		buckets = GameObject.Find("Buckets").GetComponent<Buckets>();
		friends = new List<Friend>();
		permissibleIndividuals = new List<string>();
		friends.Add(GameObject.Find("Player_1").GetComponent<Friend>());
		friends.Add(GameObject.Find("Player_2").GetComponent<Friend>());
		
		socket = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();

		socket.On("start", HandleStart);
		socket.On("update", OnSocketUpdate);
		socket.On("give", HandleGenerosity);
		socket.On("getOut", HandleRejection);
		light = GameObject.Find("My Light").GetComponent<Light>();
		interfaceTransform = gameObject.transform;
		scoreText = GameObject.Find("ScoreText").GetComponent<TextMeshPro>();
		timeText = GameObject.Find("TimeText").GetComponent<TextMeshPro>();
		scoreBar = GameObject.Find("Bar").transform;
		redBar = GameObject.Find("RedBar").transform;
		blueBar = GameObject.Find("BlueBar").transform;
		greenBar = GameObject.Find("GreenBar").transform;
		emptyBar = GameObject.Find("EmptyBar").transform;
		BarOrigin = greenBar.localPosition;
		
		terrainScript = GameObject.Find("Terrain").GetComponent<TerrainScript>();
		boundaries = GameObject.Find("Boundaries").GetComponent<Boundaries>();

		scoreText.enabled = false;
		timeText.enabled = false;
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
		MyId = res["id"];
		Vector3 myPos = DeserializeVector3(e.data["position"]);
		myPos.y =
			terrainScript.GetHeightAt(myPos)
			+ terrainScript.transform.localPosition.y
			+ heightThreshold + 0.1f;
		transform.localPosition = myPos;
		JSONObject topologyArray = e.data["topology"][MyId];
		Goal = (int) e.data["goal"].n;
		permissibleIndividuals.Clear();
		for (int i = 0; i < topologyArray.Count; i++) {
			permissibleIndividuals.Add(topologyArray[i].str);
		}

		MyCoinsOwned = 0;
		OtherCoinsOwned = 0;
		MyScore = 0;
		foreach (Friend f in friends) {
			f.Score = 0;
			f.OtherCoins = 0;
			f.MyCoins = 0;
		}
		buckets.Hide();
		flying = false;
		slowingDown = false;

		//TODO initial rotation (random or facing forward always)
		light.range = InitialRange;
		setInitialPositions = false;
		UpdateScore();

		MapOrigin = DeserializeVector3(e.data["origin"]);
		MapScale = DeserializeVector3(e.data["scale"]);
		boundaries.Set(MapOrigin, MapScale);
		boundaries.SetScenery();
		timeText.text = " 0:00";
	}

	void ShowGenerosity(Friend friend) {
		string to = friend.GetId();
		if (MyCoinsOwned == 0 || !permissibleIndividuals.Contains(to)) return;

		MyCoinsOwned--;
		Dictionary<string, string> dict = new Dictionary<string, string>();
		dict["id"] = to;
		socket.Emit("give", new JSONObject(dict));
	}

	void HandleGenerosity(SocketIOEvent e) {
		/*
		 * Animate from friend obj with id e.data[from] to e.data[to],
		 * or if e.data[to] == myId, animate from e.data[from] to me:
		 */
		Dictionary<string, string> data = e.data.ToDictionary();
		GetFriendById(data["from"]).MyCoins--;
		if (data["to"].Equals(MyId)) {
			OtherCoinsOwned++;
			//TODO Animation of receiving a coin
		}
		else {
			//VirtueSignal(GetFriendById(data["from"]), GetFriendById(data["to"])); //TODO uncomment virtue signaling?
			GetFriendById(data["to"]).OtherCoins++;
		}
	}
	
	[Obsolete("Map is way too big for this method to be useful")]
	void VirtueSignal(Friend from, Friend to) {
		Vector3 fromVec = from.transform.localPosition;
		Vector3 toVec = to.transform.localPosition;
		GameObject inst = Instantiate(arrowOfVirtue); 
		inst.transform.LookAt(toVec - fromVec);
		Vector3 currScale = inst.transform.localScale;
		currScale.z = Vector3.Distance(fromVec, toVec) - to.transform.localScale.x * 1.5f; // Width of player objects
		inst.transform.localScale = currScale;
		inst.transform.localPosition = toVec;
		Destroy(inst, 1.0f);
	}
	
	
	public static Friend GetFriendById(string id) {
		foreach (Friend f in friends) {
			if (f.GetId().Equals(id)) {
				return f;
			}
		}

		throw new Exception("Friend " + id + " not found.");
	}

	public static void UpdateScore() {
		int blueScore = 0, redScore = 0, greenScore = MyScore;
		foreach (Friend f in friends) {
			if (f.GetColor().Equals(Color.red)) {
				redScore = f.Score;
			}
			else {
				blueScore = f.Score;
			}
		}

		if (Tutorial.InTutorial) { //Override whatever values came from above for example values:
			redScore = Tutorial.RedScore;
			blueScore = Tutorial.BlueScore;
			greenScore = Tutorial.MyScore;
		}

		int scoreSum = redScore + blueScore + greenScore;

		scoreText.text = scoreSum + "/" + Goal;
		if (!MulticolorBar) {
			ModifyBarTransform(scoreBar, scoreSum, 0f);
		}
		else {
			float blueWid = ModifyBarTransform(blueBar, blueScore, 0);
			float redWid = ModifyBarTransform(redBar, redScore, blueWid);
			float greenWid = ModifyBarTransform(greenBar, greenScore, blueWid + redWid);
			ModifyBarTransform(emptyBar, Goal - scoreSum, blueWid + redWid + greenWid);
		}
	}

	static float ModifyBarTransform(Transform t, int score, float translate) {
		/*
		Vector2 offsetMax = rectTransform.offsetMax;
		Vector2 offsetMin = rectTransform.offsetMin;
		float oldX = offsetMax.x;
		offsetMax.x = -(score * 1.0f / Goal * (MaxScoreRight - ZeroScoreRight) + ZeroScoreRight);
		//offsetMin.x = minX;
		rectTransform.offsetMax = offsetMax;
		rectTransform.offsetMin = offsetMin;
		float width = offsetMax.x - oldX;
		Vector3 position = rectTransform.localPosition;
		position.x = -66.5f; // + translate;
		rectTransform.localPosition = position;
		*/
		Vector3 scale = t.localScale;
		Vector3 position = t.localPosition;
		scale.x = (score * 1.0f / Goal) * MaxScale;
		position.x = BarOrigin.x + 5f * scale.x + translate;
		t.localPosition = position;
		t.localScale = scale;
		return scale.x * 10f;
	}

	void OnSocketUpdate(SocketIOEvent e) {
		if (friends[0].GetId().Equals("")) { 
			//If this is the first update, assign ids:
			int ind = 0;
			foreach (string key in e.data["users"].keys) {
				if (!key.Equals(MyId)) {
					friends[ind++].SetId(key);
				}
			}
		}
		else {
			foreach (Friend f in friends) {
				//f.AdjustTransform(e.data["users"], !setInitialPositions);
				//TODO uncomment above line
			}
			setInitialPositions = true;
		}

		timeText.text = ParseMilliseconds((int) e.data["time"].f);

		JSONObject send = new JSONObject(JSONObject.Type.OBJECT);
		send.AddField("position", SerializeVector3(transform.localPosition));
		send.AddField("rotation", SerializeQuaternion(GetMyRotation()));
		send.AddField("range", light.range);
		send.AddField("flying", flying);
		socket.Emit("update", send);
	}

	public static string ParseMilliseconds(int time_ms) {
		int seconds_total = Mathf.FloorToInt(time_ms / 1000f);
		int minutes = Mathf.FloorToInt(seconds_total / 60f);
		int seconds = seconds_total % 60;
		return (minutes > 9 ? "" : " ") + minutes + ":"
		       + (seconds > 9 ? "" : "0") + seconds;
	}


	public static Vector3 GetMyPosition() {
		return interfaceTransform.localPosition;
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
		if (LightDecreasing) {
			if (light.range > MinRange) {
				light.range -= ConstDecrease * Time.deltaTime;
			}
		}

		if (MyCoinsOwned > 0) {
			MyCoinsOwned--;
			light.range += OwnRangeIncrease;
		}
		if (OtherCoinsOwned > 0) {
			OtherCoinsOwned--;
			light.range += OtherRangeIncrease;
		}

		RenderSettings.fogDensity = Mathf.Clamp(5f / light.range, 0.03f, 0.2f);
		//TODO Max range
		buckets.UpdateHealth();
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
			//_displayCoins();
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
				if (Tutorial.InTutorial && Tutorial.CurrStep >= Tutorial.ShowCoinsStep || !Tutorial.InTutorial) {
					bool __ = flying ? slowingDown = !slowingDown : flying = true;
				}
			}

			if (Input.GetMouseButtonDown(0)) {
				if (!Tutorial.InTutorial || 
				    Tutorial.CurrStep != Tutorial.ShowBucketsStep &&
				    Tutorial.CurrStep != Tutorial.ShowBucketsStep + 1) {
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					RaycastHit hit;

					if (Physics.Raycast(ray, out hit)) {
						if (hit.transform.tag.Equals("Not Me")) {
							//ShowGenerosity(hit.transform.GetComponent<Friend>());
						}
						else if (hit.transform.tag.Equals("Bucket")) {
							buckets.HandleClick(hit.transform);
						}
					}
				}
			}
		}

		if (flying) {
			Fly();
		}
	}
	
	public static void LogMessage(string s) {
		JSONObject msg = new JSONObject(JSONObject.Type.OBJECT);
		msg.AddField("Message", s);
		socket.Emit("log", msg);
	}

	void Fly() {
		if (!slowingDown) {
			if (speed < MaxSpeed) {
				speed += SpeedIncrement * Time.deltaTime;
			}
		}
		else {
			if (speed > 0f) {
				speed -= SpeedDecrement * Time.deltaTime;
				if (speed <= 0f) {
					speed = 0f;
					flying = false;
					slowingDown = false;
				}
			}
		}

		float incr = speed * Time.deltaTime;
		Transform t = transform;
		Vector3 forward = t.GetChild(0).GetChild(0).forward;
		Vector3 localPosition = t.localPosition;
		Vector3 terrainWorldPosition = terrainScript.transform.localPosition;
		if (localPosition.y 
			<= terrainScript.GetHeightAt(localPosition) 
			+ terrainWorldPosition.y + heightThreshold) {
			//Colliding with terrain, so forward vec must be clamped
			//(minimum of vec orthogonal to terrain and curr vec, assuming they might be moving away
			//and we don't want to lock their position):
			float nextY = terrainScript.GetHeightAt(t.localPosition + forward * incr) 
						  + terrainWorldPosition.y + heightThreshold;
			Vector3 nextPos = localPosition + forward * incr;
			nextPos.y = Mathf.Max(nextY, nextPos.y);
			t.localPosition = nextPos;
		}
		else {
			t.localPosition += forward * incr;
		}

		Vector3 currPosition = t.localPosition;
		Vector3 modPosition = boundaries.Outside(currPosition);
		if (!currPosition.Equals(modPosition)) {
			t.localPosition = modPosition;
		}
	}

	public static Quaternion GetMyRotation() { //Left eye camera if in VR, otherwise, whole camera rig's rotation:
		if (!interfaceTransform.GetComponent<Interface>().disableVR) {
			return interfaceTransform.GetChild(0).GetChild(0).localRotation;
		}
		return interfaceTransform.localRotation;
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
