using System.Collections;
using System.Collections.Generic;
using OculusSampleFramework;
using OVR.OpenVR;
using SocketIO;
using UnityEngine;

public class Buckets : MonoBehaviour {
	private int coins = 0;
	public static SocketIOComponent socket;
	public GameObject crossPrefab;

	private bool colorsInitialized = false;
	private GameObject crossInstance;
	void Start() {
		Hide();
		GameObject sockObject = GameObject.Find("SocketIO");
		socket = sockObject.GetComponent<SocketIOComponent>();
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

	public void Hide() {
		//TODO make transluscent
		foreach (Transform child in transform) {
			child.gameObject.SetActive(false);
		}
		
		Destroy(crossInstance);
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

	private void Show() {
		int idx = 0;
		foreach (Transform child in transform) {
			child.gameObject.SetActive(true);
			if (idx == 1) {
				if (!colorsInitialized) {
					SetChildColor(child, Color.green);
				}
			}
			else {
				Friend friendSet = Interface.friends[idx == 0 ? 0 : 1];
				if (!Tutorial.InTutorial) {// || idx == 0) {
					if (!Interface.permissibleIndividuals.Contains(friendSet.GetId())) {
						GameObject inst = Instantiate(crossPrefab, GameObject.Find("TrackingSpace").transform);
						inst.transform.position = child.position;
						inst.transform.Translate(new Vector3(0, 0.203f, -0.134f));
						//if (crossInstance != null) Destroy(crossInstance);
						crossInstance = inst; //TODO make transluscent
					}
				}

				if (!colorsInitialized) {
					SetChildColor(child, !Tutorial.InTutorial 
						? friendSet.GetColor() : (idx == 0 ? Color.blue : Color.red));
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
	public void ScaleTo(Transform bar, float newScaleY) {
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
			if (bucketColor.Equals(Color.green)) {
				bar = child.GetChild(child.childCount - 1);
				ScaleTo(bar, Interface.light.range / 50f);
			}
			else {
				foreach (Friend f in Interface.friends) {
					if (GetBucketColor(child).Equals(f.GetColor())) {
						bar = child.GetChild(child.childCount - 1);
						ScaleTo(bar, f.GetRange() / 50f);
						//Vector3 scale = bar.localScale;
						//scale.y = o.GetRange() / 50f;
						//bar.localScale = scale;
					}
				}
			}
		}
	}

	public void HandleClick(Transform t) {
		Color c = GetBucketColor(t);
		
		if (coins > 0) {
			if (c.Equals(Color.green)) {
				Interface.MyCoinsOwned++;
				socket.Emit("claim");
			}
			else {
				foreach (Friend friend in Interface.friends) {
					if (friend.GetColor().Equals(c)) {
						if (Interface.permissibleIndividuals.Contains(friend.GetId())) {
							Dictionary<string, string> dict = new Dictionary<string, string>();
							dict["id"] = friend.GetId();
							socket.Emit("give", new JSONObject(dict));
							friend.OtherCoins++;
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

	void Update() {
		
	}
}
