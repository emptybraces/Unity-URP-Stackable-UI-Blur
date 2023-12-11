using UnityEngine;

namespace UIBlur_Demo
{
	public class Move : MonoBehaviour
	{
		[SerializeField] float move;
		void Update()
		{
			transform.position = new Vector3(Mathf.Sin(Time.time) * move, transform.position.y, transform.position.z);
		}
	}

}