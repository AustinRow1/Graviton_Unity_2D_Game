/**
** 
** 		NOTE: THIS SCRIPT IS OUTDATED AND NO LONGER USED AS THE GAME NO LONER FEATURES EFFECT-COLLECTABLES.
** 	
**/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Collectable_Generator : MonoBehaviour {

	public float respawn_delay = 0.5f;
	public GameObject collectable;
	public float spawn_radius = 0.7f;

	/***************************************************************************************** 
	*  Function: 	void Start()
	*  Description: Function called at start of script. Generates first collectable.
	******************************************************************************************/ 
	void Start () {
		float actual_respawn_delay = respawn_delay;
		respawn_delay = 0f;
		generate_new ();
		respawn_delay = actual_respawn_delay;
	}

	/***************************************************************************************** 
	*  Function: 	generate_new()
	*  Description: Used by other scripts to call the generate coroutine to start process of 
	* 		   		generating a new collectable.
	******************************************************************************************/ 
	public void generate_new(){
		StartCoroutine (generate ());
	}

	/********************************************************************************************** 
	*  Function: 	IEnumerator generate()
	*  Description: Generates collectable item associated with this generator after spawn area is
	* 		   		cleared and appropriate delay occurs. 
	**********************************************************************************************/ 
	IEnumerator generate(){
		while (spawn_blocked ())
			yield return null;
			
		yield return new WaitForSeconds (respawn_delay);
		GameObject child_collectable = Instantiate (collectable, transform.position, transform.rotation) as GameObject;
		child_collectable.transform.parent = transform;	
	}

	/********************************************************************************************** 
	*  Function: 	bool spawn_blocked()
	*  Description: Returns true if there are any gameobjects blocking the collectable spawn area
	* 		   		(defined as circle centered at generator position with radius spawn_radius)
	* 		   		and false otherwise.
	**********************************************************************************************/ 
	bool spawn_blocked(){
		/*Debug.DrawRay (transform.position, Vector2.right * spawn_radius);
		Debug.DrawRay (transform.position, Vector2.left * spawn_radius);
		Debug.DrawRay (transform.position, Vector2.up * spawn_radius);
		Debug.DrawRay (transform.position, Vector2.down * spawn_radius);*/
		return Physics2D.OverlapCircle (transform.position, spawn_radius, 1 << LayerMask.NameToLayer ("PhysicsLayer"));
	}
}
