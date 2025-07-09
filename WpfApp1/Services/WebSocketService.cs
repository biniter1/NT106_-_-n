using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Generic;

namespace WpfApp1.Services
{
    public class WebSocketService
    {
        private ClientWebSocket _clientWebSocket;
        private CancellationTokenSource _cancellationTokenSource;

        public event Action<string> MessageReceived;
        public bool IsConnected { get; private set; }

        public async Task ConnectAsync(string serverUrl, string userId)
        {
            if (IsConnected) return;

            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                _clientWebSocket = new ClientWebSocket();
                var uri = new Uri($"{serverUrl}/ws/{userId}");
                Debug.WriteLine($"[WebSocketService] Trying to connect to: {uri}");

                await _clientWebSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

                IsConnected = _clientWebSocket.State == WebSocketState.Open;
                Debug.WriteLine($"[WebSocketService] Connection successful! State: {_clientWebSocket.State}");

                _ = ReceiveLoop(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WebSocketService] FAILED to connect: {ex.Message}");
                IsConnected = false;
            }
        }

        // ====================================================================
        // === SỬA LỖI QUAN TRỌNG NẰM TRONG HÀM NÀY ===
        private async Task ReceiveLoop(CancellationToken cancellationToken)
        {
            try
            {
                while (_clientWebSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
                {
                    // Sử dụng MemoryStream để ghép các mảnh tin nhắn lại
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        var bufferSegment = new ArraySegment<byte>(new byte[1024 * 4]);

                        // Vòng lặp bên trong để nhận tất cả các mảnh của một tin nhắn
                        do
                        {
                            result = await _clientWebSocket.ReceiveAsync(bufferSegment, cancellationToken);

                            if (result.MessageType == WebSocketMessageType.Close)
                            {
                                await DisconnectAsync();
                                return; // Thoát khỏi hàm
                            }

                            // Ghi mảnh nhận được vào MemoryStream
                            ms.Write(bufferSegment.Array, bufferSegment.Offset, result.Count);

                        } while (!result.EndOfMessage); // Lặp lại cho đến khi nhận được mảnh cuối cùng

                        // Sau khi đã nhận đủ, chuyển toàn bộ MemoryStream thành chuỗi
                        string message = Encoding.UTF8.GetString(ms.ToArray());

                        if (!string.IsNullOrEmpty(message))
                        {
                            Debug.WriteLine($"[WS] FULL Message received ({ms.Length} bytes): {message}");
                            MessageReceived?.Invoke(message);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.WriteLine("[WS] Receive loop was canceled as requested.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WS] Error in receive loop: {ex.Message}");
            }
            finally
            {
                IsConnected = false;
                Debug.WriteLine("[WS] Receive loop finished.");
            }
        }
        // ====================================================================


        public async Task SendAsync(string message)
        {
            if (!IsConnected) return;
            try
            {
                var messageBuffer = Encoding.UTF8.GetBytes(message);
                await _clientWebSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WS] Error sending message: {ex.Message}");
            }
        }

        public async Task DisconnectAsync()
        {
            if (_clientWebSocket != null)
            {
                IsConnected = false;
                _cancellationTokenSource?.Cancel();

                if (_clientWebSocket.State == WebSocketState.Open)
                {
                    await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }

                _clientWebSocket.Dispose();
                _clientWebSocket = null;
                _cancellationTokenSource?.Dispose();
                Debug.WriteLine("[WS] Disconnected.");
            }
        }
    }
}