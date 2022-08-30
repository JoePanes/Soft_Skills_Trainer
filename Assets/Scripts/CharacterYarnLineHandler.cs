using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity;
using Yarn.Markup;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.Events;
using System.Threading.Tasks;

public class CharacterYarnLineHandler : MonoBehaviour
{
    public string characterName;

    public GameObject characterModel;

    public YarnProject myYarnProject;

    public SpeechManager characterSpeechManager;
    //public YarnCommandController YarnCommandController;
    public AudioSource characterAudioSource;

    [SerializeField] private GameObject GPTHandler;

    private List<string> allLinesIDList = new List<string>();
    private List<string> characterLineIDList = new List<string>();
    private List<string> characterNegativeLineIDList = new List<string>();
    private List<string> characterNeutralLineIDList = new List<string>();
    private List<string> characterPositiveLineIDList = new List<string>();

    [Header("Stage 1 Line ID's")]
    private List<string> characterNegativeLineStage1IDList = new List<string>();
    private List<string> characterNeutralLineStage1IDList = new List<string>();
    private List<string> characterPositiveLineStage1IDList = new List<string>();

    [Header("Stage 2 Line ID's")]
    private List<string> characterNegativeLineStage2IDList = new List<string>();
    private List<string> characterNeutralLineStage2IDList = new List<string>();
    private List<string> characterPositiveLineStage2IDList = new List<string>();

    [Header("Stage 3 Line ID's")]
    private List<string> characterNegativeLineStage3IDList = new List<string>();
    private List<string> characterNeutralLineStage3IDList = new List<string>();
    private List<string> characterPositiveLineStage3IDList = new List<string>();

    [Header("All Lines")]
    public List<string> characterTextLineList = new List<string>();
    public List<string> characterNegativeLineList = new List<string>();
    public List<string> characterNeutralLineList = new List<string>();
    public List<string> characterPositiveLineList = new List<string>();

    [Header("Stage 1 Lines")]
    public List<string> characterNegativeLineStage1List = new List<string>();
    public List<string> characterNeutralLineStage1List = new List<string>();
    public List<string> characterPositiveLineStage1List = new List<string>();

    [Header("Stage 2 Lines")]
    public List<string> characterNegativeLineStage2List = new List<string>();
    public List<string> characterNeutralLineStage2List = new List<string>();
    public List<string> characterPositiveLineStage2List = new List<string>();

    [Header("Stage 3 Lines")]
    public List<string> characterNegativeLineStage3List = new List<string>();
    public List<string> characterNeutralLineStage3List = new List<string>();
    public List<string> characterPositiveLineStage3List = new List<string>();

    public int characterLineCount;

    public string learningResponseLine;

    public bool learningResponseActivate;

    public float waitTime;

    public UnityEvent characterFinishedTalking;

    public Text debugText;

    private ConversationController ConversationController;

    

    private void Awake()
    {
        GetLineIDs();
        SortLineIDs();
        GetLinesFromIDs();
        ConversationController = GameObject.Find("ConversationController").GetComponent<ConversationController>();
    }


    public void GetLineIDs()                                                //finds all of the line IDs in the referenced YARN project and lists
    {
        var lineID = myYarnProject.GetLocalization("En").GetLineIDs();
        allLinesIDList = lineID.ToList();
    }

    public void SortLineIDs()                                               //sorts all of the IDs based on string segments found in the YARN line ID and lists
    {
        foreach (string tempString in allLinesIDList)
        {
            if (tempString.Contains(characterName))
            {
                characterLineIDList.Add(tempString);
            }

            if (tempString.Contains(characterName + "Negative"))
            {
                characterNegativeLineIDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "Neutral"))
            {
                characterNeutralLineIDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "Positive"))
            {
                characterPositiveLineIDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "NegativeStage1"))
            {
                characterNegativeLineStage1IDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "NeutralStage1"))
            {
                characterNeutralLineStage1IDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "PositiveStage1"))
            {
                characterPositiveLineStage1IDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "NegativeStage2"))
            {
                characterNegativeLineStage2IDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "NeutralStage2"))
            {
                characterNeutralLineStage2IDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "PositiveStage2"))
            {
                characterPositiveLineStage2IDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "NegativeStage3"))
            {
                characterNegativeLineStage3IDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "NeutralStage3"))
            {
                characterNeutralLineStage3IDList.Add(tempString);
            }
            if (tempString.Contains(characterName + "PositiveStage3"))
            {
                characterPositiveLineStage3IDList.Add(tempString);
            }

        }
    }

    public void GetLinesFromIDs()                                           //pulls all of the text for each line ID out of YARN project and lists
    {
        foreach (string tempString in characterLineIDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterTextLineList.Add(text);
        }

        foreach (string tempString in characterNegativeLineIDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterNegativeLineList.Add(text);
        }

        foreach (string tempString in characterNeutralLineIDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterNeutralLineList.Add(text);
        }

        foreach (string tempString in characterPositiveLineIDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterPositiveLineList.Add(text);
        }

        foreach (string tempString in characterNegativeLineStage1IDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterNegativeLineStage1List.Add(text);
        }

        foreach (string tempString in characterNeutralLineStage1IDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterNeutralLineStage1List.Add(text);
        }

        foreach (string tempString in characterPositiveLineStage1IDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterPositiveLineStage1List.Add(text);
        }

        foreach (string tempString in characterNegativeLineStage2IDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterNegativeLineStage2List.Add(text);
        }

        foreach (string tempString in characterNeutralLineStage2IDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterNeutralLineStage2List.Add(text);
        }

        foreach (string tempString in characterPositiveLineStage2IDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterPositiveLineStage2List.Add(text);
        }

        foreach (string tempString in characterNegativeLineStage3IDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterNegativeLineStage3List.Add(text);
        }

        foreach (string tempString in characterNeutralLineStage3IDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterNeutralLineStage3List.Add(text);
        }

        foreach (string tempString in characterPositiveLineStage3IDList)
        {
            var text = myYarnProject.GetLocalization("En").GetLocalizedString(tempString);
            characterPositiveLineStage3List.Add(text);
        }

        //var text = myYarnProject.GetLocalization("En").GetLocalizedString("line:01c1039");
        //lineTextString = text;
        //Debug.LogError(text);
    }

    public async void CharacterSpeechPlayback()                                   //sets the line to be sent to the TTS from the line list and sends
    {
        //Debug.LogError(characterName + " speaking...");
        if (characterSpeechManager.isReady)
        {
            if (learningResponseActivate)
            {

                learningResponseLine = await GetResponse(learningResponseLine);
                Debug.Log(learningResponseLine);
                characterSpeechManager.SpeakWithSDKPlugin(learningResponseLine);

                await DetermineSentiment(learningResponseLine);

                //StartCoroutine(CharacterVolTrim());
                StartCoroutine(CharacterWaitForLineToFinish());
                learningResponseActivate = false;
            }
            else
            {
                
                string lineToBeSpoken = await GetResponse(characterTextLineList[characterLineCount]);

                Debug.Log(lineToBeSpoken);
                characterSpeechManager.SpeakWithSDKPlugin(lineToBeSpoken);

                await DetermineSentiment(learningResponseLine);

                characterLineCount++;
                StartCoroutine(CharacterWaitForLineToFinish());
            }

        }
        else
        {
            Debug.Log(characterName + "'s speechManager is not ready. Wait until authentication has completed.");
        }
    }

    /// <summary>
    /// Determine which text should be given to the text-to-speech system.
    /// Whether to use the pre-written or GPT generated system
    /// </summary>
    /// <param name="text">The original pre-written text</param>
    /// <returns></returns>
    private async Task<string> GetResponse(string text)
    {
        switch (ConversationController.NPC_TEXT_MODE)
        {
            case 0:
                //Nothing needs to be done
                return text;
            case 1:
                return text = await GPTHandler.GetComponent<GPT3>().HandleVariationCall(text);
            case 2:
                //To do:
                return "boop";
            default:
                Debug.LogError("Invalid value for NPC_TEXT_MODE in ConversationController.cs");
                return "If you are seeing/hearing this then there is something wrong with NPC_TEXT_MODE in ConversationController.cs.";
               
        }
    }

    private async Task DetermineSentiment(string lineToBeSpoken)
    {
        string sentiment = await GPTHandler.GetComponent<GPT3>().GetSentiment(lineToBeSpoken);
        characterModel.GetComponent<NPCAnimationController>().UpdateEmotion(sentiment);
    }

    public IEnumerator CharacterWaitForLineToFinish()                       //coroutine set to complete once the NPCs audio clip has completed
    {
        // Trigger appropriate animation for virtual humans
        //For the current speaker
        characterModel.GetComponent<NPCAnimationController>().ToggleTalking();
        //Inform other virtual humans that someone is speaking
        ConversationController.GetComponent<ConversationController>().ToggleSomeoneTalking();
                
        yield return new WaitUntil(() => characterAudioSource.isPlaying);
        yield return new WaitUntil(() => !characterAudioSource.isPlaying);
        
        //Notify that all is over
        characterModel.GetComponent<NPCAnimationController>().ToggleTalking();
        ConversationController.GetComponent<ConversationController>().ToggleSomeoneTalking();
        characterFinishedTalking.Invoke();
        //Debug.LogError(characterName + " finished talking");
    }
    
    //public IEnumerator CharacterVolTrim()                                   //a test coroutine to try and fix the clicking sound heard at the start of TTS audio clip
    //{
    //    characterAudioSource.volume = 0;
    //    yield return new WaitForSeconds(waitTime);
    //    characterAudioSource.volume = 1;
    //}
}
