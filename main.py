import asyncio
import json
from typing import Dict, List

from fastapi import FastAPI, WebSocket, WebSocketDisconnect

# Khởi tạo FastAPI app
app = FastAPI()

class ConnectionManager:
    """
    Lớp quản lý các kết nối WebSocket.
    Nó sẽ lưu trữ một dictionary ánh xạ từ user_id sang đối tượng WebSocket.
    """
    def __init__(self):
        # Dictionary để lưu các kết nối: { "user_id": WebSocket }
        self.active_connections: Dict[str, WebSocket] = {}

    async def connect(self, websocket: WebSocket, user_id: str):
        """Chấp nhận một kết nối mới và lưu trữ nó."""
        await websocket.accept()
        self.active_connections[user_id] = websocket
        print(f"INFO:    User '{user_id}' connected. Total connections: {len(self.active_connections)}")

    def disconnect(self, user_id: str):
        """Ngắt kết nối và xóa khỏi danh sách."""
        if user_id in self.active_connections:
            del self.active_connections[user_id]
            print(f"INFO:    User '{user_id}' disconnected. Total connections: {len(self.active_connections)}")

    async def send_personal_message(self, message: str, user_id: str):
        """Gửi tin nhắn đến một người dùng cụ thể."""
        if user_id in self.active_connections:
            websocket = self.active_connections[user_id]
            await websocket.send_text(message)
            print(f"INFO:    Sent message to '{user_id}'.")
        else:
            print(f"WARNING: User '{user_id}' not found. Message not sent.")

# Tạo một instance của ConnectionManager để sử dụng toàn cục
manager = ConnectionManager()

@app.websocket("/ws/{user_id}")
async def websocket_endpoint(websocket: WebSocket, user_id: str):
    """
    Đây là điểm cuối (endpoint) để client kết nối vào.
    Nó sẽ lắng nghe tin nhắn và chuyển tiếp đến người nhận tương ứng.
    """
    await manager.connect(websocket, user_id)
    try:
        while True:
            # Chờ nhận tin nhắn từ client (dưới dạng JSON text)
            data_text = await websocket.receive_text()
            data = json.loads(data_text)
            
            # Lấy thông tin người nhận từ message
            target_user_id = data.get("target")

            if not target_user_id:
                print(f"WARNING: Message from '{user_id}' is missing a 'target'.")
                continue

            print(f"INFO:    Relaying message from '{user_id}' to '{target_user_id}'. Type: {data.get('type')}")
            
            # Chuyển tiếp tin nhắn đến đúng người nhận
            await manager.send_personal_message(data_text, target_user_id)

    except WebSocketDisconnect:
        manager.disconnect(user_id)
    except Exception as e:
        print(f"ERROR:   An error occurred with user '{user_id}': {e}")
        manager.disconnect(user_id)

