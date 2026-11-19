export type MockQuestionType =
  | "multiple_choice"
  | "listening_choice"
  | "spelling"
  | "dictation"
  | "word_matching"
  | "sentence_scramble"
  | "image_choice"
  | "pronunciation";

export type QuizOption = {
  key: string;
  text: string;
  is_correct?: boolean;
  image_url?: string;
};

export type MockQuestion = {
  id: string;
  level: string;
  difficulty: number;
  question_type: MockQuestionType;
  prompt: string;
  prompt_audio_url?: string;
  prompt_audio_text?: string;
  options: QuizOption[];
  correct_answer: string;
  explanation: string;
  points: number;
  payload?: {
    expected_text?: string;
    tokens?: string[];
    pairs?: { left: string; right: string }[];
  };
};

export const quizTypeOptions: { id: MockQuestionType; label: string; description: string }[] = [
  { id: "multiple_choice", label: "Chọn đáp án", description: "Chọn từ phù hợp với câu" },
  { id: "listening_choice", label: "Nghe chọn nghĩa", description: "Nghe tiếng Anh rồi chọn nghĩa Việt" },
  { id: "spelling", label: "Nghe chọn từ", description: "Nghe và chọn cách viết đúng" },
  { id: "dictation", label: "Nghe điền từ", description: "Nghe audio và nhập lại" },
  { id: "word_matching", label: "Nối từ với nghĩa", description: "Ghép từ tiếng Anh với tiếng Việt" },
  { id: "sentence_scramble", label: "Sắp xếp câu", description: "Kéo các từ thành câu đúng" },
  { id: "image_choice", label: "Chọn hình ảnh", description: "Chọn hình minh họa phù hợp" },
  { id: "pronunciation", label: "Phát âm", description: "Đọc câu và nhận diện bằng giọng nói" },
];

export const mockQuestions: MockQuestion[] = [
  {
    id: "mock-multiple-choice-breakfast", level: "A1", difficulty: 1, question_type: "multiple_choice",
    prompt: "Choose the word that completes the sentence: I eat ___ every morning.",
    options: [{ key: "A", text: "breakfast", is_correct: true }, { key: "B", text: "midnight" }, { key: "C", text: "yesterday" }, { key: "D", text: "quickly" }],
    correct_answer: "A", explanation: "Breakfast là bữa sáng, phù hợp với every morning.", points: 10,
  },
  {
    id: "mock-listening-hello", level: "A1", difficulty: 1, question_type: "listening_choice",
    prompt: "Nghe và chọn nghĩa đúng.", prompt_audio_text: "Hello",
    options: [{ key: "A", text: "Xin chào", is_correct: true }, { key: "B", text: "Tạm biệt" }, { key: "C", text: "Cảm ơn" }, { key: "D", text: "Xin lỗi" }],
    correct_answer: "A", explanation: "Hello nghĩa là xin chào.", points: 10,
  },
  {
    id: "mock-spelling-school", level: "A1", difficulty: 1, question_type: "spelling",
    prompt: "Nghe và chọn cách viết đúng.", prompt_audio_text: "School",
    options: [{ key: "A", text: "scool" }, { key: "B", text: "school", is_correct: true }, { key: "C", text: "shcool" }, { key: "D", text: "schoole" }],
    correct_answer: "B", explanation: "School được viết là school.", points: 10,
  },
  {
    id: "mock-dictation-book", level: "A1", difficulty: 2, question_type: "dictation",
    prompt: "Nghe audio và nhập lại từ bạn nghe được.", prompt_audio_text: "Book",
    options: [], correct_answer: "book", explanation: "Đáp án là book (quyển sách).", points: 10,
    payload: { expected_text: "book" },
  },
  {
    id: "mock-matching", level: "A1", difficulty: 2, question_type: "word_matching",
    prompt: "Nối mỗi từ tiếng Anh với nghĩa tiếng Việt.", options: [],
    correct_answer: "0:0,1:1,2:2", explanation: "Apple là quả táo, house là ngôi nhà, water là nước.", points: 10,
    payload: { pairs: [{ left: "apple", right: "quả táo" }, { left: "house", right: "ngôi nhà" }, { left: "water", right: "nước" }] },
  },
  {
    id: "mock-scramble", level: "A1", difficulty: 2, question_type: "sentence_scramble",
    prompt: "Sắp xếp các từ thành câu đúng.", options: [{ key: "0", text: "I" }, { key: "1", text: "like" }, { key: "2", text: "English" }],
    correct_answer: "I like English", explanation: "Câu đúng: I like English.", points: 10,
    payload: { tokens: ["I", "like", "English"] },
  },
  {
    id: "mock-image", level: "A1", difficulty: 1, question_type: "image_choice",
    prompt: "Nghe từ và chọn hình ảnh đúng.", prompt_audio_text: "Cat",
    options: [{ key: "A", text: "Con mèo", image_url: "/quiz-assets/cat.svg", is_correct: true }, { key: "B", text: "Con chó", image_url: "/quiz-assets/dog.svg" }, { key: "C", text: "Con cá", image_url: "/quiz-assets/fish.svg" }, { key: "D", text: "Con chim", image_url: "/quiz-assets/bird.svg" }],
    correct_answer: "A", explanation: "Cat nghĩa là con mèo.", points: 10,
  },
  {
    id: "mock-pronunciation", level: "A1", difficulty: 3, question_type: "pronunciation",
    prompt: "Đọc câu: Good morning.", options: [], correct_answer: "good morning",
    explanation: "Hãy đọc gần giống “Good morning”. Đây là bản mock, chưa chấm điểm phát âm chuyên sâu.", points: 10,
    payload: { expected_text: "good morning" },
  },
  {
    id: "mock-multiple-choice-conditionals", level: "B1", difficulty: 3, question_type: "multiple_choice",
    prompt: "If I had known about the traffic, I ___ earlier.",
    options: [{ key: "A", text: "would have left", is_correct: true }, { key: "B", text: "will leave" }, { key: "C", text: "have left" }, { key: "D", text: "would leave" }],
    correct_answer: "A", explanation: "Câu điều kiện loại 3 dùng would have + past participle.", points: 20,
  },
  {
    id: "mock-listening-interview", level: "B1", difficulty: 3, question_type: "listening_choice", prompt: "Nghe câu và chọn ý chính.", prompt_audio_text: "The meeting has been postponed until next Thursday.",
    options: [{ key: "A", text: "Cuộc họp đã bị hủy" }, { key: "B", text: "Cuộc họp được dời sang thứ Năm tuần sau", is_correct: true }, { key: "C", text: "Cuộc họp diễn ra hôm nay" }, { key: "D", text: "Cuộc họp bắt đầu sớm hơn" }],
    correct_answer: "B", explanation: "Postponed until next Thursday nghĩa là được dời sang thứ Năm tuần sau.", points: 20,
  },
  {
    id: "mock-spelling-necessary", level: "B1", difficulty: 3, question_type: "spelling", prompt: "Nghe và chọn cách viết đúng.", prompt_audio_text: "Necessary",
    options: [{ key: "A", text: "neccessary" }, { key: "B", text: "necessary", is_correct: true }, { key: "C", text: "necesary" }, { key: "D", text: "necessery" }],
    correct_answer: "B", explanation: "Necessary được viết với một c ở giữa và hai s.", points: 20,
  },
  {
    id: "mock-dictation-environment", level: "B1", difficulty: 3, question_type: "dictation", prompt: "Nghe audio và nhập lại câu bạn nghe được.", prompt_audio_text: "We should protect the environment for future generations.",
    options: [], correct_answer: "We should protect the environment for future generations.", explanation: "Chú ý protect, environment và future generations.", points: 20,
    payload: { expected_text: "We should protect the environment for future generations." },
  },
  {
    id: "mock-matching-advanced", level: "B1", difficulty: 3, question_type: "word_matching", prompt: "Nối từ học thuật với nghĩa phù hợp.", options: [],
    correct_answer: "0:0,1:1,2:2", explanation: "Reliable là đáng tin cậy, improve là cải thiện, evidence là bằng chứng.", points: 20,
    payload: { pairs: [{ left: "reliable", right: "đáng tin cậy" }, { left: "improve", right: "cải thiện" }, { left: "evidence", right: "bằng chứng" }] },
  },
  {
    id: "mock-scramble-advanced", level: "B1", difficulty: 3, question_type: "sentence_scramble",
    prompt: "Sắp xếp các từ thành câu đúng.", options: [{ key: "0", text: "Although" }, { key: "1", text: "it was raining" }, { key: "2", text: "we went for a walk" }],
    correct_answer: "Although it was raining we went for a walk", explanation: "Mệnh đề nhượng bộ Although đứng trước mệnh đề chính.", points: 20,
    payload: { tokens: ["Although", "it was raining", "we went for a walk"] },
  },
  {
    id: "mock-image-advanced", level: "B1", difficulty: 3, question_type: "image_choice", prompt: "Nghe khái niệm và chọn hình ảnh phù hợp.", prompt_audio_text: "A crowded city street",
    options: [{ key: "A", text: "Một căn phòng yên tĩnh", image_url: "/quiz-assets/cat.svg" }, { key: "B", text: "Một bãi biển vắng", image_url: "/quiz-assets/fish.svg" }, { key: "C", text: "Một khu rừng", image_url: "/quiz-assets/bird.svg" }, { key: "D", text: "Một con phố đông đúc", image_url: "/quiz-assets/city.svg", is_correct: true }],
    correct_answer: "D", explanation: "A crowded city street nghĩa là một con phố thành phố đông đúc.", points: 20,
  },
  {
    id: "mock-pronunciation-advanced", level: "B1", difficulty: 3, question_type: "pronunciation",
    prompt: "Đọc câu: Could you explain that one more time?", options: [], correct_answer: "could you explain that one more time",
    explanation: "Hãy nối âm tự nhiên ở Could you và giữ trọng âm ở explain, more time.", points: 20,
    payload: { expected_text: "Could you explain that one more time?" },
  },
];
