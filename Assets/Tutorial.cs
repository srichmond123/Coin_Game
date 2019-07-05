using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using OVR.OpenVR;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#pragma warning disable 618
public class Tutorial : MonoBehaviour {
	public static bool InTutorial = true;

	public const int
		Welcome = 0,
		ShowFriendsStep = 1,
		ShowScoreTextStep = 2,
		ShowScoreBarStep = 3,
		ExplainScoreBarStep = 4,
		ShowTimerStep = 5,
		ShowCoinRulesStep = 6,
		ShowCoinsStep = 7,
		ShowBucketsStep = 8,
		TryBucketsStep = 9,
		CollectSecondTime = 10,
		ShareBlue = 11,
		CollectThirdTime = 12,
		ShareRed = 13,
		TopologyExplanation = 14,
		/*
		TopologyExample1 = 15,
		TopologyExample2 = 16,
		BoundsExplanation = 17,
		LeaderboardExplanation = 18,
		EndStep = 19;
		*/
		BoundsExplanation = 15,
		LeaderboardExplanation = 16,
		EndStep = 17;

	private const float MinRange = 60f; //Higher than Interface.MinRange for tutorial

	private static TextMeshPro HelpText;
	public static int CurrStep = 0;
	//private static ControllerHelper controllerHelper;

	public GameObject redWhalePrefab, blueWhalePrefab, boundsPrefab;
	private static GameObject _redWhalePrefab, _blueWhalePrefab, _boundsPrefab;
	private static List<GameObject> instances;
	private static GameObject coinPrefab, helpArrow, dirArrow;
	private static bool _dirArrowVisible = false;
	private static TerrainScript terrainScript;
	private static CoinManager coinManager;
	private static Transform myTransform;

	private static Vector3 arrowPositionScoreText => new Vector3(0.515f, 0.783f, 1.557f);
	private static Vector3 arrowPositionScoreBar => new Vector3(0.602f, 0.855f, 1.557f);
	private static Vector3 arrowPositionTimer => new Vector3(0.529f, 0.475f, 1.557f);
	private static float arrowXRotationScoreText => 0f;
	private static float arrowXRotationScoreBar => -45f;
	private static float arrowXRotationTimer => arrowXRotationScoreText;

	private static GameObject
		RightTriggerArrow,
		RightTriggerHighlight,
		LeftTriggerArrow,
		LeftTriggerHighlight,
		AArrow,
		BArrow,
		BHighlight; /*
					 * 3 buttons (left trig, right trig, B) for giving coins will be color-highlighted.
					 * The fly button (A) just needs an arrow
					 */

	public static int MyScore = 0, RedScore = 0, BlueScore = 0;
	private static float RedRange = 100f;
	private static float BlueRange = 60f;
	private static float timeCount = -1f;
	private static string currTimeStr = " 0:00";
	void Start() {
		HelpText = GetComponentInChildren<TextMeshPro>();
		HelpText.text = StepTexts[CurrStep];
		
		//controllerHelper = GetComponentInChildren<ControllerHelper>();
		//controllerHelper.ShowRightTrigger();
		
		instances = new List<GameObject>(); //To collect instantiated prefabs and destroy later
		coinManager = GameObject.Find("I manage coins").GetComponent<CoinManager>();
		coinPrefab = coinManager.coinPrefab;
		terrainScript = GameObject.Find("Terrain").GetComponent<TerrainScript>();
		_redWhalePrefab = redWhalePrefab;
		_blueWhalePrefab = blueWhalePrefab;
		_boundsPrefab = boundsPrefab;
		myTransform = transform; //OK since Tutorial.cs isn't going to be instantiated many times
		helpArrow = GameObject.Find("HelpArrow");
		helpArrow.GetComponent<MeshRenderer>().enabled = false;
		dirArrow = GameObject.Find("DirArrow");
		dirArrow.GetComponentInChildren<MeshRenderer>().enabled = false;

		GameObject rightAnchor = GameObject.Find("RightHandAnchor/RightControllerAnchor");
		GameObject leftAnchor = GameObject.Find("LeftHandAnchor/LeftControllerAnchor");
		foreach (Transform child in rightAnchor.transform) {
			switch (child.name) {
				case "BButtonHighlight":
					BHighlight = child.gameObject;
					BHighlight.SetActive(false);
					break;
				case "BButtonArrow":
					BArrow = child.gameObject;
					BArrow.SetActive(false);
					break;
				case "RightTriggerHighlight":
					RightTriggerHighlight = child.gameObject;
					RightTriggerHighlight.SetActive(false);
					break;
				case "RightTriggerArrow":
					RightTriggerArrow = child.gameObject;
					break;
				case "AButtonArrow":
					AArrow = child.gameObject;
					AArrow.SetActive(false);
					break;
			}
		}

		foreach (Transform child in leftAnchor.transform) {
			switch (child.name) {
				case "LeftTriggerHighlight":
					LeftTriggerHighlight = child.gameObject;
					LeftTriggerHighlight.SetActive(false);
					break;
				case "LeftTriggerArrow":
					LeftTriggerArrow = child.gameObject;
					LeftTriggerArrow.SetActive(false);
					break;
			}
		}
	}

	private static void ToggleDirArrowVisibility(bool b) {
		if (b == _dirArrowVisible) return;
		_dirArrowVisible = b;
		dirArrow.GetComponentInChildren<MeshRenderer>().enabled = b;
	}

	public static void TellClicked() {
		if (CurrStep < ShowCoinsStep || CurrStep == ShowBucketsStep || CurrStep >= TopologyExplanation) {
			NextStep();
		}
	}

	// Update is called once per frame
	void Update() {
		if (InTutorial && CurrStep < EndStep) {
			if (CurrStep >= ShowBucketsStep) {
				float decr = CurrStep == ShowBucketsStep ? 17f : Interface.ConstDecrease;
				Interface.light.range = Mathf.Max(MinRange, Interface.light.range - decr * Time.deltaTime);
				RedRange = Mathf.Max(MinRange + 35f, RedRange - decr * Time.deltaTime);
				BlueRange = Mathf.Max(MinRange + 35f, BlueRange - decr * Time.deltaTime);
			}

			if (Input.GetKeyDown(KeyCode.Space)) { //shortcut
				EndTutorial();
			}
			
			if (Input.GetKeyDown(KeyCode.R)){
				StartOver();
			}

			if (timeCount >= 0f) {
				timeCount += Time.deltaTime;
				currTimeStr = Interface.ParseMilliseconds((int) (timeCount * 1000));
				if (!Interface.timeText.text.Equals(currTimeStr)) {
					Interface.timeText.text = currTimeStr;
				}
			}

			if (CurrStep == ShowCoinsStep || CurrStep == CollectSecondTime || CurrStep == CollectThirdTime) {
				//Show arrow pointing to coin if they miss it:
				float fov = Interface.GetFieldOfView();
				Vector3 coinPos = coinManager.GetGreenPosition();
				if (!coinPos.Equals(Vector3.zero)) {
					Vector3 myPos = Interface.GetMyPosition();
					Vector3 myDirVec = Interface.GetMyForward();
					Vector3 coinDirVec = coinPos - myPos;
					if (Vector3.Angle(coinDirVec, myDirVec) > fov / 2f) {
						ToggleDirArrowVisibility(true);
						dirArrow.transform.LookAt(coinPos);
						/*
						Vector3 eulerAngles = dirArrow.transform.localEulerAngles;
						eulerAngles.x += 180f;
						eulerAngles.z = 90f;
						dirArrow.transform.localEulerAngles = eulerAngles;
						*/
					}
					else {
						ToggleDirArrowVisibility(false);
					}
				}
			}
			else {
				ToggleDirArrowVisibility(false);
			}
		}
	}

	private static void SetArrowPositionAndRotation(Vector3 position, float xRotation) {
		helpArrow.transform.localPosition = position;
		Vector3 eulerAngles = helpArrow.transform.localEulerAngles;
		eulerAngles.x = xRotation;
		helpArrow.transform.localEulerAngles = eulerAngles;
	}

	public static void HandleBucketClick(Color col) { //Handle tutorial bucket behavior internally:
		switch (CurrStep) {
			case ShowBucketsStep: 
			case TopologyExplanation: 
			//case TopologyExample1: 
			//case TopologyExample2:
			case BoundsExplanation:
			case LeaderboardExplanation:
			case EndStep: {
				NextStep();
				break;
			}
			case TryBucketsStep: {
				if (Buckets.CompareRGB(Color.green, col)) {
					Interface.buckets.PlaySound();
					Interface.buckets.Hide();
					Interface.MyCoinsOwned += 1;
					NextStep();
				}
				break;
			}

			case ShareBlue: {
				if (Buckets.CompareRGB(Color.blue, col)) {
					Interface.buckets.PlaySound();
					Interface.buckets.Hide();
					BlueRange += Interface.OtherRangeIncrease;
					NextStep();
				}
				break;
			}

			case ShareRed: {
				if (Buckets.CompareRGB(Color.red, col)) {
					Interface.buckets.PlaySound();
					Interface.buckets.Hide();
					RedRange += Interface.OtherRangeIncrease;
					NextStep();
				}
				break;
			}
		}
	}

	public static float GetFriendRange(Color c) {
		return Buckets.CompareRGB(c, Color.blue) ? BlueRange / 50f : RedRange / 50f;
	}

	private static void StartOver() {
		DestroyAllInstances();
		Interface.light.range = Interface.InitialRange;
		coinManager._destroyAll();
		RightTriggerArrow.SetActive(true);
		RightTriggerHighlight.SetActive(false);
		LeftTriggerArrow.SetActive(false);
		LeftTriggerHighlight.SetActive(false);
		AArrow.SetActive(false);
		BArrow.SetActive(false);
		BHighlight.SetActive(false);
		Interface.scoreText.enabled = false;
		Interface.timeText.enabled = false;
		MyScore = RedScore = BlueScore = 0;
		Vector3 flatX = new Vector3(0f, 1f, 1f); //To easily flatten score bars on X (scale) axis
		Interface.emptyBar.localScale.Scale(flatX);
		Interface.greenBar.localScale.Scale(flatX);
		Interface.redBar.localScale.Scale(flatX);
		Interface.blueBar.localScale.Scale(flatX);
		helpArrow.GetComponent<MeshRenderer>().enabled = false;
		Interface.buckets.HardSet(false);
		Interface.buckets.Hide();
		CurrStep = 0;
		HelpText.text = StepTexts[CurrStep];
	}
	
	public static void NextStep() {
		HelpText.text = StepTexts[++CurrStep];
		switch (CurrStep) {
			case ShowFriendsStep: {
				SpawnWhales();
				break;
			}
			
			case ShowScoreTextStep: {
				Interface.scoreText.enabled = true;
				helpArrow.GetComponent<MeshRenderer>().enabled = true;
				SetArrowPositionAndRotation(arrowPositionScoreText, arrowXRotationScoreText);
				DestroyAllInstances();
				break;
			}

			case ShowScoreBarStep: {
				MyScore = BlueScore = RedScore = 0;
				Interface.UpdateScore();
				SetArrowPositionAndRotation(arrowPositionScoreBar, arrowXRotationScoreBar);
				break;
			}

			case ExplainScoreBarStep: {
				MyScore = 4;
				BlueScore = 1;
				RedScore = 0;
				Interface.UpdateScore();
				break;
			}

			case ShowTimerStep: {
				MyScore = BlueScore = RedScore = 0;
				Interface.UpdateScore();
				Interface.timeText.enabled = true;
				SetArrowPositionAndRotation(arrowPositionTimer, arrowXRotationTimer);
				timeCount = 0f;
				break;
			}

			case ShowCoinRulesStep: {
				helpArrow.GetComponent<MeshRenderer>().enabled = false;
				SpawnCoin(Color.red, -2.5f);
				SpawnCoin(Color.green, 0);
				SpawnCoin(Color.blue, 4.5f);
				break;
			}

			case ShowCoinsStep: {
				RightTriggerArrow.SetActive(false);
				AArrow.SetActive(true);
				break;
			}

			case ShowBucketsStep: {
				MyScore = 1;
				AArrow.SetActive(false);
				RightTriggerArrow.SetActive(true);
				Interface.buckets.HardSet(true);
				Interface.UpdateScore();
				break;
			}

			case TryBucketsStep: {
				RightTriggerArrow.SetActive(false);
				BHighlight.SetActive(true);
				BArrow.SetActive(true);
				break;
			}

			case CollectSecondTime: {
				BArrow.SetActive(false);
				AArrow.SetActive(true);
				SpawnCoin(Color.green, offset: -0.25f);
				SpawnCoin(Color.blue, offset: 3f);
				break;
			}

			case ShareBlue: {
				MyScore = 2;
				AArrow.SetActive(false);
				LeftTriggerArrow.SetActive(true);
				LeftTriggerHighlight.SetActive(true);
				Interface.UpdateScore();
				break;
			}

			case CollectThirdTime: {
				LeftTriggerArrow.SetActive(false);
				AArrow.SetActive(true);
				SpawnCoin(Color.green, offset: -0.5f);
				break;
			}

			case ShareRed: {
				MyScore = 3;
				RightTriggerArrow.SetActive(true);
				RightTriggerHighlight.SetActive(true);
				AArrow.SetActive(false);
				Interface.UpdateScore();
				break;
			}

			case TopologyExplanation: {
				MyScore = RedScore = BlueScore = 0;
				Interface.UpdateScore();
				Interface.buckets.Show();
				break;
			}

			/*case TopologyExample1: case TopologyExample2: {
				break;
			}*/

			case BoundsExplanation: {
				Interface.buckets.Hide();
				SpawnBoundary();
				break;
			}

			case LeaderboardExplanation: {
				Interface.LeaderBoard.enabled = true;
				break;
			}
			
			case EndStep: {
				DestroyAllInstances();
				break;
			}

			case EndStep + 1: {
				EndTutorial();
				break;
			}
		}
	}

	private static void DestroyAllInstances() {
		foreach (GameObject o in instances) {
			Destroy(o);
		}
		instances.Clear();
	}
	
	private static void SpawnCoin(Color col, float offset) {
		GameObject inst = Instantiate(coinPrefab);
		Vector3 position = Interface.GetMyPosition() + Interface.GetMyForward() * 6.0f + Interface.GetMyRight() * offset;
		position.y = terrainScript.transform.localPosition.y + 7.2f;
		inst.transform.localPosition = position + Vector3.up * terrainScript.GetHeightAt(position);
		inst.GetComponent<Collider>().enabled = false;
		inst.GetComponent<Collider>().enabled = true;
		Coin cn = inst.GetComponent<Coin>();
		cn.SetColor(col); 
		cn.SetId(col.Equals(Color.green) ? Interface.MyId : "");
		cn.SetParent(coinManager);
		cn.SetAlbedo(0f);
		coinManager.AppendToList(inst);
		instances.Add(inst);
	}

	private static void SpawnBoundary() {
		GameObject inst = Instantiate(_boundsPrefab);
		Vector3 looking = Interface.GetMyForward(); //myTransform.forward;
		Vector3 position = Interface.GetMyPosition() + looking * 13f;
		position.y = terrainScript.transform.localPosition.y;
		Vector3 finalPosition = position + Vector3.up * terrainScript.GetHeightAt(position);
		inst.transform.localPosition = finalPosition;
		inst.transform.localScale = new Vector3(700f, 70f, 0.1f);
		inst.transform.localRotation = Interface.GetMyRotation();
		Interface.SetTutorialBoundary(finalPosition, looking, Interface.GetMyRight());
		instances.Add(inst);
	}

	private static void SpawnWhales() {
		GameObject redInst = Instantiate(_redWhalePrefab);
		Vector3 forwardVec = Interface.GetMyForward();
		forwardVec.y = 0.4f; //eye level
		redInst.transform.localPosition = Interface.GetMyPosition() + forwardVec * 8f + 2.5f * Interface.GetMyRight();
		redInst.transform.localRotation = Interface.GetMyRotation();
		redInst.transform.Rotate(Vector3.up, 45f);
		instances.Add(redInst);
		GameObject blueInst = Instantiate(_blueWhalePrefab);
		blueInst.transform.localPosition = redInst.transform.localPosition + -5f * Interface.GetMyRight();
		blueInst.transform.localRotation = Interface.GetMyRotation();
		blueInst.transform.Rotate(Vector3.up, -45f);
		instances.Add(blueInst);
	}
	
	private static string[] StepTexts => new[] {
		"Hello and thank you for your participation. " +
		"You will now go through a short tutorial.\n\n" +
		"To start, press the trigger on your right hand controller by your index finger:",
		
		"After completing this tutorial, you will be connected with two players (shown ahead):\n\n" +
		"(Press your right trigger to continue)",
		
		"Your goal is to gain " + Interface.Goal + " points as fast as possible each round, for three rounds.\n\n" +
		"Above, your team's total points out of " + Interface.Goal + " will be shown.\n\n" +
		"(Press your right trigger to continue)",
		
		"Additionally, this bar will show how close your team is to " + Interface.Goal + " points, " +
		"as well as each team member's contribution.\n\n",  
		
		"For example, this is what it would look like if your team had <b>5</b> points, " +
		"you (the <color=green>green</color> player) had contributed 4 points, the <color=#0099ff>blue</color> " +
		"player had contributed <b>1</b> point, and <color=red>red</color> had contributed <b>0</b> points.",
		
		"\nFinally, the time elapsed since the start of the round will be shown here.\n\n" +
		"Press your right trigger to learn how to gain points.",
		
		"You will collect <color=green>green</color> colored coins.\n" +
		"Your <color=#0099ff>blue</color> teammate will collect <color=#0099ff>blue</color> coins,\n" +
		"and your <color=red>red</color> teammate will collect <color=red>red</color> coins\n\n" +
		"(Press your right trigger to continue)",
		
		"Some coins are available in front of you. Try to swim to the <color=green>green</color> coin " +
		"by pressing and holding the A button on your controller:",
		
		"You may notice your visibility decreasing.\n" +
		"Throughout gameplay, you and your teammates will <b>continuously</b> lose visibility.\n\n" +
		"After collecting a coin, you will be shown 3 buckets.\n" +
		"\n(Press your right trigger to continue)",
		
		"You will have the option to give the coin to either yourself (<color=green>green</color> bucket), " +
		"or to one of your teammates. Whoever you give it to will receive a boost in their visibility.\n\n" +
		"Try to give it to yourself by pressing B on your right controller:",
		
		"You and your teammates' visibilities will be indicated by the bar next to each bucket.\nTry " +
		"swimming to and collecting one of the coins in front of you by pressing and holding A again:",
		
		"Now, try giving this coin to your <color=#0099ff>blue</color> teammate by pressing the trigger " +
		"on your <b>left</b> hand:",
		
		"Try swimming to the coin in front of you again by pressing and holding A:",
		
		"Now, try to give this coin to your <color=red>red</color> teammate by pressing the trigger " +
		"on your <b>right</b> hand:",
		
		"This experiment will consist of 3 rounds, where each round, you and " +
		"your team try to earn " + Interface.Goal + " points as quickly as possible.\n\n" +
		"Each round, one of your teammates' buckets might be transparent, and you will be " +
		"<b>unable</b> to share coins with that player, as shown: (Press your right trigger to continue)",
		
		/*
		"For instance, this is what you would see if you could share with your <color=#0099ff>blue</color> " +
		"teammate,\nbut <b>not</b> with your <color=red>red</color> teammate." +
		"\nYou will always be able to share coins with yourself, no matter the round." +
		"\n\n(Press your right trigger to continue)",
		
		"And this is what you would see if you could share with your <color=red>red</color> " +
		"teammate,\n but <b>not</b> with your <color=#0099ff>blue</color> teammate." +
		"\n\n(Press your right trigger to continue)",
		*/
		"Since your team's map is limited, if you get close to the edge, you will see a wall of red fog, " +
		"like what is in front of you. You will not be able to move past it.\n\n(Press your right trigger to continue)",
		
		"Lastly, the top 3 scores of all previous teams in any round are shown in front.\n" +
		"This panel will be visible throughout gameplay.\n\n" +
		"(Press your right trigger to continue)",
		
		"You are now ready to play." +
		"\n\nIf you have no further questions,\npress your right trigger to begin.",
		
		"END",
	};
	
	private static void EndTutorial() {
		DestroyAllInstances();
		ToggleDirArrowVisibility(false);
		CurrStep = EndStep;
		Interface.LeaderBoard.enabled = true;
		Interface.socket.enabled = true;
		Interface.scoreText.enabled = true;
		Interface.timeText.enabled = true;
		Interface.timeText.text = " 0:00"; //TODO lobby
		InTutorial = false;
		Interface.LightDecreasing = true;
		Interface.MyScore = 0;
		Interface.UpdateScore();
		helpArrow.GetComponent<MeshRenderer>().enabled = false;
		
		RightTriggerArrow.SetActive(false);
		LeftTriggerArrow.SetActive(false);
		AArrow.SetActive(false);
		BArrow.SetActive(false);
		RightTriggerHighlight.SetActive(true);
		LeftTriggerHighlight.SetActive(true);
		BHighlight.SetActive(true);
		
		HelpText.enabled = false;
		Interface.buckets.HardSet(false);
		timeCount = -1f;
		Interface.ToggleLobby(true);
		Interface.TutorialBoundarySet = false;
	}
}

