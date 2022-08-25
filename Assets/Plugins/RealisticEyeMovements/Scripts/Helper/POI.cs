using UnityEngine;


namespace RealisticEyeMovements
{
	public class POI : MonoBehaviour
	{
		#region fields

			[SerializeField] Color color = Color.white;

		#endregion


	    void OnDrawGizmos()
	    {
		    Gizmos.color = color;
	        Gizmos.DrawSphere(transform.position, 0.05f);
	    }
	    

	}
}