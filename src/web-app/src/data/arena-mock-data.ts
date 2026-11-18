export interface ArenaQuestion {
  id: string;
  prompt: string;
  definition: string;
  answer: string;
  options: string[];
  category: string;
}

export interface ArenaOpponent {
  name: string;
  initials: string;
  level: string;
  streak: number;
}

export const ARENA_MOCK_QUESTIONS: ArenaQuestion[] = [
  {
    id: "q1",
    prompt: "curious",
    definition: "Muốn biết thêm, thích khám phá điều mới",
    answer: "tò mò",
    options: ["tò mò", "cẩn thận", "bình tĩnh", "hào phóng"],
    category: "A2 · Adjective",
  },
  {
    id: "q2",
    prompt: "achieve",
    definition: "Hoàn thành hoặc đạt được điều bạn đã cố gắng",
    answer: "đạt được",
    options: ["trì hoãn", "đạt được", "thuyết phục", "ghi nhớ"],
    category: "A2 · Verb",
  },
  {
    id: "q3",
    prompt: "journey",
    definition: "Một chuyến đi, thường kéo dài trong một khoảng thời gian",
    answer: "hành trình",
    options: ["thói quen", "hành trình", "mục tiêu", "lời khuyên"],
    category: "A2 · Noun",
  },
  {
    id: "q4",
    prompt: "reliable",
    definition: "Có thể tin cậy, luôn làm đúng điều đã hứa",
    answer: "đáng tin cậy",
    options: ["đáng tin cậy", "tò mò", "linh hoạt", "ồn ào"],
    category: "B1 · Adjective",
  },
  {
    id: "q5",
    prompt: "improve",
    definition: "Trở nên tốt hơn nhờ luyện tập hoặc thay đổi",
    answer: "cải thiện",
    options: ["cải thiện", "so sánh", "đo lường", "mô tả"],
    category: "A2 · Verb",
  },
];

export const ARENA_MOCK_OPPONENT: ArenaOpponent = {
  name: "Minh Anh",
  initials: "MA",
  level: "A2",
  streak: 12,
};
