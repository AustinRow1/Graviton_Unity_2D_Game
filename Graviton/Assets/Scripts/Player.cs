/************************************************************************
** Filename: 	Player.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Handles user input to control player.
**				Handles player spawning and gravity direction.
**				Contains settings for player's jump hieght and time, 
**				move speed, and acceleration.
**				Handles player's display and animation.
**				Allows for enabling/disabling user control of player.
** Problems:	Currently the immediate flip during a gravity change
**				can--depending on the surrounding environment--result in 
**				the overlap of the player's collider with that of something
**				in the physics layer which can cause slightly erratic movement 
**				for a couple frames. I will fix this soon.
** Functions:
**				void Awake();
**				void Start();
**				void Update();
**				void handle_movement();
**				void gravity_adjusted_input(ref float, ref Keycode);
**				void handle_display();
**				void flip();
**				public int gravity_direction();
**				public void change_gravity(int);
**				void rotate_player(int);
**				void tranfer_momentum(int);
**				void disable_control();
**				void enable_control();
**				public void respawn();
**				IEnumerator _respawn();
**				public void set_spawn(Transform, int);
************************************************************************/
using UnityEngine;
using System.Collections;

[RequireComponent (typeof(Player_Controller_2D))]
public class Player : MonoBehaviour {

	public float jump_height = 4f;
	public float time_to_jump_apex = 0.4f;

	public float move_speed = 5f;
	private Player_Controller_2D controller;
	public Vector3 velocity = Vector3.zero;
	private float velocity_x_smoothing;
	public float grounded_acceleration_time = 0.1f;
	public float airborne_acceleration_time = 0.2f;

	public float gravity;
	private int _gravity_direction = 0;		//0-down, 1-left, 2-up, 3-right
	private float jump_velocity;
	public float max_fall_speed = 1f;
	public float target_velocity_x = 0;

	private Animator character_anim;
	public bool start_facing_right = true;
	public bool facing_right = true;
	private SpriteRenderer sprite_renderer;
	private bool user_control_enabled = true;
	private BoxCollider2D box_collider;
	private bool respawning = false;

	private Vector3 spawn_position;
	private int spawn_gravity = 0;

	/**************************************************************************************************
	 *  Function: 	 void Awake()
	 *  Description: Called at creation of script. Sets strength of gravity and jump_velocity based
	 * 				 on time it takes and reach jump apex and jump height, both of which are supplied
	 * 				 in inspector. Also gets references to various components on player's gameobject.
	 *************************************************************************************************/
	void Awake(){
		gravity = (2 * jump_height) / Mathf.Pow (time_to_jump_apex, 2);
		jump_velocity = Mathf.Abs (gravity) * time_to_jump_apex;

		box_collider = GetComponent<BoxCollider2D> ();
		sprite_renderer = GetComponent<SpriteRenderer> ();
		character_anim = GetComponent<Animator> ();
		controller = GetComponent<Player_Controller_2D> ();
	}

	/***********************************************************************************************
	 *  Function: 	 void Start()
	 *  Description: Called at start of script. Ensures player is facing the proper direction upon
	 * 				 initial spawn as indicated by the game controller script and sets initial
	 * 				 gravity direction.
	 **********************************************************************************************/
	void Start(){
		if (!start_facing_right)
			flip ();
		_gravity_direction = spawn_gravity;
	}

	/***********************************************************************************************
	 *  Function: 	 void Update()
	 *  Description: Called every frame. If player is not currently respawning, calls functions
	 * 				 to affect player's display/animation and movement.
	 **********************************************************************************************/
	void Update(){
		if (!respawning) {
			handle_display ();
			handle_movement ();
		}
	}
		
	/***********************************************************************************************
	 *  Function: 	 void handle_movement()
	 *  Description: Makes changes to player's velocity based on user input and collisions detected
	 * 				 by the Player_Controller_2D.cs script then moves player using the 2D 
	 * 				 controller's move(Vector3) function.
	 **********************************************************************************************/
	void handle_movement(){
		if (controller.collision_info.above || controller.collision_info.below)		//If player just had a vertical collision, velocity should be set to zero.
			velocity.y = 0f;

		float input_x = 0;

		if (user_control_enabled) {
			KeyCode alpha_jump_key = KeyCode.W;
			gravity_adjusted_input (ref input_x, ref alpha_jump_key);

			if ((Input.GetKeyDown (KeyCode.Space) || Input.GetKeyDown (alpha_jump_key)) && !controller.collision_info.on_steep_slope) {
				if (controller.collision_info.below)
					velocity.y = jump_velocity;
				else if (controller.collision_info.stepping)	//This ensures that you can still jump if failing to step because of roof.
				velocity.y += jump_velocity;
			}
		}
			
		target_velocity_x = input_x * move_speed;	//Accelerate to target x velocity with different airborne and grounded accelerations.
		velocity.x = Mathf.SmoothDamp (velocity.x, target_velocity_x, ref velocity_x_smoothing, controller.collision_info.below ? grounded_acceleration_time : airborne_acceleration_time);

		if (velocity.y - gravity * Time.deltaTime >= -max_fall_speed)	//Limits the player's falling velocity.
			velocity.y -= gravity * Time.deltaTime;
		else
			velocity.y = -max_fall_speed;

		controller.max_fall_speed = max_fall_speed * Time.deltaTime;
		controller.move (velocity * Time.deltaTime);
	}
		
	/***********************************************************************
	 *  Function: 	 void gravity_adjusted_input(ref float, ref Keycode)
	 *  Description: Takes input for x movement and alpha key jumping from
	 * 				 certain keys based on the player's current gravity 
	 * 				 direction.
	 **********************************************************************/
	void gravity_adjusted_input(ref float input_x, ref KeyCode alpha_jump_key){
		if (_gravity_direction == 0) {
			alpha_jump_key = KeyCode.W;
			input_x = Input.GetAxisRaw ("Horizontal");
		} else if (_gravity_direction == 1) {
			alpha_jump_key = KeyCode.D;
			input_x = Input.GetAxisRaw ("Vertical") * -1f;
		} else if (_gravity_direction == 2) {
			alpha_jump_key = KeyCode.S;
			input_x = Input.GetAxisRaw ("Horizontal") * -1f;
		} else if (_gravity_direction == 3) {
			alpha_jump_key = KeyCode.A;
			input_x = Input.GetAxisRaw ("Vertical");
		}
	}


	/***********************************************************************
	 *  Function: 	 void handle_display()
	 *  Description: Controls which direction the player is facing as well
	 * 				 as the player's animation state.
	 **********************************************************************/
	void handle_display(){
		if ((velocity.x < 0 && facing_right) || (velocity.x > 0 && !facing_right))
			flip ();

		if ((!controller.collision_info.below || controller.collision_info.on_steep_slope) && !controller.collision_info.stepping)
			character_anim.SetBool ("isJumping", true);
		else {
			character_anim.SetBool ("isJumping", false);

			if((Mathf.Abs(velocity.x) > 2f || target_velocity_x != 0f) && !controller.collision_info.on_steep_slope)
				character_anim.SetBool ("isWalking", true);
			else
				character_anim.SetBool ("isWalking", false);
		}
	}

	/***********************************************************************
	 *  Function: 	 void flip()
	 *  Description: Flips direction player is facing over local y-axis.
	 **********************************************************************/
	void flip(){
		Vector3 scale = transform.localScale;
		scale.x *= -1;
		transform.localScale = scale;
		facing_right = !facing_right;
	}

	/************************************************************
	 *  Function: 	 public int gravity_direction()
	 *  Description: Returns player's current gravity direction.
	 ***********************************************************/
	public int gravity_direction(){
		return _gravity_direction;
	}

	/*******************************************************************************************
	 *  Function: 	 public void change_gravity(int)
	 *  Description: Changes direction of gravity affecting player to indicated new direction.
	 ******************************************************************************************/
	public void change_gravity(int new_direction){
		rotate_player(new_direction);
		transfer_momentum (new_direction);
		_gravity_direction = new_direction;
	}
		
	/************************************************************************************ 
	*  Function: 	void rotate_player(int)
	*  Description: Rotates the gameobject so that it is oriented appropriately for the 
	* 		   		current direction of gravity (e.g. if gravity is pulling to the 
	* 		   		right, gameobjects feet/bottom will face right).
	************************************************************************************/
	void rotate_player(int new_direction){
		Quaternion target;

		if (new_direction == 0) //down
			target = Quaternion.Euler (0f, 0f, 0f);
		else if(new_direction == 1)	//left
			target = Quaternion.Euler (0f, 0f, -90f);
		else if(new_direction == 2)	//up
			target = Quaternion.Euler (0f, 0f, 180f);
		else  //right
			target = Quaternion.Euler (0f, 0f, 90f);

		transform.rotation = target;
	}

	/************************************************************************************** 
	*  Function: 	void transfer_momentum(int)
	*  Description: Adjusts velocity so that momentum is conserved through gravity change.
	**************************************************************************************/
	void transfer_momentum(int new_direction){
		if (new_direction % 2 == _gravity_direction % 2) {	//180-degree gravity change
			velocity.x *= -1;
			velocity.y *= -1;
		} else if ((new_direction == 1 && _gravity_direction == 0) || (new_direction == 0 && _gravity_direction == 3) || (new_direction == 3 && _gravity_direction == 2) || (new_direction == 2 && _gravity_direction == 1)) {	//clockwise gravity change
			float original_x = velocity.x;
			velocity.x = -velocity.y;
			velocity.y = original_x;
		} else {	//counterclockwise gravity change
			float original_x = velocity.x;
			velocity.x = velocity.y;
			velocity.y = -original_x;
		}
	}

	/******************************************************************************* 
	*  Function: 	void disable_control()
	*  Description: Disables user's ability to control player.
	*******************************************************************************/
	public void disable_control(){
		user_control_enabled = false;
	}

	/******************************************************************************* 
	*  Function: 	void enable_control()
	*  Description: Enables user's ability to control player.
	*******************************************************************************/
	public void enable_control(){
		user_control_enabled = true;
	}

	/************************************************************************************************ 
	*  Function: 	public void respawn_player()
	*  Description: Function used by other scripts for starting the respawn coroutine to respawn
	* 		   		player. Stops previous respawn coroutine if there is one running as a precaution
	* 		   		so that the character flashing part of the respawn doesn't overlap between two 
	* 		   		coroutines.
	************************************************************************************************/ 
	public void respawn(){
		StopCoroutine ("_respawn");
		StartCoroutine ("_respawn");
	}

	/********************************************************************************************** 
	*  Function: 	public void respawn()
	*  Description: Coroutine to respawn player at last saved checkpoint.
	**********************************************************************************************/ 
	IEnumerator _respawn(){
		respawning = true;	//Makes it so that handle_movement() and handle_display() won't run in Update().
		disable_control ();
		box_collider.enabled = false;
		sprite_renderer.enabled = false;
		yield return new WaitForSeconds (0.5f);		//Give a pause so that camera will remain focused on place where player died before focusing on respawn point.
		velocity = Vector3.zero;
		change_gravity (spawn_gravity);
		transform.position = spawn_position;
		sprite_renderer.enabled = true;
		box_collider.enabled = true;
		respawning = false;

		//Make sprite renderer flash to indicate player has just respawned.
		for (int i = 0; i < 8; i++) {
			sprite_renderer.enabled = !sprite_renderer.enabled;
			yield return new WaitForSeconds (0.15f);
			if (i == 4)	//Not giving player control back right away.
				enable_control ();	
		}

		sprite_renderer.enabled = true; //Protection against above loop condition being changed to something that leaves player sprite disabled.
	}

	/********************************************************************************************* 
	*  Function: 	public void set_spawn(Transform, int)
	*  Description: Function used by other scripts to save a new spawn point (used when player
	* 		   		reaches a new checkpoint). Also saves gravity direction at time of checkpoint
	* 				for when player spawns/respawns at the newly saved spawn point.
	*********************************************************************************************/ 
	public void set_spawn(Transform new_spawn, int direction){
		spawn_position = new_spawn.position;
		spawn_gravity = direction;
	}
}
