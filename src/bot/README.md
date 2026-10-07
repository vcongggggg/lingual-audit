# 🤖 MEZON BOT MODULE (`src/bot/`)

Thư mục này dành riêng cho logic Mezon Bot.

### Cài đặt và SDK:
- **TypeScript / Node.js:** Sử dụng thư viện chính thức `mezon-sdk`:
  ```bash
  npm.cmd install mezon-sdk dotenv
  ```
- **Python:** Sử dụng Mezon Python SDK hoặc FastAPI Webhook handler.

### Cấu trúc khuyến nghị khi phát triển:
```
src/bot/
├── client.ts          # Khởi tạo kết nối Mezon Client & Gateway WebSocket
├── commands/          # Xử lý các lệnh Slash (/help, /status, /ping, ...)
├── events/            # Xử lý các sự kiện Clan (onMessage, onMemberJoin, ...)
├── handlers/          # Logic xử lý tương tác nút bấm, form, modal
└── index.ts           # Entrypoint khởi động Bot
```
