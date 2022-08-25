using System;
using UnityEngine;

namespace RealisticEyeMovements {

	// This should run before all other scripts.
	// Scripts that need to do something before all others' Update can subscribe here.
	[DefaultExecutionOrder(-999999)]
	public class EarlyUpdateCallback : MonoBehaviour
	{
		#region fields

			public event Action onEarlyUpdate;

		#endregion


		void Start()
		{
			hideFlags = HideFlags.HideInInspector;
		}
		
		
		void Update()
		{
			if ( onEarlyUpdate != null )
				onEarlyUpdate.Invoke();
		}
		
	}
}