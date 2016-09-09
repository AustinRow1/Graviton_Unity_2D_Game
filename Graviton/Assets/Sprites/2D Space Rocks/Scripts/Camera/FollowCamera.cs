using UnityEngine;
using System.Collections;

public class FollowCamera : MonoBehaviour {

	public float CameraSpeed;
	float interpVelocity;
	public GameObject target;
	//True if want the camera to follow constantly the given target.
	public bool FollowConstantly = false;
	public Vector3 offset;
	Vector3 targetPos;
	Vector3 targetDirection;
	// Use this for initialization
	void Start () {
		targetPos = transform.position;
	}

	public void NextTarget()
	{
		Vector3 posNoZ = transform.position;
		posNoZ.z = target.transform.position.z;
		
		targetDirection = (target.transform.position - posNoZ);
	}

	// Update is called once per frame
	void FixedUpdate () {
		if (target)
		{
		
			if(FollowConstantly)
			{
				NextTarget();
			}

			interpVelocity = targetDirection.magnitude * 5f;

			targetPos = transform.position + (targetDirection.normalized * interpVelocity * Time.deltaTime); 

			transform.position = Vector3.Lerp( transform.position, targetPos + offset, CameraSpeed * Time.deltaTime);

		}
	}
}
