# Introduction

This example demonstrates how Fusion 2 and WebRTC Video Chat can be integrated.
Fusion 2 manages game state and networking, while WebRTC provides a secondary connection for real-time audio and video communication.
Fusion 2 is also used for signaling, eliminating the need for a separate signaling server to establish the WebRTC connection.


# Setup
1. Clone repository and open the Unity 6 project in safe mode
2. Import Fusion 2 SDK  https://doc.photonengine.com/fusion/current/getting-started/sdk-download
3. Setup Fusion App ID https://doc.photonengine.com/fusion/current/getting-started/appid-instructions
4. Import WebRTC Video Chat https://assetstore.unity.com/packages/tools/network/webrtc-video-chat-68030

After that, no error should be visible. 

# Testing

1. Open scene Assets\RtcFusionExample\SampleScene.unity
2. Press Host (optionally tick audio / video to send media)
3. Open the scene in a player build in a second window or on a second device
4. Press Join (optionally tick audio / video to send media)
5. This will connect Fusion 2. Players can control the box using with W,S,A,D for testing
6. Press "Call Player:" to trigger WebRTC Video Chat

For simplicity, the call is handled by the existing ConferenceCallApp. Any real-world application should use the ICall API directly. 
