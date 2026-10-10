# Workspace Clan Moderator

## Mục đích và phạm vi

`clan_moderator` là quyền vận hành có phạm vi giới hạn trong một Clan Mezon được cấu hình. Quyền này khác `moderator` toàn cục (soạn nội dung học) và `admin` (quản trị dịch vụ). Một người có thể giữ nhiều quyền, nhưng từng thao tác phải được kiểm tra theo quyền cần thiết.

Clan Moderator điều hành hoạt động học cộng đồng mà không sửa nội dung toàn hệ thống hoặc cấu hình bảo mật. Grant này chỉ áp dụng cho Clan duy nhất được cấu hình và được lưu tách biệt với role toàn cục.

## Workspace và điều hướng

| Khu vực | Nội dung |
| --- | --- |
| Tổng quan Clan | Thành viên hoạt động, quiz/học tập tuần và trạng thái lịch đăng |
| Thiết lập Bot | Kênh mặc định, múi giờ, Word of the Day, bật/tắt quiz và thời lượng |
| Lịch hoạt động | Tạo/tạm dừng lịch Word of the Day, quiz hằng ngày hoặc tổng kết tuần |
| Quiz cộng đồng | Chọn câu hỏi đã phát hành, xem thời gian/câu trả lời, kết thúc/hủy quiz và gửi giải thích |
| Hoạt động thành viên | Top học viên tuần và số liệu tổng hợp trong Clan được cấu hình |

Workspace phải hiển thị Clan và kênh hiện tại. Không cung cấp bộ chọn Clan, thao tác thêm Clan thứ hai hoặc tìm kiếm chéo Clan.

## Tính năng

- Cấu hình kênh mặc định, múi giờ, lịch Word of the Day (mặc định 08:00) và thời lượng quiz (mặc định 30 giây, giới hạn 5–300 giây).
- Lên lịch, tạm dừng, tiếp tục hoặc xóa lời nhắc; xem thời gian chạy tiếp theo và lần chạy gần nhất.
- Mở quiz cộng đồng từ ngân hàng câu hỏi đã phát hành; theo dõi bộ đếm thời gian và câu trả lời; đóng quiz rồi đăng đáp án/giải thích.
- Để Bot công bố người đúng nhanh nhất và áp dụng XP/thưởng tốc độ theo PRD, không cộng trùng.
- Xem hoạt động thành viên và top 10 tuần trong Clan; không đọc phân tích riêng hoặc hội thoại AI của học viên.
- Chỉ được cấp/thu hồi grant `clan_moderator` khi đồng thời có quyền Admin; riêng grant này không cho phép quản lý role.

## Yêu cầu chức năng

| Mã | Yêu cầu |
| --- | --- |
| CLM-01 | Yêu cầu grant `clan_moderator` còn hiệu lực; xác minh người gọi vẫn là thành viên Clan khi thao tác yêu cầu tư cách thành viên. |
| CLM-02 | Lấy Clan từ bản ghi cấu hình Bot duy nhất; từ chối request có Clan hoặc channel ID không khớp integration. |
| CLM-03 | Chỉ cho sửa thiết lập/lịch của Clan cấu hình; kiểm tra kênh thuộc Clan và dùng đúng múi giờ. |
| CLM-04 | Chỉ cho mở quiz từ câu hỏi đã phát hành; áp dụng thời lượng 5–300 giây, mặc định 30 giây. |
| CLM-05 | Mỗi thành viên chỉ gửi một câu trả lời trong một phiên; lưu thời điểm/độ trễ và tự đóng quiz khi hết hạn. |
| CLM-06 | Chọn người thắng trong các đáp án đúng theo thời gian phản hồi; cộng +10 XP cho đáp án đúng và +5 XP tốc độ cho người đúng nhanh nhất trong 5 giây đầu bằng thao tác idempotent. |
| CLM-07 | Chỉ đăng giải thích sau khi quiz đóng; ghi người mở/đóng/hủy và thời gian thực hiện. |
| CLM-08 | Chỉ hiển thị số liệu thành viên tổng hợp và bảng top 10 trong Clan đã cấu hình. |
| CLM-09 | Không cho Clan Moderator sửa câu hỏi/giáo trình toàn cục, cấp role toàn cục, đổi Clan ID, truy cập secret hoặc đọc hội thoại AI riêng. |
| CLM-10 | Ghi audit khi thay đổi thiết lập/lịch Bot hoặc hủy quiz thủ công; lưu người thực hiện, thời điểm và lý do khi có. |

## Tiêu chí nghiệm thu

1. Clan Moderator lên lịch Word of the Day trong kênh hợp lệ và xem được lần chạy tiếp theo.
2. Quiz tự kết thúc đúng hạn và không nhận câu trả lời thứ hai từ cùng một người.
3. Webhook hoặc yêu cầu gửi lại không thể cộng trùng XP quiz/thưởng tốc độ.
4. Thay Clan/channel ID trong request không cho phép thao tác ngoài Clan được cấu hình.
5. Nếu không có quyền bổ sung, Clan Moderator không thể sửa nội dung, quản lý role toàn cục hoặc đọc hội thoại LingLing riêng.
