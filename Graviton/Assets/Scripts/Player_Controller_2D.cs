/************************************************************************
** Filename: 	Player_Controller_2D.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Uses raycasts to handle player physics. Currently handles
**				standard collisions, ascending and descending slopes, 
**				sliding down slopes steeper than designated traversable
**				angle, and climbing steps. 
** Problems: 	Steep slope sliding does not work perfectly at the moment,
**				acceleration is too fast because slowing it causes an 
**				issue with initial interaction which leads to bumping down
**				the slope rather than sliding. Looking to fix this as I
**				move forward.
** Functions:	
**				void Awake();
**				public void move(Vector3);
**				void vertical_collisions(ref Vector3);
**				void horizontal_collisions(ref Vector3);
**				void limit_fall_velocity(ref Vector3);
**				void ascend_slope(ref Vector3, float);
**				void descend_slope(ref Vector3);
**				bool slope_close_enough(float, float, Vector3);
**				float relative_slope_direction(RaycastHit2D);
**				void steep_slope_slide(ref Vector3, float, float);
**				float relative_x_input();
**				void handle_descent(ref Vector3, float, float, float);
**				DEPRECATED-->limit_slope_descent_velocity(ref Vector3, float);
**				void update_references();
**				void updated_raycast_origins();
**				void calculate_ray_spacing();
************************************************************************/
using UnityEngine;
using System.Collections;

[RequireComponent (typeof(BoxCollider2D))]
[RequireComponent (typeof(Player))]
public class Player_Controller_2D : MonoBehaviour {

	struct Raycast_Origins{
		public Vector2 top_left, top_right;
		public Vector2 bottom_left, bottom_right;
	}

	struct No_Rotation_Raycast_Origins{
		public Vector3 top_left, top_right;
		public Vector3 bottom_left, bottom_right;
	}

	public struct Collision_Info{
		public bool above, below;
		public bool left, right;
		public bool ascending_slope, descending_slope, on_steep_slope, stepping;
		public float slope_angle, old_slope_angle, slope_slide_distance;
		public float old_y_velocity;

		//public bool was_on_steep_slope;	//for testing purposes

		public void reset(Vector3 velocity){
			above = below = false;
			left = right = false;
			ascending_slope = descending_slope = stepping = false;
			old_slope_angle = slope_angle;
			slope_angle = 0;

			//was_on_steep_slope = on_steep_slope;	//for testing purposes

			if (!on_steep_slope)
				slope_slide_distance = 0;

			on_steep_slope = false;
			old_y_velocity = velocity.y;
		}
	}

	public LayerMask physics_layer;

	[SerializeField]
	private int vertical_ray_count = 4;		//number of rays being used to detect vertical collisions
	[SerializeField]
	private int horizontal_ray_count = 4;	//number of rays being used to detect horizontal collisions
	private float vertical_ray_spacing;
	private float horizontal_ray_spacing;
	private const float skin_width = 0.06f;

	private No_Rotation_Raycast_Origins no_rotation_raycast_origins;	//location of raycast origins if player were not rotated at all
	private Raycast_Origins raycast_origins;							//location of raycast origins taking into account player's rotation
	public Collision_Info collision_info;
	private BoxCollider2D box_collider;
	public float max_fall_speed = 1f;
	public float max_traversable_angle = 60f;	//maximum slope angle that player can ascend/descend
	public float max_step_height = 0.1f;
	public float steep_slope_friction = 0.5f;	//friction used for affecting speed at which player accelerates when sliding down a steep/non-traversable slope
	private Player handler;

	/******************************************************************************************** 
	* Function:    void Awake()
	* Description: Called at creation of script. Gets reference to player gameobject components,
	* 		  	   updates reference vectors used for determining locations of corners of box
	* 		       collider and sets spacing needed for even distribution of collision rays.
	********************************************************************************************/
	void Awake(){
		handler = GetComponent<Player> ();
		box_collider = GetComponent<BoxCollider2D> ();
		update_references ();
		calculate_ray_spacing ();
	}

	/***************************************************************************************** 
	* Function:    public void move(Vector3)
	* Description: Moves player based on player's velocity using raycast collision detection.
	******************************************************************************************/
	public void move(Vector3 velocity){
		collision_info.reset (velocity);
		update_raycast_origins ();

		if (velocity.y <= 0)
			descend_slope (ref velocity);

		if (velocity.x != 0)
			horizontal_collisions (ref velocity);
		if (velocity.y != 0 && !collision_info.descending_slope)	//!descending_slope condition needed to eliminate collision detection stopping y movement on slope descents
			vertical_collisions (ref velocity);

		/*USE OF limit_fall_velocity(ref Vector3) HAS BEEN DEPRECATED*/
		//limit_fall_velocity(ref velocity);	//must do this even with collision below in case collision is detected at distance farther than max fall speed

		//Debug.DrawRay (transform.position, velocity, Color.magenta, 10f);

		transform.Translate (velocity);
		
	}

	/******************************************************************************************** 
	* Function:    void vertical_collisions(ref Vector3)
	* Description: Detects collisions above player if player's y velocity is positive and below 
	* 		   	   player if player's y velocity is negative. Makes necessary adjusments to
	* 		       player's velocity based on the detected collisions.
	********************************************************************************************/
	void vertical_collisions(ref Vector3 velocity){
		float y_direction = Mathf.Sign (velocity.y);
		float ray_length = Mathf.Abs (velocity.y) + skin_width;

		for (int i = 0; i < vertical_ray_count; i++) {
			Vector2 ray_origin = (y_direction < 0) ? raycast_origins.bottom_left : raycast_origins.top_left;
			ray_origin += new Vector2 (transform.right.x, transform.right.y) * i * vertical_ray_spacing;
			RaycastHit2D hit = Physics2D.Raycast (ray_origin, new Vector2 (transform.up.x, transform.up.y) * y_direction, ray_length, physics_layer);

			if (hit) {
				Debug.DrawRay (ray_origin, new Vector2 (transform.up.x, transform.up.y) * y_direction * ray_length, Color.red, 0.5f);
				//If a ceiling collision is detected when trying to step, step is cancelled and previous y velocity is restored. Also, adjust ray_length and y_direction to match previous y velocity.
				if (collision_info.stepping) {
					collision_info.stepping = false;
					velocity.y = collision_info.old_y_velocity;
					ray_length = Mathf.Abs (velocity.y) + skin_width;
					y_direction = Mathf.Sign (velocity.y);
					i = 0;
				} else{
					velocity.y = (hit.distance - skin_width) * y_direction;
					ray_length = hit.distance;
					collision_info.above = (y_direction > 0);
					collision_info.below = (y_direction < 0);

					//Player can only be climbing slopes in situations where player is not stepping.
					if (collision_info.ascending_slope)
						velocity.x = velocity.y / Mathf.Tan (collision_info.slope_angle * Mathf.Deg2Rad) * Mathf.Sign (velocity.x);
				}
			}
		}
	}

	/*********************************************************************************************
	* Function:    void horizontal_collisions(ref Vector3)
	* Description: Detects collisions to the right of player if player's x velocity is positive 
	* 		  	   and to the left of player if player's x velocity is negative. Makes necessary 
	* 		       adjusments to player's velocity based on the detected collisions. Also detects
	* 		   	   climbable steps and adjusts player's y velocity to mount a detected step.
	*********************************************************************************************/
	void horizontal_collisions(ref Vector3 velocity){
		float x_direction = Mathf.Sign (velocity.x);
		float ray_length = Mathf.Abs (velocity.x) + skin_width;


		RaycastHit2D prev_hit = new RaycastHit2D();		//Used for determing if there is a step or if there is just nothing in front of player.
		Vector2 corner_origin = (x_direction < 0) ? raycast_origins.bottom_left : raycast_origins.bottom_right;		//Used for determining step height when step detected.

		for (int i = 0; i < horizontal_ray_count; i++) {
			Vector2 ray_origin = corner_origin;

			ray_origin += new Vector2 (transform.up.x, transform.up.y) * i * horizontal_ray_spacing;
			RaycastHit2D hit = Physics2D.Raycast (ray_origin, new Vector2 (transform.right.x, transform.right.y) * x_direction, ray_length, physics_layer);

			if (hit) {
				prev_hit = hit;

				//If stepping activated but something else detected in front of player blocking step action, cancel step
				if (collision_info.stepping) {
					velocity.y = collision_info.old_y_velocity;
					collision_info.stepping = false;
				}

				float slope_angle = Vector2.Angle (hit.normal, transform.up);

				if (i == 0 && slope_angle <= max_traversable_angle) {
					float distance_to_slope_start = 0;	//distance to start of new slope angle

					//if we are climbing a slope and changing slope angles, we need to treat the next frame's movement as if we are climbing the new slope angle rather than the old slope angle.
					if (slope_angle != collision_info.old_slope_angle) {
						distance_to_slope_start = hit.distance - skin_width;
						velocity.x -= distance_to_slope_start * x_direction;
					}

					ascend_slope (ref velocity, slope_angle);
					velocity.x += distance_to_slope_start * x_direction;
				} else if (!collision_info.ascending_slope || slope_angle > max_traversable_angle) {
					velocity.x = (hit.distance - skin_width) * x_direction;
					ray_length = hit.distance;	//Relying on this made it so that some frames the ray length was just a hair too short to detect collisions above the first when walls were aligned.

					if (collision_info.ascending_slope)
						velocity.y = Mathf.Tan (collision_info.slope_angle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x);
										
					collision_info.right = (x_direction > 0);
					collision_info.left = (x_direction < 0);
				}
			} else if (prev_hit && !collision_info.stepping && !collision_info.ascending_slope && (ray_origin - corner_origin).magnitude + skin_width <= max_step_height) {	//have_hit indicates that we have actually encountered ground in a previous raycast that we can step on. Without this, we would try to step even if there was no step in the way.
				float step_height = (ray_origin - corner_origin).magnitude + skin_width;
				if (prev_hit.distance <= skin_width){
					//Debug.DrawRay (ray_origin, ((new Vector2 (transform.right.x, transform.right.y)).normalized * x_direction) * (step_height / Mathf.Tan ((max_traversable_angle-2) * Mathf.Deg2Rad)), Color.green, 5f);
					if (!Physics2D.Raycast (ray_origin, (new Vector2 (transform.right.x, transform.right.y) * x_direction), (step_height / Mathf.Tan ((max_traversable_angle-2) * Mathf.Deg2Rad)), physics_layer)) {		//check that there is actually enough space beyond the ledge to step up on to it
						if (step_height > velocity.y) {
							Debug.DrawRay (ray_origin, (transform.right * x_direction) * (step_height / Mathf.Tan ((max_traversable_angle-2) * Mathf.Deg2Rad)), Color.blue, 5f);
							collision_info.stepping = true;
							velocity.y = step_height;
							collision_info.right = false;
							collision_info.left = false;
						}
					}
				}
			}
		}
	}

	/****************************************************************************************** 
	* Function:    void limit_fall_velocity(ref Vector3)
	* Description: Keeps player's y velocity within maximum allowed fall speed.
	******************************************************************************************/
	/*void limit_fall_velocity(ref Vector3 velocity){
		if (velocity.y < -max_fall_speed)
			velocity.y = -max_fall_speed;
	}*/

	/****************************************************************************************** 
	* Function:    void ascend_slope(ref Vector3, float)
	* Description: Adjusts player's velocity to traverse a slope with a given angle.
	******************************************************************************************/
	void ascend_slope(ref Vector3 velocity, float slope_angle){
		float move_distance = Mathf.Abs(velocity.x);
		float climb_velocity_y = Mathf.Sin (slope_angle * Mathf.Deg2Rad) * move_distance;

		if(velocity.y <= climb_velocity_y) {
			velocity.y = climb_velocity_y;
			velocity.x = Mathf.Cos (slope_angle * Mathf.Deg2Rad) * move_distance * Mathf.Sign (velocity.x);
			collision_info.below = true;
			collision_info.ascending_slope = true;
			collision_info.slope_angle = slope_angle;
		}
	}

	/*************************************************************************************************** 
	* Function:    void descend_slope(ref Vector3)
	* Description: Adjusts player's velocity to either descend a slope on which player is standing
	* 		   	   in a controlled manner if slope angle is traversable or to make player player 
	* 			   slide down the slope if its angle is greater than the defined max traversable angle.
	***************************************************************************************************/
	void descend_slope(ref Vector3 velocity){
		/* Using facing_right boolean rather than taking sign of velocity.x to determine which direction player is facing.
		 * This ensures that the proper direction is used rather than defaulting to 1f (right) when velocity.x is 0. */
		Vector2 back_origin = handler.facing_right ? raycast_origins.bottom_left : raycast_origins.bottom_right;
		RaycastHit2D back_hit = Physics2D.Raycast (back_origin, -transform.up, Mathf.Infinity, physics_layer);
		Vector2 front_origin = handler.facing_right ? raycast_origins.bottom_right : raycast_origins.bottom_left;
		RaycastHit2D front_hit = Physics2D.Raycast (front_origin, -transform.up, Mathf.Infinity, physics_layer);

		/*float b_angle_test = -1f;
		float f_angle_test = -1f;

		bool b_affect_test = false;
		bool f_affect_test = false;*/

		if (back_hit || front_hit) {
			float back_slope_angle = back_hit ? Vector2.Angle (back_hit.normal, transform.up) : 0;
			float front_slope_angle = front_hit ? Vector2.Angle (front_hit.normal, transform.up) : 0;

			/* Determine if either slope is close enough and facing the correct direction to affect player's movement.
			 * It is necessary to account for the slope's direction so that there is not a situation where a player climbs one slope that forms a point with another
			 * descending slope and, once atop the point, attempts to descend the slope in front of them before fully ascending the slope behind them or attempts to 
			 * descend the slope behind them despite not moving in that direction.*/
			bool back_slope_can_affect = slope_close_enough (back_hit.distance, back_slope_angle, velocity) && relative_slope_direction(back_hit) == (handler.facing_right ? 1f : -1f);
			bool front_slope_can_affect = slope_close_enough (front_hit.distance, front_slope_angle, velocity) && relative_slope_direction(front_hit) != (handler.facing_right ? 1f : -1f);


			/*b_angle_test = back_slope_angle;
			f_angle_test = front_slope_angle;
			b_affect_test = back_slope_can_affect;
			f_affect_test = front_slope_can_affect;*/

			if (!(front_slope_can_affect && front_slope_angle <= max_traversable_angle)) {	//Does slope descents as long as there isn't traversable terrain that the player is trying to walk forward on to.
				if (back_slope_can_affect && back_slope_angle <= max_traversable_angle && back_slope_angle != 0) {
					//print ("using back_hit controlled"); //Debug.DrawRay (back_origin, -transform.up * back_hit.distance * 100, Color.blue, 5f);
					handle_descent (ref velocity, back_slope_angle, relative_slope_direction(back_hit), Mathf.Abs(velocity.x));
				} else if (front_slope_can_affect && front_slope_angle > max_traversable_angle) {
					//print ("using front_hit slide"); //Debug.DrawRay (front_origin, -transform.up * front_hit.distance * 100, Color.green, 5f);
					steep_slope_slide (ref velocity, front_slope_angle, relative_slope_direction(front_hit));
				} else if (back_slope_can_affect && back_slope_angle > max_traversable_angle) {
					//print ("using back_hit slide"); //Debug.DrawRay (back_origin, -transform.up * back_hit.distance * 100, Color.red, 5f);
					steep_slope_slide (ref velocity, back_slope_angle, relative_slope_direction(back_hit));
				}
			}
				
			/*		//For testing purposes.
			 if (collision_info.was_on_steep_slope && !collision_info.on_steep_slope) {
				Debug.Break ();
				print ("--Steep Slope Exited--\nBack Angle: " + b_angle_test + "\nBack Can Affect: " + b_affect_test + "\nFront Angle: " + f_angle_test + "\nFront Can Affect: " + f_affect_test);
				if (!f_affect_test && slope_close_enough (front_hit.distance, front_slope_angle, velocity))
					print ("Front cannot affect because of slope direction.");
				else if (!f_affect_test && relative_slope_direction (front_hit) != (handler.facing_right ? 1f : -1f)) {
					print ("Front cannot affect because slope not close enough.\nLHS: " + (front_hit.distance - skin_width) + "\nRHS: " + (Mathf.Tan (front_slope_angle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x)) + "\nDifference: " + ((front_hit.distance - skin_width) - (Mathf.Tan (front_slope_angle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x))));
					Debug.DrawRay (front_origin, -transform.up * (Mathf.Tan (front_slope_angle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x) + skin_width), Color.yellow, 10f);
					Debug.DrawRay (front_origin, transform.right * (handler.facing_right ? 1f : -1f) * skin_width, Color.yellow, 10f);

				} else if (!front_slope_can_affect)
					print ("Neither conditions for front affect satisfied.");
			}
			else if(!collision_info.was_on_steep_slope && collision_info.on_steep_slope){
				Debug.DrawRay (front_origin, -transform.up * (Mathf.Tan (front_slope_angle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x) + skin_width), Color.cyan, 10f);
				Debug.DrawRay (front_origin, transform.right * (handler.facing_right ? 1f : -1f) * skin_width, Color.cyan, 10f);
				Debug.Break ();
			}*/
		}


	}

	/******************************************************************************************** 
	* Function:    bool slope_close_enough(float, float, Vector3)
	* Description: Returns true if a slope with a given angle and distance from player is close
	* 		  	   enough to affect player's movement based on current velocity. Returns false 
	* 		  	   otherwise.
	*********************************************************************************************/
	bool slope_close_enough(float distance_to_slope, float slope_angle, Vector3 velocity){
		return distance_to_slope - skin_width <= Mathf.Tan (slope_angle * Mathf.Deg2Rad) * Mathf.Abs (velocity.x);
	}

	/*************************************************************************************************
	* Function:    float relative_slope_direction(Raycast2D)
	* Description: Returns direction (1f or -1f) of slope relative to player's rotation. E.g. if
	* 		  	   player is rotated 180 degrees so their feet are in the air, then walking down
	* 		  	   (or in a literal sense, up) a slope that slopes to the right in world coordinates
	* 		 	   will return -1f because the player is actually desending to the left in local
	* 		  	   coordinates.
	*************************************************************************************************/
	float relative_slope_direction(RaycastHit2D hit){
		float z_rotation = transform.eulerAngles.z;

		/*Last condition is included because rotations perfectly divisible by 360 in Unity inspector will result in eulerAngles slightly off from 0. 
		If this difference is negative (From negative multiple of 360 in inspector, the default case would be executed (left gravity) even though the start is almost flat.*/
		if ((z_rotation >= 0 && z_rotation < 45f) || (z_rotation > 315f && z_rotation < 360f) || z_rotation < 0)
			return Mathf.Sign((transform.rotation * hit.normal).x);
		else if (z_rotation >= 45f && z_rotation < 135f)
			return -Mathf.Sign((transform.rotation * hit.normal).x);
		else if (z_rotation >= 135f && z_rotation < 225f)
			return Mathf.Sign((transform.rotation * hit.normal).x);
		else
			return -Mathf.Sign((transform.rotation * hit.normal).x);
	}

	/********************************************************************************************* 
	* Function:    void steep_slope_slide(ref Vector3, float, float)
	* Description: Adjusts player's velocity to make them slide down a slope with given angle in
	* 		  	   an uncontrolled fashion. Allows for player to step off of slope in direction
	* 		 	   that they are sliding.
	*********************************************************************************************/
	void steep_slope_slide(ref Vector3 velocity, float slope_angle, float slope_direction){
		float angle_adjustment = Mathf.Pow(slope_angle, 1f/2f);		//taking root reduces the range of adjustment values --> makes difference in acceleration smaller for different slope angles
		float max_slide_speed = Mathf.Abs(max_fall_speed * Mathf.Sin (slope_angle * Mathf.Deg2Rad));

		if (collision_info.slope_slide_distance > max_slide_speed)
			collision_info.slope_slide_distance = max_slide_speed;

		float slope_slide_smoothing = 0;
		//if (collision_info.slope_slide_distance == 0)
		//	print ("reset slope_slide_distance");
		collision_info.slope_slide_distance = Mathf.SmoothDamp(collision_info.slope_slide_distance, max_slide_speed, ref slope_slide_smoothing, steep_slope_friction / (handler.gravity * angle_adjustment));

		float original_x_velocity = velocity.x;	//save x_velocity before it is changed to be used if player is trying to move in same direction as slope

		handle_descent (ref velocity, slope_angle, slope_direction, collision_info.slope_slide_distance);

		//If player is trying to move in same direction as slope, let them step off the slope so they aren't stuck sliding down it to the bottom.
		float move_x = relative_x_input();	//velocity.x is smoothed so need to get input again to determine if player is trying in this frame to move horizontally
		if (move_x != 0 && Mathf.Sign (move_x) == slope_direction && Mathf.Sign (original_x_velocity) == slope_direction) {
			velocity.x += original_x_velocity;
			collision_info.descending_slope = false;
			collision_info.below = false;
			collision_info.on_steep_slope = false;
		}
		else
			collision_info.on_steep_slope = true;
	}

	/********************************************************************************************* 
	* Function:    float relative_x_input()
	* Description: Gets x movement input relative to gravity direction as discerned by player's
	* 			   rotation. For example, when gravity direction is down, the horizontal axis
	* 			   is used for determining x movement input. But when gravity direction is right,
	* 			   the vertical axis is used.
	*********************************************************************************************/
	float relative_x_input(){
		float z_rotation = transform.eulerAngles.z;

		/*Last condition is included because rotations perfectly divisible by 360 in Unity inspector will result in eulerAngles slightly off from 0. 
		If this difference is negative (From negative multiple of 360 in inspector, the default case would be executed (left gravity) even though the start is almost flat.*/
		if ((z_rotation >= 0 && z_rotation < 45f) || (z_rotation > 315f && z_rotation < 360f) || z_rotation < 0)
			return Input.GetAxisRaw ("Horizontal");
		else if (z_rotation >= 45f && z_rotation < 135f)
			return Input.GetAxisRaw ("Vertical");
		else if (z_rotation >= 135f && z_rotation < 225f)
			return -Input.GetAxisRaw ("Horizontal");
		else
			return -Input.GetAxisRaw ("Vertical");
	}



	/***************************************************************************************** 
	* Function: void handle_descent(ref Vector3, float, float, float)
	* Description: Adjusts player's velocity to descend slope with given angle and direction.
	******************************************************************************************/
	void handle_descent(ref Vector3 velocity, float slope_angle, float slope_direction, float move_distance){
		velocity.x = Mathf.Cos (slope_angle * Mathf.Deg2Rad) * move_distance * slope_direction;
		velocity.y = Mathf.Sin (slope_angle * Mathf.Deg2Rad) * move_distance * -1;

		collision_info.slope_angle = slope_angle;
		collision_info.below = true;
		collision_info.descending_slope = true;

		//Currently unecessary as explained in limit_slope_descent_velocity function header.
		//limit_slope_descent_velocity (ref velocity, slope_angle);	
	}

	/********************************************************************************************* 
	* Function:    void limit_slope_descent_velocity(ref Vector3, float)
	* Description: Limits magnitude of y velocity defined max fall speed and adjusts x velocity 
	* 			   to maintain proper angle of descent.
	* 
	* 		  NOTE: This function will be unecessary as long as slope_slide_speed 
	* 			    (representing the move distance along the slope in steep_slope_slide())
	* 			    is equivalent to or less than the length of the hypotnuse of the triangle
	* 			    with y-leg of length max_fall_speed and angle theta equal to the slope 
	* 				angle.
	*********************************************************************************************/
	void limit_slope_descent_velocity(ref Vector3 velocity, float slope_angle){
		if (Mathf.Abs(velocity.y) > max_fall_speed) {
			velocity.y = max_fall_speed * Mathf.Sign (velocity.y);
			velocity.x = velocity.y / Mathf.Tan(slope_angle * Mathf.Deg2Rad);
		}
	}

	/******************************************************************************************* 
	* Function:    void update_references()
	* Description: Updates location of corners of box collider when player is not rotated. Used
	* 		  	   as reference points for update_raycast_origins().
	*******************************************************************************************/
	void update_references(){
		Quaternion original_rotation = transform.rotation;
		transform.rotation = Quaternion.Euler (0, 0, 0);

		Bounds bounds = box_collider.bounds;
		bounds.Expand (skin_width * -2);

		no_rotation_raycast_origins.bottom_right = new Vector3(bounds.max.x, bounds.min.y, 2 * transform.position.z) - transform.position;
		no_rotation_raycast_origins.bottom_left = new Vector3(bounds.min.x, bounds.min.y, 2 * transform.position.z) - transform.position;
		no_rotation_raycast_origins.top_right = new Vector3(bounds.max.x, bounds.max.y, 2 * transform.position.z) - transform.position;
		no_rotation_raycast_origins.top_left = new Vector3(bounds.min.x, bounds.max.y, 2 * transform.position.z) - transform.position;

		transform.rotation = original_rotation;
	}

	/********************************************************************************************** 
	* Function:    void update_raycast_origins()
	* Description: Updates location of corners of box collider taking rotation into consideration.
	* 		       These points are used for originating raycasts for collision detection.
	**********************************************************************************************/
	void update_raycast_origins(){
		Vector3 position = transform.position;
		Quaternion rotation = transform.rotation;

		//Rotate 3D vectors to current object rotation and position so that they point to location of raycast origins considering rotation.
		raycast_origins.bottom_left = rotation * no_rotation_raycast_origins.bottom_left + position;
		raycast_origins.bottom_right = rotation * no_rotation_raycast_origins.bottom_right + position;
		raycast_origins.top_left = rotation * no_rotation_raycast_origins.top_left + position;
		raycast_origins.top_right = rotation * no_rotation_raycast_origins.top_right + position;
	}

	/****************************************************************************************** 
	* Function:    void calculate_ray_spacing()
	* Description: Calculates the amount of space needed between each raycast being used for 
	* 		       collision detection based on the number of rays being used so that they are 
	* 		       spaced evenly along box collider in collision detection.
	******************************************************************************************/
	void calculate_ray_spacing(){
		//Must have at least 2 rays for collision detection.
		vertical_ray_count = Mathf.Clamp (vertical_ray_count, 2, int.MaxValue);
		horizontal_ray_count = Mathf.Clamp (horizontal_ray_count, 2, int.MaxValue);

		//Must do calculations using zero rotation else bounds.size.x/y will return incorrect values as these are in world coordinates.
		Quaternion original_rotation = transform.rotation;
		transform.rotation = Quaternion.Euler (0, 0, 0);

		Bounds bounds = box_collider.bounds;
		bounds.Expand (skin_width * -2);

		vertical_ray_spacing = bounds.size.x / (vertical_ray_count - 1);
		horizontal_ray_spacing = bounds.size.y / (horizontal_ray_count - 1);

		transform.rotation = original_rotation;
	}
}
