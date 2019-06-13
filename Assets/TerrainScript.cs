using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainScript : MonoBehaviour {
	// Start is called before the first frame update
	private TerrainData data;
	private Vector3 origin;
	void Start() {
		data = gameObject.GetComponent<TerrainCollider>().terrainData;
		origin = transform.localPosition;
	}

	public float GetHeightAt(Vector3 pos) {
		Vector3 terrainLocalPos = pos - origin;
		Vector2 normalizedPos = new Vector2(Mathf.InverseLerp(0.0f, data.size.x, terrainLocalPos.x),
				Mathf.InverseLerp(0.0f, data.size.z, terrainLocalPos.z));
		return data.GetInterpolatedHeight(normalizedPos.x, normalizedPos.y);
	}

	/*private void OnTriggerEnter (Collider other) {
		if (other.tag.Equals("Me")) {
			Controller.flying = false;
		}
	}*/
		// Update is called once per frame
	void Update() {

	}
}
