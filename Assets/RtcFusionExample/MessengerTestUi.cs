using TMPro;
using UnityEngine;

/// <summary>
/// Helper to manually test
/// FusionMessenger.
/// </summary>
public class MessengerTestUi : MonoBehaviour
{
    public TMP_Text _messageOutput;
    private FusionMessenger mSignaler;
    private void OnEnable()
    {
        mSignaler = this.GetComponent<FusionMessenger>();
        mSignaler.SignalingMessageReceived += Signaler_SignalingMessageReceived;
    }
    private void OnDisable()
    {
        mSignaler.SignalingMessageReceived -= Signaler_SignalingMessageReceived;
    }


    private void OnGUI()
    {
        if (this.mSignaler.IsAvailable == false)
            return;
        int i = 0;
        foreach (var player in this.mSignaler.ActivePlayers)
        {
            if (GUI.Button(new Rect(0, 100 + 40 * i, 200, 40), "" + player))
            {
                mSignaler.SendSignalingMessage("Hello from " + this.mSignaler.LocalPlayer, player);
            }
            i++;
        }
    }
    private void Signaler_SignalingMessageReceived(string message, Fusion.PlayerRef from)
    {

        Debug.Log("received: " + message);
        if (_messageOutput != null)
        {
            message = $"REC: {from}: {message}\n";
            _messageOutput.text += message;
        }
    }
}
