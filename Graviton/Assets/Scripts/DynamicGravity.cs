/**
** 
** 		NOTE: THIS SCRIPT IS OUTDATED SINCE THE TRANSITION FROM RIGIBODY-BASED
** 			  PHYSICS TO RAYCAST-BASED PHYSICS. IT IS NOT BEING USED CURRENTLY.
** 	
**/

using UnityEngine;
using System.Collections;

public class DynamicGravity : MonoBehaviour {
	
	[Range(0,3)]	//gravity direction can only be 0=Down, 1=Up, 2=Left, 3=Right
	private int direction = 0;
	public float gravity_strength = 30;
	public float max_fall_rate = 10;
	public bool rotates = false;
	private Rigidbody2D rb;
	private Vector2 gravity;

	/********************************************************************************************* 
	*  Function: 	void Start()
	*  Description: At start of script changes gravity to match option chosen in Unity inspector
	* 		   		and gets reference to rigidbody component of game object. 
	*********************************************************************************************/ 
	void Start () {
		change_gravity (direction);
		rb = GetComponent<Rigidbody2D> ();
	}

	/********************************************************************************************* 
	*  Function: 	void FixedUpdate()
	*  Description: Every frame adds gravitational force in appropriate direction to game object
	* 		   		and clamps speed of game object to max_fall_rate.
	*********************************************************************************************/ 
	void FixedUpdate () {			
		rb.AddForce(gravity * Time.fixedDeltaTime);
		contain_fall_rate ();
	}

	public int gravity_direction(){
		return direction;
	}

	/*********************************************************************************************** 
	*  Function: 	public void change_gravity(int)
	*  Description: Generally called by other scripts (notably scripts attached to gravity-changing
	* 		   		collectable items) to change direction of gravity and sprite orientation
	* 		   		(rotation) for game object that this script is attached to. Puts game object
	* 		   		into a falling state (which may or may not trigger a falling animation).
	***********************************************************************************************/ 
	public void change_gravity(int new_direction){
		direction = new_direction;
		if (rotates) {
			StopCoroutine ("set_rotation"); //Just in case player is still rotation from recent gravity change when they change gravity again.
			StartCoroutine ("set_rotation");
		}
		set_gravity_vector ();
	}

	/************************************************************************************* 
	*  Function: 	void set_gravity_vector()
	*  Description: Adjusts a Vector2 representing gravity in a downwards direction to 
	* 		   		gravity in the direction of "gc.gravity_direction" and assigns it to
	* 		   		"gravity".
	*************************************************************************************/
	void set_gravity_vector(){
		gravity = adjusted_velocity(new Vector2(0f, -10f*gravity_strength));
	}

	/************************************************************************************ 
	*  Function:	void set_rotation()
	*  Description: Rotates the gameobject so that it is oriented appropriately for the 
	* 		   		current direction of gravity (e.g. if gravity is pulling to the 
	* 		   		right, gameobjects feet/bottom will face right).
	************************************************************************************/
	IEnumerator set_rotation(){
		float start_time = Time.time;
		float duration = 0.1f;
		Quaternion target;

		if (direction == 0) 
			target = Quaternion.Euler (0f, 0f, 0f);
		else if(direction == 1)
			target = Quaternion.Euler (0f, 0f, 180f);
		else if(direction == 2)
			target = Quaternion.Euler (0f, 0f, -90f);
		else 
			target = Quaternion.Euler (0f, 0f, 90f);

		float t;
		while ((t = (Time.time - start_time) / duration) < 1f) {
			transform.rotation = Quaternion.Lerp (transform.rotation, target, t);
			yield return null;
		}

	}


	/*************************************************************************************** 
	*  Function: 	Vector2 adjusted_velocity(int, Vector2)
	*  Description: Is the antithesis of standard_velocity(int direct) 
	* 
	* 		   		For example, if gravity is working to the right (player falls to right)
	* 		   		and player is falling to the right with a velocity of 10 and is moving
	* 		   		up with a velocity of 7, the y component
	* 		   		of returned vector would be -10 (falling down at rate of 10) and the 
	* 		   		x component would be 7 (moving to the right at rate of 7). 
	***************************************************************************************/
	Vector2 adjusted_velocity(Vector2 standard){
		if (direction == 0)
			return standard;
		else if (direction == 1) 
			return new Vector2 (standard.x, -standard.y);
		else if (direction == 2) 
			return new Vector2 (standard.y, -standard.x);
		else
			return new Vector2 (-standard.y, standard.x);
	}

	/****************************************************************************************** 
	*  Function: 	void contain_fall_rate()
	*  Description: Clamps speed to a maximum value every frame. Used primarily to ensure that 
	* 		   		character's speed does not become too high while falling.
	******************************************************************************************/ 
	void contain_fall_rate(){
		if (rb.velocity.y > max_fall_rate)
			rb.velocity = new Vector2 (rb.velocity.x, max_fall_rate);
		else if (rb.velocity.y < -max_fall_rate)
			rb.velocity = new Vector2 (rb.velocity.x, -max_fall_rate);
		if (rb.velocity.x > max_fall_rate)
			rb.velocity = new Vector2 (max_fall_rate, rb.velocity.y);
		else if (rb.velocity.x < -max_fall_rate)
			rb.velocity = new Vector2 (-max_fall_rate, rb.velocity.y);
	}
}
