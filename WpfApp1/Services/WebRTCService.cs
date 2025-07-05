using Microsoft.MixedReality.WebRTC;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using WpfApp1.Models;
using Firebase.Database;
using Firebase.Database.Query;
using System.Reactive;
using System.Collections.Generic;

namespace WpfApp1.Services
{
    /// <summary>
    /// Encapsulates the WebRTC logic for creating and managing a peer-to-peer call.
    /// </summary>
    public class WebRTCService
    {
        private PeerConnection _peerConnection;
        private readonly FirebaseClient _firebaseClient;
        private readonly CallSignal _callSignal;

        public event Action<string> OnSdpOfferReady;
        public event Action<string> OnSdpAnswerReady;
        public event Action<IceCandidate> OnIceCandidateReady;
        public event Action OnConnectionClosed;

        public event Action<LocalVideoTrack> LocalVideoTrackReady;
        public event Action<RemoteVideoTrack> RemoteVideoTrackReady;

        public WebRTCService(FirebaseClient firebaseClient, CallSignal callSignal)
        {
            _firebaseClient = firebaseClient;
            _callSignal = callSignal;
        }
        public async Task InitializeAsync()
        {
            _peerConnection = new PeerConnection();

            var config = new PeerConnectionConfiguration
            {
                IceServers = new List<IceServer> {
                    new IceServer { Urls = { "stun:stun.l.google.com:19302" } }
                }
            };

            await _peerConnection.InitializeAsync(config);
            Debug.WriteLine("WebRTC: PeerConnection initialized.");

            // === BƯỚC 2: LẮNG NGHE KHI CÓ VIDEO TRACK TỪ NGƯỜI Ở XA ===
            _peerConnection.VideoTrackAdded += (RemoteVideoTrack track) =>
            {
                Debug.WriteLine($"WebRTC: Remote video track added - {track.Name}");
                RemoteVideoTrackReady?.Invoke(track);
            };

            _peerConnection.Connected += () => Debug.WriteLine("WebRTC: PeerConnection connected!");
            _peerConnection.IceStateChanged += (IceConnectionState newState) =>
            {
                Debug.WriteLine($"WebRTC: ICE state changed to {newState}.");
                if (newState == IceConnectionState.Closed || newState == IceConnectionState.Failed || newState == IceConnectionState.Disconnected)
                {
                    OnConnectionClosed?.Invoke();
                }
            };
        }

        public async Task CreateOfferAsync()
        {
            if (_peerConnection == null) await InitializeAsync();

            // Set up event handler for when local SDP is ready
            TaskCompletionSource<SdpMessage> tcs = new TaskCompletionSource<SdpMessage>();

            _peerConnection.LocalSdpReadytoSend += (SdpMessage sdp) => {
                if (sdp.Type == SdpMessageType.Offer)
                {
                    tcs.SetResult(sdp);
                }
            };

            // Create offer - this will trigger the LocalSdpReadytoSend event
            bool success = _peerConnection.CreateOffer();
            if (!success)
            {
                throw new InvalidOperationException("Failed to create SDP offer");
            }

            // Wait for the offer to be generated
            var offer = await tcs.Task;
            Debug.WriteLine("WebRTC: SDP Offer created.");
            OnSdpOfferReady?.Invoke(offer.Content);
        }
        public async Task AddAudioTrackAsync()
        {
            if (_peerConnection == null) await InitializeAsync();
            var audioTrackSource = await DeviceAudioTrackSource.CreateAsync();
            var audioTrack = LocalAudioTrack.CreateFromSource(audioTrackSource, new LocalAudioTrackInitConfig { trackName = "audio_track" });

            // SỬA LỖI: Sử dụng AddTransceiver cho phiên bản thư viện mới
            var audioTransceiver = _peerConnection.AddTransceiver(MediaKind.Audio);
            audioTransceiver.LocalAudioTrack = audioTrack;

            Debug.WriteLine("WebRTC: Audio track added.");
        }

        public async Task AddVideoTrackAsync()
        {
            if (_peerConnection == null) await InitializeAsync();
            var videoTrackSource = await DeviceVideoTrackSource.CreateAsync();
            var videoTrack = LocalVideoTrack.CreateFromSource(videoTrackSource, new LocalVideoTrackInitConfig { trackName = "video_track" });

            // SỬA LỖI: Sử dụng AddTransceiver cho phiên bản thư viện mới
            var videoTransceiver = _peerConnection.AddTransceiver(MediaKind.Video);
            videoTransceiver.LocalVideoTrack = videoTrack;

            LocalVideoTrackReady?.Invoke(videoTrack);
            Debug.WriteLine("WebRTC: Video track added.");
        }
        public async Task HandleOfferAsync(string sdpOffer)
        {
            if (_peerConnection == null) await InitializeAsync();

            var offer = new SdpMessage { Type = SdpMessageType.Offer, Content = sdpOffer };
            await _peerConnection.SetRemoteDescriptionAsync(offer);

            // Set up event handler for when local SDP answer is ready
            TaskCompletionSource<SdpMessage> tcs = new TaskCompletionSource<SdpMessage>();

            _peerConnection.LocalSdpReadytoSend += (SdpMessage sdp) => {
                if (sdp.Type == SdpMessageType.Answer)
                {
                    tcs.SetResult(sdp);
                }
            };

            // Create answer - this will trigger the LocalSdpReadytoSend event
            bool success = _peerConnection.CreateAnswer();
            if (!success)
            {
                throw new InvalidOperationException("Failed to create SDP answer");
            }

            // Wait for the answer to be generated
            var answer = await tcs.Task;
            Debug.WriteLine("WebRTC: SDP Answer created.");
            OnSdpAnswerReady?.Invoke(answer.Content);
        }

        public async Task HandleAnswerAsync(string sdpAnswer)
        {
            var answer = new SdpMessage { Type = SdpMessageType.Answer, Content = sdpAnswer };
            await _peerConnection.SetRemoteDescriptionAsync(answer);
            Debug.WriteLine("WebRTC: Remote description (Answer) set.");
        }
        public async Task AddIceCandidateAsync(IceCandidate candidate)
        {
            _peerConnection.AddIceCandidate(candidate);
            Debug.WriteLine($"WebRTC: Added remote ICE candidate: {candidate.Content}");
        }
        public void HangUp()
        {
            _peerConnection?.Close();
            _peerConnection = null;
            Debug.WriteLine("WebRTC: Connection closed.");
        }
    }
}