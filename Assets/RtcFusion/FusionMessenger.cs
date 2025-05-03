using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// This class implements a simple message system to exchange text
/// messages between players. This is used by the SignalingHandler
/// to send and receive the signaling messages.
/// </summary>
public class FusionMessenger : NetworkBehaviour, INetworkRunnerCallbacks
{
    public event Action<String, PlayerRef> SignalingMessageReceived;
    /// <summary>
    /// Key used as the first number to identify our messages.
    /// </summary>
    const int KEY_PREFIX = 127;

    /// <summary>
    /// 
    /// </summary>
    public bool IsAvailable
    {
        get
        {
            return this.Runner != null && Object != null;
        }
    }
    public PlayerRef LocalPlayer
    {
        get
        {
            return this.Runner.LocalPlayer;
        }
    }
    public IEnumerable<PlayerRef> ActivePlayers
    {
        get
        {
            return this.Runner.ActivePlayers;
        }
    }

    public override void Spawned()
    {
        base.Spawned();

        this.Runner.AddCallbacks(this);
    }

    /// <summary>
    /// Sends a signaling message to the server. The server will
    /// either process it (if they are the toPlayer) or forward it to
    /// the intended player.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="toPlayer"></param>
    public void SendSignalingMessage(string message, PlayerRef toPlayer)
    {
        Debug.Log($"Sending to {toPlayer}: {message}");
        //Key has 4 numbers.
        //1. is a prefix to identify messages specific to this class
        //2. is the sender
        //3. is the receiver
        //4. we don't need and set to 0
        //This system allows the server to simply relay messages without further processing
        ReliableKey key = ReliableKey.FromInts(KEY_PREFIX, this.LocalPlayer.AsIndex, toPlayer.AsIndex, 0);
        byte[] messageData = Encoding.Unicode.GetBytes(message);
        this.Runner.SendReliableDataToServer(key, messageData);
    }
    /// <summary>
    /// Triggered once fusion received reliable data. 
    /// </summary>
    /// <param name="from"></param>
    /// <param name="key"></param>
    /// <param name="data"></param>
    private void OnFusionDataReceived(PlayerRef from, ReliableKey key, ArraySegment<byte> data)
    {
        //Check the key 
        int keyPrefix; int fromIndex; int toIndex; int d;
        key.GetInts(out keyPrefix, out fromIndex, out toIndex, out d);

        //Not our prefix? Message is intendet for us at all
        if (keyPrefix != KEY_PREFIX)
            return;
        //Check if the message should be received by our player or
        //forward to another
        PlayerRef fromPlayer = PlayerRef.FromIndex(fromIndex);
        if (toIndex == this.LocalPlayer.AsIndex)
        {

            //message is for us -> process
            OnDataReceived(data, fromPlayer);
        }
        else
        {
            //Message is for another player (we are the host). Forward it
            PlayerRef toPlayer = PlayerRef.FromIndex(toIndex);
            Debug.Log($"Relaying message from {fromPlayer} to {toPlayer}");
            this.Runner.SendReliableDataToPlayer(toPlayer, key, data.ToArray());
        }
    }
    public void RelayMessageToClient(PlayerRef from, PlayerRef to, byte[] data)
    {
        ReliableKey key = ReliableKey.FromInts(KEY_PREFIX, from.AsIndex, 0, 0);
        this.Runner.SendReliableDataToPlayer(to, key, data);
    }



    /// <summary>
    /// We received a data from another user.
    /// Convert to string, debug print it and then send to the signaling system.
    /// </summary>
    /// <param name="data"></param>
    /// <param name="from"></param>
    private void OnDataReceived(ArraySegment<byte> data, PlayerRef from)
    {
        var message = Encoding.Unicode.GetString(data.ToArray());

        Debug.Log($"Received message from {from}: {message}");
        if (SignalingMessageReceived != null)
        {
            try
            {
                SignalingMessageReceived(message, from);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SignalingMessageReceived event handler triggered an exception: {ex.Message}");
                Debug.LogException(ex);
            }
        }
    }

    /// <summary>
    /// Only callback from INetworkRunnerCallbacks we need. 
    /// This is triggered when any reliable message is received. 
    /// </summary>
    /// <param name="runner"></param>
    /// <param name="from"></param>
    /// <param name="key"></param>
    /// <param name="data"></param>
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef from, ReliableKey key, ArraySegment<byte> data)
    {
        this.OnFusionDataReceived(from, key, data);
    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {

    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {

    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {

    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {

    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {

    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }


    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {

    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }
    public void OnConnectedToServer(NetworkRunner runner)
    {

    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {

    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {

    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {

    }
}
