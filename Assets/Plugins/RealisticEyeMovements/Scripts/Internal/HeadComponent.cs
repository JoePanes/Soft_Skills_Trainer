using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RealisticEyeMovements
{
	[Serializable]
	public class HeadComponent
	{
		#region fields

			public Vector3 actualVelocity { get; private set; }
			public float maxHeadHorizSpeedSinceSaccadeStart { get; private set; }
			public float maxHeadVertSpeedSinceSaccadeStart { get; private set; }
			public Transform headXform { get; private set; }
			public Transform headBaseXform { get; private set; } // The headBaseXform is the basis for computing local head angles for movement.

		
			EyeAndHeadAnimator eyeAndHeadAnimator;

			public const float kMaxHorizHeadAngle = 65;
			public const float kMaxVertHeadAngle = 65;
			
			const float hill_a1 = 1.75f;
			const float hill_a2 = hill_a1 + 0.3f;
			const float hill_c = 4f;
			const float hill_tMax = 7.5f;
			readonly float hill_yMax = Mathf.Pow(hill_tMax, hill_a1)/ (Mathf.Pow(hill_c, hill_a1) + Mathf.Pow(hill_tMax, hill_a2));
			readonly float hill_cToPowA1 = Mathf.Pow(hill_c, hill_a1);

			Transform headEffectorPivotXform;
			Transform neckXform;
			Transform spineXform;
			Transform spineBaseXform;		
			
			#if USE_FINAL_IK || REM_USE_FINAL_IK
				RootMotion.FinalIK.LookAtIK lookAtIK;
			#endif
		
			// Head jitter
			readonly Vector3 headJitterRotationComponents = new Vector3(1, 1, 0);
			Vector2[] headJitterNoiseVectors;
			const int kHeadJitterOctave = 3;
			float headJitterTime;
			
			bool useHillIfPossible;
			
			float currentHeadIKWeight;
			
			float timeOfHeadMovementStart;
			float timeSinceEnabled;
			float headMaxSpeed;
			float headDuration;
			float startHeadDuration;
			Vector3 headMovementLocalToHeadBaseEulerDirection;
			Vector3 lastHillVelocity;
			float maxHillSmoothLerpDuringThisHeadMovement;
		
			Vector3 smoothDampVelocity;
			
			Quaternion lastHeadBaseFromHeadEffectorPivotQ;
			float headMovementTotalAngle;
			
			Vector3 forwardInHeadSpace;
			Vector3 forwardInNeckSpace;
			Quaternion character_From_Head_Q;
			Quaternion head_From_Character_Q;
			Quaternion character_From_Neck_Q;
			Quaternion neck_From_Character_Q;
			Quaternion headBase_From_HeadEffectorPivot_Q;
			Quaternion targetHeadBase_From_HeadEffector_Q;
			Quaternion headBase_From_head_Q;

			Vector3 eyeCenterOnHeadAxisInHeadPivotLocalCoords;

			Vector3 lastHeadEuler;

			public enum HeadSpeed
			{
				Slow,
				Fast
			}
			HeadSpeed headSpeed = HeadSpeed.Slow;
			
			public enum HeadControl
			{
				AnimatorIK,
				Transform,
				HeadTarget,
				FinalIK,
				None
			}
			public HeadControl headControl = HeadControl.Transform;
			
			public enum HeadAnimationType
			{
				HillHybrid,
				SmoothDamping
			}
			public HeadAnimationType headAnimationType = HeadAnimationType.HillHybrid;
			
		#endregion

	
		float ClampHorizontalHeadAngle(float headAngle)
		{
			float maxLimitedHeadAngle = Mathf.Lerp(kMaxHorizHeadAngle, 0, eyeAndHeadAnimator.limitHeadAngle);

			headAngle = Utils.NormalizedDegAngle(headAngle);
			float absAngle = Mathf.Abs(headAngle);

			return Mathf.Sign(headAngle) * Mathf.Min(maxLimitedHeadAngle, absAngle);
		}
		
		
		float ClampVerticalHeadAngle(float headAngle)
		{
			float maxLimitedHeadAngle = Mathf.Lerp(kMaxVertHeadAngle, 0, eyeAndHeadAnimator.limitHeadAngle);

			headAngle = Utils.NormalizedDegAngle(headAngle);
			float absAngle = Mathf.Abs(headAngle);

			return Mathf.Sign(headAngle) * Mathf.Min(maxLimitedHeadAngle, absAngle);
		}
		
		
		public Vector3 GetForwardRelativeToSpineToHeadAxis()
		{
			Vector3 up = (headXform.position - spineBaseXform.position).normalized;
			Vector3 right = Vector3.Cross(up, headBaseXform.forward);
			
			return Vector3.Cross(right, up);
		}
		
		
		public Quaternion GetHeadBoneOrientationForLookingAt(Vector3 headTargetGlobal)
		{
			return headEffectorPivotXform.parent.rotation * Quaternion.Euler(GetHeadEffectorTargetLocalAngelsForHeadTarget(headTargetGlobal)) * character_From_Head_Q;
		}

		
		public Vector3 GetHeadDirection()
		{
				return headXform.rotation * character_From_Head_Q * Vector3.forward;
		}
		
		
		Vector3 GetHeadEffectorTargetLocalAngelsForHeadTarget(Vector3 headTargetGlobalPos)
		{
			Vector3 lookForward = (headTargetGlobalPos - headEffectorPivotXform.position).normalized;
			
			Vector3 targetLocalAngles = (Quaternion.Inverse(headEffectorPivotXform.parent.rotation) *
							                             Quaternion.FromToRotation(headEffectorPivotXform.forward, lookForward) *
							                             headEffectorPivotXform.rotation).eulerAngles;
			
			//*** Adjust head angles such that the head rotates to make the eye center look at the head target, not the head base, which is below the eye center
			{
				Vector3 localAngles = headEffectorPivotXform.localEulerAngles;
				
				headEffectorPivotXform.localEulerAngles = targetLocalAngles;
				
				Vector3 headTargetCoordsInHeadPivotSpace = headEffectorPivotXform.InverseTransformPoint(headTargetGlobalPos);
				Vector3 eyeToTargetEuler = Quaternion.LookRotation(headTargetCoordsInHeadPivotSpace - eyeCenterOnHeadAxisInHeadPivotLocalCoords, Vector3.up).eulerAngles;
				targetLocalAngles = new Vector3(targetLocalAngles.x + eyeToTargetEuler.x, targetLocalAngles.y, targetLocalAngles.z);
				headEffectorPivotXform.localEulerAngles = localAngles;
				
				targetLocalAngles = new Vector3(LimitVerticalHeadAngleSoftly(targetLocalAngles.x),
								LimitHorizontalHeadAngleSoftly(targetLocalAngles.y),
								targetLocalAngles.z);
			}
				
			return targetLocalAngles;
		}
		

		public void Initialize(EyeAndHeadAnimator eyeAndHeadAnimator, Animator animator)
		{
			this.eyeAndHeadAnimator = eyeAndHeadAnimator;
			
			#if USE_FINAL_IK || REM_USE_FINAL_IK
				lookAtIK = eyeAndHeadAnimator.GetComponentInChildren<RootMotion.FinalIK.LookAtIK>();
				
				if ( lookAtIK != null && headControl != HeadControl.FinalIK )
					Debug.LogWarning(eyeAndHeadAnimator.name + " RealisticEyeMovements: head control is set to " + headControl + ", but LookAtIK component found.", eyeAndHeadAnimator.gameObject);
			#endif
			
			//*** Head jitter
			{
				headJitterTime = Random.value * 10;
				headJitterNoiseVectors = new Vector2[3];

		        for (var i = 0; i < 3; i++)
		        {
		            var theta = Random.value * Mathf.PI * 2;
		            headJitterNoiseVectors[i].Set(Mathf.Cos(theta), Mathf.Sin(theta));
		        }
			}
			
			currentHeadIKWeight = eyeAndHeadAnimator.headWeight;

			InitializeHeadXform(animator);
			InitializeHeadAnchor(animator);
		}
		
		
		void InitializeHeadAnchor(Animator animator)
		{
			if (headBaseXform == null)
			{
				spineXform = eyeAndHeadAnimator.spineBoneNonMecanim;
				
				if ( spineXform == null )
				{
					if ( animator != null )
						spineXform = Utils.GetSpineBoneFromAnimator(animator);
					
					if ( spineXform == null )
					{
						if ( headControl != HeadControl.None )
							Debug.LogWarning(eyeAndHeadAnimator.name + " RealisticEyeMovements: you should assign a spine bone in the Head section to use as base for head angles.", eyeAndHeadAnimator.gameObject);
						spineXform = eyeAndHeadAnimator.transform;
					}
				}
				
				// Spine base is positioned at the spine bone, but vertically under the head. It is used to compute "forward" for selecting new idle look targets: spine base to head bone is used as "up".
				GameObject go = new GameObject("REM " + eyeAndHeadAnimator.name + " spine base") { hideFlags = HideFlags.HideAndDontSave };
				spineBaseXform = go.transform;
				spineBaseXform.parent = spineXform;
				spineBaseXform.position =  headXform.position + Vector3.Project(spineXform.position  - headXform.position, eyeAndHeadAnimator.transform.up);
				spineBaseXform.rotation = eyeAndHeadAnimator.transform.rotation;
				
				go = new GameObject("REM " + eyeAndHeadAnimator.name + " head base") { hideFlags = HideFlags.HideAndDontSave };
				headBaseXform = go.transform;
				headBaseXform.parent = spineXform;
				headBaseXform.position = headXform.position;
				headBaseXform.rotation = eyeAndHeadAnimator.transform.rotation;
				
				headBase_From_head_Q = Quaternion.Inverse(headBaseXform.rotation) * headXform.rotation;
			}

			if (headEffectorPivotXform == null)
			{
				GameObject go = new GameObject("REM " + eyeAndHeadAnimator.name + " head target") { hideFlags = HideFlags.HideAndDontSave };
				headEffectorPivotXform = go.transform;
				headEffectorPivotXform.parent = headBaseXform;
				headEffectorPivotXform.localPosition = Vector3.zero;
				headEffectorPivotXform.localRotation = Quaternion.identity;

				lastHeadEuler = headEffectorPivotXform.localEulerAngles;
			}
		}

		
		void InitializeHeadXform(Animator animator)
		{
			if ( headXform != null )
				return;
			
			if ( headControl == HeadControl.FinalIK )
			{
				#if USE_FINAL_IK || REM_USE_FINAL_IK
					if ( lookAtIK != null )
						headXform = lookAtIK.solver.head.transform;
						
					if ( headXform == null )
					{
						Debug.LogError(eyeAndHeadAnimator.name + ": RealisticEyeMovements: head control is set to FinalIK, but head bone not found! Reverting to no head control.", eyeAndHeadAnimator.gameObject);
						headControl = HeadControl.None;
					}
				#else
					Debug.LogError(eyeAndHeadAnimator.name + ": RealisticEyeMovements: head control is set to FinalIK, but REM_USE_FINAL_IK is not defined. Reverting to no head control.", eyeAndHeadAnimator.gameObject);
					headControl = HeadControl.None;
				#endif
			}
			if ( headControl == HeadControl.AnimatorIK )
			{
				if ( headXform == null && animator != null && animator.GetBoneTransform(HumanBodyBones.Head) != null )
					headXform = animator.GetBoneTransform(HumanBodyBones.Head);
				
				if ( headXform == null )
				{
					Debug.LogError(eyeAndHeadAnimator.name + ": RealisticEyeMovements: head control is set to AnimatorIK, but head bone not found! Reverting to no head control.", eyeAndHeadAnimator.gameObject);
					headControl = HeadControl.None;
				}
			}
			if ( headControl == HeadControl.Transform || headControl == HeadControl.HeadTarget )
			{ 
				headXform = eyeAndHeadAnimator.headBoneNonMecanim;
				if ( headXform == null && animator != null )
					headXform = animator.GetBoneTransform(HumanBodyBones.Head);
				
				neckXform =eyeAndHeadAnimator.neckBoneNonMecanim;
				if ( neckXform == null && animator != null )
					neckXform = animator.GetBoneTransform(HumanBodyBones.Neck);
				
				if ( headXform == null )
				{
					Debug.LogError( eyeAndHeadAnimator.name + ": " + (headControl == HeadControl.Transform
							? "RealisticEyeMovements: head control is set to Transform, but no transform assigned and mecanim head not found! Reverting to no head control."
							: "ealisticEyeMovements: head control is set to Head Target, but no head target assigned! Reverting to no head control."),
						eyeAndHeadAnimator.gameObject);
					headControl = HeadControl.None;
				}
			}

			if ( headXform == null )
				headXform = eyeAndHeadAnimator.transform;
			
			Quaternion character_From_World_Q = Quaternion.Inverse(eyeAndHeadAnimator.transform.rotation);
			character_From_Head_Q = character_From_World_Q * headXform.rotation;
			head_From_Character_Q = Quaternion.Inverse(character_From_Head_Q);
			if ( neckXform != null )
			{
				character_From_Neck_Q = character_From_World_Q * neckXform.rotation;
				neck_From_Character_Q = Quaternion.Inverse(character_From_Neck_Q);
				forwardInNeckSpace = Quaternion.Inverse(neckXform.rotation) * eyeAndHeadAnimator.transform.forward;
			}
			forwardInHeadSpace = Quaternion.Inverse(headXform.rotation) * eyeAndHeadAnimator.transform.forward;
		}

		
		bool IsHeadMovementTotalAngleTooSmall()
		{
			return headMovementTotalAngle < 0.1f;
		}
		
		
		public bool IsSwitchingHeadTarget()
		{
			if ( headDuration <= 0 )
				return false;
			
			return Time.time - timeOfHeadMovementStart < headDuration;
		}
		
		
		public void LateUpdate(float deltaTime, float targetHeadWeight)
		{
			timeSinceEnabled += deltaTime;
			
			currentHeadIKWeight = Mathf.Lerp( currentHeadIKWeight, targetHeadWeight, deltaTime);
			
			UpdateHeadEffector(deltaTime);
			
			SetHeadOrientationFromHeadEffector();
		}
		
		
		public float LimitHorizontalHeadAngleSoftly( float headAngle )
		{
			float maxLimitedHeadAngle = Mathf.Lerp(kMaxHorizHeadAngle, 0, eyeAndHeadAnimator.limitHeadAngle);

			headAngle = Utils.NormalizedDegAngle(headAngle);
			float absAngle = Mathf.Abs(headAngle);
			const float kHorizontalFactor = 0.7f;
			
			float limitedAngle = Mathf.Sign(headAngle) * Mathf.Min(maxLimitedHeadAngle, absAngle * kHorizontalFactor);
			
			return limitedAngle;
		}


		public float LimitVerticalHeadAngleSoftly( float headAngle )
		{
			float maxLimitedHeadAngle = Mathf.Lerp(kMaxVertHeadAngle, 0, eyeAndHeadAnimator.limitHeadAngle);

			headAngle = Utils.NormalizedDegAngle(headAngle);
			float absAngle = Mathf.Abs(headAngle);
			const float kVerticalFactor = 0.7f;
			
			float limitedAngle = Mathf.Sign(headAngle) * Mathf.Min(maxLimitedHeadAngle, absAngle * kVerticalFactor);
			
			return limitedAngle;
		}


		public void OnAnimatorIK(Animator animator)
		{
			Vector3 headTargetPos = headEffectorPivotXform.TransformPoint(2 * eyeAndHeadAnimator.eyeDistanceScale * Vector3.forward);
			
			animator.SetLookAtPosition(headTargetPos);
			animator.SetLookAtWeight(eyeAndHeadAnimator.mainWeight * currentHeadIKWeight, eyeAndHeadAnimator.bodyWeight, 1, 0, 0);
		}
		
		
		
		public void OnEnable()
		{
			timeSinceEnabled = 0;
		}
		
		
		public void SetEyeRootXform(Transform eyeRootXform)
		{
			eyeCenterOnHeadAxisInHeadPivotLocalCoords = headEffectorPivotXform.InverseTransformPoint(eyeRootXform.position);
			eyeCenterOnHeadAxisInHeadPivotLocalCoords.z = 0;
		}
		
		
		void SetHeadOrientationFromHeadEffector()
		{
			if ( headControl == HeadControl.Transform )
			{
				if ( headXform != null )
				{
					if ( neckXform != null && (eyeAndHeadAnimator.neckHorizWeight > 0 || eyeAndHeadAnimator.neckVertWeight > 0) )
					{
						Vector3 localEuler = headEffectorPivotXform.localEulerAngles;
						Vector3 neckTargetForward = headEffectorPivotXform.parent.rotation *
						                                Quaternion.Euler(Utils.NormalizedDegAngle(localEuler.x) * eyeAndHeadAnimator.neckVertWeight * 0.5f,
															                                Utils.NormalizedDegAngle(localEuler.y) * eyeAndHeadAnimator.neckHorizWeight * 0.5f,
															                                0) *
									                                character_From_Neck_Q *
									                                forwardInNeckSpace;
						Vector3 neckCurrentForward = neckXform.rotation * forwardInNeckSpace;
						Quaternion target_world_From_neck_Q = Quaternion.FromToRotation(neckCurrentForward, neckTargetForward) * neckXform.rotation;
						neckXform.rotation = Quaternion.Slerp(neckXform.rotation, target_world_From_neck_Q, eyeAndHeadAnimator.mainWeight * currentHeadIKWeight);
					}
					
					Vector3 targetForward = (headEffectorPivotXform.TransformPoint( eyeAndHeadAnimator.eyeDistanceScale * Vector3.forward)  - headXform.position).normalized;
					Vector3 headForward = headXform.rotation * forwardInHeadSpace;
					
					Quaternion target_world_From_head_Q = Quaternion.FromToRotation(headForward, targetForward) * headXform.rotation;
					
					headXform.rotation = Quaternion.Slerp(headXform.rotation, target_world_From_head_Q, eyeAndHeadAnimator.mainWeight * currentHeadIKWeight);
				}
			}
			
			else if ( headControl == HeadControl.HeadTarget )
			{
				if ( eyeAndHeadAnimator.headTarget != null )
					eyeAndHeadAnimator.headTarget.position = Vector3.Lerp(headBaseXform.TransformPoint(Vector3.forward),
																											headEffectorPivotXform.TransformPoint(Vector3.forward), 
																											eyeAndHeadAnimator.mainWeight * currentHeadIKWeight);
			}
			
			#if USE_FINAL_IK || REM_USE_FINAL_IK
				else if ( headControl == HeadControl.FinalIK )
					UpdateFinalIK();
			#endif
		}
		
		
		public void SetHeadSpeed(HeadSpeed headSpeed)
		{
			this.headSpeed = headSpeed;
		}
		
		
		public void StartHeadMovement()
		{
			Vector3 targetLocalAngles =GetHeadEffectorTargetLocalAngelsForHeadTarget(eyeAndHeadAnimator.GetCurrentHeadTargetPos());

			Quaternion headBaseFromWorldQ = Quaternion.Inverse(headBaseXform.rotation);
			headBase_From_HeadEffectorPivot_Q = lastHeadBaseFromHeadEffectorPivotQ = headBaseFromWorldQ * headEffectorPivotXform.rotation;
			targetHeadBase_From_HeadEffector_Q = headBaseFromWorldQ * headEffectorPivotXform.parent.rotation * Quaternion.Euler(targetLocalAngles);
			headMovementTotalAngle = Mathf.Abs(Utils.NormalizedDegAngle(Quaternion.Angle(headBase_From_HeadEffectorPivot_Q, targetHeadBase_From_HeadEffector_Q)));
			
			if ( IsHeadMovementTotalAngleTooSmall() )
				return;
			
			Vector3 localToHeadBaseAngles = headBase_From_HeadEffectorPivot_Q.eulerAngles;
			Vector3 targetLocalToHeadBaseAngles = targetHeadBase_From_HeadEffector_Q.eulerAngles;
			
			headMovementLocalToHeadBaseEulerDirection = new Vector3(Utils.NormalizedDegAngle(targetLocalToHeadBaseAngles.x) - Utils.NormalizedDegAngle(localToHeadBaseAngles.x),
				Utils.NormalizedDegAngle(targetLocalToHeadBaseAngles.y) - Utils.NormalizedDegAngle(localToHeadBaseAngles.y),
				Utils.NormalizedDegAngle(targetLocalToHeadBaseAngles.z) - Utils.NormalizedDegAngle(localToHeadBaseAngles.z)).normalized; 
			lastHillVelocity = Vector3.zero;
			
			bool isQuickMove = headSpeed == HeadSpeed.Fast;

			const float d1fast = 0.38746871f;
			const float d2fast = 0.00741433f;
			const float d1slow = 0.58208538f;
			const float d2slow = 0.01056395f;
			float d1 = isQuickMove ? d1fast : d1slow;
			float d2 = isQuickMove ? d2fast : d2slow;
			headDuration = d1 + d2 * headMovementTotalAngle;

			const float m1fast = 33.42039746f;
			const float m2fast = 2.58679992f;
			const float m1slow = 19.79938085f;
			const float m2slow = 1.6078972f;
			float m1 = isQuickMove ? m1fast : m1slow;
			float m2 = isQuickMove ? m2fast : m2slow;
			headMaxSpeed = m1 + m2 * headMovementTotalAngle;

			const float realismFactor = 0.6f; // slow down to make head movement look better
			float mod = (isQuickMove ? 1 : 0.5f) * eyeAndHeadAnimator.headChangeToNewTargetSpeed * realismFactor;
			headMaxSpeed *= mod;
			headDuration /= mod;
			
			startHeadDuration = headDuration;

			timeOfHeadMovementStart = Time.time;

			maxHeadHorizSpeedSinceSaccadeStart = maxHeadVertSpeedSinceSaccadeStart = 0;
			
			useHillIfPossible = headMovementTotalAngle > 3 && headAnimationType == HeadAnimationType.HillHybrid && timeSinceEnabled >= 0.1f; 
			maxHillSmoothLerpDuringThisHeadMovement = useHillIfPossible ? 0 : 1;
		}


		public void TiltHead()
		{
			if ( neckXform != null )
			{
				Quaternion neckTargetRotation = neckXform.rotation * neck_From_Character_Q * Quaternion.Euler(eyeAndHeadAnimator.neckPitchAngle, eyeAndHeadAnimator.neckYawAngle, eyeAndHeadAnimator.neckRollAngle) * character_From_Neck_Q;
				neckXform.rotation = Quaternion.Slerp(neckXform.rotation, neckTargetRotation, eyeAndHeadAnimator.mainWeight * currentHeadIKWeight);
			}
			
			Quaternion headTargetRotation = headXform.rotation * head_From_Character_Q * Quaternion.Euler(eyeAndHeadAnimator.headPitchAngle, eyeAndHeadAnimator.headYawAngle, eyeAndHeadAnimator.headRollAngle) * character_From_Head_Q;
			headXform.rotation = Quaternion.Slerp(headXform.rotation, headTargetRotation, eyeAndHeadAnimator.mainWeight * currentHeadIKWeight);
		}
		
		
		public void Update()
		{
			if ( headControl == HeadControl.Transform && headXform != null && eyeAndHeadAnimator.headWeight > 0 && eyeAndHeadAnimator.resetHeadAtFrameStart )
			{
				// Reset head to default orientation relative to headBase because we use FromToRotation later
				headXform.rotation = headBaseXform.rotation * headBase_From_head_Q;
			}
		}
		
		
		#if USE_FINAL_IK || REM_USE_FINAL_IK
			void UpdateFinalIK()
			{
				if ( lookAtIK != null )
				{
					lookAtIK.solver.IKPositionWeight = eyeAndHeadAnimator.mainWeight * currentHeadIKWeight;
					lookAtIK.solver.IKPosition = headEffectorPivotXform.TransformPoint( 2 * eyeAndHeadAnimator.eyeDistanceScale * Vector3.forward );
				}
			}
		#endif
		
		
		void UpdateHeadEffector(float deltaTime)
		{
			if ( headControl == HeadControl.None || currentHeadIKWeight <= 0 || deltaTime <= 0 || IsHeadMovementTotalAngleTooSmall() )
				return;

			Vector3 headTargetGlobalPos = eyeAndHeadAnimator.GetCurrentHeadTargetPos();
			
			Quaternion headBaseInverse = Quaternion.Inverse(headBaseXform.rotation);
			
			Quaternion currentLocalToHeadBaseQ = headBaseInverse * headEffectorPivotXform.rotation;
			Vector3 localToHeadBaseAngles = new Vector3(ClampVerticalHeadAngle(currentLocalToHeadBaseQ.eulerAngles.x), ClampHorizontalHeadAngle(currentLocalToHeadBaseQ.eulerAngles.y), currentLocalToHeadBaseQ.eulerAngles.z);
			Vector3 targetLocalAngles = GetHeadEffectorTargetLocalAngelsForHeadTarget(headTargetGlobalPos);
			
			Quaternion targetLoalToHeadBaseQ = headBaseInverse * headEffectorPivotXform.parent.rotation * Quaternion.Euler(targetLocalAngles);
			Vector3 targetLocalToHeadBaseAngles =  targetLoalToHeadBaseQ.eulerAngles;
			
			localToHeadBaseAngles = new Vector3(Utils.NormalizedDegAngle(localToHeadBaseAngles.x), Utils.NormalizedDegAngle(localToHeadBaseAngles.y), Utils.NormalizedDegAngle(localToHeadBaseAngles.z));
			targetLocalToHeadBaseAngles = new Vector3(Utils.NormalizedDegAngle(targetLocalToHeadBaseAngles.x), Utils.NormalizedDegAngle(targetLocalToHeadBaseAngles.y), Utils.NormalizedDegAngle(targetLocalToHeadBaseAngles.z));
			
			//*** Head jitter
			{
				if (eyeAndHeadAnimator.useHeadJitter)
				{
					headJitterTime += deltaTime * eyeAndHeadAnimator.headJitterFrequency;
				
					var r = new Vector3(
						Utils.Fbm(headJitterNoiseVectors[0] * headJitterTime, kHeadJitterOctave),
						Utils.Fbm(headJitterNoiseVectors[1] * headJitterTime, kHeadJitterOctave),
						Utils.Fbm(headJitterNoiseVectors[2] * headJitterTime, kHeadJitterOctave)
					);
					r = Vector3.Scale(r, headJitterRotationComponents) * (eyeAndHeadAnimator.headJitterAmplitude * 2);
					targetLocalToHeadBaseAngles += r;
				}
			}

			//*** After the head moved to the new POI, adjust head duration so the head keeps tracking the target quickly enough.
			{
				float kHeadDurationForTracking = eyeAndHeadAnimator.headTrackTargetSpeed > 0 ? 0.025f / eyeAndHeadAnimator.headTrackTargetSpeed : 100000;
				float kHeadMaxSpeedForTracking = 500 * eyeAndHeadAnimator.headTrackTargetSpeed;
				float timeSinceInitiatedHeadMovementStop = Time.time - (timeOfHeadMovementStart + startHeadDuration);
				if (timeSinceInitiatedHeadMovementStop > 0)
				{
					headDuration = Mathf.Lerp(headDuration, kHeadDurationForTracking, Time.deltaTime * 2);
					headMaxSpeed = Mathf.Lerp(headMaxSpeed, kHeadMaxSpeedForTracking, Time.deltaTime * 2);
				}
			}
			
			Vector3 velBeforeLerp = smoothDampVelocity;
			smoothDampVelocity = Vector3.Lerp(actualVelocity, smoothDampVelocity, maxHillSmoothLerpDuringThisHeadMovement);
			float smoothDuration = headDuration * 0.4f;
			
			Vector3 velBeforeSmoothDamp = smoothDampVelocity;
			Vector3 smoothDampLocalAngles = Vector3.SmoothDamp(localToHeadBaseAngles, targetLocalToHeadBaseAngles, ref smoothDampVelocity, smoothDuration, headMaxSpeed, deltaTime);
			
			if ( float.IsNaN(smoothDampLocalAngles.x) || float.IsNaN(smoothDampLocalAngles.y) || float.IsNaN(smoothDampLocalAngles.z) )
			{
				Debug.LogError($"NaN in smoothDampLocalAngles: {smoothDampLocalAngles.x} {smoothDampLocalAngles.y} {smoothDampLocalAngles.z} local: {localToHeadBaseAngles.x} {localToHeadBaseAngles.y} {localToHeadBaseAngles.z} target: {targetLocalToHeadBaseAngles.x} {targetLocalToHeadBaseAngles.y} {targetLocalToHeadBaseAngles.z}", eyeAndHeadAnimator.gameObject);
				Debug.LogError($"\tvel {velBeforeSmoothDamp.x} {velBeforeSmoothDamp.y} {velBeforeSmoothDamp.z} smoothDuration: {smoothDuration} headMaxSpeed: {headMaxSpeed} deltaTime: {deltaTime}");
				Debug.LogError($"\tvelBeforeLerp: {velBeforeLerp.x} {velBeforeLerp.y} {velBeforeLerp.z} maxHillSmoothLerpDuringThisHeadMovement: {maxHillSmoothLerpDuringThisHeadMovement}");
				Debug.LogError($"\theadMovementTotalAngle: {headMovementTotalAngle:0.00} headMaxSpeed: {headMaxSpeed:0.00}");
				
				return;
			}
			
			float timeSinceHeadMovementStart = Time.time - timeOfHeadMovementStart;
			if ( useHillIfPossible &&  headDuration > 0 && headDuration >= timeSinceHeadMovementStart && maxHillSmoothLerpDuringThisHeadMovement < 1 )
			{
				float t = timeSinceHeadMovementStart * hill_tMax / headDuration;
				float y01 = Mathf.Pow(t, hill_a1)/hill_yMax / (hill_cToPowA1  + Mathf.Pow(t, hill_a2));
			
				float velocityDiff =	Mathf.Abs(Utils.NormalizedDegAngle(lastHillVelocity.x - Utils.NormalizedDegAngle(smoothDampVelocity.x))) +
											Mathf.Abs(Utils.NormalizedDegAngle(lastHillVelocity.y - Utils.NormalizedDegAngle(smoothDampVelocity.y))) +
											Mathf.Abs(Utils.NormalizedDegAngle(lastHillVelocity.z - Utils.NormalizedDegAngle(smoothDampVelocity.z)));
				
				Quaternion hillLocalToHeadBaseQ = Quaternion.Slerp(headBase_From_HeadEffectorPivot_Q, targetHeadBase_From_HeadEffector_Q, y01);
				
				float angleFromExpectedCurrentDirection = Mathf.Abs(Utils.NormalizedDegAngle(Quaternion.Angle(lastHeadBaseFromHeadEffectorPivotQ, Quaternion.Euler(localToHeadBaseAngles))));
				float angleFromExpectedTarget = Mathf.Abs(Utils.NormalizedDegAngle(Quaternion.Angle(targetHeadBase_From_HeadEffector_Q, targetLoalToHeadBaseQ)));
				lastHeadBaseFromHeadEffectorPivotQ = hillLocalToHeadBaseQ;
				const float relativeAngleErrorAtWhichToUseSmoothDampInsteadOfHill = 0.1f;
				const float relativeAngleSpeedErrorAtWhichToUseSmoothDampInsteadOfHill = 0.25f;
				float hillSmoothLerp = Mathf.Clamp01((angleFromExpectedCurrentDirection + angleFromExpectedTarget)/headMovementTotalAngle/relativeAngleErrorAtWhichToUseSmoothDampInsteadOfHill +
				                                     velocityDiff/headMaxSpeed/relativeAngleSpeedErrorAtWhichToUseSmoothDampInsteadOfHill);
				
				if ( float.IsNaN(hillSmoothLerp) )
					Debug.LogError($"hillSmoothLerp is NaN. angleFromExpectedCurrentDirection: {angleFromExpectedCurrentDirection:0.00} angleFromExpectedTarget: {angleFromExpectedTarget:0.00} velocityDiff: {velocityDiff:0.00}");
				
				maxHillSmoothLerpDuringThisHeadMovement = Mathf.Max(maxHillSmoothLerpDuringThisHeadMovement, hillSmoothLerp);
				
				headEffectorPivotXform.rotation = headBaseXform.rotation * 
				                                  Quaternion.Slerp(hillLocalToHeadBaseQ, Quaternion.Euler(smoothDampLocalAngles), maxHillSmoothLerpDuringThisHeadMovement);
			
				float denominatorFactor = hill_cToPowA1 + Mathf.Pow(t, hill_a2); 
				float hillSpeedNormalized = hill_a1 * Mathf.Pow(t, hill_a1-1)/(hill_cToPowA1 + Mathf.Pow(t, hill_a2))  -  hill_a2 * Mathf.Pow(t, 2*hill_a1-0.7f)/(denominatorFactor*denominatorFactor);
				float hillSpeed = headMovementTotalAngle/hill_yMax * hill_tMax/headDuration * hillSpeedNormalized;
				lastHillVelocity = hillSpeed * headMovementLocalToHeadBaseEulerDirection;
			}
			else
			{
				headEffectorPivotXform.rotation = headBaseXform.rotation * Quaternion.Euler(smoothDampLocalAngles);
				maxHillSmoothLerpDuringThisHeadMovement = 1;
			}
			
			Vector3 currentLocalToHeadBaseEuler = (headBaseInverse * headEffectorPivotXform.rotation).eulerAngles;
			Vector3 diff = new Vector3(Utils.NormalizedDegAngle(currentLocalToHeadBaseEuler.x) - Utils.NormalizedDegAngle(lastHeadEuler.x),
													Utils.NormalizedDegAngle(currentLocalToHeadBaseEuler.y) - Utils.NormalizedDegAngle(lastHeadEuler.y),
													Utils.NormalizedDegAngle(currentLocalToHeadBaseEuler.z) - Utils.NormalizedDegAngle(lastHeadEuler.z));
			actualVelocity = diff/deltaTime;

			lastHeadEuler = currentLocalToHeadBaseEuler;
			maxHeadHorizSpeedSinceSaccadeStart = Mathf.Max(maxHeadHorizSpeedSinceSaccadeStart, Mathf.Abs(actualVelocity.y));
			maxHeadVertSpeedSinceSaccadeStart = Mathf.Max(maxHeadHorizSpeedSinceSaccadeStart, Mathf.Abs(actualVelocity.x));
		}

	}

}