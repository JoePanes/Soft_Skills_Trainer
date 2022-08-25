	
using System;

namespace RealisticEyeMovements
{
	[Serializable]
	public class EyeAndHeadAnimatorForSerialization
	{
		#region fields
		
			public float mainWeight = 1;
			public float eyesWeight = 1;
			public float eyelidsWeight = 1;
			public HeadComponent.HeadControl headControl;
			public HeadComponent.HeadAnimationType headAnimationType;
			public EyeAndHeadAnimator.UpdateType updateType;
			public string headBonePath;
			public string neckBonePath;
			public string spineBonePath;
			public float headChangeToNewTargetSpeed = 1;
			public float headTrackTargetSpeed = 1;
			public float headWeight = 1;
			public float bodyWeight = 0.1f;
			public float neckHorizWeight = 0.5f;
			public float neckVertWeight = 0.5f;
			public bool resetHeadAtFrameStart = false;
			public bool useMicroSaccades = true;
			public bool useMacroSaccades = true;
			public bool useHeadJitter = true;
			public float headJitterFrequency = 0.2f;
			public float headJitterAmplitude = 1.0f;
			public bool kDrawSightlinesInEditor = false;
			public ControlDataForSerialization controlData;
			public float kMinNextBlinkTime = 3f;
			public float kMaxNextBlinkTime = 15f;
			public float blinkSpeed = 1;
			public bool eyelidsFollowEyesVertically = true;
			public float idleTargetHorizAngle = 12;
			public float crossEyeCorrection = 1;
			public float saccadeSpeed = 0.5f;
			public float macroSaccadesPerMinute = 10;
			public float microSaccadesPerMinuteLookingIdle = 45;
			public float microSaccadesPerMinuteLookingAtPOI = 80;
			public float limitHeadAngle = 0;
		
		#endregion
		
		
		public static EyeAndHeadAnimatorForSerialization CreateFromLegacy(EyeAndHeadAnimatorForExport export)
		{
			EyeAndHeadAnimatorForSerialization eyeAndHeadAnimatorForSerialization = new EyeAndHeadAnimatorForSerialization
			{
				headBonePath = export.headBonePath,
				headChangeToNewTargetSpeed = export.headSpeedModifier,
				headTrackTargetSpeed = export.headSpeedModifier,
				headWeight = export.headWeight,
				useMicroSaccades = export.useMicroSaccades,
				useMacroSaccades = export.useMacroSaccades,
				useHeadJitter = true,
				kDrawSightlinesInEditor = export.kDrawSightlinesInEditor,
				controlData = ControlDataForSerialization.CreateFromLegacy(export.controlData),
				kMinNextBlinkTime = export.kMinNextBlinkTime,
				kMaxNextBlinkTime = export.kMaxNextBlinkTime,
				blinkSpeed = 1,
				eyelidsFollowEyesVertically = export.eyelidsFollowEyesVertically,
				crossEyeCorrection = export.crossEyeCorrection,
				limitHeadAngle = export.limitHeadAngle
			};
			
			return eyeAndHeadAnimatorForSerialization;
		}
		
		
	}
}