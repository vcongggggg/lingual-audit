using Lingual.Modules.Learning.Domain;

namespace Lingual.Api;

internal static class VocabularyTopics
{
    private static readonly (string Id, string Title, string Description, string[] Terms)[] Topics =
    {
        ("people", "Con người & gia đình", "Quan hệ, gia đình và đặc điểm con người.", new[] { "family", "mother", "father", "parent", "sister", "brother", "child", "children", "friend", "people", "person", "woman", "man", "husband", "wife", "baby", "adult", "human", "relative", "neighbor", "kind", "friendly", "personality" }),
        ("food", "Đồ ăn & nấu nướng", "Thực phẩm, bữa ăn, đồ uống và cách chế biến.", new[] { "food", "eat", "drink", "meal", "breakfast", "lunch", "dinner", "cook", "kitchen", "bread", "rice", "meat", "fruit", "vegetable", "coffee", "tea", "restaurant", "recipe", "taste", "hungry", "sweet", "salt", "sugar" }),
        ("home", "Nhà cửa & sinh hoạt", "Không gian sống, đồ dùng và hoạt động thường ngày.", new[] { "home", "house", "room", "bedroom", "bathroom", "furniture", "door", "window", "garden", "clean", "sleep", "wake", "clothes", "wear", "wash", "bath", "live", "apartment", "flat", "building" }),
        ("travel", "Du lịch & giao thông", "Địa điểm, phương tiện và các chuyến đi.", new[] { "travel", "journey", "trip", "tour", "tourist", "airport", "airplane", "plane", "train", "bus", "station", "ticket", "hotel", "visit", "holiday", "vacation", "road", "drive", "ride", "flight", "passport", "luggage", "map", "direction" }),
        ("education", "Học tập & giáo dục", "Trường lớp, học tập và kiến thức.", new[] { "school", "student", "teacher", "learn", "teach", "study", "education", "lesson", "class", "course", "university", "college", "exam", "test", "homework", "book", "read", "write", "language", "knowledge", "skill", "degree" }),
        ("work", "Công việc & kinh doanh", "Nghề nghiệp, nơi làm việc và hoạt động kinh doanh.", new[] { "work", "job", "career", "office", "business", "company", "worker", "employer", "employee", "manager", "meeting", "profession", "salary", "wage", "career", "market", "customer", "service", "industry", "trade", "finance", "economy" }),
        ("health", "Sức khỏe & cơ thể", "Cơ thể, chăm sóc sức khỏe và y tế.", new[] { "health", "healthy", "body", "head", "face", "hand", "heart", "foot", "eye", "doctor", "hospital", "medicine", "medical", "ill", "sick", "pain", "disease", "patient", "exercise", "sport", "sleep", "tired", "stress" }),
        ("nature", "Thiên nhiên & môi trường", "Thời tiết, động thực vật và môi trường tự nhiên.", new[] { "nature", "environment", "weather", "rain", "wind", "sun", "cloud", "tree", "flower", "plant", "animal", "bird", "water", "river", "sea", "ocean", "mountain", "earth", "climate", "pollution", "forest", "energy", "sustainable" }),
        ("technology", "Khoa học & công nghệ", "Khoa học, thiết bị và đổi mới công nghệ.", new[] { "technology", "computer", "internet", "digital", "phone", "software", "science", "scientist", "research", "data", "information", "system", "machine", "device", "online", "website", "program", "programming", "innovation", "technical", "electricity", "technology" }),
        ("communication", "Giao tiếp & xã hội", "Ngôn ngữ, truyền thông và các mối quan hệ xã hội.", new[] { "speak", "talk", "say", "tell", "ask", "answer", "conversation", "communicate", "communication", "message", "news", "media", "social", "community", "society", "culture", "discuss", "explain", "opinion", "agree", "argue", "relationship" }),
        ("shopping", "Mua sắm & tiền bạc", "Mua bán, giá cả và quản lý tiền.", new[] { "buy", "sell", "shop", "shopping", "store", "price", "cost", "pay", "money", "cash", "bank", "bill", "receipt", "cheap", "expensive", "customer", "credit", "save", "spend", "purchase", "product" }),
        ("places", "Địa điểm & nơi chốn", "Thành phố, địa chỉ và những nơi thường gặp.", new[] { "place", "city", "town", "village", "country", "street", "road", "address", "area", "center", "centre", "park", "library", "museum", "station", "airport", "beach", "farm", "church", "restaurant", "shop", "store", "school", "office", "building", "capital", "square", "bridge" }),
        ("clothing", "Quần áo & phụ kiện", "Trang phục, giày dép và phụ kiện cá nhân.", new[] { "clothes", "clothing", "shirt", "t-shirt", "trousers", "pants", "jeans", "skirt", "dress", "coat", "jacket", "shoes", "boots", "socks", "hat", "cap", "scarf", "glove", "wear", "fashion", "suit", "uniform" }),
        ("objects", "Đồ vật & vật dụng", "Đồ dùng quen thuộc trong học tập và sinh hoạt.", new[] { "thing", "object", "bag", "box", "key", "bottle", "cup", "glass", "plate", "spoon", "fork", "knife", "table", "chair", "desk", "lamp", "clock", "picture", "paper", "pen", "pencil", "wallet", "umbrella", "gift", "toy", "tool", "machine", "device" }),
        ("colors", "Màu sắc & hình dáng", "Màu sắc, kích thước và hình dạng cơ bản.", new[] { "color", "colour", "black", "white", "red", "blue", "green", "yellow", "brown", "pink", "orange", "purple", "gray", "grey", "shape", "circle", "square", "round", "size", "large", "small", "big", "tiny", "wide", "narrow" }),
        ("emotions", "Cảm xúc & tính cách", "Cảm xúc, thái độ và cách mô tả tính cách.", new[] { "feel", "feeling", "happy", "sad", "angry", "afraid", "fear", "love", "like", "hate", "hope", "worry", "surprise", "excited", "upset", "calm", "confident", "curious", "kind", "proud", "disappointed", "emotion" }),
        ("time", "Thời gian & số lượng", "Ngày tháng, thời điểm và số lượng.", new[] { "time", "day", "week", "month", "year", "today", "tomorrow", "yesterday", "morning", "evening", "night", "hour", "minute", "second", "date", "early", "late", "always", "often", "sometimes", "never", "number", "amount", "total" }),
        ("arts", "Nghệ thuật & giải trí", "Âm nhạc, phim ảnh, thể thao và hoạt động giải trí.", new[] { "art", "music", "song", "film", "movie", "cinema", "theater", "dance", "paint", "picture", "photo", "game", "play", "fun", "sport", "football", "concert", "show", "actor", "artist", "entertainment", "hobby" }),
        ("general", "Từ vựng tổng hợp", "Các từ thông dụng thuộc nhiều tình huống khác nhau.", Array.Empty<string>())
    };

    public static IReadOnlyList<TopicInfo> All => Topics.Select(topic => new TopicInfo(topic.Id, topic.Title, topic.Description)).ToArray();

    public static TopicInfo Classify(VocabularyItem word)
    {
        var tokens = word.Term.ToLowerInvariant()
            .Split(new[] { ' ', '-', '_', '.', '/', '\\', '\'', '’', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var topic in Topics.Take(Topics.Length - 1))
        {
            if (topic.Terms.Any(tokens.Contains))
                return new TopicInfo(topic.Id, topic.Title, topic.Description);
        }
        return word.PartOfSpeech?.ToLowerInvariant() switch
        {
            "noun" => new TopicInfo("things", "Sự vật & khái niệm", "Danh từ thông dụng chưa thuộc một chủ đề cụ thể."),
            "verb" => new TopicInfo("actions", "Hành động & hoạt động", "Động từ dùng trong các hành động và hoạt động thường gặp."),
            "adjective" => new TopicInfo("qualities", "Đặc điểm & tính chất", "Tính từ mô tả đặc điểm, trạng thái và tính chất."),
            "adverb" => new TopicInfo("manner", "Cách thức & mức độ", "Trạng từ bổ nghĩa cho hành động, thời gian và mức độ."),
            _ => new TopicInfo("useful-words", "Từ chức năng & diễn đạt", "Các từ hỗ trợ cấu trúc câu và cách diễn đạt.")
        };
    }

    public sealed record TopicInfo(string Id, string Title, string Description);
}
