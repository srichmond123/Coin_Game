using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoinScript : MonoBehaviour {
    // Start is called before the first frame 
    private string id;
    public int index = -1;
    private CoinManager parent;
    private bool collided = false;
    
    void Start() {
    }

    public void SetColor(Color c) {
        transform.GetChild(0).GetComponent<Renderer>().material.color = c;
    }

    public string GetId() {
        return id;
    }

    public void SetParent(CoinManager parent) {
        this.parent = parent;
    }

    private void OnTriggerStay(Collider other) {
        if (!collided) {
            if (other.gameObject.tag.Equals("Me") && Controller.myId == id) { 
                //parent.Collect(index);
                parent.Collect(index);
                collided = true;
            }
        }
    }


    public void SetId(string id) {
        this.id = id;
    }

    // Update is called once per frame
    void Update() {
        
    }
}
