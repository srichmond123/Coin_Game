using System.Collections;
using System.Collections.Generic;
using OculusSampleFramework;
using OVR.OpenVR;
using SocketIO;
using UnityEngine;

public class Buckets : MonoBehaviour {
	private int coins = 0;
	private static SocketIOComponent socket;
	public GameObject crossPrefab;

	private bool colorsInitialized = false;
	private GameObject crossInstance;
	private AudioSource giveSound;
	void Start() {
		GameObject sockObject = GameObject.Find("SocketIO");
		socket = sockObject.GetComponent<SocketIOComponent>();
		giveSound = GameObject.Find("GiveSound").GetComponent<AudioSource>();
	}

	public void Handle() {
		if (coins == 0) {
			Show();
		}

		coins += 1;
	}

	public int GetCoinsHeld() {
		return coins;
	}

	public void HardSet(bool b) {
		foreach (Transform child in transform) {
			child.gameObject.SetActive(b);
		}
	}
	
	public void Hide() {
		foreach (Transform child in transform) {
			Color col = GetBucketColor(child);
			col.a = 0.08f;
			SetChildColor(child, col);
			colorsInitialized = false;
			//child.gameObject.SetActive(false);
		}
		
		//Destroy(crossInstance);
		coins = 0;
	}

	private void SetChildColor(Transform t, Color c) {
		foreach (Transform child in t) {
			if (child.name.Equals("Tube01")) {
				Material m = child.GetComponent<MeshRenderer>().materials[0];
				m.color = c;
			}
		}
	}

	public void Show() {
		int idx = 0;
		foreach (Transform child in transform) {
			//child.gameObject.SetActive(true);
			if (idx == 1) {
				if (!colorsInitialized) {
					SetChildColor(child, Color.green);
				}
			}
			else {
				Friend friendSet = Interface.friends[idx == 0 ? 0 : 1];
				float albedo = 1f;
				if (!Tutorial.InTutorial) {// || idx == 0) {
					if (!Interface.permissibleIndividuals.Contains(friendSet.GetId())) {
						/*
						GameObject inst = Instantiate(crossPrefab, GameObject.Find("TrackingSpace").transform);
						inst.transform.position = child.position;
						inst.transform.Translate(new Vector3(0, 0.203f, -0.134f));
						crossInstance = inst;
						*/
						albedo = 0.1f;
					}
				}
				else if (idx != 0 && Tutorial.CurrStep == Tutorial.TopologyExplanation) {
					albedo = 0.1f;
				}

				if (!colorsInitialized) {
					Color col = !Tutorial.InTutorial
						? friendSet.GetColor()
						: (idx == 0 ? Color.blue : Color.red);
					col.a = albedo;
					SetChildColor(child, col);
				}
			}
			idx++;
		}

		if (!Tutorial.InTutorial) {
			colorsInitialized = true;
		}
	}

	private Color GetBucketColor(Transform t) {
		foreach (Transform child in t) {
			if (child.name.Equals("Tube01")) {
				return child.GetComponent<MeshRenderer>().materials[0].color;
			}
		}

		return Interface.NullColor;
	}

	//Apply shift with scale:
	private void ScaleTo(Transform bar, float newScaleY) {
		Vector3 localScale = bar.localScale;
		float oldScaleY = localScale.y;
		float positionChange = -(newScaleY - oldScaleY)/2f;
		bar.localPosition += -positionChange * Vector3.up;
		localScale.y = newScaleY;
		bar.localScale = localScale;
	}

	public void UpdateHealth() {
		foreach (Transform child in transform) {
			Color bucketColor = GetBucketColor(child);
			Transform bar = null;
			if (CompareRGB(bucketColor, Color.green)) {
				bar = child.GetChild(child.childCount - 1);
				ScaleTo(bar, Interface.light.range / 50f);
			}
			else {
				if (!Tutorial.InTutorial) {
					foreach (Friend f in Interface.friends) {
						if (CompareRGB(bucketColor, f.GetColor())) {
							bar = child.GetChild(child.childCount - 1);
							float scale = f.GetRange() / 50f;
							ScaleTo(bar, scale);
						}
					}
				}
				else {
					bar = child.GetChild(child.childCount - 1);
					ScaleTo(bar, Tutorial.GetFriendRange(bucketColor));
				}
			}
		}
	}

	public void PlaySound() {
		giveSound.Play();
	}

	public static bool CompareRGB(Color a, Color b) {
		return a.r.Equals(b.r) && a.g.Equals(b.g) && a.b.Equals(b.b);
	}

	public void HandleClick(Color c) {
		if (coins > 0) {
			if (!Tutorial.InTutorial) {
				if (CompareRGB(c, Color.green)) {
					Interface.MyCoinsOwned++;
					socket.Emit("claim");
					PlaySound();
                    DataCollector.WriteEvent("claim", "me");
				}
				else {
					foreach (Friend friend in Interface.friends) {
						if (CompareRGB(friend.GetColor(), c)) {
							if (Interface.permissibleIndividuals.Contains(friend.GetId())) {
								Dictionary<string, string> dict = new Dictionary<string, string>();
								dict["id"] = friend.GetId();
								socket.Emit("give", new JSONObject(dict));
								friend.OtherCoins++;
								PlaySound();
                                DataCollector.WriteEvent("give", friend.GetId());
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
		if (Tutorial.InTutorial) Tutorial.HandleBucketClick(c);
	}

	void Update() {
		
	}
}
