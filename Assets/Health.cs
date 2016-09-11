﻿using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour {
	public float health = 1000;
	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	public void TakeDamage(float damage){
		health -= damage;
		if(health <= 0f){
			Destroy(gameObject);
		}
	}
}