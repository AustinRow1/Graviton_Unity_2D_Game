/**
 **
 **		NOTE: THIS SCRIPT IS OUTDATED SINCE THE TRANSITION FROM RIGIBODY-BASED
 ** 		  PHYSICS TO RAYCAST-BASED PHYSICS. IT IS NOT BEING USED CURRENTLY.
 **
 **/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {

	public float speed = 1f;
	public float jumpForce = 1f;
	public bool user_control_enabled = true;
	private float moveX = 0f;
	private bool facing_right = true;
	public bool start_facing_right = true;
	private Rigidbody2D rb;
	private DynamicGravity dm;
	private Animator character_anim;
	private bool grounded;
	public Transform[] groundchecks;
	public BoxCollider2D sides_collider;
	private SpriteRenderer sprite_renderer;
	public CircleCollider2D[] circle_colliders;

	private Vector3 spawn_position;
	private int spawn_direction;
	private bool is_active = true;
	public Transform start;


	/**************************************************************************************************** 
	*  Function: 	void Awake()
	*  Description: Function runs at initialization of script. Gets references to player's rigidbody and
	* 		   		animator player's DynamicGravity script.
	****************************************************************************************************/ 
	void Awake(){
		circle_colliders = gameObject.GetComponents<CircleCollider2D> ();
		sprite_renderer = gameObject.GetComponent<SpriteRenderer> ();
		sides_collider = gameObject.GetComponent<BoxCollider2D> ();
		rb = GetComponent<Rigidbody2D> ();
		dm = GetComponent<DynamicGravity> ();
		character_anim = GetComponent<Animator>();
	}

	void Start(){
		if (!start_facing_right)
			flip ();

		dm.change_gravity (spawn_direction);
	}

	/***************************************************************************************** 
	*  Function: 	void Update()
	*  Description: Every frame gets input from keyboard (if enabled) for moving and jumping and 
	* 		   		updates player's display.
	******************************************************************************************/ 
	void Update(){
		grounded = is_grounded ();
		if (user_control_enabled)
			get_movement_input ();
		else
			moveX = 0f;
		display ();

		if (grounded) {
			character_anim.SetBool ("isJumping", false);
			if (user_control_enabled && Input.GetKeyDown (KeyCode.Space))
				jump ();
		}
	}

	/********************************************************************************************** 
	*  Function: 	void FixedUpdate()
	*  Description: Runs every frame. Makes player jump if signaled. Affects player movement based
	* 		   		on keyboard input. Affects player movement differently based on whether player
	* 		   		is falling or not.
	**********************************************************************************************/ 
	void FixedUpdate(){
		if (is_active) {
			if (dm.gravity_direction() == 0 || dm.gravity_direction() == 1)
				rb.velocity = new Vector2 (speed * moveX, rb.velocity.y);
			else if (dm.gravity_direction() == 2 || dm.gravity_direction() == 3)
				rb.velocity = new Vector2 (rb.velocity.x, -speed * moveX);
		} else
			rb.velocity = Vector2.zero;
	}
		
	/*********************************************************************************************** 
	*  Function: 	void get_movement_input()
	*  Description: Gets keyboard input from appropriate axis based on gravity direction. If player
	* 		   		is trying to run into wall, makes there movement in that direction 0 so they
	* 		   		can't (prevents running up faces that are steep but not entirely vertical).
	***********************************************************************************************/ 
	void get_movement_input(){
		if (dm.gravity_direction() == 0 || dm.gravity_direction() == 1)
			moveX = Input.GetAxis ("Horizontal");
		else
			moveX = -Input.GetAxis ("Vertical");
		
		if (moveX != 0) {
			Vector2 move_direction = (dm.gravity_direction() != 3) ? adjusted_vector2 (new Vector2 (moveX, 0f)) : adjusted_vector2 (new Vector2 (-moveX, 0f));	//fixed problem where it would check the opposite direction when gravity was going right
			if (will_hit_wall (move_direction))	
				moveX = 0f;
		}
	}

	/***************************************************************************************** 
	*  Function: 	void display()
	*  Description: Flips player sprite and collider if they need to be flipped and sets
	* 		   		appropriate animation.
	******************************************************************************************/ 
	void display(){
		if (needs_flip ())
			flip ();

		if (!grounded)
			character_anim.SetBool ("isJumping", true);
		else if (Mathf.Abs(adjusted_vector2 (rb.velocity).x) > 0.2f)
			character_anim.SetBool ("isWalking", true);
		else if (moveX == 0)	//It may seem redundant to have moveX condition here seeing as the velocity was checked already, but including it prevents brief idle animation during running direction change.
			character_anim.SetBool ("isWalking", false);
	}

	/***************************************************************************************** 
	*  Function: 	bool needs_flip()
	*  Description: Returns whether player sprite and collider needs to be flipped.
	******************************************************************************************/ 
	bool needs_flip(){
		if (((dm.gravity_direction() == 0 || dm.gravity_direction() == 2) && ((facing_right && moveX < 0) || (!facing_right && moveX > 0)))
			|| ((dm.gravity_direction() == 1 || dm.gravity_direction() == 3) && ((facing_right && moveX > 0) || (!facing_right && moveX < 0))))
			return true;
		return false;
	}

	/***************************************************************************************** 
	*  Function: 	void flip()
	*  Description: Flips player sprite and collider over y axis.
	******************************************************************************************/ 
	void flip(){
		Vector3 scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;
		facing_right = !facing_right;
	}

	/***************************************************************************************** 
	*  Function: 	void jump()
	*  Description: Makes player jump and starts jumping animation.
	******************************************************************************************/ 
	void jump(){ 
		if(grounded) {
			rb.AddForce(adjusted_vector2 (new Vector2(rb.velocity.x, jumpForce*10)));
			character_anim.SetBool ("isJumping", true);
		}
	}

	/********************************************************************************************** 
	*  Function: 	void is_grounded()
	*  Description: Returns true if any raycasts downward (downward being the current direction of
	* 		   		gravity) from ground check points hit gameobject that is part of the 
	* 		   		PhysicsLayer.
	**********************************************************************************************/ 
	bool is_grounded(){
		float check_dist = 0.2f;
		for (int i = 0; i < groundchecks.Length; i++) {
			Vector2 ground_direction = adjusted_vector2 (new Vector2 (0f, -check_dist));
			if (Physics2D.Raycast (groundchecks [i].position, ground_direction, check_dist, 1 << LayerMask.NameToLayer ("PhysicsLayer"))) {
				//Debug.DrawRay (groundchecks [i].position, new Vector3 (-check_dist,0, 0), Color.green, 1f);
				return true;
			}
		}
		return false;
	}

	/******************************************************************************************* 
	*  Function: 	bool will_hit_wall()
	*  Description: Returns true moving in the provided direction will result in running into a
	* 		  		wall. False otherwise.
	*******************************************************************************************/ 
	bool will_hit_wall(Vector2 move_direction){
		Debug.DrawRay (transform.position, move_direction.normalized*0.5f, Color.green, 1f);
		return Physics2D.Raycast (transform.position, move_direction, sides_collider.bounds.extents.x + 0.15f, 1 << LayerMask.NameToLayer ("PhysicsLayer"));
	}

	/************************************************************************************* 
	*  Function: Vector2 adjusted_vector2(Vector2 standard)
	*  Description: Returns vector2 that is anologous to provided vector but for current
	* 		   		gravity. For example, if providing the Vector2 (0f, -1f), this is 
	* 		   		like saying "go down one". However, if gravity is working to the
	* 		   		left, then the downward direction is to the left and so the function
	* 		   		would return (-1f, 0f) to account for left being down.
	*************************************************************************************/
	Vector2 adjusted_vector2(Vector2 standard){
		if (dm.gravity_direction() == 0)
			return standard;
		else if (dm.gravity_direction() == 1) 
			return new Vector2 (standard.x, -standard.y);
		else if (dm.gravity_direction() == 2) 
			return new Vector2 (standard.y, -standard.x);
		else
			return new Vector2 (-standard.y, standard.x);
	}

	public void disable_control(){
		user_control_enabled = false;
	}

	public void enable_control(){
		user_control_enabled = true;
	}

	/********************************************************************************************** 
	*  Function: 	public void respawn_player()
	*  Description: Function used by other scripts for starting the respawn coroutine to respawn
	* 		   		player. Stops previous respawn coroutine if there is one running so that the 
	* 		   		character flashing part of the respawn doesn't overlap between two coroutines.
	**********************************************************************************************/ 
	public void respawn(){
		StopCoroutine ("_respawn");
		StartCoroutine ("_respawn");
	}

	/************************************************************************************************ 
	*  Function: 	public void respawn()
	*  Description: Coroutine to respawn player at last saved checkpoint. First, destroys current
	* 		   		player object. Then spawns new player object at last saved spawn_point. Then
	* 		   		makes player flash several times by disabling and enabling the player object's
	* 		   		sprite renderer.
	************************************************************************************************/ 
	IEnumerator _respawn(){
		disable_control ();
		deactivate ();
		yield return new WaitForSeconds (0.25f);
		transform.position = spawn_position;
		activate ();
		dm.change_gravity (spawn_direction);	//must happen after player set active again for change to take effect

		for (int i = 0; i < 8; i++) {
			sprite_renderer.enabled = !sprite_renderer.enabled;
			yield return new WaitForSeconds (0.15f);
			if (i == 4)
				enable_control ();
		}

		sprite_renderer.enabled = true; //Just an extra precaution to make sure player is visible at the end of the respawn.
	}
	
	void deactivate(){
		sprite_renderer.enabled = false;
		sides_collider.enabled = false;
		for (int i = 0; i < circle_colliders.Length; i++)
			circle_colliders [i].enabled = false;
		dm.enabled = false;
		is_active = false;
	}

	void activate(){
		sprite_renderer.enabled = true;
		sides_collider.enabled = true;
		for (int i = 0; i < circle_colliders.Length; i++)
			circle_colliders [i].enabled = true;
		dm.enabled = true;
		is_active = true;
	}

	/********************************************************************************************* 
	*  Function: 	public void set_spawn(Transform, int)
	*  Description: Function used by other scripts to save a new spawn point (used when player
	* 		   		reaches a new checkpoint). Also saves a new gravity direction for when player
	* 		   		spawns/respawns at the newly saved spawn point.
	*********************************************************************************************/ 
	public void set_spawn(Transform new_spawn, int direction){
		spawn_position = new_spawn.position;
		//change_gravity sets the player's rotation correctly when they respawn, no need to set anything but position here. Would give NullReferenceException anyway when GameManager attemtps to instantiate player obect and set_spawn
		spawn_direction = direction;
	}
}
