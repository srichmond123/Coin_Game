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
		FlyBackwardsStep = 10,
		CollectSecondTime = 11,
		ShareBlue = 12,
		CollectThirdTime = 13,
		ShareRed = 14,
		TopologyExplanation = 15,
		BoundsExplanation = 16,
		LeaderboardExplanation = 17,
		EndStep = 18;

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
		LeftTriggerArrow,
		XButtonArrow,
		YButtonArrow,
		AButtonArrow,
		BButtonArrow;

	private static GameObject
		PrimaryTriggerArrow,
		PrimaryButtonOneArrow,
		PrimaryButtonTwoArrow; //Once hand is chosen, these will be set to either AArrow or BArrow, etc.

	public static int MyScore = 0, RedScore = 0, BlueScore = 0;
	private static float RedRange = 100f;
	private static float BlueRange = 60f;
	private static float timeCount = -1f;
	private static string currTimeStr = " 0:00";
	private static float _swimBackwardsTime = 0f;
	private static float SwimBackwardsRequirement => 2f;

	void Start() {
		HelpText = GetComponentInChildren<TextMeshPro>();
		HelpText.text = StepTexts(CurrStep);

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

		GameObject rightAnchor = GameObject.Find("RightHandAnchor");
		GameObject leftAnchor = GameObject.Find("LeftHandAnchor");

		//Show both triggers at first until they press one:
		foreach (Transform child in rightAnchor.transform) {
			switch (child.name) {
				case "BButtonArrow":
					BButtonArrow = child.gameObject;
					BButtonArrow.SetActive(false);
					break;
				case "RightTriggerArrow":
					RightTriggerArrow = child.gameObject;
					break;
				case "AButtonArrow":
					AButtonArrow = child.gameObject;
					AButtonArrow.SetActive(false);
					break;
			}
		}

		foreach (Transform child in leftAnchor.transform) {
			switch (child.name) {
				case "LeftTriggerArrow":
					LeftTriggerArrow = child.gameObject;
					break;
				case "XButtonArrow":
					XButtonArrow = child.gameObject;
					XButtonArrow.SetActive(false);
					break;
				case "YButtonArrow":
					YButtonArrow = child.gameObject;
					YButtonArrow.SetActive(false);
					break;
			}
		}
		
		SetPrimaryArrows(rightHandInUse: true);
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
				float decr = CurrStep == ShowBucketsStep ? 17f : 4f;
				Interface.light.range = Mathf.Max(MinRange, Interface.light.range - decr * Time.deltaTime);
				RedRange = Mathf.Max(MinRange + 35f, RedRange - decr * Time.deltaTime);
				BlueRange = Mathf.Max(MinRange + 35f, BlueRange - decr * Time.deltaTime);
			}

			if (Input.GetKeyDown(KeyCode.Space)) { //shortcut
				EndTutorial(true);
			}

			if (Input.GetKeyDown(KeyCode.Tilde)) {
				StartOver();
			}

			if (timeCount >= 0f) {
				timeCount += Time.deltaTime;
				currTimeStr = Interface.ParseMilliseconds((int) (timeCount * 1000));
				if (!Interface.timeText.text.Equals(currTimeStr)) {
					Interface.timeText.text = currTimeStr;
				}
			}

			if (OVRInput.Get(Interface.GetButtonTwo()) && CurrStep == FlyBackwardsStep) {
				//User is swimming backwards, make them do this for 1 second:
				_swimBackwardsTime += Time.deltaTime;
				if (_swimBackwardsTime >= SwimBackwardsRequirement) {
					NextStep();
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
					if (Vector3.Distance(Interface.GetMyPosition(), coinPos) > 14f
					    || Vector3.Angle(coinDirVec, myDirVec) > fov / 2f) {
						ToggleDirArrowVisibility(true);
						dirArrow.transform.LookAt(coinPos);
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
		LeftTriggerArrow.SetActive(true);
		RightTriggerArrow.SetActive(true);
		XButtonArrow.SetActive(false);
		YButtonArrow.SetActive(false);
		AButtonArrow.SetActive(false);
		BButtonArrow.SetActive(false);
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
		CurrStep = Welcome;
		HelpText.text = StepTexts(CurrStep);
	}

	public static void SetPrimaryArrows(bool rightHandInUse) {
		PrimaryTriggerArrow = rightHandInUse ? RightTriggerArrow : LeftTriggerArrow;
		PrimaryButtonOneArrow = rightHandInUse ? AButtonArrow : XButtonArrow;
		PrimaryButtonTwoArrow = rightHandInUse ? BButtonArrow : YButtonArrow;
		if (rightHandInUse) {
			LeftTriggerArrow.SetActive(false);
			YButtonArrow.SetActive(false);
			XButtonArrow.SetActive(false);
		}
		else {
			RightTriggerArrow.SetActive(false);
			BButtonArrow.SetActive(false);
			AButtonArrow.SetActive(false);
		}
	}

	public static void NextStep() {
		HelpText.text = StepTexts(++CurrStep);
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
				PrimaryTriggerArrow.SetActive(false);
				PrimaryButtonOneArrow.SetActive(true);
				break;
			}

			case ShowBucketsStep: {
				MyScore = 1;
				PrimaryButtonOneArrow.SetActive(false);
				PrimaryTriggerArrow.SetActive(true);
				Interface.buckets.HardSet(true);
				Interface.UpdateScore();
				break;
			}

			case TryBucketsStep: {
				//RightTriggerArrow.SetActive(false);
				//BArrow.SetActive(true); //(old method, they point and click now)
				break;
			}

			case FlyBackwardsStep: {
				PrimaryTriggerArrow.SetActive(false);
				PrimaryButtonTwoArrow.SetActive(true);
				break;
			}

			case CollectSecondTime: {
				PrimaryButtonTwoArrow.SetActive(false);
				PrimaryButtonOneArrow.SetActive(true);
				SpawnCoin(Color.green, offset: -0.25f);
				SpawnCoin(Color.blue, offset: 3f);
				break;
			}

			case ShareBlue: {
				MyScore = 2;
				PrimaryButtonOneArrow.SetActive(false);
				PrimaryTriggerArrow.SetActive(true);
				Interface.UpdateScore();
				break;
			}

			case CollectThirdTime: {
				PrimaryTriggerArrow.SetActive(false);
				PrimaryButtonOneArrow.SetActive(true);
				SpawnCoin(Color.green, offset: -0.5f);
				break;
			}

			case ShareRed: {
				MyScore = 3;
				PrimaryTriggerArrow.SetActive(true);
				PrimaryButtonOneArrow.SetActive(false);
				Interface.UpdateScore();
				break;
			}

			case TopologyExplanation: {
				MyScore = RedScore = BlueScore = 0;
				Interface.UpdateScore();
				Interface.buckets.Show();
				break;
			}

			case BoundsExplanation: {
				Interface.buckets.Hide();
				SpawnBoundary();
				break;
			}

			case LeaderboardExplanation: {
				Interface.LeaderBoard.SetActive(true);
				break;
			}

			case EndStep: {
				DestroyAllInstances();
				break;
			}

			case EndStep + 1: {
				EndTutorial(true);
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
		Vector3 position = Interface.GetMyPosition() + Interface.GetMyForward() * 6.0f +
		                   Interface.GetMyRight() * offset;
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
		forwardVec.y = 0.2f; //eye level
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

	private static string StepTexts(int stepNum) {
		switch (stepNum) {
			case Welcome:
				return "Hello and thank you for your participation. " +
				       "You will now go through a short tutorial.\n\n" +
				       "To start, press the trigger on your controller by your index finger:";
			
			case ShowFriendsStep:
				return "After completing this tutorial, you will be connected with two players (shown ahead):\n\n" +
				       $"(Press your {(Interface.RightHandInUse ? "right" : "left")} trigger to continue)";
	 
			case ShowScoreTextStep:
				return
					$"Your goal is to gain {Interface.Goal} points as fast as possible each round, for three rounds.\n\n" +
					$"Above, your team's total points out of {Interface.Goal} will be shown.\n\n" +
					$"(Press your {(Interface.RightHandInUse ? "right" : "left")} trigger to continue)";
	 
			case ShowScoreBarStep:
				return $"Additionally, this bar will show how close your team is to {Interface.Goal} points.";
				       //$" as well as each team member's contribution.\n\n";
			
			case ExplainScoreBarStep:
				return "For example, this is what it would look like if your team had <b>5</b> points.";// +
				       //"you (the <color=green>green</color> player) had contributed 4 points, the <color=#0099ff>blue</color> " +
				       //"player had contributed <b>1</b> point, and <color=red>red</color> had contributed <b>0</b> points.";
			
			case ShowTimerStep:
				return "\nFinally, the time elapsed since the start of the round will be shown here.\n\n" +
				       $"Press your {(Interface.RightHandInUse ? "right" : "left")} trigger to learn how to gain points.";
			
			case ShowCoinRulesStep:
				return "You will collect <color=green>green</color> colored coins.\n" +
				       "Your <color=#0099ff>blue</color> teammate will collect <color=#0099ff>blue</color> coins,\n" +
				       "and your <color=red>red</color> teammate will collect <color=red>red</color> coins\n\n" +
				       $"(Press your {(Interface.RightHandInUse ? "right" : "left")} trigger to continue)";
			
			case ShowCoinsStep:
				return "Some coins are available in front of you. Try to swim to the <color=green>green</color> coin " +
				       $"by pressing and holding the {(Interface.RightHandInUse ? "A" : "X")} button on your controller:";
	 
			case ShowBucketsStep:
				return "You may notice your visibility decreasing.\n" +
				       "Throughout gameplay, you and your teammates will <b>continuously</b> lose visibility.\n\n" +
				       "After collecting a coin, you will be shown 3 buckets.\n" +
				       $"\n(Press your {(Interface.RightHandInUse ? "right" : "left")} trigger to continue)";
			
			case TryBucketsStep:
				return
					"You will have the option to give the coin to either yourself (<color=green>green</color> bucket), " +
					"or to one of your teammates. Whoever you give it to will receive a boost in their visibility.\n\n" +
					"Try to give it to yourself by pointing your laser at the <color=green>green</color> bucket " +
					$"and pressing your {(Interface.RightHandInUse ? "right" : "left")} trigger:";
							  
			case FlyBackwardsStep:
				return $"To swim backwards, press and hold the {(Interface.RightHandInUse ? "B" : "Y")} " +
				       "button on your controller.\n\nTry it out now.";
			
			case CollectSecondTime:
				return "You and your teammates' visibilities will be indicated by the bar next to each bucket.\nTry " +
				       "swimming to and collecting one of the coins in front of you by pressing and holding " +
				       $"{(Interface.RightHandInUse ? "A" : "X")} again:";
			
			case ShareBlue:
				return "Now, try giving this coin to your <color=#0099ff>blue</color> teammate by pointing at the blue bucket " +
					$"and pressing your {(Interface.RightHandInUse ? "right" : "left")} trigger:";
			
			case CollectThirdTime:
				return "Try swimming to the coin in front of you again by pressing and " +
			       $"holding {(Interface.RightHandInUse ? "A" : "X")}:";
			
			case ShareRed:
				return "Now, try to give this coin to your <color=red>red</color> teammate by pointing and clicking " +
				       "at the red bucket:";
	 
			case TopologyExplanation:
				return $"This experiment will consist of 3 rounds, where each round, " +
				       $"you and your team try to earn {Interface.Goal} points as quickly as possible.\n\n" +
				       $"Each round, one of your teammates' buckets might be transparent, " +
				       $"and you will be <b>unable</b> to share coins with that player, as shown: " +
				       $"(Press your {(Interface.RightHandInUse ? "right" : "left")} trigger to continue)";
			
			case BoundsExplanation:
				return "Since your team's map is limited, if you get close to the edge, you will see a wall of fog, " +
				       "like what is in front of you. You will not be able to move past it.\n\n" +
				       $"(Press your {(Interface.RightHandInUse ? "right" : "left")} trigger to continue)";
			
			case LeaderboardExplanation:
				return "Lastly, the top 3 scores of all previous teams in any round are shown in front.\n" +
				       "This panel will be visible throughout gameplay.\n\n" +
				       $"(Press your {(Interface.RightHandInUse ? "right" : "left")} trigger to continue)";
			
			case EndStep:
				return "You are now ready to play.\n\nIf you have no further questions,\n" +
				       $"press your {(Interface.RightHandInUse ? "right" : "left")} trigger to begin.";
		}

		return "END";
	}

	public static void EndTutorial(bool notifyServer) {
		DestroyAllInstances();
		ToggleDirArrowVisibility(false);
		CurrStep = EndStep;
		Interface.LeaderBoard.SetActive(true);
		if (notifyServer) {
			Interface.socket.Emit("doneTutorial");
		}

		Interface.scoreText.enabled = true;
		Interface.timeText.enabled = true;
		Interface.timeText.text = " 0:00";
		InTutorial = false;
		Interface.LightDecreasing = true;
		Interface.MyScore = 0;
		Interface.UpdateScore();
		helpArrow.GetComponent<MeshRenderer>().enabled = false;
		
		RightTriggerArrow.SetActive(false);
		LeftTriggerArrow.SetActive(false);
		XButtonArrow.SetActive(false);
		YButtonArrow.SetActive(false);
		AButtonArrow.SetActive(false);
		BButtonArrow.SetActive(false);
		
		HelpText.enabled = false;
		Interface.buckets.HardSet(false);
		timeCount = -1f;
		Interface.ToggleLobby(true);
		Interface.TutorialBoundarySet = false;
	}
}

