using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

public class Detector : MonoBehaviour {
	// Start is called before the first frame update
	private bool _Collided = false;

	public bool Collided => _Collided;
	public Color Color { get; set; }
	private Material _material;

	void Start() {
		foreach (Transform child in transform) {
			if (child.name.Equals("Tube01")) {
				_material = child.GetComponent<MeshRenderer>().material;
				break;
			}
		}
	}

	// Update is called once per frame
	void Update() {
	}

	private void OnTriggerEnter(Collider other) {
		//TODO Interface -> set laser color (on Exit too)
		string laserTag = Interface.RightHandInUse ? "RLaser" : "LLaser";
		if (other.tag.Contains(laserTag)) {
			_Collided = true;
		}
		else {
			return;
		}

		if (!_material.color.a.Equals(Buckets.NoShareAllowedAlbedo)) {
			_material.SetColor("_EmissionColor", Color.gray);
		}
	}

	private void OnTriggerExit(Collider other) {
		string laserTag = Interface.RightHandInUse ? "RLaser" : "LLaser";
		if (other.tag.Contains(laserTag)) {
			_Collided = false;
		}
		else {
			return;
		}
		
		_material.SetColor("_EmissionColor", Color.black);
	}
}
