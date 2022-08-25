using UnityEditor;
using UnityEngine;


public static class SelectMecanimHead
{

	[MenuItem("Tools/Realistic Eye Movements/Select Mecanim Head %#h")]
	static void SelectHead()
	{
		if ( Selection.transforms == null || Selection.transforms.Length == 0 )
		{
			ShowError("Please select the character you want to find the Mecanim head bone of.");
			return;
		}
		
		Transform t = Selection.transforms[0];
		
		if ( false == t.gameObject.activeInHierarchy )
		{
			ShowError("GameObject needs to be active to find the head bone.");
			return;
		}
		
		if ( t == null )
		{
			ShowError("Please select the character you want to find the Mecanim head bone of.");
			return;
		}
		
		Animator animator = t.GetComponentInChildren<Animator>();
		if ( animator == null )
		{
			animator = t.GetComponentInParent<Animator>();
			if ( animator == null )
			{
				ShowError("No Animator component found.");
				return;
			}
		}
		
		Transform head = animator.GetBoneTransform(HumanBodyBones.Head);
		if ( head == null )
		{
			ShowError("No Mecanim head bone found. Is the charter rig set to Humanoid and a head bone assigned?");
			return;
		}
		
		Selection.objects = new Object[] { head.gameObject };
	}
	
	
	static void ShowError(string errorMessage)
	{
		EditorUtility.DisplayDialog("Cannot select Mecanim head",
							"ERROR\n\n" + errorMessage,
							"Ok");
	}


}
