using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationController : MonoBehaviour
{
    private string transcript = "";

    // Controls the use of GPT-3
    // 0, use the human written script
    // 1, text variation
    // 2, prompted conversation
    public const int NPC_TEXT_MODE = 2;
    public bool someoneTalking { get; private set;}
    
    void Awake()
    {
        someoneTalking = false;
    }
    
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToggleSomeoneTalking()
    {
        someoneTalking = !someoneTalking;
    }

    public void SomeoneIsTalking()
    {
        someoneTalking = true;
    }

    public void SomeoneIsNotTalking()
    {
        someoneTalking = false;
    }

    /// <summary>
    /// Add to the record of conversation
    /// </summary>
    /// <param name="talkerIdentifier">The identifier used to keep track of the speaker throughout the conversation</param>
    /// <param name="newText">The new text to added to the conversation</param>
    public void AddToTranscript(string talkerIdentifier, string newText)
    {
        transcript = transcript + "\n" + talkerIdentifier + "| " + newText;

        Debug.Log(transcript);
    }

    public string GetTranscript()
    {
        return transcript;
    }

}
