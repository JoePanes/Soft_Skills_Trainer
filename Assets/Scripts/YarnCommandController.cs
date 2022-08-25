using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using UnityEngine.Events;
using Oculus.Voice.Demo.UIShapesDemo;
using Oculus.Voice;


public class YarnCommandController : MonoBehaviour
{
    [Header("Voice")]
    public InteractionHandler interactionHandler;
    public GameObject indicator;
    public OptionController OptionController;
    public UnityEvent PlayerFinishedTalking;

    [Header("Yarn")]
    public InMemoryVariableStorage yarnInMemoryVariableStorage;
    public CharacterYarnLineHandler[] characterYarnLineHandlers;
    public LearningResponse LearningResponse;
    private string characterToTalk;
    public int numberOfScoreOpportunities, score;
    private float scorePercentCheck;
    //public LineManager LineManager;

    [Header("SceneChange")]
    public OVRScreenFade OVRScreenFade;
    //public float sceneNumber;
    public GameObject[] charactersToSwitch;
    //public GameObject[] teacher;

    private GameObject ConversationController;

    private void Awake()
    {
        ConversationController = GameObject.Find("ConversationController");

        indicator.SetActive(false);

        for (int i = 0; i < charactersToSwitch.Length; i++)
        {
            charactersToSwitch[i].SetActive(false);
        }
        //for (int i = 0; i < teacher.Length; i++)
        //{
        //    teacher[i].SetActive(true);
        //}

        for (int i = 0; i < characterYarnLineHandlers.Length ; i++ )                             //checks all characters in array and makes sure tags are set to the character name
        {
            characterYarnLineHandlers[i].tag = characterYarnLineHandlers[i].characterName;
            //Debug.LogError(characterYarnLineHandlers[i].tag);
        }
    }

    [YarnCommand("activate_voice_recognition")]                                                 //YARN command to activate Wit.AI voice recognition and gather available options
    public void ActivateVoiceRecognition()
    {

        //Let virtual humans know that the user is talking
        ConversationController.GetComponent<ConversationController>().SomeoneIsTalking();

        //Debug.LogError("Attempting voice recog...");
        StartCoroutine(OptionController.GatherOptions());
        indicator.SetActive(true);
        interactionHandler.YarnVoiceAttempt();
            //interactionHandler.ToggleActivation();
        //Debug.LogError("Attempt voice recog activation complete");
    }


    [YarnCommand("NPC_start_talking")]                                                          //YARN command retrieving current character set in YARN, finding the relevant character and activating its audio playback
    public void NPCStartToTalk()
    {
        yarnInMemoryVariableStorage.TryGetValue("$characterToTalk", out characterToTalk);
        Debug.LogError(characterToTalk + " is set to talk.");
        for (int i = 0; i < characterYarnLineHandlers.Length; i++)
        {
            if(characterYarnLineHandlers[i].tag == characterToTalk)
            {
                Debug.LogError(characterToTalk + " speech playback called.");

                characterYarnLineHandlers[i].CharacterSpeechPlayback();
            }
        }
    }

    [YarnCommand("NPC_learning_response")]                                                          //YARN command retrieving current character set in YARN, finding the relevant character and activating its audio playback
    public void NPCLearningResponse()
    {
        LearningResponse.SetupLearningResponse();
        yarnInMemoryVariableStorage.TryGetValue("$characterToTalk", out characterToTalk);
        //Debug.LogError(characterToTalk + " is set to talk.");
        for (int i = 0; i < characterYarnLineHandlers.Length; i++)
        {
            if (characterYarnLineHandlers[i].tag == characterToTalk)
            {
                characterYarnLineHandlers[i].CharacterSpeechPlayback();
            }
        }
    }


    [YarnCommand("NPC_finish_talking_wait")]                                                    //YARN command retrieving the current character and waiting for it's 'finishedTalking' event to trigger
    public IEnumerator NPCWaitFinishTalking()
    {
        for (int i = 0; i < characterYarnLineHandlers.Length; i++)
        {
            if (characterYarnLineHandlers[i].tag == characterToTalk)
            {
                var trigger = false;
                System.Action action = () => trigger = true;
                characterYarnLineHandlers[i].characterFinishedTalking.AddListener(action.Invoke);
                yield return new WaitUntil(() => trigger);
                characterYarnLineHandlers[i].characterFinishedTalking.RemoveListener(action.Invoke);
            }
        }
    }

    [YarnCommand("NPC_score_response")]                                                          //YARN command retrieving current character set in YARN, finding the relevant character and activating its audio playback
    public void NPCScoreResponse()
    {

        if (scorePercentCheck >= 80)
        {
            LearningResponse.GoodScore();
        }
        else if(scorePercentCheck > 50 && scorePercentCheck < 80)
        {
            LearningResponse.MiddleScore();
        }
        else if(scorePercentCheck <= 50)
        {
            LearningResponse.BadScore();
        }
        yarnInMemoryVariableStorage.TryGetValue("$characterToTalk", out characterToTalk);
        //Debug.LogError(characterToTalk + " is set to talk.");
        for (int i = 0; i < characterYarnLineHandlers.Length; i++)
        {
            if (characterYarnLineHandlers[i].tag == characterToTalk)
            {
                characterYarnLineHandlers[i].CharacterSpeechPlayback();
            }
        }
    }

    [YarnCommand("add_positive_score")]                                                    //YARN command retrieving the current character and waiting for it's 'finishedTalking' event to trigger
    public void AddPositiveScore()
    {
        score = score + 2;
    }

    [YarnCommand("add_neutral_score")]                                                    //YARN command retrieving the current character and waiting for it's 'finishedTalking' event to trigger
    public void AddNeutralScore()
    {
        score = score + 1;
    }

    [YarnCommand("calculate_score")]                                                    //YARN command retrieving the current character and waiting for it's 'finishedTalking' event to trigger
    public void CalculateScore()
    {
        float maxScore = (numberOfScoreOpportunities * 2);
        float scoreFraction = (score / maxScore);
        float scorePercent = (scoreFraction * 100);

        yarnInMemoryVariableStorage.SetValue("$scorePercent", scorePercent);

        Debug.LogError("Max score: " + maxScore);
        Debug.LogError("Score fraction: " + scoreFraction);
        Debug.LogError("Score percent: " + scorePercent);
        
    }

    //[YarnCommand("scene_change")]
    //public void SceneChange()
    //{
    //    StartCoroutine(SwitchCharacters());
    //}

    [YarnCommand ("Quit_Application")]                                                          //YARN command to enter the application Quit procedure
    public void CloseApplication()
    {
        OVRScreenFade.FadeOut();
        Application.Quit();
    }


    [YarnCommand("switch_characters")]                                                          //YARN command to enter the application Quit procedure
    public void SwitchCharacters()
    {
        StartCoroutine(CharacterSwitch());
    }


    public IEnumerator CharacterSwitch()
    {
        OVRScreenFade.FadeOut();
        yield return new WaitForSeconds(1f);
        bool enter;
        yarnInMemoryVariableStorage.TryGetValue("$bringInCharacters", out enter);
        if (enter)
        {
            for (int i = 0; i < charactersToSwitch.Length; i++)
            {
                charactersToSwitch[i].SetActive(true);
            }
        }
        if (!enter)
        {
            for (int i = 0; i < charactersToSwitch.Length; i++)
            {
                charactersToSwitch[i].SetActive(false);
            }
        }
        OVRScreenFade.FadeIn();
    }

    //public IEnumerator SwitchCharacters()
    //{
    //    OVRScreenFade.FadeOut();
    //    yield return new WaitForSeconds(1f);
    //    yarnInMemoryVariableStorage.TryGetValue("$sceneNumber", out sceneNumber);
    //    if (sceneNumber == 1)
    //    {
    //        for (int i = 0; i < children.Length; i++)
    //        {
    //            children[i].SetActive(true);
    //        }
    //        for (int i = 0; i < teacher.Length; i++)
    //        {
    //            teacher[i].SetActive(false);
    //        }
    //    }
    //    if (sceneNumber == 2)
    //    {
    //        for (int i = 0; i < children.Length; i++)
    //        {
    //            children[i].SetActive(false);
    //        }
    //        for (int i = 0; i < teacher.Length; i++)
    //        {
    //            teacher[i].SetActive(true);
    //        }
    //    }
    //    OVRScreenFade.FadeIn();
    //}

    //[YarnCommand("wait_for_speech_recog")]                                                   //yarn command to wait for player to finish talking - for YARN line view mode
    //public IEnumerator CustomWait()
    //{
    //    //Debug.Log("Coroutine started...");
    //    var trigger = false;
    //    System.Action action = () => trigger = true;
    //    PlayerFinishedTalking.AddListener(action.Invoke);
    //    yield return new WaitUntil(() => trigger);
    //    PlayerFinishedTalking.RemoveListener(action.Invoke);
    //    //Debug.Log("Coroutine finished!!");
    //}

    //[YarnCommand("girl_talk")]                          //OLD TEST COMMANDS - yarn command retrieving required voiceline number from YARN and setting clip and initiating Audio in VOLineController
    //public void GirlToTalk()
    //{
    //    LineManager.Char1SpeechPlayback();
    //}

    //[YarnCommand("char1_finish_talking_wait")]                         
    //public IEnumerator Char1FinishTalking()
    //{
    //    //Debug.Log("Coroutine started...");
    //    var trigger = false;
    //    System.Action action = () => trigger = true;
    //    LineManager.Char1FinishedTalking.AddListener(action.Invoke);
    //    yield return new WaitUntil(() => trigger);
    //    LineManager.Char1FinishedTalking.RemoveListener(action.Invoke);
    //    //LineView.OnContinueClicked();
    //    //Debug.Log("Coroutine finished!!");
    //    //
    // }


    //[YarnCommand("boy_talk")]                           //yarn command retrieving required voiceline number from YARN and setting clip and initiating Audio in VOLineController
    //public void BoyToTalk()
    //{
    //    LineManager.Char2SpeechPlayback();
    //}

    //[YarnCommand("char2_finish_talking_wait")]                         
    //public IEnumerator Char2FinishTalking()
    //{
    //    //Debug.Log("Coroutine started...");
    //    var trigger = false;
    //    System.Action action = () => trigger = true;
    //    LineManager.Char2FinishedTalking.AddListener(action.Invoke);
    //    yield return new WaitUntil(() => trigger);
    //    LineManager.Char2FinishedTalking.RemoveListener(action.Invoke);
    //    //LineView.OnContinueClicked();
    //    //Debug.Log("Coroutine finished!!");
    //    //
    //}

}
