using System;
using UnityEngine;

namespace RealisticEyeMovements
{
	public partial class ControlData
	{
		[Serializable]
		public class EyelidPositionBlendshapeForExport
		{
			public string skinnedMeshRendererPath;
			public float defaultWeight;
			public float positionWeight;
			public int index;
			public string name;
		}


		[Serializable]
		public class EyelidPositionBlendshape
		{
			public SkinnedMeshRenderer skinnedMeshRenderer;
			public float defaultWeight;
			public float positionWeight;
			public int index;
			public string name;
			[NonSerialized]
			public bool isUsedInLookingUp;
			[NonSerialized]
			public bool isUsedInLookingDown;
			[NonSerialized]
			public bool deactivatedBecauseOfUseInOtherConfig;

			public static bool CanImport(EyelidPositionBlendshapeForExport import, Transform startXform, out string errorMessage)
			{
				errorMessage = null;
				
				if ( string.IsNullOrEmpty(import.skinnedMeshRendererPath) )
				{
					errorMessage = "Invalid path to skinned mesh renderer in import: '" + import.skinnedMeshRendererPath + "'"; 
					return false;
				}

				Transform t = Utils.GetTransformFromPath(startXform, import.skinnedMeshRendererPath);

				if ( t == null )
				{
					errorMessage = "Cannot find skinned mesh renderer '" + import.skinnedMeshRendererPath + "'";
					return false;
				}

				SkinnedMeshRenderer meshRenderer = t.GetComponent<SkinnedMeshRenderer>();

				if ( meshRenderer == null )
				{
					errorMessage = "Cannot find skinned mesh renderer '" + import.skinnedMeshRendererPath + "'";
					return false;
				}

				if ( false == string.IsNullOrEmpty(import.name) )
				{
					bool containsName = false;
					for ( int i=0;  i<meshRenderer.sharedMesh.blendShapeCount;  i++ )
						if ( meshRenderer.sharedMesh.GetBlendShapeName(i).Equals( import.name ) )
						{
							containsName = true;
							break;
						}

					if ( false == containsName )
					{
						errorMessage = "Blendshape not found on skinned mesh renderer '" + import.skinnedMeshRendererPath + "': '" + import.name +"'";
						return false;
					}
				}									

				return true;
			}


			bool FindIndexOfBlendshape(string name, out int index)
			{
				index = -1;
				
				for ( int i=0;  i<skinnedMeshRenderer.sharedMesh.blendShapeCount;  i++ )
				{
					string fullName = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(i);
					if ( fullName.Equals( name ) )
					{
						index = i;
						return true;
					}
				}
				
				return false;
			}
			
			
			public EyelidPositionBlendshapeForExport GetExport(Transform startXform)
			{
				EyelidPositionBlendshapeForExport export = new EyelidPositionBlendshapeForExport
				{
					skinnedMeshRendererPath = skinnedMeshRenderer != null ? Utils.GetPathForTransform(startXform, skinnedMeshRenderer.transform) : null,
					defaultWeight = defaultWeight,
					positionWeight = positionWeight,
					index = index,
					name = name
				};

				return export;
			}


			public void Import(EyelidPositionBlendshapeForExport export, Transform startXform)
			{
				skinnedMeshRenderer = export.skinnedMeshRendererPath != null ? Utils.GetTransformFromPath(startXform, export.skinnedMeshRendererPath).GetComponent<SkinnedMeshRenderer>() : null;
				defaultWeight = export.defaultWeight;
				positionWeight = export.positionWeight;
				index = export.index;
				name = export.name;

				//*** If we imported a name for the blendshape, find the correct index, because during runtime we use the index to manipulate blendshapes
				{
					if ( false == string.IsNullOrEmpty(name) && skinnedMeshRenderer != null)
					{
						int foundIndex;
						if ( FindIndexOfBlendshape(name, out foundIndex) )
						    index = foundIndex;
					}
				}
				
				if ( string.IsNullOrEmpty(name) && skinnedMeshRenderer != null )
					name = skinnedMeshRenderer.sharedMesh.GetBlendShapeName(index);
			}
			
			
			public bool IsUsedInBlendshapeConfig(EyelidPositionBlendshape[] otherBlendshapesConfig)
			{
				foreach (EyelidPositionBlendshape otherBlendshape in otherBlendshapesConfig)
					if (skinnedMeshRenderer == otherBlendshape.skinnedMeshRenderer )
					{
						bool haveNames = !string.IsNullOrEmpty(name) &&
													!string.IsNullOrEmpty(otherBlendshape.name);
						bool isSameName = haveNames && name == otherBlendshape.name;
						bool isSameIndex = index == otherBlendshape.index;
						if ( haveNames && isSameName || false == haveNames && isSameIndex )
							return true;
					}

				return false;
			}

		}
		
	}

}