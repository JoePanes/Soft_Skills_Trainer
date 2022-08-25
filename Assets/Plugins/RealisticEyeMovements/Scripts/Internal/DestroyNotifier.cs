// DestroyNotifier.cs
// Tore Knabe
// Copyright 2019 ioccam@ioccam.com

using System;
using UnityEngine;

namespace RealisticEyeMovements {

	public class DestroyNotifier : MonoBehaviour
	{

		public event Action<DestroyNotifier> OnDestroyedEvent;


		void OnDestroyed()
		{
			if ( OnDestroyedEvent != null )
				OnDestroyedEvent( this );
		}

	}

}