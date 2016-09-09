/***************************************************************************
** Filename: 	Flamethrower.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Controls all aspects of flamethrowers such as timing, 
**				managing the shrinking and growing of killzones with the 
**				flames, and managing the glow of the flames on the
**				surrounding environment.
** Functions:
**				void Awake();
**				void Start();
**				IEnumerator run_flamethrower();
**				IEnumerator fade_up_killzone(float active_time);
**				IEnumerator fade_out_killzone();
**				void initialize_glow();
**				IEnumerator fade_glow_up();
**				IEnumerator fade_glow_out();
**				
***************************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Flamethrower : MonoBehaviour {

	private ParticleSystem flamethrower;
	public float[] active_lengths;
	public float[] pause_lengths;
	public float start_delay = 0;
	public GameObject collider_gameobject;
	private List<BoxCollider2D> active_killzones;
	private float flame_speed;
	private float remaining_time;
	private float remaining_distance; //remaining distance for collider to travel if flame does not get to fully extend (active_length < startlifetime)
	public bool is_on = true;
	private Light[] glow;
	[SerializeField]
	private float light_spacing = 3f;
	public float glow_range = 5f;

	/*********************************************************************************************** 
	*  Function: 	void Awake()
	*  Description: Called at creation of script. Initializes list of active killzones (trigger 
	* 		   		colliders), gets reference to gameobject's particle system (the flamethrower 
	* 		   		effect), and gets start speed of flamethrower particle system. Initializes
	* 		   		lighting acting as flames' glow.
	***********************************************************************************************/
	void Awake(){
		active_killzones = new List<BoxCollider2D> ();
		flamethrower = gameObject.GetComponent<ParticleSystem> ();
		flame_speed = flamethrower.startSpeed;	//Note: Using this assumes that the startSpeed of the flamethrower particle system is not changed at any time
		initialize_glow ();
	}

	/************************************************************************************************* 
	*  Function:	void Start()
	*  Description: Function called at start of script. Logs error in console and pauses editor
	* 		        if there is not exactly one pause length for every active length of the 
	* 		        flamethrower (only if running in the editor, does not effect standalone builds).
	* 		        Also starts the coroutine that runs the flamethrower.
	*************************************************************************************************/
	void Start(){
		#if UNITY_EDITOR
		if (active_lengths.Length != pause_lengths.Length) {
			Debug.LogError ("Flamethrower active_lengths and pause_lengths arrays must be same size.");
			Debug.Break();
		}
		#endif
		StartCoroutine (run_flamethrower ());
	}


	/************************************************************************************************* 
	*  Function: 	IEnumerator run_flamethrower()
	*  Description: Runs as long as the flamethrower is on. Cycles through arrays of active and
	* 		   		pause times, appropriately cycling between shooting flame and not shooting 
	* 		  	    flame.
	*************************************************************************************************/
	IEnumerator run_flamethrower(){
		float offset_adjustment = 0;
		float start_time = Time.time;
		yield return new WaitForSeconds (start_delay);
		float supposed_total_time = start_delay;

		while (is_on) {
			for (int i = 0; i < active_lengths.Length; i++) {
				flamethrower.Play ();
				StartCoroutine(fade_glow_up());
				yield return StartCoroutine (fade_up_killzone (active_lengths[i]));	
				yield return new WaitForSeconds (remaining_time);	//will wait only if killzone collider should remain in place at its max length (if active time > lifetime of particles so collider spans entire range of flamethrower)
		
				flamethrower.Stop ();
				StartCoroutine (fade_glow_out ());
				StartCoroutine (fade_out_killzone ());	//Moves killzone with flame as it travels forward through the remaining flamethrower range and shrinks killzone with flame as it fades
				yield return new WaitForSeconds (pause_lengths [i]-offset_adjustment);	//Pauses flamethrower for specified amount of time minus the timing error from the last cycle.

				supposed_total_time += active_lengths [i] + pause_lengths [i];
				offset_adjustment = (Time.time-start_time-supposed_total_time);		//Approximate amount of error in flame timing as a result from imperfection in floating point numbers.
				/*if(print_offset)
					print("Total Alignment Error: "+(Time.time-start_time-supposed_total_time));*/
			}
		}

		flamethrower.Stop ();
	}

	/************************************************************************************************* 
	*  Function: 	IEnumerator fade_up_killzone(float)
	*  Description: Called every time flamethrower starts up and begins putting out a new flame.
	* 		   		Creates and scales up killzone collider to match flame as it first comes out 
	* 		   		of flamethrower and grows to its max length. Sets remaining distance for 
	* 		   		collider to travel to reach the edge of the flamethrower range after it has
	* 		   		scaled up to match the full length of the flame being created. Sets remaining
	* 		   		time for killzone collider to stay in place after it has scaled up to match full
	* 		   		length of flame being created.
	* 
	* 		   		Note: Creates a new collider and adds it to list of currently active killzone
	* 				colliders in case there are several bursts of flame travelling in line
	* 				from flamethrower (needs a killzone collider for each burst).
	**************************************************************************************************/
	IEnumerator fade_up_killzone(float active_time){
		//Create, initialize, and add new killzone collider to active killzones list.
		BoxCollider2D killzone = collider_gameobject.AddComponent<BoxCollider2D> () as BoxCollider2D;
		killzone.isTrigger = true;
		active_killzones.Add (killzone);

		//store how long flamethrower will be producing new flame and set remaining time for collider to stay in place once reaching max length as well as the remaining distance it has to travel to reach end of flamethrower range
		float producing_time;
		if (flamethrower.startLifetime > active_time) {
			producing_time = active_time;
			remaining_time = 0;
			remaining_distance = flamethrower.startLifetime - active_time;
		} else {
			producing_time = flamethrower.startLifetime;
			remaining_time = active_time - flamethrower.startLifetime;
			remaining_distance = 0;
		}

		//Store maximum length that flame will reach and initialize killzone size and offset
		float max_length = (producing_time * flame_speed) ;	//do this calculation every time in the while loop condition to compensate for changing flame speed (have to use flamethrower.startSpeed instead of flame_speed for this)
		killzone.offset = new Vector2(0f, 0.1f);
		killzone.size = new Vector2 (0.6f, 0.2f);

		//scale up killzone with flame until both reach max length at same time
		while (killzone.size.y < max_length) {
			float grow_rate = Time.deltaTime * flame_speed;	//time elapsed * speed = distance traveled = how much more space flame has covered that killzone now needs to cover
			killzone.size = new Vector2 (killzone.size.x, killzone.size.y + grow_rate);
			killzone.offset = new Vector2 (0f, killzone.offset.y + grow_rate/2);	//push it forward in flame direction by half as much as it grew to compensate for the half of the size growth that went backwards
			yield return null;
		}
	}

	/********************************************************************************************** 
	*  Function: 	IEnumerator fade_out_killzone()
	*  Description: Moves killzone collider along with flame until it reaches end of flamethrower
	* 		   		range (if flame and collider do not already extend full length of range) and 
	* 		   		shrinks collider with flame as it fades out.
	**********************************************************************************************/
	IEnumerator fade_out_killzone(){
		//Retrieve the killzone collider at front of progression of active killzone colliders and remove it from active list.
		BoxCollider2D killzone = active_killzones [0];
		active_killzones.Remove (killzone);

		//Store remaining distance for collider/flame to travel before reaching end of flamethrower range (will be 0 if active time for flame is longer than startLifetime/range)
		float remaining = remaining_distance*flame_speed; //using remaining_distance at beginning of this coroutine frees it to be used by next call to fade_up_killzone coroutine
		while(remaining > 0){
			float move_amount = Time.deltaTime * flame_speed;
			killzone.offset = new Vector2 (0f, move_amount+killzone.offset.y);
			remaining -= move_amount;
			yield return null;
		}

		//After killzone collider has traveled so that it is up against the max range of the flamethrower, shrink it with the flame to certain point then destroy it.
		while (killzone.size.y > 1f) {
			float shrink_rate = Time.deltaTime * flame_speed;
			killzone.size = new Vector2 (killzone.size.x, killzone.size.y - shrink_rate);
			killzone.offset = new Vector2 (0f, killzone.offset.y + shrink_rate/2);
			yield return null;
		}

		Destroy (killzone);
	}

	/*********************************************************************************************** 
	*  Function: 	IEnumerator intialize_glow()
	*  Description: Creates orange point lights as child objects of flamethrower parent object and
	* 		   		spaces them evenly (other than the first one which is always in the same place
	* 		   		near the base of the flamethrower) along lenght of the flamethrower range.
	***********************************************************************************************/
	void initialize_glow(){
		glow = new Light[Mathf.CeilToInt(flamethrower.startLifetime*flame_speed/light_spacing)];
		GameObject new_light_object;
		for (int i = 0; i < glow.Length; i++) {
			new_light_object = new GameObject ("Glow Light" + i);
			new_light_object.transform.parent = collider_gameobject.transform;
			Light new_light = new_light_object.AddComponent<Light> () as Light;
			new_light.type = LightType.Point;
			new_light.color = new Color (255f/255f, 118f/255f, 56f/255f);
			new_light.range = glow_range;
			new_light.intensity = 0f;
			if (i != 0)
				new_light.transform.localPosition = new Vector3 (0f, light_spacing * i, -2f);
			else
				new_light.transform.localPosition = new Vector3 (0f, 1f, -2f);
			glow [i] = new_light;
		}
	}
		
	/***************************************************************************************** 
	*  Function: 	IEnumerator fade_glow_up()
	*  Description: Coroutine to fade in lights as flame reaches them to act as flame's glow.
	******************************************************************************************/
	IEnumerator fade_glow_up(){
		Light_Effects.fade_in(glow[0], 30f);
		float interval_time = light_spacing / flame_speed;	//time it takes flame to travel distance between two adjacent glow lights (time = distance/speed)
		for (int i = 1; i < glow.Length; i++) {
			yield return new WaitForSeconds(interval_time);	//wait until flame has had just enough time to travel to next glow light
			Light_Effects.fade_in(glow[i], 30f);
		}
	}

	/********************************************************************************************** 
	*  Function: 	IEnumerator fade_glow_up()
	*  Description: Coroutine to fade out glow lights as flame's tail end passes and leaves them.
	* 
	* 		   Note: This coroutine is called after the flamethrower has stopped for a pause.
	* 				 Therefore we do not need to wait for portion of flame between two 
	* 				 endpoints to come out because it has already done so.
	**********************************************************************************************/
	IEnumerator fade_glow_out(){
		Light_Effects.fade_out(glow[0], 30f);
		float interval_time = light_spacing / flame_speed;	//time it takes flame to travel distance between two adjacent glow lights (time = distance/speed)
		for (int i = 1; i < glow.Length; i++) {
			yield return new WaitForSeconds (interval_time);	//wait until flame's end has had just enough time to travel to next glow light
			Light_Effects.fade_out(glow[i], 30f);	//fade out next glow light as tail end of flame leaves
		}
		yield return null;
	}
}
