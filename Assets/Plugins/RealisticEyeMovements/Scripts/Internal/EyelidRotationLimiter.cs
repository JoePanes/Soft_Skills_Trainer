// EyelidRotationLimiter.cs
// Tore Knabe
// Copyright 2020 tore.knabe@gmail.com

using System;
using UnityEngine;

namespace RealisticEyeMovements
{
	[Serializable]
	public class EyelidRotationLimiter
	{
		[Serializable]
		public class EyelidRotationLimiterForExport
		{
			public  string transformPath;
			public  SerializableQuaternion defaultQ;
			public  SerializableQuaternion closedQ;
			public  SerializableQuaternion lookUpQ;
			public  SerializableQuaternion lookDownQ;
			public  float eyeMaxDownAngle;
			public  float eyeMaxUpAngle;

			public  SerializableVector3 defaultPos;
			public  SerializableVector3 closedPos;
			public  SerializableVector3 lookUpPos;
			public  SerializableVector3 lookDownPos;

			public  bool isLookUpSet;
			public  bool isLookDownSet;
			public  bool isDefaultPosSet;
			public  bool isClosedPosSet;
			public  bool isLookUpPosSet;
			public  bool isLookDownPosSet;
		}



		#region fields

			public Transform transform;
			[SerializeField] Quaternion defaultQ;
			[SerializeField] Quaternion closedQ;
			[SerializeField] Quaternion lookUpQ;
			[SerializeField] Quaternion lookDownQ;
			[SerializeField] float eyeMaxDownAngle;
			[SerializeField] float eyeMaxUpAngle;

			[SerializeField] Vector3 defaultPos;
			[SerializeField] Vector3 closedPos;
			[SerializeField] Vector3 lookUpPos;
			[SerializeField] Vector3 lookDownPos;

			[SerializeField] bool isLookUpSet;
			[SerializeField] bool isLookDownSet;
			[SerializeField] bool isDefaultPosSet;
			[SerializeField] bool isClosedPosSet;
			[SerializeField] bool isLookUpPosSet;
			[SerializeField] bool isLookDownPosSet;

		#endregion


		public static bool CanImport(EyelidRotationLimiterForExport import, Transform startXform, string targetNameForErrorMessage=null)
		{
			return Utils.CanGetTransformFromPath(startXform, import.transformPath, targetNameForErrorMessage);
		}


		public EyelidRotationLimiterForExport GetExport(Transform startXform)
		{
			EyelidRotationLimiterForExport export = new EyelidRotationLimiterForExport
			{
					transformPath = Utils.GetPathForTransform(startXform, transform),
					defaultQ = defaultQ,
					closedQ = closedQ,
					lookUpQ = lookUpQ,
					lookDownQ = lookDownQ,
					eyeMaxDownAngle = eyeMaxDownAngle,
					eyeMaxUpAngle = eyeMaxUpAngle,
					defaultPos = defaultPos,
					closedPos = closedPos,
					lookUpPos = lookUpPos,
					lookDownPos = lookDownPos,
					isLookUpSet = isLookUpSet,
					isLookDownSet = isLookDownSet,
					isDefaultPosSet = isDefaultPosSet,
					isClosedPosSet = isClosedPosSet,
					isLookUpPosSet = isLookUpPosSet,
					isLookDownPosSet = isLookDownPosSet
			};

			return export;
		}


		public void GetRotationAndPosition( float eyeAngle, float blink01, float eyeWidenOrSquint, bool isUpper, out Quaternion rotation, ref Vector3 position, ControlData.EyelidBoneMode eyelidBoneMode )
		{
			bool isLookingDown = eyeAngle > 0;
			float angle01 = Mathf.InverseLerp(0, isLookingDown	? eyeMaxDownAngle : -eyeMaxUpAngle, eyeAngle);
					
			if ( eyeWidenOrSquint < 0 )
				blink01 = Mathf.Lerp(blink01, 1, -eyeWidenOrSquint);
			else
				eyeWidenOrSquint *= 1-blink01;

			//*** Rotation
			{
				if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Rotation )
				{
					rotation = Quaternion.Slerp(defaultQ, isLookingDown ? lookDownQ : lookUpQ, angle01);
					rotation = Quaternion.Slerp(rotation, closedQ, blink01);
					if ( eyeWidenOrSquint > 0 )
						rotation = Quaternion.Slerp(rotation, isUpper ? lookUpQ : lookDownQ, eyeWidenOrSquint);
				}
				else
					rotation = Quaternion.identity;
			}

			//*** Position
			{
				if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Position )
				{
					if ( isLookingDown )
					{
						if ( isDefaultPosSet && isLookDownPosSet )
							position = Vector3.Lerp(defaultPos, lookDownPos, angle01);
					}
					else
					{
						if ( isDefaultPosSet && isLookUpPosSet )
							position = Vector3.Lerp(defaultPos, lookUpPos, angle01);
					}

					if ( isDefaultPosSet && isClosedPosSet )
						position = Vector3.Lerp(position, closedPos, blink01);
					if ( eyeWidenOrSquint > 0 )
						position = Vector3.Lerp(position, isUpper ? lookUpPos : lookDownPos, eyeWidenOrSquint);
				}
			}			
		}


		public static EyelidRotationLimiter Import(EyelidRotationLimiterForExport import, Transform startXform)
		{
			EyelidRotationLimiter limiter = new EyelidRotationLimiter
			{
				transform = Utils.GetTransformFromPath(startXform, import.transformPath),
				defaultQ = import.defaultQ,
				closedQ = import.closedQ,
				lookUpQ = import.lookUpQ,
				lookDownQ = import.lookDownQ,
				eyeMaxDownAngle = import.eyeMaxDownAngle,
				eyeMaxUpAngle = import.eyeMaxUpAngle,
				defaultPos = import.defaultPos,
				closedPos = import.closedPos,
				lookUpPos = import.lookUpPos,
				lookDownPos = import.lookDownPos,
				isLookUpSet = import.isLookUpSet,
				isLookDownSet = import.isLookDownSet,
				isDefaultPosSet = import.isDefaultPosSet,
				isClosedPosSet = import.isClosedPosSet,
				isLookUpPosSet = import.isLookUpPosSet,
				isLookDownPosSet = import.isLookDownPosSet,
			};
			
			return limiter;
		}


		public void PrettyPrint()
		{
			Debug.Log("default rotation: " + defaultQ.x + " " + defaultQ.y + " " + defaultQ.z + " " + defaultQ.w);
			Debug.Log("closed rotation: " + closedQ.x + " " + closedQ.y + " " + closedQ.z + " " + closedQ.w);
			Debug.Log("default pos: " + defaultPos.x + " " + defaultPos.y + " " + defaultPos.z);
			Debug.Log("closed pos: " + closedPos.x + " " + closedPos.y + " " + closedPos.z);
		}
		
		
		public void RestoreClosed(ControlData.EyelidBoneMode eyelidBoneMode)
		{
			if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Rotation )
				transform.localRotation = closedQ;

			if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Position )
				if ( isClosedPosSet )
					transform.localPosition = closedPos;
		}


		public void RestoreDefault(ControlData.EyelidBoneMode eyelidBoneMode)
		{
			if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Rotation )
				transform.localRotation = defaultQ;

			if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Position )
				if ( isDefaultPosSet )
					transform.localPosition = defaultPos;
		}


		public void RestoreLookDown(ControlData.EyelidBoneMode eyelidBoneMode)
		{
			if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Rotation )
				transform.localRotation = lookDownQ;

			if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Position )
				if ( isLookDownPosSet )
					transform.localPosition = lookDownPos;
		}


		public void RestoreLookUp(ControlData.EyelidBoneMode eyelidBoneMode)
		{
			if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Rotation )
				transform.localRotation = lookUpQ;

			if ( eyelidBoneMode == ControlData.EyelidBoneMode.RotationAndPosition || eyelidBoneMode == ControlData.EyelidBoneMode.Position )
				if ( isLookUpPosSet)
					transform.localPosition = lookUpPos;
		}


		public void SaveClosed()
		{
			closedQ = transform.localRotation;
			closedPos = transform.localPosition;
			isClosedPosSet = true;
		}


		public void SaveDefault( Transform t )
		{
			transform = t;

			defaultQ = t.localRotation;
					if (false == isLookDownSet)
						lookDownQ = defaultQ * Quaternion.Euler(20, 0, 0);
					if (false == isLookUpSet)
						lookUpQ = defaultQ * Quaternion.Euler(-8, 0, 0);

			defaultPos = t.localPosition;
					isDefaultPosSet = true;
					if ( false == isLookUpPosSet )
						lookUpPos = defaultPos;
					if ( false == isLookDownPosSet )
						lookDownPos = defaultPos;
					if ( false == isClosedPosSet )
						closedPos = defaultPos;
		}


		public void SaveLookDown(float eyeMaxDownAngle)
		{
			this.eyeMaxDownAngle = eyeMaxDownAngle;
			lookDownQ = transform.localRotation;
			lookDownPos = transform.localPosition;
			isLookDownSet = true;
			isLookDownPosSet = true;
		}

		
		public void SaveLookUp(float eyeMaxUpAngle)
		{
			this.eyeMaxUpAngle = eyeMaxUpAngle;
			lookUpQ = transform.localRotation;
			lookUpPos = transform.localPosition;
			isLookUpSet = true;
			isLookUpPosSet = true;
		}
	}

}