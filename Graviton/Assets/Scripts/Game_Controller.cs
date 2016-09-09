/***************************************************************************
** Filename: 	Game_Controller.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Intializes player position, rotation, and gravity.
*				Starts and ends scene at appropriate times.
** Functions:
**				void Awake();
**				void Start();
**				void Update();
**				Vector3 get_start_position();
**				float round_to_nearest_multiple(float, float);
**				float get_start_rotation();
**				int get_start_gravity_direction();
**				IEnumerator start_scene();
**				IEnumerator end_scene();
**				public void add_transport_stone();
***************************************************************************/

using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Game_Controller : MonoBehaviour {

	public Transform start;
	private Transform spawn_point;
	public GameObject player_prefab;
	private int spawn_direction;
	private GameObject player;
	private Player player_controller;
	private SpriteRenderer player_renderer;
	private Camera_Behaviour main_cam_script;


	//variables used for fading scene
	public bool do_scene_fade = true;
	public bool do_player_fade = true;
	public bool do_initial_focus = true;
	public GameObject focus_target;

	private int transport_stones = 0;
	public int transport_stones_needed = 1;
	public GameObject transport_effect;
	private bool ending_level = false;
	Scene_Manager scene_manager = null;
	[SerializeField]
	private bool start_facing_right;



	/************************************************************************************************* 
	*  Function: 	void Awake()
	*  Description: Runs at initialization of script.
	* 		   		If player fade disabled, disables scene fade by default so that scene can't 
	* 		   		fade in without a player and instead scene will start without any fading (Not 
	* 		   		fixing this situation would result in scene fading in without visible player 
	* 		  		and with player controls disabled so user couldn't do anything). If scene fade 
	* 		   		enabled, initializes fader Image to black and full screen, else disables fader.
	************************************************************************************************/ 
	void Awake(){
		scene_manager = GetComponent<Scene_Manager> ();
		player = Instantiate (player_prefab, start.position, Quaternion.Euler(0, 0, get_start_rotation())) as GameObject;
		player.transform.position = get_start_position ();
		player_renderer = player.GetComponent<SpriteRenderer> ();
		player_controller = player.GetComponent<Player> ();
		player_controller.start_facing_right = start_facing_right;
		player_controller.set_spawn (player.transform, get_start_gravity_direction ());
		main_cam_script = Camera.main.GetComponent<Camera_Behaviour>();
		main_cam_script.set_target(player.transform);
	}

	/************************************************************************************************* 
	*  Function: 	void Start()
	*  Description: Runs at start of script.
	* 		   		Gets reference to camera follow script so follow target can be changed when
	* 		   		player spawned/respawned. Spawns player. If scene fade enabled, starts coroutine
	* 		   		that fades in screen from black then fades in player afterwards. Else if the
	* 		   		player fade is enabled, fades in player without fading from black first.
	*************************************************************************************************/ 
	void Start(){
		StartCoroutine (start_scene ());
	}

	/************************************************************************************************* 
	*  Function: 	void Update()
	*  Description: Runds every frame. Checks to see if necessary conditions are met to end the
	* 				level. If so, ends the level.
	************************************************************************************************/ 
	void Update(){
		if (transport_stones >= transport_stones_needed /*&& Input.GetKeyDown (KeyCode.F)*/ && !ending_level) {
			ending_level = true;
			end_scene ();
		}
	}

	/************************************************************************************************* 
	*  Function: 	Vector3 get_start_position()
	*  Description: Calculates and returns position of player's spawn at beginning of level based on 
	* 				position and rotation of start platform.
	************************************************************************************************/ 
	Vector3 get_start_position(){
		if (start == null)
			return new Vector3 (0, 0, 0);
		
		Quaternion original_player_rotation = player.transform.rotation;
		Quaternion original_platform_rotation = start.transform.rotation;
		player.transform.rotation = start.transform.rotation = Quaternion.identity;

		float start_z_rotation = round_to_nearest_multiple(original_platform_rotation.eulerAngles.z, 90f);
		float platform_slope_angle = Mathf.Abs(original_platform_rotation.eulerAngles.z - start_z_rotation);

		BoxCollider2D player_collider = player.GetComponent<BoxCollider2D> ();
		BoxCollider2D platform_collider = start.GetComponent<BoxCollider2D> ();
		float offset_from_rotation = platform_collider.bounds.extents.y / Mathf.Cos(platform_slope_angle * Mathf.Deg2Rad) + Mathf.Tan (platform_slope_angle * Mathf.Deg2Rad) * player_collider.bounds.extents.x;
		Vector3 offset = Quaternion.Euler(new Vector3(0, 0, start_z_rotation)) * new Vector3(0, player_collider.bounds.extents.y + offset_from_rotation - 0.06f, -start.position.z - 5f);	//-0.06f added to compensate for slight offset error that was occurring

		//Debug.DrawRay (player.transform.position, offset, Color.cyan, 1f);
		player.transform.rotation = original_player_rotation;
		start.transform.rotation = original_platform_rotation;

		return start.transform.position + offset;
	}

	/************************************************************************************************* 
	*  Function: 	float round_to_nearest_multiple(float, float)
	*  Description: Rounds a given number to the nearest multiple of a given factor.
	************************************************************************************************/ 
	float round_to_nearest_multiple(float number, float factor){
		float sign = Mathf.Sign (number);
		number = Mathf.Abs (number);

		return (number % factor >= factor / 2) ? (number + factor - number % factor) * sign : (number - number % factor) * sign;
	}

	/************************************************************************************************* 
	*  Function: 	float get_start_rotation()
	*  Description: Returns the start rotation of the player about the z-axis based on the rotation
	* 				of the start platform.
	************************************************************************************************/ 
	float get_start_rotation(){
		if (start == null)
			return 0;
		
		return round_to_nearest_multiple (start.transform.eulerAngles.z, 90f);
	}

	/************************************************************************************************* 
	*  Function: 	int get_start_gravity_direction()
	*  Description: Returns the initial direction of gravity based on the starting rotation of the
	* 				player.
	************************************************************************************************/ 
	int get_start_gravity_direction(){
		if (start == null)
			return 0;

		float start_tilt = get_start_rotation();

		/*Last condition is included because rotations perfectly divisible by 360 in Unity inspector will result in eulerAngles slightly off from 0. 
		If this difference is negative (From negative multiple of 360 in inspector, the default case would be executed (left gravity) even though the start is almost flat.*/
		if ((start_tilt >= 0 && start_tilt < 45f) || (start_tilt > 315f && start_tilt < 360f)) 
			return 0;	//Down Gravity
		else if (start_tilt >= 45f && start_tilt < 135f)
			return 3;	//Right Gravity
		else if (start_tilt >= 135f && start_tilt < 225f)
			return 2;	//Up Gravity
		else
			return 1;	//Left Gravity
	}


	/************************************************************************************************* 
	*  Function: 	IEnumerator start_scene()
	*  Description: Function called at start of scene. Sets the player's sprite renderer to clear 
	* 		   		so it can't be seen, removes user's ability to control player, fades in scene 
	* 		   		from current fader color (set as black in Awake()), and then starts the 
	* 		   		fade_in_player coroutine to fade the player's sprite in and return user control.
	*************************************************************************************************/ 
	IEnumerator start_scene(){
		player_controller.disable_control ();
	
		if (do_player_fade)
			player_renderer.color = new Color (1f, 1f, 1f, 0f);
	
		if (do_scene_fade) {
			Scene_Effects.fade_to_clear (1f);

			while (Scene_Effects.fader.color.a > 0.4)
				yield return null;
		}

		if(do_player_fade)
			yield return Scene_Effects.fade_in_sprite (player_renderer, 2f);

		if (do_initial_focus)
			yield return main_cam_script.focus (focus_target.transform, 0.8f, 5f, 1f);

		player_controller.enable_control ();
	}

	/************************************************************************************************* 
	*  Function: 	public void end_scene()
	*  Description: Public function for other scripts to end the scene.
	************************************************************************************************/ 
	public void end_scene(){
		StartCoroutine (_end_scene ());
	}

	/************************************************************************************************* 
	*  Function: 	IEnumerator _end_scene()
	*  Description: Coroutine that ends scene. Does transport effect and fades out scene then loads
	* 				next scene.
	************************************************************************************************/ 
	IEnumerator _end_scene(){
		GameObject transport = Instantiate(transport_effect, player.transform.position, Quaternion.identity) as GameObject;
		player.SetActive (false);
		float transport_duration = transport.GetComponent<ParticleSystem> ().duration;
		yield return new WaitForSeconds (transport_duration+1f);
		yield return Scene_Effects.fade_to_black (1f);
		yield return new WaitForSeconds (1f);	//pause on black before loading next scene

		#if UNITY_EDITOR
		int current_level_index = 0;
		string current_level_name = SceneManager.GetActiveScene().name;
		for(int i = 0; i < scene_manager.scene_names.Length; i++){
			if(scene_manager.scene_names[i] == current_level_name){
				current_level_index = i;
				break;
			}
		}

		SceneManager.LoadScene(scene_manager.scene_names[(current_level_index + 1) % scene_manager.scene_names.Length]);
	
		#else
		int loaded_level = SceneManager.GetActiveScene ().buildIndex;
		SceneManager.LoadScene(SceneManager.sceneCount % (loaded_level + 1));

		#endif
			
	}

	/************************************************************************************************* 
	*  Function: 	public void add_transport_stone()
	*  Description: Increments current number of collected transport stones.
	************************************************************************************************/ 
	public void add_transport_stone(){
		transport_stones++;
	}
}
