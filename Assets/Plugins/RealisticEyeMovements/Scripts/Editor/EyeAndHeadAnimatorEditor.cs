// EyeAndHeadAnimatorEditor.cs
// Tore Knabe
// Copyright 2020 tore.knabe@gmail.com

using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RealisticEyeMovements {

	[CanEditMultipleObjects]
	[CustomEditor( typeof (EyeAndHeadAnimator))]
	public class EyeAndHeadAnimatorEditor : Editor
	{
		#region fields
		
			GUIStyle yellowTextStyle;
			GUIStyle redTextStyle;
			
			Animator animator;
			Transform leftEyeFromAnimator;
			Transform rightEyeFromAnimator;
			EyeAndHeadAnimator eyeAndHeadAnimator;
			ControlData controlData;

			SerializedProperty mainWeightProp;
			SerializedProperty eyesWeightProp;
			SerializedProperty useMicroSaccadesProp;
			SerializedProperty useMacroSaccadesProp;
			SerializedProperty useHeadJitterProp;
			SerializedProperty headJitterFrequencyProp;
			SerializedProperty headJitterAmplitudeProp;
			SerializedProperty kDrawSightlinesInEditorProp;
			SerializedProperty updateTypeProp;
			SerializedProperty idleTargetHorizAngleProp;
			SerializedProperty crossEyeCorrectionProp;
			SerializedProperty saccadeSpeedProp;
			SerializedProperty microSaccadesPerMinuteLookingIdleProp;
			SerializedProperty microSaccadesPerMinuteLookingAtPOIProp;
			SerializedProperty macroSaccadesPerMinuteProp;
			SerializedProperty limitHeadAngleProp;
			
			SerializedProperty kMinNextBlinkTimeProp;
			SerializedProperty kMaxNextBlinkTimeProp;
			SerializedProperty blinkSpeedProp;
			SerializedProperty eyelidsFollowEyesVerticallyProp;
			
			SerializedProperty headControlProp;
			SerializedProperty headAnimationTypeProp;
			SerializedProperty headTransformProp;
			SerializedProperty neckXformProp;
			SerializedProperty spineXformProp;
			SerializedProperty headTargetProp;
			SerializedProperty headWeightProp;
			SerializedProperty headPitchAngleProp;
			SerializedProperty headRollAngleProp;
			SerializedProperty headYawAngleProp;
			SerializedProperty neckPitchAngleProp;
			SerializedProperty neckRollAngleProp;
			SerializedProperty neckYawAngleProp;
			SerializedProperty resetHeadAtFrameStartProp;
			SerializedProperty bodyWeightProp;
			SerializedProperty neckHorizWeightProp;
			SerializedProperty neckVertWeightProp;
			SerializedProperty headSpeedChangeToNewTargetProp;
			SerializedProperty headSpeedTrackTargetProp;
			SerializedProperty eyeWidenOrSquintProp;
			SerializedProperty eyeControlProp;
			SerializedProperty leftEyeProp;
			SerializedProperty rightEyeProp;
			SerializedProperty eyelidsWeightProp;
			SerializedProperty eyelidControlProp;
			SerializedProperty upperLeftEyelidBonesProp;
			SerializedProperty upperRightEyelidBonesProp;
			SerializedProperty lowerLeftEyelidBonesProp;
			SerializedProperty lowerRightEyelidBonesProp;
			SerializedProperty eyelidBoneModeProp;

    		bool showEyesFoldout;
			bool showHeadFoldout;
			bool showEyelidsFoldout;
			bool showSetupFoldout;
			
			bool canUseMecanimHeadBone;
			bool canUseMecanimNeckBone;
			bool animatorHasSpine;
			
			bool isEyeballControl;
			bool isEyeBoneControl;
			bool isEyelidBoneControl;
			bool isEyelidBlendshapeControl;
			bool isDefaultSet;
			bool isClosedSet;
			bool isLookUpSet;
			bool isLookDownSet;
			bool isLookLeftSet;
			bool isLookRightSet;
			bool isAnimatorMissing;
			bool areEyelidTransformsMissing;
			bool areEyeTransformsMissing;
			bool areEyeBonesMissing;

		#endregion
		
		
		void DrawEyelidsConfiguration()
		{
			showEyelidsFoldout = EditorGUILayout.Foldout(showEyelidsFoldout, "Eyelids", true);
			if ( showEyelidsFoldout )
			{
				EditorGUI.indentLevel++;
				
				EditorGUILayout.Slider(eyelidsWeightProp, 0, 1, new GUIContent( "Eyelids weight", "How much this component controls eyelids (Modulated by the main and eyes weights)."));
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(kMinNextBlinkTimeProp, new GUIContent("Min next blink time", "Minimum seconds until next blink"));
				EditorGUILayout.PropertyField(kMaxNextBlinkTimeProp, new GUIContent("Max next blink time", "Maximum seconds until next blink"));
				EditorGUILayout.Space();
				EditorGUILayout.Slider(blinkSpeedProp, 0.1f, 3, new GUIContent("Blink speed", "The blinking speed. Default is 1."));
				if (controlData.eyelidControl == ControlData.EyelidControl.Bones)
				{
					const string tooltip = "0: normal. 1: max widened, -1: max squint";
					EditorGUILayout.Slider(eyeWidenOrSquintProp, -1, 1, new GUIContent("Eye widen or squint", tooltip));
				}
				else
				{
					const string tooltip = "0: normal. -1: max squint";
					EditorGUILayout.Slider(eyeWidenOrSquintProp, -1, 0, new GUIContent("Eye widen or squint", tooltip));
				}
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(eyelidsFollowEyesVerticallyProp, new GUIContent("Eyelids follow eyes vertically", "Whether the eyelids move up a bit when looking up and down when looking down."));

				EditorGUI.indentLevel--;
			}
		}
		
		
		void DrawEyesConfiguration()
		{
			showEyesFoldout = EditorGUILayout.Foldout(showEyesFoldout, "Eyes", true);
			if ( showEyesFoldout )
			{
				EditorGUI.indentLevel++;
				EditorGUILayout.Slider(eyesWeightProp, 0, 1, new GUIContent( "Eyes weight", "How much this component controls eyes direction (modulated by main weight)."));
				EditorGUILayout.Space();
				EditorGUILayout.PropertyField(useMicroSaccadesProp);
				EditorGUILayout.PropertyField(useMacroSaccadesProp);
				EditorGUILayout.Space();
				EditorGUILayout.Space();
				EditorGUILayout.Slider(idleTargetHorizAngleProp, 0, 40, new GUIContent("Idle target horiz angle", "In Look Idly mode, choose next look target within this number of angles horizontally relative to looking forward."));
				EditorGUILayout.Slider(crossEyeCorrectionProp, 0, 5, new GUIContent("Cross eye correction", "Cross eye correction factor"));
				EditorGUILayout.Slider(saccadeSpeedProp, 0, 5, new GUIContent("Saccade speed", "1 is most realistic, but a slower value like 0.5 looks better for most characters."));
				EditorGUILayout.Slider(microSaccadesPerMinuteLookingIdleProp, 0, 180, new GUIContent("Micro saccades/min looking idle", "How many macro saccades are made on average per minute when looking idle."));
				EditorGUILayout.Slider(microSaccadesPerMinuteLookingAtPOIProp, 0, 180, new GUIContent("Micro saccades/min looking at POI", "How many macro saccades are made on average per minute when looking at a specific thing."));
				EditorGUILayout.Slider(macroSaccadesPerMinuteProp, 0, 120, new GUIContent("Macro saccades per min", "How many macro saccades are made on average per minute."));
				EditorGUI.indentLevel--;
			}
		}


		void DrawHeadConfiguration()
		{
			showHeadFoldout = EditorGUILayout.Foldout(showHeadFoldout, "Head", true);
			if ( showHeadFoldout )
			{
				EditorGUI.indentLevel++;
				
				const string tooltipHeadControl = "How to control head turning.";
				
				EditorGUILayout.PropertyField(headControlProp, new GUIContent("Head control", tooltipHeadControl));
				
				if ( eyeAndHeadAnimator.headComponent.headControl != HeadComponent.HeadControl.None )
				{
					if ( eyeAndHeadAnimator.headComponent.headControl == HeadComponent.HeadControl.AnimatorIK )
					{
						if ( null == animator )
							EditorGUILayout.HelpBox("No animator found", MessageType.Error);
						else if ( false == canUseMecanimHeadBone )
							EditorGUILayout.HelpBox("No head bone, check Mecanim import settings", MessageType.Error);
					}
					if ( eyeAndHeadAnimator.headComponent.headControl == HeadComponent.HeadControl.FinalIK )
					{
						#if USE_FINAL_IK || REM_USE_FINAL_IK
							if ( null == eyeAndHeadAnimator.GetComponent<RootMotion.FinalIK.LookAtIK>() )
								EditorGUILayout.HelpBox("Add a LookAtIK component to use FinalIK", MessageType.Error);
						#else
								EditorGUILayout.HelpBox("REM_USE_FINAL_IK not defined", MessageType.Error);
						#endif
					}
					if ( eyeAndHeadAnimator.headComponent.headControl == HeadComponent.HeadControl.Transform )
					{
						EditorGUILayout.PropertyField(headTransformProp, new GUIContent( "Head transform", "If you have a non-Mecanim character, assign the head bone or head gameObject here."));
						if ( eyeAndHeadAnimator.headBoneNonMecanim == null && false == canUseMecanimHeadBone )
							EditorGUILayout.LabelField("Assign a head bone or head object", redTextStyle);

						EditorGUILayout.PropertyField(neckXformProp, new GUIContent( "Neck transform", "If you have a neck bone that you want to follow the head, assign it here."));
					}
					if ( false == animatorHasSpine || eyeAndHeadAnimator.headComponent.headControl == HeadComponent.HeadControl.Transform )
					{
						EditorGUILayout.PropertyField(spineXformProp, new GUIContent( "Spine transform", "Assign an ancestor bone of the head bone to be used as reference for computing head angles."));
						if ( false == animatorHasSpine && eyeAndHeadAnimator.spineBoneNonMecanim == null )
							EditorGUILayout.LabelField("Assign a spine bone so that head angles can be computed", yellowTextStyle);
					}
					if ( eyeAndHeadAnimator.headComponent.headControl == HeadComponent.HeadControl.HeadTarget )
					{
						EditorGUILayout.PropertyField(headTargetProp, new GUIContent( "Head target", "Assing a transform that you want REM to keep positioning as head aim target. You can then use this target for head control components like animation rigging."));
						if ( eyeAndHeadAnimator.headTarget == null )
							EditorGUILayout.LabelField("Assign a head target", redTextStyle);
					}
					
					const string tooltipHeadAnimationType = "Animation method for head.";
					EditorGUILayout.PropertyField(headAnimationTypeProp, new GUIContent("Head animation type", tooltipHeadAnimationType));

					EditorGUILayout.Space();
					
					EditorGUILayout.Slider(headWeightProp, 0, 1, new GUIContent( "Head weight", "How much this component controls head direction (modulated by main weight)."));
					
					if ( eyeAndHeadAnimator.headComponent.headControl == HeadComponent.HeadControl.AnimatorIK )
						EditorGUILayout.Slider(bodyWeightProp, 0, 1, new GUIContent("Body weight", "How much this component orients the body when orienting the head."));
					if ( eyeAndHeadAnimator.headComponent.headControl == HeadComponent.HeadControl.Transform && (eyeAndHeadAnimator.neckBoneNonMecanim != null || canUseMecanimNeckBone) )
					{
						EditorGUILayout.Slider(neckHorizWeightProp, 0, 1, new GUIContent("Neck horizontal weight", "How much the neck follows the head horizontally."));
						EditorGUILayout.Slider(neckVertWeightProp, 0, 1, new GUIContent("Neck vertical weight", "How much the neck follows the head vertically."));
					}
					
					EditorGUILayout.Space();
					
					EditorGUILayout.LabelField("Additional head tilt", EditorStyles.boldLabel);
					EditorGUI.indentLevel++;
					EditorGUILayout.Slider(headPitchAngleProp, -45, 45, new GUIContent( "Pitch angle", "The pitch angle of the head (up/down)"));
					EditorGUILayout.Slider(headRollAngleProp, -45, 45, new GUIContent( "Roll angle", "The roll angle of the head (tilt around backward-forward axis like a confused dog)"));
					EditorGUILayout.Slider(headYawAngleProp, -45, 45, new GUIContent( "Yaw angle", "The yaw angle of the head (look left/right around vertical axis)"));
					EditorGUI.indentLevel--;
					
					EditorGUILayout.Space();
					
					if ( eyeAndHeadAnimator.headComponent.headControl == HeadComponent.HeadControl.Transform )
					{
						if ( eyeAndHeadAnimator.neckBoneNonMecanim != null || canUseMecanimNeckBone )
						{
							EditorGUILayout.LabelField("Additional neck tilt", EditorStyles.boldLabel);
							EditorGUI.indentLevel++;
							EditorGUILayout.Slider(neckPitchAngleProp, -45, 45, new GUIContent( "Pitch angle", "The pitch angle of the neck (up/down)"));
							EditorGUILayout.Slider(neckRollAngleProp, -45, 45, new GUIContent( "Roll angle", "The roll angle of the neck (tilt around backward-forward axis like a confused dog)"));
							EditorGUILayout.Slider(neckYawAngleProp, -45, 45, new GUIContent( "Yaw angle", "The yaw angle of the neck (look left/right around vertical axis)"));
							EditorGUI.indentLevel--;
							EditorGUILayout.Space();
						}
						EditorGUILayout.PropertyField(resetHeadAtFrameStartProp, new GUIContent("Reset head at frame start", "Check if the head's forward is ok, but he head is rotated wrongly around that forward (might occur when the head is not animated)"));
						EditorGUILayout.Space();
					}
					
					EditorGUILayout.LabelField("Head speed", EditorStyles.boldLabel);
					EditorGUI.indentLevel++;
					const string tooltipHeadSpeedSwitchTarget = "Increases or decreases head speed when switching to a new look target (1: normal)";
					EditorGUILayout.Slider(headSpeedChangeToNewTargetProp, 0.1f, 5, new GUIContent("Speed for switching target", tooltipHeadSpeedSwitchTarget));
					const string tooltipHeadSpeedTrackTarget = "Increases or decreases head speed when tracking the current look target (1: normal)";
					EditorGUILayout.Slider(headSpeedTrackTargetProp, 0.1f, 5, new GUIContent("Speed for following target", tooltipHeadSpeedTrackTarget));
					EditorGUI.indentLevel--;
					EditorGUILayout.Space();

					EditorGUILayout.LabelField("Head jitter", EditorStyles.boldLabel);
					EditorGUI.indentLevel++;
					EditorGUILayout.PropertyField(useHeadJitterProp);
					EditorGUILayout.PropertyField(headJitterFrequencyProp, new GUIContent("Jitter frequency", "The frequency of the head jitter."));
					EditorGUILayout.PropertyField(headJitterAmplitudeProp, new GUIContent("Jitter amplitude", "The amplitude of the head jitter."));
					EditorGUI.indentLevel--;
					EditorGUILayout.Space();

					EditorGUILayout.Slider(limitHeadAngleProp, 0, 1, new GUIContent("Limit head angle", "Limits the angle for the head movement"));
				}
				
				EditorGUI.indentLevel--;
			}
		}

		
		void DrawImportExportButtons()
		{
			using (new EditorGUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Export"))
				{
					string filename = EditorUtility.SaveFilePanel("Export settings", "", "REMsettings.json", "json");
					if (false == string.IsNullOrEmpty(filename))
						eyeAndHeadAnimator.ExportToFile(filename);
					GUIUtility.ExitGUI();
				}

				if (GUILayout.Button("Import"))
				{
					string filename = EditorUtility.OpenFilePanel("Import settings", "", "json");
					if (false == string.IsNullOrEmpty(filename))
					{
						if (eyeAndHeadAnimator.CanImportFromFile(filename))
						{
							eyeAndHeadAnimator.ImportFromFile(filename);
							EditorUtility.DisplayDialog("Import successful", Path.GetFileName(filename) + " imported.", "Ok");
							EditorUtility.SetDirty(target);
						}
						else
							EditorUtility.DisplayDialog("Cannot import",
								"ERROR\n\nSettings don't match target model. See console for details.", "Ok");
					}
					GUIUtility.ExitGUI();
				}

				if (GUILayout.Button("Import legacy (.dat)"))
				{
					string filename = EditorUtility.OpenFilePanel("Import settings", "", "dat");
					if (false == string.IsNullOrEmpty(filename))
					{
						if (eyeAndHeadAnimator.CanImportFromFile(filename))
						{
							eyeAndHeadAnimator.ImportFromFile(filename);
							EditorUtility.DisplayDialog("Import successful", Path.GetFileName(filename) + " imported.", "Ok");
							EditorUtility.SetDirty(target);
					}
						else
							EditorUtility.DisplayDialog("Cannot import",
								"Settings don't match target model. See console for details.", "Ok");
					}
					GUIUtility.ExitGUI();
				}
			}
		}

		
		void DrawSetupConfiguration()
		{
			showSetupFoldout = EditorGUILayout.Foldout(showSetupFoldout, "Setup", true);
			if ( showSetupFoldout )
			{
				EditorGUI.indentLevel++;

				EditorGUILayout.PropertyField(updateTypeProp);

				EditorGUILayout.PropertyField(eyeControlProp, new GUIContent("Eye control", "How the eyes are controlled."));

				//*** For eyeball control, slots to assign eye objects
				{
					if ( controlData.eyeControl == ControlData.EyeControl.SelectedObjects )
					{
						EditorGUILayout.PropertyField(leftEyeProp, new GUIContent("Left eye"));
						EditorGUILayout.PropertyField(rightEyeProp, new GUIContent("Right eye"));
					}
				}

				//*** Error message if any data for eye control is missing
				{
					bool somethingIsMissing = false;

					if ( isEyeBoneControl || isEyeballControl )
					{
						if ( areEyeTransformsMissing )
						{
							EditorGUILayout.HelpBox("The eyeballs need to be assigned.", MessageType.Error);
							somethingIsMissing = true;
						}
						
						if ( isAnimatorMissing )
						{
							EditorGUILayout.HelpBox("No Animator found.", MessageType.Error);
							somethingIsMissing = true;
						}
						
						if ( areEyeBonesMissing )
						{
							EditorGUILayout.HelpBox("Eye bones not found; is the Mecanim rig set up correctly?", MessageType.Error);
							somethingIsMissing = true;
						}

						if ( somethingIsMissing )
							return;
					}
					else
						return;
				}

				EditorGUILayout.PropertyField(eyelidControlProp, new GUIContent("Eyelid control"));

				//*** Eyelid bone control: assign transforms for the four bones
				{
					if ( controlData.eyelidControl == ControlData.EyelidControl.Bones )
					{
						EditorGUILayout.PropertyField(upperLeftEyelidBonesProp, true);
						EditorGUILayout.PropertyField(upperRightEyelidBonesProp, true);
						EditorGUILayout.PropertyField(lowerLeftEyelidBonesProp, true);
						EditorGUILayout.PropertyField(lowerRightEyelidBonesProp, true);
					}
				}


				//*** Error message if eyelid transforms are missing
				{
					if ( areEyelidTransformsMissing )
					{
						EditorGUILayout.HelpBox("At least the upper eyelid bones need to be assigned", MessageType.Error);

						return;
					}
				}

				//*** Error message if only one of the lower eyelids is assigned
				{
					if ( isEyelidBoneControl && controlData.upperLeftEyelidBones.Count == 0 != ( controlData.upperRightEyelidBones.Count == 0))
					{
						EditorGUILayout.HelpBox("Only one of the lower eyelid bones is assigned", MessageType.Error);

						return;
					}
				}

				if ( isEyelidBoneControl )
					EditorGUILayout.PropertyField(eyelidBoneModeProp, new GUIContent("Eyelid bone mode"));

				//*** Buttons to select eyes or eyelids
				{
					GUILayout.Space(10);

					EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField(new GUIContent("Select:"), GUILayout.Width(80));

						if (GUILayout.Button("Eyes"))
						{ 
							Object[] newSelection = new Object[2];
							
					        newSelection[0] = isEyeballControl ? controlData.leftEye.gameObject : leftEyeFromAnimator.gameObject;
					        newSelection[1] = isEyeballControl ? controlData.rightEye.gameObject : rightEyeFromAnimator.gameObject;

							Selection.objects = newSelection;
						}
						
						if ( isEyelidBoneControl )
						{
							if (GUILayout.Button("Upper eyelids"))
							{ 
								int numLeftEyelidBones = controlData.upperLeftEyelidBones.Count;
								int numRightEyelidBones = controlData.upperRightEyelidBones.Count;
								int numEyelidBones = numLeftEyelidBones + numRightEyelidBones;
								Object[] newSelection = new Object[numEyelidBones];
								
								for (int i=0;  i<numLeftEyelidBones;  i++ )
									newSelection[i] = controlData.upperLeftEyelidBones[i].gameObject;
								for (int i=0;  i<numRightEyelidBones;  i++ )
									newSelection[numLeftEyelidBones + i] = controlData.upperRightEyelidBones[i].gameObject;

								Selection.objects = newSelection;
							}
							if (controlData.lowerLeftEyelidBones.Count > 0 && controlData.lowerRightEyelidBones.Count > 0 && GUILayout.Button("Lower eyelids"))
							{ 
								int numLeftEyelidBones = controlData.lowerLeftEyelidBones.Count;
								int numRightEyelidBones = controlData.lowerRightEyelidBones.Count;
								int numEyelidBones = numLeftEyelidBones + numRightEyelidBones;
								Object[] newSelection = new Object[numEyelidBones];
								
								for (int i=0;  i<numLeftEyelidBones;  i++ )
									newSelection[i] = controlData.lowerLeftEyelidBones[i].gameObject;
								for (int i=0;  i<numRightEyelidBones;  i++ )
									newSelection[numLeftEyelidBones + i] = controlData.lowerRightEyelidBones[i].gameObject;

								Selection.objects = newSelection;
							}
					}
							
					EditorGUILayout.EndHorizontal();
				}
				
				GUILayout.Space(10);
			

				//*** Default eye opening
				{
					EditorGUILayout.BeginHorizontal();
						EditorGUILayout.LabelField("Eyes open, looking straight");
						if ( GUILayout.Button("Save") )
							controlData.SaveDefault( eyeAndHeadAnimator );

						if ( isDefaultSet )
						{
							if ( GUILayout.Button( "Load") )
								controlData.RestoreDefault();
						}
						else
							EditorGUILayout.HelpBox("Not saved yet", MessageType.Error);
					EditorGUILayout.EndHorizontal();
				}

				if ( isDefaultSet )
				{
					//*** Closed
					{
						if ( isEyelidBoneControl || isEyelidBlendshapeControl )
						{
							EditorGUILayout.BeginHorizontal();
								EditorGUILayout.LabelField("Eyes closed, looking straight");
								if ( GUILayout.Button("Save") )
									controlData.SaveClosed( eyeAndHeadAnimator );

								if ( isClosedSet )
								{
									if ( GUILayout.Button("Load") )
										controlData.RestoreClosed();
								}
								else
									EditorGUILayout.HelpBox("Not saved yet", MessageType.Error);
								EditorGUILayout.EndHorizontal();
						}
					}

					//*** Looking up
					{
						EditorGUILayout.BeginHorizontal();

									string tooltip = "Rotate " + (isEyeBoneControl ? "eyebones" : "eyes") + " to look up maximally";
									if ( isEyelidBoneControl || isEyelidBlendshapeControl )
										tooltip += ", and adjust eyelid " + (isEyelidBoneControl ? "bone rotation" : "blendshapes") + " for that position";
									EditorGUILayout.LabelField(new GUIContent("Looking up", tooltip));
									if ( GUILayout.Button("Save") )
										controlData.SaveLookUp( eyeAndHeadAnimator );

									if ( isLookUpSet )
									{
										if ( GUILayout.Button("Load") )
											controlData.RestoreLookUp();
									}
									else
										EditorGUILayout.HelpBox("Not saved yet", MessageType.Error);
									
						EditorGUILayout.EndHorizontal();
					}

					//*** Looking down
					{
						EditorGUILayout.BeginHorizontal();

									string tooltip = "Rotate " + (isEyeBoneControl ? "eyebones" : "eyes") + " to look down maximally";
									if ( isEyelidBoneControl || isEyelidBlendshapeControl )
										tooltip += ", and adjust eyelid " + (isEyelidBoneControl ? "bone rotation" : "blendshapes") + " for that position";
									EditorGUILayout.LabelField(new GUIContent("Looking down", tooltip));
									if ( GUILayout.Button("Save") )
										controlData.SaveLookDown( eyeAndHeadAnimator );

									if (isLookDownSet)
									{
										if (GUILayout.Button("Load"))
											controlData.RestoreLookDown();
									}
									else
										EditorGUILayout.HelpBox("Not saved yet", MessageType.Error);

						EditorGUILayout.EndHorizontal();
					}
					
					//*** Looking left
					{
						EditorGUILayout.BeginHorizontal();

								string tooltip = "Rotate " + (isEyeBoneControl ? "eyebones" : "eyes") + " to look left maximally";
								if ( isEyelidBoneControl || isEyelidBlendshapeControl )
									tooltip += ", and adjust eyelid " + (isEyelidBoneControl ? "bone rotation" : "blendshapes") + " for that position";
								EditorGUILayout.LabelField(new GUIContent("Looking left (optional)", tooltip));
								if ( GUILayout.Button("Save") )
									controlData.SaveLookLeft( eyeAndHeadAnimator );

								if ( isLookLeftSet )
								{
									if ( GUILayout.Button("Load") )
										controlData.RestoreLookLeft();
								}
								
						EditorGUILayout.EndHorizontal();
					}

					//*** Looking right
					{
						EditorGUILayout.BeginHorizontal();

								string tooltip = "Rotate " + (isEyeBoneControl ? "eyebones" : "eyes") + " to look right maximally";
								if ( isEyelidBoneControl || isEyelidBlendshapeControl )
									tooltip += ", and adjust eyelid " + (isEyelidBoneControl ? "bone rotation" : "blendshapes") + " for that position";
								EditorGUILayout.LabelField(new GUIContent("Looking right (optional)", tooltip));
								if ( GUILayout.Button("Save") )
									controlData.SaveLookRight( eyeAndHeadAnimator );

								if ( isLookRightSet )
								{
									if ( GUILayout.Button("Load") )
										controlData.RestoreLookRight();
								}
								
						EditorGUILayout.EndHorizontal();
					}
				}
					
				EditorGUI.indentLevel--;
			}
			
			if ( false == isDefaultSet || false == isClosedSet || false == isLookDownSet || false == isLookUpSet || isAnimatorMissing || areEyeBonesMissing || areEyelidTransformsMissing || areEyeBonesMissing )
				if ( eyeAndHeadAnimator.gameObject.activeInHierarchy )
					EditorGUILayout.LabelField("Please complete setup.", redTextStyle);
		}
			

		void InitVariables()
		{
			eyeAndHeadAnimator = (EyeAndHeadAnimator) target;
			controlData = eyeAndHeadAnimator.controlData;
			animator = eyeAndHeadAnimator.GetComponent<Animator>();
			if ( animator != null )
			{
				leftEyeFromAnimator = animator.GetBoneTransform(HumanBodyBones.LeftEye);
				rightEyeFromAnimator = animator.GetBoneTransform(HumanBodyBones.RightEye);
			}		
			
			canUseMecanimHeadBone = animator != null && animator.GetBoneTransform(HumanBodyBones.Head) != null;
			canUseMecanimNeckBone = animator != null && animator.GetBoneTransform(HumanBodyBones.Neck) != null;
			animatorHasSpine = animator != null && Utils.GetSpineBoneFromAnimator(animator) != null;
		}
		
		
		void OnEnable()
		{
			mainWeightProp = serializedObject.FindProperty("mainWeight");
			eyesWeightProp = serializedObject.FindProperty("eyesWeight");
			useMicroSaccadesProp = serializedObject.FindProperty("useMicroSaccades");
			useMacroSaccadesProp = serializedObject.FindProperty("useMacroSaccades");
			useHeadJitterProp = serializedObject.FindProperty("useHeadJitter");
			headJitterFrequencyProp = serializedObject.FindProperty("headJitterFrequency");
			headJitterAmplitudeProp = serializedObject.FindProperty("headJitterAmplitude");
			kDrawSightlinesInEditorProp = serializedObject.FindProperty("kDrawSightlinesInEditor");
			updateTypeProp = serializedObject.FindProperty("updateType");
			idleTargetHorizAngleProp = serializedObject.FindProperty("idleTargetHorizAngle");
			crossEyeCorrectionProp = serializedObject.FindProperty("crossEyeCorrection");
			saccadeSpeedProp = serializedObject.FindProperty("saccadeSpeed");
			microSaccadesPerMinuteLookingIdleProp = serializedObject.FindProperty("microSaccadesPerMinuteLookingIdle");
			microSaccadesPerMinuteLookingAtPOIProp = serializedObject.FindProperty("microSaccadesPerMinuteLookingAtPOI");
			macroSaccadesPerMinuteProp = serializedObject.FindProperty("macroSaccadesPerMinute");
			
			limitHeadAngleProp = serializedObject.FindProperty("limitHeadAngle");
			
			kMinNextBlinkTimeProp = serializedObject.FindProperty("kMinNextBlinkTime");
			kMaxNextBlinkTimeProp = serializedObject.FindProperty("kMaxNextBlinkTime");
			blinkSpeedProp = serializedObject.FindProperty("blinkSpeed");
			eyelidsFollowEyesVerticallyProp = serializedObject.FindProperty("eyelidsFollowEyesVertically");
			eyelidsWeightProp = serializedObject.FindProperty("eyelidsWeight");
			
			headTransformProp = serializedObject.FindProperty("headBoneNonMecanim");
			neckXformProp = serializedObject.FindProperty("neckBoneNonMecanim");
			spineXformProp = serializedObject.FindProperty("spineBoneNonMecanim");
			headTargetProp = serializedObject.FindProperty("headTarget");
			headWeightProp = serializedObject.FindProperty("headWeight");
			bodyWeightProp = serializedObject.FindProperty("bodyWeight");
			neckHorizWeightProp = serializedObject.FindProperty("neckHorizWeight");
			neckVertWeightProp = serializedObject.FindProperty("neckVertWeight");
			headPitchAngleProp = serializedObject.FindProperty("headPitchAngle");
			headRollAngleProp = serializedObject.FindProperty("headRollAngle");
			headYawAngleProp = serializedObject.FindProperty("headYawAngle");
			neckPitchAngleProp = serializedObject.FindProperty("neckPitchAngle");
			neckRollAngleProp = serializedObject.FindProperty("neckRollAngle");
			neckYawAngleProp = serializedObject.FindProperty("neckYawAngle");
			resetHeadAtFrameStartProp = serializedObject.FindProperty("resetHeadAtFrameStart");
			headSpeedChangeToNewTargetProp = serializedObject.FindProperty("headChangeToNewTargetSpeed");
			headSpeedTrackTargetProp = serializedObject.FindProperty("headTrackTargetSpeed");
			
			SerializedProperty headComponentProp = serializedObject.FindProperty("headComponent");
			headControlProp = headComponentProp.FindPropertyRelative("headControl");
			headAnimationTypeProp = headComponentProp.FindPropertyRelative("headAnimationType");
			
			SerializedProperty controlDataProp = serializedObject.FindProperty("controlData");

			eyeWidenOrSquintProp = controlDataProp.FindPropertyRelative("eyeWidenOrSquint");
			eyeControlProp = controlDataProp.FindPropertyRelative("eyeControl");
			leftEyeProp = controlDataProp.FindPropertyRelative("leftEye");
			rightEyeProp = controlDataProp.FindPropertyRelative("rightEye");
			eyelidControlProp = controlDataProp.FindPropertyRelative("eyelidControl");
			eyelidBoneModeProp = controlDataProp.FindPropertyRelative("eyelidBoneMode");
			
			upperLeftEyelidBonesProp = controlDataProp.FindPropertyRelative("upperLeftEyelidBones");
			upperRightEyelidBonesProp = controlDataProp.FindPropertyRelative("upperRightEyelidBones");
			lowerLeftEyelidBonesProp = controlDataProp.FindPropertyRelative("lowerLeftEyelidBones");
			lowerRightEyelidBonesProp = controlDataProp.FindPropertyRelative("lowerRightEyelidBones");
			
			InitVariables();
			
			eyeAndHeadAnimator.ConvertLegacyIfNecessary();
			controlData.ConvertLegacyIfNecessary();
			
			controlData.StoreSetup();
		}

		
		public override void OnInspectorGUI()
		{
			if ( yellowTextStyle == null )
				yellowTextStyle = new GUIStyle (GUI.skin.label) {normal = {textColor = Color.yellow}};
			if ( redTextStyle == null )
				redTextStyle = new GUIStyle (GUI.skin.label) {normal = {textColor = Color.red}};

			serializedObject.Update();

			EditorGUI.indentLevel = 0;
			
			EditorGUILayout.PropertyField(kDrawSightlinesInEditorProp);
			EditorGUILayout.Slider(mainWeightProp, 0, 1, new GUIContent( "Main weight", "How much this component controls eyes, eyelids and head (Modulated by the other weight sliders like eyelid weight)."));
			
			UpdateSetupStateVariables();

			DrawEyesConfiguration();
			DrawEyelidsConfiguration();
			DrawHeadConfiguration();
			DrawSetupConfiguration();
			DrawImportExportButtons();

			serializedObject.ApplyModifiedProperties ();
		}

		
		void UpdateSetupStateVariables()
		{
			if ( animator == null )
				InitVariables();
			
			isEyeballControl = controlData.eyeControl == ControlData.EyeControl.SelectedObjects;
			isEyeBoneControl = controlData.eyeControl == ControlData.EyeControl.MecanimEyeBones;
			isEyelidBoneControl = controlData.eyelidControl == ControlData.EyelidControl.Bones;
			isEyelidBlendshapeControl = controlData.eyelidControl == ControlData.EyelidControl.Blendshapes;

			isAnimatorMissing = isEyeBoneControl && animator == null;
			areEyeTransformsMissing = isEyeballControl && ( controlData.leftEye == null || controlData.rightEye == null );
			areEyeBonesMissing = isEyeBoneControl && ( isAnimatorMissing || null == leftEyeFromAnimator || null == rightEyeFromAnimator );
			areEyelidTransformsMissing = isEyelidBoneControl && (controlData.upperLeftEyelidBones.Count == 0 || controlData.upperRightEyelidBones.Count == 0 );
			
			isDefaultSet = true;
					if ( isEyeballControl )
						isDefaultSet &= controlData.isEyeBallDefaultSet;
					if ( isEyeBoneControl )
						isDefaultSet &= controlData.isEyeBoneDefaultSet;
					if ( isEyelidBoneControl )
						isDefaultSet &= controlData.isEyelidBonesDefaultSet;
					if ( isEyelidBlendshapeControl )
						isDefaultSet &= controlData.isEyelidBlendshapeDefaultSet;
					
			isClosedSet = true;
					if ( isEyelidBoneControl )
						isClosedSet &= controlData.isEyelidBonesClosedSet;
					if ( isEyelidBlendshapeControl )
						isClosedSet &= controlData.isEyelidBlendshapeClosedSet;

			isLookUpSet = true;
						if ( isEyeballControl )
							isLookUpSet &= controlData.isEyeBallLookUpSet;
						if ( isEyeBoneControl )
							isLookUpSet &= controlData.isEyeBoneLookUpSet;
						if ( isEyelidBoneControl )
							isLookUpSet &= controlData.isEyelidBonesLookUpSet;
						if ( isEyelidBlendshapeControl )
							isLookUpSet &= controlData.isEyelidBlendshapeLookUpSet;
						
			isLookDownSet = true;
							if ( isEyeballControl )
								isLookDownSet &= controlData.isEyeBallLookDownSet;
							if ( isEyeBoneControl )
								isLookDownSet &= controlData.isEyeBoneLookDownSet;
							if ( isEyelidBoneControl )
								isLookDownSet &= controlData.isEyelidBonesLookDownSet;
							if ( isEyelidBlendshapeControl )
								isLookDownSet &= controlData.isEyelidBlendshapeLookDownSet;
							
			isLookLeftSet = true;
						if ( isEyeballControl )
							isLookLeftSet &= controlData.isEyeBallLookLeftSet;
						if ( isEyeBoneControl )
							isLookLeftSet &= controlData.isEyeBoneLookLeftSet;
						if ( isEyelidBlendshapeControl )
							isLookLeftSet &= controlData.isEyelidBlendshapeLookLeftSet;
						
			isLookRightSet = true;
						if ( isEyeballControl )
							isLookRightSet &= controlData.isEyeBallLookRightSet;
						if ( isEyeBoneControl )
							isLookRightSet &= controlData.isEyeBoneLookRightSet;
						if ( isEyelidBlendshapeControl )
							isLookRightSet &= controlData.isEyelidBlendshapeLookRightSet;
		}
		
	}

}
