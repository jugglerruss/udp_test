using System.Net;
using UnityEngine;

public class Game : MonoBehaviour
{
    private const int PORT_LOCAL = 13000;
    private const int PORT_REMOTE = 13200;
    public const int TICK_COUNT = 255;
    [SerializeField] private InputController _input;
    [SerializeField] private Plane _plane;
    private Client _client;
    public static int MyPlayerId;
    private static int _tick;
    public static bool Stoped;
    private void Start()
    {
        Application.runInBackground = true;
        _input.OnConnect += Connect;
        _tick = 1;
    }
    private void Connect(string ip)
    {
        Stoped = false;
        if(_client is { Registred: true }) return;
        if (ip == "") ip = "91.239.19.112";
        _client = new Client(PORT_LOCAL,PORT_REMOTE, ip);
        _client.OnReceive += SetDirections;
        _client.Work();
        _input.OnInput += SetInput;
        _input.OnStop += StopClient;
        _input.OnChangeTick += ChangeTickFromInput;
    }
    private void SetDirections(int[] buffer)
    {
        if (Stoped) return;
        SetTick(buffer[0]);
        _plane.SetDictDirections(buffer);
    }
    private void SetInput(int x,int y, int boost)
    {
        if (Stoped) return;
        _client.SetInput(x,y,boost);
        _plane.SetInput(x,y,boost);
    }
    private void SetTick(int tick)
    {
        _tick = tick;
    }
    private void ChangeTickFromInput(int tick)
    {
        if (!Stoped) return;
        _plane.MovePlayerShadows(tick);
    }
    private void StopClient()
    {
        Stoped = true;
    }
    public static int GetTick()
    {
        return _tick;
    }
    public static int GetPrevTick(int tick = 0)
    {
        if (tick == 0) tick = _tick;
        if (tick == 1)
            return TICK_COUNT;
        return tick-1;
    } 
}
