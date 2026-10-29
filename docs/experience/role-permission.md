# Mô hình role và quyền hạn

## Danh sách role

Lingual có ba role toàn cục và một quyền riêng có phạm vi Clan. Quyền Clan không phải role toàn cục.

| Quyền | Phạm vi | Trách nhiệm chính | Workspace |
| --- | --- | --- | --- |
| `learner` | Tài khoản người học | Học, ôn tập, luyện quiz và theo dõi tiến độ cá nhân | Học viên |
| `moderator` | Nội dung toàn hệ thống | Quản lý giáo trình, từ vựng, ngân hàng quiz và báo cáo tổng hợp | Moderator |
| `admin` | Toàn bộ dịch vụ Lingual | Quản lý người dùng/role, tích hợp một Clan, thiết lập, bảo mật và vận hành | Admin |
| `clan_moderator` | Một Clan Mezon đã cấu hình | Điều hành Bot, lịch hoạt động và quiz cộng đồng | Clan Moderator |

`moderator` là tên mới của `Content Admin`; `admin` là tên mới của `Super Admin`. Quyền `clan_moderator` vẫn tách biệt vì chỉ cấp quyền vận hành cộng đồng trong Clan đã cấu hình.

## Ma trận năng lực

| Năng lực | Learner | Moderator | Clan Moderator | Admin |
| --- |:---:|:---:|:---:|:---:|
| Học nội dung đã phát hành, quản lý deck/SRS của mình | Có | Xem trước | Không | Có |
| Xem tiến độ riêng và hội thoại AI của mình | Có | Không | Không | Không mặc định |
| Tạo/sửa giáo trình và từ vựng | Không | Có | Không | Có |
| Tạo/sửa câu hỏi và phát hành nội dung | Không | Có | Không | Có |
| Nhập nội dung bằng CSV | Không | Có | Không | Có |
| Xem báo cáo retention tổng hợp | Tiến độ của mình | Có | Chỉ số Clan | Có |
| Tham gia/điều hành quiz Clan | Chỉ tham gia | Cần grant `clan_moderator` | Có | Có |
| Cấu hình lịch/kênh Bot | Không | Cần grant `clan_moderator` | Có | Có |
| Cấp/thu hồi role hoặc grant Clan | Không | Không | Không | Có |
| Đổi Clan cấu hình hoặc thiết lập bảo mật | Không | Không | Không | Có |
| Đọc giá trị thô OAuth/webhook secret | Không | Không | Không | Không; quản lý qua secret manager |

## Quy tắc phân quyền

1. Mỗi thao tác được kiểm tra quyền phía máy chủ; ẩn menu hoặc nút không thay thế kiểm tra quyền.
2. Role toàn cục lưu trực tiếp trong cột `users(role)` (Simple RBAC: `learner`, `moderator`, `admin`). Grant `clan_moderator` lưu trong bảng `clan_moderator_grants` và chỉ có hiệu lực trong Clan Bot duy nhất được cấu hình.
3. Một người có thể nhận nhiều quyền. Năng lực hiệu lực là hợp của các quyền được cấp, nhưng vẫn bị giới hạn theo phạm vi Clan và quy tắc riêng tư dữ liệu.
4. Sản phẩm chỉ hỗ trợ một Clan cho Bot; không role nào được tạo integration thứ hai hoặc truy vấn bảng xếp hạng liên Clan.
5. Admin có thể cấp/thu hồi role; mọi thay đổi nhạy cảm phải được audit vào bảng `audit_logs` (Module 09) và không được vô tình xóa Admin hoạt động cuối cùng.
6. Nội dung hội thoại AI riêng không xuất hiện trong dashboard của Moderator, Clan Moderator hoặc Admin theo mặc định.
7. Thu hồi quyền truy cập phù hợp khi tài khoản bị khóa, người dùng rời Clan cấu hình hoặc grant theo phạm vi bị thu hồi.

## Tài liệu theo role

- [Workspace Học viên](role/learner.md)
- [Workspace Moderator](role/moderator.md)
- [Workspace Clan Moderator](role/clan-moderator.md)
- [Workspace Admin](role/admin.md)
