using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
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

	public const float 
		MinRange = 35f,
		MaxRange = 200f,
		OwnRangeIncrease = 22f, 
		OtherRangeIncrease = 22f, 
		ConstDecrease = 2.0f, 
		InitialRange = 80f, 
		MaxSpeed = 4f, 
		SpeedIncrement = 2.5f,
		SpeedDecrement = 4f;

	private const int CountdownTimeMs = 10 * 1000;

	public static int Goal = 30; //Default value (referenced in tutorial before server tells clients goal)
	private static bool MulticolorBar => true;
	private const float HeightThreshold = 0.8f; //How high user can be above terrain Y
	private static Vector3 BarOrigin;
	
	//private const int ZeroScoreRight = 347;
	//private const int MaxScoreRight = -200;
	private const float MaxScale = 0.18f;

	public static bool DisableVr = false;

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

	private Transform blueBucketTransform, greenBucketTransform, redBucketTransform;
	//public GameObject arrowOfVirtue;
	public int _MyCoins, _OtherCoins;
	private static bool flying = false;
	private static bool slowingDown = false;
	private float currBoost = 1f;
	//private float boostTime = 0f;
	private float timeSpentSlowingDown = 0f;
	//private float oldBoost = -1f;
	public static Buckets buckets;
	private float luminosity = 0.2f;
	public static Light light;
	private static Transform _interfaceTransform, _centerEyeTransform;
	public static TextMeshPro scoreText, timeText, lobbyText, countdownText;
	public static Transform scoreBar, redBar, greenBar, blueBar, emptyBar;
	public static bool LightDecreasing = false;
	private TerrainScript terrainScript;
	private Boundaries boundaries;
	
	private static bool _inLobby = false;
	public static bool TutorialBoundarySet = false;
	private static bool _inCountdown = false;
	private static int _elapsedMs = 0;
	private static float _boundaryX, _boundaryZ, _boundarySlope;
	private static bool _boundaryBlockBelowLine;
	private static LoadingCircle _loadingCircle;
	private static int RoundNum = 0; //1, 2, or 3 for simplicity - Socket start emission will incr. this to 1 at first
	private static string PrevRoundScoreText = "";
	public static MeshRenderer LeaderBoard;

	private void Start() {
		if (DisableVr) {
			XRSettings.LoadDeviceByName("");
			XRSettings.enabled = false;
		}
		buckets = GameObject.Find("Buckets").GetComponent<Buckets>();
		friends = new List<Friend>();
		permissibleIndividuals = new List<string>();
		friends.Add(GameObject.Find("Player_1").GetComponent<Friend>());
		friends.Add(GameObject.Find("Player_2").GetComponent<Friend>());
		
		socket = GameObject.Find("SocketIO").GetComponent<SocketIOComponent>();

		socket.On("start", HandleStart);
		socket.On("update", HandleUpdate);
		socket.On("give", HandleGenerosity);
		socket.On("getOut", HandleRejection);
		socket.On("newConnection", HandleNewConnection);
		light = GameObject.Find("My Light").GetComponent<Light>();
		_interfaceTransform = transform; //Since there's only one of these this is fine
		_centerEyeTransform = GameObject.Find("CenterEyeAnchor").transform;
		scoreText = GameObject.Find("ScoreText").GetComponent<TextMeshPro>();
		timeText = GameObject.Find("TimeText").GetComponent<TextMeshPro>();
		lobbyText = GameObject.Find("LobbyText").GetComponent<TextMeshPro>();
		countdownText = GameObject.Find("CountdownText").GetComponent<TextMeshPro>();
		scoreBar = GameObject.Find("Bar").transform;
		redBar = GameObject.Find("RedBar").transform;
		blueBar = GameObject.Find("BlueBar").transform;
		greenBar = GameObject.Find("GreenBar").transform;
		emptyBar = GameObject.Find("EmptyBar").transform;
		LeaderBoard = GameObject.Find("LeaderBoard").GetComponent<MeshRenderer>();
		LeaderBoard.enabled = false;
		BarOrigin = greenBar.localPosition;
		
		terrainScript = GameObject.Find("Terrain").GetComponent<TerrainScript>();
		boundaries = GameObject.Find("Boundaries").GetComponent<Boundaries>();

		scoreText.enabled = false;
		timeText.enabled = false;
		lobbyText.enabled = false;
		buckets.HardSet(false); //Hide for tutorial

		_loadingCircle = GameObject.Find("LoadingCircle").GetComponent<LoadingCircle>();
		_loadingCircle.Set(false);
		countdownText.enabled = false;

		blueBucketTransform = GameObject.Find("bucket").transform;
		greenBucketTransform = GameObject.Find("bucket (1)").transform;
		redBucketTransform = GameObject.Find("bucket (2)").transform;
	}

	private void OnApplicationQuit() {
		if (socket.enabled) { //Means player is quitting early, must notify others
			socket.Emit("quit");
			socket.enabled = false;
		}
	}


	public static void ToggleLobby(bool inLobby) {
		_inLobby = inLobby;
		lobbyText.enabled = inLobby;
		_loadingCircle.Set(inLobby);
	}

	private void HandleRejection(SocketIOEvent e) {
		ToggleCountdown(true);
		socket.enabled = false;
		if (!e.data["quit"].b) {
			PrevRoundScoreText = "Your team finished round 3 in "
			                     + ParseMilliseconds(_elapsedMs - CountdownTimeMs) + ".\n\n" +
			                     "The game is over, thank you for your participation.";
		}
		else {
			ToggleLobby(false);
			PrevRoundScoreText = "Since one of your teammates has quit, the game is now over.\n\n" +
			                     "Thank you for your participation.";
		}

		countdownText.text = PrevRoundScoreText;
	}

	private void _displayCoins() {
		_MyCoins = MyCoinsOwned;
		_OtherCoins = OtherCoinsOwned;
	}

	private void HandleStart(SocketIOEvent e) {
		RoundNum++;
		Dictionary<string, string> res = e.data.ToDictionary();
		MyId = res["id"];
		Vector3 myPos = DeserializeVector3(e.data["position"]);
		myPos.y =
			terrainScript.GetHeightAt(myPos)
			+ terrainScript.transform.localPosition.y
			+ HeightThreshold + 0.1f;
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

		buckets.HardSet(true); //Invisible in lobby (might still be waiting on connections)
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
		
		ToggleLobby(false);
		ToggleCountdown(true);

		if (RoundNum > 1) { //Tell previous score in countdown waiting room:
			PrevRoundScoreText = "Your team finished round " + (RoundNum - 1) + " in "
			                     + ParseMilliseconds(_elapsedMs - CountdownTimeMs) + ".\n\n";
		}
	}

	private void HandleNewConnection(SocketIOEvent e) {
		int numLeft = (int) e.data["left"].f;
		if (_inLobby) {
			lobbyText.text = "We are waiting for " + 
                 numLeft + " more " + (numLeft == 1 ? "player" : "players");
		}
	}

	private void ToggleCountdown(bool state) {
		_inCountdown = state;
		timeText.enabled = !state;
		countdownText.enabled = state;
	}

	private void ShowGenerosity(Friend friend) {
		string to = friend.GetId();
		if (MyCoinsOwned == 0 || !permissibleIndividuals.Contains(to)) return;

		MyCoinsOwned--;
		Dictionary<string, string> dict = new Dictionary<string, string>();
		dict["id"] = to;
		socket.Emit("give", new JSONObject(dict));
	}

	private void HandleGenerosity(SocketIOEvent e) {
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
			//VirtueSignal(GetFriendById(data["from"]), GetFriendById(data["to"]));
			GetFriendById(data["to"]).OtherCoins++;
		}
	}
	
	[Obsolete("Map is way too big for this method to be useful")]
	private void VirtueSignal(Friend from, Friend to) {
		Vector3 fromVec = from.transform.localPosition;
		Vector3 toVec = to.transform.localPosition;
		GameObject inst = new GameObject(); //GameObject inst = Instantiate(arrowOfVirtue); 
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
		Vector3 scale = t.localScale;
		Vector3 position = t.localPosition;
		scale.x = (score * 1.0f / Goal) * MaxScale;
		position.x = BarOrigin.x + 5f * scale.x + translate;
		t.localPosition = position;
		t.localScale = scale;
		return scale.x * 10f;
	}

	private void HandleUpdate(SocketIOEvent e) {
		if (friends[0].GetId().Equals("")) { 
			//If this is the first update, assign ids:
			int ind = 0;
			foreach (string key in e.data["users"].keys) {
				if (!key.Equals(MyId)) {
					friends[ind++].SetId(key);
				}
			}
			buckets.Show();
			buckets.Hide();
		}
		else {
			foreach (Friend f in friends) {
				//f.AdjustTransform(e.data["users"], !setInitialPositions);
				//TODO uncomment above line
			}
			setInitialPositions = true;
		}
		
		_elapsedMs = (int) e.data["time"].f;
		if (_inCountdown && _elapsedMs >= CountdownTimeMs) ToggleCountdown(false);
        if (_inCountdown) {
            int remainder = (int) Mathf.Ceil((CountdownTimeMs - _elapsedMs) / 1000f);
            string text = "Round " + RoundNum + " starts in " + remainder 
                          + (remainder == 1 ? " second" : " seconds");
            
            countdownText.text = PrevRoundScoreText + text;
        } else {
            timeText.text = ParseMilliseconds(_elapsedMs - CountdownTimeMs);
		}

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
		return _interfaceTransform.localPosition;
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

	private void AdjustMyLight() {
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
	private void Update() {
		if (_inLobby || _inCountdown) {
			
		} else {
			AdjustMyLight();
			if (!DisableVr) {
				if (OVRInput.GetDown(OVRInput.RawButton.A)) {
					//A button pressed, right controller:
					//Fly(speed * Time.deltaTime);
					if (Tutorial.InTutorial && Tutorial.CurrStep >= Tutorial.ShowCoinsStep || !Tutorial.InTutorial) {
						bool __ = flying ? slowingDown = !slowingDown : flying = true;
					}
				}
				else if (OVRInput.GetUp(OVRInput.RawButton.A)) {
					if (Tutorial.InTutorial && Tutorial.CurrStep >= Tutorial.ShowCoinsStep || !Tutorial.InTutorial) {
						//bool __ = flying ? slowingDown = !slowingDown : flying = true;
						slowingDown = true;
					}
				}

				if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger)) {
					if (Tutorial.InTutorial && Tutorial.CurrStep <= Tutorial.ShowBucketsStep) {
						Tutorial.TellClicked();
					}
					else {
						buckets.HandleClick(Color.red);
					}
				}
				else if (OVRInput.GetDown(OVRInput.RawButton.B)) buckets.HandleClick(Color.green);
				else if (OVRInput.GetDown(OVRInput.RawButton.LIndexTrigger)) buckets.HandleClick(Color.blue);
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

				if (Input.GetKeyDown(KeyCode.S)) {
					if (Tutorial.InTutorial && Tutorial.CurrStep >= Tutorial.ShowCoinsStep || !Tutorial.InTutorial) {
						bool __ = flying ? slowingDown = !slowingDown : flying = true;
					}
				}
				/* //Old system for clicking on the buckets directly
				if (Input.GetMouseButtonDown(0)) {
					Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
					RaycastHit hit;

					if (Physics.Raycast(ray, out hit)) {
						if (hit.transform.tag.Equals("Not Me")) {
							//ShowGenerosity(hit.transform.GetComponent<Friend>());
						}
						else if (hit.transform.tag.Equals("Bucket")) {
							//buckets.HandleClick(hit.transform);
						}
					}
					else {
						if (Tutorial.InTutorial) {
							Tutorial.TellClicked();
						}
					}
				}
				*/
				if (Input.GetKeyDown(KeyCode.E)) {
					if (Tutorial.InTutorial && Tutorial.CurrStep <= Tutorial.ShowBucketsStep) {
						Tutorial.TellClicked();
					}
					else {
						buckets.HandleClick(Color.red);
					}
				}
				else if (Input.GetKeyDown(KeyCode.W)) buckets.HandleClick(Color.green);
				else if (Input.GetKeyDown(KeyCode.Q)) buckets.HandleClick(Color.blue);
			}

			if (flying) {
				Fly();
			}
		}
	}

	public static void SetTutorialBoundary(Vector3 center, Vector3 looking, Vector3 right) {
		//In tutorial, boundary step has been reached, so don't allow flight beyond that boundary
		//(only in tutorial) - find line in x-z plane they can't cross:
		Vector3 looking2D = looking;
		looking2D.y = 0;
		looking2D.Normalize();
		center -= looking2D * Boundaries.Buffer;
		_boundarySlope = right.z / right.x;
		_boundaryX = center.x;
		_boundaryZ = center.z;
		_boundaryBlockBelowLine = looking.z < 0;
		TutorialBoundarySet = true;
		//z - z1 = (slope) * (x - x1)
	}
	
	public static void LogMessage(string s) {
		JSONObject msg = new JSONObject(JSONObject.Type.OBJECT);
		msg.AddField("Message", s);
		socket.Emit("log", msg);
	}

	private void Fly() {
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
			+ terrainWorldPosition.y + HeightThreshold) {
			//Colliding with terrain, so forward vec must be clamped
			//(minimum of vec orthogonal to terrain and curr vec, assuming they might be moving away
			//and we don't want to lock their position):
			float nextY = terrainScript.GetHeightAt(t.localPosition + forward * incr) 
						  + terrainWorldPosition.y + HeightThreshold;
			Vector3 nextPos = localPosition + forward * incr;
			nextPos.y = Mathf.Max(nextY, nextPos.y);
			t.localPosition = nextPos;
		}
		else {
			t.localPosition += forward * incr;
		}

		Vector3 currPosition = t.localPosition;
		Vector3 modifyPosition = boundaries.Outside(currPosition); //Will modify position if out of bounds
		if (!currPosition.Equals(modifyPosition)) {
			t.localPosition = modifyPosition;
		}

		if (TutorialBoundarySet) {
			float myX = currPosition.x;
			float myZ = currPosition.z;
			float lineZ = _boundarySlope * (myX - _boundaryX) + _boundaryZ;
			if (_boundaryBlockBelowLine) myZ = Mathf.Max(lineZ, myZ);
			else myZ = Mathf.Min(lineZ, myZ);
			currPosition.z = myZ;
			t.localPosition = currPosition;
		}
	}

	public static Quaternion GetMyRotation() { //Left eye camera if in VR, otherwise, whole camera rig's rotation:
		if (!DisableVr) {
			return _centerEyeTransform.localRotation;
			//return _interfaceTransform.GetChild(0).GetChild(0).localRotation;
		}
		return _interfaceTransform.localRotation;
	}

	public static Vector3 GetMyForward() {
		return _centerEyeTransform.forward;
		//return _interfaceTransform.GetChild(0).GetChild(0).forward;
	}

	public static Vector3 GetMyRight() {
		return _centerEyeTransform.right;
	}
}
