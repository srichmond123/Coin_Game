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
		ShowTimerStep = 4,
		ShowCoinRulesStep = 5,
		ShowCoinsStep = 6,
		ShowBucketsStep = 7,
		TryBucketsStep = 8,
		CollectSecondTime = 9,
		ShareBlue = 10,
		CollectThirdTime = 11,
		ShareRed = 12,
		TopologyExplanation = 13,
		TopologyExample1 = 14,
		TopologyExample2 = 15,
		BoundsExplanation = 16,

		EndStep = 17;

	private static TextMeshPro HelpText;
	public static int CurrStep = 0;
	private static ControllerHelper controllerHelper;
	private static bool disableVR;
	public GameObject redWhalePrefab, blueWhalePrefab, boundsPrefab;
	private static GameObject _redWhalePrefab, _blueWhalePrefab, _boundsPrefab;
	private static List<GameObject> instances;
	private static GameObject coinPrefab, helpArrow;
	private static TerrainScript terrainScript;
	private static CoinManager coinManager;
	private static Transform myTransform;
	
	private static Vector3 arrowPositionScoreText => new Vector3(0.089f, 0.143f, 0.258f);
	private static Vector3 arrowPositionScoreBar => new Vector3(0.09f, 0.17f, 0.26f);
	private static Vector3 arrowPositionTimer => new Vector3(0.089f, 0.086f, 0.254f);
	private static float arrowXRotationScoreText => 0f;
	private static float arrowXRotationScoreBar => -45f;
	private static float arrowXRotationTimer => arrowXRotationScoreText;

	public static int MyScore = 0, RedScore = 0, BlueScore = 0;
	private static float RedRange = 100f;
	private static float BlueRange = 60f;
	private static float timeCount = -1f;
	private static string currTimeStr = " 0:00";
	void Start() {
		HelpText = GetComponentInChildren<TextMeshPro>();
		HelpText.text = StepTexts[CurrStep];
		controllerHelper = GetComponentInChildren<ControllerHelper>();
		controllerHelper.ShowRightTrigger();
		disableVR = GetComponentInParent<Interface>().disableVR;
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
	}

	public static void TellClicked() {
        if (CurrStep < ShowCoinsStep || CurrStep == ShowBucketsStep || CurrStep >= TopologyExplanation) {
            NextStep();
        }
	}

	// Update is called once per frame
	void Update() {
		if (CurrStep < EndStep) {
			if (!disableVR) {
				//TODO VR controller Tutorial screen change
			}
			else {
				if (CurrStep >= ShowBucketsStep) {
					float decr = CurrStep == ShowBucketsStep ? 5f : Interface.ConstDecrease;
					Interface.light.range = Mathf.Max(Interface.MinRange + 10f, Interface.light.range - decr * Time.deltaTime);
					RedRange = Mathf.Max(Interface.MinRange + 10f, RedRange - decr * Time.deltaTime);
					BlueRange = Mathf.Max(Interface.MinRange + 10f, BlueRange - decr * Time.deltaTime);
				}

				if (Input.GetKeyDown(KeyCode.Space)) { //shortcut
					EndTutorial();
				}
			}

			if (timeCount >= 0f) {
				timeCount += Time.deltaTime;
				currTimeStr = Interface.ParseMilliseconds((int) (timeCount * 1000));
				if (!Interface.timeText.text.Equals(currTimeStr)) {
					Interface.timeText.text = currTimeStr;
				}
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
			case ShowBucketsStep: case TopologyExplanation: case TopologyExample1: case TopologyExample2:{
				NextStep();
				break;
			}
			case TryBucketsStep: {
				if (Buckets.CompareRGB(Color.green, col)) {
					Interface.buckets.Hide();
					Interface.MyCoinsOwned += 1;
					NextStep();
				}
				break;
			}

			case ShareBlue: {
				if (Buckets.CompareRGB(Color.blue, col)) {
					Interface.buckets.Hide();
					BlueRange += Interface.OtherRangeIncrease;
					NextStep();
				}
				break;
			}

			case ShareRed: {
				if (Buckets.CompareRGB(Color.red, col)) {
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
				controllerHelper.SetVisible(false);
				DestroyAllInstances();
				break;
			}

			case ShowScoreBarStep: {
				MyScore = 4;
				BlueScore = 1;
				RedScore = 0;
				Interface.UpdateScore();
				SetArrowPositionAndRotation(arrowPositionScoreBar, arrowXRotationScoreBar);
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
				SpawnCoin(Color.green, 1);
				SpawnCoin(Color.blue, 4.5f);
				break;
			}

			case ShowCoinsStep: {
				controllerHelper.ShowAButton();
				break;
			}

			case ShowBucketsStep: {
				MyScore = 1;
				controllerHelper.SetVisible(false);
				Interface.buckets.HardSet(true);
				Interface.UpdateScore();
				break;
			}

			case TryBucketsStep: {
				controllerHelper.ShowBButton();
				break;
			}

			case CollectSecondTime: {
				controllerHelper.ShowAButton();
				SpawnCoin(Color.green, offset: 0f);
				SpawnCoin(Color.blue, offset: 3f);
				break;
			}

			case ShareBlue: {
				MyScore = 2;
				controllerHelper.ShowLeftTrigger();
				Interface.UpdateScore();
				break;
			}

			case CollectThirdTime: {
				controllerHelper.ShowAButton();
				SpawnCoin(Color.green, offset: -1f);
				break;
			}

			case ShareRed: {
				MyScore = 3;
				controllerHelper.ShowRightTrigger();
				Interface.UpdateScore();
				break;
			}

			case TopologyExplanation: {
				controllerHelper.SetVisible(false);
				break;
			}

			case TopologyExample1: case TopologyExample2: {
				Interface.buckets.Show();
				break;
			}

			case BoundsExplanation: {
				Interface.buckets.Hide();
				SpawnBoundary();
				break;
			}
			
			case EndStep: {
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
		Vector3 position = Interface.GetMyPosition() + myTransform.forward * 4.5f + myTransform.right * offset;
		position.y = terrainScript.transform.localPosition.y + 4.2f;
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
		Vector3 looking = myTransform.forward;
		Vector3 position = Interface.GetMyPosition() + looking * 7f;
		position.y = terrainScript.transform.localPosition.y;
		Vector3 finalPosition = position + Vector3.up * terrainScript.GetHeightAt(position);
		inst.transform.localPosition = finalPosition;
		inst.transform.localScale = new Vector3(700f, 70f, 0.1f);
		inst.transform.localRotation = Interface.GetMyRotation();
		Interface.SetTutorialBoundary(finalPosition, looking, myTransform.right);
		instances.Add(inst);
	}

	private static void SpawnWhales() {
		GameObject redInst = Instantiate(_redWhalePrefab);
		Vector3 forwardVec = myTransform.forward;
		forwardVec.y = 0; //eye level
		redInst.transform.localPosition = Interface.GetMyPosition() + forwardVec * 5f + 2.5f * myTransform.right;
		redInst.transform.localRotation = Interface.GetMyRotation();
		redInst.transform.Rotate(Vector3.up, 45f);
		instances.Add(redInst);
		GameObject blueInst = Instantiate(_blueWhalePrefab);
		blueInst.transform.localPosition = redInst.transform.localPosition + -5f * myTransform.right;
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
		
		"\n\n\n\nYour goal is to gain " + Interface.Goal + " points as fast as possible each round, for three rounds.\n\n" +
		"Above, your team's total points out of " + Interface.Goal + " will be shown.\n\n" +
		"(Press your right trigger to continue)",
		
		"\n\n\n\nAdditionally, this bar will show how close your team is to " + Interface.Goal + " points, " +
		"as well as each team member's contribution.\n\n" + 
		"For example, this is what it would look like if your team had <b>5</b> points, " +
		"you (the <color=green>green</color> player) had contributed 4 points, the <color=blue>blue</color> " +
		"player had contributed <b>1</b> point, and <color=red>red</color> had contributed <b>0</b> points.",
		
		"\n\n\n\n\nFinally, the time elapsed since the start of the round will be shown here.\n\n" +
		"Press your right trigger to learn how to gain points.",
		
		"\n\n\n\nYou will collect <color=green>green</color> colored coins.\n" +
		"Your <color=blue>blue</color> teammate will collect <color=blue>blue</color> coins,\n" +
		"and your <color=red>red</color> teammate will collect <color=red>red</color> coins\n\n" +
		"(Press your right trigger to continue)",
		
		"\n\n\n\nSome coins are available in front of you. Try to swim to the <color=green>green</color> coin " +
		"by pressing and holding the A button on your controller:",
		
		"\n\n\n\nYou may notice your visibility decreasing.\n" +
		"Throughout gameplay, you and your teammates will <b>continuously</b> lose visibility.\n\n" +
		"After collecting a coin, you will be shown 3 buckets.\n" +
		"\n(Press your right trigger to continue)",
		
		"\n\n\n\nYou will have the option to give the coin to either yourself (<color=green>green</color> bucket), " +
		"or to one of your teammates. Whoever you give it to will receive a boost in their visibility.\n\n" +
		"Try to give it to yourself by pressing B on your right controller:",
		
		"\n\n\n\n.You and your teammates' visibilities will be indicated by the bar next to each bucket.\nTry " +
		"swimming to and collecting one of the coins in front of you by pressing and holding A again:",
		
		"\n\n\n\nNow, try giving this coin to your <color=blue>blue</color> teammate by pressing the trigger " +
		"on your <b>left</b> hand:",
		
		"\n\n\n\nTry swimming to the coin in front of you again by pressing and holding A:",
		
		"\n\n\n\nNow, try to give this coin to your <color=red>red</color> teammate by pressing the trigger " +
		"on your <b>right</b> hand:",
		
		"\n\n\n\nAs explained earlier, this experiment will consist of 3 rounds, where each round, you and " +
		"your team try to earn " + Interface.Goal + " points as quickly as possible.\n\n" +
		"Each round, one or both of your teammates' buckets might be transparent, meaning that you will be " +
		"<b>unable</b> to share coins with that player in that round. (Press your right trigger to continue)",
		
		"\n\n\n\nFor instance, this is what you would see if you could share with your <color=blue>blue</color> " +
		"teammate,\nbut <b>not</b> with your <color=red>red</color> teammate." +
		"\nYou will always be able to share coins with yourself, no matter the round." +
		"\n\n(Press your right trigger to continue)",
		
		"\n\n\n\nThis is what you would see if you could share with your <color=red>red</color> " +
		"teammate,\n but <b>not</b> with your <color=blue>blue</color> teammate." +
		"\n\n(Press your right trigger to continue)",
		
		"\n\n\n\nSince your team's map is limited, if you get close to the edge, you will see a wall of red fog, " +
		"like what is in front of you. You will not be able to move past it.\n\n(Press your right trigger to continue)",
		
		"\n\n\n\nThank you, press right trigger to play",
		
		//TODO quit at any time, bounds, previous times of "other players"
		
		"END",
	};
	
	private static void EndTutorial() {
		DestroyAllInstances();
		CurrStep = EndStep;
		Interface.socket.enabled = true;
		Interface.scoreText.enabled = true;
		Interface.timeText.enabled = true;
		Interface.timeText.text = " 0:00"; //TODO lobby
		InTutorial = false;
		Interface.LightDecreasing = true;
		Interface.MyScore = 0;
		Interface.UpdateScore();
		helpArrow.GetComponent<MeshRenderer>().enabled = false;
		controllerHelper.SetVisible(false);
		HelpText.enabled = false;
		Interface.buckets.HardSet(false);
		timeCount = -1f;
		Interface.ToggleLobby(true);
		Interface.TutorialBoundarySet = false;
	}
}

