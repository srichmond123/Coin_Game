using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LoadingCircle : MonoBehaviour {
	// Start is called before the first frame update
	private bool _enabled = false;
	void Start() {
		
	}

	// Update is called once per frame
	void Update() {
		if (_enabled) {
			transform.Rotate(Vector3.up, 300f * Time.deltaTime);
		}
	}

	public void Set(bool state) {
		_enabled = state;
		gameObject.SetActive(state);
	}
}
