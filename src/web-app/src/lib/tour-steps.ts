export type TourPageKey =
  | "home"
  | "words"
  | "learn"
  | "quiz"
  | "games"
  | "arena"
  | "tutor"
  | "leaderboard"
  | "fallback";

export type SpotlightPlacement = "auto" | "top" | "bottom" | "left" | "right";

export interface SpotlightStep {
  id: string;
  target: string;
  badgeVi: string;
  badgeEn: string;
  titleVi: string;
  titleEn: string;
  descVi: string;
  descEn: string;
  tipVi?: string;
  tipEn?: string;
  placement?: SpotlightPlacement;
  padding?: number;
  radius?: number;
}

export function resolveTourPageKey(pathname: string): TourPageKey {
  if (pathname === "/") return "home";
  if (pathname === "/words") return "words";
  if (pathname === "/learn") return "learn";
  if (pathname === "/quiz") return "quiz";
  if (pathname === "/games") return "games";
  if (pathname === "/arena") return "arena";
  if (pathname === "/tutor") return "tutor";
  if (pathname === "/leaderboard") return "leaderboard";
  return "fallback";
}

export function isAutoTourPage(pageKey: TourPageKey): boolean {
  return pageKey !== "fallback";
}

export function getTourStorageKey(pageKey: TourPageKey): string {
  return `lingual_spotlight_v1_${pageKey}`;
}

export function getTourStepsForPage(pageKey: TourPageKey): SpotlightStep[] {
  switch (pageKey) {
    case "home":
      return [
        {
          id: "home-nav",
          target: '[data-tour="nav-tabs"]',
          badgeVi: "ĐIỀU HƯỚNG",
          badgeEn: "NAVIGATION",
          titleVi: "Thanh điều hướng 6 khu vực học tập",
          titleEn: "6 Core Learning Destinations",
          descVi:
            "Chuyển nhanh giữa Trang tổng quan, Học từ (Kho từ & Flashcard), Luyện tập (Quiz), Đấu trường (Game Center & 1vs1), Gia sư LingLing AI và Giải đấu.",
          descEn:
            "Jump quickly between Overview, Vocabulary (Library & Flashcards), Practice (Quiz), Arena (Game Center & 1v1), LingLing AI Tutor, and Leaderboard.",
          tipVi: "Nút dấu hỏi (?) góc phải thanh điều hướng luôn sẵn sàng mở lại hướng dẫn cho trang bạn đang xem.",
          tipEn: "The (?) button on the top bar reopens the interactive walkthrough for your current page anytime.",
          placement: "bottom",
          padding: 8,
          radius: 14,
        },
        {
          id: "home-daily-goal",
          target: '[data-tour="home-daily-goal"]',
          badgeVi: "MỤC TIÊU NGÀY",
          badgeEn: "DAILY GOAL",
          titleVi: "Theo dõi mục tiêu mỗi ngày",
          titleEn: "Track Your Daily Target",
          descVi:
            "Thanh tiến độ hiển thị số từ đã ôn (mục tiêu 10 từ/ngày) và lượng XP kiếm được hôm nay (mục tiêu 50 XP/ngày), giúp bạn giữ nhịp học đều đặn chỉ với 5–10 phút.",
          descEn:
            "Monitor words reviewed today (10-word target) and daily XP earned (50 XP target) to keep a steady 5–10 minute habit.",
          placement: "bottom",
          padding: 10,
          radius: 16,
        },
        {
          id: "home-streak",
          target: '[data-tour="home-streak"]',
          badgeVi: "CHUỖI STREAK",
          badgeEn: "LEARNING STREAK",
          titleVi: "Giữ lửa chuỗi ngày học liên tục",
          titleEn: "Keep Your Daily Streak Alive",
          descVi:
            "Mỗi ngày hoàn thành ít nhất một phiên ôn Flashcard, Quiz hoặc Mini-game để tăng chuỗi Streak và mở khóa các huy hiệu danh dự.",
          descEn:
            "Complete at least one Flashcard session, Quiz, or Mini-game daily to grow your streak and unlock honor badges.",
          placement: "left",
          padding: 10,
          radius: 18,
        },
        {
          id: "home-stats",
          target: '[data-tour="home-stats"]',
          badgeVi: "CHỈ SỐ TIẾN ĐỘ",
          badgeEn: "PROGRESS METRICS",
          titleVi: "Tổng quan thành tích & kho từ vựng",
          titleEn: "Your Learning Snapshot",
          descVi:
            "Cập nhật trực tiếp tổng XP tích lũy trên hồ sơ, số bài Quiz đã hoàn thành, hoạt động trong 7 ngày gần đây và tổng số từ vựng CEFR (A1–C2) sẵn sàng học.",
          descEn:
            "See live profile XP, completed quizzes, recent 7-day activity, and total CEFR (A1–C2) words ready to study.",
          placement: "bottom",
          padding: 10,
          radius: 16,
        },
        {
          id: "home-wotd",
          target: '[data-tour="home-wotd"]',
          badgeVi: "TỪ VỰNG HÔM NAY",
          badgeEn: "WORD OF THE DAY",
          titleVi: "Học nhanh 1 từ mới mỗi ngày",
          titleEn: "Discover One New Word Daily",
          descVi:
            "Mỗi ngày hệ thống gợi ý một từ tiếng Anh kèm phiên âm IPA, nghĩa tiếng Việt và câu ví dụ thực tế. Bạn có thể bấm nghe phát âm, đổi sang từ khác hoặc vào học ngay.",
          descEn:
            "A featured English word with IPA pronunciation, meaning, and example sentence. Listen, shuffle another word, or jump right into studying.",
          placement: "top",
          padding: 10,
          radius: 18,
        },
        {
          id: "home-activities",
          target: '[data-tour="home-activities"]',
          badgeVi: "HOẠT ĐỘNG HỌC TẬP",
          badgeEn: "LEARNING ACTIVITIES",
          titleVi: "6 chế độ học & luyện tập tương tác",
          titleEn: "6 Interactive Ways to Learn",
          descVi:
            "Chọn ngay hình thức bạn thích: khám phá Kho từ CEFR, ôn Flashcard SM-2, làm Quiz nhanh, chơi 6 Mini-game, luyện hội thoại với LingLing AI hoặc đấu 1vs1.",
          descEn:
            "Pick how you want to practice: CEFR Library, SM-2 Flashcards, Quick Quiz, 6 Mini-games, LingLing AI Tutor, or 1v1 Arena.",
          tipVi: "Hoàn thành Game (>=50%), Quiz (>=60%) hoặc ôn >=5 thẻ Flashcard mỗi ngày sẽ thưởng thêm lượt hỏi LingLing AI!",
          tipEn: "Finishing a Game (>=50%), Quiz (>=60%), or >=5 Flashcards daily awards +1 bonus LingLing AI turn!",
          placement: "top",
          padding: 10,
          radius: 18,
        },
        {
          id: "home-weekly",
          target: '[data-tour="home-weekly"]',
          badgeVi: "BIỂU ĐỒ TUẦN",
          badgeEn: "WEEKLY ACTIVITY",
          titleVi: "Biểu đồ tiến độ 7 ngày & 30 ngày",
          titleEn: "7-Day & 30-Day Progress Chart",
          descVi:
            "Theo dõi số lượng từ vựng và XP bạn đạt được theo từng ngày. Chuyển linh hoạt giữa chế độ 7 ngày và 30 ngày để nhìn rõ sự tiến bộ.",
          descEn:
            "Track words learned and XP earned day by day. Toggle between 7-day and 30-day views to visualize your momentum.",
          placement: "left",
          padding: 10,
          radius: 18,
        },
      ];

    case "words":
      return [
        {
          id: "words-subnav",
          target: '[data-tour="vocab-subnav"]',
          badgeVi: "CHUYỂN NHANH",
          badgeEn: "QUICK SWITCH",
          titleVi: "Chuyển giữa Thư viện từ vựng & Ôn Flashcard",
          titleEn: "Switch Between Library & SM-2 Flashcards",
          descVi:
            "Dùng Thư viện từ vựng để duyệt lộ trình theo từng chương/chủ đề, hoặc bấm Ôn Flashcard SM-2 để vào thẳng phiên lật thẻ 2 cột ngay lập tức.",
          descEn:
            "Browse vocabulary by chapter and topic here, or switch straight to the 2-column SM-2 Flashcards session.",
          placement: "bottom",
          padding: 8,
          radius: 14,
        },
        {
          id: "words-tabs",
          target: ".vocab-tabs",
          badgeVi: "DANH MỤC TỪ",
          badgeEn: "CATEGORIES",
          titleVi: "Phân loại lộ trình từ vựng chuẩn CEFR",
          titleEn: "Structured CEFR Vocabulary Tracks",
          descVi:
            "Hệ thống tổ chức từ vựng khoa học theo bậc CEFR (A1–C2), chứng chỉ IELTS/TOEIC và các chủ đề giao tiếp thực tế.",
          descEn:
            "Explore curated word lists organized by CEFR levels (A1–C2), IELTS/TOEIC certificates, and practical topics.",
          placement: "bottom",
          padding: 8,
          radius: 12,
        },
        {
          id: "words-progress",
          target: ".vocab-progress-section",
          badgeVi: "TIẾN ĐỘ CÁ NHÂN",
          badgeEn: "YOUR PROGRESS",
          titleVi: "Ôn lại Từ đã học & thẻ đến hạn SM-2",
          titleEn: "Review Learned & Due Words",
          descVi:
            "Xem nhanh tổng số từ bạn đã lưu và số thẻ đến hạn ôn hôm nay. Khi đã đăng nhập, bấm trực tiếp vào thẻ để mở bộ ôn tập cá nhân hóa.",
          descEn:
            "See how many words you have saved and how many SM-2 cards are due today. Sign in to launch your personalized review deck in one click.",
          placement: "bottom",
          padding: 10,
          radius: 16,
        },
        {
          id: "words-search",
          target: ".vocab-search",
          badgeVi: "TÌM KIẾM NHANH",
          badgeEn: "INSTANT SEARCH",
          titleVi: "Tra cứu từ vựng, chủ đề hoặc cấp độ",
          titleEn: "Search Words, Levels, or Topics",
          descVi:
            "Gõ bất kỳ từ tiếng Anh, nghĩa tiếng Việt hoặc mã cấp độ (A1, B2...) để tìm ngay trong thư viện và mở Flashcard của từ đó.",
          descEn:
            "Type any English term, Vietnamese meaning, or CEFR level (A1, B2...) to find words instantly and open them as flashcards.",
          placement: "bottom",
          padding: 8,
          radius: 12,
        },
        {
          id: "words-levels",
          target: ".vocab-level-grid",
          badgeVi: "LỘ TRÌNH CHƯƠNG",
          badgeEn: "CEFR CHAPTERS",
          titleVi: "Chọn chương trình độ để vào học theo bài",
          titleEn: "Pick a CEFR Chapter to Start Studying",
          descVi:
            "Mỗi chương chia nhỏ thành nhiều nhóm chủ đề (chào hỏi, gia đình, công việc...). Bấm vào một thẻ cấp độ rồi chọn chủ đề để bắt đầu ôn Flashcard ngay.",
          descEn:
            "Each CEFR chapter breaks down into bite-sized topic lessons. Click any level card and pick a topic to launch your flashcards.",
          tipVi: "Ôn đủ từ 5 thẻ Flashcard khác nhau trở lên mỗi ngày sẽ thưởng thêm +1 lượt hỏi LingLing AI!",
          tipEn: "Reviewing 5+ distinct flashcards in a day earns +1 bonus LingLing AI turn!",
          placement: "top",
          padding: 10,
          radius: 18,
        },
      ];

    case "learn":
      return [
        {
          id: "learn-filters",
          target: '[data-tour="learn-filters"]',
          badgeVi: "BỘ LỌC THẺ",
          badgeEn: "DECK FILTERS",
          titleVi: "Chọn cấp độ CEFR & số thẻ mỗi phiên",
          titleEn: "Filter by CEFR Level & Session Size",
          descVi:
            "Lọc nhanh bộ từ vựng theo trình độ từ A1 đến C2 và chọn độ dài phiên ôn (10, 20 hoặc 50 thẻ) phù hợp với quỹ thời gian của bạn.",
          descEn:
            "Filter flashcards by CEFR level (A1–C2) and pick a comfortable session length (10, 20, or 50 cards).",
          placement: "bottom",
          padding: 8,
          radius: 14,
        },
        {
          id: "learn-card",
          target: '[data-tour="learn-card"]',
          badgeVi: "THẺ FLASHCARD 3D",
          badgeEn: "3D FLASHCARD",
          titleVi: "Chạm thẻ hoặc nhấn Space để lật xem nghĩa",
          titleEn: "Tap Card or Press Space to Flip",
          descVi:
            "Mặt trước hiển thị từ vựng, phiên âm IPA và nút nghe phát âm chuẩn; lật sang mặt sau để kiểm tra nghĩa tiếng Việt và câu ví dụ trong ngữ cảnh thật.",
          descEn:
            "The front shows the term, IPA, and audio button; flip to check the Vietnamese meaning and real-world example sentence.",
          placement: "right",
          padding: 10,
          radius: 20,
        },
        {
          id: "learn-controls",
          target: '[data-tour="learn-controls"]',
          badgeVi: "BẢNG ĐIỀU KHIỂN",
          badgeEn: "CONTROLS PANEL",
          titleVi: "Điều khiển bên phải — không cần cuộn trang",
          titleEn: "Right-Side Zero-Scroll Controls",
          descVi:
            "Nút Lật thẻ (Space), Nghe phát âm (R) và bật/tắt hiệu ứng âm thanh (SFX) được gom gọn sang cột phải để bạn thao tác cực nhanh trên một khung nhìn.",
          descEn:
            "Flip (Space), Pronounce (R), and SFX mute toggle sit neatly on the right column so you never have to scroll.",
          placement: "left",
          padding: 10,
          radius: 16,
        },
        {
          id: "learn-ratings",
          target: '[data-tour="learn-ratings"]',
          badgeVi: "THUẬT TOÁN SM-2",
          badgeEn: "SM-2 SPACED REPETITION",
          titleVi: "Đánh giá trí nhớ bằng 4 mức (Phím 1–4)",
          titleEn: "Rate Your Recall (Keys 1–4)",
          descVi:
            "Sau khi lật thẻ, chọn Lại / Khó / Tốt / Rất dễ để thuật toán SM-2 tự động hẹn ngày ôn lại đúng lúc bạn sắp quên.",
          descEn:
            "After flipping, rate Again / Hard / Good / Easy so SM-2 schedules the optimal next review interval.",
          tipVi: "Ôn đủ từ 5 thẻ khác nhau trở lên mỗi ngày sẽ tự động cộng thưởng +1 lượt hỏi LingLing AI!",
          tipEn: "Reviewing 5+ distinct cards in a day automatically earns +1 bonus LingLing AI turn!",
          placement: "left",
          padding: 10,
          radius: 16,
        },
      ];

    case "quiz":
      return [
        {
          id: "quiz-actions",
          target: ".quiz-heading-actions",
          badgeVi: "TIỆN ÍCH NHANH",
          badgeEn: "QUICK CONTROLS",
          titleVi: "Bật/tắt âm thanh phản hồi & xem Bảng xếp hạng",
          titleEn: "Audio Feedback & Leaderboard",
          descVi:
            "Chủ động bật hoặc tắt hiệu ứng âm thanh khi trả lời đúng/sai, đếm ngược 5 giây cuối và mở nhanh Bảng xếp hạng ngay tại góc phải.",
          descEn:
            "Toggle Web Audio sound effects for right/wrong answers and final countdown ticks, or jump to the Leaderboard.",
          placement: "bottom",
          padding: 8,
          radius: 12,
        },
        {
          id: "quiz-types",
          target: ".quiz-start > .quiz-start-modes:nth-child(2)",
          badgeVi: "DẠNG BÀI QUIZ",
          badgeEn: "QUIZ TYPES",
          titleVi: "Chọn kỹ năng muốn luyện tập",
          titleEn: "Choose Your Quiz Format",
          descVi:
            "Đa dạng hình thức luyện: Trắc nghiệm từ vựng, Nghe chọn đáp án, Chép chính tả, Nối cặp từ, Sắp xếp câu và Luyện phát âm.",
          descEn:
            "Practice multiple-choice vocabulary, listening, dictation, word matching, sentence scramble, and pronunciation.",
          placement: "bottom",
          padding: 10,
          radius: 16,
        },
        {
          id: "quiz-difficulty",
          target: ".quiz-start > .quiz-start-modes:nth-child(3)",
          badgeVi: "ĐỘ KHÓ",
          badgeEn: "DIFFICULTY",
          titleVi: "Tùy chỉnh độ khó phù hợp trình độ",
          titleEn: "Pick Your Challenge Level",
          descVi:
            "Chọn mức Dễ, Trung bình, Khó hoặc Tất cả để bộ câu hỏi khớp đúng trình độ hiện tại của bạn.",
          descEn:
            "Choose Easy, Medium, Hard, or All levels so the question set matches your current fluency.",
          placement: "top",
          padding: 10,
          radius: 16,
        },
        {
          id: "quiz-start-btn",
          target: ".quiz-start-button",
          badgeVi: "BẮT ĐẦU & THƯỞNG",
          badgeEn: "START & REWARDS",
          titleVi: "Hoàn thành Quiz để nhận XP & lượt AI",
          titleEn: "Complete Quizzes for XP & AI Turns",
          descVi:
            "Bấm để vào phiên làm bài có đồng hồ đếm ngược và giải thích chi tiết từng câu.",
          descEn:
            "Click to start a timed quiz session with instant explanations after every answer.",
          tipVi: "Đạt độ chính xác từ 60% trở lên (chấm phía server) sẽ tự động cộng +1 lượt hỏi LingLing AI!",
          tipEn: "Scoring >=60% (server-verified) automatically grants +1 bonus LingLing AI turn!",
          placement: "top",
          padding: 10,
          radius: 14,
        },
      ];

    case "games":
      return [
        {
          id: "games-subnav",
          target: '[data-tour="games-subnav"]',
          badgeVi: "CHUYỂN CHẾ ĐỘ",
          badgeEn: "MODE SWITCHER",
          titleVi: "Chuyển nhanh giữa Game Center & Đấu trường 1vs1",
          titleEn: "Switch Between Game Center & 1v1 Arena",
          descVi:
            "Luyện phản xạ cá nhân với 6 mini-game tại Game Center, hoặc chuyển sang Đấu trường 1vs1 để so tài tốc độ từ vựng thời gian thực với bạn bè.",
          descEn:
            "Practice solo across 6 mini-games here, or jump to the 1v1 Arena for live head-to-head vocabulary duels.",
          placement: "bottom",
          padding: 8,
          radius: 14,
        },
        {
          id: "games-sfx",
          target: '[data-tour="games-sfx"]',
          badgeVi: "ÂM THANH & BXH",
          badgeEn: "AUDIO & LEADERBOARD",
          titleVi: "Hiệu ứng âm thanh tương tác & Bảng xếp hạng",
          titleEn: "Interactive Sound Effects & Leaderboard",
          descVi:
            "Bật/tắt âm thanh phản hồi đúng, sai, combo ngay tại đây (trạng thái được ghi nhớ tự động) hoặc chuyển sang tab Bảng xếp hạng để xem top điểm cao.",
          descEn:
            "Toggle instant Web Audio feedback for correct/wrong answers and combos, or switch to the Leaderboard tab.",
          placement: "bottom",
          padding: 8,
          radius: 14,
        },
        {
          id: "games-catalog",
          target: '[data-tour="games-catalog"]',
          badgeVi: "6 MINI-GAME",
          badgeEn: "6 MINI-GAMES",
          titleVi: "Chọn trò chơi theo kỹ năng & cấp độ",
          titleEn: "Pick a Game by Skill & Level",
          descVi:
            "Thử sức với Nối từ, Xếp câu, Đua gõ phím, Điền từ siêu tốc, Lingo Wordle và Phản xạ âm thanh. Hoàn thành đạt từ 50% trở lên được thưởng XP và +1 lượt LingLing AI.",
          descEn:
            "Play Word Match, Sentence Scramble, Typing Race, Fill Blitz, Lingo Wordle, and Sound Reflex. Score >=50% to earn XP and +1 LingLing AI turn.",
          placement: "top",
          padding: 10,
          radius: 18,
        },
        {
          id: "games-rules-btn",
          target: '[data-tour="games-rules-btn"]',
          badgeVi: "LUẬT CHƠI",
          badgeEn: "GAME RULES",
          titleVi: "Xem hướng dẫn 3 bước cho từng trò chơi",
          titleEn: "3-Step Guide for Every Game",
          descVi:
            "Bấm nút Luật chơi trên bất kỳ thẻ trò chơi nào để xem cách tính điểm, mẹo phím tắt và hướng dẫn chi tiết trước khi bắt đầu.",
          descEn:
            "Click Rules on any game card to inspect scoring rules, keyboard tips, and quick walkthrough steps before playing.",
          placement: "top",
          padding: 8,
          radius: 12,
        },
      ];

    case "arena":
      return [
        {
          id: "arena-subnav",
          target: '[data-tour="games-subnav"]',
          badgeVi: "CHUYỂN CHẾ ĐỘ",
          badgeEn: "MODE SWITCHER",
          titleVi: "Chuyển nhanh giữa Đấu trường 1vs1 & Game Center",
          titleEn: "Switch Between 1v1 Arena & Game Center",
          descVi:
            "Tại đây bạn đấu tốc độ từ vựng trực tiếp theo từng vòng; muốn ôn luyện nhẹ nhàng hơn có thể chuyển sang tab Game Center (6 mini-game).",
          descEn:
            "Compete in rapid round-by-round duels here, or switch to Game Center for solo practice across 6 mini-games.",
          placement: "bottom",
          padding: 8,
          radius: 14,
        },
        {
          id: "arena-lobby",
          target: ".arena-lobby-main",
          badgeVi: "PHÒNG ĐẤU 1VS1",
          badgeEn: "1V1 MATCH LOBBY",
          titleVi: "Bắt đầu trận đấu phản xạ từ vựng",
          titleEn: "Start a Speed Vocabulary Duel",
          descVi:
            "Bấm nút Bắt đầu để vào trận đấu tốc độ theo vòng; cuối trận LingLing AI sẽ tổng hợp lại những từ bạn trả lời sai để ôn kịp thời.",
          descEn:
            "Press Start to jump into a timed multi-round duel; at the end, LingLing highlights any missed words for quick review.",
          placement: "right",
          padding: 10,
          radius: 18,
        },
        {
          id: "arena-rules",
          target: ".arena-rules",
          badgeVi: "LUẬT THI ĐẤU",
          badgeEn: "ARENA RULES",
          titleVi: "Nắm rõ cách tính điểm & thời gian mỗi vòng",
          titleEn: "Scoring & Round Timing Rules",
          descVi:
            "Xem nhanh số vòng đấu, giới hạn thời gian trả lời mỗi câu và cách cộng điểm chuỗi thắng trước khi vào trận.",
          descEn:
            "Check round count, per-question timers, and streak scoring rules before starting your match.",
          placement: "left",
          padding: 10,
          radius: 18,
        },
      ];

    case "tutor":
      return [
        {
          id: "tutor-quota",
          target: '[data-tour="tutor-quota"]',
          badgeVi: "VÍ LƯỢT AI",
          badgeEn: "AI QUOTA WALLET",
          titleVi: "Số lượt hỏi LingLing AI dùng chung Web & Bot",
          titleEn: "Shared Web + Bot AI Turn Wallet",
          descVi:
            "Khách dùng thử có 2 lượt/ngày. Khi đăng nhập Mezon, bạn nhận 5 lượt miễn phí mỗi ngày (làm mới lúc 00:00 GMT+7) và kiếm thêm tối đa +5 lượt thưởng từ Game, Quiz, Flashcard.",
          descEn:
            "Guests get 2 trial turns/day. Sign in with Mezon for 5 free turns daily (resets 00:00 GMT+7) plus up to +5 bonus turns from Game, Quiz, and Flashcards.",
          placement: "bottom",
          padding: 10,
          radius: 16,
        },
        {
          id: "tutor-modes",
          target: '[data-tour="tutor-modes"]',
          badgeVi: "CHẾ ĐỘ HỌC",
          badgeEn: "TUTOR MODES",
          titleVi: "Chọn Đóng vai hội thoại hoặc Sửa ngữ pháp",
          titleEn: "Switch Roleplay or Grammar Check",
          descVi:
            "Chế độ Roleplay giúp bạn thực hành đối đáp theo tình huống thực tế; chế độ Kiểm tra ngữ pháp phân tích từng lỗi sai và gợi ý câu tự nhiên hơn.",
          descEn:
            "Use Roleplay for realistic dialogue practice, or Grammar Check for detailed sentence corrections and phrasing tips.",
          placement: "bottom",
          padding: 8,
          radius: 14,
        },
        {
          id: "tutor-scenarios",
          target: '[data-tour="tutor-scenarios"]',
          badgeVi: "TÌNH HUỐNG NHẬP VAI",
          badgeEn: "ROLEPLAY SCENARIOS",
          titleVi: "Chọn bối cảnh giao tiếp thực tế",
          titleEn: "Choose a Real-World Context",
          descVi:
            "Thử gọi đồ uống tại quán cà phê London, làm thủ tục sân bay hoặc trả lời phỏng vấn xin việc. Bấm chọn thẻ kịch bản để LingLing hóa thân ngay.",
          descEn:
            "Practice ordering at a London café, checking in at the airport, or acing a job interview—click any card to start.",
          placement: "bottom",
          padding: 10,
          radius: 16,
        },
        {
          id: "tutor-input",
          target: '[data-tour="tutor-input"]',
          badgeVi: "KHUNG TRÒ CHUYỆN",
          badgeEn: "CHAT INPUT",
          titleVi: "Nhập câu tiếng Anh & nhận phản hồi tức thì",
          titleEn: "Send Your Message for Instant Feedback",
          descVi:
            "Gõ câu trả lời tiếng Anh của bạn (hoặc chọn nhanh các câu gợi ý phía trên) rồi nhấn Enter. Nếu kết nối AI gặp lỗi, lượt của bạn sẽ được hoàn lại tự động.",
          descEn:
            "Type your English reply (or tap a quick suggestion chip above) and press Enter. If the AI provider errors out, your turn is refunded automatically.",
          placement: "top",
          padding: 10,
          radius: 16,
        },
      ];

    case "leaderboard":
      return [
        {
          id: "lb-summary",
          target: ".leaderboard-summary",
          badgeVi: "THỨ HẠNG CỦA BẠN",
          badgeEn: "YOUR RANKING",
          titleVi: "Theo dõi vị trí cá nhân & XP tuần này",
          titleEn: "Track Your Weekly Rank & XP",
          descVi:
            "Hiển thị thứ hạng hiện tại, tổng XP bạn kiếm được trong tuần và nút nhảy nhanh sang làm Quiz để bứt phá lên hạng cao hơn.",
          descEn:
            "See your live position, weekly XP total, and a quick button to jump into a Quiz and climb higher.",
          placement: "bottom",
          padding: 10,
          radius: 16,
        },
        {
          id: "lb-metrics",
          target: ".league-metrics",
          badgeVi: "CHỈ SỐ GIẢI ĐẤU",
          badgeEn: "LEAGUE METRICS",
          titleVi: "Toàn cảnh cuộc đua trong Clan",
          titleEn: "Clan Competition Overview",
          descVi:
            "Cho biết tổng số thành viên tham gia, tổng XP cả Clan tích lũy trong tuần và chính xác số XP bạn cần thêm để vượt người xếp ngay trên.",
          descEn:
            "Check active clan participants, combined weekly clan XP, and the exact XP needed to pass the rank above you.",
          placement: "bottom",
          padding: 10,
          radius: 16,
        },
        {
          id: "lb-panel",
          target: ".leaderboard-panel",
          badgeVi: "BỤC VINH DANH",
          badgeEn: "TOP LEARNERS",
          titleVi: "Top 3 nổi bật & bảng xếp hạng chi tiết",
          titleEn: "Top 3 Podium & Full Rankings",
          descVi:
            "Vinh danh 3 học viên chăm chỉ nhất tuần kèm biến động tăng/giảm hạng theo thời gian thực của toàn bộ thành viên trong Clan.",
          descEn:
            "Highlights this week's Top 3 podium alongside live rank movements across your Clan.",
          placement: "top",
          padding: 10,
          radius: 18,
        },
      ];

    default:
      return [
        {
          id: "fallback-nav",
          target: '[data-tour="nav-tabs"]',
          badgeVi: "ĐIỀU HƯỚNG",
          badgeEn: "NAVIGATION",
          titleVi: "Khám phá các khu vực của LinguaMezon",
          titleEn: "Explore LinguaMezon Sections",
          descVi:
            "Sử dụng thanh điều hướng trên cùng để chuyển sang Tổng quan, Học từ (Thư viện & Flashcard), Luyện tập (Quiz), Đấu trường (Game Center & 1vs1), LingLing AI hoặc Giải đấu.",
          descEn:
            "Use the top navigation bar to move between Overview, Vocabulary (Library & Flashcards), Practice (Quiz), Arena (Games & 1v1), LingLing AI, and Leaderboard.",
          placement: "bottom",
          padding: 8,
          radius: 14,
        },
        {
          id: "fallback-help",
          target: '[data-tour="nav-help"]',
          badgeVi: "HƯỚNG DẪN TỪNG TRANG",
          badgeEn: "CONTEXTUAL GUIDE",
          titleVi: "Mở hướng dẫn khoanh vùng bất cứ lúc nào",
          titleEn: "Reopen Spotlight Guide Anytime",
          descVi:
            "Toàn bộ 6 khu vực chính đều có hướng dẫn khoanh vùng riêng. Bấm nút (?) này bất cứ khi nào bạn cần xem lại hướng dẫn của trang hiện tại.",
          descEn:
            "All 6 main sections have their own contextual spotlight walkthrough. Click (?) anytime to replay the guide for your current page.",
          placement: "bottom",
          padding: 8,
          radius: 12,
        },
      ];
  }
}
