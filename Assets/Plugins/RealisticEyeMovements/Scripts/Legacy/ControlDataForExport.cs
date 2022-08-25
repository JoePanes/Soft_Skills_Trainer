using System;

namespace RealisticEyeMovements
{
	
	public partial class ControlData
	{
		[Serializable]
		public class BlendshapesConfigForExport
		{
			public string skinnedMeshRendererPath;
			public int blendShapeCount;
			public string[] blendshapeNames;
			public float[] blendshapeWeights;
		}
					
		[Serializable]
		public class ControlDataForExport
		{
			public EyeControl eyeControl;
			public EyelidBoneMode eyelidBoneMode;

			public string leftEyePath;
			public string rightEyePath;

			public float maxEyeUpBoneAngle;
			public float maxEyeDownBoneAngle;

			public float maxEyeUpEyeballAngle;
			public float maxEyeDownEyeballAngle;

			public bool isEyeBallDefaultSet;
			public bool isEyeBoneDefaultSet;
			public bool isEyeBallLookUpSet;
			public bool isEyeBoneLookUpSet;
			public bool isEyeBallLookDownSet;
			public bool isEyeBoneLookDownSet;

			public EyeRotationLimiter.EyeRotationLimiterForExport leftBoneEyeRotationLimiter;
			public EyeRotationLimiter.EyeRotationLimiterForExport rightBoneEyeRotationLimiter;
			public EyeRotationLimiter.EyeRotationLimiterForExport leftEyeballEyeRotationLimiter;
			public EyeRotationLimiter.EyeRotationLimiterForExport rightEyeballEyeRotationLimiter;

			public EyelidControl eyelidControl;
			public bool eyelidsFollowEyesVertically;

			public string upperEyeLidLeftPath;
			public string upperEyeLidRightPath;
			public string lowerEyeLidLeftPath;
			public string lowerEyeLidRightPath;

			public bool isEyelidBonesDefaultSet;
			public bool isEyelidBonesClosedSet;
			public bool isEyelidBonesLookUpSet;
			public bool isEyelidBonesLookDownSet;

			public EyelidRotationLimiter.EyelidRotationLimiterForExport upperLeftLimiter;
			public EyelidRotationLimiter.EyelidRotationLimiterForExport upperRightLimiter;
			public EyelidRotationLimiter.EyelidRotationLimiterForExport lowerLeftLimiter;
			public EyelidRotationLimiter.EyelidRotationLimiterForExport lowerRightLimiter;

			public float eyeWidenOrSquint;

			public EyelidPositionBlendshapeForExport[] blendshapesForBlinking;
			public EyelidPositionBlendshapeForExport[] blendshapesForLookingUp;
			public EyelidPositionBlendshapeForExport[] blendshapesForLookingDown;
			public BlendshapesConfigForExport[] blendshapesConfigs;

			public bool isEyelidBlendshapeDefaultSet;
			public bool isEyelidBlendshapeClosedSet;
			public bool isEyelidBlendshapeLookUpSet;
			public bool isEyelidBlendshapeLookDownSet;
		}
	}
		
}