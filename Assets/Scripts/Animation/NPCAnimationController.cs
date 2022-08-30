using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCAnimationController : MonoBehaviour
{
    private Animator animControl;
    
    private int numberOfIdleAnimations = 5;

    private int numberOfTalkingAnimations = 3;

    private int numberOfListeningAnimations = 3;

    private ConversationController ConversationController;

    // Start is called before the first frame update
    void Start()
    {
        animControl = GetComponent<Animator>();
        
        InvokeRepeating("SelectIdleAnimation", 0.0f, 10.0f);
        InvokeRepeating("SelectListeningAnimation", 0.0f, 3.0f);
        InvokeRepeating("SelectTalkingAnimation", 0.0f, 10.0f);

        ConversationController = GameObject.Find("ConversationController").GetComponent<ConversationController>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (ConversationController.someoneTalking)
        {
            if (animControl.GetBool("is_talking") == false)
            {
                if (animControl.GetBool("is_listening") == false)
                {
                    ToggleListening();
                }
            }
        } else
        {
            if (animControl.GetBool("is_listening"))
            {
                ToggleListening();
            }
        }
         
    }

    //Select one of the potential idle animation
    void SelectIdleAnimation()
    {
        animControl.SetFloat("idle", Random.Range(1, numberOfIdleAnimations + 1));
    }

    public void ToggleTalking()
    {
        if (animControl.GetBool("is_talking"))
        {
            animControl.SetBool("is_talking", false);

        } else 
        {
            //Select animation prior to toggles
            animControl.SetBool("is_talking", true);
        }
    }

    void SelectTalkingAnimation()
    {
        animControl.SetFloat("talk", Random.Range(1, numberOfTalkingAnimations + 1));
    }

    void ToggleListening()
    {
        if (animControl.GetBool("is_listening"))
        {
            animControl.SetBool("is_listening", false);
        }
        else
        {
            //Select animation prior to toggles
            animControl.SetBool("is_listening", true);
        }
    }

    void SelectListeningAnimation()
    {
        animControl.SetFloat("listen", Random.Range(1, numberOfListeningAnimations + 1));
    }

    public void UpdateEmotion(string sentiment)
    {
        sentiment = sentiment.ToLower();
        switch (sentiment)
        {
            case "positive":
                animControl.SetBool("positive", true);
                animControl.SetBool("aggressive", false);
                animControl.SetBool("negative", false);
                break;
            case "aggressive":
                animControl.SetBool("positive", false);
                animControl.SetBool("aggressive", true);
                animControl.SetBool("negative", false);
                break;
            case "negative":
                animControl.SetBool("positive", false);
                animControl.SetBool("aggressive", false);
                animControl.SetBool("negative", true);
                break;
            case "neutal":
                animControl.SetBool("positive", false);
                animControl.SetBool("aggressive", false);
                animControl.SetBool("negative", false);
                break;
            default:
                animControl.SetBool("positive", false);
                animControl.SetBool("aggressive", false);
                animControl.SetBool("negative", false);
                break;
        }

    }
}
