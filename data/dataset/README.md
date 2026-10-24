# Kho Dữ Liệu Từ Vựng & Đề Thi Chuẩn Hóa (Dataset Trích Xuất Từ LinguaFlow)

Thư mục này chứa bộ dữ liệu chuẩn hóa từ vựng và câu hỏi được chuyển giao từ dự án **LinguaFlow** sang **Mezon Campus Studio (LINGUAL)**.

---

## 1. File Dữ Liệu Chi Tiết

### A. [vocab-bulk.json](file:///c:/Study/HocKy6/MezonCampusStudio/data/dataset/vocab-bulk.json) (Dung lượng: ~2.5 MB)
Bộ từ vựng tổng hợp Oxford 5000 kết hợp Tatoeba và OVDP, đã được chuẩn hóa với các trường thông tin:
- `targetText`: Từ vựng tiếng Anh (ví dụ: `abandon`, `ability`, `academic`...).
- `translation`: Nghĩa tiếng Việt đầy đủ, chính xác.
- `phonetic`: Phiên âm quốc tế IPA chuẩn (ví dụ: `/əˈbændən/`).
- `partOfSpeech`: Từ loại (`noun`, `verb`, `adjective`, `adverb`...).
- `cefrLevel`: Phân cấp độ chuẩn khung châu Âu (`A1`, `A2`, `B1`, `B2`, `C1`, `C2`).
- `exampleSentence`: Câu ví dụ ngữ cảnh tiếng Anh.
- `exampleTranslation`: Dịch câu ví dụ sang tiếng Việt.
- `audioUrl`: Đường dẫn audio mẫu chuẩn IPA.

### B. [oxford-5k.csv](file:///c:/Study/HocKy6/MezonCampusStudio/data/dataset/oxford-5k.csv) (Dung lượng: ~1.1 MB)
Bảng tra cứu dạng CSV phục vụ import nhanh hoặc đối soát:
- Cột: `word`, `phonetics`, `pos`, `cefr_level`, `meaning_vi`, `example_en`, `example_vi`.

---

## 2. Ứng Dụng Trong Dự Án LINGUAL

1. **Học tập Flashcard (Spaced Repetition SM-2):**
   - Đủ dữ liệu chia theo từng cấp độ A1, A2, B1 để người học mở khóa thẻ học mà không cần gõ tay thủ công.
2. **Sinh đề Quiz & Minigame Đấu trường 1v1:**
   - Dùng trường `translation` và các từ cùng từ loại trong dataset để sinh các phương án nhiễu (distractors) tự động cho câu hỏi trắc nghiệm 4 đáp án.
3. **Database Seed / Migration:**
   - Dùng để nạp trực tiếp vào bảng `vocabularies` trong PostgreSQL qua script EF Core hoặc Dapper.
