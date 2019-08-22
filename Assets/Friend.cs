using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;

public class Friend : MonoBehaviour {
	// Start is called before the first frame update
	public static bool colorTaken = false; //Static global allows Friend object to handle diff friend colors internally
	
	private string id = "";
	private Vector3 targetPosition = Vector3.zero, oldPosition = Vector3.zero;
	private Quaternion targetRotation = Quaternion.identity, oldRotation = Quaternion.identity;
	private float targetTime = 0f, oldTime = 0f;
	private float timeSinceUpdate = -1f;
	private Color color = Color.white;
	private Light light;
	private bool flying = false;
	private List<Vector3> positionQueue;
	private List<Quaternion> rotationQueue;
	private List<float> timestampQueue;
	private bool startQueue = false;
	private float interval = 0f;
	private float speed = 0f;
	public int Score = 0, MyCoins = 0, OtherCoins = 0;
	private Animation animation;

	void Start() {
		light = transform.GetComponentsInChildren<Light>()[0];
		positionQueue = new List<Vector3>();
		rotationQueue = new List<Quaternion>();
		timestampQueue = new List<float>();
		animation = GetComponentInChildren<Animation>();
	}

	// Update is called once per frame
	void Update() {
		try { //Sloppy but experiments are soon and we can't have any crashes
			if (startQueue) {
				//Take interval time to go from queue[0] to queue[1]:
				if (timeSinceUpdate >= interval || oldPosition.Equals(Vector3.zero)) {
					if (oldPosition.Equals(Vector3.zero)) {
						oldPosition = Pop(positionQueue, 0);
						oldRotation = Pop(rotationQueue, 0);
						oldTime = Pop(timestampQueue, 0);
					}
					else {
						Transform t = transform;
						oldPosition = t.localPosition;
						oldRotation = t.localRotation;
						oldTime = targetTime + (timeSinceUpdate - interval);
					}

					targetPosition = Pop(positionQueue, 0);
					targetRotation = Pop(rotationQueue, 0);
					targetTime = Pop(timestampQueue, 0);

					timeSinceUpdate = 0f;
					interval = targetTime - oldTime;
				}

				timeSinceUpdate += Time.deltaTime;

				Vector3 dir = Vector3.Normalize(targetPosition - oldPosition);
				Vector3 incr = speed * Time.deltaTime * dir;
				Transform tr = transform;
				tr.localPosition += incr;
				if (!speed.Equals(0f)) {
					//tr.localRotation = Quaternion.LookRotation(incr);
				}

				//transform.localPosition = Vector3.LerpUnclamped(oldPosition, targetPosition, timeSinceUpdate / interval); //<--lurchy method
				tr.localRotation = Quaternion.LerpUnclamped(oldRotation, targetRotation, timeSinceUpdate / interval);
			}
		}
		catch (Exception e) {
			Debug.Log(e);
		}
	}

	private static T Pop<T>(List<T> v, int i) {
		T elem = v[i];
		v.RemoveAt(i);
		return elem;
	}

	public void _SetColor(Color c) {
		color = c;
		transform.GetComponentsInChildren<Light>()[1].color = c;
	}
	
	public void SetId(string id) {
		this.id = id;
		if (color == Color.white) {
			_SetColor(colorTaken ? Color.red : Color.blue);
			colorTaken = true;
		}
	}

	public float GetRange() {
		return light.range;
	}

	public Color GetColor() {
		return color;
	}
	
	public string GetId() {
		return id;
	}

	//Turn raw server data into position, rotation for specific this.id:
	public void AdjustTransform(JSONObject data, bool hardSet) {
		JSONObject myData;
		if (!Interface.Release) myData = data[Interface.MyId];
		else myData = data[id];
		JSONObject pos = myData["position"];
		JSONObject rot = myData["rotation"];
		light.range = myData["range"].f;
		flying = myData["flying"].b;

		Vector3 tPosition = Interface.DeserializeVector3(pos);
		if (!Interface.Release) tPosition += new Vector3(10f, 0f, 10f);
		Quaternion tRotation = Interface.DeserializeQuaternion(rot);

		speed = myData["speed"].f;
		Transform t = transform;
		if (hardSet) {
			t.localPosition = tPosition;
			t.localRotation = tRotation;
		}
		else {
			positionQueue.Add(tPosition);
			rotationQueue.Add(tRotation);
			timestampQueue.Add(Time.time);
			if (positionQueue.Count > 2) {
				startQueue = true;
			}
			else if (positionQueue.Count < 1) {
				Debug.Log("Ran out");
				startQueue = false;
			}
		}
	}
}
