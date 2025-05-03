using Byn.Awrtc;
using Fusion;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// This class connects the Fusion specific components (FusionMessenger / PlayerRef)
/// and WebRTC Video Chat specific components and translates between them.
/// 
/// To connect:
/// 1. Initialize Fusion and connect
/// 
/// 2. make sure FusionMessenger and FusionSignalingHandler are attached to
/// a NetworkObject in the scene
/// 
/// 3. Create a call with an instance of CommonNetworkConfig and
/// set CommonNetworkConfig.SignalingNetwork to FusionSignalingHandler.Signaler
/// 
/// 4. Set the Call into Listening mode via ICall.Listen. It will now wait for
/// connections initialized by the signaling system. 
/// If the example CallApp is used simply select if you want to send audio / video
/// and then press the join button. The address field is ignored.
/// 
/// 5. Call FusionSignalingHandler.Connect(PlayerRef) on one side when ready to connect.
/// 
/// </summary>
public class FusionSignalingHandler : MonoBehaviour
{
    private FusionMessenger mFusion;
    public FusionMessenger Messenger
    {
        get
        {
            return mFusion;
        }
    }
    private CustomSignaling mSignaler;
    public CustomSignaling Signaler
    {
        get { return mSignaler; }
    }

    //This keeps track of players / connection we are communicating with
    private Dictionary<PlayerRef, ConnectionId> playerRefToId = new Dictionary<PlayerRef, ConnectionId>();
    private Dictionary<ConnectionId, PlayerRef> idToPlayerRef = new Dictionary<ConnectionId, PlayerRef>();

    private void Start()
    {
        mFusion = GetComponent<FusionMessenger>();
        mFusion.SignalingMessageReceived += Signaling_SignalingMessageReceived;

        mSignaler = new CustomSignaling();
        mSignaler.OnConnectionRequest += Signaler_OnConnectionRequest;
        mSignaler.OnRelayMessage += Signaler_OnRelayMessage;
    }

    public void Connect(PlayerRef player)
    {
        //Trigger a new incoming signaling connection for the local call
        //the lower layer will automatically attempt to start a call in response to this.
        ConnectionId id = mSignaler.AcceptIncomingConnection();
        AddConnection(id, player);
    }

    private void Signaler_OnConnectionRequest(ConnectionId id, string address)
    {
        //The handler is designed around the ICall being passive and 
        //not actively initiating a connection until FusionSignalingHandler.Connect is called.
        //If this error happens someone used ICall.Call.
        Debug.LogError("Use ICall.Listen! Do not use ICall.Call");
        //block any outgoing connection attempt
        mSignaler.RespondConnectionRequest(id, address, false);
    }

    private void AddConnection(ConnectionId connectionId, PlayerRef playerRef)
    {
        playerRefToId[playerRef] = connectionId;
        idToPlayerRef[connectionId] = playerRef;
    }
    private void RemoveConnection(ConnectionId connectionId, PlayerRef playerRef)
    {
        idToPlayerRef.Remove(connectionId);
        playerRefToId.Remove(playerRef);
    }

    private void Signaler_OnRelayMessage(ConnectionId id, string message)
    {
        PlayerRef playerRef;
        //lower level attempts to forward a signaling message to another user
        if (idToPlayerRef.TryGetValue(id, out playerRef) == false)
        {
            Debug.LogError($"Failed to relay signaling message to connection id {id}. Unknown PlayerRef");
            return;
        }
        if(playerRef == this.mFusion.LocalPlayer)
        {
            Debug.LogError("Dropped message to own local player.");
            return;
        }
        mFusion.SendSignalingMessage(message, playerRef);
    }

    private void Signaling_SignalingMessageReceived(string message, PlayerRef playerRef)
    {
        ConnectionId id;
        if(playerRefToId.TryGetValue(playerRef, out id) == false)
        {
            //Other side initiated signaling -> add as a new connection
            id = mSignaler.AcceptIncomingConnection();
            AddConnection(id, playerRef);
        }
        //send signaling to the platform specific components
        mSignaler.DeliverMessage(id, message);
    }

    private void OnDestroy()
    {
        mFusion.SignalingMessageReceived -= Signaling_SignalingMessageReceived;
    }
}
