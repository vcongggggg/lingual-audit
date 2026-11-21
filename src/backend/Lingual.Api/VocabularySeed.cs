using System;
using System.Collections.Generic;
using System.Linq;
using Lingual.Modules.Learning.Domain;

namespace Lingual.Api;

internal static class VocabularySeed
{
    internal static readonly VocabularyGroup[] Groups =
    {
        new("A1", "Nền Tảng Căn Bản", "Từ vựng thiết yếu về bản thân, gia đình và chào hỏi.", 500, 3, 4),
        new("A2", "Giao Tiếp Hằng Ngày", "Mua sắm, hỏi đường và các tình huống quen thuộc.", 800, 3, 6),
        new("B1", "Độc Lập Ngôn Ngữ", "Trình bày quan điểm, xử lý tình huống và học tập.", 1200, 3, 8),
        new("B2", "Lưu Loát & Học Thuật", "Thảo luận chuyên sâu về môi trường, kinh tế và công nghệ.", 1500, 3, 10),
        new("C1", "Tinh Tế & Chuyên Gia", "Cấu trúc thành ngữ, từ ngữ giàu hình tượng và sắc thái.", 2000, 3, 12),
        new("C2", "Bản Xứ Điêu Luyện", "Hiểu trọn vẹn mọi sắc thái văn hóa và chuyên môn.", 2500, 3, 14),
    };

    internal sealed record VocabularyGroup(string level, string title, string description, int target_words, int word_count, int chapters);

    internal static readonly List<VocabularyItem> Words = new()
    {
        W("hello", "xin chào", "A1", "interjection", "/həˈləʊ/", "Hello, my name is Anna.", "Xin chào, tên tôi là Anna."),
        W("family", "gia đình", "A1", "noun", "/ˈfæməli/", "My family lives near the sea.", "Gia đình tôi sống gần biển."),
        W("breakfast", "bữa sáng", "A1", "noun", "/ˈbrekfəst/", "I have breakfast at seven.", "Tôi ăn sáng lúc bảy giờ."),
        W("usually", "thường xuyên", "A2", "adverb", "/ˈjuːʒuəli/", "We usually walk to school.", "Chúng tôi thường đi bộ đến trường."),
        W("journey", "hành trình", "A2", "noun", "/ˈdʒɜːni/", "The journey takes two hours.", "Hành trình mất hai giờ."),
        W("receipt", "hóa đơn", "A2", "noun", "/rɪˈsiːt/", "Could I have a receipt, please?", "Cho tôi xin hóa đơn được không?"),
        W("independent", "độc lập", "B1", "adjective", "/ˌɪndɪˈpendənt/", "She became financially independent.", "Cô ấy đã tự chủ về tài chính."),
        W("opportunity", "cơ hội", "B1", "noun", "/ˌɒpəˈtjuːnəti/", "This is a great opportunity to learn.", "Đây là cơ hội tuyệt vời để học hỏi."),
        W("reliable", "đáng tin cậy", "B1", "adjective", "/rɪˈlaɪəbəl/", "We need a reliable source of information.", "Chúng ta cần một nguồn thông tin đáng tin cậy."),
        W("sustainable", "bền vững", "B2", "adjective", "/səˈsteɪnəbəl/", "Cities need sustainable transport.", "Các thành phố cần giao thông bền vững."),
        W("significant", "đáng kể; quan trọng", "B2", "adjective", "/sɪɡˈnɪfɪkənt/", "The study found a significant change.", "Nghiên cứu phát hiện một thay đổi đáng kể."),
        W("allocate", "phân bổ", "B2", "verb", "/ˈæləkeɪt/", "The team allocated more time to research.", "Nhóm đã phân bổ thêm thời gian cho nghiên cứu."),
        W("ambiguous", "mơ hồ; đa nghĩa", "C1", "adjective", "/æmˈbɪɡjuəs/", "The wording was deliberately ambiguous.", "Cách diễn đạt được cố ý làm cho mơ hồ."),
        W("compelling", "đầy sức thuyết phục", "C1", "adjective", "/kəmˈpelɪŋ/", "She made a compelling argument.", "Cô ấy đưa ra lập luận đầy sức thuyết phục."),
        W("intrinsic", "vốn có; nội tại", "C1", "adjective", "/ɪnˈtrɪnsɪk/", "Curiosity has intrinsic value.", "Sự tò mò có giá trị tự thân."),
        W("ubiquitous", "có mặt khắp nơi", "C2", "adjective", "/juːˈbɪkwɪtəs/", "Smartphones are ubiquitous today.", "Điện thoại thông minh ngày nay có mặt khắp nơi."),
        W("nuance", "sắc thái tinh tế", "C2", "noun", "/ˈnjuːɑːns/", "Translation must preserve every nuance.", "Bản dịch phải giữ được mọi sắc thái."),
        W("resilience", "khả năng phục hồi", "C2", "noun", "/rɪˈzɪliəns/", "Resilience helped the community recover.", "Khả năng phục hồi giúp cộng đồng hồi phục."),
    };

    private static VocabularyItem W(string term, string meaning, string level, string pos, string ipa, string sentence, string translation) => new()
    {
        Id = Guid.NewGuid(), Term = term, NormalizedTerm = term, SourceLanguage = "en", TargetLanguage = "vi",
        Meaning = meaning, CefrLevel = level, PartOfSpeech = pos, Ipa = ipa, Status = "published",
        Examples = System.Text.Json.JsonSerializer.Serialize(new[] { new { source_text = sentence, target_text = translation } })
    };
}
