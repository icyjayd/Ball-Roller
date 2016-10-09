﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityStandardAssets.CrossPlatformInput;

[RequireComponent (typeof (Health))]
public class PlayerController: MonoBehaviour {

	private bool isLocked, charging;
	private Camera cam;
	private CameraFollow camFollow;
	private float boostSpeed, normalSpeed, checkLock, lockOffTime, newLockTime, pseudoTime; 
	private CharacterController charCon;
	private int targeter;
	private Transform hand;
	private GameObject[] targets;
	private Weapon weapon;
	private Vector3 movement;
	private ParticleSystem particles;

	public Rigidbody rb;
	public PseudoPlayer pseudo;
	public Animator anim;
	public GameObject bullet, bomb, blast, target;
	public float speed = 150, newLockLimit = 1, rotationSpeed =1, meleeRange = 1, minEnemyDistance, minEnemyAltitudeDistance;
	public float boostMultiplier;
	public float lockLimit = 2;
	public bool isBoosted, moving, canBomb, canAttack = true, shielding = false, makingBomb = false, stalled = false;

	void Start (){
		rb = GetComponent<Rigidbody>();
		pseudoTime = Time.time;
		particles = GetComponentInChildren<ParticleSystem>();
		particles.startLifetime = 1f;
		//movement = Vector3.zero;
		charCon = GetComponent<CharacterController>();
		weapon = GetComponentInChildren<Weapon>();
		anim = GetComponent<Animator>();
		targeter = 0;
		lockOffTime = Time.time;
		newLockTime = Time.time;
		checkLock = Mathf.Round(CrossPlatformInputManager.GetAxis("LockOn"));
		isLocked = false;
		canBomb = true;
		target = null;

		normalSpeed = speed;
		boostSpeed = speed * boostMultiplier;
		camFollow = FindObjectOfType<CameraFollow>();
//		newY = 0;
	}
	void Update(){
		if(!stalled){
			ControlPlayer();
		}
		HandleAnimationLayer();
	}
	void FixedUpdate () {
		if(!stalled){
			charCon.Move(movement * speed * Time.deltaTime);
		}
		camFollow.AdjustDamping();
		pseudo.transform.position = transform.position;
		if(moving){
			pseudoTime = Time.time;
			if(!isLocked){
				transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), Time.deltaTime * 10);
				if(Input.GetAxis("Right Horizontal") != 0){
					AdjustPseudo();
				}
			}else{
					transform.LookAt(target.transform);
			}
		}else{
			if(isLocked){
				//AdjustPseudo();
			}
		}
		RightStick();

	}


	void ControlPlayer(){
		if(CanMove()){//can move while not charging or boosting
			GetMovement();
		}
		Charge();
		LockOn();
		Attack();

	}

	bool CanMove(){
		if(makingBomb){
			return false;
		}else if(charging){
			return false;
		} else if(isBoosted){
			return true;
		}else{
			return true;
		}
	}


	void GetMovement(){
		float moveY = 0;
		if(CrossPlatformInputManager.GetButton("Altitude")){
			moveY = CrossPlatformInputManager.GetAxis("Altitude");
		}
		float moveX = CrossPlatformInputManager.GetAxis ("Horizontal");
		float moveZ = CrossPlatformInputManager.GetAxis ("Vertical");
		movement = new Vector3 (moveX, 0, moveZ);
		anim.SetFloat("Velocity X", moveX);
		anim.SetFloat("Velocity Z", moveZ);
		//translate movement by the rotation along the y axis
		movement = pseudo.transform.TransformDirection(movement);
////		transform.rotation = Quaternion.Euler (0, newY, 0);
		movement = new Vector3(movement.x, moveY, movement.z);
		//TODO: fix altitude GLITCH


//		Vector3 newMove = transform.position + movement * speed * Time.deltaTime;
//		if(isLocked){
//			if(Mathf.Abs(newMove.y - target.transform.position.y) <= minEnemyAltitudeDistance && (Vector2.Distance(new Vector2(newMove.x, newMove.z), new Vector2(target.transform.position.x, target.transform.position.z)) <= minEnemyDistance)){
//		
//					print("getting here");
//					movement = new Vector3(0, 0, 0);
//			}
//		}
		if((moveX == 0 && moveY == 0 && moveZ == 0) && !isBoosted){
			moving = false;
			particles.startLifetime = 1;
		} else{//not moving
			moving = true;
			particles.startLifetime = 0.05f;
		}


	}
	void RightStick(){
		if(!isLocked){
			float rotateHorizontal = CrossPlatformInputManager.GetAxis("Right Horizontal");
			pseudo.transform.RotateAround (transform.localPosition, Vector3.up, rotateHorizontal * rotationSpeed);			
		}else{
			if(target){
				if(CrossPlatformInputManager.GetAxis("Right Horizontal") != 0){
					float checkNewLockTime = Time.time;
					if(checkNewLockTime - newLockTime > newLockLimit){
						if(CrossPlatformInputManager.GetAxis("Right Horizontal") > 0){
							targeter -= 1;
						}else if (CrossPlatformInputManager.GetAxis("Right Horizontal") < 0){
							targeter += 1;
						}

						HandleLock();
					}

				}
			}else{
				isLocked = false;
			}
			
		}
//		newY = transform.rotation.eulerAngles.y;

	}


	void Charge(){
		float boostCheck = CrossPlatformInputManager.GetAxis("Boost");
		if (boostCheck > 0 && !shielding){//on R2 button press
			if(!moving){//charge if stationary and not shielding
				charging = true;
				anim.SetBool("Charging", true);
			}else if(!isBoosted && moving && !charging){//boost if moving and not shielding and already boosted
				camFollow.anim.SetBool("Boosting", true);
				isBoosted = true;
				anim.SetBool("Dashing", true);
				charging = false;
				speed = boostSpeed;
			}
		} else {
			anim.SetBool("Dashing", false);
			isBoosted = false;
			charging = false;
			anim.SetBool("Charging", false);
			camFollow.anim.SetBool("Boosting", false);
			speed = normalSpeed;
		}
	}

	void HandleAnimationLayer(){
		if(isLocked){
			anim.SetLayerWeight(anim.GetLayerIndex("Not Locked On"), 0);
			anim.SetLayerWeight(anim.GetLayerIndex("Locked On"), 1);
		}else{
			anim.SetLayerWeight(anim.GetLayerIndex("Not Locked On"), 1);
			anim.SetLayerWeight(anim.GetLayerIndex("Locked On"), 0);
		}
	}
	void LockOn(){
		if(CanLockOn()){
			if(Mathf.Round(CrossPlatformInputManager.GetAxis("LockOn")) > 0){
				HandleLock();
			}
		} else{
			if(Mathf.Round(CrossPlatformInputManager.GetAxis("LockOn")) > 0 && isLocked){ //holding down button while locked on
				
				if(canLockOff()){
					lockOffTime = Time.time;
					target = null;
					isLocked = false;

					camFollow.SlowDamping();
				}
			}else{
				lockOffTime = Time.time;
			}
		}
		if (isLocked == true) {
			if(target){
				pseudo.transform.LookAt (target.transform);

			}
			else{
				isLocked = false;

				transform.LookAt(transform.forward);
				HandleLock();
			}
		}
	}

	private bool CanLockOn(){
		float lockOn =  Mathf.Round(CrossPlatformInputManager.GetAxis("LockOn"));
		if(lockOn != checkLock){
			checkLock = Mathf.Round(CrossPlatformInputManager.GetAxis("LockOn"));
			return true;
		}return false;

	}

	void HandleLock(){
		targets = Targets(GameObject.FindGameObjectsWithTag("Lockable"));
		if (isLocked == false) {//to lock on in the beginning
			if(targets.Length >0){
				camFollow.SlowDamping();
				target = targets[0];
				isLocked = true;

			}
		} else {// to switch targets;
			if(targets.Length > 1){
				if(Mathf.Round(CrossPlatformInputManager.GetAxis("LockOn")) > 0){
					if(target != targets[0]){
						target = targets[0];
					}else{
						target = targets[1];
					}
				}else if(CrossPlatformInputManager.GetAxis("Right Horizontal") != 0){
					int targetPos = System.Array.IndexOf(targets, target);
					if(CrossPlatformInputManager.GetAxis("Right Horizontal") > 0){
						if(targetPos == 0){
							target = targets[targets.Count() - 1];
						}else{
							target = targets[targetPos - 1];
						}
					}else{
						target = targets[targetPos + 1];
					}
					if(targeter < 0){//index out of range
						targeter = targets.Count() -1;

					}
					target = targets[targeter % targets.Count()];

					newLockTime = Time.time;
				}
			}
		}
	}

	private bool canLockOff(){
		float checkTime = Time.time;
		if(Mathf.Abs(checkTime - lockOffTime) >= lockLimit){
			lockOffTime = Time.time;
			return true;
		}
			return false;
	}


	private GameObject[] Targets(GameObject[] targetsList){//sorts game objects in an array from left to right
		GameObject[] sortedList;

		foreach(GameObject target in targetsList){
			target.GetComponent<LockableTarget>().setPositionFromPlayer(camFollow.gameObject);
		}
		sortedList = targetsList.ToList().OrderBy(go => go.GetComponent<LockableTarget>().positionFromPlayer).ToArray();
		return sortedList;
}



	void AllowShoot(){
		canAttack = true;

	}

	void Attack(){
		if(canAttack){
			if(CrossPlatformInputManager.GetButtonDown("Attack")){ 
				canAttack = false;
				if(isLocked){
					if(target.GetComponent<Enemy>()){//checks that the enemy is actually attackable
						if(Vector3.Distance(target.transform.position, transform.position) <= meleeRange){//Check if in melee
							MeleeAttack();
						}else{
							Shoot();
						}
					}else{
						Shoot();
					}
				}else{
					Shoot();
				}
			}else if(CrossPlatformInputManager.GetButtonDown("Subweapon")){//for grabbing and sub weapon use
				canAttack = false;
				UseSubweapon();
			}else if(CrossPlatformInputManager.GetButton("Shield")){//shielding
				canAttack = false;
				Shield();
			}
		} else{
			if(shielding){

				if(!CrossPlatformInputManager.GetButton("Shield")){
					shielding = false;
					anim.SetBool("Shielding", false);
					anim.SetBool("Dashing", false);
					canAttack = true;
					normalSpeed = normalSpeed * 2;

				}
			}
		}
	}

	void Shield(){
		shielding = true;
		anim.SetBool("Shielding", true);
		normalSpeed = normalSpeed/2;
		isBoosted = false;
	}

	void UseSubweapon(){
		Debug.Log("Subweapon used");
		canAttack = true;

	}
	void MeleeAttack(){
		if(!isBoosted && !charging){//stationary shot
			anim.SetTrigger("Begin Melee Combo");
			weapon.damage = weapon.comboDamage;
		} else if(charging && canBomb){
			anim.SetTrigger("Spin Slice");
			weapon.damage = weapon.burstDamage;
		} else if(isBoosted){
			anim.SetTrigger("Lunge");
			weapon.damage = weapon.dashDamage;
		}
		canAttack = true;

	}

	void OnWeaponsClash(){
		if(weapon.damage == weapon.comboDamage){
			anim.SetTrigger("Clash");
		}

	}
	void Shoot(){
		if(!isBoosted && !charging){//stationary shot
			anim.SetTrigger("Shoot Bullet");
		} else if(charging && canBomb){
			anim.SetTrigger("Shoot Bomb");
		} else if(isBoosted){
			anim.SetBool("Locking Blast", true);
			canAttack = true;
		}
	}
	public void LockBlast(){
		Enemy[] Enemies = GameObject.FindObjectsOfType<Enemy>();
		if(Input.GetButton("Attack")){
			foreach(Enemy enemy in Enemies){
				if(enemy.canBeHomedInOn){

				//TODO make some kind of UI lock-on thing
				}
			}
		}
		else{
			anim.SetBool("Locking Blast", false);
			ShootBlast(Enemies);
		}
	}

	void ShootBlast(Enemy[] EnemyArray){
		List<Blast> blasts = new List<Blast>();
		//sort enemies into lockable enemies;
		List<Enemy> lockableEnemies = new List<Enemy>();

		foreach(Enemy enemy in EnemyArray){
			if(enemy.GetComponentInChildren<Renderer>().isVisible){
				lockableEnemies.Add(enemy);
			}
		}
		//generate six blasts to start
		for(int i = 0; i <6; i++){
			GameObject shot = Instantiate(blast, transform.forward, Quaternion.identity) as GameObject;	
			shot.GetComponent<Projectile>().SetShooter(this.gameObject);
			shot.transform.Translate(Vector3.left * 2);
			blasts.Add(shot.GetComponent<Blast>());
		}
		if(lockableEnemies.Count < blasts.Count && lockableEnemies.Count != 0){//have multiple blasts on enemies if blsts outnumber enemies
			for(int i = 0; i < blasts.Count; i++){
				blasts[i].setTarget(lockableEnemies[i%lockableEnemies.Count]);
			}
		} else{
			while(blasts.Count < lockableEnemies.Count){//match the blast count to the enemies
				GameObject shot = Instantiate(blast, transform.forward, Quaternion.identity) as GameObject;	
				shot.GetComponent<Projectile>().SetShooter(this.gameObject);
				blasts.Add(shot.GetComponent<Blast>());
			}
			for(int i = 0; i < lockableEnemies.Count; i++){
				blasts[i].setTarget(lockableEnemies[i]);
			}
		}

	}
	void ShootBomb(){
		GameObject shot = Instantiate(bomb, transform.forward, Quaternion.identity) as GameObject;	
		shot.GetComponent<Projectile>().SetShooter(this.gameObject);
		canBomb = false;
		makingBomb = true;
	}

	void ShootBullet(){
		hand = GetComponentInChildren<Shooter>().transform;
		GameObject shot = Instantiate(bullet, hand.position + transform.forward, Quaternion.identity) as GameObject;
		shot.GetComponent<Projectile>().SetShooter(this.gameObject);
		Quaternion q = Quaternion.FromToRotation(Vector3.up, transform.forward);
		shot.transform.rotation = q * shot.transform.rotation;
		shot.GetComponent<Rigidbody>().AddForce(transform.forward * shot.GetComponent<Projectile>().speed, ForceMode.Impulse);

	}

	void AdjustPseudo(){
		Vector3 newDir = Vector3.RotateTowards(pseudo.transform.forward, transform.forward, Time.deltaTime * 5, 5f);
		pseudo.transform.rotation = Quaternion.LookRotation(newDir);
	}
}
