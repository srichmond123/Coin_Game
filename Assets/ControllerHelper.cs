using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;

public class ControllerHelper : MonoBehaviour {
	// Start is called before the first frame update
	private Transform controllerMesh, circle;
	private const int TriggerState = 0, AButtonState = 1;
	private int state = 0;
	private bool _visible = true;

	private Quaternion showTriggerMeshRotation
		=> Quaternion.Euler(new Vector3(0.183f, -89.899f, 33.52f));
	private Vector3 showTriggerCirclePosition
		=> new Vector3(0.2034f, -1.5976f, 1.5155f);
	private Vector3 showTriggerCircleScale
		=> new Vector3(0.005000009f, 0.005000006f, 0.005000009f);

	private Quaternion showAButtonMeshRotation
		=> Quaternion.Euler(new Vector3(-60.035f, -26.215f, 34.261f));
	private Vector3 showAButtonCirclePosition
		=> new Vector3(0.2181f, -1.5757f, 1.5367f);
	private Vector3 showAButtonCircleScale
		=> new Vector3(0.002030354f, 0.002030354f, 0.002030354f);

	private Vector3 showBButtonCirclePosition
		=> new Vector3(0.2117f, -1.5625f, 1.5372f);

	void Start() {
		controllerMesh = transform.GetChild(0);
		circle = transform.GetChild(1);
		SetVisible(false);
	}

	public void SetVisible(bool visible) {
		if (_visible != visible) {
			controllerMesh.gameObject.SetActive(visible);
			circle.gameObject.SetActive(visible);
			_visible = visible;
		}
	}

	public void ShowAButton() {
		SetVisible(true);
		controllerMesh.localRotation = showAButtonMeshRotation;
		circle.localPosition = showAButtonCirclePosition;
		circle.localScale = showAButtonCircleScale;
	}

	public void ShowBButton() {
		SetVisible(true);
		controllerMesh.localRotation = showAButtonMeshRotation;
		circle.localPosition = showBButtonCirclePosition;
		circle.localScale = showAButtonCircleScale;
	}

	public void ShowTrigger() {
		SetVisible(true);
		controllerMesh.localRotation = showTriggerMeshRotation;
		circle.localPosition = showTriggerCirclePosition;
		circle.localScale = showTriggerCircleScale;
	}

	// Update is called once per frame
	void Update() {
	}
}
