/****************************************************************************************************
** Note:		CREDIT FOR THIS SCRIPT GOES TO USER Bunny83 FROM THE UNITY ANSWERS FORUMS.
**				HIS POST CAN BE FOUND HERE: 
**				answers.unity3d.com/questions/33263/how-to-get-names-of-all-available-levels.html#answer-245867
** Description: Keeps list of scene names for enabled scenes in Unity Build Settings.
**				This is useful mainly for testing in the editor when the scenes in
**				SceneManager don't necessarily include all of the scenes enabled in
**				the build settings.
** Functions:
**				static string[] read_names();
**				static void update_names(UnityEditor.MenuCommand);
**				void reset();
***************************************************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Scene_Manager : MonoBehaviour {

	public string[] scene_names;

	#if UNITY_EDITOR

	/****************************************************************************************** 
	* Function:    static string[] read_names()
	* Description: Constructs and returns array of scene names of all scenes in the Unity 
	* 			   Editor Build Settings.
	******************************************************************************************/
	static string[] read_names(){
		List<string> temp = new List<string> ();

		foreach (UnityEditor.EditorBuildSettingsScene scene in UnityEditor.EditorBuildSettings.scenes) {
			if (scene.enabled) {
				string name = scene.path.Substring (scene.path.LastIndexOf ('/') + 1);	//The scene name comes after the final forward slash in the scene's path.
				name = name.Substring (0, name.Length - 6);		//Removes the .unity extension from the scene name.
				temp.Add (name);
			}
		}

		return temp.ToArray ();
	}

	/****************************************************************************************** 
	* Function:    static void update_names()
	* Description: Function used by Unity Editor Component Menu for script to update the list
	* 			   of names taken from the Unity Editor Build Settings by calling read_names().
	******************************************************************************************/
	[UnityEditor.MenuItem("CONTEXT/Scene_Manager/Update Scene Names")]
	static void update_names(UnityEditor.MenuCommand command){
		Scene_Manager context = (Scene_Manager)command.context;
		context.scene_names = read_names ();
	}

	/****************************************************************************************** 
	* Function:    void reset()
	* Description: Sets scene_names to list of names of all scenes in Unity Editor Build 
	* 			   Settings.
	******************************************************************************************/
	void reset(){
		scene_names = read_names ();
	}

	#endif

}
