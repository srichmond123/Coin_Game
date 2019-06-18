using System;
using System.Collections;
using System.Collections.Generic;
using SocketIO;
using UnityEngine;

[Obsolete("Not a race anymore. Will probably delete this class in a later commit.")]
public class Endzone : MonoBehaviour {
    // Start is called before the first frame update
    private SocketIOComponent socket;
    public static bool Finished = false;
    public static int OthersFinished = 0;
    void Start() {
        GameObject sockObject = GameObject.Find("SocketIO");
        socket = sockObject.GetComponent<SocketIOComponent>(); 
    }
 
    private void OnTriggerEnter(Collider other) {
        if (other.tag.Equals("Me") && Finished == false) {
            socket.Emit("finish");
            Finished = true;
        }
        else if (other.tag.Equals("Not Me")) {
            OthersFinished++; //TODO this should probably be refined (prevent reentries)
            //TODO Scoreboard / info board and minimap should have this info
        }
    }

    // Update is called once per frame
    void Update() {
        
    }
}
