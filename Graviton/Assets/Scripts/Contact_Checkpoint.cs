/**************************************************************************************
** Filename: 	Contact_Checkpoint.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Acts as checkpoint for where player spawns when contacted by player
**				gameobject.
** Functions:	
**				void Start();
**				void OnTriggerEnter2D(Collider2D);
**************************************************************************************/
using UnityEngine;
using System.Collections;

public class Contact_Checkpoint : MonoBehaviour {

	/****************************************************************************************** 
	* Function:    void Start()
	* Description: Called at start of script. Sets color of checkpoint mesh to white.
	* 			   Temporary until something other than basic sphere gameobjects are used
	* 			   as chekpoints.
	******************************************************************************************/
	void Start () {
		gameObject.GetComponent<MeshRenderer> ().material.color = Color.white;
	}

	/****************************************************************************************** 
	* Function:    void OnTriggerEnter2D(Collider2D)
	* Description: Called when a 2D collider enters 2D trigger collider attached to same
	* 			   gameobject as this script. If other 2D collider is attached to player 
	* 			   gameobject, it sets that players spawn to position of checkpoint gameobject
	* 			   and sets player's spawn gravity to current gravity of player. Changes 
	* 			   checkpoint mesh color to greent to indicate that checkpoint has been 
	* 			   activated (this is temporary and will be changed when something other than
	* 			   basic sphere gameobject is used for checkpoints).
	******************************************************************************************/
	void OnTriggerEnter2D(Collider2D other){
		if (other.tag == "Player") {
			Player player_controller = other.gameObject.GetComponent<Player> ();
			player_controller.set_spawn (gameObject.transform, player_controller.gravity_direction ());
		}
		gameObject.GetComponent<MeshRenderer> ().material.color = Color.green;	//This will need to be changed to use a sprite renderer when artwork is available.
	}
}
