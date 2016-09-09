/**************************************************************************************
** Filename: 	Transporter_Power.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Handles the actions of what are currently being referred to as 
**				as "transport stones" (used for continuing to next level/scene).
** Functions:	
**				void Awake();
**				void Update();
**				void OnTriggerEnter2D(Collider2D);
**************************************************************************************/
using UnityEngine;
using System.Collections;

public class Transporter_Power : MonoBehaviour {

	[SerializeField]
	private float rotate_speed = 50f;
	public GameObject game_master;
	private Game_Controller game_controller;

	/***************************************************************************************** 
	* Function:    void Awake()
	* Description: Called at creation of script. Gets reference to Game_Controller script.
	******************************************************************************************/
	void Awake(){
		game_controller = game_master.GetComponent<Game_Controller> ();
	}

	/***************************************************************************************** 
	* Function:    void Update()
	* Description: Called once per frame. Rotates transport stone at given rotation speed.
	******************************************************************************************/
	void Update(){
		transform.Rotate (0f, 0f, rotate_speed * Time.deltaTime);
	}

	/***************************************************************************************** 
	* Function:    void OnTriggerEnter2D(Collider2D)
	* Description: Called when other 2D collider enters 2D trigger collider of gameObject that
	* 			   this script is attached to. If other 2D collider is component of player
	* 			   gameObject, calls function to increment number transport stones player has
	* 			   collected and deactivates itself from the scene.
	******************************************************************************************/
	void OnTriggerEnter2D(Collider2D other){
		if (other.gameObject.tag == "Player"){
			game_controller.add_transport_stone();
			gameObject.SetActive (false);	//instead of destroying
		}
	}
}
