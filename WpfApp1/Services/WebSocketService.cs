using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace WpfApp1.Services
{
    public class WebSocketService
    {
        private ClientWebSocket _clientWebSocket;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public event Action<string> MessageReceived;

        // THAY ĐỔI 1: Thêm 'private set' để cho phép gán giá trị từ bên trong lớp này
        public bool IsConnected { get; private set; }

        public async Task ConnectAsync(string serverUrl, string userId)
        {
            // Nếu đang kết nối rồi thì không làm gì cả
            if (IsConnected) return;

            try
            {
                _clientWebSocket = new ClientWebSocket();
                // THAY ĐỔI 2: Sửa lỗi chính tả từ 'url' thành 'serverUrl'
                var uri = new Uri($"{serverUrl}/ws/{userId}");
                Debug.WriteLine($"[WebSocketService] Trying to connect to: {uri}");

                await _clientWebSocket.ConnectAsync(uri, _cancellationTokenSource.Token);

                // THAY ĐỔI 3: Gán giá trị cho IsConnected sau khi kết nối thành công
                IsConnected = _clientWebSocket.State == WebSocketState.Open;
                Debug.WriteLine($"[WebSocketService] Connection successful! State: {_clientWebSocket.State}");

                // Start listening for messages
                // THAY ĐỔI 4: Gọi đúng hàm ReceiveLoop đã được định nghĩa
                _ = ReceiveLoop();
            }
            catch (Exception ex)
            {
                // Quan trọng: Log lỗi ra để biết chuyện gì xảy ra
                Debug.WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Debug.WriteLine($"[WebSocketService] FAILED to connect: {ex.Message}");
                Debug.WriteLine($"!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");

                // THAY ĐỔI 5: Gán giá trị cho IsConnected khi kết nối thất bại
                IsConnected = false;
            }
        }

        private async Task ReceiveLoop()
        {
            var buffer = new byte[1024 * 4];
            try
            {
                while (_clientWebSocket != null && _clientWebSocket.State == WebSocketState.Open && !_cancellationTokenSource.IsCancellationRequested)
                {
                    var result = await _clientWebSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cancellationTokenSource.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await DisconnectAsync();
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Debug.WriteLine($"[WS] Message received: {message}");
                        MessageReceived?.Invoke(message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Bỏ qua lỗi này vì đây là hành động mong muốn khi ngắt kết nối
                Debug.WriteLine("[WS] Receive loop was canceled as requested.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WS] Error in receive loop: {ex.Message}");
                IsConnected = false; // Cập nhật trạng thái nếu có lỗi
            }
        }

        public async Task SendAsync(string message)
        {
            if (!IsConnected)
            {
                Debug.WriteLine("[WS] Cannot send message, not connected.");
                return;
            }
            try
            {
                var messageBuffer = Encoding.UTF8.GetBytes(message);
                await _clientWebSocket.SendAsync(new ArraySegment<byte>(messageBuffer), WebSocketMessageType.Text, true, _cancellationTokenSource.Token);
                Debug.WriteLine($"[WS] Message sent: {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WS] Error sending message: {ex.Message}");
                IsConnected = false;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_clientWebSocket != null)
            {
                _cancellationTokenSource.Cancel();
                if (_clientWebSocket.State == WebSocketState.Open)
                {
                    await _clientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                }
                _clientWebSocket.Dispose();
                _clientWebSocket = null;

                // THAY ĐỔI 6: Cập nhật IsConnected khi ngắt kết nối
                IsConnected = false;
                Debug.WriteLine("[WS] Disconnected.");
            }
        }
    }
}