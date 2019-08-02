using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Color = UnityEngine.Color;

public class Detector : MonoBehaviour {
	// Start is called before the first frame update
	private bool _RCollided = false, _LCollided = false;

	public bool RCollided => _RCollided;
	public bool LCollided => _LCollided;
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
		Debug.Log($"{other.tag} entered");
		//TODO Interface -> set laser color (on Exit too)
		if (other.tag.Equals("RLaser")) {
			_RCollided = true;
		} else if (other.tag.Equals("LLaser")) {
			_LCollided = true;
		}
		else {
			return;
		}
		
		_material.SetColor("_EmissionColor", Color.gray);
	}

	private void OnTriggerExit(Collider other) {
		Debug.Log($"{other.tag} left");
		if (other.tag.Equals("RLaser")) {
			_RCollided = false;
		} else if (other.tag.Equals("LLaser")) {
			_LCollided = false;
		}
		else {
			return;
		}
		
		_material.SetColor("_EmissionColor", Color.black);
	}
}
