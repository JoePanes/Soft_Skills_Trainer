// Utils.cs
// Tore Knabe
// Copyright 2020 tore.knabe@gmail.com

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace RealisticEyeMovements {


	public static class Utils
	{

		static readonly Dictionary<string, GameObject> dummyObjects = new Dictionary<string, GameObject>();


		public static bool AreTransformsChildrenOf(List<Transform> children, Transform ancestor)
		{
			foreach (Transform child in children)
				if ( false == child.IsChildOf(ancestor) )
					return false;
			
			return true;
		}
		
		
		public static bool CanGetTransformFromPath(Transform startXform, string path, string targetNameForErrorMessage=null)
		{
			if ( string.IsNullOrEmpty(path) )
				return true;
			
			if ( null != GetTransformFromPath(startXform, path) )
				return true;

			if ( targetNameForErrorMessage != null )
				Debug.LogError(startXform.name + ": Cannot find path for " + targetNameForErrorMessage + ": " + path, startXform.gameObject);
			else
				Debug.LogError(startXform.name + ": Cannot find path " + path, startXform.gameObject);

			return false;				
		}
		
	
		public static float Fbm(Vector2 coord, int octave)
	    {
	        var f = 0.0f;
	        var w = 1.0f;
	        for (var i = 0; i < octave; i++)
	        {
	            f += w * (Mathf.PerlinNoise(coord.x, coord.y) - 0.5f);
	            coord *= 2;
	            w *= 0.5f;
	        }
	        return f;
	    }

	
		public static GameObject FindChildInHierarchy(GameObject go, string name)
		{
			if (go.name == name)
				return go;
		
			foreach (Transform t in go.transform)
			{
				GameObject foundGO = FindChildInHierarchy(t.gameObject, name);
				if (foundGO != null)
					return foundGO;
			}
		
			return null;
		}
	
	
	
		public static Transform GetCommonAncestor( Transform xform1, Transform xform2 )
		{
			List<Transform> ancestors1 = new List<Transform> { xform1 };

			while ( xform1.parent != null )
			{
				xform1 = xform1.parent;
				ancestors1.Add( xform1 );
			}

			while ( xform2.parent != null && false == ancestors1.Contains( xform2 ))
				xform2 = xform2.parent;

			return ancestors1.Contains( xform2) ? xform2 : null;
		}



		public static string GetPathForTransform(Transform startXform, Transform targetXform)
		{
			if ( startXform == null || targetXform == null )
				return null;
			
			if ( false == targetXform.IsChildOf(startXform) )
				return null;
			
			List<string> path = new List<string>();
			Transform xform = targetXform;

			while ( xform != startXform && xform != null )
			{
				path.Add(xform.name);
				xform = xform.parent;
			}
			
			path.Reverse();
			
			string pathStr = string.Join("/", path.ToArray());

			return pathStr;
		}
		

		public static Transform GetSpineBoneFromAnimator(Animator animator)
		{
			Transform spineXform = animator.GetBoneTransform(HumanBodyBones.UpperChest);
			if (spineXform == null)
				spineXform = animator.GetBoneTransform(HumanBodyBones.Chest);
			if (spineXform == null)
				spineXform = animator.GetBoneTransform(HumanBodyBones.Spine);
			
			return spineXform;
		}
		
		
		public static Transform GetTransformFromPath(Transform startXform, string path)
		{
			if ( string.IsNullOrEmpty(path) )
				return null;

			return startXform.Find(path);
		}


		#if UNITY_2020_0_OR_NEWER
	    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	    static void InitializeOnLoad()
	    {
	        dummyObjects.Clear();;   
	    }
	    #endif

		
		public static bool IsEqualOrDescendant(Transform ancestor, Transform descendant)
		{
			if ( ancestor == descendant )
				return true;

			foreach (Transform t in ancestor.transform)
				if ( IsEqualOrDescendant(t, descendant) )
					return true;

			return false;
		}

		
	    public static bool IsVRDevicePresent()
	    {
		    #if UNITY_2020_0_OR_NEWER || UNITY_2020_1_OR_NEWER
		        var xrDisplaySubsystems = new List<XRDisplaySubsystem>();
		        SubsystemManager.GetInstances(xrDisplaySubsystems);
		        foreach (var xrDisplay in xrDisplaySubsystems)
		            if (xrDisplay.running)
		                return true;
		        
		        return false;
			#else
				return XRDevice.isPresent;
		    #endif
	    }

	    
		// returns the angle in the range -180 to 180
		public static float NormalizedDegAngle ( float degrees )
		{
			int factor = (int) (degrees/360);
			degrees -= factor * 360;
			if ( degrees > 180 )
				return degrees - 360;
		
			if ( degrees < -180 )
				return degrees + 360;
		
			return degrees;
		}
	


		public static void PlaceDummyObject ( string name, Vector3 pos, float scale = 0.1f, Quaternion? rotation = null )
		{
			GameObject dummyObject;
		
			if ( dummyObjects.ContainsKey(name) )
				dummyObject = dummyObjects[ name ];
			else
			{
				dummyObject = GameObject.CreatePrimitive( PrimitiveType.Cube );
				dummyObject.transform.localScale = scale * Vector3.one;
				dummyObject.GetComponent<Renderer>().material = Resources.Load ("DummyObjectMaterial") as Material;
				Object.Destroy (dummyObject.GetComponent<Collider>());
				dummyObject.name = name;
			
				dummyObjects[ name ] = dummyObject;
			}
		
			dummyObject.transform.position = pos;
			dummyObject.transform.rotation = rotation ?? Quaternion.identity;
		}

	}

}
