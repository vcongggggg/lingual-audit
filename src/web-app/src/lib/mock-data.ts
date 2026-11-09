import type { VocabularyItem } from "@/components/FlashcardStudy";

export const MOCK_VOCABULARY: VocabularyItem[] = [
  { id: "mock-word-01", term: "curious", normalized_term: "curious", source_language: "en", target_language: "vi", ipa: "/ˈkjʊəriəs/", cefr_level: "A2", part_of_speech: "adjective", meaning: "tò mò; ham tìm hiểu", examples: [{ source_text: "The curious student asked one more question.", target_text: "Bạn học sinh ham tìm hiểu hỏi thêm một câu nữa." }] },
  { id: "mock-word-02", term: "journey", normalized_term: "journey", source_language: "en", target_language: "vi", ipa: "/ˈdʒɜːrni/", cefr_level: "A2", part_of_speech: "noun", meaning: "hành trình; chuyến đi", examples: [{ source_text: "Learning a language is a long journey.", target_text: "Học một ngôn ngữ là một hành trình dài." }] },
  { id: "mock-word-03", term: "improve", normalized_term: "improve", source_language: "en", target_language: "vi", ipa: "/ɪmˈpruːv/", cefr_level: "A2", part_of_speech: "verb", meaning: "cải thiện; tiến bộ", examples: [{ source_text: "A little practice can improve your speaking.", target_text: "Luyện tập một chút có thể cải thiện kỹ năng nói của bạn." }] },
  { id: "mock-word-04", term: "achieve", normalized_term: "achieve", source_language: "en", target_language: "vi", ipa: "/əˈtʃiːv/", cefr_level: "A2", part_of_speech: "verb", meaning: "đạt được; hoàn thành", examples: [{ source_text: "She worked hard to achieve her goal.", target_text: "Cô ấy chăm chỉ để đạt được mục tiêu." }] },
  { id: "mock-word-05", term: "habit", normalized_term: "habit", source_language: "en", target_language: "vi", ipa: "/ˈhæbɪt/", cefr_level: "A1", part_of_speech: "noun", meaning: "thói quen", examples: [{ source_text: "Reading every day is a good habit.", target_text: "Đọc sách mỗi ngày là một thói quen tốt." }] },
  { id: "mock-word-06", term: "careful", normalized_term: "careful", source_language: "en", target_language: "vi", ipa: "/ˈkerfəl/", cefr_level: "A1", part_of_speech: "adjective", meaning: "cẩn thận", examples: [{ source_text: "Be careful when you cross the street.", target_text: "Hãy cẩn thận khi băng qua đường." }] },
  { id: "mock-word-07", term: "confident", normalized_term: "confident", source_language: "en", target_language: "vi", ipa: "/ˈkɒnfɪdənt/", cefr_level: "A2", part_of_speech: "adjective", meaning: "tự tin", examples: [{ source_text: "Practice makes learners more confident.", target_text: "Luyện tập giúp người học tự tin hơn." }] },
  { id: "mock-word-08", term: "discover", normalized_term: "discover", source_language: "en", target_language: "vi", ipa: "/dɪˈskʌvə(r)/", cefr_level: "A2", part_of_speech: "verb", meaning: "khám phá; phát hiện", examples: [{ source_text: "We discover new words every day.", target_text: "Chúng ta khám phá những từ mới mỗi ngày." }] },
];

export type MockLeaderboardEntry = {
  rank: number;
  user_id: string;
  name: string;
  xp: number;
  previous_rank?: number;
  level: string;
  words_learned: number;
};

export const MOCK_LEADERBOARD: MockLeaderboardEntry[] = [
  { rank: 1, user_id: "mock-01", name: "Minh Anh", xp: 2840, previous_rank: 2, level: "B1 · Tiến bộ", words_learned: 184 },
  { rank: 2, user_id: "mock-02", name: "Hoàng Nam", xp: 2510, previous_rank: 1, level: "A2 · Bền bỉ", words_learned: 156 },
  { rank: 3, user_id: "mock-03", name: "Thu Hà", xp: 2195, previous_rank: 3, level: "A2 · Đều đặn", words_learned: 139 },
  { rank: 4, user_id: "mock-04", name: "Gia Huy", xp: 1880, previous_rank: 5, level: "A2", words_learned: 112 },
  { rank: 5, user_id: "mock-05", name: "Ngọc Linh", xp: 1640, previous_rank: 4, level: "A1", words_learned: 98 },
  { rank: 6, user_id: "mock-06", name: "Đức Anh", xp: 1415, previous_rank: 6, level: "A1", words_learned: 82 },
  { rank: 7, user_id: "mock-07", name: "Khánh Vy", xp: 1190, previous_rank: 8, level: "A1", words_learned: 71 },
  { rank: 8, user_id: "mock-08", name: "Tuấn Kiệt", xp: 980, previous_rank: 7, level: "A1", words_learned: 64 },
];
