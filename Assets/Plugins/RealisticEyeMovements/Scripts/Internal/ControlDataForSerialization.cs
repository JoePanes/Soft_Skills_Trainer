using System;
using System.Collections.Generic;

namespace RealisticEyeMovements
{
	[Serializable]
	public class ControlDataForSerialization
	{
		#region fields
			public float eyeWidenOrSquint;

			public ControlData.EyeControl eyeControl;
			public ControlData.EyelidBoneMode eyelidBoneMode;

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
			public bool isEyeBallLookLeftSet;
			public bool isEyeBoneLookLeftSet;
			public bool isEyeBallLookRightSet;
			public bool isEyeBoneLookRightSet;

			public EyeRotationLimiter.EyeRotationLimiterForExport leftBoneEyeRotationLimiter;
			public EyeRotationLimiter.EyeRotationLimiterForExport rightBoneEyeRotationLimiter;
			public EyeRotationLimiter.EyeRotationLimiterForExport leftEyeballEyeRotationLimiter;
			public EyeRotationLimiter.EyeRotationLimiterForExport rightEyeballEyeRotationLimiter;

			public ControlData.EyelidControl eyelidControl;

			public List<string> upperLeftEyelidBonePaths = new List<string>();
			public List<string> upperRightEyelidBonePaths = new List<string>();
			public List<string> lowerLeftEyelidBonePaths = new List<string>();
			public List<string> lowerRightEyelidBonePaths = new List<string>();

			public bool isEyelidBonesDefaultSet;
			public bool isEyelidBonesClosedSet;
			public bool isEyelidBonesLookUpSet;
			public bool isEyelidBonesLookDownSet;

			public List<EyelidRotationLimiter.EyelidRotationLimiterForExport> upperLeftLimiters =
				new List<EyelidRotationLimiter.EyelidRotationLimiterForExport>();

			public List<EyelidRotationLimiter.EyelidRotationLimiterForExport> upperRightLimiters =
				new List<EyelidRotationLimiter.EyelidRotationLimiterForExport>();

			public List<EyelidRotationLimiter.EyelidRotationLimiterForExport> lowerLeftLimiters =
				new List<EyelidRotationLimiter.EyelidRotationLimiterForExport>();

			public List<EyelidRotationLimiter.EyelidRotationLimiterForExport> lowerRightLimiters =
				new List<EyelidRotationLimiter.EyelidRotationLimiterForExport>();

			public ControlData.EyelidPositionBlendshapeForExport[] blendshapesForBlinking;
			public ControlData.EyelidPositionBlendshapeForExport[] blendshapesForLookingUp;
			public ControlData.EyelidPositionBlendshapeForExport[] blendshapesForLookingDown;
			public ControlData.EyelidPositionBlendshapeForExport[] blendshapesForLookingLeft;
			public ControlData.EyelidPositionBlendshapeForExport[] blendshapesForLookingRight;

			public bool isEyelidBlendshapeDefaultSet;
			public bool isEyelidBlendshapeClosedSet;
			public bool isEyelidBlendshapeLookUpSet;
			public bool isEyelidBlendshapeLookDownSet;
			public bool isEyelidBlendshapeLookLeftSet;
			public bool isEyelidBlendshapeLookRightSet;
			
			public bool arePathsRelativeToHead;
		#endregion
		
		
		public static ControlDataForSerialization CreateFromLegacy(ControlData.ControlDataForExport export)
		{
			ControlDataForSerialization controlDataForSerialization = new ControlDataForSerialization
			{
				eyeControl = export.eyeControl,
				eyelidBoneMode = export.eyelidBoneMode,
				leftEyePath = export.leftEyePath,
				rightEyePath = export.rightEyePath,
				maxEyeUpBoneAngle = export.maxEyeUpBoneAngle,
				maxEyeDownBoneAngle = export.maxEyeDownBoneAngle,
				maxEyeUpEyeballAngle = export.maxEyeUpEyeballAngle,
				maxEyeDownEyeballAngle = export.maxEyeDownEyeballAngle,
				isEyeBallDefaultSet = export.isEyeBallDefaultSet,
				isEyeBoneDefaultSet = export.isEyeBoneDefaultSet,
				isEyeBallLookUpSet = export.isEyeBallLookUpSet,
				isEyeBoneLookUpSet = export.isEyeBoneLookUpSet,
				isEyeBallLookDownSet = export.isEyeBallLookDownSet,
				isEyeBoneLookDownSet = export.isEyeBoneLookDownSet,
				leftBoneEyeRotationLimiter = export.leftBoneEyeRotationLimiter,
				rightBoneEyeRotationLimiter = export.rightBoneEyeRotationLimiter,
				leftEyeballEyeRotationLimiter = export.leftEyeballEyeRotationLimiter,
				rightEyeballEyeRotationLimiter = export.rightEyeballEyeRotationLimiter,
				eyelidControl = export.eyelidControl,
				isEyelidBonesDefaultSet = export.isEyelidBonesDefaultSet,
				isEyelidBonesClosedSet = export.isEyelidBonesClosedSet,
				isEyelidBonesLookUpSet = export.isEyelidBonesLookUpSet,
				isEyelidBonesLookDownSet = export.isEyelidBonesLookDownSet,
				eyeWidenOrSquint = export.eyeWidenOrSquint,
				blendshapesForBlinking = export.blendshapesForBlinking,
				blendshapesForLookingUp = export.blendshapesForLookingUp,
				blendshapesForLookingDown = export.blendshapesForLookingDown,
				isEyelidBlendshapeDefaultSet = export.isEyelidBlendshapeDefaultSet,
				isEyelidBlendshapeClosedSet = export.isEyelidBlendshapeClosedSet,
				isEyelidBlendshapeLookUpSet = export.isEyelidBlendshapeLookUpSet,
				isEyelidBlendshapeLookDownSet = export.isEyelidBlendshapeLookDownSet
			};
			
			if ( false == string.IsNullOrEmpty(export.upperEyeLidLeftPath) )
				controlDataForSerialization.upperLeftEyelidBonePaths.Add(export.upperEyeLidLeftPath);
			if ( false == string.IsNullOrEmpty(export.upperEyeLidRightPath) )
				controlDataForSerialization.upperRightEyelidBonePaths.Add(export.upperEyeLidRightPath);
			if ( false == string.IsNullOrEmpty(export.lowerEyeLidLeftPath) )
				controlDataForSerialization.lowerLeftEyelidBonePaths.Add(export.lowerEyeLidLeftPath);
			if ( false == string.IsNullOrEmpty(export.lowerEyeLidRightPath) )
				controlDataForSerialization.lowerRightEyelidBonePaths.Add(export.lowerEyeLidRightPath);
			
			if ( export.upperLeftLimiter != null )
				controlDataForSerialization.upperLeftLimiters.Add(export.upperLeftLimiter);
			if ( export.upperRightLimiter != null )
				controlDataForSerialization.upperRightLimiters.Add(export.upperRightLimiter);
			if ( export.lowerLeftLimiter != null )
				controlDataForSerialization.lowerLeftLimiters.Add(export.lowerLeftLimiter);
			if ( export.lowerRightLimiter != null )
				controlDataForSerialization.lowerRightLimiters.Add(export.lowerRightLimiter);
			
			return controlDataForSerialization;
		}
	}
}