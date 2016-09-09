/**************************************************************************************
** Filename: 	Contact_Kill.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Kills player when player enters trigger collider of gameobject that
*				this script is attached to.
** Functions:	
**				void OnTriggerEnter2D(Collider2D);
**************************************************************************************/
using UnityEngine;
using System.Collections;


public class Contact_Kill : MonoBehaviour {

	/***************************************************************************************** 
	* Function:    void OnTriggerEnter2D(Collider2D)
	* Description: Kills player when player enters trigger collider of gameobject that
	*			   this script is attached to.
	******************************************************************************************/
	void OnTriggerEnter2D(Collider2D other){
		if (other.tag == "Player") {
			/*Potential For Later: 
			 * if player still has lives.*/
			other.GetComponent<Player>().respawn();
			//Else: end game
		}
	}
}
