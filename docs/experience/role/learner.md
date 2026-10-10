# Workspace Học viên

## Mục đích và phạm vi

Workspace Học viên giúp người dùng duy trì thói quen học ngoại ngữ qua bài học ngắn, ôn tập đúng hạn, luyện tập và hoạt động trong Clan. Người học đăng nhập bằng Mezon SSO; Lingual tạo hoặc cập nhật hồ sơ từ danh tính Mezon đã xác thực. Tiến độ học thuộc về từng người dùng, kể cả khi họ mở Lingual ngoài Clan được cấu hình.

## Workspace và điều hướng

Mezon Channel Mini-App là workspace trực quan chính. Các khu vực chính:

| Khu vực | Nội dung |
| --- | --- |
| Trang chủ | Kế hoạch học hôm nay, số từ đến hạn ôn, streak, XP và hành động nên làm tiếp theo |
| Học tập | Lộ trình khóa học → chương → bài học, nội dung bài và trạng thái hoàn thành |
| Ôn tập | Hàng đợi SRS trong ngày và thao tác đánh giá Again / Hard / Good / Easy |
| Từ vựng | Bộ từ cá nhân đã lưu, tìm kiếm và chi tiết thẻ từ |
| Luyện tập | Quiz đã phát hành, kết quả và chức năng Word Duel khả dụng |
| LingLing | Sửa ngữ pháp/chính tả và hội thoại theo kịch bản |
| Hồ sơ | Mục tiêu, thời lượng cam kết, trình độ, XP, streak, streak freeze, huy hiệu và biểu đồ hoạt động |
| Clan | Bảng xếp hạng thành viên, hoạt động quiz và thống kê gắn kết của Clan được cấu hình |

Bot cung cấp lối tắt trong Mezon chat: `/learn`, `/review`, `/streak`, `/profile`, `/quiz [topic]` và `/duel @username` khi được bật. Tin nhắn Bot, lịch nhắc, quiz cộng đồng và Word Duel qua Bot chỉ hoạt động trong **một Clan Mezon được cấu hình**. Tin nhắn riêng có thể trả về thẻ học hoặc kết quả ôn tập cá nhân, nhưng không tạo tích hợp Clan thứ hai.

## Tính năng

- **Onboarding:** chọn mục tiêu giao tiếp hằng ngày, công việc hoặc chứng chỉ; cam kết 5, 15 hoặc 30 phút/ngày; làm bài phân loại thích ứng 10 câu hoặc bỏ qua/tự chọn A1–B2.
- **Giáo trình:** học các khóa đã phát hành theo chương chủ đề và bài học, thường gồm 5–7 từ/cấu trúc mới.
- **Thẻ từ vựng:** xem từ, IPA, loại từ, nghĩa tiếng Việt, ví dụ song ngữ, âm thanh phát âm và hình minh họa nếu có. Lưu từ vào bộ cá nhân từ Lingual hoặc các điểm vào được hỗ trợ trong Clan chat.
- **Ôn tập ngắt quãng:** nhận thẻ đến hạn theo trạng thái SM-2 của từng người; đánh giá Again, Hard, Good hoặc Easy. Again được lặp lại ngay trong phiên; các mức khác điều chỉnh khoảng ôn tiếp theo.
- **Luyện tập:** làm quiz 4 lựa chọn và các dạng ghép đôi, sắp xếp câu, chính tả hoặc nghe-chép khi nội dung tương ứng đã được phát hành. Xem đáp án và giải thích sau khi trả lời.
- **Quiz cộng đồng:** trả lời câu hỏi Bot trong Clan được cấu hình trong thời gian quy định (mặc định 30 giây). Trả lời đúng nhận 10 XP; người đúng nhanh nhất trong 5 giây đầu nhận thêm 5 XP.
- **Word Duel:** thách đấu thành viên khác trong Clan được cấu hình qua 5 câu hỏi; kết quả tính theo độ chính xác và thời gian phản hồi.
- **Thói quen và phần thưởng:** duy trì streak khi kiếm được tối thiểu 20 XP trong ngày theo múi giờ của người học. Streak freeze có thể bảo vệ chuỗi khi bỏ lỡ ngày; các mốc phần thưởng là 7, 14 và 30 ngày.
- **LingLing:** nhận phản hồi khích lệ gồm điểm đúng, lỗi, cách diễn đạt tự nhiên và bản dịch tiếng Việt; hoặc luyện hội thoại theo kịch bản có sẵn.
- **Tiến độ:** xem heatmap tháng, số từ đã thuộc/đang học, từ yếu, hoạt động học và thứ hạng cá nhân hằng tuần trong Clan được cấu hình.

## Yêu cầu chức năng

| Mã | Yêu cầu |
| --- | --- |
| LRN-01 | Xác thực bằng Mezon SSO, kiểm tra danh tính từ nhà cung cấp và tạo/cập nhật hồ sơ Lingual mà không yêu cầu mật khẩu riêng. |
| LRN-02 | Lưu mục tiêu, thời lượng cam kết, múi giờ, lượt kiểm tra đầu vào, trình độ và trạng thái bỏ qua/tự chọn. |
| LRN-03 | Chỉ hiển thị nội dung giáo trình đã phát hành và lưu tiến độ, trạng thái hoàn thành từng bài. |
| LRN-04 | Cho phép lưu/bỏ lưu từ theo cách idempotent; hiển thị nguồn lưu nếu biết. |
| LRN-05 | Duy trì một thẻ SRS cho mỗi cặp người học–từ vựng; lưu số lần lặp, khoảng ôn, ease factor, thời điểm đến hạn, số lần quên và lịch sử ôn. |
| LRN-06 | Tạo hàng đợi từ các thẻ đã đến hạn và chưa tạm dừng; lưu mức đánh giá cùng lịch ôn mới. |
| LRN-07 | Lưu câu trả lời, tính đúng/sai, điểm, thời gian hoàn thành và trạng thái xem giải thích cho từng lượt quiz. |
| LRN-08 | Ghi XP vào ledger chỉ-ghi-thêm với idempotency key để retry không cộng trùng: +5 từ mới, +3 ôn SRS thành công, +25 hoàn thành bài học, +10 quiz Clan đúng, +5 thưởng tốc độ, +30/+10 cho thắng/tham gia Word Duel theo PRD. |
| LRN-09 | Tính điều kiện streak từ ngưỡng 20 XP trong ngày theo múi giờ người học; lưu số dư và lịch sử nhận/tiêu streak freeze. |
| LRN-10 | Chỉ cho thành viên Clan được cấu hình tham gia quiz Clan và Word Duel qua Bot; không có bộ chọn Clan hoặc bảng xếp hạng liên Clan. |
| LRN-11 | Lưu hội thoại AI theo người gửi; không cho học viên khác hoặc Clan Moderator đọc hội thoại riêng. |
| LRN-12 | Hiển thị phân tích cá nhân và top 10 học viên hằng tuần của Clan được cấu hình, không ngụ ý xếp hạng giữa các Clan Mezon. |

## Trạng thái rỗng và lỗi

- Người mới thấy onboarding và lựa chọn làm bài phân loại hoặc tự chọn trình độ. Nếu chưa có thẻ SRS đến hạn, gợi ý bài học tiếp theo thay vì hiển thị lỗi rỗng.
- Nếu trình độ đã chọn chưa có khóa học phát hành, giải thích tình trạng và chỉ ra trình độ khác đang có nội dung.
- Nếu xác thực Mezon thất bại, không tạo hồ sơ dở dang; cho phép thử lại và hiển thị lỗi an toàn.
- Nếu mạng gián đoạn khi làm quiz, giữ câu trả lời đã được máy chủ chấp nhận và thông báo lượt làm có thể tiếp tục hay không.
- Nếu AI không khả dụng hoặc bị giới hạn tần suất, giữ nội dung gửi khi phù hợp, cho phép thử lại và không cộng XP cho hoạt động chưa hoàn thành.
- Nếu người học không thuộc Clan được cấu hình, vô hiệu hóa thao tác Bot chỉ dành cho Clan và giải thích giới hạn này; bài học và SRS cá nhân vẫn dùng được.

## Tiêu chí nghiệm thu

1. Người dùng mới có thể đăng nhập, chọn/bỏ qua kiểm tra trình độ và mở thẻ từ đầu tiên trong dưới 30 giây khi Mezon phản hồi bình thường.
2. Sau khi hoàn thành bài học hoặc phiên ôn tập, tiến độ, XP và lịch đến hạn được cập nhật; retry không tạo XP trùng.
3. Người học chỉ xem được phân tích của mình và hoạt động cộng đồng trong phạm vi Clan; không đọc hồ sơ riêng hay hội thoại LingLing của người khác.
4. Màn hình tương tác đầu tiên của Mini-App tải dưới 1,5 giây theo mục tiêu sản phẩm và bố cục dùng được trên Mezon desktop/web.
