/**************************************************************************************
** Filename: 	Gravity_Affector.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Changes gravity when direction of player when triggered by player.
** Functions:	
**				void Awake();
**				void OnTriggerEnter2D(Collider2D);
**************************************************************************************/
using UnityEngine;
using System.Collections;

public class Gravity_Affector : MonoBehaviour {

	//Inserted for convenience of selecting gravity direction associated with affector in the Unity inspector.
	public enum Direction{
		Down, Up, Left, Right
	}

	public Direction direction;
	private int numeric_direction;
	private Light glow;

	/********************************************************************************************** 
	* Function:    void Awake()
	* Description: Called at creation of script. Gets reference to light used as affector's
	* 			   glow and converts enumerated Direction type selected in Unity inspector to the 
	* 		   	   appropriate integer direction.
	**********************************************************************************************/
	void Awake(){
		glow = gameObject.GetComponent<Light> ();

		if (direction == Direction.Down)
			numeric_direction = 0;
		else if (direction == Direction.Up)
			numeric_direction = 2;
		else if (direction == Direction.Left)
			numeric_direction = 1;
		else
			numeric_direction = 3;
	}

	/*********************************************************************************************** 
	*  Function: 	void OnTriggerEnter2D(Collider2D)
	*  Description: Function runs when collider of other game object crosses collider of game
	* 		   		object with this script. If other collider is attached to player gameobject,
	* 				changes player's gravity direction to affector's specified direction and 
	* 			    flares affector's glow light.
	***********************************************************************************************/ 
	void OnTriggerEnter2D(Collider2D other){
		//DynamicGravity gravity_script = other.gameObject.GetComponent<DynamicGravity> ();
		Player player = other.gameObject.GetComponent<Player> ();
		
		if (player != null && numeric_direction != player.gravity_direction ()) {
			player.change_gravity (numeric_direction);
			Light_Effects.flare (glow, 13f);
		}
	}
}
