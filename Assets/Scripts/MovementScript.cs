﻿using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class MovementScript : MonoBehaviour {

	private Rigidbody2D playerBody;//Tyler: I changed the collider to a circle (just so I could change the 'feet' to it.)
	//Tyler: Update on this collider thing. Not sure if it's used if I detect the ground a different way.
	private CircleCollider2D playerCollider;
	public Vector3 lastCheckpoint, startingPos;

	public float speed;
	public float jumpSpeed; //Tyler: Allowing me to change horizontal speed/jump height seperately.
	public Transform groundCheck; //Tyler: Had to change the 'IsTouchingLayers' to 'OverlapCircle'
	//so that it would detect moving surfaces as ground instead of glitching.
	public float groundRadius = 0.1f;//Variable used for the new ground checking thingy.

	public bool isFacingRight, isGrounded, movementEnabled;
	public LayerMask groundLayer;
	//public BurstJump boost; //locke

	private Vector2 boostSpeed = new Vector2(50,0); //locke
	public bool canBoost = true; //locke
	private float boostCooldown = 2f; //locke

	//Taylor: health object is used in SaveGameToLocal() and IsLoadingGame() to set/save the number of hearts when saved and loaded
	public HealthBarManager health;
	public GameObject[] checkpointArray;

	void Start () 
	{
		//Taylor: Fades screen in.
		GameObject.Find ("GameManager").GetComponent<FadeScript> ().LoadingLevel();

		playerBody = GetComponent<Rigidbody2D> ();
		playerCollider = GetComponent<CircleCollider2D> ();
		startingPos = gameObject.transform.position;
		lastCheckpoint = gameObject.transform.position;
		health = GetComponent<HealthBarManager> ();
		isFacingRight = true;
		isGrounded = true;
		movementEnabled = true;
		//Taylor: Checks at start to see if game is loading. If so, sets variables.
		checkpointArray = GameObject.FindGameObjectsWithTag("Checkpoint");
		IsLoadingGame();
	}

	void Update () 
	{
		//Taylor: Saves data every frame. I decided this would be better than creating a 
		//MovingScript object in SaveLoad and calling the save function.
		SaveGameToLocal ();

		//Tyler: Had to replace the way the ground was detected so it'd be more responsive on moving platforms.
		isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
		//isGrounded = Physics2D.IsTouchingLayers (playerCollider, groundLayer); //Just commenting this out.

		if (isGrounded && Input.GetKeyDown(KeyCode.Space))
			playerBody.velocity = new Vector2 (playerBody.velocity.x, jumpSpeed);

		if (!isGrounded && canBoost && Input.GetKeyDown(KeyCode.Space))
		{
			SoundManager.instance.playSoundEffect (0);
			StartCoroutine(Boost(0.15f));
		}

		if (Input.GetKeyDown (KeyCode.Q))
			SetDeath (true);
	}

	void FixedUpdate()
	{
		if(movementEnabled)
			Moving (Input.GetAxis ("Horizontal"), speed);
	}

	private void Moving(float horizontal, float speedVelocity)
	{
		playerBody.velocity = new Vector2 ((horizontal * speedVelocity), playerBody.velocity.y);
		Flip (horizontal);
	}

	private void Flip(float horizontal)
	{
		if((horizontal>0 && !isFacingRight) || (horizontal<0 && isFacingRight)){
			isFacingRight = !isFacingRight;
			Vector3 theFlipScale = transform.localScale;
			theFlipScale.x *= (-1);
			transform.localScale = theFlipScale;
		}
	}

	private void SetDeath(bool isDead)
	{
		if(isDead){
			SoundManager.instance.playSoundEffect (2);
			if(lastCheckpoint!=startingPos){
				HealthBarManager healthBarManager = GetComponent<HealthBarManager> ();
				healthBarManager.SendMessage ("Reset");
				playerBody.transform.position = lastCheckpoint;
			}else{
				SceneManager.LoadScene (SceneManager.GetActiveScene().buildIndex);
				playerBody.transform.position = startingPos;
			}
		}
	}

	public void SetCheckpointPosition(Vector3 checkpointPos)
	{
		lastCheckpoint = checkpointPos;
	}

	/*	Taylor:
		When player comes into contact with platform, sets the platform as the parent.
		Allows the player to move with the platform, rather than sliding off. */
	void OnCollisionEnter2D(Collision2D other)
	{
		if (other.transform.tag == "MovingPlatform")
		{
			transform.parent = other.transform;
		}
	}

	/*	Taylor:
		When player jumps off platform, removes platform as player.
		Keeps player from moving with platform while on the ground. */
	void OnCollisionExit2D(Collision2D other)
	{
		if (other.transform.tag == "MovingPlatform")
		{
			transform.parent = null;
		}
	}

	IEnumerator Boost(float boostDuration)
	{
		float time = 0;
		canBoost = false;

		while (boostDuration > time && movementEnabled == true)
		{
			time += Time.deltaTime;
			GetComponent<Rigidbody2D>().velocity = boostSpeed;

			if (isFacingRight == true)
				GetComponent<Rigidbody2D>().AddForce(new Vector2(speed * 200, 0));
			else if (isFacingRight == false)
				GetComponent<Rigidbody2D>().AddForce(new Vector2(-speed * 200, 0));
            
			yield return 1;
		}

		yield return new WaitForSeconds(boostCooldown);
		canBoost = true;
	}
	/*	Taylor:
		Checks to see if load function has been called
		In SaveLoad.cs, LoadScene is set to true when activated
		Saved data is copied to the GameManagers localData
		Health object uses setupScene to number of hearts and the gui
		Moves player to saved position
		Sets LoadingScene to false to indicate loading is over */
	public void IsLoadingGame()
	{
		if (SaveLoad.Instance.LoadingScene) 
		{
			GameManager.Instance.localData = SaveLoad.Instance.LocalData;
			health.SetupScene(SaveLoad.Instance.LocalData.Health); 
			lastCheckpoint = new Vector3 (SaveLoad.Instance.LocalData.LastCheckpointX, 
				SaveLoad.Instance.LocalData.LastCheckpointY);

			for (int i = 0; i < checkpointArray.Length; i++)
			{
				if (lastCheckpoint.x >= checkpointArray[i].transform.position.x)
				{
					Destroy (checkpointArray[i]);
				}
			}

			transform.position = lastCheckpoint;
			SaveLoad.Instance.LoadingScene = false;
		}
	}

	/*	Taylor:
	 	Saves scene index, # of hearts, and x/y position to localData
		The if statment prevents the location from saving when on the moving platform
		This is because the other in game objects aren't saved.
		If you were able to save while on the moving platform, then loaded it, you could fall to your death  */
	public void SaveGameToLocal()
	{
		GameManager.Instance.localData.SceneIndex = SceneManager.GetActiveScene ().buildIndex;
		GameManager.Instance.localData.Health = health.numberOfHearts;

		GameManager.Instance.localData.LastCheckpointX = lastCheckpoint.x;
		GameManager.Instance.localData.LastCheckpointY = lastCheckpoint.y;

	}


}





