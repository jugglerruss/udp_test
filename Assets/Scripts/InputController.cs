using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    [SerializeField] private Text _textIp;
    [SerializeField] private Text _textTick;
    public Action<int,int,int> OnInput;
    public Action<string> OnConnect;
    public Action<long> OnChangeTick;
    public Action OnStop;
    private int _x, _y, _boost;
    private int _boostedCounter;
    private int _boostStopCounter;
    private bool _boostStop;
    private void Update()
    {
        if(!Game.Stoped)
            ChangeTick(Game.TickServer);
        if(Input.GetKey(KeyCode.LeftShift))
            _boost = 1;
        _x = (int)Math.Round(Input.GetAxis("Horizontal"));
        _y = (int)Math.Round(Input.GetAxis("Vertical"));
    }
    public void InputUpdate()
    {
        OnInput?.Invoke(_x,_y,_boost);
        if (_boostStop)
        {
            _boostStopCounter++;
            if (_boostStopCounter != 10)
                return;
            _boostStopCounter = 0;
            _boostStop = false;
            return;
        }
        if (_boost == 1)
        {
            _boostedCounter++;
            if (_boostedCounter != 5)
                return;
            _boostedCounter = 0;
            _boost = 0; 
            _boostStop = true;
        }
        else
        {
            _boostedCounter = 0;
        }
    }
    public void Connect()
    {
        OnConnect?.Invoke(_textIp.text);
    }
    public void Stop()
    {
        OnStop?.Invoke();
    }
    public void PrevTick()
    {
        long tick = Int64.Parse(_textTick.text);
        tick--;
        ChangeTick(tick);
        OnChangeTick?.Invoke(tick); 
    }
    public void NextTick()
    {
        long tick = Int64.Parse(_textTick.text);
        tick++;
        ChangeTick(tick);
        OnChangeTick?.Invoke(tick); 
    }
    private void ChangeTick(long tick)
    {
        _textTick.text = tick.ToString();
    }
}
