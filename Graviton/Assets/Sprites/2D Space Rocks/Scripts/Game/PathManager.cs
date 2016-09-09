using UnityEngine;
using System.Collections;

public class PathManager : MonoBehaviour {
	
	private GameObject CurrentPoint;
	private GameObject NextPoint;
	public GameObject [] Points;
	private int index=0;
	public bool Loop=false;
	// Use this for initialization
	void Start () {
		if(Camera.main.GetComponent<FollowCamera>()==null)
			Camera.main.gameObject.AddComponent<FollowCamera>();

		StartCoroutine("GoToNextPoint");
	}

	public float delay = 1f;

	IEnumerator  GoToNextPoint()
	{
		while (index < Points.Length)
		{

			if(Points[index]!=null)
			{
				CurrentPoint  =  Points [index];
			
				if(index + 1  < Points.Length)
				NextPoint  =  Points [index + 1];
				else
					if(Loop)
						index=-1;
				Camera.main.GetComponent<FollowCamera>().target = CurrentPoint;
				Camera.main.GetComponent<FollowCamera>().NextTarget();
				index++;
				//yield return new WaitForSeconds(delay);
			
			}
			else { 
				break;}

			yield return new WaitForSeconds(delay);
		}

	}


}
