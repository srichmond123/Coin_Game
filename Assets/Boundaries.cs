using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boundaries : MonoBehaviour {
	// Start is called before the first frame update
	private List<Transform> bounds;
	private Vector3 origin, scale;
	private TerrainScript terrainScript;
	private float Buffer => 0.18f; //Collision buffer (so user never goes through red boundaries)
	void Start() {
		bounds = new List<Transform>();
		foreach (Transform child in transform) {
			bounds.Add(child);
		}

		terrainScript = GameObject.Find("Terrain").GetComponent<TerrainScript>();
	}

	// Update is called once per frame
	void Update() {
			
	}

	public Vector3 Outside(Vector3 position) {
		float terrainY = terrainScript.GetHeightAt(position) + terrainScript.transform.localPosition.y;
		if (position.x < origin.x + Buffer)
			position.x = origin.x + Buffer;
		else if (position.x > origin.x + scale.x - Buffer)
			position.x = origin.x + scale.x - Buffer;
		if (position.z < origin.z + Buffer)
			position.z = origin.z + Buffer;
		else if (position.z > origin.z + scale.z - Buffer)
			position.z = origin.z + scale.z - Buffer;
		if (position.y > terrainY + 15f)
			position.y = terrainY + 15f;

		return position;
	}
	
	// Set 4 boundaries for x, z movement, and one upper bound:
	public void Set(Vector3 _origin, Vector3 _scale) {
		origin = _origin - Vector3.right * 1.2f - Vector3.forward * 1.2f; //Slight extra room
		scale = _scale + Vector3.right * 2.4f + Vector3.forward * 2.4f;
		const float yPos = 40f, yScale = 120f;
		Vector3 leftPos = origin + Vector3.forward * scale.z / 2;
		Vector3 leftScale = scale;
		leftScale.x = 0.01f;
		leftScale.y = yScale;
		bounds[0].localScale = leftScale;
		leftPos.y = yPos;
		bounds[0].position = leftPos;
		Vector3 rightPos = leftPos;
		Vector3 rightScale = leftScale;
		rightPos += Vector3.right * scale.x;
		bounds[1].position = rightPos;
		bounds[1].localScale = rightScale;
		Vector3 backPos = origin + Vector3.right * scale.x / 2;
		Vector3 backScale = scale;
		backScale.y = yScale;
		backScale.z = 0.01f;
		backPos.y = yPos;
		bounds[2].position = backPos;
		bounds[2].localScale = backScale;
		Vector3 frontScale = backScale;
		Vector3 frontPos = backPos;
		frontPos += Vector3.forward * scale.z;
		bounds[3].position = frontPos;
		bounds[3].localScale = frontScale;
	}
}
