﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Quaternion = UnityEngine.Quaternion;

public class ControllerHelper : MonoBehaviour {
	// Start is called before the first frame update
	private Transform controllerMesh, circle;
	private int state = 0;
	private bool _visible = true;

	private static float rightZ => 1.2f;
	private static float leftZ => -1.2f; //Z scale for mesh
	private Quaternion showRightTriggerMeshRotation
		=> Quaternion.Euler(new Vector3(0.183f, -89.899f, 33.52f));
	private Vector3 showRightTriggerCirclePosition
		=> new Vector3(0.1966f, -1.6071f, 1.5196f);

	private Vector3 showLeftTriggerCirclePosition
		=> new Vector3(0.2401f, -1.6071f, 1.5196f);
	private Vector3 showRightTriggerCircleScale
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
		Vector3 meshScale = controllerMesh.localScale;
		meshScale.z = rightZ;
		controllerMesh.localScale = meshScale;
		circle.localPosition = showAButtonCirclePosition;
		circle.localScale = showAButtonCircleScale;
	}

	public void ShowBButton() {
		SetVisible(true);
		controllerMesh.localRotation = showAButtonMeshRotation;
		Vector3 meshScale = controllerMesh.localScale;
		meshScale.z = rightZ;
		controllerMesh.localScale = meshScale;
		circle.localPosition = showBButtonCirclePosition;
		circle.localScale = showAButtonCircleScale;
	}

	public void ShowRightTrigger() {
		SetVisible(true);
		Vector3 meshScale = controllerMesh.localScale;
		meshScale.z = rightZ;
		controllerMesh.localScale = meshScale;
		controllerMesh.localRotation = showRightTriggerMeshRotation;
		circle.localPosition = showRightTriggerCirclePosition;
		circle.localScale = showRightTriggerCircleScale;
	}

	public void ShowLeftTrigger() {
		SetVisible(true);
		Vector3 meshScale = controllerMesh.localScale;
		meshScale.z = leftZ;
		controllerMesh.localScale = meshScale;
		controllerMesh.localRotation = showRightTriggerMeshRotation;
		circle.localScale = showRightTriggerCircleScale;
		circle.localPosition = showLeftTriggerCirclePosition;
	}

	// Update is called once per frame
	void Update() {
	}
}
