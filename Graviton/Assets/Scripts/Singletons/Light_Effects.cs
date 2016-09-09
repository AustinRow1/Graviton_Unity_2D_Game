/**************************************************************************************
** Filename: 	Light_Effects.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Contains functions for handling individual light effects such
**				fading, flaring, and pulsing (to be implemented in the future when 
**				needed).
** Functions:	
**				void Awake();
**				void OnDestroy();
**				static public Coroutine flare(Light, float);
**				static public Coroutine fade_in(Light, float);
**				static public Coroutine fade_out(Light, float);
**				IEnumerator _flare(Light, float);
**				IEnumerator _fade_in(Light, float);
**				IEnumerator _fade_out(Light, float);
**************************************************************************************/
using UnityEngine;
using System.Collections;

public class Light_Effects : MonoBehaviour{

	private bool is_duplicate = false;		//Used to make sure instance is not set to null when a duplicate is destroyed in OnDestroy()
	static public Light_Effects instance = null;

	/***************************************************************************************** 
	* Function:    void Awake()
	* Description: Called at creation of script. Ensures that only one instance of this script
	* 			   can exist in the scene at any given time.
	******************************************************************************************/
	void Awake(){
		if (instance == null)
			instance = this;
		else if (instance != this) {
			is_duplicate = true;
			Destroy (this);
		}
	}

	/***************************************************************************************** 
	* Function:    void OnDestroy()
	* Description: Sets script instance to null when instance of script is destroyed.
	******************************************************************************************/
	void OnDestroy(){
		if(!is_duplicate)	//Ensures that instance is only set to null if there are truly no instances of this script in the scene.
			instance = null;
	}

	/*static public void pulse(int flares){
		//Does multiple flares in a row.
	}*/

	/***************************************************************************************** 
	*  Function: 	static public Coroutine flare(Light, float)
	*  Description: Function for other scripts to use flare effect.
	******************************************************************************************/
	static public Coroutine flare(Light light, float flare_range){
		return instance.StartCoroutine (instance._flare (light, flare_range));
	}

	/***************************************************************************************** 
	*  Function: 	static public Coroutine fade_in(Light, float)
	*  Description: Function for other scripts to fade-in light effect.
	******************************************************************************************/
	static public Coroutine fade_in(Light light, float speed){
		return instance.StartCoroutine (instance._fade_in (light, speed));
	}

	/***************************************************************************************** 
	*  Function: 	static public Coroutine fade_out(Light, float)
	*  Description: Function for other scripts to use fade-out light effect.
	******************************************************************************************/
	static public Coroutine fade_out(Light light, float speed){
		return instance.StartCoroutine (instance._fade_out (light, speed));
	}

	/***************************************************************************************** 
	*  Function: 	IEnumerator _flare(Light, float)
	*  Description: Flares a light by rapidly increasing then decreasing the light's range.
	******************************************************************************************/
	IEnumerator _flare(Light light, float flare_range){
		float initial_range = light.range;
		float flare_speed = 20f;

		while (light.range < flare_range) {
			light.range += flare_speed * 1.5f * Time.deltaTime;
			yield return null;
		}

		yield return null;	//Must include this for fadeout or it will run both while loops which doesn't allow range to ever decrease.

		while (light.range > initial_range) {
			light.range -= flare_speed * Time.deltaTime;
			yield return null;
		}

	}

	/***************************************************************************************** 
	*  Function: 	IEnumerator _fade_in(Light, float)
	*  Description: Brightens a single light to full brightness over time period.
	******************************************************************************************/
	IEnumerator _fade_in(Light light, float speed){
		//If light is still fading out, put it at 0 brightness so that this light's fade out coroutine will stop
		if (light.intensity != 0) {
			light.intensity = 0;
			yield return null;
		}

		while (light.intensity < 8) {
			light.intensity += speed * Time.deltaTime ;
			yield return null;
		}
	}

	/***************************************************************************************** 
	*  Function: 	IEnumerator _fade_out(Light, float)
	*  Description: Fades out a single light to 0 brightness over time period.
	******************************************************************************************/
	IEnumerator _fade_out(Light light, float speed){
		//If light is still fading up, put it at max brightness so that this light's fade up coroutine will stop.
		if (light.intensity != 8f) {
			light.intensity = 8f;
			yield return null;
		}
		while (light.intensity > 0) {
			light.intensity -=  speed * Time.deltaTime;
			yield return null;
		}
	}
}
