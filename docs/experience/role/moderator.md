# Workspace Moderator

## Mục đích và phạm vi

Vai trò toàn cục `moderator` phụ trách nội dung học và theo dõi kết quả học tập ở mức tổng hợp. Đây là tên mới thay cho `Content Admin`. Vai trò này khác `clan_moderator`, vốn chỉ cho phép điều hành hoạt động Bot trong một Clan Mezon duy nhất; xem [Workspace Clan Moderator](clan-moderator.md).

Moderator quản lý khóa học, chương, bài học, từ vựng, ví dụ và ngân hàng câu hỏi. Moderator không được cấp vai trò toàn cục, đổi Clan Mezon đang cấu hình, quản lý bí mật hệ thống hoặc đọc nội dung hội thoại AI riêng của người học.

## Workspace và điều hướng

| Khu vực | Nội dung |
| --- | --- |
| Tổng quan nội dung | Số lượng bản nháp/đã phát hành, nội dung vừa sửa, trạng thái nhập liệu và lỗi kiểm tra |
| Khóa học | Thông tin khóa, cặp ngôn ngữ, trình độ A1–B2, trạng thái và thứ tự |
| Chương và bài học | Cấu trúc chủ đề, mô tả bài, thời lượng dự kiến và thứ tự từ vựng |
| Từ vựng | Từ, IPA, loại từ, nghĩa, ví dụ song ngữ, đường dẫn âm thanh và hình ảnh |
| Ngân hàng quiz | Dạng câu hỏi, lựa chọn/đáp án đúng, độ khó, trình độ, giải thích và trạng thái phát hành |
| Nhập hàng loạt | Tải CSV, ánh xạ cột, xem trước, lỗi theo dòng và kết quả nhập |
| Báo cáo học tập | Retention tổng hợp, mức hoàn thành nội dung, từ thường sai và thống kê Clan đã cấu hình |
| Lịch sử kiểm toán | Hành động nội dung và phát hành, kèm người thực hiện và thông tin thay đổi |

Workspace có thể mở từ giao diện quản trị Lingual hoặc điểm vào được cấp quyền trong Admin Console; mọi thao tác dùng danh tính Mezon đã xác minh và cùng cơ chế phân quyền phía máy chủ.

## Tính năng

- Tạo/sửa cấu trúc khóa học → chương → bài học với mã ổn định và thứ tự rõ ràng.
- Quản lý chi tiết từ vựng và nhiều ví dụ song ngữ; liên kết từ vào một hoặc nhiều bài.
- Soạn quiz trắc nghiệm, ghép đôi, sắp xếp câu, chính tả và nghe-chép; kiểm tra payload theo từng dạng trước khi phát hành.
- Nhập CSV có bước xem trước, kiểm tra từng dòng, phát hiện trùng lặp và báo cáo tổng kết.
- Lưu nháp, xem trước như học viên, phát hành nội dung hợp lệ và lưu trữ nội dung không còn dùng.
- Xem retention và mức độ gắn kết của Clan đã cấu hình; không so sánh hoặc báo cáo chéo nhiều Clan.
- Tra cứu nhật ký tạo, sửa, nhập, phát hành và lưu trữ nội dung.

## Yêu cầu chức năng

| Mã | Yêu cầu |
| --- | --- |
| MOD-01 | Kiểm tra quyền `moderator` tại từng API phía máy chủ; ẩn nút trên giao diện không được xem là cơ chế phân quyền. |
| MOD-02 | Tạo/sửa khóa học, chương, bài học và từ vựng, đồng thời bảo toàn khóa ngoại và thứ tự duy nhất trong phạm vi đối tượng cha. |
| MOD-03 | Kiểm tra trường bắt buộc của từ vựng và đường dẫn media; IPA/âm thanh/hình ảnh có thể tùy chọn; hỗ trợ nhiều ví dụ dịch. |
| MOD-04 | Tạo/sửa câu hỏi và lựa chọn; câu hỏi trắc nghiệm 4 đáp án phải có đúng một phương án đúng trước khi phát hành. |
| MOD-05 | Cho xem trước CSV trước khi ghi dữ liệu; báo dòng, trường và nguyên nhân từng lỗi; không phát hành một phần lô dữ liệu không hợp lệ. |
| MOD-06 | Hỗ trợ vòng đời nháp → đã phát hành → lưu trữ. Học viên chỉ truy vấn nội dung đã phát hành; lưu trữ không xóa lượt làm hay tiến độ cũ. |
| MOD-07 | Xem trước nội dung bằng cùng quy tắc hiển thị thẻ từ và câu hỏi của workspace Học viên. |
| MOD-08 | Cung cấp báo cáo tổng hợp retention, từ yếu, hoàn thành bài/quiz và thành viên hoạt động/tỷ lệ hoàn thành của Clan đã cấu hình; không lộ nội dung hội thoại AI hoặc định danh cá nhân không cần thiết. |
| MOD-09 | Ghi audit event khi tạo, sửa, nhập hàng loạt, phát hành hoặc lưu trữ: người thao tác, thời điểm, bản ghi, hành động và tóm tắt thay đổi. |
| MOD-10 | Từ chối thao tác cấu hình Bot, Clan, role, secret và giới hạn hệ thống nếu người gọi không có quyền Admin tương ứng. |

## Ranh giới quyền

| Năng lực | Moderator |
| --- | --- |
| Đọc/sửa khóa học, chương, bài, từ vựng, ngân hàng câu hỏi | Có |
| Nhập CSV, phát hành/lưu trữ nội dung | Có |
| Xem retention và báo cáo Clan tổng hợp | Có |
| Gửi lệnh Bot/quản lý lịch quiz Clan | Chỉ khi có thêm quyền `clan_moderator` |
| Cấp/thu hồi role toàn cục | Không |
| Đổi Clan cấu hình, secret hoặc cài đặt hệ thống | Không |
| Đọc hội thoại LingLing riêng | Không |

## Tiêu chí nghiệm thu

1. Moderator hoàn thiện một bài học và xem trước được ở giao diện học viên trước khi phát hành.
2. CSV không hợp lệ trả báo cáo lỗi theo dòng, không để lại lô dữ liệu phát hành một phần.
3. Học viên không tìm thấy bản nháp/lưu trữ bằng tìm kiếm hoặc gọi trực tiếp mã nội dung.
4. Mọi thay đổi nội dung hiển thị cho học viên đều có audit entry xác định được người thực hiện.
5. Báo cáo chỉ hiển thị dữ liệu tổng hợp phục vụ quyết định, không lộ hội thoại riêng hoặc dữ liệu ngoài phạm vi Clan.
