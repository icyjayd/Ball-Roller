﻿using UnityEngine;
using System.Collections;

public class Environment : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	void OnCollisionEnter(Collision collider){
		
		if(collider.gameObject.GetComponent<Projectile>()){
			Destroy(collider.gameObject);

		}
	}
}
