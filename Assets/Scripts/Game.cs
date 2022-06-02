using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

public class Game : MonoBehaviour
{
    private const int PORT_LOCAL = 13000;
    private const int PORT_REMOTE = 13200;
    [SerializeField] private InputController _input;
    [SerializeField] private Plane _plane;
    [SerializeField] private FileCreator _fileCreator; 
    private Client _client;
    public static int MyPlayerId { get; private set; }
    public static long LocalTickFromServer { get; private set; }
    public static long TickServer { get; private set; }
    public static long TickLocal { get; private set; }
    public static bool Stoped { get; private set; }
    private bool _isFirstTick;
    private byte[] _receiveBuffer;
    private Player _myPlayer;
    private void Start() 
    {
        Application.runInBackground = true;
        _input.OnConnect += Connect;
        TickServer = 1;
        
    }
    private void Update() 
    {
        if(_myPlayer == null) return;
        var preLastPosOnServer = _myPlayer.PreLastServerPosition; 
        var lastPosOnServer = _myPlayer.ServerPosition;
        var lastPosOnClient = _myPlayer.LocalPosition;
        _fileCreator.AddString($"{Time.time} {TickLocal} {TickServer-1} {TickServer} " +
                               $"{preLastPosOnServer.x} {preLastPosOnServer.y} {lastPosOnServer.x} {lastPosOnServer.y} " +
                               $"{lastPosOnClient.x} {lastPosOnClient.y} {_myPlayer.Interpolation}\n");
    }
    private void UpdateInput(System.Object obj)
    {
        if(Stoped)return;
        _input.InputUpdate();
    }
    private void Connect(string ip)
    {
        Stoped = false;
        if(_client is { Registered: true }) return;
        if (ip == "") ip = "91.239.19.112";
        _client = new Client(PORT_LOCAL,PORT_REMOTE, ip);
        _client.OnRegister += SetId;
        _client.OnReceive += SetDirections;
        _client.Work();
        _input.OnInput += SetInput;
        _input.OnStop += StopClient;
        _input.OnChangeTick += ChangeTickFromInput;
        _plane.OnSetMyPlayer += SetMyPlayer;
        
        TimerCallback timerCallback = UpdateInput;
        Timer inputSender = new Timer(timerCallback, null, 0, 100);
    }
    private void SetMyPlayer(Player myPlayer)
    {
        _myPlayer = myPlayer;
    }
    private void SetDirections(byte[] buffer)
    {
        if (Stoped) return;
        SetTick(buffer);
        _receiveBuffer = buffer.Skip(16).ToArray();
        _plane.SetDictDirections(_receiveBuffer);
    }
    private void SetInput(int x,int y, int boost)
    {
        if (Stoped || MyPlayerId == 0 ) return;
        if (_isFirstTick) SetLocalTick();
        _client.SendInput(TickLocal, x, y, boost);
        _plane.SetInput(x,y,boost); 
    }
    private void SetLocalTick()
    {
        TickLocal++;
        _plane.ChangeTick();
    }
    private void SetTick(byte[] buffer)
    {
        var newBuffer = buffer.Take(8).ToArray();
        LocalTickFromServer = BitConverter.ToInt64(newBuffer, 0); 
        newBuffer = buffer.Skip(8).Take(8).ToArray();
        TickServer = BitConverter.ToInt64(newBuffer, 0);
     //   Debug.Log("Game.LocalTickFromServer" + Game.LocalTickFromServer + " TickLocal " + TickLocal + " TickServer " + TickServer);  
        if (_isFirstTick)
            return;
        _isFirstTick = true;
        TickLocal = TickServer;
    }
    private void ChangeTickFromInput(long tick)
    {
        if (!Stoped) return;
        _plane.MovePlayerShadows(tick);
    }
    private void StopClient()
    { 
        Stoped = true;
        _isFirstTick = false;
    }
    private void SetId(byte id)
    {
        MyPlayerId = id;
    }
}
