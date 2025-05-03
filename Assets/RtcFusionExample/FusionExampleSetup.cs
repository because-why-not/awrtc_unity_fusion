using Byn.Awrtc.Base;
using Byn.Unity.Examples;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public class FusionExampleSetup : MonoBehaviour
{
    private BasicSpawner mSpawner;
    private FusionSignalingHandler mSignalingHandler;
    private CallAppUi mCallApp;
    private ConferenceApp mConApp;

    private void Awake()
    {
        mSpawner = Object.FindFirstObjectByType<BasicSpawner>();
        mSignalingHandler = Object.FindFirstObjectByType<FusionSignalingHandler>();
        mCallApp = Object.FindFirstObjectByType<CallAppUi>();
        mConApp = Object.FindFirstObjectByType<ConferenceApp>();

    }
    private void SetupCall()
    {
        //Create a new network configuration that forces
        //signaling connections to go through fusion
        CommonNetworkConfig config = new CommonNetworkConfig();
        //This replaces our usual websocket / signaling server
        config.SignalingNetwork = mSignalingHandler.Signaler;
        //Ice server is still needed for online connections!
        config.IceServers.Add(ExampleGlobals.DefaultIceServer);
        config.KeepSignalingAlive = true;
        config.IsConference = true;
        //replace default configuraion
        mConApp.NetConfig = config;
        mConApp.JoinButtonPressed();
    }
    private void OnGUI()
    {
        if(mSpawner.Runner == null)
        {
            bool started = false;
            if (GUI.Button(new Rect(0, 0, 200, 40), "Host"))
            {
                started = true;
                mSpawner.StartGame(GameMode.Host);
            }
            if (GUI.Button(new Rect(0, 40, 200, 40), "Join"))
            {
                started = true;
                mSpawner.StartGame(GameMode.Client);
            }

            if (started)
            {
                //Make sure the call is ready to connect
                if (mCallApp != null)
                    mCallApp.JoinButtonPressed();

                SetupCall();
            }
        }

        if (mSignalingHandler.Messenger.IsAvailable)
        {
            int y = 0;
            foreach (var player in mSignalingHandler.Messenger.ActivePlayers)
            {
                if (player != mSignalingHandler.Messenger.LocalPlayer)
                {
                    if (GUI.Button(new Rect(Screen.width - 300, 100 + 40 * y, 200, 40), "Call" + player))
                    {
                        mSignalingHandler.Connect(player);
                    }
                }
                y++;
            }
        }
    }
}
