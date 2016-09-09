/**************************************************
 * 
 * 
 * 		CURRENTLY NOT IN USE. WILL LIKELY REWRITE
 * 		WHEN BACKGROUND METHOD DECIDED UPON.
 * 
 * 
 *************************************************/
using UnityEngine;
using System.Collections;

public class Background : MonoBehaviour {

	public Camera cam;
	public float y_parallax;
	public float x_parallax;
	private Vector3 previous_pos;
	private Vector3 cam_velocity;
	private Material[] backgrounds;
	private MeshRenderer[] renderers;
	private float x_scale;
	private float y_scale;
	public GameObject background_quad;

	void Awake () {
		
		//print (background_quad.GetComponent<MeshRenderer> ().bounds.extents.magnitude);
		instantiate_background ();//must be done before getting anything in children as this instantiates gameobject's children
		renderers = gameObject.GetComponentsInChildren<MeshRenderer> ();
		backgrounds = new Material[renderers.Length];
		for (int i = 0; i < backgrounds.Length; i++)
			backgrounds [i] = renderers [i].material;
		x_scale = gameObject.GetComponentInChildren<Transform> ().localScale.x;
		y_scale = gameObject.GetComponentInChildren<Transform> ().localScale.y;
	}

	void instantiate_background(){
		float quad_height = background_quad.transform.localScale.y;
		float quad_width = background_quad.transform.localScale.x;
		float cam_height = cam.orthographicSize * 2f;
		float cam_width = cam_height * Screen.width / Screen.height;
		int rows = Mathf.CeilToInt(cam_height / quad_height)+1;
		int columns = Mathf.CeilToInt(cam_width / quad_width)+1;
		float start_x = transform.position.x - cam_width / 2f;
		float start_y = transform.position.y + cam_height / 2f;

		for (int i = 0; i < rows; i++) {
			Vector3 background_spawn = new Vector3 (start_x, start_y - quad_height * i, transform.position.z);
			for (int j = 0; j < columns; j++) {
				GameObject tile = Instantiate (background_quad, background_spawn, transform.rotation) as GameObject;
				tile.transform.parent = gameObject.transform;
				tile.transform.position = background_spawn;
				background_spawn = new Vector3 (start_x + (j+1) * quad_width, background_spawn.y, background_spawn.z);

			}
		}
	}

	void Update(){
		//Debug.Break ();
		transform.position =  new Vector3(cam.transform.position.x, cam.transform.position.y, transform.position.z);

		Vector2 offset = backgrounds[0].mainTextureOffset;
		offset.x = (transform.position.x / x_scale) / x_parallax;
		offset.y = (transform.position.y / y_scale) / y_parallax;

		for (int i = 0; i < backgrounds.Length; i++)
			backgrounds [i].mainTextureOffset = offset;
	}
}
