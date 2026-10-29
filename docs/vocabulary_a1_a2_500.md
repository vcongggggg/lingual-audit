# Bộ Dữ Liệu 500 Từ Vựng Tiếng Anh Cốt Lõi (CEFR A1–A2)

> Tài liệu đặc tả chuẩn hóa dữ liệu cho hệ thống **Lingual** (PostgreSQL Relational & JSONB Examples).

## 1. Thông Tin Tổng Quan

- **Chuẩn CEFR:** A1 – A2 (Oxford 3000 / Cambridge Key English Test)
- **Tổng số từ:** 500 từ vựng cốt lõi phân theo 15 chủ đề
- **Bảng CSDL:** `vocabulary_items` trong `curriculum.sql`
- **Khóa chính:** UUIDv5 tất định (`uuid5('en:<term>')`), đảm bảo tương thích tuyệt đối với `lessons.vocabulary_ids` và `srs_cards`

## 2. Bảng Thống Kê Phân Bổ Chủ Đề

| STT | Chủ đề | Số lượng từ | Cấp độ |
| :---: | :--- | :---: | :---: |
| 1 | Greetings & Communication | 35 | A1–A2 |
| 2 | Family & Relationships | 35 | A1–A2 |
| 3 | House & Home Objects | 35 | A1–A2 |
| 4 | Food, Drinks & Dining | 40 | A1–A2 |
| 5 | Clothes & Colors | 30 | A1–A2 |
| 6 | Daily Routines & Time | 35 | A1–A2 |
| 7 | Body & Health | 30 | A1–A2 |
| 8 | Education & Study | 35 | A1–A2 |
| 9 | Jobs & Workplace | 35 | A1–A2 |
| 10 | Travel & Transport | 40 | A1–A2 |
| 11 | Nature & Weather | 30 | A1–A2 |
| 12 | Hobbies & Sports | 35 | A1–A2 |
| 13 | Feelings & Personality | 30 | A1–A2 |
| 14 | Shopping & Money | 25 | A1–A2 |
| 15 | Modals & Connectors | 30 | A1–A2 |

## 3. Danh Sách Chi Tiết 500 Từ Vựng

| STT | Từ vựng | Phiên âm IPA | Từ loại | Nghĩa tiếng Việt | Ví dụ Anh - Việt |
| :---: | :--- | :--- | :---: | :--- | :--- |
| 1 | **hello** | `/həˈləʊ/` | *noun* | xin chào | **Hello, how are you today?**<br>_Xin chào, hôm nay bạn thế nào?_ |
| 2 | **goodbye** | `/ˌɡʊdˈbaɪ/` | *noun* | tạm biệt | **Goodbye, see you tomorrow!**<br>_Tạm biệt, hẹn gặp lại bạn vào ngày mai!_ |
| 3 | **please** | `/pliːz/` | *adverb* | làm ơn, vui lòng | **Please open the window.**<br>_Làm ơn mở cửa sổ ra._ |
| 4 | **thank** | `/θæŋk/` | *verb* | cảm ơn | **I want to thank you for your help.**<br>_Tôi muốn cảm ơn bạn vì sự giúp đỡ._ |
| 5 | **welcome** | `/ˈwel.kəm/` | *adjective* | được chào đón, không có chi | **You are always welcome here.**<br>_Bạn luôn được chào đón ở đây._ |
| 6 | **sorry** | `/ˈsɒr.i/` | *adjective* | xin lỗi, lấy làm tiếc | **I am sorry for being late.**<br>_Tôi xin lỗi vì đã đến muộn._ |
| 7 | **excuse** | `/ɪkˈskjuːz/` | *verb* | thứ lỗi, xin phép | **Excuse me, where is the station?**<br>_Xin lỗi, nhà ga ở đâu vậy?_ |
| 8 | **name** | `/neɪm/` | *noun* | tên | **My name is Linh.**<br>_Tên của tôi là Linh._ |
| 9 | **meet** | `/miːt/` | *verb* | gặp, gặp gỡ | **Nice to meet you.**<br>_Rất vui được gặp bạn._ |
| 10 | **speak** | `/spiːk/` | *verb* | nói (ngôn ngữ) | **Do you speak English?**<br>_Bạn có nói tiếng Anh không?_ |
| 11 | **talk** | `/tɔːk/` | *verb* | nói chuyện, trò chuyện | **We talk on the phone every night.**<br>_Chúng tôi trò chuyện qua điện thoại mỗi tối._ |
| 12 | **listen** | `/ˈlɪs.ən/` | *verb* | lắng nghe | **Listen carefully to the teacher.**<br>_Hãy lắng nghe giáo viên cẩn thận._ |
| 13 | **understand** | `/ˌʌn.dəˈstænd/` | *verb* | hiểu | **I do not understand this question.**<br>_Tôi không hiểu câu hỏi này._ |
| 14 | **repeat** | `/rɪˈpiːt/` | *verb* | lặp lại, nhắc lại | **Could you repeat that, please?**<br>_Bạn có thể nhắc lại điều đó được không?_ |
| 15 | **help** | `/help/` | *verb* | giúp đỡ | **Can you help me with my luggage?**<br>_Bạn có thể giúp tôi với hành lý này không?_ |
| 16 | **spell** | `/spel/` | *verb* | đánh vần | **How do you spell your last name?**<br>_Bạn đánh vần họ của mình như thế nào?_ |
| 17 | **introduce** | `/ˌɪn.trəˈdjuːs/` | *verb* | giới thiệu | **Let me introduce my friend to you.**<br>_Để tôi giới thiệu bạn của tôi với bạn._ |
| 18 | **friend** | `/frend/` | *noun* | người bạn | **She is my best friend.**<br>_Cô ấy là bạn thân nhất của tôi._ |
| 19 | **question** | `/ˈkwes.tʃən/` | *noun* | câu hỏi | **Do you have any questions?**<br>_Bạn có câu hỏi nào không?_ |
| 20 | **answer** | `/ˈɑːn.sər/` | *noun* | câu trả lời | **Write your answer on the paper.**<br>_Hãy viết câu trả lời của bạn vào giấy._ |
| 21 | **ask** | `/ɑːsk/` | *verb* | hỏi, yêu cầu | **You can ask the teacher for advice.**<br>_Bạn có thể hỏi xin giáo viên lời khuyên._ |
| 22 | **reply** | `/rɪˈplaɪ/` | *verb* | trả lời, phản hồi | **He did not reply to my email.**<br>_Anh ấy đã không phản hồi email của tôi._ |
| 23 | **explain** | `/ɪkˈspleɪn/` | *verb* | giải thích | **Can you explain this rule again?**<br>_Bạn có thể giải thích lại quy tắc này được không?_ |
| 24 | **meaning** | `/ˈmiː.nɪŋ/` | *noun* | ý nghĩa | **What is the meaning of this word?**<br>_Ý nghĩa của từ này là gì?_ |
| 25 | **language** | `/ˈlæŋ.ɡwɪdʒ/` | *noun* | ngôn ngữ, tiếng | **English is a global language.**<br>_Tiếng Anh là một ngôn ngữ toàn cầu._ |
| 26 | **call** | `/kɔːl/` | *verb* | gọi điện, gọi tên | **Please call me after 6 PM.**<br>_Vui lòng gọi cho tôi sau 6 giờ tối._ |
| 27 | **message** | `/ˈmes.ɪdʒ/` | *noun* | tin nhắn | **I sent you a text message.**<br>_Tôi đã gửi cho bạn một tin nhắn văn bản._ |
| 28 | **email** | `/ˈiː.meɪl/` | *noun* | thư điện tử | **Check your email for confirmation.**<br>_Kiểm tra email của bạn để xác nhận._ |
| 29 | **conversation** | `/ˌkɒn.vəˈseɪ.ʃən/` | *noun* | cuộc trò chuyện | **We had a long conversation about books.**<br>_Chúng tôi đã có một cuộc trò chuyện dài về sách._ |
| 30 | **agree** | `/əˈɡriː/` | *verb* | đồng ý | **I totally agree with your idea.**<br>_Tôi hoàn toàn đồng ý với ý kiến của bạn._ |
| 31 | **disagree** | `/ˌdɪs.əˈɡriː/` | *verb* | không đồng ý | **They disagree on the final plan.**<br>_Họ không đồng ý về kế hoạch cuối cùng._ |
| 32 | **say** | `/seɪ/` | *verb* | nói rằng | **What did he say to you?**<br>_Anh ấy đã nói gì với bạn?_ |
| 33 | **tell** | `/tel/` | *verb* | kể, bảo | **Tell me the truth.**<br>_Hãy nói cho tôi biết sự thật._ |
| 34 | **shout** | `/ʃaʊt/` | *verb* | hét lên, la lớn | **Don't shout in the library.**<br>_Đừng hét lên trong thư viện._ |
| 35 | **whisper** | `/ˈwɪs.pər/` | *verb* | nói thì thầm | **She whispered a secret in my ear.**<br>_Cô ấy thì thầm một bí mật vào tai tôi._ |
| 36 | **family** | `/ˈfæm.əl.i/` | *noun* | gia đình | **I have a large and happy family.**<br>_Tôi có một gia đình lớn và hạnh phúc._ |
| 37 | **parent** | `/ˈpeə.rənt/` | *noun* | bố mẹ, phụ huynh | **Both of my parents are teachers.**<br>_Cả bố và mẹ tôi đều là giáo viên._ |
| 38 | **father** | `/ˈfɑː.ðər/` | *noun* | bố, cha | **My father works in a hospital.**<br>_Bố tôi làm việc ở bệnh viện._ |
| 39 | **mother** | `/ˈmʌð.ər/` | *noun* | mẹ | **My mother cooks delicious meals.**<br>_Mẹ tôi nấu những bữa ăn rất ngon._ |
| 40 | **brother** | `/ˈbrʌð.ər/` | *noun* | anh trai, em trai | **He has two younger brothers.**<br>_Anh ấy có hai người em trai._ |
| 41 | **sister** | `/ˈsɪs.tər/` | *noun* | chị gái, em gái | **My sister is studying at university.**<br>_Em gái tôi đang học đại học._ |
| 42 | **son** | `/sʌn/` | *noun* | con trai | **They have a ten-year-old son.**<br>_Họ có một cậu con trai mười tuổi._ |
| 43 | **daughter** | `/ˈdɔː.tər/` | *noun* | con gái | **Their daughter loves painting.**<br>_Con gái của họ rất thích vẽ tranh._ |
| 44 | **husband** | `/ˈhʌz.bənd/` | *noun* | chồng | **Her husband is an engineer.**<br>_Chồng cô ấy là một kỹ sư._ |
| 45 | **wife** | `/waɪf/` | *noun* | vợ | **He bought flowers for his wife.**<br>_Anh ấy đã mua hoa tặng vợ mình._ |
| 46 | **child** | `/tʃaɪld/` | *noun* | đứa trẻ, con | **Every child needs love and care.**<br>_Mọi đứa trẻ đều cần tình yêu thương và sự chăm sóc._ |
| 47 | **baby** | `/ˈbeɪ.bi/` | *noun* | em bé | **The baby is sleeping peacefully.**<br>_Em bé đang ngủ rất yên lành._ |
| 48 | **grandfather** | `/ˈɡræn.fɑː.ðər/` | *noun* | ông nội, ông ngoại | **My grandfather tells great stories.**<br>_Ông tôi kể những câu chuyện rất hay._ |
| 49 | **grandmother** | `/ˈɡræn.mʌð.ər/` | *noun* | bà nội, bà ngoại | **My grandmother knits warm sweaters.**<br>_Bà tôi đan những chiếc áo len ấm áp._ |
| 50 | **uncle** | `/ˈʌŋ.kəl/` | *noun* | chú, bác, cậu | **My uncle lives in London.**<br>_Bác tôi sống ở Luân Đôn._ |
| 51 | **aunt** | `/ɑːnt/` | *noun* | cô, dì, bác gái | **Aunt Mary sent me a birthday card.**<br>_Dì Mary đã gửi cho tôi một tấm thiệp sinh nhật._ |
| 52 | **cousin** | `/ˈkʌz.ən/` | *noun* | anh chị em họ | **I often play video games with my cousin.**<br>_Tôi thường chơi trò chơi điện tử với anh họ._ |
| 53 | **person** | `/ˈpɜː.sən/` | *noun* | người, một người | **She is a kind and generous person.**<br>_Cô ấy là một người tốt bụng và hào phóng._ |
| 54 | **people** | `/ˈpiː.pəl/` | *noun* | mọi người, con người | **Many people visit the museum on weekends.**<br>_Nhiều người đến thăm bảo tàng vào cuối tuần._ |
| 55 | **man** | `/mæn/` | *noun* | đàn ông, người đàn ông | **A tall man opened the door for us.**<br>_Một người đàn ông cao lớn đã mở cửa cho chúng tôi._ |
| 56 | **woman** | `/ˈwʊm.ən/` | *noun* | phụ nữ, người phụ nữ | **She is a successful business woman.**<br>_Bà ấy là một nữ doanh nhân thành đạt._ |
| 57 | **boy** | `/bɔɪ/` | *noun* | cậu bé, bé trai | **The little boy is flying a kite.**<br>_Cậu bé đang thả diều._ |
| 58 | **girl** | `/ɡɜːl/` | *noun* | cô bé, bé gái | **That girl plays the violin very well.**<br>_Cô bé đó chơi vĩ cầm rất giỏi._ |
| 59 | **neighbor** | `/ˈneɪ.bər/` | *noun* | người hàng xóm | **Our neighbor has a friendly dog.**<br>_Người hàng xóm của chúng tôi có một chú chó thân thiện._ |
| 60 | **relative** | `/ˈrel.ə.tɪv/` | *noun* | họ hàng, người thân | **We visit our relatives during Tet holiday.**<br>_Chúng tôi đến thăm họ hàng vào dịp Tết._ |
| 61 | **adult** | `/ˈæd.ʌlt/` | *noun* | người lớn, người trưởng thành | **Tickets are cheaper for children than adults.**<br>_Vé cho trẻ em rẻ hơn vé cho người lớn._ |
| 62 | **teenager** | `/ˈtiːnˌeɪ.dʒər/` | *noun* | thanh thiếu niên (13-19 tuổi) | **Most teenagers like listening to pop music.**<br>_Hầu hết thanh thiếu niên đều thích nghe nhạc trẻ._ |
| 63 | **couple** | `/ˈkʌp.əl/` | *noun* | cặp đôi, vợ chồng | **The young couple just got married.**<br>_Cặp đôi trẻ vừa mới kết hôn._ |
| 64 | **guest** | `/ɡest/` | *noun* | khách, khách mời | **We invited thirty guests to the party.**<br>_Chúng tôi đã mời ba mươi vị khách đến bữa tiệc._ |
| 65 | **stranger** | `/ˈstreɪn.dʒər/` | *noun* | người lạ | **Do not accept gifts from a stranger.**<br>_Đừng nhận quà từ người lạ._ |
| 66 | **colleague** | `/ˈkɒl.iːɡ/` | *noun* | đồng nghiệp | **I have lunch with my colleagues every day.**<br>_Tôi ăn trưa với các đồng nghiệp mỗi ngày._ |
| 67 | **partner** | `/ˈpɑːt.nər/` | *noun* | đối tác, bạn đời | **She is looking for a tennis partner.**<br>_Cô ấy đang tìm một người bạn chơi quần vợt._ |
| 68 | **crowd** | `/kraʊd/` | *noun* | đám đông | **There was a huge crowd at the concert.**<br>_Có một đám đông khổng lồ tại buổi hòa nhạc._ |
| 69 | **single** | `/ˈsɪŋ.ɡəl/` | *adjective* | độc thân, đơn lẻ | **He is still single and lives alone.**<br>_Anh ấy vẫn độc thân và sống một mình._ |
| 70 | **married** | `/ˈmær.id/` | *adjective* | đã kết hôn | **They have been married for five years.**<br>_Họ đã kết hôn được năm năm._ |
| 71 | **house** | `/haʊs/` | *noun* | ngôi nhà | **They bought a new house with a garden.**<br>_Họ đã mua một ngôi nhà mới có sân vườn._ |
| 72 | **home** | `/həʊm/` | *noun* | tổ ấm, nhà | **After a busy day, I love relaxing at home.**<br>_Sau một ngày bận rộn, tôi thích thư giãn tại nhà._ |
| 73 | **apartment** | `/əˈpɑːt.mənt/` | *noun* | căn hộ | **We live in an apartment on the third floor.**<br>_Chúng tôi sống trong một căn hộ ở tầng ba._ |
| 74 | **room** | `/ruːm/` | *noun* | căn phòng | **This room has a lot of natural light.**<br>_Căn phòng này có rất nhiều ánh sáng tự nhiên._ |
| 75 | **bedroom** | `/ˈbed.ruːm/` | *noun* | phòng ngủ | **My bedroom is painted light blue.**<br>_Phòng ngủ của tôi được sơn màu xanh nhạt._ |
| 76 | **bathroom** | `/ˈbɑːθ.ruːm/` | *noun* | phòng tắm | **Please wash your hands in the bathroom.**<br>_Làm ơn rửa tay trong phòng tắm._ |
| 77 | **kitchen** | `/ˈkɪtʃ.ən/` | *noun* | nhà bếp | **Dad is cooking dinner in the kitchen.**<br>_Bố đang nấu bữa tối trong bếp._ |
| 78 | **living room** | `/ˈlɪv.ɪŋ ˌruːm/` | *noun* | phòng khách | **The family gathers in the living room every evening.**<br>_Gia đình sum họp ở phòng khách mỗi tối._ |
| 79 | **door** | `/dɔːr/` | *noun* | cửa ra vào | **Remember to lock the front door.**<br>_Nhớ khóa cửa trước nhé._ |
| 80 | **window** | `/ˈwɪn.dəʊ/` | *noun* | cửa sổ | **The sunlight comes through the open window.**<br>_Ánh nắng chiếu qua ô cửa sổ đang mở._ |
| 81 | **wall** | `/wɔːl/` | *noun* | bức tường | **They hung family photos on the wall.**<br>_Họ treo những bức ảnh gia đình trên tường._ |
| 82 | **floor** | `/flɔːr/` | *noun* | sàn nhà, tầng nhà | **The wooden floor is clean and shiny.**<br>_Sàn gỗ sạch bóng._ |
| 83 | **ceiling** | `/ˈsiː.lɪŋ/` | *noun* | trần nhà | **A crystal lamp hangs from the ceiling.**<br>_Một chiếc đèn pha lê treo trên trần nhà._ |
| 84 | **roof** | `/ruːf/` | *noun* | mái nhà | **The roof leaked during the heavy rain.**<br>_Mái nhà bị dột trong trận mưa lớn._ |
| 85 | **garden** | `/ˈɡɑː.dən/` | *noun* | khu vườn | **There are many colorful flowers in the garden.**<br>_Có nhiều bông hoa rực rỡ trong vườn._ |
| 86 | **balcony** | `/ˈbæl.kə.ni/` | *noun* | ban công | **I like drinking coffee on the balcony.**<br>_Tôi thích uống cà phê ngoài ban công._ |
| 87 | **furniture** | `/ˈfɜː.nɪ.tʃər/` | *noun* | đồ đạc nội thất | **They bought modern furniture for the new flat.**<br>_Họ đã mua đồ nội thất hiện đại cho căn hộ mới._ |
| 88 | **table** | `/ˈteɪ.bəl/` | *noun* | cái bàn | **Put the dishes on the dining table.**<br>_Đặt bát đĩa lên bàn ăn nhé._ |
| 89 | **chair** | `/tʃeər/` | *noun* | cái ghế | **Pull up a chair and join us.**<br>_Kéo ghế lại và ngồi cùng chúng tôi nào._ |
| 90 | **desk** | `/desk/` | *noun* | bàn làm việc, bàn học | **I study at my desk every evening.**<br>_Tôi học bài tại bàn học mỗi tối._ |
| 91 | **bed** | `/bed/` | *noun* | chiếc giường | **It is time to go to bed.**<br>_Đã đến lúc đi ngủ rồi._ |
| 92 | **sofa** | `/ˈsəʊ.fə/` | *noun* | ghế sô pha | **He fell asleep on the soft sofa.**<br>_Anh ấy đã ngủ thiếp đi trên chiếc ghế sô pha êm ái._ |
| 93 | **lamp** | `/læmp/` | *noun* | cây đèn | **Turn on the reading lamp, please.**<br>_Làm ơn bật chiếc đèn đọc sách lên._ |
| 94 | **mirror** | `/ˈmɪr.ər/` | *noun* | chiếc gương | **She checked her hair in the mirror.**<br>_Cô ấy nhìn gương để chỉnh lại tóc._ |
| 95 | **clock** | `/klɒk/` | *noun* | đồng hồ treo tường | **The wall clock shows quarter to nine.**<br>_Đồng hồ treo tường chỉ chín giờ kém mười lăm._ |
| 96 | **key** | `/kiː/` | *noun* | chìa khóa | **I lost my house keys yesterday.**<br>_Hôm qua tôi bị mất chìa khóa nhà._ |
| 97 | **doorbell** | `/ˈdɔː.bel/` | *noun* | chuông cửa | **Someone is ringing the doorbell.**<br>_Ai đó đang bấm chuông cửa kìa._ |
| 98 | **curtain** | `/ˈkɜː.tən/` | *noun* | rèm cửa | **Draw the curtains to keep out the hot sun.**<br>_Kéo rèm lại để chắn ánh nắng gắt nhé._ |
| 99 | **blanket** | `/ˈblæŋ.kɪt/` | *noun* | chiếc chăn | **Wrap this warm blanket around you.**<br>_Hãy quấn chiếc chăn ấm này quanh người._ |
| 100 | **pillow** | `/ˈpɪl.əʊ/` | *noun* | chiếc gối | **This pillow is very soft and comfortable.**<br>_Chiếc gối này rất mềm và thoải mái._ |
| 101 | **shelf** | `/ʃelf/` | *noun* | kệ, giá đỡ | **Put the dictionary back on the top shelf.**<br>_Để cuốn từ điển lại lên giá trên cùng nhé._ |
| 102 | **drawer** | `/drɔːr/` | *noun* | ngăn kéo | **The scissors are in the kitchen drawer.**<br>_Cây kéo nằm trong ngăn kéo nhà bếp._ |
| 103 | **fridge** | `/frɪdʒ/` | *noun* | tủ lạnh | **Keep milk and butter in the fridge.**<br>_Hãy để sữa và bơ trong tủ lạnh._ |
| 104 | **stove** | `/stəʊv/` | *noun* | bếp nấu | **The soup is boiling on the stove.**<br>_Nồi súp đang sôi trên bếp._ |
| 105 | **sink** | `/sɪŋk/` | *noun* | bồn rửa | **Do not leave dirty cups in the sink.**<br>_Đừng để cốc bẩn trong bồn rửa._ |
| 106 | **food** | `/fuːd/` | *noun* | thức ăn, thực phẩm | **Vietnamese food is fresh and healthy.**<br>_Thức ăn Việt Nam rất tươi và tốt cho sức khỏe._ |
| 107 | **water** | `/ˈwɔː.tər/` | *noun* | nước uống | **Drink at least two liters of water daily.**<br>_Uống ít nhất hai lít nước mỗi ngày._ |
| 108 | **bread** | `/bred/` | *noun* | bánh mì | **I usually have bread and eggs for breakfast.**<br>_Tôi thường ăn bánh mì và trứng cho bữa sáng._ |
| 109 | **rice** | `/raɪs/` | *noun* | cơm, gạo | **Rice is the main staple food in Asia.**<br>_Cơm là lương thực chính ở châu Á._ |
| 110 | **noodle** | `/ˈnuː.dəl/` | *noun* | mì, bún, phở | **He ordered a hot bowl of chicken noodles.**<br>_Anh ấy đã gọi một tô mì gà nóng hổi._ |
| 111 | **meat** | `/miːt/` | *noun* | thịt | **Do you eat red meat or poultry?**<br>_Bạn ăn thịt đỏ hay thịt gia cầm?_ |
| 112 | **beef** | `/biːf/` | *noun* | thịt bò | **This restaurant is famous for grilled beef.**<br>_Nhà hàng này nổi tiếng với món bò nướng._ |
| 113 | **chicken** | `/ˈtʃɪk.ɪn/` | *noun* | thịt gà, con gà | **We roasted a chicken for Sunday dinner.**<br>_Chúng tôi nướng một con gà cho bữa tối chủ nhật._ |
| 114 | **pork** | `/pɔːk/` | *noun* | thịt lợn, thịt heo | **Pork is widely consumed across the country.**<br>_Thịt lợn được tiêu thụ rộng rãi trên cả nước._ |
| 115 | **fish** | `/fɪʃ/` | *noun* | cá, món cá | **Eating fresh fish is good for your heart.**<br>_Ăn cá tươi rất tốt cho tim mạch._ |
| 116 | **egg** | `/eɡ/` | *noun* | trứng | **Boil two eggs for ten minutes.**<br>_Luộc hai quả trứng trong mười phút._ |
| 117 | **vegetable** | `/ˈvedʒ.tə.bəl/` | *noun* | rau củ | **Green vegetables are rich in vitamins.**<br>_Rau xanh rất giàu vitamin._ |
| 118 | **fruit** | `/fruːt/` | *noun* | trái cây, hoa quả | **Fresh fruit makes a great dessert.**<br>_Hoa quả tươi là món tráng miệng tuyệt vời._ |
| 119 | **apple** | `/ˈæp.əl/` | *noun* | quả táo | **An apple a day keeps the doctor away.**<br>_Mỗi ngày một quả táo giúp tránh xa bác sĩ._ |
| 120 | **banana** | `/bəˈnɑː.nə/` | *noun* | quả chuối | **Monkeys love ripe bananas.**<br>_Những chú khỉ rất thích chuối chín._ |
| 121 | **orange** | `/ˈɒr.ɪndʒ/` | *noun* | quả cam | **She drinks fresh orange juice every morning.**<br>_Cô ấy uống nước cam tươi mỗi sáng._ |
| 122 | **potato** | `/pəˈteɪ.təʊ/` | *noun* | củ khoai tây | **Mashed potatoes are my favorite side dish.**<br>_Khoai tây nghiền là món ăn kèm yêu thích của tôi._ |
| 123 | **tomato** | `/təˈmɑː.təʊ/` | *noun* | quả cà chua | **Add sliced tomatoes to the salad.**<br>_Cho thêm cà chua thái lát vào món salad._ |
| 124 | **onion** | `/ˈʌn.jən/` | *noun* | củ hành tây | **Chop the onion finely before frying.**<br>_Băm nhỏ hành tây trước khi phi thơm._ |
| 125 | **soup** | `/suːp/` | *noun* | món canh, súp | **Hot soup warms you up in winter.**<br>_Súp nóng làm ấm người vào mùa đông._ |
| 126 | **salad** | `/ˈsæl.əd/` | *noun* | món rau trộn, salad | **We made a mixed salad with olive oil.**<br>_Chúng tôi làm món salad trộn với dầu ô liu._ |
| 127 | **cheese** | `/tʃiːz/` | *noun* | phô mai | **French people are proud of their cheese.**<br>_Người Pháp rất tự hào về phô mai của họ._ |
| 128 | **milk** | `/mɪlk/` | *noun* | sữa tươi | **A glass of warm milk helps you sleep.**<br>_Một ly sữa ấm giúp bạn dễ ngủ hơn._ |
| 129 | **tea** | `/tiː/` | *noun* | trà, chè | **Would you like a cup of green tea?**<br>_Bạn có muốn dùng một tách trà xanh không?_ |
| 130 | **coffee** | `/ˈkɒf.i/` | *noun* | cà phê | **He drinks strong black coffee without sugar.**<br>_Anh ấy uống cà phê đen đậm đặc không đường._ |
| 131 | **juice** | `/dʒuːs/` | *noun* | nước ép hoa quả | **Apple juice is sweet and refreshing.**<br>_Nước ép táo ngọt và mát lành._ |
| 132 | **sugar** | `/ˈʃʊɡ.ər/` | *noun* | đường ăn | **Too much sugar is harmful to your teeth.**<br>_Quá nhiều đường sẽ có hại cho răng của bạn._ |
| 133 | **salt** | `/sɒlt/` | *noun* | muối ăn | **Pass me the salt shaker, please.**<br>_Làm ơn chuyển cho tôi lọ muối._ |
| 134 | **pepper** | `/ˈpep.ər/` | *noun* | hạt tiêu, ớt chuông | **Grind black pepper over the steak.**<br>_Xay hạt tiêu đen lên miếng bít tết._ |
| 135 | **butter** | `/ˈbʌt.ər/` | *noun* | bơ | **Spread some butter on your toast.**<br>_Phết một ít bơ lên lát bánh mì nướng của bạn._ |
| 136 | **cake** | `/keɪk/` | *noun* | bánh ngọt, bánh kem | **She baked a chocolate cake for his birthday.**<br>_Cô ấy nướng một chiếc bánh sô cô la cho sinh nhật anh._ |
| 137 | **biscuit** | `/ˈbɪs.kɪt/` | *noun* | bánh quy | **Have a biscuit with your afternoon tea.**<br>_Hãy ăn một chiếc bánh quy kèm trà chiều nhé._ |
| 138 | **breakfast** | `/ˈbrek.fəst/` | *noun* | bữa ăn sáng | **Never skip breakfast before work.**<br>_Đừng bao giờ bỏ bữa sáng trước khi đi làm._ |
| 139 | **lunch** | `/lʌntʃ/` | *noun* | bữa ăn trưa | **Let's meet for lunch at noon.**<br>_Hãy gặp nhau ăn trưa lúc 12 giờ trưa nhé._ |
| 140 | **dinner** | `/ˈdɪn.ər/` | *noun* | bữa ăn tối | **The whole family eats dinner together.**<br>_Cả gia đình cùng ăn bữa tối với nhau._ |
| 141 | **meal** | `/miːl/` | *noun* | bữa ăn | **We had a delicious three-course meal.**<br>_Chúng tôi đã có một bữa ăn ba món ngon tuyệt._ |
| 142 | **restaurant** | `/ˈres.trɒnt/` | *noun* | nhà hàng, quán ăn | **They reserved a table at an Italian restaurant.**<br>_Họ đã đặt bàn ở một nhà hàng Ý._ |
| 143 | **menu** | `/ˈmen.juː/` | *noun* | thực đơn | **Can we see the dessert menu, please?**<br>_Làm ơn cho chúng tôi xem thực đơn món tráng miệng._ |
| 144 | **order** | `/ˈɔː.dər/` | *verb* | gọi món, đặt hàng | **Are you ready to order your drinks?**<br>_Quý khách đã sẵn sàng gọi đồ uống chưa?_ |
| 145 | **taste** | `/teɪst/` | *verb* | nếm, có vị | **This soup tastes rich and spicy.**<br>_Món súp này có vị đậm đà và cay cay._ |
| 146 | **clothes** | `/kləʊðz/` | *noun* | quần áo, trang phục | **Put on your warm clothes before going outside.**<br>_Mặc quần áo ấm vào trước khi ra ngoài nhé._ |
| 147 | **shirt** | `/ʃɜːt/` | *noun* | áo sơ mi | **He wore a crisp white shirt to the interview.**<br>_Anh ấy mặc một chiếc áo sơ mi trắng tinh đến buổi phỏng vấn._ |
| 148 | **t-shirt** | `/ˈtiː.ʃɜːt/` | *noun* | áo thun, áo phông | **A cotton t-shirt is comfortable in summer.**<br>_Một chiếc áo thun cotton rất thoải mái vào mùa hè._ |
| 149 | **trousers** | `/ˈtraʊ.zəz/` | *noun* | quần tây, quần dài | **These dark trousers match your jacket.**<br>_Chiếc quần dài màu tối này rất hợp với áo khoác của bạn._ |
| 150 | **jeans** | `/dʒiːnz/` | *noun* | quần bò, quần jean | **Blue jeans are popular all over the world.**<br>_Quần jean xanh phổ biến trên toàn thế giới._ |
| 151 | **dress** | `/dres/` | *noun* | váy liền, đầm | **She bought an elegant evening dress.**<br>_Cô ấy đã mua một chiếc đầm dạ hội thanh lịch._ |
| 152 | **skirt** | `/skɜːt/` | *noun* | chân váy | **The school uniform includes a pleated skirt.**<br>_Đồng phục học sinh bao gồm một chiếc chân váy xếp ly._ |
| 153 | **jacket** | `/ˈdʒæk.ɪt/` | *noun* | áo khoác ngắn | **Zip up your jacket; the wind is cold.**<br>_Kéo khóa áo khoác lại; gió lạnh lắm._ |
| 154 | **coat** | `/kəʊt/` | *noun* | áo khoác dài, áo măng tô | **A heavy wool coat protects you from snow.**<br>_Chiếc áo măng tô len dày giúp bạn tránh khỏi tuyết._ |
| 155 | **sweater** | `/ˈswet.ər/` | *noun* | áo len | **My grandma knitted this cozy sweater.**<br>_Bà tôi đã đan chiếc áo len ấm cúng này._ |
| 156 | **shoe** | `/ʃuː/` | *noun* | chiếc giày | **Take off your shoes before entering the temple.**<br>_Hãy cởi giày trước khi bước vào đền._ |
| 157 | **boot** | `/buːt/` | *noun* | đôi bốt, ủng | **Leather boots keep your feet dry in the rain.**<br>_Đôi bốt da giữ cho chân bạn luôn khô ráo dưới mưa._ |
| 158 | **sock** | `/sɒk/` | *noun* | chiếc tất, vớ | **He is wearing thick woolen socks.**<br>_Anh ấy đang đi đôi tất len dày._ |
| 159 | **hat** | `/hæt/` | *noun* | chiếc mũ, nón | **Wear a wide sun hat at the beach.**<br>_Hãy đội một chiếc mũ rộng vành che nắng ở bãi biển._ |
| 160 | **cap** | `/kæp/` | *noun* | mũ lưỡi trai | **The baseball player adjusted his cap.**<br>_Cầu thủ bóng chày chỉnh lại chiếc mũ lưỡi trai của mình._ |
| 161 | **scarf** | `/skɑːf/` | *noun* | khăn quàng cổ | **Wrap a silk scarf around your neck.**<br>_Quàng một chiếc khăn lụa quanh cổ bạn._ |
| 162 | **glove** | `/ɡlʌv/` | *noun* | chiếc găng tay, bao tay | **Wear warm gloves in winter weather.**<br>_Hãy đeo găng tay ấm trong thời tiết mùa đông._ |
| 163 | **belt** | `/belt/` | *noun* | thắt lưng, dây nịt | **He fastened his black leather belt.**<br>_Anh ấy thắt chiếc thắt lưng da màu đen._ |
| 164 | **pocket** | `/ˈpɒk.ɪt/` | *noun* | túi quần, túi áo | **I keep my house keys in my jacket pocket.**<br>_Tôi cất chìa khóa nhà trong túi áo khoác._ |
| 165 | **suit** | `/suːt/` | *noun* | bộ comple, bộ vest | **He looked handsome in his wedding suit.**<br>_Anh ấy trông rất bảnh bao trong bộ vest cưới._ |
| 166 | **color** | `/ˈkʌl.ər/` | *noun* | màu sắc | **What is your favorite color?**<br>_Màu sắc yêu thích của bạn là màu gì?_ |
| 167 | **red** | `/red/` | *adjective* | màu đỏ | **She applied red lipstick for the party.**<br>_Cô ấy thoa son đỏ cho bữa tiệc._ |
| 168 | **blue** | `/bluː/` | *adjective* | màu xanh lam, xanh nước biển | **The deep ocean water is bright blue.**<br>_Nước biển sâu có màu xanh lam rực rỡ._ |
| 169 | **green** | `/ɡriːn/` | *adjective* | màu xanh lá cây | **Spring leaves turn fresh green.**<br>_Lá mùa xuân chuyển sang màu xanh lá non tươi._ |
| 170 | **yellow** | `/ˈjel.əʊ/` | *adjective* | màu vàng | **Sunflowers have bright yellow petals.**<br>_Hoa hướng dương có những cánh hoa màu vàng tươi._ |
| 171 | **black** | `/blæk/` | *adjective* | màu đen | **He always wears a stylish black watch.**<br>_Anh ấy luôn đeo một chiếc đồng hồ màu đen phong cách._ |
| 172 | **white** | `/waɪt/` | *adjective* | màu trắng | **Clouds in the blue sky are fluffy white.**<br>_Những đám mây trên bầu trời xanh có màu trắng bồng bềnh._ |
| 173 | **brown** | `/braʊn/` | *adjective* | màu nâu | **The puppy has shiny brown fur.**<br>_Chú cún con có bộ lông màu nâu óng ả._ |
| 174 | **grey** | `/ɡreɪ/` | *adjective* | màu xám | **The autumn sky looks overcast and grey.**<br>_Bầu trời mùa thu trông u ám và xám xịt._ |
| 175 | **pink** | `/pɪŋk/` | *adjective* | màu hồng | **Cherry blossoms are delicate and pink.**<br>_Hoa anh đào mang sắc hồng thanh tao._ |
| 176 | **wake** | `/weɪk/` | *verb* | thức dậy, đánh thức | **I wake up at six every morning.**<br>_Tôi thức dậy lúc sáu giờ mỗi sáng._ |
| 177 | **sleep** | `/sliːp/` | *verb* | ngủ | **Adults need to sleep seven hours a night.**<br>_Người lớn cần ngủ bảy tiếng một đêm._ |
| 178 | **wash** | `/wɒʃ/` | *verb* | rửa, giặt | **Always wash your hands before eating.**<br>_Luôn rửa tay trước khi ăn._ |
| 179 | **brush** | `/brʌʃ/` | *verb* | chải (tóc, răng) | **Brush your teeth twice a day.**<br>_Hãy đánh răng hai lần mỗi ngày._ |
| 180 | **shower** | `/ˈʃaʊ.ər/` | *noun* | vòi sen, tắm vòi sen | **I take a warm shower before going to bed.**<br>_Tôi tắm vòi sen nước ấm trước khi đi ngủ._ |
| 181 | **clean** | `/kliːn/` | *verb* | dọn dẹp, lau chùi | **We clean our house every Saturday.**<br>_Chúng tôi dọn dẹp nhà cửa vào mỗi thứ Bảy._ |
| 182 | **cook** | `/kʊk/` | *verb* | nấu ăn, nấu nướng | **She loves to cook for her family.**<br>_Cô ấy thích nấu ăn cho gia đình mình._ |
| 183 | **relax** | `/rɪˈlæks/` | *verb* | thư giãn, nghỉ ngơi | **Music helps me relax after work.**<br>_Âm nhạc giúp tôi thư giãn sau giờ làm việc._ |
| 184 | **rest** | `/rest/` | *verb* | nghỉ ngơi, tĩnh dưỡng | **Sit down and rest your legs.**<br>_Hãy ngồi xuống và để đôi chân được nghỉ ngơi._ |
| 185 | **habit** | `/ˈhæb.ɪt/` | *noun* | thói quen | **Reading books is a wonderful daily habit.**<br>_Đọc sách là một thói quen hàng ngày tuyệt vời._ |
| 186 | **routine** | `/ruːˈtiːn/` | *noun* | lịch trình hàng ngày | **Exercise is part of my morning routine.**<br>_Tập thể dục là một phần trong thói quen buổi sáng của tôi._ |
| 187 | **time** | `/taɪm/` | *noun* | thời gian, giờ giấc | **Do you have time for a quick chat?**<br>_Bạn có thời gian trò chuyện nhanh một chút không?_ |
| 188 | **hour** | `/aʊər/` | *noun* | tiếng, giờ đồng hồ | **The flight lasts about two hours.**<br>_Chuyến bay kéo dài khoảng hai tiếng đồng hồ._ |
| 189 | **minute** | `/ˈmɪn.ɪt/` | *noun* | phút | **Wait here for five minutes, please.**<br>_Làm ơn hãy đợi ở đây năm phút._ |
| 190 | **second** | `/ˈsek.ənd/` | *noun* | giây | **Just give me a second to finish this.**<br>_Chỉ cho tôi một giây để hoàn thành việc này thôi._ |
| 191 | **morning** | `/ˈmɔː.nɪŋ/` | *noun* | buổi sáng | **The morning air is crisp and cool.**<br>_Không khí buổi sáng thật trong lành và mát mẻ._ |
| 192 | **afternoon** | `/ˌɑːf.təˈnuːn/` | *noun* | buổi chiều | **We have a team meeting this afternoon.**<br>_Chúng tôi có cuộc họp nhóm vào chiều nay._ |
| 193 | **evening** | `/ˈiːv.nɪŋ/` | *noun* | buổi tối | **I enjoy walking in the park every evening.**<br>_Tôi thích đi dạo trong công viên mỗi buổi tối._ |
| 194 | **night** | `/naɪt/` | *noun* | ban đêm | **The city lights look stunning at night.**<br>_Ánh đèn thành phố trông thật lộng lẫy vào ban đêm._ |
| 195 | **midnight** | `/ˈmɪd.naɪt/` | *noun* | nửa đêm (12h đêm) | **The train reached the station around midnight.**<br>_Chuyến tàu đã đến ga vào khoảng nửa đêm._ |
| 196 | **noon** | `/nuːn/` | *noun* | buổi trưa (12h trưa) | **The sun is highest at noon.**<br>_Mặt trời lên cao nhất vào buổi trưa._ |
| 197 | **today** | `/təˈdeɪ/` | *noun* | hôm nay | **Today is sunny and warm.**<br>_Hôm nay trời nắng và ấm áp._ |
| 198 | **tomorrow** | `/təˈmɒr.əʊ/` | *noun* | ngày mai | **See you at the conference tomorrow.**<br>_Hẹn gặp lại bạn tại hội nghị vào ngày mai._ |
| 199 | **yesterday** | `/ˈjes.tə.deɪ/` | *noun* | hôm qua | **Yesterday was a rainy day.**<br>_Hôm qua là một ngày mưa._ |
| 200 | **day** | `/deɪ/` | *noun* | ngày, ban ngày | **Have a nice day at school!**<br>_Chúc một ngày vui vẻ ở trường nhé!_ |
| 201 | **week** | `/wiːk/` | *noun* | tuần lễ | **There are seven days in a week.**<br>_Có bảy ngày trong một tuần._ |
| 202 | **weekend** | `/ˌwiːkˈend/` | *noun* | cuối tuần | **What are your plans for the weekend?**<br>_Kế hoạch cho cuối tuần của bạn là gì?_ |
| 203 | **month** | `/mʌnθ/` | *noun* | tháng | **February is the shortest month of the year.**<br>_Tháng Hai là tháng ngắn nhất trong năm._ |
| 204 | **year** | `/jɪər/` | *noun* | năm | **We moved to Hanoi three years ago.**<br>_Chúng tôi chuyển đến Hà Nội ba năm trước._ |
| 205 | **century** | `/ˈsen.tʃər.i/` | *noun* | thế kỷ | **We are living in the twenty-first century.**<br>_Chúng ta đang sống trong thế kỷ hai mươi mốt._ |
| 206 | **early** | `/ˈɜː.li/` | *adverb* | sớm | **She arrived early to prepare the slides.**<br>_Cô ấy đến sớm để chuẩn bị các trang trình chiếu._ |
| 207 | **late** | `/leɪt/` | *adverb* | muộn, trễ | **Hurry up or we will be late for work.**<br>_Nhanh lên nếu không chúng ta sẽ muộn làm đấy._ |
| 208 | **now** | `/naʊ/` | *adverb* | bây giờ, ngay lúc này | **We are starting our lesson right now.**<br>_Chúng ta bắt đầu bài học ngay bây giờ._ |
| 209 | **soon** | `/suːn/` | *adverb* | sớm, chẳng bao lâu nữa | **The train will arrive soon.**<br>_Chuyến tàu sẽ đến sớm thôi._ |
| 210 | **already** | `/ɔːlˈred.i/` | *adverb* | đã, rồi | **I have already finished my homework.**<br>_Tôi đã hoàn thành xong bài tập về nhà rồi._ |
| 211 | **body** | `/ˈbɒd.i/` | *noun* | cơ thể, thân thể | **Regular exercise keeps your body fit.**<br>_Tập thể dục thường xuyên giữ cho cơ thể bạn khỏe mạnh._ |
| 212 | **head** | `/hed/` | *noun* | đầu | **Wear a helmet to protect your head.**<br>_Hãy đội mũ bảo hiểm để bảo vệ phần đầu._ |
| 213 | **face** | `/feɪs/` | *noun* | khuôn mặt | **She greeted us with a smile on her face.**<br>_Cô ấy chào đón chúng tôi với một nụ cười trên mặt._ |
| 214 | **eye** | `/aɪ/` | *noun* | con mắt | **He has clear hazel eyes.**<br>_Anh ấy có đôi mắt màu hạt dẻ trong sáng._ |
| 215 | **ear** | `/ɪər/` | *noun* | tai, lỗ tai | **Loud music can damage your ears.**<br>_Âm nhạc quá lớn có thể làm hỏng tai của bạn._ |
| 216 | **nose** | `/nəʊz/` | *noun* | mũi | **She touched her nose with her finger.**<br>_Cô ấy chạm ngón tay vào mũi._ |
| 217 | **mouth** | `/maʊθ/` | *noun* | miệng | **Open your mouth so the doctor can examine you.**<br>_Hãy mở miệng để bác sĩ khám cho bạn nhé._ |
| 218 | **tooth** | `/tuːθ/` | *noun* | chiếc răng | **I need to visit the dentist for a sore tooth.**<br>_Tôi cần đi gặp nha sĩ vì một chiếc răng bị đau._ |
| 219 | **hair** | `/heər/` | *noun* | mái tóc, sợi tóc | **She has long and wavy black hair.**<br>_Cô ấy có mái tóc đen dài và gợn sóng._ |
| 220 | **neck** | `/nek/` | *noun* | cổ | **He wrapped a scarf around his neck.**<br>_Anh ấy quàng một chiếc khăn quanh cổ._ |
| 221 | **shoulder** | `/ˈʃəʊl.dər/` | *noun* | bờ vai | **He carried the backpack on his shoulder.**<br>_Anh ấy đeo chiếc ba lô trên vai._ |
| 222 | **arm** | `/ɑːm/` | *noun* | cánh tay | **She held the baby gently in her arms.**<br>_Cô ấy ẵm em bé nhẹ nhàng trên tay._ |
| 223 | **hand** | `/hænd/` | *noun* | bàn tay | **Wash your hands thoroughly with soap.**<br>_Rửa sạch bàn tay bằng xà phòng._ |
| 224 | **finger** | `/ˈfɪŋ.ɡər/` | *noun* | ngón tay | **She wore a gold ring on her finger.**<br>_Cô ấy đeo một chiếc nhẫn vàng trên ngón tay._ |
| 225 | **leg** | `/leɡ/` | *noun* | cẳng chân, chân | **Running strengthened my legs.**<br>_Chạy bộ đã giúp đôi chân tôi khỏe hơn._ |
| 226 | **knee** | `/niː/` | *noun* | đầu gối | **He injured his left knee while playing soccer.**<br>_Anh ấy bị chấn thương đầu gối trái khi đá bóng._ |
| 227 | **foot** | `/fʊt/` | *noun* | bàn chân | **My feet hurt after walking all day.**<br>_Bàn chân tôi đau nhức sau cả ngày đi bộ._ |
| 228 | **toe** | `/təʊ/` | *noun* | ngón chân | **He stubbed his toe against the chair leg.**<br>_Anh ấy va ngón chân vào chân ghế._ |
| 229 | **heart** | `/hɑːt/` | *noun* | trái tim | **Cardio exercise improves heart health.**<br>_Bài tập tim mạch cải thiện sức khỏe trái tim._ |
| 230 | **stomach** | `/ˈstʌm.ək/` | *noun* | dạ dày, bụng | **Do not take medication on an empty stomach.**<br>_Đừng uống thuốc khi bụng đói._ |
| 231 | **health** | `/helθ/` | *noun* | sức khỏe | **Good health is the greatest wealth.**<br>_Sức khỏe tốt là tài sản quý giá nhất._ |
| 232 | **healthy** | `/ˈhel.θi/` | *adjective* | khỏe mạnh, lành mạnh | **She eats a balanced and healthy diet.**<br>_Cô ấy ăn một chế độ cân bằng và lành mạnh._ |
| 233 | **ill** | `/ɪl/` | *adjective* | ốm, bệnh | **He stayed home from work because he felt ill.**<br>_Anh ấy nghỉ làm ở nhà vì cảm thấy bị ốm._ |
| 234 | **sick** | `/sɪk/` | *adjective* | bị ốm, buồn nôn | **Take care of your sick brother.**<br>_Hãy chăm sóc người em trai bị ốm của bạn._ |
| 235 | **fever** | `/ˈfiː.vər/` | *noun* | cơn sốt | **The doctor measured his temperature for fever.**<br>_Bác sĩ đã đo nhiệt độ để kiểm tra xem anh ấy có bị sốt không._ |
| 236 | **headache** | `/ˈhed.eɪk/` | *noun* | chứng đau đầu | **I took an aspirin to relieve my headache.**<br>_Tôi đã uống một viên aspirin để giảm cơn đau đầu._ |
| 237 | **cough** | `/kɒf/` | *noun* | tiếng ho, cơn ho | **She covered her mouth while having a cough.**<br>_Cô ấy lấy tay che miệng khi bị ho._ |
| 238 | **medicine** | `/ˈmed.ɪ.sən/` | *noun* | thuốc điều trị | **Take this medicine after each meal.**<br>_Hãy uống loại thuốc này sau mỗi bữa ăn._ |
| 239 | **doctor** | `/ˈdɒk.tər/` | *noun* | bác sĩ | **You should see a doctor about your pain.**<br>_Bạn nên đi khám bác sĩ về cơn đau của mình._ |
| 240 | **hospital** | `/ˈhɒs.pɪ.təl/` | *noun* | bệnh viện | **The ambulance rushed the patient to the hospital.**<br>_Xe cứu thương vội vã đưa bệnh nhân đến bệnh viện._ |
| 241 | **school** | `/skuːl/` | *noun* | trường học | **Children walk to primary school together.**<br>_Lũ trẻ cùng nhau đi bộ đến trường tiểu học._ |
| 242 | **teacher** | `/ˈtiː.tʃər/` | *noun* | giáo viên, thầy cô | **Our English teacher is patient and friendly.**<br>_Giáo viên tiếng Anh của chúng tôi rất kiên nhẫn và thân thiện._ |
| 243 | **student** | `/ˈstjuː.dənt/` | *noun* | học sinh, sinh viên | **University students study hard for final exams.**<br>_Các sinh viên đại học học tập chăm chỉ cho kỳ thi cuối kỳ._ |
| 244 | **class** | `/klɑːs/` | *noun* | lớp học, tiết học | **Class begins promptly at eight o'clock.**<br>_Tiết học bắt đầu đúng vào lúc tám giờ._ |
| 245 | **classroom** | `/ˈklɑːs.ruːm/` | *noun* | phòng học | **The classroom is equipped with a modern projector.**<br>_Phòng học được trang bị một máy chiếu hiện đại._ |
| 246 | **lesson** | `/ˈles.ən/` | *noun* | bài học, tiết học | **Today's grammar lesson is very useful.**<br>_Bài học ngữ pháp hôm nay rất hữu ích._ |
| 247 | **book** | `/bʊk/` | *noun* | cuốn sách | **I read an interesting history book last night.**<br>_Tôi đã đọc một cuốn sách lịch sử thú vị tối qua._ |
| 248 | **pen** | `/pen/` | *noun* | cây bút mực | **Could I borrow your blue pen for a moment?**<br>_Tôi có thể mượn cây bút xanh của bạn một lát được không?_ |
| 249 | **pencil** | `/ˈpen.səl/` | *noun* | bút chì | **Sketch the diagram first with a pencil.**<br>_Hãy phác thảo sơ đồ trước bằng bút chì._ |
| 250 | **paper** | `/ˈpeɪ.pər/` | *noun* | tờ giấy | **Write your full name at the top of the paper.**<br>_Hãy viết họ tên đầy đủ ở đầu tờ giấy._ |
| 251 | **notebook** | `/ˈnəʊt.bʊk/` | *noun* | vở ghi chép | **Keep all your class notes in one notebook.**<br>_Hãy giữ tất cả ghi chú lớp học trong một cuốn vở._ |
| 252 | **board** | `/bɔːd/` | *noun* | bảng viết | **The teacher wrote new vocabulary on the board.**<br>_Giáo viên đã viết từ vựng mới lên bảng._ |
| 253 | **eraser** | `/ɪˈreɪ.zər/` | *noun* | cục tẩy | **Use an eraser to correct any pencil mistakes.**<br>_Dùng cục tẩy để sửa lại bất kỳ lỗi bút chì nào._ |
| 254 | **ruler** | `/ˈruː.lər/` | *noun* | cây thước kẻ | **Draw a straight five-centimeter line with a ruler.**<br>_Dùng thước kẻ vẽ một đường thẳng dài 5 cm._ |
| 255 | **bag** | `/bæɡ/` | *noun* | cặp sách, túi xách | **Pack your textbooks into your school bag.**<br>_Hãy xếp sách giáo khoa vào cặp của bạn._ |
| 256 | **learn** | `/lɜːn/` | *verb* | học, tiếp thu kiến thức | **It is fun to learn a new foreign language.**<br>_Thật thú vị khi học một ngoại ngữ mới._ |
| 257 | **study** | `/ˈstʌd.i/` | *verb* | nghiên cứu, học tập | **She studies biology at the national university.**<br>_Cô ấy học ngành sinh học tại trường đại học quốc gia._ |
| 258 | **teach** | `/tiːtʃ/` | *verb* | dạy học, giảng dạy | **He teaches mathematics to high school students.**<br>_Anh ấy dạy toán cho học sinh trung học._ |
| 259 | **read** | `/riːd/` | *verb* | đọc | **Try to read ten pages of English every day.**<br>_Hãy cố gắng đọc mười trang tiếng Anh mỗi ngày._ |
| 260 | **write** | `/raɪt/` | *verb* | viết | **Write a short paragraph about your hometown.**<br>_Hãy viết một đoạn văn ngắn về quê hương của bạn._ |
| 261 | **homework** | `/ˈhəʊm.wɜːk/` | *noun* | bài tập về nhà | **Finish your math homework before watching TV.**<br>_Hãy làm xong bài tập toán trước khi xem tivi._ |
| 262 | **exam** | `/ɪɡˈzæm/` | *noun* | kỳ thi, bài kiểm tra | **We must prepare thoroughly for the entrance exam.**<br>_Chúng ta phải chuẩn bị kỹ càng cho kỳ thi tuyển sinh._ |
| 263 | **test** | `/test/` | *noun* | bài kiểm tra | **Tomorrow we have a 15-minute vocabulary test.**<br>_Ngày mai chúng ta có bài kiểm tra từ vựng 15 phút._ |
| 264 | **grade** | `/ɡreɪd/` | *noun* | điểm số, cấp lớp | **She earned high grades in all her subjects.**<br>_Cô ấy đạt điểm cao ở tất cả các môn học._ |
| 265 | **pass** | `/pɑːs/` | *verb* | thi đỗ, vượt qua | **Study hard and you will pass the driving test.**<br>_Học chăm chỉ và bạn sẽ thi đỗ bằng lái xe._ |
| 266 | **fail** | `/feɪl/` | *verb* | thi trượt, thất bại | **Do not give up even if you fail once.**<br>_Đừng từ bỏ ngay cả khi bạn thi trượt một lần._ |
| 267 | **course** | `/kɔːs/` | *noun* | khóa học | **I enrolled in an intensive beginner English course.**<br>_Tôi đã đăng ký một khóa học tiếng Anh sơ cấp cấp tốc._ |
| 268 | **university** | `/ˌjuː.nɪˈvɜː.sə.ti/` | *noun* | trường đại học | **She graduated from Oxford University last year.**<br>_Cô ấy đã tốt nghiệp Đại học Oxford vào năm ngoái._ |
| 269 | **library** | `/ˈlaɪ.brər.i/` | *noun* | thư viện | **The public library offers a quiet space to read.**<br>_Thư viện công cộng cung cấp không gian yên tĩnh để đọc._ |
| 270 | **dictionary** | `/ˈdɪk.ʃən.ər.i/` | *noun* | từ điển | **Look up unknown words in an English dictionary.**<br>_Tra những từ chưa biết trong một cuốn từ điển tiếng Anh._ |
| 271 | **page** | `/peɪdʒ/` | *noun* | trang sách | **Turn to page forty-five in your workbook.**<br>_Hãy mở đến trang bốn mươi lăm trong sách bài tập._ |
| 272 | **project** | `/ˈprɒdʒ.ekt/` | *noun* | dự án, đề án | **Our science project won first prize.**<br>_Dự án khoa học của chúng tôi đã đạt giải nhất._ |
| 273 | **practice** | `/ˈpræk.tɪs/` | *verb* | luyện tập, thực hành | **Practice speaking English every single day.**<br>_Hãy luyện nói tiếng Anh mỗi ngày._ |
| 274 | **remember** | `/rɪˈmem.bər/` | *verb* | ghi nhớ, nhớ lại | **Can you remember your password?**<br>_Bạn có thể nhớ mật khẩu của mình không?_ |
| 275 | **forget** | `/fəˈɡet/` | *verb* | quên | **Do not forget your umbrella on a cloudy day.**<br>_Đừng quên ô vào một ngày trời nhiều mây._ |
| 276 | **job** | `/dʒɒb/` | *noun* | công việc, nghề nghiệp | **She applied for an exciting job in marketing.**<br>_Cô ấy đã nộp đơn cho một công việc thú vị ngành tiếp thị._ |
| 277 | **work** | `/wɜːk/` | *verb* | làm việc, công tác | **My father works for an international company.**<br>_Bố tôi làm việc cho một công ty quốc tế._ |
| 278 | **office** | `/ˈɒf.ɪs/` | *noun* | văn phòng làm việc | **Our headquarters office is located in downtown.**<br>_Trụ sở văn phòng của chúng tôi nằm ở trung tâm thành phố._ |
| 279 | **company** | `/ˈkʌm.pə.ni/` | *noun* | công ty, doanh nghiệp | **The software company launched a new app.**<br>_Công ty phần mềm vừa ra mắt một ứng dụng mới._ |
| 280 | **boss** | `/bɒs/` | *noun* | sếp, người quản lý | **The boss approved our annual budget proposal.**<br>_Sếp đã phê duyệt đề xuất ngân sách hàng năm của chúng tôi._ |
| 281 | **manager** | `/ˈmæn.ɪ.dʒər/` | *noun* | người quản lý, giám đốc | **Discuss your schedule with the branch manager.**<br>_Hãy thảo luận lịch trình của bạn với người quản lý chi nhánh._ |
| 282 | **worker** | `/ˈwɜː.kər/` | *noun* | công nhân, người lao động | **Factory workers wear protective safety helmets.**<br>_Các công nhân nhà máy đội mũ bảo hộ an toàn._ |
| 283 | **engineer** | `/ˌen.dʒɪˈnɪər/` | *noun* | kỹ sư | **Civil engineers build durable roads and bridges.**<br>_Các kỹ sư xây dựng xây nên những con đường và cây cầu bền vững._ |
| 284 | **nurse** | `/nɜːs/` | *noun* | y tá, điều dưỡng | **The kind nurse took my blood pressure.**<br>_Cô y tá tốt bụng đã đo huyết áp cho tôi._ |
| 285 | **police** | `/pəˈliːs/` | *noun* | cảnh sát, công an | **Call the police immediately if you see a crime.**<br>_Hãy gọi cảnh sát ngay lập tức nếu bạn nhìn thấy tội phạm._ |
| 286 | **driver** | `/ˈdraɪ.vər/` | *noun* | tài xế, người lái xe | **The taxi driver knows all shortcut routes.**<br>_Bác tài xế taxi biết tất cả các tuyến đường tắt._ |
| 287 | **chef** | `/ʃef/` | *noun* | đầu bếp trưởng | **The French chef prepared a magnificent dinner.**<br>_Bếp trưởng người Pháp đã chuẩn bị một bữa tối tuyệt vời._ |
| 288 | **farmer** | `/ˈfɑː.mər/` | *noun* | người nông dân | **Farmers harvest golden ripe rice in autumn.**<br>_Những người nông dân thu hoạch lúa vàng óng vào mùa thu._ |
| 289 | **pilot** | `/ˈpaɪ.lət/` | *noun* | phi công | **The skilled pilot landed the plane safely.**<br>_Người phi công điêu luyện đã hạ cánh máy bay an toàn._ |
| 290 | **waiter** | `/ˈweɪ.tər/` | *noun* | nam nhân viên phục vụ | **The polite waiter brought us clean water.**<br>_Người bồi bàn lịch sự đã mang nước sạch cho chúng tôi._ |
| 291 | **waitress** | `/ˈweɪ.trəs/` | *noun* | nữ nhân viên phục vụ | **The friendly waitress recommended local fish.**<br>_Cô phục vụ bàn thân thiện đã gợi ý món cá địa phương._ |
| 292 | **singer** | `/ˈsɪŋ.ər/` | *noun* | ca sĩ | **She dreamed of becoming a professional singer.**<br>_Cô ấy từng mơ ước trở thành một ca sĩ chuyên nghiệp._ |
| 293 | **actor** | `/ˈæk.tər/` | *noun* | diễn viên nam | **The talented actor starred in an action film.**<br>_Nam diễn viên tài năng đã đóng vai chính trong một bộ phim hành động._ |
| 294 | **artist** | `/ˈɑː.tɪst/` | *noun* | họa sĩ, nghệ sĩ | **The painter exhibited colorful landscape art.**<br>_Người nghệ sĩ đã triển lãm tranh phong cảnh rực rỡ._ |
| 295 | **writer** | `/ˈraɪ.tər/` | *noun* | nhà văn, tác giả | **The author is writing his second mystery novel.**<br>_Nhà văn đang viết cuốn tiểu thuyết trinh thám thứ hai._ |
| 296 | **meeting** | `/ˈmiː.tɪŋ/` | *noun* | cuộc họp | **We will discuss the deadline at the meeting.**<br>_Chúng ta sẽ thảo luận về thời hạn tại cuộc họp._ |
| 297 | **salary** | `/ˈsæl.ər.i/` | *noun* | tiền lương hàng tháng | **Employees receive their monthly salary on Friday.**<br>_Nhân viên nhận lương hàng tháng vào thứ Sáu._ |
| 298 | **career** | `/kəˈrɪər/` | *noun* | sự nghiệp | **She built an impressive career in technology.**<br>_Cô ấy đã xây dựng một sự nghiệp ấn tượng trong ngành công nghệ._ |
| 299 | **business** | `/ˈbɪz.nɪs/` | *noun* | kinh doanh, thương mại | **Starting an online business requires dedication.**<br>_Bắt đầu kinh doanh trực tuyến đòi hỏi sự cống hiến._ |
| 300 | **customer** | `/ˈkʌs.tə.mər/` | *noun* | khách hàng | **Treat every customer with utmost courtesy.**<br>_Hãy đối xử với mọi khách hàng bằng sự lịch thiệp tối đa._ |
| 301 | **staff** | `/stɑːf/` | *noun* | đội ngũ nhân viên | **The hotel staff is available twenty-four hours.**<br>_Đội ngũ nhân viên khách sạn luôn sẵn sàng phục vụ 24 giờ._ |
| 302 | **interview** | `/ˈɪn.tə.vjuː/` | *noun* | buổi phỏng vấn | **Prepare answers for common interview questions.**<br>_Hãy chuẩn bị câu trả lời cho các câu hỏi phỏng vấn thường gặp._ |
| 303 | **hire** | `/haɪər/` | *verb* | thuê, tuyển dụng | **The corporation plans to hire twenty interns.**<br>_Tập đoàn có kế hoạch tuyển dụng hai mươi thực tập sinh._ |
| 304 | **fire** | `/faɪər/` | *verb* | sa thải, đuổi việc | **The company decided to fire lazy employees.**<br>_Công ty quyết định sa thải những nhân viên lười biếng._ |
| 305 | **retire** | `/rɪˈtaɪər/` | *verb* | nghỉ hưu | **My grandparents will retire to the countryside.**<br>_Ông bà tôi sẽ về hưu ở vùng nông thôn._ |
| 306 | **task** | `/tɑːsk/` | *noun* | nhiệm vụ, đầu việc | **Organize your daily tasks before starting work.**<br>_Hãy sắp xếp các nhiệm vụ hàng ngày trước khi bắt đầu làm việc._ |
| 307 | **contract** | `/ˈkɒn.trækt/` | *noun* | hợp đồng | **Both parties signed a two-year employment contract.**<br>_Cả hai bên đã ký một hợp đồng lao động hai năm._ |
| 308 | **team** | `/tiːm/` | *noun* | đội ngũ, nhóm làm việc | **Our marketing team won the quarterly award.**<br>_Đội tiếp thị của chúng tôi đã giành giải thưởng quý._ |
| 309 | **busy** | `/ˈbɪz.i/` | *adjective* | bận rộn | **I am very busy preparing the financial report.**<br>_Tôi đang rất bận rộn chuẩn bị báo cáo tài chính._ |
| 310 | **free** | `/friː/` | *adjective* | rảnh rỗi, miễn phí | **Are you free for lunch tomorrow noon?**<br>_Bạn có rảnh để ăn trưa vào trưa mai không?_ |
| 311 | **travel** | `/ˈtræv.əl/` | *verb* | du lịch, đi lại | **I love to travel to foreign destinations.**<br>_Tôi rất thích đi du lịch đến các điểm đến nước ngoài._ |
| 312 | **trip** | `/trɪp/` | *noun* | chuyến đi, chuyến du ngoạn | **We had a wonderful weekend trip to the beach.**<br>_Chúng tôi đã có một chuyến đi cuối tuần tuyệt vời ra biển._ |
| 313 | **journey** | `/ˈdʒɜː.ni/` | *noun* | hành trình dài | **The train journey through mountains was scenic.**<br>_Hành trình tàu hỏa qua núi non thật thơ mộng._ |
| 314 | **ticket** | `/ˈtɪk.ɪt/` | *noun* | vé (tàu, xe, máy bay) | **Show your boarding ticket at the departure gate.**<br>_Xuất trình vé lên máy bay tại cửa khởi hành._ |
| 315 | **passport** | `/ˈpɑːs.pɔːt/` | *noun* | hộ chiếu | **You need a valid passport to travel abroad.**<br>_Bạn cần một cuốn hộ chiếu hợp lệ để ra nước ngoài._ |
| 316 | **luggage** | `/ˈlʌɡ.ɪdʒ/` | *noun* | hành lý | **Do not leave your luggage unattended anywhere.**<br>_Đừng để hành lý của bạn không người trông nom ở bất cứ đâu._ |
| 317 | **airport** | `/ˈeə.pɔːt/` | *noun* | sân bay, phi trường | **We must arrive at the airport two hours early.**<br>_Chúng ta phải đến sân bay trước hai tiếng._ |
| 318 | **station** | `/ˈsteɪ.ʃən/` | *noun* | nhà ga, bến xe | **The express train departs from central station.**<br>_Tàu tốc hành khởi hành từ ga trung tâm._ |
| 319 | **bus** | `/bʌs/` | *noun* | xe buýt | **I take the number seven bus to work.**<br>_Tôi đi xe buýt số 7 đi làm._ |
| 320 | **train** | `/treɪn/` | *noun* | tàu hỏa, xe lửa | **The bullet train travels at high speed.**<br>_Tàu cao tốc chạy với tốc độ rất cao._ |
| 321 | **plane** | `/pleɪn/` | *noun* | máy bay | **The plane took off on schedule despite mist.**<br>_Máy bay đã cất cánh đúng giờ bất chấp sương mù._ |
| 322 | **car** | `/kɑːr/` | *noun* | xe hơi, ô tô | **He drives an electric car to save fuel.**<br>_Anh ấy lái xe điện để tiết kiệm nhiên liệu._ |
| 323 | **taxi** | `/ˈtæk.si/` | *noun* | xe taxi | **We hailed a taxi outside the hotel lobby.**<br>_Chúng tôi vẫy một chiếc taxi ngoài sảnh khách sạn._ |
| 324 | **bicycle** | `/ˈbaɪ.sɪ.kəl/` | *noun* | xe đạp | **Riding a bicycle is great cardio exercise.**<br>_Đi xe đạp là một bài tập tim mạch tuyệt vời._ |
| 325 | **motorbike** | `/ˈməʊ.tə.baɪk/` | *noun* | xe máy | **Many people in Vietnam ride motorbikes daily.**<br>_Nhiều người ở Việt Nam đi xe máy hàng ngày._ |
| 326 | **boat** | `/bəʊt/` | *noun* | thuyền, đò | **We rented a small wooden boat on the lake.**<br>_Chúng tôi đã thuê một chiếc thuyền gỗ nhỏ trên hồ._ |
| 327 | **ship** | `/ʃɪp/` | *noun* | tàu thủy lớn | **A luxury cruise ship sailed into the harbor.**<br>_Một chiếc du thuyền sang trọng đã cập cảng._ |
| 328 | **street** | `/striːt/` | *noun* | đường phố | **The old street is lined with green trees.**<br>_Con phố cổ rợp bóng những hàng cây xanh._ |
| 329 | **road** | `/rəʊd/` | *noun* | con đường | **Drive carefully along this winding mountain road.**<br>_Hãy lái xe cẩn thận dọc theo con đường đèo uốn lượn này._ |
| 330 | **bridge** | `/brɪdʒ/` | *noun* | cây cầu | **The suspension bridge crosses the wide river.**<br>_Cây cầu treo bắc qua con sông rộng lớn._ |
| 331 | **city** | `/ˈsɪt.i/` | *noun* | thành phố lớn | **Tokyo is one of the most populous cities.**<br>_Tokyo là một trong những thành phố đông dân nhất._ |
| 332 | **town** | `/taʊn/` | *noun* | thị trấn, thị xã | **He grew up in a peaceful coastal town.**<br>_Anh lớn lên ở một thị trấn ven biển yên bình._ |
| 333 | **village** | `/ˈvɪl.ɪdʒ/` | *noun* | ngôi làng, vùng quê | **The small village is surrounded by green hills.**<br>_Ngôi làng nhỏ được bao quanh bởi những ngọn đồi xanh._ |
| 334 | **country** | `/ˈkʌn.tri/` | *noun* | quốc gia, đất nước | **Vietnam is a beautiful and hospitable country.**<br>_Việt Nam là một đất nước xinh đẹp và hiếu khách._ |
| 335 | **hotel** | `/həʊˈtel/` | *noun* | khách sạn | **We booked a sea-view room at the hotel.**<br>_Chúng tôi đã đặt một phòng hướng biển ở khách sạn._ |
| 336 | **bank** | `/bæŋk/` | *noun* | ngân hàng | **I need to exchange foreign currency at the bank.**<br>_Tôi cần đổi ngoại tệ ở ngân hàng._ |
| 337 | **park** | `/pɑːk/` | *noun* | công viên | **Children love playing games in the city park.**<br>_Trẻ em thích chơi trò chơi trong công viên thành phố._ |
| 338 | **museum** | `/mjuːˈziː.əm/` | *noun* | bảo tàng | **The art museum showcases historical paintings.**<br>_Bảo tàng nghệ thuật trưng bày các bức tranh lịch sử._ |
| 339 | **cinema** | `/ˈsɪn.ə.mɑː/` | *noun* | rạp chiếu phim | **Let's watch a comedy film at the cinema.**<br>_Hãy cùng xem một bộ phim hài ở rạp chiếu phim nhé._ |
| 340 | **supermarket** | `/ˈsuː.pəˌmɑː.kɪt/` | *noun* | siêu thị | **Buy fresh groceries from the local supermarket.**<br>_Mua thực phẩm tươi sống từ siêu thị địa phương._ |
| 341 | **market** | `/ˈmɑː.kɪt/` | *noun* | chợ, khu chợ | **The traditional morning market is bustling.**<br>_Khu chợ truyền thống buổi sáng rất nhộn nhịp._ |
| 342 | **pharmacy** | `/ˈfɑː.mə.si/` | *noun* | hiệu thuốc tây | **Get prescription cough drops from the pharmacy.**<br>_Mua kẹo ngậm ho theo đơn từ hiệu thuốc tây._ |
| 343 | **post office** | `/ˈpəʊst ˌɒf.ɪs/` | *noun* | bưu điện | **Send this parcel from the neighborhood post office.**<br>_Hãy gửi bưu kiện này từ bưu điện gần nhà._ |
| 344 | **map** | `/mæp/` | *noun* | bản đồ | **Check the interactive map on your smartphone.**<br>_Kiểm tra bản đồ tương tác trên điện thoại thông minh của bạn._ |
| 345 | **tourist** | `/ˈtʊə.rɪst/` | *noun* | khách du lịch | **Tourists take photos of ancient architecture.**<br>_Khách du lịch chụp ảnh những công trình kiến trúc cổ kính._ |
| 346 | **visit** | `/ˈvɪz.ɪt/` | *verb* | thăm quan, ghé thăm | **We plan to visit Hue imperial citadel.**<br>_Chúng tôi dự định đến thăm Đại nội Huế._ |
| 347 | **arrive** | `/əˈraɪv/` | *verb* | đến nơi, cập bến | **The flight will arrive in Singapore on time.**<br>_Chuyến bay sẽ đến Singapore đúng giờ._ |
| 348 | **depart** | `/dɪˈpɑːt/` | *verb* | khởi hành, rời đi | **Trains depart punctually from platform number three.**<br>_Các chuyến tàu khởi hành đúng giờ từ sân ga số ba._ |
| 349 | **left** | `/left/` | *noun* | bên trái | **Turn left at the next traffic light.**<br>_Rẽ trái tại cột đèn giao thông tiếp theo._ |
| 350 | **right** | `/raɪt/` | *noun* | bên phải | **The souvenir gift shop is on your right.**<br>_Cửa hàng quà lưu niệm nằm ở phía bên phải của bạn._ |
| 351 | **sun** | `/sʌn/` | *noun* | mặt trời | **The morning sun shines brightly over the ocean.**<br>_Mặt trời buổi sáng tỏa sáng rực rỡ trên đại dương._ |
| 352 | **moon** | `/muːn/` | *noun* | mặt trăng | **The full moon lights up the quiet night sky.**<br>_Vầng trăng tròn thắp sáng bầu trời đêm tĩnh lặng._ |
| 353 | **star** | `/stɑːr/` | *noun* | ngôi sao | **Millions of stars sparkle in the dark sky.**<br>_Hàng triệu ngôi sao lấp lánh trên bầu trời đêm._ |
| 354 | **sky** | `/skaɪ/` | *noun* | bầu trời | **The clear blue sky has not a single cloud.**<br>_Bầu trời xanh trong không một gợn mây._ |
| 355 | **cloud** | `/klaʊd/` | *noun* | đám mây | **Dark clouds indicate that a storm is approaching.**<br>_Những đám mây đen báo hiệu một cơn bão đang đến gần._ |
| 356 | **rain** | `/reɪn/` | *noun* | cơn mưa, mưa | **Gentle spring rain nurtures blooming flowers.**<br>_Cơn mưa xuân dịu dàng nuôi dưỡng những bông hoa đang nở._ |
| 357 | **snow** | `/snəʊ/` | *noun* | tuyết, bông tuyết | **Children built a smiling snowman in the fresh snow.**<br>_Trẻ em đắp một người tuyết tươi cười trong lớp tuyết mới._ |
| 358 | **wind** | `/wɪnd/` | *noun* | cơn gió | **A cool breeze wind blew from the mountain.**<br>_Một ngọn gió mát lành thổi tới từ ngọn núi._ |
| 359 | **storm** | `/stɔːm/` | *noun* | cơn bão | **The tropical storm brought heavy rainfall.**<br>_Cơn bão nhiệt đới đã mang theo mưa lớn._ |
| 360 | **weather** | `/ˈweð.ər/` | *noun* | thời tiết | **What will the weather be like this weekend?**<br>_Thời tiết cuối tuần này sẽ như thế nào?_ |
| 361 | **hot** | `/hɒt/` | *adjective* | nóng, nóng nực | **Summer in the central region is very hot.**<br>_Mùa hè ở miền Trung rất nóng nực._ |
| 362 | **cold** | `/kəʊld/` | *adjective* | lạnh, lạnh giá | **Put on a thick coat; it is freezing cold.**<br>_Mặc áo khoác dày vào; ngoài trời lạnh cóng đấy._ |
| 363 | **warm** | `/wɔːm/` | *adjective* | ấm áp | **Spring brings pleasantly warm sunshine.**<br>_Mùa xuân mang đến ánh nắng ấm áp dễ chịu._ |
| 364 | **cool** | `/kuːl/` | *adjective* | mát mẻ, ngầu | **Autumn evenings are comfortably cool.**<br>_Những buổi tối mùa thu mát mẻ dễ chịu._ |
| 365 | **wet** | `/wet/` | *adjective* | ẩm ướt | **Be careful not to slip on the wet floor.**<br>_Cẩn thận kẻo trượt ngã trên sàn ướt._ |
| 366 | **dry** | `/draɪ/` | *adjective* | khô ráo | **Store cereal in a cool and dry place.**<br>_Bảo quản ngũ cốc ở nơi thoáng mát và khô ráo._ |
| 367 | **tree** | `/triː/` | *noun* | cây cối | **Birds build their nests high in the tall tree.**<br>_Những chú chim làm tổ trên ngọn cây cao._ |
| 368 | **flower** | `/flaʊər/` | *noun* | bông hoa | **She received a bouquet of red flowers.**<br>_Cô ấy nhận được một bó hoa đỏ thắm._ |
| 369 | **grass** | `/ɡrɑːs/` | *noun* | bãi cỏ, thảm cỏ | **We sat on the soft green grass for a picnic.**<br>_Chúng tôi ngồi trên thảm cỏ xanh mượt để dã ngoại._ |
| 370 | **forest** | `/ˈfɒr.ɪst/` | *noun* | khu rừng | **Tigers and deer inhabit the deep forest.**<br>_Hổ và hươu nai sinh sống trong khu rừng sâu._ |
| 371 | **mountain** | `/ˈmaʊn.tɪn/` | *noun* | ngọn núi | **Hikers climbed to the summit of the high mountain.**<br>_Những người đi bộ đường dài đã leo lên đỉnh ngọn núi cao._ |
| 372 | **hill** | `/hɪl/` | *noun* | ngọn đồi | **Tea plantations cover the gentle rolling hills.**<br>_Những đồi chè bao phủ các sườn đồi thoai thoải._ |
| 373 | **river** | `/ˈrɪv.ər/` | *noun* | con sông | **The Red River flows through the capital city.**<br>_Sông Hồng chảy qua lòng thủ đô._ |
| 374 | **lake** | `/leɪk/` | *noun* | hồ nước | **We walked peacefully around West Lake at sunset.**<br>_Chúng tôi đi dạo thanh bình quanh Hồ Tây lúc hoàng hôn._ |
| 375 | **sea** | `/siː/` | *noun* | biển | **They sailed across the calm Mediterranean Sea.**<br>_Họ căng buồm vượt qua vùng biển Địa Trung Hải êm ả._ |
| 376 | **ocean** | `/ˈəʊ.ʃən/` | *noun* | đại dương | **Whales migrate across the vast Pacific Ocean.**<br>_Cá voi di cư qua đại dương Thái Bình Dương rộng lớn._ |
| 377 | **beach** | `/biːtʃ/` | *noun* | bãi biển | **Sunbathers relax on the golden sandy beach.**<br>_Những người tắm nắng thư giãn trên bãi biển cát vàng._ |
| 378 | **island** | `/ˈaɪ.lənd/` | *noun* | hòn đảo | **Phu Quoc is the largest tropical island in Vietnam.**<br>_Phú Quốc là hòn đảo nhiệt đới lớn nhất ở Việt Nam._ |
| 379 | **animal** | `/ˈæn.ɪ.məl/` | *noun* | động vật, muông thú | **Protect endangered animals in national parks.**<br>_Bảo vệ động vật có nguy cơ tuyệt chủng trong các vườn quốc gia._ |
| 380 | **plant** | `/plɑːnt/` | *noun* | thực vật, cây cảnh | **Water indoor potted plants twice a week.**<br>_Tưới nước cho cây cảnh trong nhà hai lần một tuần._ |
| 381 | **hobby** | `/ˈhɒb.i/` | *noun* | sở thích | **Photography is her favorite creative hobby.**<br>_Nhiếp ảnh là sở thích sáng tạo yêu thích của cô ấy._ |
| 382 | **music** | `/ˈmjuː.zɪk/` | *noun* | âm nhạc | **Listening to classical music helps me focus.**<br>_Nghe nhạc cổ điển giúp tôi tập trung._ |
| 383 | **song** | `/sɒŋ/` | *noun* | bài hát, ca khúc | **This cheerful song became a global hit.**<br>_Bài hát vui tươi này đã trở thành một bản hit toàn cầu._ |
| 384 | **sing** | `/sɪŋ/` | *verb* | hát, ca hát | **The choir sings harmonies in church.**<br>_Dàn hợp xướng hát những giai điệu hòa quyện trong nhà thờ._ |
| 385 | **dance** | `/dɑːns/` | *verb* | khiêu vũ, nhảy múa | **Young couples dance gracefully to the waltz.**<br>_Các cặp đôi trẻ khiêu vũ duyên dáng theo điệu waltz._ |
| 386 | **game** | `/ɡeɪm/` | *noun* | trò chơi, ván đấu | **Let's play a strategic board game tonight.**<br>_Hãy chơi một trò chơi cờ bàn chiến thuật tối nay nhé._ |
| 387 | **sport** | `/spɔːt/` | *noun* | thể thao | **Swimming is an excellent all-around sport.**<br>_Bơi lội là một môn thể thao toàn diện xuất sắc._ |
| 388 | **football** | `/ˈfʊt.bɔːl/` | *noun* | bóng đá | **Millions watch international football tournaments.**<br>_Hàng triệu người theo dõi các giải bóng đá quốc tế._ |
| 389 | **basketball** | `/ˈbɑː.skɪt.bɔːl/` | *noun* | bóng rổ | **He plays point guard on the basketball team.**<br>_Anh ấy chơi ở vị trí hậu vệ dẫn bóng trong đội bóng rổ._ |
| 390 | **tennis** | `/ˈten.ɪs/` | *noun* | quần vợt | **They practice tennis on clay courts on weekends.**<br>_Họ tập quần vợt trên sân đất nện vào cuối tuần._ |
| 391 | **badminton** | `/ˈbæd.mɪn.tən/` | *noun* | cầu lông | **Badminton is very popular in Asian parks.**<br>_Cầu lông rất phổ biến ở các công viên châu Á._ |
| 392 | **swim** | `/swɪm/` | *verb* | bơi lội | **Learn to swim to stay safe in deep water.**<br>_Học bơi để giữ an toàn trong vùng nước sâu._ |
| 393 | **run** | `/rʌn/` | *verb* | chạy bộ | **He runs five kilometers every morning.**<br>_Anh ấy chạy năm cây số mỗi buổi sáng._ |
| 394 | **walk** | `/wɔːk/` | *verb* | đi bộ | **A brisk walk daily improves cardiovascular health.**<br>_Đi bộ nhanh hàng ngày cải thiện sức khỏe tim mạch._ |
| 395 | **jump** | `/dʒʌmp/` | *verb* | nhảy, bật nhảy | **Athletes jump high over the bar.**<br>_Các vận động viên bật nhảy cao qua xà ngang._ |
| 396 | **movie** | `/ˈmuː.vi/` | *noun* | bộ phim | **We watched an exciting action movie together.**<br>_Chúng tôi đã cùng nhau xem một bộ phim hành động gay cấn._ |
| 397 | **film** | `/fɪlm/` | *noun* | phim điện ảnh | **The documentary film won prestigious film festival awards.**<br>_Bộ phim tài liệu đã giành các giải thưởng liên hoan phim danh giá._ |
| 398 | **play** | `/pleɪ/` | *verb* | chơi, đóng kịch | **Children love to play hide-and-seek outdoors.**<br>_Trẻ em thích chơi trò trốn tìm ngoài trời._ |
| 399 | **guitar** | `/ɡɪˈtɑːr/` | *noun* | đàn ghi-ta | **He plays acoustic guitar around the campfire.**<br>_Anh ấy chơi đàn ghi-ta mộc bên đống lửa trại._ |
| 400 | **piano** | `/piˈæn.əʊ/` | *noun* | đàn dương cầm, piano | **She took classical piano lessons for ten years.**<br>_Cô ấy đã học các bài học dương cầm cổ điển mười năm._ |
| 401 | **draw** | `/drɔː/` | *verb* | vẽ (bằng bút chì, nét vẽ) | **He can draw detailed comic illustrations.**<br>_Anh ấy có thể vẽ tranh minh họa truyện tranh chi tiết._ |
| 402 | **paint** | `/peɪnt/` | *verb* | vẽ tranh (bằng sơn, màu) | **Artists paint scenic mountain landscapes on canvas.**<br>_Các họa sĩ vẽ tranh phong cảnh núi non hữu tình lên vải._ |
| 403 | **picture** | `/ˈpɪk.tʃər/` | *noun* | bức tranh, bức ảnh | **Hang this family picture above the fireplace.**<br>_Treo bức ảnh gia đình này lên phía trên lò sưởi._ |
| 404 | **photo** | `/ˈfəʊ.təʊ/` | *noun* | bức ảnh chụp | **Take a souvenir photo in front of the monument.**<br>_Chụp một bức ảnh lưu niệm trước tượng đài._ |
| 405 | **camera** | `/ˈkæm.rə/` | *noun* | máy ảnh, máy quay | **She bought a digital camera for her trip.**<br>_Cô ấy đã mua một chiếc máy ảnh kỹ thuật số cho chuyến đi._ |
| 406 | **party** | `/ˈpɑː.ti/` | *noun* | bữa tiệc | **We are organizing a surprise birthday party.**<br>_Chúng tôi đang tổ chức một bữa tiệc sinh nhật bất ngờ._ |
| 407 | **fun** | `/fʌn/` | *noun* | niềm vui, sự vui vẻ | **We had so much fun at the amusement park.**<br>_Chúng tôi đã có rất nhiều niềm vui ở công viên giải trí._ |
| 408 | **match** | `/mætʃ/` | *noun* | trận đấu | **The championship football match begins at eight.**<br>_Trận đấu bóng đá tranh chức vô địch bắt đầu lúc tám giờ._ |
| 409 | **win** | `/wɪn/` | *verb* | chiến thắng | **Our team worked hard to win the trophy.**<br>_Đội của chúng tôi đã làm việc chăm chỉ để giành cúp._ |
| 410 | **lose** | `/luːz/` | *verb* | thua cuộc, mất | **Athletes learn lessons even when they lose.**<br>_Các vận động viên học được bài học ngay cả khi thua cuộc._ |
| 411 | **camp** | `/kæmp/` | *verb* | cắm trại | **We camped under the starry sky in the woods.**<br>_Chúng tôi đã cắm trại dưới bầu trời đầy sao trong rừng._ |
| 412 | **exercise** | `/ˈek.sə.saɪz/` | *noun* | bài tập thể dục | **Morning exercise boosts your energy for the day.**<br>_Tập thể dục buổi sáng tăng cường năng lượng cho ngày mới._ |
| 413 | **club** | `/klʌb/` | *noun* | câu lạc bộ | **She joined the university English debate club.**<br>_Cô ấy đã tham gia câu lạc bộ tranh biện tiếng Anh của trường đại học._ |
| 414 | **concert** | `/ˈkɒn.sət/` | *noun* | buổi hòa nhạc | **Thousands of fans cheered at the rock concert.**<br>_Hàng nghìn người hâm mộ đã reo hò tại buổi hòa nhạc rock._ |
| 415 | **holiday** | `/ˈhɒl.ə.deɪ/` | *noun* | kỳ nghỉ, ngày lễ | **Tet is the most important traditional holiday in Vietnam.**<br>_Tết là ngày lễ truyền thống quan trọng nhất ở Việt Nam._ |
| 416 | **happy** | `/ˈhæp.i/` | *adjective* | vui vẻ, hạnh phúc | **Receiving the acceptance letter made him happy.**<br>_Nhận được thư trúng tuyển khiến anh ấy rất vui vẻ._ |
| 417 | **sad** | `/sæd/` | *adjective* | buồn bã | **She felt sad after bidding farewell to friends.**<br>_Cô ấy cảm thấy buồn bã sau khi chào tạm biệt bạn bè._ |
| 418 | **angry** | `/ˈæŋ.ɡri/` | *adjective* | tức giận, giận dữ | **He was angry about the delayed train schedule.**<br>_Anh ấy tức giận vì lịch trình tàu bị hoãn._ |
| 419 | **excited** | `/ɪkˈsaɪ.tɪd/` | *adjective* | hào hứng, phấn khích | **The children were excited about visiting Disneyland.**<br>_Lũ trẻ rất phấn khích khi được đi thăm Disneyland._ |
| 420 | **tired** | `/taɪəd/` | *adjective* | mệt mỏi | **I feel exhausted and tired after a long flight.**<br>_Tôi cảm thấy kiệt sức và mệt mỏi sau chuyến bay dài._ |
| 421 | **afraid** | `/əˈfreɪd/` | *adjective* | sợ hãi, e ngại | **Do not be afraid to make small grammar mistakes.**<br>_Đừng sợ mắc phải những lỗi ngữ pháp nhỏ._ |
| 422 | **scared** | `/skeəd/` | *adjective* | hoảng sợ | **The loud thunder made the puppy scared.**<br>_Tiếng sấm lớn làm chú cún con hoảng sợ._ |
| 423 | **bored** | `/bɔːd/` | *adjective* | buồn chán, thấy chán | **He grew bored of playing repetitive video games.**<br>_Anh ấy thấy chán khi chơi những trò chơi điện tử lặp đi lặp lại._ |
| 424 | **surprised** | `/səˈpraɪzd/` | *adjective* | ngạc nhiên | **We were pleasantly surprised by the warm reception.**<br>_Chúng tôi rất ngạc nhiên một cách dễ chịu trước sự tiếp đón nồng hậu._ |
| 425 | **worried** | `/ˈwʌr.id/` | *adjective* | lo lắng | **Parents are often worried about teenage safety.**<br>_Các bậc cha mẹ thường lo lắng về sự an toàn của con cái._ |
| 426 | **nervous** | `/ˈnɜː.vəs/` | *adjective* | hồi hộp, bồn chồn | **He felt nervous before giving his first speech.**<br>_Anh ấy cảm thấy hồi hộp trước khi phát biểu bài nói đầu tiên._ |
| 427 | **proud** | `/praʊd/` | *adjective* | tự hào, hãnh diện | **She was proud of graduating with high honors.**<br>_Cô ấy tự hào vì đã tốt nghiệp với tấm bằng danh dự._ |
| 428 | **kind** | `/kaɪnd/` | *adjective* | tốt bụng, tử tế | **Thank you for your kind assistance yesterday.**<br>_Cảm ơn vì sự giúp đỡ tốt bụng của bạn ngày hôm qua._ |
| 429 | **friendly** | `/ˈfrend.li/` | *adjective* | thân thiện, cởi mở | **Local residents are friendly and welcoming to guests.**<br>_Cư dân địa phương rất thân thiện và niềm nở với du khách._ |
| 430 | **polite** | `/pəˈlaɪt/` | *adjective* | lịch sự, lễ phép | **It is polite to say thank you when served.**<br>_Thật là lịch sự khi nói lời cảm ơn khi được phục vụ._ |
| 431 | **lazy** | `/ˈleɪ.zi/` | *adjective* | lười biếng | **He spent a lazy Sunday afternoon lounging on the sofa.**<br>_Anh ấy đã trải qua một buổi chiều chủ nhật lười biếng nằm dài trên ghế sô pha._ |
| 432 | **clever** | `/ˈklev.ər/` | *adjective* | thông minh, khéo léo | **She found a clever solution to the problem.**<br>_Cô ấy đã tìm ra một giải pháp thông minh cho vấn đề._ |
| 433 | **smart** | `/smɑːt/` | *adjective* | thông minh, sáng dạ | **The smart student solved the equation in seconds.**<br>_Người học sinh thông minh đã giải phương trình chỉ trong vài giây._ |
| 434 | **funny** | `/ˈfʌn.i/` | *adjective* | hài hước, buồn cười | **He told a funny joke that made everybody laugh.**<br>_Anh ấy kể một câu chuyện cười hài hước khiến ai nấy đều bật cười._ |
| 435 | **brave** | `/breɪv/` | *adjective* | dũng cảm, can đảm | **Firefighters are brave heroes who save lives.**<br>_Lính cứu hỏa là những người hùng dũng cảm cứu mạng mọi người._ |
| 436 | **calm** | `/kɑːm/` | *adjective* | bình tĩnh, điềm đạm | **Stay calm during emergencies and follow safety exits.**<br>_Hãy giữ bình tĩnh trong các tình huống khẩn cấp và theo lối thoát hiểm._ |
| 437 | **shy** | `/ʃaɪ/` | *adjective* | nhút nhát, e thẹn | **The shy girl hid behind her mother's coat.**<br>_Bé gái nhút nhát nép mình sau vạt áo mẹ._ |
| 438 | **honest** | `/ˈɒn.ɪst/` | *adjective* | trung thực, chân thật | **Give an honest opinion about the new design.**<br>_Hãy đưa ra ý kiến trung thực về thiết kế mới._ |
| 439 | **patient** | `/ˈpeɪ.ʃənt/` | *adjective* | kiên nhẫn | **Good teachers are always patient with slow learners.**<br>_Những giáo viên giỏi luôn kiên nhẫn với học sinh tiếp thu chậm._ |
| 440 | **generous** | `/ˈdʒen.ər.əs/` | *adjective* | hào phóng, rộng lượng | **He made a generous donation to the charity.**<br>_Anh ấy đã quyên góp một khoản hào phóng cho quỹ từ thiện._ |
| 441 | **careful** | `/ˈkeə.fəl/` | *adjective* | cẩn thận, kỹ lưỡng | **Be careful when crossing the busy crossroads.**<br>_Hãy cẩn thận khi băng qua ngã tư đông đúc._ |
| 442 | **careless** | `/ˈkeə.ləs/` | *adjective* | bất cẩn, cẩu thả | **Careless drivers cause preventable traffic accidents.**<br>_Những tài xế bất cẩn gây ra các vụ tai nạn giao thông có thể phòng ngừa._ |
| 443 | **curious** | `/ˈkjʊə.ri.əs/` | *adjective* | tò mò, hiếu kỳ | **Children are naturally curious about the world.**<br>_Trẻ em vốn có tính tò mò tự nhiên về thế giới xung quanh._ |
| 444 | **serious** | `/ˈsɪə.ri.əs/` | *adjective* | nghiêm túc, nghiêm trọng | **Learning a language requires serious commitment.**<br>_Học một ngôn ngữ đòi hỏi sự cam kết nghiêm túc._ |
| 445 | **confident** | `/ˈkɒn.fɪ.dənt/` | *adjective* | tự tin | **Practice helps you become more confident in conversations.**<br>_Luyện tập giúp bạn trở nên tự tin hơn trong các cuộc hội thoại._ |
| 446 | **money** | `/ˈmʌn.i/` | *noun* | tiền bạc | **Do not spend more money than you earn.**<br>_Đừng tiêu nhiều tiền hơn số tiền bạn kiếm được._ |
| 447 | **buy** | `/baɪ/` | *verb* | mua sắm | **I want to buy fresh vegetables at the market.**<br>_Tôi muốn mua rau tươi ở chợ._ |
| 448 | **sell** | `/sel/` | *verb* | bán hàng | **They sell homemade pastries every morning.**<br>_Họ bán bánh ngọt tự làm vào mỗi buổi sáng._ |
| 449 | **pay** | `/peɪ/` | *verb* | thanh toán, trả tiền | **Can I pay for this coffee by credit card?**<br>_Tôi có thể thanh toán ly cà phê này bằng thẻ tín dụng không?_ |
| 450 | **cost** | `/kɒst/` | *verb* | có giá là, tốn phí | **How much does this smartphone cost?**<br>_Chiếc điện thoại thông minh này có giá bao nhiêu?_ |
| 451 | **price** | `/praɪs/` | *noun* | mức giá, giá cả | **The price of oil fluctuates constantly.**<br>_Giá dầu liên tục biến động._ |
| 452 | **cheap** | `/tʃiːp/` | *adjective* | rẻ, giá rẻ | **Public buses provide a cheap way to travel.**<br>_Xe buýt công cộng cung cấp phương tiện đi lại giá rẻ._ |
| 453 | **expensive** | `/ɪkˈspen.sɪv/` | *adjective* | đắt đỏ, tốn kém | **Dining at luxury restaurants is very expensive.**<br>_Ăn uống tại các nhà hàng sang trọng rất đắt đỏ._ |
| 454 | **shop** | `/ʃɒp/` | *noun* | cửa hàng, tiệm | **There is a souvenir gift shop on the corner.**<br>_Có một cửa hàng quà lưu niệm ở góc phố._ |
| 455 | **mall** | `/mɔːl/` | *noun* | trung tâm thương mại | **We spent Saturday afternoon shopping at the mall.**<br>_Chúng tôi dành cả chiều thứ Bảy mua sắm ở trung tâm thương mại._ |
| 456 | **cash** | `/kæʃ/` | *noun* | tiền mặt | **Do you carry enough cash to pay for parking?**<br>_Bạn có mang đủ tiền mặt để trả tiền đỗ xe không?_ |
| 457 | **card** | `/kɑːd/` | *noun* | thẻ ngân hàng, thẻ từ | **Insert your chip card into the payment terminal.**<br>_Cắm thẻ gắn chip của bạn vào thiết bị thanh toán._ |
| 458 | **coin** | `/kɔɪn/` | *noun* | đồng xu, tiền kim loại | **Drop a coin into the vending machine slot.**<br>_Thả một đồng xu vào khe máy bán hàng tự động._ |
| 459 | **bill** | `/bɪl/` | *noun* | hóa đơn thanh toán | **Could we have the dinner bill, please?**<br>_Làm ơn cho chúng tôi xin hóa đơn bữa tối được không?_ |
| 460 | **receipt** | `/rɪˈsiːt/` | *noun* | biên lai, hóa đơn thu tiền | **Keep your receipt in case you need an exchange.**<br>_Giữ biên lai đề phòng trường hợp bạn cần đổi hàng._ |
| 461 | **discount** | `/ˈdɪs.kaʊnt/` | *noun* | sự giảm giá, chiết khấu | **Students get a ten percent discount on books.**<br>_Học sinh được giảm giá mười phần trăm khi mua sách._ |
| 462 | **sale** | `/seɪl/` | *noun* | đợt giảm giá, việc bán hàng | **The department store is holding a summer sale.**<br>_Cửa hàng bách hóa đang tổ chức đợt giảm giá mùa hè._ |
| 463 | **spend** | `/spend/` | *verb* | tiêu xài, dành (thời gian) | **She spends two hours every day learning vocabulary.**<br>_Cô ấy dành hai tiếng mỗi ngày để học từ vựng._ |
| 464 | **save** | `/seɪv/` | *verb* | tiết kiệm, cứu giúp | **Save money for future unexpected emergencies.**<br>_Tiết kiệm tiền cho những trường hợp khẩn cấp bất ngờ trong tương lai._ |
| 465 | **borrow** | `/ˈbɒr.əʊ/` | *verb* | vay, mượn | **Can I borrow your English grammar reference book?**<br>_Tôi có thể mượn cuốn sách tham khảo ngữ pháp tiếng Anh của bạn được không?_ |
| 466 | **lend** | `/lend/` | *verb* | cho vay, cho mượn | **Banks lend money to small business entrepreneurs.**<br>_Các ngân hàng cho những doanh nhân kinh doanh nhỏ vay tiền._ |
| 467 | **wallet** | `/ˈwɒl.ɪt/` | *noun* | ví tiền bỏ túi | **He pulled his driving license from his leather wallet.**<br>_Anh ấy rút bằng lái xe từ chiếc ví da của mình._ |
| 468 | **purse** | `/pɜːs/` | *noun* | ví nữ, túi xách nhỏ | **She searched her purse for the house keys.**<br>_Cô ấy lục tìm chìa khóa nhà trong chiếc ví xách của mình._ |
| 469 | **change** | `/tʃeɪndʒ/` | *noun* | tiền lẻ, tiền thừa | **Here is your change and receipt, sir.**<br>_Đây là tiền thừa và hóa đơn của quý khách, thưa ông._ |
| 470 | **afford** | `/əˈfɔːd/` | *verb* | có đủ khả năng chi trả | **I cannot afford to buy a brand new luxury car.**<br>_Tôi không có đủ khả năng chi trả để mua một chiếc xe hơi sang trọng hoàn toàn mới._ |
| 471 | **can** | `/kæn/` | *verb* | có thể (năng lực, cho phép) | **She can speak three languages fluently.**<br>_Cô ấy có thể nói ba thứ tiếng một cách lưu loát._ |
| 472 | **could** | `/kʊd/` | *verb* | có thể (quá khứ, lịch sự) | **Could you please pass me the salt shaker?**<br>_Bạn có thể vui lòng chuyển cho tôi lọ muối được không?_ |
| 473 | **must** | `/mʌst/` | *verb* | phải (bắt buộc) | **Drivers must obey traffic light signals.**<br>_Các tài xế phải tuân theo tín hiệu đèn giao thông._ |
| 474 | **should** | `/ʃʊd/` | *verb* | nên (lời khuyên) | **You should drink plenty of warm water.**<br>_Bạn nên uống nhiều nước ấm._ |
| 475 | **would** | `/wʊd/` | *verb* | muốn, sẽ (lịch sự) | **Would you like a hot cup of black tea?**<br>_Bạn có muốn dùng một tách trà đen nóng không?_ |
| 476 | **will** | `/wɪl/` | *verb* | sẽ (tương lai) | **We will launch the new mobile application next month.**<br>_Chúng tôi sẽ ra mắt ứng dụng di động mới vào tháng tới._ |
| 477 | **because** | `/bɪˈkəz/` | *conjunction* | bởi vì | **He stayed home because he caught a cold.**<br>_Anh ấy ở nhà bởi vì anh ấy bị cảm lạnh._ |
| 478 | **although** | `/ɔːlˈðəʊ/` | *conjunction* | mặc dù | **Although it rained heavily, we still enjoyed hiking.**<br>_Mặc dù trời mưa to, chúng tôi vẫn rất thích chuyến đi bộ đường dài._ |
| 479 | **however** | `/haʊˈev.ər/` | *adverb* | tuy nhiên | **She practiced hard; however, the competition was fierce.**<br>_Cô ấy đã tập luyện chăm chỉ; tuy nhiên sự cạnh tranh rất khốc liệt._ |
| 480 | **therefore** | `/ˈðeə.fɔːr/` | *adverb* | do đó, vì vậy | **He studied diligently and therefore passed with top marks.**<br>_Anh ấy đã học tập chăm chỉ và do đó đỗ với số điểm cao nhất._ |
| 481 | **and** | `/ænd/` | *conjunction* | và | **I bought fresh apples and sweet oranges.**<br>_Tôi đã mua táo tươi và những quả cam ngọt._ |
| 482 | **but** | `/bʌt/` | *conjunction* | nhưng | **He is very tired but still smiling warmly.**<br>_Anh ấy rất mệt nhưng vẫn mỉm cười ấm áp._ |
| 483 | **or** | `/ɔːr/` | *conjunction* | hoặc, hay là | **Would you prefer iced tea or black coffee?**<br>_Bạn thích trà đá hay cà phê đen hơn?_ |
| 484 | **so** | `/səʊ/` | *conjunction* | vì vậy, cho nên | **The road was blocked, so we took a detour.**<br>_Đường bị chặn, vì vậy chúng tôi đã đi đường vòng._ |
| 485 | **if** | `/ɪf/` | *conjunction* | nếu, giả sử | **If it snows tomorrow, schools will close.**<br>_Nếu ngày mai trời có tuyết rơi, các trường học sẽ đóng cửa._ |
| 486 | **when** | `/wen/` | *conjunction* | khi, lúc | **Call me when you arrive safely at the airport.**<br>_Hãy gọi cho tôi khi bạn đến sân bay an toàn nhé._ |
| 487 | **while** | `/waɪl/` | *conjunction* | trong khi | **She listens to podcasts while jogging in the park.**<br>_Cô ấy nghe podcast trong khi chạy bộ trong công viên._ |
| 488 | **before** | `/bɪˈfɔːr/` | *preposition* | trước khi | **Always wash your hands before eating dinner.**<br>_Luôn rửa tay trước khi ăn tối._ |
| 489 | **after** | `/ˈɑːf.tər/` | *preposition* | sau khi | **We went for a short stroll after dinner.**<br>_Chúng tôi đi dạo một đoạn ngắn sau bữa tối._ |
| 490 | **during** | `/ˈdʒʊə.rɪŋ/` | *preposition* | trong suốt thời gian | **Turn off mobile phones during the movie screening.**<br>_Tắt điện thoại di động trong suốt thời gian chiếu phim._ |
| 491 | **always** | `/ˈɔːl.weɪz/` | *adverb* | luôn luôn (100%) | **She always arrives ten minutes early for meetings.**<br>_Cô ấy luôn luôn đến sớm mười phút cho các cuộc họp._ |
| 492 | **usually** | `/ˈjuː.ʒu.ə.li/` | *adverb* | thường xuyên (80%) | **I usually cook meals at home on weekdays.**<br>_Tôi thường tự nấu ăn ở nhà vào các ngày trong tuần._ |
| 493 | **often** | `/ˈɒf.ən/` | *adverb* | thường (60%) | **We often visit our grandparents on weekends.**<br>_Chúng tôi thường đến thăm ông bà vào cuối tuần._ |
| 494 | **sometimes** | `/ˈsʌm.taɪmz/` | *adverb* | thỉnh thoảng, đôi khi (40%) | **He sometimes rides his bicycle to the office.**<br>_Anh ấy thỉnh thoảng đạp xe đến cơ quan._ |
| 495 | **rarely** | `/ˈreə.li/` | *adverb* | hiếm khi (10%) | **She rarely drinks sugary soft soda drinks.**<br>_Cô ấy hiếm khi uống nước ngọt có ga nhiều đường._ |
| 496 | **never** | `/ˈnev.ər/` | *adverb* | không bao giờ (0%) | **I never skip breakfast before working.**<br>_Tôi không bao giờ bỏ bữa sáng trước khi làm việc._ |
| 497 | **very** | `/ˈver.i/` | *adverb* | rất | **The weather is very pleasant in Hanoi today.**<br>_Thời tiết hôm nay ở Hà Nội rất dễ chịu._ |
| 498 | **really** | `/ˈrɪə.li/` | *adverb* | thực sự, quả thật | **I really appreciate your kind support.**<br>_Tôi thực sự cảm kích sự hỗ trợ tốt bụng của bạn._ |
| 499 | **almost** | `/ˈɔːl.məʊst/` | *adverb* | gần như, hầu như | **Dinner is almost ready to be served.**<br>_Bữa tối gần như đã sẵn sàng để phục vụ._ |
| 500 | **together** | `/təˈɡeð.ər/` | *adverb* | cùng nhau | **We accomplished this challenging project together.**<br>_Chúng tôi đã cùng nhau hoàn thành dự án đầy thử thách này._ |