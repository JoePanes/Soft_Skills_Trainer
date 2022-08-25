// EyeRotationLimiter.cs
// Tore Knabe
// Copyright 2020 tore.knabe@gmail.com

using System;
using UnityEngine;

namespace RealisticEyeMovements
{
	[Serializable]
	public class EyeRotationLimiter
	{

		[Serializable]
		public class EyeRotationLimiterForExport
		{
			public string transformPath;
			public SerializableQuaternion defaultQ;
			public SerializableQuaternion lookUpQ;
			public SerializableQuaternion lookDownQ;
			public SerializableQuaternion lookLeftQ;
			public SerializableQuaternion lookRightQ;

			public bool isLookUpSet;
			public bool isLookDownSet;
			public bool isLookLeftSet;
			public bool isLookRightSet;
		}

		#region fields

			public Transform transform;
			[SerializeField] Quaternion defaultQ;
			[SerializeField] Quaternion lookUpQ;
			[SerializeField] Quaternion lookDownQ;
			[SerializeField] Quaternion lookLeftQ;
			[SerializeField] Quaternion lookRightQ;

			public float maxDownAngle = 20;
			public float maxLeftAngle = 30;
			public float maxRightAngle = 30;
			public float maxUpAngle = 8;

			[SerializeField] bool isLookUpSet;
			[SerializeField] bool isLookDownSet;
			[SerializeField] bool isLookLeftSet;
			[SerializeField] bool isLookRightSet;

		#endregion


		public bool CanImport(EyeRotationLimiterForExport import, Transform startXform, string targetNameForErrorMessage=null)
		{
			return Utils.CanGetTransformFromPath(startXform, import.transformPath, targetNameForErrorMessage);
		}

		
		public float ClampHorizAngle( float angle )
		{
			return Mathf.Clamp( Utils.NormalizedDegAngle(angle), -maxLeftAngle, maxRightAngle );
		}


		public float ClampVertAngle( float angle )
		{
			return Mathf.Clamp( Utils.NormalizedDegAngle(angle), -maxUpAngle, maxDownAngle );
		}


		float ComputeMaxAngle(Quaternion lookQ)
		{
			return Utils.NormalizedDegAngle(Quaternion.Angle(defaultQ, lookQ));
		}
		
		
		public EyeRotationLimiterForExport GetExport(Transform startXform)
		{
			EyeRotationLimiterForExport export = new EyeRotationLimiterForExport
			{
				transformPath = Utils.GetPathForTransform(startXform, transform),
				defaultQ = defaultQ,
				lookUpQ = lookUpQ,
				lookDownQ = lookDownQ,
				lookLeftQ = lookLeftQ,
				lookRightQ = lookRightQ,
				isLookUpSet = isLookUpSet,
				isLookDownSet = isLookDownSet,
				isLookLeftSet = isLookLeftSet,
				isLookRightSet = isLookRightSet
			};

			return export;
		}


		public float GetEyeDown01( float angle )
		{
			return angle <= 0 ? 0 : Mathf.InverseLerp(0, maxDownAngle, angle);
		}


		public float GetEyeLeft01( float angle )
		{
			return angle >= 0 ? 0 : Mathf.InverseLerp(0, maxLeftAngle, -angle);
		}


		public float GetEyeRight01( float angle )
		{
			return angle <= 0 ? 0 : Mathf.InverseLerp(0, maxRightAngle, angle);
		}


		public float GetEyeUp01( float angle )
		{
			return angle >= 0 ? 0 : Mathf.InverseLerp(0, maxUpAngle, -angle);
		}


		public void Import( EyeRotationLimiterForExport import, Transform targetXform )
		{
			transform = targetXform;
			defaultQ = import.defaultQ;
			lookUpQ = import.lookUpQ;
			lookDownQ = import.lookDownQ;
			lookLeftQ = import.lookLeftQ;
			lookRightQ = import.lookRightQ;
			isLookUpSet = import.isLookUpSet;
			isLookDownSet = import.isLookDownSet;
			isLookLeftSet = import.isLookLeftSet;
			isLookRightSet = import.isLookRightSet;

			UpdateMaxAngles();
		}


		public void Initialize()
		{
			UpdateMaxAngles();
		}
		
		
		public void RestoreDefault()
		{
			transform.localRotation = defaultQ;
		}


		public void RestoreLookDown()
		{
			transform.localRotation = lookDownQ;
		}


		public void RestoreLookLeft()
		{
			transform.localRotation = lookLeftQ;
		}
		
		
		public void RestoreLookRight()
		{
			transform.localRotation = lookRightQ;
		}
		
		
		public void RestoreLookUp()
		{
			transform.localRotation = lookUpQ;
		}
		

		public void SaveDefault( Transform t )
		{
			transform = t;
			defaultQ = t.localRotation;
			if (false == isLookDownSet)
				lookDownQ = defaultQ * Quaternion.Euler(maxDownAngle, 0, 0);
			if (false == isLookLeftSet)
				lookLeftQ = defaultQ * Quaternion.Euler(0, -maxLeftAngle, 0);
			if (false == isLookRightSet)
				lookRightQ = defaultQ * Quaternion.Euler(0, maxRightAngle, 0);
			if (false == isLookUpSet)
				lookUpQ = defaultQ * Quaternion.Euler(-maxUpAngle, 0, 0);
			
			UpdateMaxAngles();
		}


		public void SaveLookDown()
		{
			lookDownQ = transform.localRotation;
			isLookDownSet = true;
			UpdateMaxAngles();
		}

		
		public void SaveLookLeft()
		{
			lookLeftQ = transform.localRotation;
			isLookLeftSet = true;
			UpdateMaxAngles();
		}

		
		public void SaveLookRight()
		{
			lookRightQ = transform.localRotation;
			isLookRightSet = true;
			UpdateMaxAngles();
		}

		
		public void SaveLookUp()
		{
			lookUpQ = transform.localRotation;
			isLookUpSet = true;
			UpdateMaxAngles();
		}
		
		
		void UpdateMaxAngles()
		{
			if ( isLookLeftSet )
				maxLeftAngle = ComputeMaxAngle(lookLeftQ);
			if ( isLookRightSet )
				maxRightAngle = ComputeMaxAngle(lookRightQ);
			if ( isLookDownSet)
				maxDownAngle = ComputeMaxAngle(lookDownQ);
			if ( isLookUpSet )
				maxUpAngle = ComputeMaxAngle(lookUpQ);
		}
		
		
	}

}