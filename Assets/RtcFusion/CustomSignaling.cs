
using Byn.Awrtc;
using NanoSockets;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


/// <summary>
/// This is an example how simple 1 to 1 signaling connection might look like.
/// 
/// It converts messages to string & back to show how it can be used with an 
/// external system that can only handle strings.
/// 
/// Note this does not include much error handling and covers just the normal usage as
/// done via the CallApp example. 
/// </summary>
public class CustomSignaling : IBasicNetwork
{
    /// <summary>
    /// This flag helps to make sure the signaling system behaves similar to the current
    /// signaling server. 
    /// Many examples will automatically call ICall.Listen to wait for a connection. If an user
    /// already is listening on an address for the 2nd user ICall.Listen fails. They then will
    /// attemt to connect via ICall.Call. This creates the "join" mechanic.
    /// 
    /// Our custom signaling system might not have an address. As such, both users would get stuck
    /// in the ICall.Listen role and both will wait forever for the other side to initiate a connection. 
    /// To work around this we force an active role unto one user via SetActive. This user will then
    /// create the outgoing connection.
    /// 
    /// </summary>
    private bool mIsActive = false;
    //signaling needs to forward message to connection id
    public event Action<ConnectionId, string> OnRelayMessage;
    //a new connection id was created and attempts to connect to the given address
    //allow or deny via RespondConnectionRequest
    //Use this event to store what ConnectionId sends messages to which address
    public event Action<ConnectionId, string> OnConnectionRequest;

    public event Action<string> OnListenRequest;

    public void RespondConnectionRequest(ConnectionId id, string address, bool isAccepted) {

        if (isAccepted)
        {
            mActiveConnections.Add(id);
            Enqueue(new NetworkEvent(NetEventType.NewConnection, id));
        }
        else
        {
            Debug.Log($"Connection to {address} denied!");
            Enqueue(new NetworkEvent(NetEventType.ConnectionFailed, id));
        }
    }
    public void RespondListenRequest(string address, bool isAccepted)
    {
        if (isAccepted)
        {
            mAddress = address;
            this.Enqueue(new NetworkEvent(NetEventType.ServerInitialized, ConnectionId.INVALID));
        }
        else
        {
            this.Enqueue(new NetworkEvent(NetEventType.ServerInitFailed, ConnectionId.INVALID));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public ConnectionId AcceptIncomingConnection()
    {
        ConnectionId id = NextId();
        mActiveConnections.Add(id);
        Enqueue(new NetworkEvent(NetEventType.NewConnection, id));
        return id;
    }


    private Queue<NetworkEvent> mEvents = new Queue<NetworkEvent>();

    /// <summary>
    /// ConnectionId's that are currently active either due to 
    /// accepted as an outgoing connection or because we received incoming messages from them.
    /// </summary>
    private HashSet<ConnectionId> mActiveConnections = new HashSet<ConnectionId>();

    private short mNextId = 1;


    private ConnectionId NextId()
    {
        ConnectionId id = new ConnectionId(mNextId);
        mNextId++;
        return id;
    }

    private string mAddress = null;


    /// <summary>
    /// This method is called when we want to wait and accept connection from other users. 
    /// Typically used during ICall.Listen("passphraseOrAddress"); and 
    /// IMediaNetwork.StartServer("address"). 
    /// 
    /// </summary>
    /// <param name="address">
    /// Any kind of string or address so others can identify who to connect to.
    /// If null StartServer is expected to assign a random address (rarely used). 
    /// </param>
    public void StartServer(string address = null)
    {
        //for our dummy test the active side will refuse to enter server / waiting mode
        //this will force the CallApp to try and trigger the Connect method below
        if (mIsActive)
        {
            this.Enqueue(new NetworkEvent(NetEventType.ServerInitFailed, ConnectionId.INVALID));
            return;
        }

        if (OnListenRequest != null)
        {
            OnListenRequest(address);
        }
        else
        {
            RespondListenRequest(address, true);
        }
    }

    /// <summary>
    /// Called when we are expected to connect to another client.
    /// 
    /// This method is typical called during ICall.Call("passphrase");
    /// </summary>
    /// <param name="address"> can be any kind of string we use to identify the other user </param>
    /// <returns>
    /// We must return a valid connection id. If there is only one you can simply return
    /// new ConnectionId(1);
    /// This ID is later used to reference this connection e.g. Disconnect(conId);
    /// </returns>
    public ConnectionId Connect(string address)
    {
        var id = NextId();
        if(OnConnectionRequest != null)
        {
            //let the user decide
            OnConnectionRequest(id, address);
        }
        else
        {
            //if no handler is registered we assume no address system is needed
            //and we just return a connected event to continue signaling
            RespondConnectionRequest(id, address, true);
        }
        return id;
    }

    private void Enqueue(NetworkEvent evt)
    {
        mEvents.Enqueue(evt);
    }
    /// <summary>
    /// This returns events that are either replies to any of the other calls (Connect/Disconnect/StartServer and so on)
    /// or it returns messages it received from the network. 
    /// 
    /// </summary>
    /// <param name="evt"></param>
    /// <returns></returns>
    public bool Dequeue(out NetworkEvent evt)
    {

        //Tell the other side we are ready to accept their connection
        //evt = new NetworkEvent(NetEventType.Disconnected, new ConnectionId(1));
        if (mEvents.Count == 0)
        {
            evt = new NetworkEvent();
            return false;
        }
        evt = mEvents.Dequeue();
        return true;
    }


    public bool Peek(out NetworkEvent evt)
    {
        if (mEvents.Count == 0)
        {
            evt = new NetworkEvent();
            return false;
        }
        evt = mEvents.Peek();
        return true;
    }


    /// <summary>
    /// Request to stop receiving new connections. 
    /// </summary>
    public void StopServer()
    {
        //Do something to ensure new users can connect
        //once new connections are bocked return this event:
        this.Enqueue(new NetworkEvent(NetEventType.ServerClosed, ConnectionId.INVALID));
    }


    /// <summary>
    /// The app attempts to disconnect a user. 
    /// Note this is commonly used by ICall and IMediaNetwork to cut any signaling connection once
    /// it has connected a direct peer to peer connection. 
    /// 
    /// </summary>
    /// <param name="id">
    /// Same ID that was originally returned via Connect / NewConnection event
    /// </param>
    public void Disconnect(ConnectionId id)
    {
        if (mActiveConnections.Contains(id))
        {
            mActiveConnections.Remove(id);
            //Ensure no new messages get through. 
            //Then return this event:
            this.Enqueue(new NetworkEvent(NetEventType.Disconnected, id));
        }
        else
        {
            Debug.LogWarning("Attempted to disconnect unknown id " + id);
        }
    }


    /// <summary>
    /// These are the actually messages sent across. 
    /// Note in theory this can be used by the app to forward binary data. Because of this it uses byte[]
    /// 
    /// In practise the asset does only use this to forward UTF16 encoded strings. To get the string
    /// version call var msg = Encoding.Unicode.GetString(data, offset, length);
    /// 
    /// See PassThroughSignaling.SendData to see how this is done in a real world app.
    /// 
    /// </summary>
    /// <param name="id">The connection id to send the message to</param>
    /// <param name="data">A byte[] buffer</param>
    /// <param name="offset">The index when our message begins in the buffer</param>
    /// <param name="length">Length of our message in bytes</param>
    /// <param name="reliable">Always true under normal usage. 
    /// true - The callee expects this message to be sent reliably via a TCP style connection
    /// false - The callee expects this to be sent via a udp style connection. Messages can be dropped or sent without any specific order.
    /// (note the callee does not expect to receive corrupted messages!)</param>
    /// <returns>
    /// Ideally, always return true here. Most code won't be able to handle rejected messages.
    /// true - message is being delivered
    /// false - buffer full or unknown id
    /// </returns>
    public bool SendData(ConnectionId id, byte[] data, int offset, int length, bool reliable)
    {
        SendDataBinary(id, data, offset, length, reliable);
        return true;
    }


    /// <summary>
    /// This method is used to allow any other threads to synchronize the events with the Update loop.
    /// It is called before the other side starts using Dequeue to get all events that happened during the last call.
    /// </summary>
    public void Update()
    {

    }
    /// <summary>
    /// Called after Update and after the other side processed all events. 
    /// Could be used for thread synchronization 
    /// </summary>
    public void Flush()
    {

    }


    /// <summary>
    /// This is expected to disconnect all users and stop the server. 
    /// Used as part of the shutdown process / via Dispose. 
    /// TODO: This currently does not raise events for the disconnected connections
    /// </summary>
    public void Shutdown()
    {
        //TODO: Keep event list + trigger disconnect events & stop server events
        mActiveConnections.Clear();
        mAddress = null;
        mEvents.Clear();
    }

    /// <summary>
    /// Free up any handlers we have e.g. TCP sockets or similar.
    /// </summary>
    public void Dispose()
    {
        Shutdown();
    }

    /// <summary>
    /// Our text based system for the test case. 
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="text"></param>
    private void ReceiveText(ConnectionId userId, string text)
    {
        byte[] data = Encoding.Unicode.GetBytes(text);
        ReceiveData(userId, data, 0, data.Length, true);
    }

    /// <summary>
    /// This forwards the messages in the expected format to the internals
    /// </summary>
    /// <param name="conId"></param>
    /// <param name="data"></param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <param name="reliable"></param>
    private void ReceiveData(ConnectionId conId, byte[] data, int offset, int length, bool reliable)
    {
        //Older unity versions had really bad garbage collection performance
        //To avoid this we use ByteArrayBuffer instead of byte[] directly. This way buffers can be reused
        ByteArrayBuffer buffer = ByteArrayBuffer.Get(length);
        buffer.CopyFrom(data, offset, length);
        if (buffer.PositionReadRelative != 0)
        {
            SLog.LE("Received an invalid message. PositionReadRelative must be 0 but is " + buffer.PositionReadRelative);
        }
        if (buffer.PositionWriteRelative == 0)
        {
            SLog.LE("Received an invalid message. Message empty.");
        }

        NetEventType type = NetEventType.UnreliableMessageReceived;
        if (reliable)
            type = NetEventType.ReliableMessageReceived;
        this.Enqueue(new NetworkEvent(type, conId, buffer));
    }

    /// <summary>
    /// Receives the typical binary format the asset uses and converts it into text for this use-case.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="data"></param>
    /// <param name="offset"></param>
    /// <param name="length"></param>
    /// <param name="reliable"></param>
    private void SendDataBinary(ConnectionId id, byte[] data, int offset, int length, bool reliable)
    {
        var txt = Encoding.Unicode.GetString(data, offset, length);
        SendDataText(id, txt);
    }
    private void SendDataText(ConnectionId id, string text)
    {
        if (OnRelayMessage != null)
        {
            OnRelayMessage(id, text);
        }else
        {
            Debug.LogWarning("OnRelayMessage event handler set.");
        }
    }
    public void DeliverMessage(ConnectionId id, string txt)
    {
        if (mActiveConnections.Contains(id) == false) { 
            if(mIsActive)
            {
                //This could lead to bugs. The official signaling always first calls StartServer with
                //an address and only then can receive messages. Here we get a message without
                //ever setting an address. This results in undefined behavior.
                Debug.LogWarning("Received an incoming message without ever switching into StartServer / Listening mode.");
            }
            //For simplicity we just trigger a new connection event if we get a message
            //with an unknown id. This way the external side can decide ids
            mActiveConnections.Add(id);
            Enqueue(new NetworkEvent(NetEventType.NewConnection, id));
        }
        this.ReceiveText(id, txt);
    }

    public void SetActive()
    {
        mIsActive = true;
    }
}