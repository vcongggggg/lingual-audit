# Đặc tả trải nghiệm Lingual

Các tài liệu này mô tả workspace, tính năng và yêu cầu chức năng theo role. Nguồn nghiệp vụ là `PRODUCT_REQUIREMENTS_DOCUMENT_LINGUAL.docx`; phạm vi tích hợp Bot được cố định vào một Clan Mezon duy nhất.

## Tài liệu

- [Mô hình role và quyền hạn](role-permission.md) — phạm vi quyền, ma trận năng lực và quy tắc phân quyền.
- [Workspace Học viên](role/learner.md) — onboarding, học, ôn tập, luyện tập, Bot và tiến độ.
- [Workspace Moderator](role/moderator.md) — giáo trình, từ vựng, câu hỏi, nhập dữ liệu và báo cáo tổng hợp.
- [Workspace Clan Moderator](role/clan-moderator.md) — cấu hình Bot, lịch hoạt động và quiz cộng đồng trong Clan cấu hình.
- [Workspace Admin](role/admin.md) — role toàn cục, tích hợp một Clan, thiết lập, bảo mật và vận hành.

## Ràng buộc chung

- Mezon SSO là nguồn danh tính; máy chủ thực thi phân quyền.
- Bot gắn với một Clan Mezon đã cấu hình; không hỗ trợ cài đặt đa Clan hoặc bảng xếp hạng liên Clan.
- `learner`, `moderator`, `admin` là role toàn cục; `clan_moderator` là grant có phạm vi riêng.
- OAuth token và webhook secret phải nằm trong secret manager, không lưu trong client hoặc database dạng văn bản thô.
- XP phải ghi idempotent; hành động quản trị/vận hành cần có audit.
- Mục tiêu hiệu năng theo PRD: Bot phản hồi dưới 500 ms, màn hình Mini-App đầu tiên tương tác được dưới 1,5 giây, 500 người hoạt động đồng thời và availability 99,5%.
