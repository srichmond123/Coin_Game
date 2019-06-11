﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CoinScript : MonoBehaviour {
    // Start is called before the first frame 
    private string id;
    public int index = -1;
    private CoinManager parent;
    private bool collided = false;
    private float albedo = 1f;
    private float speed = 0f;
    
    void Start() {
        speed = Random.Range(170f, 190f);
    }

    public void SetAlbedo(float albedo) {
        Color curr = transform.GetChild(0).GetComponent<Renderer>().material.color;
        curr.a = albedo;
        transform.GetChild(0).GetComponent<Renderer>().material.color = curr;
        this.albedo = albedo;
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
            if (other.gameObject.tag.Equals("Me") 
                && Controller.myId == id
                && Buckets.GetCoinsHeld() < 1) {
                parent.Collect(index);
                collided = true;
            }
        }
    }


    public void SetId(string id) {
        this.id = id;
    }
    
    private void rotate(float amount) {
        transform.Rotate(transform.up, amount);
    }

    // Update is called once per frame
    void Update() {
        if (albedo < 1f) {
            SetAlbedo(Math.Min(1f, albedo + Time.deltaTime / 4.5f));
        }

        rotate(Time.deltaTime * speed);
    }
}
