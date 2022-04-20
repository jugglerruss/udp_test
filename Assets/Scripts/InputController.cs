using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour
{
    [SerializeField] private Text _textIp;
    [SerializeField] private Text _textTick;
    [SerializeField] private Slider _tickSlider;
    public Action<int,int,int> OnInput;
    public Action<string> OnConnect;
    public Action<int> OnChangeTick;
    public Action OnStop;
    private int _x, _y, _boost;
    private bool _boostedFlag;
    private void Start()
    {
        _x = 0;
        _y = 0;
    }
    private void OnDestroy()
    {
        StopCoroutine(BoostReset());
    }
    void FixedUpdate()
    {
        SetDirection();
        if(!Game.Stoped)
            ChangeTick(Game.GetTick());
    }
    private void SetDirection()
    {
        if(!_boostedFlag && Input.GetKey(KeyCode.LeftShift))
            _boost = 1;
        _x = (int)Math.Round(Input.GetAxis("Horizontal"));
        _y = (int)Math.Round(Input.GetAxis("Vertical"));
        OnInput?.Invoke(_x,_y,_boost);
        if (_boost == 1 && !_boostedFlag)
            StartCoroutine(BoostReset());
    }
    private IEnumerator BoostReset()
    {
        _boostedFlag = true;
        yield return new WaitForSeconds(0.5f);
        _boost = 0;
        yield return new WaitForSeconds(1f);
        _boostedFlag = false;
    }
    public void Connect()
    {
        OnConnect?.Invoke(_textIp.text);
    }
    public void Stop()
    {
        OnStop?.Invoke();
    }
    public void OnChangeSlider(float tick)
    {
        _textTick.text = tick.ToString();
        OnChangeTick?.Invoke((int)tick);
    }
    private void ChangeTick(int tick)
    {
        _textTick.text = tick.ToString();
        _tickSlider.value = tick;
    }
}
