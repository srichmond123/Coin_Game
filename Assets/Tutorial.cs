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
		TryBucketsStep = 8;

	private static TextMeshPro HelpText;
	public static int CurrStep = 0;
	private static ControllerHelper controllerHelper;
	private static bool disableVR;
	public GameObject redWhalePrefab, blueWhalePrefab;
	private static GameObject _redWhalePrefab, _blueWhalePrefab; //To instantiate on step 2
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
	private static float timeCount = -1f;
	private static string currTimeStr = " 0:00";
	void Start() {
		HelpText = GetComponentInChildren<TextMeshPro>();
		HelpText.text = STEP_TEXTS[CurrStep];
		controllerHelper = GetComponentInChildren<ControllerHelper>();
		controllerHelper.ShowTrigger();
		disableVR = GetComponentInParent<Interface>().disableVR;
		instances = new List<GameObject>(); //To collect instantiated prefabs and destroy later
		coinManager = GameObject.Find("I manage coins").GetComponent<CoinManager>();
		coinPrefab = coinManager.coinPrefab;
		terrainScript = GameObject.Find("Terrain").GetComponent<TerrainScript>();
		_redWhalePrefab = redWhalePrefab;
		_blueWhalePrefab = blueWhalePrefab;
		myTransform = transform; //OK since Tutorial.cs isn't going to be instantiated many times
		helpArrow = GameObject.Find("HelpArrow");
		helpArrow.GetComponent<MeshRenderer>().enabled = false;
	}

	// Update is called once per frame
	void Update() {
		if (!disableVR) {
			//TODO VR controller Tutorial screen change
		}
		else {
			if (Input.GetMouseButtonDown(0) && CurrStep != ShowCoinsStep) {
				NextStep();
			} else if (Input.GetKey(KeyCode.S) && CurrStep == ShowCoinsStep) {
				Interface.light.range = Mathf.Max(45f, Interface.light.range - 20.0f * Time.deltaTime);
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

	private static void SetArrowPositionAndRotation(Vector3 position, float xRotation) {
		helpArrow.transform.localPosition = position;
		Vector3 eulerAngles = helpArrow.transform.localEulerAngles;
		eulerAngles.x = xRotation;
		helpArrow.transform.localEulerAngles = eulerAngles;
	}

	public static void NextStep() {
		HelpText.text = STEP_TEXTS[++CurrStep];
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
				controllerHelper.ShowAButton(); //TODO dont allow swimming until now
				break;
			}

			case ShowBucketsStep: {
				MyScore = 1;
				controllerHelper.SetVisible(false);
				break;
			}

			case TryBucketsStep: {
				controllerHelper.ShowBButton();
				break;
			}

			default: {
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
		Vector3 position = Interface.GetMyPosition() + myTransform.forward * 7f + myTransform.right * offset;
		position.y += terrainScript.transform.localPosition.y + 1.2f;
		inst.transform.localPosition = position + Vector3.up * terrainScript.GetHeightAt(position);
		inst.GetComponent<Collider>().enabled = false;
		inst.GetComponent<Collider>().enabled = true;
		CoinScript cs = inst.GetComponent<CoinScript>();
		cs.SetColor(col); 
		cs.SetId(col.Equals(Color.green) ? Interface.MyId : "");
		cs.SetParent(coinManager);
		cs.SetAlbedo(0f);
		coinManager.AppendToList(inst);
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
	
	private static string[] STEP_TEXTS => new[] {
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
		
		"\n\n\n\nYou may have noticed your visibility going down.\n" +
		"Throughout each trial, you and your teammates will lose visibility at a constant rate.\n\n" +
		"After collecting a coin, you will be shown 3 buckets.\n" +
		"\n(Press your right trigger to continue)",
		
		"\n\n\n\nYou will have the option to give the coin to either yourself (<color=green>green</color> bucket), " +
		"or to one of your teammates. Whoever you give it to will receive a boost in their visibility.\n\n" +
		"Try to give it to yourself by pressing B on your right controller:",
		
		//TODO explain cross. Describe 3 trials, how they can quit at any time, etc.
		
		"END",
	};
	
	private static void EndTutorial() {
		Interface.socket.enabled = true;
		Interface.scoreText.enabled = true;
		Interface.timeText.enabled = true;
		Interface.timeText.text = " 0:00"; //TODO lobby
		InTutorial = false;
		Interface.LightDecreasing = true;
		helpArrow.GetComponent<MeshRenderer>().enabled = false;
		controllerHelper.SetVisible(false);
		HelpText.enabled = false;
		timeCount = -1f;
	}
}

