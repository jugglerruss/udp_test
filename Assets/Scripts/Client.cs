using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class Client
{
    private static UdpClient _clientUdpSender;
    private static UdpClient _clientUdpReceiver;
    public Action<byte[]> OnReceive;
    public Action<byte> OnRegister;

    private delegate void ReceiveHandler(byte[] buffer);
    private readonly Dictionary<long,byte[]> _sendBufferList;
    public bool Registered { get; private set; } 
    private int _portLocal;
    private int _portRemote;
    private readonly Thread _clientListener;
    private readonly IPAddress _ip;
    public Client(int portLocal,int portRemote, string connectIp)
    {
        _ip = IPAddress.Parse(connectIp);
        _clientUdpSender = new UdpClient(portRemote,_ip.AddressFamily);
        _clientUdpReceiver = new UdpClient(portLocal);
        _sendBufferList = new Dictionary<long, byte[]>();
        _portRemote = portRemote;
        _clientListener = new Thread(Reader);
        _portLocal = portLocal;
        _clientUdpSender.Connect(_ip, _portRemote);
    }
    public void Work()
    {
        _clientListener.Start();
    }
    public void SendInput( long localTick, int x, int y, int boost, int fire)
    {
        if (!Registered) return;
        byte[] bufferLocalTick = BitConverter.GetBytes(localTick);
        byte[] buffer =  { (byte)boost, (byte)(x + 1), (byte)(y + 1), (byte)fire };
        byte[] newBuffer = new byte[bufferLocalTick.Length + buffer.Length];
        bufferLocalTick.CopyTo(newBuffer, 0); 
        buffer.CopyTo(newBuffer, bufferLocalTick.Length); 
        _sendBufferList[localTick] = newBuffer;
        if (localTick <= Game.LocalTickFromServer) return;
        var diff = localTick - Game.LocalTickFromServer; 
        byte[] sendBuffer = new byte[diff*_sendBufferList[localTick].Length];
        //Debug.Log("diff" + diff);
        int k = 0;
        for (long i = Game.LocalTickFromServer+1; i <= localTick; i++) 
        {
            try
            {
                if (_sendBufferList.ContainsKey(i))
                {
                    var index = k * _sendBufferList[i].Length;
                    _sendBufferList[i].CopyTo(sendBuffer, index);
                    k++;
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
                throw;
            }
        }
        _clientUdpSender.Send(sendBuffer, sendBuffer.Length);
    }
    private void Reader()
    { 
        ReceiveHandler handler = new ReceiveHandler(OnReceive);
        IPEndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (true)
            {
                if (Registered)
                    ReadDirections(handler,remoteIp);
                else
                    remoteIp = Register(remoteIp);
            }
        }
        catch(Exception ex)
        {
            Debug.Log(ex.Message);
        }
        finally
        {
            Debug.Log("Close");
            _clientUdpReceiver.Close(); 
        }
    }
    private IPEndPoint Register(IPEndPoint remoteIp)
    {
        Debug.Log("Send " + _clientUdpSender.Send(new byte []{0}, 1));
        var byteBuffer = _clientUdpReceiver.Receive(ref remoteIp);
        if (byteBuffer[0] == 0) return remoteIp; 
        OnRegister?.Invoke(byteBuffer[0]);
        Debug.Log("MyPlayerId " + Game.MyPlayerId );
        Registered = true;
        _portLocal += byteBuffer[0];
        _portRemote += byteBuffer[0];
        _clientUdpReceiver = new UdpClient(_portLocal);
        _clientUdpSender.Close();
        _clientUdpSender = new UdpClient();
        _clientUdpSender.Connect(_ip, _portRemote);
        return remoteIp;
    }
    private void ReadDirections(ReceiveHandler handler,IPEndPoint remoteIp)
    {
        var byteBuffer = _clientUdpReceiver.Receive(ref remoteIp);
        handler.BeginInvoke(byteBuffer, null, null);
    }
    ~Client()
    {
        _clientListener.Abort();
        _clientUdpSender?.Close();
        _clientUdpReceiver?.Close();
    }
}
