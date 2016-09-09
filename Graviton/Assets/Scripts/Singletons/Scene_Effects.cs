/**************************************************************************************
** Filename: 	Scene_Effects.cs
** Author: 		Austin Row
** Date: 		8/22/16
** Description: Contains functions for performing general scene effects such
**				as fading to black to fading in sprite.
** Functions:	
**				void Awake();
**				void OnDestroy();
**				void init_fader();
**				void fader_cover_screen();
**				static public Coroutine fade_to_clear(float);
**				static public Coroutine fade_to_black(float);
**				static public Coroutine fade_in_sprite(SpriteRenderer, float);
**				IEnumerator _fade_to_clear(float);
**				IEnumerator _fade_to_black(float);
**				IEnumerator _fade_in_sprite(SpriteRenderer, float);
**************************************************************************************/
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class Scene_Effects : MonoBehaviour {

	public Image set_fader;		//Used to set fader in the inspector.
	static public Image fader;
	private bool is_duplicate = false;	//Used to make sure instance is not set to null when a duplicate is destroyed in OnDestroy()

	static public Scene_Effects instance = null;	//Used to make sure that only one instance of this script exists in the scene at any given time.

	/***************************************************************************************** 
	* Function:    void Awake()
	* Description: Called at creation of script. Ensures that only one instance of this script
	* 			   can exist in the scene at any given time. Makes function call to initialize
	* 			   screen fader properties if this is the single instance of the script.
	******************************************************************************************/
	void Awake(){
		//Can only have one instance of Scene_Effects script.
		if (instance == null) {
			instance = this;
			init_fader();
		}
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

	/***************************************************************************************** 
	* Function:    void init_fader()
	* Description: Initializes screen fader properties. Sets color to black and makes it cover
	* 			   the screen.
	******************************************************************************************/
	void init_fader(){
		fader = set_fader;
		fader.color = new Color(0f, 0f, 0f);
		fader.enabled = false;
		fader_cover_screen ();
	}

	/***************************************************************************************** 
	* Function:    void fader_cover_screen()
	* Description: Changes fader size to cover the screen.
	******************************************************************************************/
	void fader_cover_screen(){
		fader.rectTransform.localScale = new Vector2 (Screen.width, Screen.height);
	}

	/***************************************************************************************** 
	* Function:    static public Coroutine fade_to_clear(float)
	* Description: Function for other scripts to fade in screen from fader color.
	******************************************************************************************/
	static public Coroutine fade_to_clear(float speed){
		return instance.StartCoroutine (instance._fade_to_clear (speed));
	}

	/***************************************************************************************** 
	* Function:    static public Coroutine fade_to_black(float)
	* Description: Function for other scripts to fade screen to black.
	******************************************************************************************/
	static public Coroutine fade_to_black(float speed){
		return instance.StartCoroutine (instance._fade_to_black (speed));
	}

	/***************************************************************************************** 
	* Function:    static public Coroutine fade_in_sprite(SpriteRenderer, float)
	* Description: Function for other scripts to fade in sprite.
	******************************************************************************************/
	static public Coroutine fade_in_sprite( SpriteRenderer sprite_renderer, float duration){
		return instance.StartCoroutine (instance._fade_in_sprite (sprite_renderer, duration));
	}

	/***************************************************************************************** 
	* Function:    IEnumerator _fade_to_clear(float)
	* Description: Coroutine to fade in screen from current fader color.
	******************************************************************************************/
	IEnumerator _fade_to_clear(float speed){
		if (!fader.enabled)
			fader.enabled = true;

		fader_cover_screen ();

		while (fader.color.a > 0.05f) {
			fader.color = Color.Lerp (fader.color, Color.clear, speed * Time.deltaTime);
			yield return null;
		}

		fader.color = Color.clear;
		fader.enabled = false;
	}

	/***************************************************************************************** 
	* Function:    IEnumerator _fade_to_black(float)
	* Description: Coroutine to fade screen to black.
	******************************************************************************************/
	IEnumerator _fade_to_black(float speed){
		if (!fader.enabled)
			fader.enabled = true;

		fader.color = Color.clear;
		fader_cover_screen ();

		while (fader.color.a < 0.95f) {
			fader.color = Color.Lerp (fader.color, Color.black, speed * Time.deltaTime);
			yield return null;
		}

		fader.color = Color.black;
	}

	/***************************************************************************************** 
	* Function:    IEnumerator _fade_in_sprite(SpriteRenderer, float)
	* Description: Coroutine to fade in sprite.
	******************************************************************************************/
	IEnumerator _fade_in_sprite(SpriteRenderer sprite_renderer, float duration){
		float start_time = Time.time;

		while (sprite_renderer.color.a < 0.99f) {
			sprite_renderer.color = new Color (1f, 1f, 1f, Mathf.SmoothStep (0, 1, (Time.time - start_time) / duration));
			yield return null;
		}
	}
}
