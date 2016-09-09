/**
 **
 **		NOTE: THE FUNCTION OF THIS SCRIPT (TELEPORTING TO NEXT LEVEL) HAS
 **			  BEEN REPLACED BY Transporter_Power.cs
 **
 **/

using UnityEngine;
using System.Collections;

public class Transporter : MonoBehaviour {
	private bool is_active = false;
	private bool in_range = false;
	public GameObject game_master;

	void Update(){
		if (is_active && in_range && Input.GetKeyDown (KeyCode.F)) {
			print ("ending level");
			StartCoroutine (end_level ());
		}
	}

	void OnTriggerEnter2D(Collider2D other){
		if(other.gameObject.tag == "Player")
			in_range = true;
	}

	void OnTriggerExit2D(Collider2D other){
		if(other.gameObject.tag == "Player")
			in_range = false;
	}

	public bool active(){
		return is_active;
	}

	public void activate_transporter(){
		is_active = true;
	}

	IEnumerator end_level(){
		game_master.GetComponent<Game_Controller> ().end_scene ();
		yield return null;
	}

}
