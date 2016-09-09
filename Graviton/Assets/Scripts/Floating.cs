/**************************************************************************************
** Filename: 	Floating.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Makes gameobject bob in place to create floating effect.
** Functions:	
**				void Update();
**				void OnEnable();
**************************************************************************************/
using UnityEngine;
using System.Collections;

public class Floating : MonoBehaviour {

	public float speed = 1f;
	public float amplitude = 0.5f;
	public Vector3 center_point;	//Note: Made a public variable so that it can be manipulated when moving while float is implemented later.
	public bool affected_by_rotation = true;

	/************************************************************************************************* 
	*  Function: 	void Update()
	*  Description: Every frame shifts gameobject that script is attached to to next position 
	*          		in a bobbing/floating animation by mapping it to a sine wave. Shift in 
	*		  		position accounts for rotation so, for example, if gameobject is rotated
	*		   		45 degrees on the z axis, the bobbing/floating axis will be rotated 45 degrees.
	*************************************************************************************************/  
	void Update () {
		float change_factor = amplitude * Mathf.Sin (speed * Time.time);
		Vector3 offset;

		if (affected_by_rotation) {
			Vector3 adjusted_up = new Vector3 (Mathf.Cos ((transform.eulerAngles.z + 90f) * Mathf.Deg2Rad), Mathf.Sin ((transform.eulerAngles.z + 90f) * Mathf.Deg2Rad), 0f);
			offset = change_factor * adjusted_up;
		} else
			offset = change_factor * Vector3.up;
		
		transform.position = center_point + offset;
	}

	/********************************************************************************************* 
	*  Function: 	void OnEnable()
	*  Description: Function is called every time this script is enabled, including at start.
	*		   		Sets starting position (position at which bobbing/floating will be centered).
	*********************************************************************************************/ 
	void OnEnable(){
		center_point = transform.position;
	}
}
