using System;
using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    [SerializeField] private Transform _panelButtons;
    [SerializeField] private Text _textIp;
    [SerializeField] private Text _textTick;
    public Action<int[]> OnInput;
    public Action<string> OnConnect;
    public Action<long> OnChangeTick;
    public Action OnStop;
    private int _x, _y, _boost, _fire;
    private int _boostedCounter;
    private int _boostStopCounter;
    private int _fireStopCounter;
    private bool _boostStop;
    private bool _fireStop;
    private void Update()
    {
        if(!Game.Stoped)
            ChangeTick(Game.TickServer);
        if(Input.GetKey(KeyCode.LeftShift))
            _boost = 1;
        _fire = Input.GetKey(KeyCode.LeftControl) ? 1 : 0;
        _x = (int)Math.Round(Input.GetAxis("Horizontal"));
        _y = (int)Math.Round(Input.GetAxis("Vertical"));
    }
    public void InputUpdate()
    {
        if (_boostStop)
        {
            _boost = 0;
            _boostStopCounter++;
            if (_boostStopCounter == 10)
            {
                _boostStopCounter = 0;
                _boostStop = false;  
            }
        }
        if (_fireStop)
        {
            _fire = 0;
            _fireStopCounter++;
            if (_fireStopCounter == 10)
            {
                _fireStopCounter = 0;
                _fireStop = false;  
            }
        }
        OnInput?.Invoke(new []{_x,_y,_boost, _fire});
        if (_fire == 1)
        {
            _fireStop = true;
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

    public void HideButtons(bool hide)
    {
        _panelButtons.gameObject.SetActive(hide);
    }
}
