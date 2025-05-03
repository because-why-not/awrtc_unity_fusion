using Byn.Awrtc.Base;
using Byn.Awrtc.Unity;
using Byn.Awrtc;
using Byn.Unity.Examples;
using UnityEngine;

public class CustomCallApp : CallApp
{
    public FusionSignalingHandler signalingHandler;

    protected override ICall CreateCall(Byn.Awrtc.NetworkConfig netConfig)
    {
        CommonNetworkConfig configNew = new CommonNetworkConfig(netConfig);
        configNew.SignalingNetwork = signalingHandler.Signaler;
        return UnityCallFactory.Instance.Create(configNew);
    }
}
