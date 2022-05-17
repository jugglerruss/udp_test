using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
    public bool Registred { get; private set; } 
    private int _portLocal;
    private int _portRemote;
    private Thread _clientListener;
    private IPAddress _ip;
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
    public void SendInput( long localTick, int x, int y, int boost)
    {
        if (!Registred) return;
        byte[] bufferLocalTick = BitConverter.GetBytes(localTick);
        byte[] buffer =  { (byte)boost, (byte)(x + 1), (byte)(y + 1) };
        byte[] newBuffer = new byte[bufferLocalTick.Length + buffer.Length];
        bufferLocalTick.CopyTo(newBuffer, 0); 
        buffer.CopyTo(newBuffer, bufferLocalTick.Length); 
        _sendBufferList[localTick] = newBuffer;
        //Debug.Log("x" + x + "y" + y);  
        if (localTick <= Game.LocalTickFromServer) return;
        for (long i = Game.LocalTickFromServer+1; i <= localTick; i++)
        {
            try
            {
                _clientUdpSender.Send(_sendBufferList[i], _sendBufferList[i].Length);
            }
            catch (Exception e)
            {
                Debug.Log(Game.LocalTickFromServer+1 - localTick);
                throw;
            }
        }

    }
    private void Reader()
    { 
        ReceiveHandler handler = new ReceiveHandler(OnReceive);
        IPEndPoint remoteIp = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (true)
            {
                if (Registred)
                {
                    ReadDirections(handler,remoteIp);
                }
                else
                {
                    remoteIp = Register(remoteIp);
                }
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
        Registred = true;
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
