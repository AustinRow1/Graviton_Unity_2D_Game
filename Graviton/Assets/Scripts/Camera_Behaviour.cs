/***************************************************************************
** Filename: 	Camera_Behaviour.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: 
** Functions:
**				void Awake();
**				void Update();
**				public bool cam_effect_is_active();
**				public void set_target(Transform);
**				public Coroutine focus(Transform, float, float, float);
**				IEnumerator _focus(Transform, float, float, float);
**				IEnumerator zoom(Vector3, float, float);
**				bool is_visible(Transform);
**				void screen_size_adjust();
**				void move_towards(Transform, float);
***************************************************************************/
using UnityEngine;
using System.Collections;

public class Camera_Behaviour : MonoBehaviour {

	public Transform target;
	private PlayerController player_controller;
	public float smooth_time = 0.15f;
	public Transform lower_bound;
	public Transform upper_bound;
	private Camera cam;
	private Vector3 zero_v3 = Vector3.zero;
	private float cam_height;
	private float cam_width;
	private float screen_width;
	private float screen_height;
	private Vector3 original_position;

	//public Transform test;
	private bool cam_effect_active = false;

	/************************************************************************************************* 
	*  Function: 	void Awake()
	*  Description: Runs at creation of script. Gets reference to camera and adjusts screen size.
	************************************************************************************************/ 
	void Awake(){
		cam = gameObject.GetComponent<Camera> ();
		screen_size_adjust ();
	}

	/************************************************************************************************* 
	*  Function: 	void Update()
	*  Description: Runs every frame. Adjusts screen size if necessary and moves camera with target.
	************************************************************************************************/ 
	void Update(){
		if (screen_width != Screen.width || screen_height != Screen.height)
			screen_size_adjust ();
		/*if (Input.GetKeyDown (KeyCode.E) && !cam_effect_active)
			focus(test, 1f, 5f, 1f);*/
		if (target && !cam_effect_active)
			move_towards (target.position, smooth_time);
	}

	/************************************************************************************************* 
	*  Function: 	public bool cam_effect_is_active()
	*  Description: Returns true if a camera effect is active, false otherwise.
	************************************************************************************************/ 
	public bool cam_effect_is_active(){
		return cam_effect_active;
	}

	/************************************************************************************************* 
	*  Function: 	public void set_target(Transform)
	*  Description: Sets camera's target.
	************************************************************************************************/ 
	public void set_target(Transform new_target){
		target = new_target;
		player_controller = target.gameObject.GetComponent<PlayerController> ();
	}

	/************************************************************************************************* 
	*  Function: 	public Coroutine focus(Transform, float, float, float)
	*  Description: Public function to call _focus coroutine.
	************************************************************************************************/ 
	public Coroutine focus(Transform focus_target, float smooth, float zoom_size, float pause){
		return StartCoroutine (_focus (focus_target, smooth, zoom_size, pause));
	}

	/************************************************************************************************* 
	*  Function: 	IEnumerator _focus(Transform, float, float, float)
	*  Description: Runs a camera effect where camera focuses on given target by moving to show 
	* 				target and zooming on target before moving back to original position.
	*************************************************************************************************/ 
	IEnumerator _focus(Transform focus_target, float smooth, float zoom_size, float pause){
		cam_effect_active = true;
		if (player_controller)
			player_controller.disable_control ();
		
		original_position = transform.position;
		//Debug.DrawRay (transform_placeholder.position, new Vector3 (1f, 0, 0), Color.green, 30f); 
		while (!is_visible (focus_target)) {
			move_towards(focus_target.position, smooth);
			yield return null;
		}
			
		float original_size = cam.orthographicSize;
		yield return StartCoroutine (zoom (focus_target.position, zoom_size, smooth / 2));
		yield return new WaitForSeconds (pause);
		//Debug.DrawRay (transform_placeholder.position, new Vector3 (1f, 0, 0), Color.green, 30f); 
		yield return StartCoroutine (zoom (original_position, original_size, smooth / 2));

		cam_effect_active = false;
		if (player_controller)
			player_controller.enable_control ();
	}

	/************************************************************************************************* 
	*  Function: 	IEnumerator zoom(Vector3, float, float)
	*  Description: Zooms on given target to given zoom size.
	************************************************************************************************/ 
	IEnumerator zoom(Vector3 zoom_target, float camera_size, float smooth){
		//Debug.DrawRay (zoom_target.position, new Vector3 (1f, 0, 0), Color.magenta, 30f);
		float zero_velocity = 0.0f;
		while (Mathf.Abs (cam.orthographicSize - camera_size) > 0.01f) {
			cam.orthographicSize = Mathf.SmoothDamp (cam.orthographicSize, camera_size, ref zero_velocity, smooth);
			screen_size_adjust ();
			move_towards(zoom_target, smooth);
			yield return null;
		}
	}

	/************************************************************************************************* 
	*  Function: 	bool is_visible(Transform)
	*  Description: Returns true if given transform can currently be seen by camera, false otherwise.
	*************************************************************************************************/ 
	bool is_visible(Transform t){
		Vector3 pos = t.position;
		float lower_visible_x = transform.position.x - cam_width / 2;
		float upper_visible_x = transform.position.x + cam_width / 2;
		float lower_visible_y = transform.position.y - cam_height / 2;
		float upper_visible_y = transform.position.y + cam_height / 2;
		//print ("X: (" + lower_visible_x + ", " + upper_visible_x + "), Y: (" + lower_visible_y + ", " + upper_visible_y + ")");
		if (pos.x > upper_visible_x || pos.x < lower_visible_x || pos.y > upper_visible_y || pos.y < lower_visible_y)
			return false;
		else
			return true;
	}

	/************************************************************************************************* 
	*  Function: 	void screen_size_adjust()
	*  Description: Adjusts camera size to use full screen.
	************************************************************************************************/ 
	void screen_size_adjust(){
		screen_width = Screen.width;
		screen_height = Screen.height;
		cam_height = cam.orthographicSize * 2f;
		cam_width = cam_height * screen_width / screen_height;
	}

	/*void FixedUpdate () {
		if (target && !cam_effect_active)
			move_towards (target, smooth_time);
	}*/

	/************************************************************************************************* 
	*  Function: 	void move_towards(Transform, smooth)
	*  Description: Smoothly moves camera towards given destination.
	************************************************************************************************/ 
	void move_towards(Vector3 dest, float smooth){
		Vector3 delta = dest - transform.position;
		Vector3 destination = transform.position + delta;
		destination = new Vector3 (Mathf.Clamp(destination.x, lower_bound.position.x + cam_width/2, upper_bound.position.x - cam_width/2), 
			Mathf.Clamp(destination.y, upper_bound.position.y + cam_height/2, lower_bound.position.y - cam_height/2), 
			transform.position.z);
		transform.position = Vector3.SmoothDamp (transform.position, destination, ref zero_v3, smooth);
	}
}