using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Chat : MonoBehaviour
{
    [SerializeField] private InputField _outMessage;
    [SerializeField] private InputField _name;
    [SerializeField] private Text _chatText;
    private static readonly List<string> _messages = new List<string>();
    private string _newMsg;
    public void ReceiveMsg(string msg)
    {
        _messages.Add(msg);
        _newMsg = msg;
    }
    private void Update()
    {
        if (_newMsg == null) return;
        _chatText.text += _newMsg + "\n";
        _newMsg = null;
    }
    public void Send()
    {
       // _client.SendMessage(_name.text + " | " + _outMessage.text + "\n");
    }
} 
