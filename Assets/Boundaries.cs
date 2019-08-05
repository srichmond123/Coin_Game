using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Boundaries : MonoBehaviour {
	// Start is called before the first frame update
	private List<Transform> bounds;
	private Vector3 origin = Vector3.one * -1000f, scale = Vector3.one * Mathf.Infinity;
	private TerrainScript terrainScript;
	public const float Buffer = 1.08f; //Collision buffer (so user never goes through red boundaries)
	private int NumFish => 8;
	private int NumFloatingParticles => 10;
	private int NumBubbles => 14;
	private int NumJellyfish => 7;
	public GameObject fishPrefab, floatingParticles, bubblesPrefab, jellyPrefab;
	private List<GameObject> sceneryInstances; //To destroy at each round
	void Start() {
		bounds = new List<Transform>();
		foreach (Transform child in transform) {
			bounds.Add(child);
		}

		terrainScript = GameObject.Find("Terrain").GetComponent<TerrainScript>();
		sceneryInstances = new List<GameObject>();
	}

	// Update is called once per frame
	void Update() {
		
	}

	public Vector3 GetRandomPositionOnMap(float offsetY, float heightVariability) {
		Vector3 pos = Vector3.zero;
		pos.x = Random.value * scale.x + origin.x;
		pos.z = Random.value * scale.z + origin.z;
		pos.y = terrainScript.GetHeightAt(pos) 
		        + terrainScript.transform.localPosition.y
			+ offsetY + Random.Range(0, heightVariability);
		return pos;
	}

	public void SetScenery() {
		foreach (GameObject o in sceneryInstances) {
			Destroy(o);
		}

		for (int i = 0; i < NumFish; i++) {
			GameObject inst = Instantiate(fishPrefab);
			inst.transform.localPosition = GetRandomPositionOnMap(50f, 0f);
			sceneryInstances.Add(inst);
		}

		for (int i = 0; i < NumFloatingParticles; i++) {
			GameObject inst = Instantiate(floatingParticles);
			inst.transform.localPosition = GetRandomPositionOnMap(5f, 20f);
			sceneryInstances.Add(inst);
		}

		for (int i = 0; i < NumBubbles; i++) {
			GameObject inst = Instantiate(bubblesPrefab);
			inst.transform.localPosition = GetRandomPositionOnMap(0f, 5f);
			sceneryInstances.Add(inst);	
		}
		
		for (int i = 0; i < NumJellyfish; i++) {
			GameObject inst = Instantiate(jellyPrefab);
			inst.transform.localPosition = GetRandomPositionOnMap(12f, 15f);
			sceneryInstances.Add(inst);
		}
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
		const float yPos = 40f, yScale = 220f;
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
