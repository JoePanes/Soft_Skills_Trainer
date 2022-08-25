using System;
using UnityEngine;

namespace RealisticEyeMovements {

	// This should run after all other scripts.
	// Scripts that need to do something after all others' LateUpdate can subscribe here.
	[DefaultExecutionOrder(999999)]
	public class VeryLateUpdateCallback : MonoBehaviour
	{
		#region fields

			public event Action onVeryLateUpdate;

		#endregion


		void Start()
		{
			hideFlags = HideFlags.HideInInspector;
		}
		
		
		void LateUpdate()
		{
			if ( onVeryLateUpdate != null )
				onVeryLateUpdate.Invoke();
		}
		
	}
}