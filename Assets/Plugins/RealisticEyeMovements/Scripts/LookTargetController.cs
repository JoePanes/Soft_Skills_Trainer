// LookTargetController.cs
// Tore Knabe
// Copyright 2020 tore.knabe@gmail.com


using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.XR;
using Random = UnityEngine.Random;
// ReSharper disable UnassignedField.Global
// ReSharper disable MemberCanBePrivate.Global

namespace RealisticEyeMovements {

	[HelpURL("https://docs.google.com/document/d/1b91EBehAyq_7GpTTxRHp1M5UbHfDQQ9CBektQgedXUg/edit?usp=sharing")]
	public class LookTargetController : MonoBehaviour
	{
		#region fields

			[Tooltip("Drag objects here for the actor to look at. If empty, actor will look in random directions.")]
			public List<Transform> pointsOfInterest;

			[Tooltip("Ratio of how often to look at player vs elsewhere. 0: never, 1: always")]
			[Range(0,1)]
			public float lookAtPlayerRatio = 0.1f;

			[Tooltip("How likely the actor is to look back at the player when player stares at actor.")]
			[Range(0,1)]
			public float stareBackFactor = 0;

			[Tooltip("If player is closer than this, notice him")]
			[Range(0, 100)]
			public float noticePlayerDistance = 0;

			[Tooltip("If player is closer than this, look away (overrides noticing him)")]
			[Range(0, 4)]
			public float personalSpaceDistance = 0;

			[Tooltip("Minimum time to look at a target")]
			[Range(0.1f, 100f)]
			public float minLookTime = 3f;

			[Tooltip("Maximum time to look at a target")]
			[Range(0.1f, 100f)]
			public float maxLookTime = 10f;

			[Tooltip("For 3rd person games, set this to the player's eye center transform")]
			#if !UNITY_WP8 && !UNITY_WP_8_1 && !UNITY_METRO
				[FormerlySerializedAs ("playerEyeCenter")]
			#endif
			public Transform thirdPersonPlayerEyeCenter;

			[Tooltip("Keep trying to track target even when it moves out of sight")]
			public bool keepTargetEvenWhenLost = true;
			[Tooltip("When looking idly, avoid choosing a look target that makes the character seem like they are looking through the player")]
			public bool avoidLookingThroughPlayer;

			public Transform playerLeftEyeXform { get; private set; }
			public Transform playerRightEyeXform { get; private set; }
			
			public float DistanceToPlayer { get; private set; }
			
			
			[Header("Events")]
			public UnityEvent OnStartLookingAtPlayer = new UnityEvent();
			public UnityEvent OnStopLookingAtPlayer = new UnityEvent();
			public UnityEvent OnPlayerEntersPersonalSpace = new UnityEvent();
			public UnityEvent OnLookAwayFromShyness = new UnityEvent();

			public Func<List<Transform>> GetPOIsDelegate;
			public Func<Transform, bool> IsPlayerInViewDelegate;
			public Func<Transform, bool> IsPlayerLookingAtMeDelegate;
			
			EyeAndHeadAnimator eyeAndHeadAnimator;

			const float minLookAtMeTimeToReact = 4;

			Transform targetPOI;
		
			Transform mainCameraXform;
			Transform mainCameraParentXform;
			Transform playerEyeCenterXform;
			Transform usedThirdPersonPlayerEyeCenter;

			Camera playerCamera;

			GameObject createdVRParentGO;
			GameObject createdPlayerEyeCenterGO;
			GameObject createdPlayerLeftEyeGO;
			GameObject createdPlayerRightEyeGO;
		
			float lastDistanceToPlayer = -1;
			float smoothedPlayerApproachSpeed;
			float playerLookingAtMeTime;
			float nextChangePOITime;
			float stareBackDeadtime;	
			float timeOfLastNoticeCheck = -1000;
			float timeOfLastLookBackCheck = -1000;
			float timeOutsideOfAwarenessZone = 1000;
			float timeInsidePersonalSpace;

			bool useNativeVRSupport;
			bool useVR;

			bool isInitialized;
			bool wasInPersonalSpaceLastFrame;
			
			readonly List<XRNodeState> nodeStateList = new List<XRNodeState>();
			readonly List<Transform> poisInView = new List<Transform>();


			enum State
			{
				LookingAtPlayer,
				LookingAroundIdly,
				LookingAtPoiDirectly,
				LookingAwayFromPlayer
			}
			State state = State.LookingAroundIdly;

		#endregion
	
	
		void Awake()
		{
			// For VR we use Unity's InputTracking to find the user's eyes, but that only gives local positions, and
			// in case the main camera has no parent, we need to create a reference parent to compute the global position from
			// the local one.
			GameObject mainCameraParentGO = new GameObject("Original Camera Position") { hideFlags = HideFlags.HideAndDontSave };
			mainCameraParentXform = mainCameraParentGO.transform;
			mainCameraParentXform.parent = transform;
		}


		public void Blink()
		{
			eyeAndHeadAnimator.Blink();
		}


		bool CanChooseAsNextTargetPOI(Transform t, bool avoidPlayer)
		{
			return t != null &&
				      t != targetPOI &&
				      eyeAndHeadAnimator.CanGetIntoView(t.position) &&
				      (false == avoidPlayer || playerEyeCenterXform == null || false == eyeAndHeadAnimator.AreInSimilarLookDirection(playerEyeCenterXform.position, t.position)) &&
				      t.gameObject.activeInHierarchy;
		}
		
		
		void ChangeStateTo(State newState)
		{
			if ( state != State.LookingAtPlayer && newState == State.LookingAtPlayer )
				if ( OnStartLookingAtPlayer != null )
					OnStartLookingAtPlayer.Invoke();

			if ( state == State.LookingAtPlayer && newState != State.LookingAtPlayer )
				if ( OnStopLookingAtPlayer != null )
					OnStopLookingAtPlayer.Invoke();

			state = newState;
		}


		Transform ChooseNextTargetPOI(bool avoidPlayer)
		{
			List<Transform> _pois = CurrentPOIs();
			
			if ( _pois == null || _pois.Count == 0 )
				return targetPOI;
			
			poisInView.Clear();
			foreach (Transform t in _pois)
				if ( CanChooseAsNextTargetPOI(t, avoidPlayer) )
					poisInView.Add(t);

			if ( poisInView.Count == 0 )
				return targetPOI;
			
			return poisInView[Random.Range(0, poisInView.Count)];
		}


		public void ClearLookTarget()
		{
			eyeAndHeadAnimator.ClearLookTarget();
			nextChangePOITime = -1;
		}


		List<Transform> CurrentPOIs()
		{
			return GetPOIsDelegate != null ? GetPOIsDelegate() : pointsOfInterest;
		}
		
			
		Transform FindPlayer()
		{
			playerCamera = null;
			if ( thirdPersonPlayerEyeCenter != null )
				return thirdPersonPlayerEyeCenter;
				
			if ( Camera.main != null )
			{
				playerCamera = Camera.main;
				return playerCamera.transform;
			}
				
			foreach ( Camera cam in FindObjectsOfType<Camera>() )
				if ( cam.targetTexture == null )
				{
					playerCamera = cam;
					return playerCamera.transform;
				}
					
			return null;
		}
				
								
		public void Initialize()
		{
			if ( isInitialized )
				return;

			if ( createdVRParentGO != null )
			{
				DestroyNotifier destroyNotifier = createdVRParentGO.GetComponent<DestroyNotifier>();
				if ( destroyNotifier != null )
					destroyNotifier.OnDestroyedEvent -= OnPlayerEyesParentDestroyed;

				Destroy(createdVRParentGO);

				createdVRParentGO = null;
				createdPlayerEyeCenterGO = null;
				createdPlayerLeftEyeGO = null;
				createdPlayerRightEyeGO = null;
			}

			eyeAndHeadAnimator = GetComponent<EyeAndHeadAnimator>();
			eyeAndHeadAnimator.Initialize();

			eyeAndHeadAnimator.OnTargetDestroyed -= OnTargetDestroyed;
			eyeAndHeadAnimator.OnCannotGetTargetIntoView -= OnCannotGetTargetIntoView;
			eyeAndHeadAnimator.OnUpdate2Finished -= VeryLateUpdate;

			eyeAndHeadAnimator.OnTargetDestroyed += OnTargetDestroyed;
			eyeAndHeadAnimator.OnCannotGetTargetIntoView += OnCannotGetTargetIntoView;
			eyeAndHeadAnimator.OnUpdate2Finished += VeryLateUpdate;

			playerEyeCenterXform = playerLeftEyeXform = playerRightEyeXform = null;

			//*** Player eyes: either user main camera or VR cameras
			{
				useNativeVRSupport = useVR = XRSettings.enabled;

				if ( useNativeVRSupport )
				{
					if ( FindPlayer() == null )
					{
						Debug.LogWarning("Main camera not found. Please set the main camera's tag to 'MainCamera'.");
						useVR = false;
						useNativeVRSupport = false;
						lookAtPlayerRatio = 0;
					}
					else
					{
						mainCameraXform = FindPlayer();
						createdPlayerEyeCenterGO = new GameObject("CreatedPlayerCenterVREye") { hideFlags = HideFlags.HideAndDontSave };
						createdPlayerLeftEyeGO = new GameObject("CreatedPlayerLeftVREye") { hideFlags = HideFlags.HideAndDontSave };
						createdPlayerRightEyeGO = new GameObject("CreatedPlayerRightVREye") { hideFlags = HideFlags.HideAndDontSave };

						playerEyeCenterXform = createdPlayerEyeCenterGO.transform;
						playerLeftEyeXform = createdPlayerLeftEyeGO.transform;
						playerRightEyeXform = createdPlayerRightEyeGO.transform;

						Transform playerXform = mainCameraXform;
						createdVRParentGO = new GameObject("PlayerEyesParent") { hideFlags = HideFlags.HideAndDontSave };
						DontDestroyOnLoad(createdVRParentGO);
						DestroyNotifier destroyNotifier = createdVRParentGO.AddComponent<DestroyNotifier>();
						destroyNotifier.OnDestroyedEvent += OnPlayerEyesParentDestroyed;
						createdVRParentGO.transform.SetPositionAndRotation(playerXform.position, playerXform.rotation);
						createdVRParentGO.transform.parent = playerXform.parent;

						createdPlayerEyeCenterGO.transform.parent = createdVRParentGO.transform;
						createdPlayerLeftEyeGO.transform.parent = createdVRParentGO.transform;
						createdPlayerRightEyeGO.transform.parent = createdVRParentGO.transform;

						UpdateNativeVREyePositions();
					}
				}

				if ( false == useVR )
				{
					if ( FindPlayer() != null )
						playerEyeCenterXform = FindPlayer();
					else
					{
						Debug.LogWarning("Main camera not found. Please set the main camera's tag to 'MainCamera' or set Player Eye Center.");
						lookAtPlayerRatio = 0;
					}
				}
			}

			UpdatePlayerEyeTransformReferences();

			isInitialized = true;
		}


		public bool IsLookingAtPlayer()
		{
			return state == State.LookingAtPlayer;
		}


		public bool IsPlayerInView()
		{
			if ( thirdPersonPlayerEyeCenter != usedThirdPersonPlayerEyeCenter )
				UpdatePlayerEyeTransformReferences();
			
			if ( IsPlayerInViewDelegate != null )
				return IsPlayerInViewDelegate(playerEyeCenterXform);
			
			return playerEyeCenterXform != null && eyeAndHeadAnimator.IsInView( playerEyeCenterXform.position );
		}

		
		public bool IsPlayerLookingAtMe()
		{
			if ( thirdPersonPlayerEyeCenter != usedThirdPersonPlayerEyeCenter )
				UpdatePlayerEyeTransformReferences();
			
			if ( IsPlayerLookingAtMeDelegate != null )
				return IsPlayerLookingAtMeDelegate(playerEyeCenterXform);
			
			float playerLookingAtMeAngle = eyeAndHeadAnimator.GetStareAngleTargetAtMe( playerEyeCenterXform );
			return playerLookingAtMeAngle < 15;
		}

		
		// To keep looking at the player until new command, set duration to -1
		public void LookAtPlayer(float duration=-1, float headLatency=0.075f)
		{
			if (false == isInitialized)
				Initialize();

			if ( thirdPersonPlayerEyeCenter != usedThirdPersonPlayerEyeCenter )
				UpdatePlayerEyeTransformReferences();

			if ( playerLeftEyeXform != null && playerRightEyeXform	!= null )
				eyeAndHeadAnimator.LookAtFace( playerLeftEyeXform, playerRightEyeXform, playerEyeCenterXform, headLatency );
			else if ( playerEyeCenterXform != null )
				eyeAndHeadAnimator.LookAtFace( playerEyeCenterXform, headLatency );
			else
				return;
			
			nextChangePOITime = duration >= 0 ? Time.time + duration : -1;

			targetPOI = null;
			timeOutsideOfAwarenessZone = 0;

			ChangeStateTo(State.LookingAtPlayer);
		}
	
	
		public void LookAroundIdly()
		{
			if (false == isInitialized)
				Initialize();

			if ( state == State.LookingAtPlayer )
				stareBackDeadtime = Random.Range(10.0f, 30.0f);
			
			nextChangePOITime = Time.time + Random.Range(Mathf.Min(minLookTime, maxLookTime), Mathf.Max(minLookTime, maxLookTime));
			
			Transform nextTargetPOI = ChooseNextTargetPOI(avoidLookingThroughPlayer);
			if ( nextTargetPOI != null && nextTargetPOI == targetPOI && state == State.LookingAroundIdly )
			{
				if ( avoidLookingThroughPlayer )
				{
					// If we cannot find a point avoiding looking through the player, search again without trying to avoid
					nextTargetPOI = ChooseNextTargetPOI(false);
					if ( nextTargetPOI != null && nextTargetPOI == targetPOI && state == State.LookingAroundIdly )
						return;
				}
				else
				return;
			}
			
			targetPOI = nextTargetPOI; 

			if ( targetPOI != null )
				eyeAndHeadAnimator.LookAtAreaAround( targetPOI );
			else
			{
				if ( avoidLookingThroughPlayer && playerEyeCenterXform != null )
					eyeAndHeadAnimator.LookAroundIdly(playerEyeCenterXform);
				else
				eyeAndHeadAnimator.LookAroundIdly();
			}
					
			ChangeStateTo(State.LookingAroundIdly);
		}


		// To keep looking at the poi until new command, set duration to -1
		public void LookAtPoiDirectly( Transform poiXform, float duration=-1, float headLatency=0.075f )
		{
			if ( null == poiXform )
				return;
			
			if (false == isInitialized)
				Initialize();

			eyeAndHeadAnimator.LookAtSpecificThing( poiXform, headLatency );
			nextChangePOITime = duration >= 0 ? Time.time + duration : -1;
			
			ChangeStateTo(State.LookingAtPoiDirectly);
		}
	
	
		// To keep looking at the poi until new command, set duration to -1
		public void LookAtPoiDirectly( Vector3 poi, float duration=-1, float headLatency=0.075f )
		{
			if (false == isInitialized)
				Initialize();

			eyeAndHeadAnimator.LookAtSpecificThing( poi, headLatency: headLatency );
			nextChangePOITime = duration >= 0 ? Time.time + duration : -1;
			ChangeStateTo(State.LookingAtPoiDirectly);
		}
	
	
		void LookAwayFromPlayer()
		{
			if ( playerEyeCenterXform == null )
				return;

			if ( state != State.LookingAwayFromPlayer )
				OnLookAwayFromShyness.Invoke();

			stareBackDeadtime = Random.Range(5.0f, 10.0f);
			
			bool isPlayerOnMyLeft = eyeAndHeadAnimator.GetHeadParentXform().InverseTransformPoint( playerEyeCenterXform.position ).x < 0;
			Vector3 awayPoint = eyeAndHeadAnimator.GetHeadParentXform().TransformPoint( eyeAndHeadAnimator.GetOwnEyeCenter() + 10 * (Quaternion.Euler(0, isPlayerOnMyLeft ? 50 : -50, 0 ) * Vector3.forward));
			eyeAndHeadAnimator.LookAtAreaAround( awayPoint );

			nextChangePOITime = Time.time + Random.Range(Mathf.Min(minLookTime, maxLookTime), Mathf.Max(minLookTime, maxLookTime));

			ChangeStateTo(State.LookingAwayFromPlayer);
		}


		void OnCannotGetTargetIntoView()
		{
			List<Transform> _pois = CurrentPOIs();
			
			bool shouldKeepTryingToLookAtTarget =	(state == State.LookingAtPoiDirectly ||
																		state == State.LookingAtPlayer && nextChangePOITime < 0 ||
																		state == State.LookingAroundIdly && _pois != null && _pois.Count == 1 && targetPOI == _pois[0])
																&& keepTargetEvenWhenLost;
			if ( false == shouldKeepTryingToLookAtTarget && eyeAndHeadAnimator.CanChangePointOfAttention() )
				OnTargetLost();
		}

		
		void OnDestroy()
		{
			if ( createdVRParentGO != null )
			{
				DestroyNotifier destroyNotifier = createdVRParentGO.GetComponent<DestroyNotifier>();
				if ( destroyNotifier != null )
					destroyNotifier.OnDestroyedEvent -= OnPlayerEyesParentDestroyed;

				Destroy(createdVRParentGO);
			}

			if ( isInitialized && eyeAndHeadAnimator != null )
			{
				eyeAndHeadAnimator.OnTargetDestroyed -= OnTargetDestroyed;
				eyeAndHeadAnimator.OnCannotGetTargetIntoView -= OnCannotGetTargetIntoView;
				eyeAndHeadAnimator.OnUpdate2Finished -= VeryLateUpdate;
			}
		}


		void OnPlayerCameraDisabled()
		{
			UpdatePlayerEyeTransformReferences();
			
			if ( IsLookingAtPlayer() && thirdPersonPlayerEyeCenter == null )
				LookAtPlayer(nextChangePOITime);
		}
		
		
		void OnPlayerEyesParentDestroyed(DestroyNotifier destroyNotifier)
		{
			if ( destroyNotifier.gameObject != createdVRParentGO )
			{
				Debug.LogWarning("Received OnPlayerEyesParentDestroyed from unknown gameObject " + destroyNotifier, destroyNotifier.gameObject);

				return;
			}

			createdVRParentGO = null;
			createdPlayerEyeCenterGO = null;
			createdPlayerLeftEyeGO = null;
			createdPlayerRightEyeGO = null;

			isInitialized = false;
			Initialize();
		}


		void OnTargetDestroyed()
		{
			OnTargetLost();
		}


		void OnTargetLost()
		{
			float r = Random.value;
			if ( r <= lookAtPlayerRatio && IsPlayerInView() )
				LookAtPlayer(Random.Range(Mathf.Min(minLookTime, maxLookTime), Mathf.Max(minLookTime, maxLookTime)));
			else
				LookAroundIdly();
		}



		void Start()
		{
			if ( false == isInitialized )
				Initialize();
		}


		void UpdateLookTarget(bool shouldLookAwayFromPlayer=false)
		{
			if ( shouldLookAwayFromPlayer )
				LookAwayFromPlayer();
			else if ( Random.value <= lookAtPlayerRatio && IsPlayerInView() )
				LookAtPlayer(Random.Range(Mathf.Min(minLookTime, maxLookTime), Mathf.Max(minLookTime, maxLookTime)));
			else
				LookAroundIdly();
		}
		
		
		void UpdateNativeVREyePositions()
		{
			if ( mainCameraXform == null )
			{
				if ( Camera.main == null )
				{
					Debug.LogError("Main camera not found");
					return;
				}
				mainCameraXform = Camera.main.transform;
			}

			if ( false == useNativeVRSupport || false == Utils.IsVRDevicePresent() || usedThirdPersonPlayerEyeCenter != null)
			{
				playerEyeCenterXform.position = playerLeftEyeXform.position = playerRightEyeXform.position = mainCameraXform.position;
				
				return;
			}
			
			if ( mainCameraXform.parent != null)
			{
				var parent = mainCameraXform.parent;
				InputTracking.GetNodeStates(nodeStateList);
				foreach (XRNodeState nodeState in nodeStateList)
					if (nodeState.nodeType == XRNode.CenterEye)
					{
						Vector3 centerEyeLocalPos;
						Quaternion centerEyeLocalQ;
						if ( nodeState.TryGetPosition(out centerEyeLocalPos) )
							playerEyeCenterXform.position = parent.TransformPoint(centerEyeLocalPos);
						if ( nodeState.TryGetRotation(out centerEyeLocalQ) )
							playerEyeCenterXform.rotation = parent.rotation * centerEyeLocalQ;
					}
					else if (nodeState.nodeType == XRNode.LeftEye)
					{
						Vector3 leftEyeLocalPos;
						if ( nodeState.TryGetPosition(out leftEyeLocalPos) )
							playerLeftEyeXform.position = parent.TransformPoint(leftEyeLocalPos);
					}
					else if (nodeState.nodeType == XRNode.RightEye)
					{
						Vector3 rightEyeLocalPos;
						if ( nodeState.TryGetPosition(out rightEyeLocalPos) )
							playerRightEyeXform.position = parent.TransformPoint(rightEyeLocalPos);
					}
			}
			else
			{
				InputTracking.GetNodeStates(nodeStateList);
				Vector3 camLocal = Vector3.zero;
				foreach (XRNodeState nodeState in nodeStateList)
					if (nodeState.nodeType == XRNode.CenterEye)
					{
						Vector3 centerEyeLocalPos;
						Quaternion centerEyeLocalQ;
						if ( nodeState.TryGetPosition(out centerEyeLocalPos) )
							camLocal = centerEyeLocalPos;
						if ( nodeState.TryGetRotation(out centerEyeLocalQ) )
							mainCameraParentXform.rotation = mainCameraXform.rotation * Quaternion.Inverse(centerEyeLocalQ);
					}
				mainCameraParentXform.position = mainCameraXform.position - camLocal.x * mainCameraParentXform.right - camLocal.y * mainCameraParentXform.up - mainCameraParentXform.forward*camLocal.z;

				InputTracking.GetNodeStates(nodeStateList);
				foreach (XRNodeState nodeState in nodeStateList)
					if (nodeState.nodeType == XRNode.CenterEye)
					{
						Vector3 centerEyeLocalPos;
						Quaternion centerEyeLocalQ;
						if ( nodeState.TryGetPosition(out centerEyeLocalPos) )
							playerEyeCenterXform.position = mainCameraParentXform.TransformPoint(centerEyeLocalPos);
						if ( nodeState.TryGetRotation(out centerEyeLocalQ) )
							playerEyeCenterXform.rotation = mainCameraParentXform.rotation * centerEyeLocalQ;
					}
					else if (nodeState.nodeType == XRNode.LeftEye)
					{
						Vector3 leftEyeLocalPos;
						if ( nodeState.TryGetPosition(out leftEyeLocalPos) )
							playerLeftEyeXform.position = mainCameraParentXform.TransformPoint(leftEyeLocalPos);
					}
					else if (nodeState.nodeType == XRNode.RightEye)
					{
						Vector3 rightEyeLocalPos;
						if ( nodeState.TryGetPosition(out rightEyeLocalPos) )
							playerRightEyeXform.position = mainCameraParentXform.TransformPoint(rightEyeLocalPos);
					}
			}

			//*** Work around a  b u g  in SteamVR plugin that gives wrong positions for left and right eye
			// https://github.com/ValveSoftware/steamvr_unity_plugin/issues/903
			{
				float leftEyeDist = Vector3.Distance(playerEyeCenterXform.position, playerLeftEyeXform.position);
				float rightEyeDist = Vector3.Distance(playerEyeCenterXform.position, playerRightEyeXform.position);
				
				playerLeftEyeXform.position = playerEyeCenterXform.position -leftEyeDist *  playerEyeCenterXform.right;
				playerRightEyeXform.position = playerEyeCenterXform.position +rightEyeDist *  playerEyeCenterXform.right;
			}
		}


		void UpdatePlayerEyeTransformReferences()
		{
			if ( thirdPersonPlayerEyeCenter != null )
			{
				if ( Utils.IsEqualOrDescendant(transform, thirdPersonPlayerEyeCenter) )
					Debug.LogError("Player Eye Center should be part of the player character who this character is supposed to look at, not part of this character itself!");

				playerEyeCenterXform = thirdPersonPlayerEyeCenter;
				playerLeftEyeXform = playerRightEyeXform = null;
			}
			else if ( useNativeVRSupport )
			{
				playerEyeCenterXform = createdPlayerEyeCenterGO.transform;
				playerLeftEyeXform = createdPlayerLeftEyeGO.transform;
				playerRightEyeXform = createdPlayerRightEyeGO.transform;
			}
			else if ( useVR )
			{
				GameObject ovrRigGO = GameObject.Find("OVRCameraRig");
				if ( ovrRigGO != null )
				{
					playerLeftEyeXform = Utils.FindChildInHierarchy(ovrRigGO, "LeftEyeAnchor").transform;
					playerRightEyeXform = Utils.FindChildInHierarchy(ovrRigGO, "RightEyeAnchor").transform;
					playerEyeCenterXform = Utils.FindChildInHierarchy(ovrRigGO, "CenterEyeAnchor").transform;
				}
				else
				{
					playerEyeCenterXform = FindPlayer();
					playerLeftEyeXform = playerRightEyeXform = null;
				}
			}
			else
			{
				playerEyeCenterXform = FindPlayer();
				playerLeftEyeXform = playerRightEyeXform = null;
			}

			usedThirdPersonPlayerEyeCenter = thirdPersonPlayerEyeCenter;
		}


		void VeryLateUpdate()
		{
			if ( false == isInitialized )
				return;
			
			if ( thirdPersonPlayerEyeCenter == null && (playerCamera == null || playerCamera.gameObject.activeInHierarchy == false || playerCamera.enabled == false) )
				OnPlayerCameraDisabled();
			
			if ( thirdPersonPlayerEyeCenter != usedThirdPersonPlayerEyeCenter )
				UpdatePlayerEyeTransformReferences();
			
			if (useNativeVRSupport && usedThirdPersonPlayerEyeCenter == null)
				UpdateNativeVREyePositions();
				
			DistanceToPlayer = playerEyeCenterXform == null ? 0 : Vector3.Distance(eyeAndHeadAnimator.GetOwnEyeCenter(), playerEyeCenterXform.position);
			if ( lastDistanceToPlayer < 0 )
				lastDistanceToPlayer = DistanceToPlayer;
			
			if ( Time.deltaTime > 0 )
			{
				float playerApproachSpeed = (lastDistanceToPlayer-DistanceToPlayer)/Time.deltaTime;
				smoothedPlayerApproachSpeed = Mathf.Lerp(smoothedPlayerApproachSpeed, playerApproachSpeed, Time.deltaTime * 2);
			}
			bool shouldLookBackAtPlayer = false;
			bool shouldNoticePlayer = false;
			bool shouldLookAwayFromPlayer = false;

			bool isPlayerInView = IsPlayerInView();
			bool isPlayerInAwarenessZone = playerEyeCenterXform != null && isPlayerInView && DistanceToPlayer < noticePlayerDistance;
			bool isPlayerInPersonalSpace = playerEyeCenterXform != null && isPlayerInView && DistanceToPlayer < personalSpaceDistance;

			
			//*** Awareness zone
			{
				if ( playerEyeCenterXform != null )
				{
					if ( isPlayerInAwarenessZone )
					{
						if ( Time.time - timeOfLastNoticeCheck > 0.1f && state != State.LookingAtPlayer )
						{
							timeOfLastNoticeCheck = Time.time;
						
							bool isPlayerApproaching = smoothedPlayerApproachSpeed > 0;
							float closenessFactor01 = (noticePlayerDistance - DistanceToPlayer)/noticePlayerDistance;
							float noticeProbability = Mathf.Lerp (0.1f, 0.5f, closenessFactor01);
							shouldNoticePlayer = isPlayerApproaching && timeOutsideOfAwarenessZone > 1 && Random.value < noticeProbability; 
						}
					}
					else
						timeOutsideOfAwarenessZone += Time.deltaTime;
				}
			}


			//*** Personal space
			{
				if ( playerEyeCenterXform != null )
				{
					 if ( isPlayerInPersonalSpace )
					 {
						timeInsidePersonalSpace += Time.deltaTime * Mathf.Clamp01((personalSpaceDistance - DistanceToPlayer)/(0.5f * personalSpaceDistance));
						const float kMinTimeInPersonalSpaceToLookAway = 1;
						if ( timeInsidePersonalSpace >= kMinTimeInPersonalSpaceToLookAway )
							shouldLookAwayFromPlayer = true;

						if ( false == wasInPersonalSpaceLastFrame )
							OnPlayerEntersPersonalSpace.Invoke();
					 }
					 else
						timeInsidePersonalSpace = 0;

					 wasInPersonalSpaceLastFrame = isPlayerInPersonalSpace;
				}
			}


			//*** Look away from player?
			{
				if ( playerEyeCenterXform != null )
				{
					if ( shouldLookAwayFromPlayer && state != State.LookingAwayFromPlayer )
					{
						LookAwayFromPlayer();

						return;
					}
				}
			}


			//*** If the player keeps staring at us, stare back?		
			{
				if ( playerEyeCenterXform != null )
				{
					if ( stareBackFactor > 0 && playerEyeCenterXform != null )
					{
						float playerLookingAtMeAngle = eyeAndHeadAnimator.GetStareAngleTargetAtMe( playerEyeCenterXform );
						bool isPlayerLookingAtMe = IsPlayerLookingAtMe();
			
						playerLookingAtMeTime = isPlayerInView && isPlayerLookingAtMe	? Mathf.Min(10, playerLookingAtMeTime + Mathf.Cos(Mathf.Deg2Rad * playerLookingAtMeAngle) * Time.deltaTime)
																															: Mathf.Max(0, playerLookingAtMeTime - Time.deltaTime);
				
						if ( false == eyeAndHeadAnimator.IsLookingAtFace() )
						{
							if ( stareBackDeadtime > 0 )
								stareBackDeadtime -= Time.deltaTime;
							
							if (	stareBackDeadtime <= 0  &&
								Time.time - timeOfLastLookBackCheck > 0.1f &&
								playerLookingAtMeTime > minLookAtMeTimeToReact  &&
								eyeAndHeadAnimator.CanChangePointOfAttention() &&
								isPlayerLookingAtMe )
							{
								timeOfLastLookBackCheck = Time.time;
								
								float lookTimeProbability = stareBackFactor * 2 * (Mathf.Min(10, playerLookingAtMeTime) - minLookAtMeTimeToReact) / (10-minLookAtMeTimeToReact);
								shouldLookBackAtPlayer = Random.value < lookTimeProbability;
							}
						}
					}
				}
			}
			
			//*** Finished looking at current target?
			{
				if ( nextChangePOITime >= 0 && Time.time >= nextChangePOITime && eyeAndHeadAnimator.CanChangePointOfAttention() )
				{
					UpdateLookTarget(shouldLookAwayFromPlayer);
					
					return;
				}
			}
			
			if ( playerEyeCenterXform != null && ( shouldLookBackAtPlayer || shouldNoticePlayer ) )
				LookAtPlayer(Random.Range(Mathf.Min(minLookTime, maxLookTime), Mathf.Max(minLookTime, maxLookTime)));

			lastDistanceToPlayer = DistanceToPlayer;
		}
	}

}