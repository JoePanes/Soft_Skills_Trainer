using RealisticEyeMovements;
using UnityEngine;

public class DemoController : MonoBehaviour
{
	#region fields

		[SerializeField] Transform sphereXform = null;
		
		LookTargetController lookTargetController;

	#endregion


	void Awake()
	{
		lookTargetController = FindObjectOfType<LookTargetController>();
	}
	

	public void OnLookAtPlayerSelected()
	{
		lookTargetController.LookAtPlayer();
	}


	public void OnLookAtSphereSelected()
	{
		lookTargetController.LookAtPoiDirectly(sphereXform);
	}


	public void OnLookIdlySelected()
	{
		lookTargetController.LookAroundIdly();
	}
	
}
