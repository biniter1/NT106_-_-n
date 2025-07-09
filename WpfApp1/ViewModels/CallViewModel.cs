using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.MixedReality.WebRTC;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfApp1.Services;

namespace WpfApp1.ViewModels
{
    public partial class CallViewModel : ObservableObject
    {
        [ObservableProperty]
        private WriteableBitmap _localVideoBitmap;

        [ObservableProperty]
        private WriteableBitmap _remoteVideoBitmap;

        public event EventHandler RequestClose;

        private readonly WebRTCService _webRTCService;

        private int _localFramesReceived = 0;
        private int _remoteFramesReceived = 0;

        public CallViewModel(WebRTCService webRTCService)
        {
            _webRTCService = webRTCService;
            _webRTCService.OnConnectionClosed += OnConnectionClosed;
            _webRTCService.LocalVideoTrackReady += OnLocalVideoTrackReady;
            _webRTCService.RemoteVideoTrackReady += OnRemoteVideoTrackReady;
        }

        private void OnLocalVideoTrackReady(LocalVideoTrack track)
        {
            Debug.WriteLine("[RENDER-DIAG] Local video track is ready. Subscribing to frames...");
            track.Argb32VideoFrameReady += OnLocalVideoFrameReady;
        }

        private void OnRemoteVideoTrackReady(RemoteVideoTrack track)
        {
            Debug.WriteLine("[RENDER-DIAG] Remote video track is ready. Subscribing to frames...");
            track.Argb32VideoFrameReady += OnRemoteVideoFrameReady;
        }

        /// <summary>
        /// Fixed method signature to match Argb32VideoFrameDelegate exactly
        /// </summary>
        private void OnLocalVideoFrameReady(Argb32VideoFrame frame)
        {
            _localFramesReceived++;
            if (_localFramesReceived % 100 == 1) // Log mỗi 100 frame để tránh spam
            {
                Debug.WriteLine($"[RENDER-DIAG] Receiving LOCAL video frame #{_localFramesReceived}. Updating bitmap...");
            }

            var width = (int)frame.width;
            var height = (int)frame.height;
            var stride = (int)frame.stride;
            var bufferSize = stride * height;

            if (bufferSize == 0) return;

            var buffer = new byte[bufferSize];
            Marshal.Copy(frame.data, buffer, 0, bufferSize);

            Application.Current.Dispatcher.Invoke(() =>
            {
                LocalVideoBitmap = UpdateBitmap(LocalVideoBitmap, width, height, stride, buffer);
            });
        }

        /// <summary>
        /// Fixed method signature to match Argb32VideoFrameDelegate exactly
        /// </summary>
        private void OnRemoteVideoFrameReady(Argb32VideoFrame frame)
        {
            _remoteFramesReceived++;
            if (_remoteFramesReceived % 100 == 1) // Log mỗi 100 frame để tránh spam
            {
                Debug.WriteLine($"[RENDER-DIAG] Receiving REMOTE video frame #{_remoteFramesReceived}. Updating bitmap...");
            }
            var width = (int)frame.width;
            var height = (int)frame.height;
            var stride = (int)frame.stride;
            var bufferSize = stride * height;

            if (bufferSize == 0) return;

            var buffer = new byte[bufferSize];
            Marshal.Copy(frame.data, buffer, 0, bufferSize);

            Application.Current.Dispatcher.Invoke(() =>
            {
                RemoteVideoBitmap = UpdateBitmap(RemoteVideoBitmap, width, height, stride, buffer);
            });
        }

        /// <summary>
        /// Common method to update a WriteableBitmap from frame data.
        /// </summary>
        private WriteableBitmap UpdateBitmap(WriteableBitmap bitmap, int width, int height, int stride, byte[] data)
        {
            if (bitmap == null || bitmap.PixelWidth != width || bitmap.PixelHeight != height)
            {
                bitmap = new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Bgra32, null);
            }

            bitmap.WritePixels(new Int32Rect(0, 0, width, height), data, stride, 0);
            return bitmap;
        }

        private void OnConnectionClosed()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        [RelayCommand]
        private void HangUp()
        {
            _webRTCService.HangUp();
        }
    }
}