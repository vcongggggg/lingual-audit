# ⚡ BACKEND & API SERVICE (`src/server/`)

Thư mục này dành cho tầng Backend xử lý nghiệp vụ, RESTful API, xác thực OAuth/JWT và tương tác Cơ sở dữ liệu.

### Công nghệ khuyến nghị:
- **FastAPI (Python):** Hiệu năng cao, AsyncIO, tự động sinh tài liệu Swagger/OpenAPI.
- **Express.js (TypeScript):** Gọn nhẹ, thống nhất hệ sinh thái JS/TS với `mezon-sdk`.

### Cấu trúc khuyến nghị khi phát triển:
```
src/server/
├── controllers/       # Xử lý request & trả response
├── models/            # Entity / Schema database (PostgreSQL)
├── services/          # Business logic chính
├── middlewares/       # Rate limiting, Auth JWT, Logging, CORS
└── index.ts / main.py # Server entrypoint
```
