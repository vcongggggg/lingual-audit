# Workspace Admin

## Mục đích và phạm vi

Vai trò `admin` là quyền quản trị cao nhất của Lingual, thay cho tên `Super Admin`. Admin vận hành dịch vụ: quản lý người dùng và phân quyền, tích hợp Mezon đơn Clan, thiết lập toàn cục, bảo mật và tình trạng hoạt động.

Admin khác `clan_moderator` (điều hành hoạt động cộng đồng trong Clan) và `moderator` (quản lý nội dung học). Admin có thể cấp các quyền này nhưng có thể ủy quyền vận hành thường ngày. Mặc định, Admin không được đọc nội dung hội thoại LingLing riêng của học viên.

## Workspace và điều hướng

| Khu vực | Nội dung |
| --- | --- |
| Tổng quan | Sức khỏe dịch vụ, người học hoạt động, luồng onboarding, hoạt động học và cảnh báo |
| Người dùng và quyền | Tra cứu Mezon identity, trạng thái tài khoản, role hiện có, cấp/thu hồi quyền và audit |
| Tích hợp Mezon | Clan ID/tên duy nhất, kênh mặc định được phép, trạng thái cài đặt và đồng bộ |
| Vận hành Bot | Lịch Word of the Day, bật/tắt quiz, thời lượng, phiên đang mở, lỗi gửi và thao tác thử lại |
| Bảo mật | Trạng thái xác thực chữ ký webhook, cấu hình giới hạn tần suất, tham chiếu secret manager và sự kiện bảo mật |
| Thiết lập hệ thống | Múi giờ mặc định, feature flag, chính sách lưu dữ liệu và mục tiêu hiệu năng |
| Nhật ký kiểm toán | Thao tác quyền, tích hợp, cài đặt và vận hành, kèm người thực hiện/thời gian |

Hệ thống chỉ có một vị trí cấu hình Bot. Không cung cấp thao tác thêm Clan, chọn Clan hay xem bảng xếp hạng liên Clan. Mezon Clan ID được lưu trong bản ghi singleton `bot_configuration`, không có bảng thực thể `clans`. Giá trị secret được quản lý bên ngoài database và không trả về trình duyệt.

## Tính năng

- Tra cứu danh tính và trạng thái tài khoản; khóa/mở khóa tài khoản; cấp/thu hồi `learner`, `moderator`, `admin` hoặc grant `clan_moderator` có phạm vi riêng.
- Cấu hình một Bot integration, Clan mục tiêu, kênh mặc định, múi giờ, thời lượng quiz và lịch nhắc.
- Theo dõi đồng bộ thành viên Mezon và bảo đảm quiz, bảng xếp hạng thành viên, bài đăng định kỳ chỉ chạy trong Clan cấu hình.
- Theo dõi xác thực webhook, rate limit (mục tiêu PRD: không quá 5 request/giây/người dùng), độ trễ Bot, thời gian tải Mini-App, lỗi gửi và availability.
- Tra cứu audit và tổng quan vận hành. Thao tác nhạy cảm phải ghi lý do và audit entry.
- Quản lý feature flag và chính sách lưu trữ; trước khi áp dụng cần nêu rõ hành vi bị ảnh hưởng.

## Yêu cầu chức năng

| Mã | Yêu cầu |
| --- | --- |
| ADM-01 | Yêu cầu xác thực Mezon hợp lệ và quyền `admin` ở tất cả API quản trị; luôn kiểm tra quyền phía máy chủ. |
| ADM-02 | Cấp/thu hồi quyền có ghi người cấp, đối tượng, thời điểm và lý do; chặn tự nâng quyền và tránh xóa nhầm Admin hoạt động cuối cùng. |
| ADM-03 | Biểu diễn Bot integration bằng một bản ghi cấu hình (`id = 1`) và một Mezon Clan ID; từ chối tạo cấu hình thứ hai, không có luồng thiết lập đa Clan. |
| ADM-04 | Giới hạn thao tác Bot và báo cáo Clan vào Clan ID được cấu hình; xác thực Clan/kênh từ request trước khi xử lý. |
| ADM-05 | Hiển thị trạng thái integration và lần đồng bộ thành công gần nhất; báo thiếu cấu hình/quyền mà không lộ thông tin xác thực. |
| ADM-06 | Lưu webhook signing secret, OAuth token và credential nhà cung cấp trong secret manager; database/UI chỉ giữ tham chiếu hoặc trạng thái đã che, không lưu/hiển thị plaintext. |
| ADM-07 | Kiểm tra chữ ký webhook Mezon trước khi nhận sự kiện; từ chối chữ ký sai và ghi kết quả bảo mật nhưng không ghi secret vào log. |
| ADM-08 | Cấu hình và theo dõi sliding-window rate limit với mục tiêu 5 request/giây/người dùng; trả lỗi có thể thử lại khi vượt ngưỡng. |
| ADM-09 | Ghi audit cho thay đổi role, integration, moderation, cấu hình và thao tác nhạy cảm; người dùng ứng dụng không được sửa/xóa audit record. |
| ADM-10 | Theo dõi mục tiêu PRD: 500 người dùng hoạt động đồng thời, phản hồi Bot dưới 500 ms, màn hình tương tác đầu tiên dưới 1,5 giây, availability 99,5%. |
| ADM-11 | Cho phép tạm dừng/gỡ Bot an toàn; giữ tiến độ học và dữ liệu quiz/XP lịch sử khi integration tạm dừng. |
| ADM-12 | Không hiển thị nội dung hội thoại AI riêng trong dashboard Admin hoặc audit thông thường. Trường hợp truy cập đặc biệt phải được cấp quyền riêng, nêu lý do và ghi audit. |

## Ranh giới quyền

| Năng lực | Admin |
| --- | --- |
| Quản lý role toàn cục và trạng thái tài khoản | Có |
| Cấu hình tích hợp một Clan và hành vi Bot | Có |
| Thay đổi thiết lập toàn cục, xem tình trạng hệ thống/bảo mật | Có |
| Sửa giáo trình và ngân hàng câu hỏi | Có, có thể dùng workspace Moderator |
| Đọc nội dung hội thoại AI riêng | Không mặc định |
| Tạo hoặc vận hành tích hợp Clan thứ hai | Không, ngoài phạm vi sản phẩm |

## Tiêu chí nghiệm thu

1. Người không có quyền Admin không thể gọi API quản trị chỉ bằng cách đoán URL hoặc gửi request trực tiếp.
2. Admin gắn Bot với đúng một Clan; thao tác thêm Clan thứ hai bị từ chối.
3. Sự kiện từ Clan khác hoặc webhook sai chữ ký bị từ chối trước khi có thể cộng XP, mở quiz hay chạy tác vụ định kỳ.
4. Thay đổi quyền/cấu hình có đầy đủ người thực hiện, đối tượng, thời điểm và lý do trong audit.
5. Không có phản hồi UI, log hoặc trường database nào tiết lộ OAuth token hay webhook secret dạng thô.
