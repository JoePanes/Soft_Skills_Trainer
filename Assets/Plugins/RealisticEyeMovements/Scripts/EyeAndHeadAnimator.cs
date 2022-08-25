// EyeAndHeadAnimator.cs
// Tore Knabe
// Copyright 2020 tore.knabe@gmail.com

// If you use FinalIK to move the head, add REM_USE_FINAL_IK to PlayerSettings/Other Settings/Script Define Symbols

#if !UNITY_WP8 && !UNITY_WP_8_1 && !UNITY_WSA
	#define SUPPORTS_SERIALIZATION
#endif

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;
#if SUPPORTS_SERIALIZATION
	using System.Runtime.Serialization.Formatters.Binary;
// ReSharper disable RedundantAssignment
#endif

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable ClassWithVirtualMembersNeverInherited.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable CommentTypo
// ReSharper disable UseNullPropagation
// ReSharper disable MemberCanBePrivate.Global

namespace RealisticEyeMovements {

	[HelpURL("https://docs.google.com/document/d/1b91EBehAyq_7GpTTxRHp1M5UbHfDQQ9CBektQgedXUg/edit?usp=sharing")]
	public class EyeAndHeadAnimator : MonoBehaviour
	{
	
		#region fields
		
			public float mainWeight = 1;
		
			const float kMaxHorizViewAngle = 100;
			const float kMaxVertViewAngle = 60;
			const float kAttentionChangeDeadTimeAfterSaccadeEnd = 0.3f;

			public event Action OnCannotGetTargetIntoView;
			public event Action OnTargetDestroyed;
			public event Action OnUpdate2Finished;

			#region head
				public float headChangeToNewTargetSpeed = 1;
				public float headTrackTargetSpeed = 1;
				[SerializeField] float headSpeedModifier; // legacy
				public float headWeight = 0.75f;
				public float bodyWeight = 0.1f;
				public float neckHorizWeight = 0.5f;
				public float neckVertWeight = 0.5f;
				public float headPitchAngle = 0;
				[FormerlySerializedAs("headTilt")]
				public float headRollAngle = 0;
				public float headYawAngle = 0;
				public float neckPitchAngle = 0;
				[FormerlySerializedAs("neckTilt")]
				public float neckRollAngle = 0;
				public float neckYawAngle = 0;
				public bool resetHeadAtFrameStart = false;
				[FormerlySerializedAs("headBoneNonMecanimXform")]
				public Transform headBoneNonMecanim;
				public Transform headTarget;
				public Transform neckBoneNonMecanim;
				public Transform spineBoneNonMecanim;

				public HeadComponent headComponent = new HeadComponent();
				

			#endregion

			public float eyesWeight = 1;
			public bool useMicroSaccades = true;
			public bool useMacroSaccades = true;
			public float saccadeSpeed = 0.5f;
			[SerializeField] float macroSaccadesPerMinute = 10;
			[FormerlySerializedAs("microSaccadesPerMinute")]
			[SerializeField] float microSaccadesPerMinuteLookingIdle = 45;
			[SerializeField] float microSaccadesPerMinuteLookingAtPOI = 80;
			public bool useHeadJitter = true;
			public float headJitterFrequency = 0.2f;
			public float headJitterAmplitude = 1.0f;

			public bool kDrawSightlinesInEditor;
			// Legacy field
			[SerializeField] bool areUpdatedControlledExternally = false;

			public UpdateType updateType = UpdateType.LateUpdate;

			public enum UpdateType
			{
				LateUpdate,
				FixedUpdate,
				External
			}

			public ControlData controlData = new ControlData();

			#region eye lids
				public float eyelidsWeight = 1;
				
				public float kMinNextBlinkTime = 3f;
				public float kMaxNextBlinkTime = 15f;
				
				public float blinkSpeed = 1;
				
				public bool eyelidsFollowEyesVertically = true;
				
				BlinkingComponent blinkingComponent;
	
				bool useUpperEyelids;
				bool useLowerEyelids;

			#endregion

			public float idleTargetHorizAngle = 10;
			
			public float crossEyeCorrection = 1.0f;

			public float limitHeadAngle;

			public float eyeDistance { get; private set; }
			public float eyeDistanceScale { get; private set; }
			
			public bool ResetBlendshapesAtFrameStartEvenIfDisabled { get; set; }

			public Ray LeftEyeRay { get; private set; }
			public Ray RightEyeRay { get; private set; }
			public Ray EyesCombinedRay { get; private set; }
			
			Transform leftEyeAnchor;
			Transform rightEyeAnchor;

			float leftMaxSpeedHoriz;
			float leftHorizDuration;
			float leftMaxSpeedVert;
			float leftVertDuration;
			float leftCurrentSpeedX;
			float leftCurrentSpeedY;

			float rightMaxSpeedHoriz;
			float rightHorizDuration;
			float rightMaxSpeedVert;
			float rightVertDuration;
			float rightCurrentSpeedX;
			float rightCurrentSpeedY;

			float startLeftEyeHorizDuration;
			float startLeftEyeVertDuration;
			float startLeftEyeMaxSpeedHoriz;
			float startLeftEyeMaxSpeedVert;

			float startRightEyeHorizDuration;
			float startRightEyeVertDuration;
			float startRightEyeMaxSpeedHoriz;
			float startRightEyeMaxSpeedVert;

			float timeOfEyeMovementStart;

			float headLatency;
			float eyeLatency;

			Animator animator;
			EarlyUpdateCallback earlyUpdateCallback;
			VeryLateUpdateCallback veryLateUpdateCallback;
			bool hasCheckedIdleLookTargetsThisFrame;
			bool placeNewIdleLookTargetsAtNextOpportunity;
			bool hasFixedUpdateRunThisFrame;

			#region Transforms for target
				Transform currentHeadTargetPOI;
				Transform currentEyeTargetPOI;
				Transform nextHeadTargetPOI;
				Transform nextEyeTargetPOI;
				Transform socialTriangleLeftEyeXform;
				Transform socialTriangleRightEyeXform;
				Transform transformToAvoidLookingAt;
				readonly Transform[] createdTargetXforms = new Transform[2];
				int createdTargetXformIndex;
			#endregion


			public Transform eyesRootXform { get; private set; }

			Quaternion eyeRoot_From_leftEyeAnchor_Q;
			Quaternion eyeRoot_From_rightEyeAnchor_Q;
			Quaternion leftEyeAnchor_From_eyeRoot_Q;
			Quaternion rightEyeAnchor_From_eyeRoot_Q;
			Vector3 currentLeftEyeLocalEuler;
			Vector3 currentRightEyeLocalEuler;
			Quaternion originalLeftEyeLocalQ;
			Quaternion originalRightEyeLocalQ;
			Quaternion lastLeftEyeLocalQ;
			Quaternion lastRightEyeLocalQ;

			Vector3 macroSaccadeTargetLocal;
			Vector3 microSaccadeTargetLocal;

			float timeOfEnteringClearingPhase;
			float timeOfCheckingWhetherIdleTargetOutOfView;
			float timeToMicroSaccade;
			float timeToMacroSaccade;

			bool isInitialized;
			Coroutine fixedUpdateCoroutine;
			
			enum LookTarget
			{
				None,
				StraightAhead,
				ClearingTargetPhase1,
				ClearingTargetPhase2,
				GeneralDirection,
				LookingAroundIdly,
				SpecificThing,
				Face
			}
			LookTarget lookTarget = LookTarget.None;

			enum FaceLookTarget
			{
				EyesCenter,
				LeftEye,
				RightEye,
				Mouth
			}
			FaceLookTarget faceLookTarget = FaceLookTarget.EyesCenter;

		#endregion


		public bool AreInSimilarLookDirection(Vector3 pointThatMaybeBetween, Vector3 targetPoint)
		{
			return Vector3.Angle(pointThatMaybeBetween - eyesRootXform.position, targetPoint - eyesRootXform.position) < 15;
		}
		
		
		void Awake()
		{
			Initialize();
			
			if ( isInitialized && lookTarget == LookTarget.None )
				LookAroundIdly();
		}


		public void Blink( bool isShortBlink =true)
		{
			blinkingComponent.Blink(isShortBlink);
		}
		
		
		public virtual bool CanGetIntoView(Vector3 point)
		{
			Vector3 targetLocalAngles = Quaternion.LookRotation( GetHeadParentXform().InverseTransformPoint( point ) ).eulerAngles;

			float x = Mathf.Abs(Utils.NormalizedDegAngle(targetLocalAngles.x));
			float y = Mathf.Abs(Utils.NormalizedDegAngle(targetLocalAngles.y));
			
			bool isLeft = y < 0;
			float clampedEyeHorizAngle = isLeft
														? controlData.ClampLeftHorizEyeAngle(targetLocalAngles.y) 
														: controlData.ClampRightHorizEyeAngle(targetLocalAngles.y);
			bool horizOk = y < HeadComponent.kMaxHorizHeadAngle + Mathf.Abs(clampedEyeHorizAngle) + 0.2f * kMaxHorizViewAngle;

			float clampedEyeVertAngle = controlData.ClampRightVertEyeAngle(targetLocalAngles.x);
			bool vertOk = x < HeadComponent.kMaxVertHeadAngle + Mathf.Abs(clampedEyeVertAngle) + 0.2f * kMaxVertViewAngle;
			
			return horizOk && vertOk;
		}


		public virtual bool CanChangePointOfAttention()
		{
			return Time.time > timeOfEyeMovementStart + Mathf.Max(startLeftEyeHorizDuration, startLeftEyeVertDuration, startRightEyeHorizDuration, startRightEyeVertDuration) + kAttentionChangeDeadTimeAfterSaccadeEnd;
		}


		public bool CanImportFromFile(string filename)
		{
			bool isJsonFile = filename.ToLower().EndsWith(".json");
			bool isBinFile = filename.ToLower().EndsWith(".dat");
			
			if ( false == isJsonFile && false == isBinFile )
				return false;
			
			#if !SUPPORTS_SERIALIZATION
				if ( isBinFile )
					return false;
			#endif
			
			EyeAndHeadAnimatorForSerialization import = null;
				
			if ( isJsonFile )
				import = JsonUtility.FromJson<EyeAndHeadAnimatorForSerialization>(File.ReadAllText(filename));
			#if SUPPORTS_SERIALIZATION
				else
				{
					EyeAndHeadAnimatorForExport eyeAndHeadAnimatorForExport;
					using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
						eyeAndHeadAnimatorForExport = (EyeAndHeadAnimatorForExport) new BinaryFormatter().Deserialize(stream);
					}
					import = EyeAndHeadAnimatorForSerialization.CreateFromLegacy(eyeAndHeadAnimatorForExport);
				}
			#endif
			
			string headBonePath = import.headBonePath;
			if ( Utils.CanGetTransformFromPath(transform, import.headBonePath) == false )
			{
				Debug.LogError(name + ": Cannot import, head path invalid: " + headBonePath, gameObject);
				return false;
			}
			
			string neckBonePath = import.neckBonePath;
			if ( Utils.CanGetTransformFromPath(transform, import.neckBonePath) == false )
			{
				Debug.LogError(name + ": Cannot import, neck path invalid: " + neckBonePath, gameObject);
				return false;
			}

			return controlData.CanImport(import.controlData, transform, GetHeadXformForImportExport());
		}


		// When looking around idly, make sure we keep looking at points that are in view based on where the head
		// looks before REM changes it, so as soon as the animation has oriented the head. So prevent REM from making
		// the character turn their head a lot when trying to keep looking at things that have moved out of view because
		// the character is walking around or dancing or so.
		protected virtual void CheckIdleLookTargets()
		{
			if ( lookTarget != LookTarget.LookingAroundIdly || hasCheckedIdleLookTargetsThisFrame )
				return;

			bool currentIdleLookTargetsAreOutOfView = false;
			Vector3 forward = headWeight <= 0	? GetHeadDirection()
																	: headComponent.GetForwardRelativeToSpineToHeadAxis();
			if ( false == placeNewIdleLookTargetsAtNextOpportunity && Time.time - timeOfCheckingWhetherIdleTargetOutOfView > 0.5f )
			{
				timeOfCheckingWhetherIdleTargetOutOfView = Time.time;
				
				Transform trans = currentEyeTargetPOI != null ? currentEyeTargetPOI : socialTriangleLeftEyeXform;

				if ( trans != null )
				{
					Vector3 eyeTargetGlobal = trans.TransformPoint(microSaccadeTargetLocal);

					Vector3 referencePosition = headWeight <= 0 ? GetOwnEyeCenter()
																						: GetHeadParentXform().position;
					Vector3 euler = Quaternion.FromToRotation(forward, eyeTargetGlobal - referencePosition).eulerAngles;
					const float kMaxHorizAngle = 45;
					const float kMaxVertAngle = 30;
					currentIdleLookTargetsAreOutOfView = Mathf.Abs(Utils.NormalizedDegAngle(euler.x)) > kMaxVertAngle || Mathf.Abs(Utils.NormalizedDegAngle(euler.y)) > kMaxHorizAngle;
				}
			}

			if ( placeNewIdleLookTargetsAtNextOpportunity || currentIdleLookTargetsAreOutOfView )
			{
				bool hasBoneEyelidControl = controlData.eyelidControl == ControlData.EyelidControl.Bones;
				
				Vector3 point;
				bool hasFoundValidPoint;
				int numTries = 0;
				do
				{
				float angleVert = Random.Range(-0.5f * (hasBoneEyelidControl ? 6f : 3f), hasBoneEyelidControl ? 6f : 4f);
				float angleHoriz = Random.Range(-idleTargetHorizAngle, idleTargetHorizAngle);

				Vector3 distortedForward = Quaternion.Euler(angleVert, angleHoriz, 0) * forward;
					point = GetOwnEyeCenter() + 2 * eyeDistanceScale * Random.Range(3.0f, 5.0f) *distortedForward;
					
					bool isLookingAtXformToAvoid = transformToAvoidLookingAt != null && AreInSimilarLookDirection(transformToAvoidLookingAt.position, point);
					hasFoundValidPoint = false == isLookingAtXformToAvoid;
					numTries++;
				}
				while (false == hasFoundValidPoint && numTries < 100);

				createdTargetXformIndex = (createdTargetXformIndex+1) % createdTargetXforms.Length;
				createdTargetXforms[createdTargetXformIndex].position = point;
				Transform poi = createdTargetXforms[createdTargetXformIndex];
				socialTriangleLeftEyeXform = socialTriangleRightEyeXform = null;

				headLatency = 0.075f;
				nextHeadTargetPOI = poi;
				StartEyeMovement(poi, false);
				placeNewIdleLookTargetsAtNextOpportunity = false;
				transformToAvoidLookingAt = null;
			}

			hasCheckedIdleLookTargetsThisFrame = true;
		}


		// If eye latency is greater than zero, the head starts turning towards new target and the eyes keep looking at the old target for a while.
		// If head latency is greater than zero, the eyes look at the new target first and the head turns later.
		void CheckLatencies()
		{
			if ( eyeLatency > 0 )
			{
				eyeLatency -= Time.deltaTime;
				if ( eyeLatency <= 0 )
				{
					currentEyeTargetPOI = nextEyeTargetPOI;
					StartEyeMovement(currentEyeTargetPOI);
				}
			}
			else if ( headLatency > 0 )
			{
				headLatency -= Time.deltaTime;
				if ( headLatency <= 0 )
					StartHeadMovement(nextHeadTargetPOI);
			}
		}


		void CheckMacroSaccades(float deltaTime)
		{
			if ( lookTarget == LookTarget.SpecificThing )
				return;

			if ( eyeLatency > 0 )
				return;

			timeToMacroSaccade -= deltaTime;
			if ( timeToMacroSaccade <= 0 )
			{
				if ( (lookTarget == LookTarget.GeneralDirection || lookTarget == LookTarget.LookingAroundIdly) && useMacroSaccades)
				{
							const float kMacroSaccadeAngle = 10;
							bool hasBoneEyelidControl = controlData.eyelidControl == ControlData.EyelidControl.Bones;
							float angleVert = Random.Range(-kMacroSaccadeAngle * (hasBoneEyelidControl ? 0.65f : 0.3f), kMacroSaccadeAngle * (hasBoneEyelidControl ? 0.65f : 0.4f));
							float angleHoriz = Random.Range(-kMacroSaccadeAngle,kMacroSaccadeAngle);
					SetMacroSaccadeTarget( eyesRootXform.TransformPoint(	Quaternion.Euler( angleVert, angleHoriz, 0)
																												* eyesRootXform.InverseTransformPoint( GetCurrentEyeTargetPos() )));

					ResetTimeToMacroSaccade();
				}
				else if ( lookTarget == LookTarget.Face )
				{
					if ( currentEyeTargetPOI == null )
					{
						//*** Social triangle: saccade between eyes and mouth (or chest, if actor isn't looking back)
						{
							switch( faceLookTarget )
							{
								case FaceLookTarget.LeftEye:
									faceLookTarget = Random.value < 0.75f ? FaceLookTarget.RightEye : FaceLookTarget.Mouth;
									break;
								case FaceLookTarget.RightEye:
									faceLookTarget = Random.value < 0.75f ? FaceLookTarget.LeftEye : FaceLookTarget.Mouth;
									break;
								case FaceLookTarget.Mouth:
								case FaceLookTarget.EyesCenter:
									faceLookTarget = Random.value < 0.5f ? FaceLookTarget.LeftEye : FaceLookTarget.RightEye;
									break;
							}
							SetMacroSaccadeTarget( GetLookTargetPosForSocialTriangle( faceLookTarget ) );
							ResetTimeToMacroSaccade();
						}
					}
				}																																				
			}
		}


		void CheckMicroSaccades(float deltaTime)
		{
			if ( false == useMicroSaccades )
				return;

			if ( eyeLatency > 0 )
				return;
			
			if ( currentEyeTargetPOI == null )
				return;

			if ( lookTarget == LookTarget.GeneralDirection || lookTarget == LookTarget.SpecificThing || lookTarget == LookTarget.Face || lookTarget == LookTarget.LookingAroundIdly )
			{
				timeToMicroSaccade -= deltaTime;
				if ( timeToMicroSaccade <= 0 )
				{
					float microSaccadeAngle = Random.Range(1.5f, 3f);
					bool hasBoneEyelidControl = controlData.eyelidControl == ControlData.EyelidControl.Bones;
					float angleVert = Random.Range(-microSaccadeAngle * (hasBoneEyelidControl ? 0.8f : 0.5f), microSaccadeAngle * (hasBoneEyelidControl ? 0.85f : 0.6f));
					float angleHoriz = Random.Range(-microSaccadeAngle,microSaccadeAngle);
					if ( lookTarget == LookTarget.Face )
					{
						angleVert *= 0.5f;
						angleHoriz *= 0.5f;
					}

					SetMicroSaccadeTarget ( eyesRootXform.TransformPoint(	Quaternion.Euler(angleVert, angleHoriz, 0)
																												* eyesRootXform.InverseTransformPoint( currentEyeTargetPOI.TransformPoint(macroSaccadeTargetLocal) )));
				}
			}
		}

		
		public void ClearLookTarget()
		{
			LookAtAreaAround( GetOwnEyeCenter() + transform.forward * (1000 * eyeDistance) );
			lookTarget = LookTarget.ClearingTargetPhase1;
			timeOfEnteringClearingPhase = Time.time;
		}


		public void ConvertLegacyIfNecessary()
		{
			if ( areUpdatedControlledExternally )
			{
				updateType = UpdateType.External;
				areUpdatedControlledExternally = false;
			}
			
			if ( headSpeedModifier > 0 )
			{
				headTrackTargetSpeed = headChangeToNewTargetSpeed = headSpeedModifier;
				headSpeedModifier = 0;
			}
		}
		

		void DrawSightlinesInEditor()
		{
			if ( controlData.eyeControl != ControlData.EyeControl.None )
			{
				Vector3 leftDirection = leftEyeAnchor.parent.rotation * leftEyeAnchor.localRotation * leftEyeAnchor_From_eyeRoot_Q * Vector3.forward;
				Vector3 rightDirection = rightEyeAnchor.parent.rotation * rightEyeAnchor.localRotation * rightEyeAnchor_From_eyeRoot_Q * Vector3.forward;
				Debug.DrawLine(leftEyeAnchor.position, leftEyeAnchor.position + leftDirection * (10 * eyeDistanceScale));
				Debug.DrawLine(rightEyeAnchor.position, rightEyeAnchor.position + rightDirection * (10 * eyeDistanceScale));
			}
		}


		public void ExportToFile(string filename)
		{
			Transform usedHeadXform = GetHeadXformForImportExport();
			
			EyeAndHeadAnimatorForSerialization serialization = new EyeAndHeadAnimatorForSerialization
			{
				mainWeight = mainWeight,
				eyesWeight = eyesWeight,
				eyelidsWeight = eyelidsWeight,
				updateType = updateType,
				headControl = headComponent.headControl,
				headAnimationType = headComponent.headAnimationType,
				headBonePath = Utils.GetPathForTransform(transform, headBoneNonMecanim),
				neckBonePath = Utils.GetPathForTransform(transform, neckBoneNonMecanim),
				spineBonePath = Utils.GetPathForTransform(transform, spineBoneNonMecanim),
				headChangeToNewTargetSpeed = headChangeToNewTargetSpeed,
				headTrackTargetSpeed = headTrackTargetSpeed,
				headWeight = headWeight,
				bodyWeight = bodyWeight,
				neckHorizWeight = neckHorizWeight,
				neckVertWeight = neckVertWeight,
				resetHeadAtFrameStart = resetHeadAtFrameStart,
				useMicroSaccades = useMicroSaccades,
				useMacroSaccades = useMacroSaccades,
				useHeadJitter = useHeadJitter,
				headJitterFrequency = headJitterFrequency,
				headJitterAmplitude = headJitterAmplitude,
				kDrawSightlinesInEditor = kDrawSightlinesInEditor,
				controlData = controlData.GetExport(transform, usedHeadXform),
				kMinNextBlinkTime = kMinNextBlinkTime,
				kMaxNextBlinkTime = kMaxNextBlinkTime,
				blinkSpeed = blinkSpeed,
				eyelidsFollowEyesVertically = eyelidsFollowEyesVertically,
				idleTargetHorizAngle = idleTargetHorizAngle,
				crossEyeCorrection = crossEyeCorrection,
				saccadeSpeed = saccadeSpeed,
				microSaccadesPerMinuteLookingIdle = microSaccadesPerMinuteLookingIdle,
				microSaccadesPerMinuteLookingAtPOI = microSaccadesPerMinuteLookingAtPOI,
				macroSaccadesPerMinute = macroSaccadesPerMinute,
				limitHeadAngle = limitHeadAngle,
			};

			File.WriteAllText(filename, JsonUtility.ToJson(serialization, true));
		}


		// We use WaitForFixedUpdate instead of FixedUpdate because we want to run after the
		// Animator (set to Animate Physics mode) has been updated.
		IEnumerator FixedUpdateRT()
		{
			if ( animator != null && animator.updateMode != AnimatorUpdateMode.AnimatePhysics )
				Debug.LogError(name + ": EyeAndHeadAnimator's update mode is set to FixedUpdateBothUpdates. The animator's update mode should be set to Animate Physics, but isn't.", gameObject);
			
			WaitForFixedUpdate waitForFixedUpdate = new WaitForFixedUpdate();
			
			while ( true )
			{
				if ( updateType != UpdateType.FixedUpdate )
					yield break;
				
				yield return waitForFixedUpdate;
				
				if ( headComponent.headControl != HeadComponent.HeadControl.AnimatorIK )
				{
					Update1(Time.fixedDeltaTime);
					
					if ( headComponent.headControl == HeadComponent.HeadControl.Transform )
						Update2(Time.fixedDeltaTime);
				}
				
				hasFixedUpdateRunThisFrame = true;
			}
		}

		
		Vector3 GetCurrentEyeTargetPos()
		{
			return currentEyeTargetPOI != null	?	currentEyeTargetPOI.position
																	:	0.5f * ( socialTriangleLeftEyeXform.position + socialTriangleRightEyeXform.position );
		}


		public Vector3 GetCurrentHeadTargetPos()
		{
			return currentHeadTargetPOI != null	?	currentHeadTargetPOI.position
												:	0.5f * ( socialTriangleLeftEyeXform.position + socialTriangleRightEyeXform.position );
		}

		
		public Quaternion GetHeadBoneOrientationForLookingAt(Vector3 headTargetGlobal)
		{
			return headComponent.GetHeadBoneOrientationForLookingAt(headTargetGlobal);
		}
		

		public Vector3 GetHeadDirection()
		{
			return headComponent.GetHeadDirection();
		}


		public Transform GetHeadParentXform()
		{
			return headComponent.headBaseXform;
		}
		
		
		Transform GetHeadXformForImportExport()
		{
			Transform usedHeadXform = null;
					Animator _animator = GetComponentInChildren<Animator>();
					if ( headComponent.headControl == HeadComponent.HeadControl.Transform )
						usedHeadXform = headBoneNonMecanim;
					if ( usedHeadXform == null && _animator != null )
						usedHeadXform = _animator.GetBoneTransform(HumanBodyBones.Head);
					
			return usedHeadXform;
		}
		
		
		Vector3 GetLeftEyeDirection()
		{
			if ( leftEyeAnchor == null )
				return eyesRootXform.forward;

			return leftEyeAnchor.parent.rotation * leftEyeAnchor.localRotation * leftEyeAnchor_From_eyeRoot_Q * Vector3.forward;
		}


		Vector3 GetLookTargetPosForSocialTriangle( FaceLookTarget playerFaceLookTarget )
		{
			if ( socialTriangleLeftEyeXform == null || socialTriangleRightEyeXform == null )
				return currentEyeTargetPOI.position;

			Vector3 faceTargetPos = Vector3.zero;

			Vector3 eyeCenter = 0.5f * (socialTriangleLeftEyeXform.position + socialTriangleRightEyeXform.position);
			float distanceBetweenTargetEyes = Vector3.Distance(socialTriangleLeftEyeXform.position, socialTriangleRightEyeXform.position);
			float distBetweenEyeCenters = Vector3.Distance(eyeCenter, GetOwnEyeCenter());
			
			// The closer you are to the char's face, make the triangle smaller,
			// so the triangle is visible when far away but looks ok when close
			const float kMinDistanceForTriangleScaling = 0.3f;
			const float kMaxDistanceForTriangleScaling = 1;
			float normalizedDistanceBetweenTargetEyes = distanceBetweenTargetEyes <= 0 ? kMinDistanceForTriangleScaling : distBetweenEyeCenters * 0.068f / distanceBetweenTargetEyes;
			float triangleSizeFactor = Mathf.Lerp(0.5f, 1,  Mathf.InverseLerp(kMinDistanceForTriangleScaling, kMaxDistanceForTriangleScaling, normalizedDistanceBetweenTargetEyes));
			switch( playerFaceLookTarget )
			{
				case FaceLookTarget.EyesCenter:
					faceTargetPos = GetCurrentEyeTargetPos();
					break;
				case FaceLookTarget.LeftEye:
					faceTargetPos = Vector3.Lerp(eyeCenter, socialTriangleLeftEyeXform.position, triangleSizeFactor);
					break;
				case FaceLookTarget.RightEye:
					faceTargetPos = Vector3.Lerp(eyeCenter, socialTriangleRightEyeXform.position, triangleSizeFactor);
					break;
				case FaceLookTarget.Mouth:
					Vector3 eyeUp = 0.5f * (socialTriangleLeftEyeXform.up + socialTriangleRightEyeXform.up);
					faceTargetPos = eyeCenter - eyeUp * (triangleSizeFactor * 0.9f * Vector3.Distance( socialTriangleLeftEyeXform.position, socialTriangleRightEyeXform.position ));
					break;
			}

			return faceTargetPos;
		}


		public Vector3 GetOwnEyeCenter()
		{
			return eyesRootXform.position;
		}


		public Transform GetOwnEyeCenterXform()
		{
			return eyesRootXform;
		}


		Vector3 GetOwnLookDirection()
		{
			return leftEyeAnchor != null && rightEyeAnchor != null	?  Quaternion.Slerp(	leftEyeAnchor.rotation * leftEyeAnchor_From_eyeRoot_Q,
					rightEyeAnchor.rotation * rightEyeAnchor_From_eyeRoot_Q, 0.5f) * Vector3.forward
																								:	eyesRootXform.forward;
		}


		Vector3 GetRightEyeDirection()
		{
			if ( rightEyeAnchor == null )
				return eyesRootXform.forward;

			return rightEyeAnchor.parent.rotation * rightEyeAnchor.localRotation * rightEyeAnchor_From_eyeRoot_Q * Vector3.forward;
		}


		public float GetStareAngleMeAtTarget( Vector3 target )
		{
			return Vector3.Angle(GetOwnLookDirection(), target - eyesRootXform.position);
		}


		public float GetStareAngleTargetAtMe( Transform targetXform )
		{
			return Vector3.Angle(targetXform.forward, GetOwnEyeCenter() - targetXform.position);
		}
		
		
		void Import(EyeAndHeadAnimatorForSerialization import)
		{
			Transform usedHeadXform = GetHeadXformForImportExport();
			
			mainWeight = import.mainWeight;
			eyesWeight = import.eyesWeight;
			eyelidsWeight = import.eyelidsWeight;
			updateType = import.updateType;
			headComponent.headControl = import.headControl;
			headComponent.headAnimationType = import.headAnimationType;
			headBoneNonMecanim = Utils.GetTransformFromPath(transform, import.headBonePath);
			neckBoneNonMecanim = Utils.GetTransformFromPath(transform, import.neckBonePath);
			spineBoneNonMecanim = Utils.GetTransformFromPath(transform, import.spineBonePath);
			headChangeToNewTargetSpeed = import.headChangeToNewTargetSpeed;
			headTrackTargetSpeed = import.headTrackTargetSpeed;
			headWeight = import.headWeight;
			bodyWeight = import.bodyWeight;
			neckHorizWeight = import.neckHorizWeight;
			neckVertWeight = import.neckVertWeight;
			resetHeadAtFrameStart = import.resetHeadAtFrameStart;
			useMicroSaccades = import.useMicroSaccades;
			useMacroSaccades = import.useMacroSaccades;
			useHeadJitter = import.useHeadJitter;
			headJitterFrequency = import.headJitterFrequency;
			headJitterAmplitude = import.headJitterAmplitude;
			kDrawSightlinesInEditor = import.kDrawSightlinesInEditor;
			kMinNextBlinkTime = import.kMinNextBlinkTime;
			kMaxNextBlinkTime = import.kMaxNextBlinkTime;
			blinkSpeed = import.blinkSpeed;
			eyelidsFollowEyesVertically = import.eyelidsFollowEyesVertically;
			idleTargetHorizAngle = import.idleTargetHorizAngle;
			crossEyeCorrection = import.crossEyeCorrection;
			saccadeSpeed = import.saccadeSpeed;
			microSaccadesPerMinuteLookingIdle = import.microSaccadesPerMinuteLookingIdle;
			microSaccadesPerMinuteLookingAtPOI = import.microSaccadesPerMinuteLookingAtPOI;
			macroSaccadesPerMinute = import.macroSaccadesPerMinute;
			limitHeadAngle = import.limitHeadAngle;
			
			ConvertLegacyIfNecessary();
			
			controlData.Import(import.controlData, transform, usedHeadXform);

			isInitialized = false;

			if ( controlData.NeedsSaveDefaultBlendshapeConfig() )
			{
				controlData.RestoreDefault();
				controlData.SaveDefault(this);
			}
			
			if ( Application.isPlaying )
				Initialize();
		}


		public void ImportFromFile(string filename)
		{
			if ( false == CanImportFromFile(filename) )
			{
				Debug.LogError(name + " cannot import from file", gameObject);
				return;
			}
			
			bool isJsonFile = filename.ToLower().EndsWith(".json");
			
			EyeAndHeadAnimatorForSerialization import = null;
				
			if ( isJsonFile )
				import = JsonUtility.FromJson<EyeAndHeadAnimatorForSerialization>(File.ReadAllText(filename));
			#if SUPPORTS_SERIALIZATION
				else
				{
					EyeAndHeadAnimatorForExport eyeAndHeadAnimatorForExport;
					using (FileStream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read)) {
						eyeAndHeadAnimatorForExport = (EyeAndHeadAnimatorForExport) new BinaryFormatter().Deserialize(stream);
					}
					import = EyeAndHeadAnimatorForSerialization.CreateFromLegacy(eyeAndHeadAnimatorForExport);
				}
			#endif
			
			Import(import);
		}

		
		public void ImportFromJson(string json)
		{
			Import(JsonUtility.FromJson<EyeAndHeadAnimatorForSerialization>(json));
		}
	
		
		public void Initialize()
		{
			if ( isInitialized )
				return;
			
			if ( controlData == null )
				return;

			eyeDistance = 0.064f;
			eyeDistanceScale = 1;
			animator = GetComponentInChildren<Animator>();
			
			if ( controlData.eyeControl == ControlData.EyeControl.MecanimEyeBones && animator != null && false == animator.isHuman )
			{
				Debug.LogError(name + ": EyeControl is set to Mecanim, but the animator is not for a humanoid", gameObject);
				
				return;
			}

			controlData.Initialize(transform);

			InitializeCreatedTargetXforms();

			headComponent.Initialize(this, animator);

			InitializeEyes();

			InitializeEyelids();

			isInitialized = true;
		}


		void InitializeCreatedTargetXforms()
		{
			if (createdTargetXforms[0] == null)
			{
				GameObject createdGO = new GameObject(name + "_createdEyeTarget_1") { hideFlags = HideFlags.HideAndDontSave };
				createdTargetXforms[0] = createdGO.transform;

				DontDestroyOnLoad(createdTargetXforms[0].gameObject);
				DestroyNotifier destroyNotifer = createdTargetXforms[0].gameObject.AddComponent<DestroyNotifier>();
				destroyNotifer.OnDestroyedEvent += OnCreatedXformDestroyed;
			}

			if (createdTargetXforms[1] == null)
			{
				GameObject createdGO = new GameObject(name + "_createdEyeTarget_2") { hideFlags = HideFlags.HideAndDontSave };
				createdTargetXforms[1] = createdGO.transform;

				DontDestroyOnLoad(createdTargetXforms[1].gameObject);
				DestroyNotifier destroyNotifer = createdTargetXforms[1].gameObject.AddComponent<DestroyNotifier>();
				destroyNotifer.OnDestroyedEvent += OnCreatedXformDestroyed;
			}
		}

		
		void InitializeEyelids()
		{
			if (controlData.eyelidControl == ControlData.EyelidControl.Bones)
			{
				if (controlData.upperLeftEyelidBones.Count > 0 && controlData.upperRightEyelidBones.Count > 0)
					useUpperEyelids = true;

				if (controlData.lowerLeftEyelidBones.Count > 0 && controlData.lowerRightEyelidBones.Count > 0)
					useLowerEyelids = true;
			}
			
			blinkingComponent = new BlinkingComponent(this);
		}


		void InitializeEyes()
		{
			if ( controlData.eyeControl == ControlData.EyeControl.MecanimEyeBones )
			{
				leftEyeAnchor = animator.GetBoneTransform(HumanBodyBones.LeftEye);
				rightEyeAnchor = animator.GetBoneTransform(HumanBodyBones.RightEye);
				if ( leftEyeAnchor == null )
					Debug.LogError(name + ": Left eye bone not found in Mecanim rig", gameObject);
				if ( rightEyeAnchor == null )
					Debug.LogError(name + ": Right eye bone not found in Mecanim rig", gameObject);
			}
			else if ( controlData.eyeControl == ControlData.EyeControl.SelectedObjects )
			{
				leftEyeAnchor = controlData.leftEye;
				rightEyeAnchor = controlData.rightEye;
			}

			if ( eyesRootXform == null )
			{
				GameObject go = new GameObject(name + "_eyesRoot") { hideFlags = HideFlags.HideAndDontSave };
				eyesRootXform = go.transform;
				eyesRootXform.rotation = transform.rotation;
			}
			
			if ( leftEyeAnchor != null && rightEyeAnchor != null )
			{
				if ( eyesRootXform.InverseTransformPoint(leftEyeAnchor.position).x > eyesRootXform.InverseTransformPoint(rightEyeAnchor.position).x )
					Debug.LogError(name + " RealisticEyeMovements error: the eye assigned as left eye is to the right of the other with respect to the character's forward direction. The assigment should be switched.", gameObject);
				eyeDistance = Vector3.Distance( leftEyeAnchor.position, rightEyeAnchor.position );
				eyeDistanceScale = eyeDistance/0.064f;
				controlData.RestoreDefault(false);
				Quaternion eyeRoot_From_World_Q = Quaternion.Inverse(eyesRootXform.rotation);
				eyeRoot_From_leftEyeAnchor_Q = eyeRoot_From_World_Q * leftEyeAnchor.rotation;
				eyeRoot_From_rightEyeAnchor_Q = eyeRoot_From_World_Q * rightEyeAnchor.rotation;
				leftEyeAnchor_From_eyeRoot_Q = Quaternion.Inverse(eyeRoot_From_leftEyeAnchor_Q);
				rightEyeAnchor_From_eyeRoot_Q = Quaternion.Inverse(eyeRoot_From_rightEyeAnchor_Q);

				originalLeftEyeLocalQ = leftEyeAnchor.localRotation;
				originalRightEyeLocalQ = rightEyeAnchor.localRotation;

				eyesRootXform.position = 0.5f * (leftEyeAnchor.position + rightEyeAnchor.position);
				Transform commonAncestorXform = Utils.GetCommonAncestor( leftEyeAnchor, rightEyeAnchor );
				eyesRootXform.parent =  commonAncestorXform != null ? commonAncestorXform : leftEyeAnchor.parent;
				
				Vector3 right = (rightEyeAnchor.position - leftEyeAnchor.position).normalized;
				Vector3 forward = Vector3.Cross(right, transform.up);
				eyesRootXform.rotation = Quaternion.LookRotation(forward, transform.up);
			}
			else if ( animator != null )
			{
				if ( headComponent.headXform != null )
				{
					eyesRootXform.position = headComponent.headXform.position;
					eyesRootXform.parent = headComponent.headXform;
				}
				else
				{
					eyesRootXform.position = transform.position;
					eyesRootXform.parent = transform;
				}
			}
			else
			{
				eyesRootXform.position = transform.position;
				eyesRootXform.parent = transform;
			}
			
			headComponent.SetEyeRootXform(eyesRootXform);
		}
		
		
		public virtual bool IsInView( Vector3 target )
		{
			if ( leftEyeAnchor == null || rightEyeAnchor == null )
			{
							Vector3 localAngles = Quaternion.LookRotation(eyesRootXform.InverseTransformDirection(target - GetOwnEyeCenter())).eulerAngles;
							float vertAngle = Utils.NormalizedDegAngle(localAngles.x);
							float horizAngle = Utils.NormalizedDegAngle(localAngles.y);
				bool seesTarget = Mathf.Abs(vertAngle) <= kMaxVertViewAngle && Mathf.Abs(horizAngle) <= kMaxHorizViewAngle;

				return seesTarget;
			}

			Vector3 localAnglesLeft = (eyeRoot_From_leftEyeAnchor_Q * Quaternion.Inverse(leftEyeAnchor.rotation) * Quaternion.LookRotation(target - leftEyeAnchor.position, leftEyeAnchor.up)).eulerAngles;
			float vertAngleLeft = Utils.NormalizedDegAngle(localAnglesLeft.x);
			float horizAngleLeft = Utils.NormalizedDegAngle(localAnglesLeft.y);
			bool leftEyeSeesTarget = Mathf.Abs(vertAngleLeft) <= kMaxVertViewAngle && Mathf.Abs(horizAngleLeft) <= kMaxHorizViewAngle;

			Vector3 localAnglesRight = (eyeRoot_From_rightEyeAnchor_Q * Quaternion.Inverse(rightEyeAnchor.rotation) * Quaternion.LookRotation(target - rightEyeAnchor.position, rightEyeAnchor.up)).eulerAngles;
			float vertAngleRight = Utils.NormalizedDegAngle(localAnglesRight.x);
			float horizAngleRight = Utils.NormalizedDegAngle(localAnglesRight.y);
			bool rightEyeSeesTarget = Mathf.Abs(vertAngleRight) <= kMaxVertViewAngle && Mathf.Abs(horizAngleRight) <= kMaxHorizViewAngle;

			return leftEyeSeesTarget || rightEyeSeesTarget;
		}


		public bool IsLookingAtFace()
		{
			return lookTarget == LookTarget.Face;
		}
	
	
		void LateUpdate()
		{
			if ( Time.timeScale <= 0 )
				return;
			
			if ( updateType == UpdateType.LateUpdate && headComponent.headControl != HeadComponent.HeadControl.AnimatorIK )
				Update1(Time.deltaTime);
		}


		public void LookAtFace( Transform eyeCenterXform, float headLatency=0.075f )
		{
			if ( false == MakeSureIsInitialized() )
			{
				Debug.LogError(name + " LookAtFace: RealisticEyeMovements component is not initialized yet.", gameObject);
				return;
			}

			lookTarget = LookTarget.Face;
			headComponent.SetHeadSpeed(HeadComponent.HeadSpeed.Fast);
			faceLookTarget = FaceLookTarget.EyesCenter;
			nextHeadTargetPOI = eyeCenterXform;
			this.headLatency = headLatency;
			socialTriangleLeftEyeXform = socialTriangleRightEyeXform = null;

			StartEyeMovement( eyeCenterXform );
		}


		public void LookAtFace(	Transform leftEyeXform,
											Transform rightEyeXform,
											Transform eyesCenterXform,
											float headLatency=0.075f )
		{
			if ( false == MakeSureIsInitialized() )
			{
				Debug.LogError(name + " LookAtFace: RealisticEyeMovements component is not initialized yet.", gameObject);
				return;
			}

			lookTarget = LookTarget.Face;
			headComponent.SetHeadSpeed(HeadComponent.HeadSpeed.Fast);
			faceLookTarget = FaceLookTarget.EyesCenter;
			this.headLatency = headLatency;
			socialTriangleLeftEyeXform = leftEyeXform;
			socialTriangleRightEyeXform = rightEyeXform;
			nextHeadTargetPOI = eyesCenterXform;

			StartEyeMovement( );
		}


		public void LookAtSpecificThing( Transform poi, float headLatency=0.075f )
		{
			if ( false == MakeSureIsInitialized() )
			{
				Debug.LogError(name + " LookAtSpecificThing: RealisticEyeMovements component is not initialized yet.", gameObject);
				return;
			}

			lookTarget = LookTarget.SpecificThing;
			headComponent.SetHeadSpeed(HeadComponent.HeadSpeed.Fast);
			this.headLatency = headLatency;
			nextHeadTargetPOI = poi;
			socialTriangleLeftEyeXform = socialTriangleRightEyeXform = null;

			StartEyeMovement( poi );
		}


		public void LookAtSpecificThing( Vector3 point, float headLatency=0.075f )
		{
			if ( false == MakeSureIsInitialized() )
			{
				Debug.LogError(name + " LookAtSpecificThing: RealisticEyeMovements component is not initialized yet.", gameObject);
				return;
			}

			createdTargetXformIndex = (createdTargetXformIndex+1) % createdTargetXforms.Length;
			createdTargetXforms[createdTargetXformIndex].position = point;
			LookAtSpecificThing( createdTargetXforms[createdTargetXformIndex], headLatency );
		}


		public void LookAroundIdly(Transform transformToAvoidLookingAt=null)
		{
			if ( false == MakeSureIsInitialized() )
			{
				Debug.LogWarning(name + " LookAroundIdly: RealisticEyeMovements component is not initialized yet.", gameObject);
				return;
			}

			this.transformToAvoidLookingAt = transformToAvoidLookingAt;
			lookTarget = LookTarget.LookingAroundIdly;
			headComponent.SetHeadSpeed(HeadComponent.HeadSpeed.Slow);
			eyeLatency = headLatency = 0;

			placeNewIdleLookTargetsAtNextOpportunity = true;
		}


		public void LookAtAreaAround( Transform poi )
		{
			if ( false == MakeSureIsInitialized() )
			{
				Debug.LogWarning(name + " LookAtAreaAround: RealisticEyeMovements component is not initialized yet.", gameObject);
				return;
			}

			lookTarget = LookTarget.GeneralDirection;
			headComponent.SetHeadSpeed(HeadComponent.HeadSpeed.Slow);
			eyeLatency = Random.Range(0.05f, 0.1f);
			
			nextEyeTargetPOI = poi;
			socialTriangleLeftEyeXform = socialTriangleRightEyeXform = null;

			StartHeadMovement( poi );
		}


		public void LookAtAreaAround( Vector3 point )
		{
			if ( false == MakeSureIsInitialized() )
			{
				Debug.LogWarning(name + " LookAtAreaAround: RealisticEyeMovements component is not initialized yet.", gameObject);
				return;
			}


			createdTargetXformIndex = (createdTargetXformIndex+1) % createdTargetXforms.Length;
			createdTargetXforms[createdTargetXformIndex].position = point;
			LookAtAreaAround( createdTargetXforms[createdTargetXformIndex] );
		}


    	bool MakeSureIsInitialized()
        {
	        if ( false == isInitialized )
		        Initialize();
	        
	        return isInitialized;
        }
        
        
		void OnAnimatorIK(int layerIndex)
		{
			if ( headComponent.headControl != HeadComponent.HeadControl.AnimatorIK )
				return;
		
			Update1();

			headComponent.OnAnimatorIK(animator);
		}
	
	
		void OnCreatedXformDestroyed( DestroyNotifier destroyNotifer )
		{
			Transform destroyedXform = destroyNotifer.GetComponent<Transform>();

			for (int i=0;  i<createdTargetXforms.Length; i++)
				if ( createdTargetXforms[i] == destroyedXform )
					createdTargetXforms[i] = null;
		}


		void OnDestroy()
		{
			foreach ( Transform createdXform in createdTargetXforms )
				if ( createdXform != null )
				{
					createdXform.GetComponent<DestroyNotifier>().OnDestroyedEvent -= OnCreatedXformDestroyed;
					Destroy( createdXform.gameObject );
				}
			
			if ( earlyUpdateCallback != null )
				Destroy(earlyUpdateCallback);
			if ( veryLateUpdateCallback != null )
				Destroy(veryLateUpdateCallback);
		}


		void OnDisable()
		{
			if ( fixedUpdateCoroutine != null )
			{
				StopCoroutine(fixedUpdateCoroutine);
				fixedUpdateCoroutine = null;
			}

			if ( earlyUpdateCallback != null && false == ResetBlendshapesAtFrameStartEvenIfDisabled )
				earlyUpdateCallback.onEarlyUpdate -= OnEarlyUpdate;
			if ( veryLateUpdateCallback != null )
				veryLateUpdateCallback.onVeryLateUpdate -= OnVeryLateUpdate;
		}

		
		void OnEarlyUpdate()
		{
			// Restore the default before all other scripts run so we can lerp with weights relative to what we find (which might have been changed by animation)
			// later in the frame.
			if ( updateType != UpdateType.FixedUpdate &&
			     (ResetBlendshapesAtFrameStartEvenIfDisabled || mainWeight > 0 && enabled ) )
				controlData.RestoreDefault();
		}
		
		
		void OnEnable()
		{
			if ( earlyUpdateCallback == null )
				earlyUpdateCallback = GetComponent<EarlyUpdateCallback>();
			if ( earlyUpdateCallback == null )
				earlyUpdateCallback = gameObject.AddComponent<EarlyUpdateCallback>();
			earlyUpdateCallback.onEarlyUpdate -= OnEarlyUpdate;
			earlyUpdateCallback.onEarlyUpdate += OnEarlyUpdate;

			if ( updateType != UpdateType.External )
			{
				if ( veryLateUpdateCallback == null )
					veryLateUpdateCallback = GetComponent<VeryLateUpdateCallback>();
				if ( veryLateUpdateCallback == null )
					veryLateUpdateCallback = gameObject.AddComponent<VeryLateUpdateCallback>();
				veryLateUpdateCallback.onVeryLateUpdate -= OnVeryLateUpdate;
				veryLateUpdateCallback.onVeryLateUpdate += OnVeryLateUpdate;
			}
			
			ConvertLegacyIfNecessary();
			controlData.ConvertLegacyIfNecessary();
			
			Initialize();
			
			headComponent.OnEnable();
			
			if ( updateType == UpdateType.FixedUpdate )
			{
				if ( fixedUpdateCoroutine != null )
					StopCoroutine(fixedUpdateCoroutine);
				fixedUpdateCoroutine = StartCoroutine(FixedUpdateRT());
			}
		}


		void OnValidate()
		{
			controlData.ValidateSetup();
		}
		
		
		void OnVeryLateUpdate()
		{
			if ( updateType == UpdateType.LateUpdate )
				Update2();
			else if ( updateType == UpdateType.FixedUpdate && hasFixedUpdateRunThisFrame )
			{
				if ( headComponent.headControl == HeadComponent.HeadControl.FinalIK ||
				     headComponent.headControl == HeadComponent.HeadControl.HeadTarget ||
				     headComponent.headControl == HeadComponent.HeadControl.AnimatorIK )
					Update2(Time.fixedDeltaTime);
			}
			
			hasFixedUpdateRunThisFrame = false;
		}
		
		
		public void ResetBlinking()
		{
			blinkingComponent.ResetBlinking();
		}
		
		
		void ResetTimeToMacroSaccade()
		{
			float r = Random.Range(0.6f, 1.4f);
			if ( lookTarget == LookTarget.Face )
				r *= faceLookTarget == FaceLookTarget.Mouth ? 0.1f : 0.3f;
			timeToMacroSaccade = macroSaccadesPerMinute <= 0 ? Mathf.Infinity : Mathf.Max(0.4f, 60/macroSaccadesPerMinute * r);
		}

		
		void ResetTimeToMicroSaccade()
		{
			float durationFactor = 1;
			if ( lookTarget == LookTarget.Face )
				durationFactor = faceLookTarget == FaceLookTarget.Mouth ? 0.7f : 1f;
		
			float microSaccadesPerMinute = lookTarget == LookTarget.Face || lookTarget == LookTarget.SpecificThing
														? microSaccadesPerMinuteLookingAtPOI
														: microSaccadesPerMinuteLookingIdle;

			timeToMicroSaccade = microSaccadesPerMinute <= 0 ? Mathf.Infinity :
				Mathf.Max(0.4f, durationFactor * 60/microSaccadesPerMinute * Random.Range(0.25f, 1.6f));
		}
		
		
		public void SetMacroSaccadesPerMinute(float macroSaccadesPerMinute)
		{
			this.macroSaccadesPerMinute = macroSaccadesPerMinute;
			
			ResetTimeToMacroSaccade();
		}
		
		
		void SetMacroSaccadeTarget( Vector3 targetGlobal, bool blinkIfEyesMoveEnough = true)
		{	
			macroSaccadeTargetLocal = (currentEyeTargetPOI != null ? currentEyeTargetPOI : socialTriangleLeftEyeXform).InverseTransformPoint( targetGlobal );

			SetMicroSaccadeTarget( targetGlobal, blinkIfEyesMoveEnough );
			timeToMicroSaccade += 0.75f;
		}


		public void SetMicroSaccadesPerMinuteLookingIdle(float microSaccadesPerMinuteLookingIdle)
		{
			this.microSaccadesPerMinuteLookingIdle = microSaccadesPerMinuteLookingIdle;
			
			ResetTimeToMicroSaccade();
		}
		
		
		public void SetMicroSaccadesPerMinuteLookingAtPOI(float microSaccadesPerMinuteLookingAtPOI)
		{
			this.microSaccadesPerMinuteLookingAtPOI = microSaccadesPerMinuteLookingAtPOI;
			
			ResetTimeToMicroSaccade();
		}
		
		
		void SetMicroSaccadeTarget( Vector3 targetGlobal, bool blinkIfEyesMoveEnough=true )
		{
			if ( controlData.eyeControl == ControlData.EyeControl.None || leftEyeAnchor == null || rightEyeAnchor == null )
				return;

			microSaccadeTargetLocal = (currentEyeTargetPOI != null ? currentEyeTargetPOI : socialTriangleLeftEyeXform).InverseTransformPoint( targetGlobal );

			Vector3 targetLeftEyeLocalAngles = Quaternion.LookRotation(eyesRootXform.InverseTransformDirection( targetGlobal - leftEyeAnchor.position)).eulerAngles;
				targetLeftEyeLocalAngles = new Vector3(controlData.ClampLeftVertEyeAngle(targetLeftEyeLocalAngles.x),
																		controlData.ClampLeftHorizEyeAngle(targetLeftEyeLocalAngles.y),
																		targetLeftEyeLocalAngles.z);

			float leftHorizDistance = Mathf.Abs(Mathf.DeltaAngle(currentLeftEyeLocalEuler.y, targetLeftEyeLocalAngles.y));

					// From "Realistic Avatar and Head Animation Using a Neurobiological Model of Visual Attention", Itti, Dhavale, Pighin
			leftMaxSpeedHoriz = saccadeSpeed * 473 * (1 - Mathf.Exp(-leftHorizDistance/7.8f));

					// From "Eyes Alive", Lee, Badler
					const float D0 = 0.025f;
					const float d = 0.00235f;
			leftHorizDuration = saccadeSpeed <= 0 ? Mathf.Infinity : (D0 + d * leftHorizDistance) / saccadeSpeed;

			float leftVertDistance = Mathf.Abs(Mathf.DeltaAngle(currentLeftEyeLocalEuler.x, targetLeftEyeLocalAngles.x));
			leftMaxSpeedVert = saccadeSpeed * 473 * (1 - Mathf.Exp(-leftVertDistance/7.8f));
			leftVertDuration = saccadeSpeed <= 0 ? Mathf.Infinity : (D0 + d * leftVertDistance) / saccadeSpeed;

			Vector3 targetRightEyeLocalAngles = Quaternion.LookRotation(eyesRootXform.InverseTransformDirection( targetGlobal - rightEyeAnchor.position)).eulerAngles;
				targetRightEyeLocalAngles = new Vector3(controlData.ClampRightVertEyeAngle(targetRightEyeLocalAngles.x),
																			controlData.ClampRightHorizEyeAngle(targetRightEyeLocalAngles.y),
																			targetRightEyeLocalAngles.z);

			float rightHorizDistance = Mathf.Abs(Mathf.DeltaAngle(currentRightEyeLocalEuler.y, targetRightEyeLocalAngles.y));
			rightMaxSpeedHoriz = saccadeSpeed * 473 * (1 - Mathf.Exp(-rightHorizDistance/7.8f));
			rightHorizDuration = saccadeSpeed <= 0 ? Mathf.Infinity : (D0 + d * rightHorizDistance) / saccadeSpeed;

			float rightVertDistance = Mathf.Abs(Mathf.DeltaAngle(currentRightEyeLocalEuler.x, targetRightEyeLocalAngles.x));
			rightMaxSpeedVert = saccadeSpeed * 473 * (1 - Mathf.Exp(-rightVertDistance/7.8f));
			rightVertDuration = saccadeSpeed <= 0 ? Mathf.Infinity : (D0 + d * rightVertDistance) / saccadeSpeed;

			leftMaxSpeedHoriz = rightMaxSpeedHoriz = Mathf.Max( leftMaxSpeedHoriz, rightMaxSpeedHoriz );
			leftMaxSpeedVert = rightMaxSpeedVert = Mathf.Max( leftMaxSpeedVert, rightMaxSpeedVert );
			leftHorizDuration = rightHorizDuration = Mathf.Max( leftHorizDuration, rightHorizDuration );
			leftVertDuration = rightVertDuration = Mathf.Max( leftVertDuration, rightVertDuration );

			ResetTimeToMicroSaccade();

			//*** Blink if eyes move enough
			{
				if (blinkIfEyesMoveEnough && blinkingComponent.timeSinceLastBlinkFinished > 0.75f)
					if ( useUpperEyelids || useLowerEyelids || controlData.eyelidControl == ControlData.EyelidControl.Blendshapes )
					{
						float distance = Mathf.Max(leftHorizDistance, Mathf.Max(rightHorizDistance, Mathf.Max(leftVertDistance, rightVertDistance)));
						const float kMinBlinkDistance = 25.0f;
						if ( distance >= kMinBlinkDistance )
							blinkingComponent.Blink( isShortBlink: false );
					}
			}

			//*** For letting the eyes keep tracking the target after they saccaded to it
			{
				startLeftEyeHorizDuration = leftHorizDuration;
				startLeftEyeVertDuration = leftVertDuration;
				startLeftEyeMaxSpeedHoriz = leftMaxSpeedHoriz;
				startLeftEyeMaxSpeedVert = leftMaxSpeedVert;

				startRightEyeHorizDuration = rightHorizDuration;
				startRightEyeVertDuration = rightVertDuration;
				startRightEyeMaxSpeedHoriz = rightMaxSpeedHoriz;
				startRightEyeMaxSpeedVert = rightMaxSpeedVert;

				timeOfEyeMovementStart = Time.time;
			}
		}

		
		void StartEyeMovement( Transform targetXform=null, bool blinkIfEyesMoveEnough = true)
		{
			eyeLatency = 0;
			currentEyeTargetPOI = targetXform;
			nextEyeTargetPOI = null;

			SetMacroSaccadeTarget ( GetCurrentEyeTargetPos(), blinkIfEyesMoveEnough );
			
			ResetTimeToMacroSaccade();

			if ( currentHeadTargetPOI == null )
				currentHeadTargetPOI = currentEyeTargetPOI;
		}


		void StartHeadMovement(Transform targetXform=null)
		{
			headLatency = 0;
			
			currentHeadTargetPOI = targetXform;
			nextHeadTargetPOI = null;

			if ( currentEyeTargetPOI == null && socialTriangleLeftEyeXform == null )
				currentEyeTargetPOI = currentHeadTargetPOI;
			
			headComponent.StartHeadMovement( );
		}
		
		
		void Update()
		{
			hasCheckedIdleLookTargetsThisFrame = false;

			if ( false == MakeSureIsInitialized() )
				return;
			
			if ( false == enabled )
				return;

			CheckLatencies();
			
			if ( fixedUpdateCoroutine != null && updateType != UpdateType.FixedUpdate )
			{
				StopCoroutine(fixedUpdateCoroutine);
				fixedUpdateCoroutine = null;
			}
			else if ( fixedUpdateCoroutine == null && updateType == UpdateType.FixedUpdate )
				fixedUpdateCoroutine = StartCoroutine(FixedUpdateRT());
			
			if ( headComponent.headControl != HeadComponent.HeadControl.None )
				headComponent.Update();
		}


		public void Update1()
		{
			Update1(Time.deltaTime);
		}
		
		
		// Update1 is supposed to be called after the animation has finished.
		// If using FinalIK, this is supposed to be called before the head is oriented, because it sets the head target.
		public void Update1(float deltaTime)
		{
			if ( false == isInitialized || false == enabled )
				return;

			if ( lookTarget == LookTarget.StraightAhead )
				return;
			if ( lookTarget == LookTarget.LookingAroundIdly )
				CheckIdleLookTargets();

			if ( currentHeadTargetPOI == null && socialTriangleLeftEyeXform == null )
			{
				if ( OnTargetDestroyed != null )
					OnTargetDestroyed();

				return;
			}

			if ( headComponent.headControl != HeadComponent.HeadControl.None )
			{
				float targetHeadIKWeight = lookTarget == LookTarget.StraightAhead || lookTarget == LookTarget.ClearingTargetPhase2 || lookTarget == LookTarget.ClearingTargetPhase1
							? 0 : headWeight;
				headComponent.LateUpdate(deltaTime, targetHeadIKWeight);
			}
			
			Transform trans = currentEyeTargetPOI != null ? currentEyeTargetPOI : socialTriangleLeftEyeXform;
			if (lookTarget != LookTarget.ClearingTargetPhase1 &&
			    lookTarget != LookTarget.ClearingTargetPhase2 &&
			    lookTarget != LookTarget.None &&
			    trans != null &&
			    OnCannotGetTargetIntoView != null &&
			    eyeLatency <= 0 &&
			    false == CanGetIntoView(trans.TransformPoint(macroSaccadeTargetLocal)) )
			{
				if (OnCannotGetTargetIntoView != null)
					OnCannotGetTargetIntoView();
			}
		}


		public void Update2()
		{
			Update2(Time.deltaTime);
		}
		
		
		// Update2 is supposed to be called after the head has been oriented.
		// If using FinalIK, this is supposed to be called after the head is oriented,
		// because it moves the eyes from the head orientation to their final look target
		public void Update2(float deltaTime)
		{
			if ( currentEyeTargetPOI == null && socialTriangleLeftEyeXform == null ) return;
			if ( deltaTime <= 0 ) return;
			if ( false == isInitialized || false == enabled ) return;
			if ( lookTarget == LookTarget.StraightAhead ) return;


			if ( headComponent.headControl != HeadComponent.HeadControl.None )
				headComponent.TiltHead();
			
			CheckMicroSaccades(deltaTime);
			CheckMacroSaccades(deltaTime);

			if ( controlData.eyeControl != ControlData.EyeControl.None )
				UpdateEyeMovement(deltaTime);
			blinkingComponent.UpdateBlinking(deltaTime);
			UpdateEyelids();

			if ( kDrawSightlinesInEditor )
				DrawSightlinesInEditor();
			
			if (leftEyeAnchor != null && rightEyeAnchor != null)
			{
				LeftEyeRay = new Ray( leftEyeAnchor.position, GetLeftEyeDirection());
				RightEyeRay = new Ray(rightEyeAnchor.position, GetRightEyeDirection());
				EyesCombinedRay = new Ray( eyesRootXform.position, GetOwnLookDirection());
			} 
			
			if ( OnUpdate2Finished != null )
				OnUpdate2Finished();
		}
		

		void UpdateEyelids()
		{
			if ( controlData.eyelidControl != ControlData.EyelidControl.None )
				controlData.UpdateEyelids( currentLeftEyeLocalEuler.x,
														currentRightEyeLocalEuler.x,
														currentLeftEyeLocalEuler.y,
														currentRightEyeLocalEuler.y,
														blinkingComponent.blink01,
														eyelidsFollowEyesVertically,
														mainWeight * eyesWeight * eyelidsWeight );
		}


		void UpdateEyeMovement(float deltaTime)
		{
			if ( lookTarget == LookTarget.ClearingTargetPhase2 )
			{
				if ( Time.time - timeOfEnteringClearingPhase >= 1 )
					lookTarget = LookTarget.StraightAhead;
				else
				{
					leftEyeAnchor.localRotation = lastLeftEyeLocalQ = Quaternion.Slerp(lastLeftEyeLocalQ, originalLeftEyeLocalQ, deltaTime);
					rightEyeAnchor.localRotation = lastRightEyeLocalQ = Quaternion.Slerp(lastRightEyeLocalQ, originalRightEyeLocalQ, deltaTime);
				}

				return;
			}

			if ( lookTarget == LookTarget.ClearingTargetPhase1 )
			{
				if ( Time.time - timeOfEnteringClearingPhase >= 2 )
				{
					lookTarget = LookTarget.ClearingTargetPhase2;
					timeOfEnteringClearingPhase = Time.time;
				}
			}
		
			bool isLookingAtFace = lookTarget == LookTarget.Face;
			bool shouldDoSocialTriangle =	isLookingAtFace &&
															faceLookTarget != FaceLookTarget.EyesCenter;
			Transform trans = currentEyeTargetPOI != null ? currentEyeTargetPOI : socialTriangleLeftEyeXform;

			if ( trans == null )
				return;

			Vector3 eyeTargetGlobal = shouldDoSocialTriangle	? GetLookTargetPosForSocialTriangle( faceLookTarget )
																						: trans.TransformPoint(microSaccadeTargetLocal);
			
			// Don't let look target go behind eyes
			Vector3 eyeTargetInLocalSpace = eyesRootXform.InverseTransformPoint(eyeTargetGlobal);
			eyeTargetGlobal = eyesRootXform.TransformPoint(new Vector3(	eyeTargetInLocalSpace.x,
																														eyeTargetInLocalSpace.y,
																														Mathf.Max(0.05f * eyeDistanceScale, eyeTargetInLocalSpace.z)));
			
			//*** Prevent cross-eyes
			{
				Vector3 ownEyeCenter = GetOwnEyeCenter();
				Vector3 eyeCenterToTarget = eyeTargetGlobal - ownEyeCenter;
				float distance = eyeCenterToTarget.magnitude / eyeDistanceScale;
				float corrDistMax = isLookingAtFace ? 2f : 0.6f;
				float corrDistMin = isLookingAtFace ? 1.5f : 0.2f;
						
				if ( distance < corrDistMax )
				{
					float modifiedDistance = corrDistMin + distance * (corrDistMax-corrDistMin)/corrDistMax;
					modifiedDistance = crossEyeCorrection * (modifiedDistance-distance) + distance;
					eyeTargetGlobal = ownEyeCenter + eyeDistanceScale * modifiedDistance * (eyeCenterToTarget/distance);
				}
			}

			//*** After the eyes saccaded to the new POI, adjust eye duration and speed so they keep tracking the target quickly enough.
			{
				const float kEyeDurationForTracking = 0.005f;
				const float kEyeMaxSpeedForTracking = 600;

				float timeSinceLeftEyeHorizInitiatedMovementStop = Time.time-(timeOfEyeMovementStart + 1.5f * startLeftEyeHorizDuration);
				if ( timeSinceLeftEyeHorizInitiatedMovementStop > 0 )
				{
					leftHorizDuration = kEyeDurationForTracking + startLeftEyeHorizDuration/(1 + timeSinceLeftEyeHorizInitiatedMovementStop);
					leftMaxSpeedHoriz = kEyeMaxSpeedForTracking - startLeftEyeMaxSpeedHoriz/(1 + timeSinceLeftEyeHorizInitiatedMovementStop);
				}

				float timeSinceLeftEyeVertInitiatedMovementStop = Time.time-(timeOfEyeMovementStart + 1.5f * startLeftEyeVertDuration);
				if ( timeSinceLeftEyeVertInitiatedMovementStop > 0 )
				{
					leftVertDuration = kEyeDurationForTracking + startLeftEyeVertDuration/(1 + timeSinceLeftEyeVertInitiatedMovementStop);
					leftMaxSpeedVert = kEyeMaxSpeedForTracking - startLeftEyeMaxSpeedVert/(1 + timeSinceLeftEyeVertInitiatedMovementStop);
				}

				float timeSinceRightEyeHorizInitiatedMovementStop = Time.time-(timeOfEyeMovementStart + 1.5f * startRightEyeHorizDuration);
				if ( timeSinceRightEyeHorizInitiatedMovementStop > 0 )
				{
					rightHorizDuration = kEyeDurationForTracking + startRightEyeHorizDuration/(1 + timeSinceRightEyeHorizInitiatedMovementStop);
					rightMaxSpeedHoriz = kEyeMaxSpeedForTracking - startRightEyeMaxSpeedHoriz/(1 + timeSinceRightEyeHorizInitiatedMovementStop);
				}

				float timeSinceRightEyeVertInitiatedMovementStop = Time.time-(timeOfEyeMovementStart + 1.5f * startRightEyeVertDuration);
				if ( timeSinceRightEyeVertInitiatedMovementStop > 0 )
				{
					rightVertDuration = kEyeDurationForTracking + startRightEyeVertDuration/(1 + timeSinceRightEyeVertInitiatedMovementStop);
					rightMaxSpeedVert = kEyeMaxSpeedForTracking - startRightEyeMaxSpeedVert/(1 + timeSinceRightEyeVertInitiatedMovementStop);
				}
			}


			Vector3 desiredLeftEyeTargetAngles = Quaternion.LookRotation(eyesRootXform.InverseTransformDirection( eyeTargetGlobal - leftEyeAnchor.position )).eulerAngles;
			Vector3 leftEyeTargetAngles = new Vector3(controlData.ClampLeftVertEyeAngle(desiredLeftEyeTargetAngles.x),
																			controlData.ClampLeftHorizEyeAngle(desiredLeftEyeTargetAngles.y),
																			0);
			float _headMaxSpeedHoriz = 4*headComponent.maxHeadHorizSpeedSinceSaccadeStart * Mathf.Sign(headComponent.actualVelocity.y);
			float _headMaxSpeedVert = 4*headComponent.maxHeadVertSpeedSinceSaccadeStart * Mathf.Sign(headComponent.actualVelocity.x);
			
			currentLeftEyeLocalEuler = new Vector3(	controlData.ClampLeftVertEyeAngle(Mathf.SmoothDampAngle(	currentLeftEyeLocalEuler.x,
																																			leftEyeTargetAngles.x,
																																			ref leftCurrentSpeedX,
																																			leftVertDuration * 0.5f,
																																			Mathf.Max(_headMaxSpeedVert, leftMaxSpeedVert),
																																			deltaTime)),
																		controlData.ClampLeftHorizEyeAngle(Mathf.SmoothDampAngle(	currentLeftEyeLocalEuler.y,
																																					leftEyeTargetAngles.y,
																																					ref leftCurrentSpeedY,
																																					leftHorizDuration * 0.5f,
																																					Mathf.Max(_headMaxSpeedHoriz, leftMaxSpeedHoriz),
																																					deltaTime)),
																		leftEyeTargetAngles.z);
		
			// For the left eye we make the rotation variables a bit more explicit to make it clearer what's going on: currentLeftEyeLocalEuler is the rotation in eyeRoot space
			Quaternion world_From_eyeRoot_Q = eyesRootXform.rotation;
			Quaternion leftEyeRotation_OperationInEyeRootSpace = Quaternion.Euler( currentLeftEyeLocalEuler );
			leftEyeAnchor.rotation = Quaternion.Slerp(	leftEyeAnchor.rotation,
																			world_From_eyeRoot_Q * leftEyeRotation_OperationInEyeRootSpace * eyeRoot_From_leftEyeAnchor_Q,
																			mainWeight * eyesWeight);

			Vector3 desiredRightEyeTargetAngles = Quaternion.LookRotation(eyesRootXform.InverseTransformDirection( eyeTargetGlobal - rightEyeAnchor.position)).eulerAngles;
			Vector3 rightEyeTargetAngles = new Vector3(	controlData.ClampRightVertEyeAngle(desiredRightEyeTargetAngles.x),
																					controlData.ClampRightHorizEyeAngle(desiredRightEyeTargetAngles.y),
																					0);
			currentRightEyeLocalEuler= new Vector3( controlData.ClampRightVertEyeAngle(Mathf.SmoothDampAngle(	currentRightEyeLocalEuler.x,
																																			rightEyeTargetAngles.x,
																																			ref rightCurrentSpeedX,
																																			rightVertDuration * 0.5f,
																																			Mathf.Max(_headMaxSpeedVert, rightMaxSpeedVert),
																																			deltaTime)),
																		controlData.ClampRightHorizEyeAngle(Mathf.SmoothDampAngle(currentRightEyeLocalEuler.y,
																																					rightEyeTargetAngles.y,
																																					ref rightCurrentSpeedY,
																																					rightHorizDuration * 0.5f,
																																					Mathf.Max(_headMaxSpeedHoriz, rightMaxSpeedHoriz),
																																					deltaTime)),
																		rightEyeTargetAngles.z);

			rightEyeAnchor.rotation = Quaternion.Slerp(rightEyeAnchor.rotation, 
																			eyesRootXform.rotation * Quaternion.Euler( currentRightEyeLocalEuler ) * eyeRoot_From_rightEyeAnchor_Q,
																			mainWeight * eyesWeight);

			lastLeftEyeLocalQ = leftEyeAnchor.localRotation;
			lastRightEyeLocalQ = rightEyeAnchor.localRotation;
		}

	}

}