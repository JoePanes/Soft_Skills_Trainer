using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.Events;
using Oculus.Voice.Demo.UIShapesDemo;
using UnityEngine.UI;
using TMPro;

public class OptionController : MonoBehaviour
{
    public InteractionHandler InteractionHandler;

    public LearningResponse LearningResponse;

    public GameObject[] Options;

    public int OptionToSelect, tagNumber;

    public bool optionSelected;

    public UnityEvent OptionSelected;

    private ConversationController ConversationController;

    void Awake()
    {
        ConversationController = GameObject.Find("ConversationController").GetComponent<ConversationController>();
    }

    public IEnumerator GatherOptions()
    {
        tagNumber = 0;
        yield return new WaitForSeconds(0.5f);
        Options = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            Options[i] = transform.GetChild(i).gameObject;
            Options[i].GetComponent<ColourSelect>().NeutralSelect();
            Options[i].tag = tagNumber.ToString();
            tagNumber++;
        }
        tagNumber = 0;
        Debug.LogError(Options.Length + " available options listed");
    }

    public void OptionOneSelect()
    {

        Options[0].GetComponent<ColourSelect>().PositiveSelect();
        OptionToSelect = 0;
        LearningResponse.optionSelected = 1;
        StartCoroutine(OptionSelectCoroutine());
        Debug.LogError("Option 1 selected");
    }

    public void OptionTwoSelect()
    {
        Options[1].GetComponent<ColourSelect>().NeutralSelect();
        OptionToSelect = 1;
        LearningResponse.optionSelected = 2;
        StartCoroutine(OptionSelectCoroutine());
        Debug.LogError("Option 2 selected");
    }

    public void OptionThreeSelect()
    {
        Options[2].GetComponent<ColourSelect>().NegativeSelect();
        OptionToSelect = 2;
        LearningResponse.optionSelected = 3;
        StartCoroutine(OptionSelectCoroutine());
        Debug.LogError("Option 3 selected");
    }

    public IEnumerator OptionSelectCoroutine()
    {
        optionSelected = true;
        Options[OptionToSelect].GetComponent<OptionView>().InvokeOptionSelected();
        Options[OptionToSelect].GetComponent<Animator>().Play("Selected");
        InteractionHandler.CancelVoiceAttempt();

        //Let virtual humans know that the user has finished talking
        ConversationController.SomeoneIsNotTalking();

        if (ConversationController.NPC_TEXT_MODE == 2)
        {
            ConversationController.AddToTranscript("User", Options[OptionToSelect].GetComponentInChildren<TextMeshProUGUI>().text);
            Debug.Log("Added user's choice to transcript");
        }

        yield return new WaitForSeconds(2f);
        for (int i = 0; i < transform.childCount; i++)
        {
            Options[i].GetComponent<ColourSelect>().NeutralSelect();
        }
        // OptionSelected.Invoke();
        //yield return new WaitForSeconds(1f);
        //LineView.OnContinueClicked();
    }



    //public void CheckIfOptionSelected()
    //{
    //    if (optionSelected)
    //    {
    //        optionSelected = false;
    //    }
    //    else
    //    {
    //        ClassroomSpeechToYarn.ActivateVoiceRecognition();
    //    }
    //}

}
