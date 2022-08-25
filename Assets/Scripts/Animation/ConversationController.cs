using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConversationController : MonoBehaviour
{
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
}
