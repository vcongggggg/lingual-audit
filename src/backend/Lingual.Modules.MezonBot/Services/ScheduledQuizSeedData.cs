using System;
using System.Collections.Generic;
using Lingual.Modules.MezonBot.Domain;

namespace Lingual.Modules.MezonBot.Services;

/// <summary>
/// Bộ ngân hàng câu hỏi đố vui CEFR A1-B2 chuẩn hóa (hơn 35 câu hỏi)
/// dùng cho tính năng Scheduled Clan Quiz và đảm bảo kho câu hỏi luôn đạt >= 30 câu.
/// </summary>
public static class ScheduledQuizSeedData
{
    public static List<QuizQuestion> GetSeedQuestions()
    {
        return new List<QuizQuestion>
        {
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000001"),
                QuestionType = "multiple_choice",
                Prompt = "She ________ to the gym three times a week.",
                Difficulty = 1,
                Level = "A1",
                Options = @"[
                    {""key"":""A"",""text"":""go"",""is_correct"":false},
                    {""key"":""B"",""text"":""goes"",""is_correct"":true},
                    {""key"":""C"",""text"":""going"",""is_correct"":false},
                    {""key"":""D"",""text"":""gone"",""is_correct"":false}
                ]",
                Explanation = "Chủ ngữ ngôi thứ ba số ít 'She' ở thì hiện tại đơn đi với động từ thêm 'es' (goes).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000002"),
                QuestionType = "multiple_choice",
                Prompt = "We have been studying English ________ three years.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""since"",""is_correct"":false},
                    {""key"":""B"",""text"":""for"",""is_correct"":true},
                    {""key"":""C"",""text"":""in"",""is_correct"":false},
                    {""key"":""D"",""text"":""during"",""is_correct"":false}
                ]",
                Explanation = "'For' dùng trước một khoảng thời gian (three years) trong thì hiện tại hoàn thành tiếp diễn.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000003"),
                QuestionType = "multiple_choice",
                Prompt = "This project is much ________ than our previous one.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""more difficult"",""is_correct"":true},
                    {""key"":""B"",""text"":""difficult"",""is_correct"":false},
                    {""key"":""C"",""text"":""difficulter"",""is_correct"":false},
                    {""key"":""D"",""text"":""most difficult"",""is_correct"":false}
                ]",
                Explanation = "So sánh hơn của tính từ dài 'difficult' là 'more difficult than'.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000004"),
                QuestionType = "multiple_choice",
                Prompt = "If they arrive early, we ________ the presentation immediately.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""start"",""is_correct"":false},
                    {""key"":""B"",""text"":""will start"",""is_correct"":true},
                    {""key"":""C"",""text"":""started"",""is_correct"":false},
                    {""key"":""D"",""text"":""would start"",""is_correct"":false}
                ]",
                Explanation = "Câu điều kiện loại 1: Mệnh đề If dùng hiện tại đơn, mệnh đề chính dùng will + V.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000005"),
                QuestionType = "multiple_choice",
                Prompt = "He suggested ________ a short break before continuing the meeting.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""take"",""is_correct"":false},
                    {""key"":""B"",""text"":""taking"",""is_correct"":true},
                    {""key"":""C"",""text"":""to take"",""is_correct"":false},
                    {""key"":""D"",""text"":""took"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: suggest + V-ing (đề xuất làm việc gì đó).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000006"),
                QuestionType = "multiple_choice",
                Prompt = "I look forward to ________ from you soon.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""hear"",""is_correct"":false},
                    {""key"":""B"",""text"":""hearing"",""is_correct"":true},
                    {""key"":""C"",""text"":""heard"",""is_correct"":false},
                    {""key"":""D"",""text"":""to hear"",""is_correct"":false}
                ]",
                Explanation = "Cụm thành ngữ: 'look forward to + V-ing' (mong đợi điều gì).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000007"),
                QuestionType = "multiple_choice",
                Prompt = "Neither the manager nor the employees ________ aware of the policy update.",
                Difficulty = 2,
                Level = "B2",
                Options = @"[
                    {""key"":""A"",""text"":""was"",""is_correct"":false},
                    {""key"":""B"",""text"":""were"",""is_correct"":true},
                    {""key"":""C"",""text"":""is"",""is_correct"":false},
                    {""key"":""D"",""text"":""are"",""is_correct"":false}
                ]",
                Explanation = "Với cấu trúc 'neither... nor...', động từ chia theo chủ ngữ gần nhất ('the employees' là số nhiều -> were).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000008"),
                QuestionType = "multiple_choice",
                Prompt = "Could you please remind me ________ the report by 5 PM?",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""submitting"",""is_correct"":false},
                    {""key"":""B"",""text"":""to submit"",""is_correct"":true},
                    {""key"":""C"",""text"":""submit"",""is_correct"":false},
                    {""key"":""D"",""text"":""submitted"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: remind someone to do something (nhắc nhở ai làm gì).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000009"),
                QuestionType = "multiple_choice",
                Prompt = "By the time we reached the cinema, the movie ________ already.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""started"",""is_correct"":false},
                    {""key"":""B"",""text"":""had started"",""is_correct"":true},
                    {""key"":""C"",""text"":""has started"",""is_correct"":false},
                    {""key"":""D"",""text"":""starts"",""is_correct"":false}
                ]",
                Explanation = "Hành động xảy ra trước một thời điểm/hành động khác trong quá khứ chia thì Quá khứ hoàn thành (had started).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000010"),
                QuestionType = "multiple_choice",
                Prompt = "The software is designed to be user-friendly, ________ making it accessible for beginners.",
                Difficulty = 2,
                Level = "B2",
                Options = @"[
                    {""key"":""A"",""text"":""thereby"",""is_correct"":true},
                    {""key"":""B"",""text"":""whereas"",""is_correct"":false},
                    {""key"":""C"",""text"":""unless"",""is_correct"":false},
                    {""key"":""D"",""text"":""although"",""is_correct"":false}
                ]",
                Explanation = "Phó từ 'thereby' đi với V-ing mang nghĩa 'bằng cách đó, do đó'.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000011"),
                QuestionType = "multiple_choice",
                Prompt = "They managed to complete the task ________ the heavy rainfall.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""although"",""is_correct"":false},
                    {""key"":""B"",""text"":""despite"",""is_correct"":true},
                    {""key"":""C"",""text"":""in spite"",""is_correct"":false},
                    {""key"":""D"",""text"":""even though"",""is_correct"":false}
                ]",
                Explanation = "'Despite' đi với danh từ hoặc cụm danh từ (the heavy rainfall), mang nghĩa 'mặc dù'.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000012"),
                QuestionType = "multiple_choice",
                Prompt = "We had better ________ before the traffic gets worse.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""leaving"",""is_correct"":false},
                    {""key"":""B"",""text"":""leave"",""is_correct"":true},
                    {""key"":""C"",""text"":""to leave"",""is_correct"":false},
                    {""key"":""D"",""text"":""left"",""is_correct"":false}
                ]",
                Explanation = "Sau 'had better' là động từ nguyên mẫu không to (bare infinitive).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000013"),
                QuestionType = "multiple_choice",
                Prompt = "The company's new branch will be located ________ Nguyen Van Linh Street.",
                Difficulty = 1,
                Level = "A1",
                Options = @"[
                    {""key"":""A"",""text"":""at"",""is_correct"":false},
                    {""key"":""B"",""text"":""on"",""is_correct"":true},
                    {""key"":""C"",""text"":""in"",""is_correct"":false},
                    {""key"":""D"",""text"":""by"",""is_correct"":false}
                ]",
                Explanation = "Trước tên đường (street) ta dùng giới từ 'on'.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000014"),
                QuestionType = "multiple_choice",
                Prompt = "She speaks three languages fluently, ________ English, French, and Japanese.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""namely"",""is_correct"":true},
                    {""key"":""B"",""text"":""such"",""is_correct"":false},
                    {""key"":""C"",""text"":""instance"",""is_correct"":false},
                    {""key"":""D"",""text"":""example"",""is_correct"":false}
                ]",
                Explanation = "'Namely' (cụ thể là) dùng để liệt kê cụ thể các danh từ đã đề cập phía trước.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000015"),
                QuestionType = "multiple_choice",
                Prompt = "I am not used to ________ up so early on weekend mornings.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""wake"",""is_correct"":false},
                    {""key"":""B"",""text"":""waking"",""is_correct"":true},
                    {""key"":""C"",""text"":""woke"",""is_correct"":false},
                    {""key"":""D"",""text"":""woken"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: 'be used to + V-ing' (quen với việc gì đó).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000016"),
                QuestionType = "multiple_choice",
                Prompt = "The doctor advised him ________ smoking to improve his lung health.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""stop"",""is_correct"":false},
                    {""key"":""B"",""text"":""to stop"",""is_correct"":true},
                    {""key"":""C"",""text"":""stopping"",""is_correct"":false},
                    {""key"":""D"",""text"":""stopped"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: advise someone to do something (khuyên ai làm gì).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000017"),
                QuestionType = "multiple_choice",
                Prompt = "It was ________ difficult test that few students scored above 90.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""so"",""is_correct"":false},
                    {""key"":""B"",""text"":""such a"",""is_correct"":true},
                    {""key"":""C"",""text"":""too"",""is_correct"":false},
                    {""key"":""D"",""text"":""very"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: such + (a/an) + adj + noun + that (quá... đến nỗi mà).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000018"),
                QuestionType = "multiple_choice",
                Prompt = "You haven't seen my glasses anywhere, ________ you?",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""do"",""is_correct"":false},
                    {""key"":""B"",""text"":""have"",""is_correct"":true},
                    {""key"":""C"",""text"":""haven't"",""is_correct"":false},
                    {""key"":""D"",""text"":""did"",""is_correct"":false}
                ]",
                Explanation = "Câu hỏi đuôi (tag question): Mệnh đề trước phủ định 'haven't seen' -> phần đuôi khẳng định 'have you'.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000019"),
                QuestionType = "multiple_choice",
                Prompt = "Hardly had the meeting started ________ the fire alarm rang.",
                Difficulty = 3,
                Level = "B2",
                Options = @"[
                    {""key"":""A"",""text"":""than"",""is_correct"":false},
                    {""key"":""B"",""text"":""when"",""is_correct"":true},
                    {""key"":""C"",""text"":""then"",""is_correct"":false},
                    {""key"":""D"",""text"":""as"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc đảo ngữ: Hardly had + S + VpII + when + S + V-ed (vừa mới... thì).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000020"),
                QuestionType = "multiple_choice",
                Prompt = "Can you help me ________ this math problem?",
                Difficulty = 1,
                Level = "A1",
                Options = @"[
                    {""key"":""A"",""text"":""solve"",""is_correct"":true},
                    {""key"":""B"",""text"":""solving"",""is_correct"":false},
                    {""key"":""C"",""text"":""solved"",""is_correct"":false},
                    {""key"":""D"",""text"":""solves"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: help someone (to) do something.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000021"),
                QuestionType = "multiple_choice",
                Prompt = "The novel, ________ was published last year, became an international bestseller.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""that"",""is_correct"":false},
                    {""key"":""B"",""text"":""which"",""is_correct"":true},
                    {""key"":""C"",""text"":""who"",""is_correct"":false},
                    {""key"":""D"",""text"":""what"",""is_correct"":false}
                ]",
                Explanation = "Trong mệnh đề quan hệ không xác định (có dấu phẩy), không dùng 'that', dùng 'which' thay thế cho sự vật.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000022"),
                QuestionType = "multiple_choice",
                Prompt = "He worked late last night, so he is completely ________ today.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""exhausting"",""is_correct"":false},
                    {""key"":""B"",""text"":""exhausted"",""is_correct"":true},
                    {""key"":""C"",""text"":""exhaust"",""is_correct"":false},
                    {""key"":""D"",""text"":""exhausts"",""is_correct"":false}
                ]",
                Explanation = "Tính từ đuôi '-ed' (exhausted) miêu tả cảm giác kiệt sức của con người.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000023"),
                QuestionType = "multiple_choice",
                Prompt = "Unless you ________ hard, you will not pass the entrance examination.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""study"",""is_correct"":true},
                    {""key"":""B"",""text"":""don't study"",""is_correct"":false},
                    {""key"":""C"",""text"":""studied"",""is_correct"":false},
                    {""key"":""D"",""text"":""will study"",""is_correct"":false}
                ]",
                Explanation = "'Unless' = If not, mệnh đề sau Unless chia khẳng định ở thì hiện tại đơn.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000024"),
                QuestionType = "multiple_choice",
                Prompt = "She apologized ________ being late for the interview.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""to"",""is_correct"":false},
                    {""key"":""B"",""text"":""for"",""is_correct"":true},
                    {""key"":""C"",""text"":""about"",""is_correct"":false},
                    {""key"":""D"",""text"":""with"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: apologize for (doing) something (xin lỗi vì điều gì).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000025"),
                QuestionType = "multiple_choice",
                Prompt = "We ________ have taken an umbrella; it didn't rain at all.",
                Difficulty = 2,
                Level = "B2",
                Options = @"[
                    {""key"":""A"",""text"":""mustn't"",""is_correct"":false},
                    {""key"":""B"",""text"":""needn't"",""is_correct"":true},
                    {""key"":""C"",""text"":""should"",""is_correct"":false},
                    {""key"":""D"",""text"":""can't"",""is_correct"":false}
                ]",
                Explanation = "'Needn't have + VpII' dùng để chỉ một hành động đã làm trong quá khứ nhưng thực tế không cần thiết.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000026"),
                QuestionType = "multiple_choice",
                Prompt = "The teacher made the students ________ their assignments before leaving.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""finish"",""is_correct"":true},
                    {""key"":""B"",""text"":""to finish"",""is_correct"":false},
                    {""key"":""C"",""text"":""finishing"",""is_correct"":false},
                    {""key"":""D"",""text"":""finished"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc sai khiến chủ động: make someone do something (bắt ai làm gì).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000027"),
                QuestionType = "multiple_choice",
                Prompt = "In spite of ________ tired, she managed to finish the report on time.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""be"",""is_correct"":false},
                    {""key"":""B"",""text"":""being"",""is_correct"":true},
                    {""key"":""C"",""text"":""been"",""is_correct"":false},
                    {""key"":""D"",""text"":""was"",""is_correct"":false}
                ]",
                Explanation = "Sau giới từ 'of' trong cụm 'in spite of', động từ dạng V-ing (being).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000028"),
                QuestionType = "multiple_choice",
                Prompt = "You should avoid ________ sugary drinks if you want to lose weight.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""consume"",""is_correct"":false},
                    {""key"":""B"",""text"":""consuming"",""is_correct"":true},
                    {""key"":""C"",""text"":""to consume"",""is_correct"":false},
                    {""key"":""D"",""text"":""consumed"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: avoid + V-ing (tránh làm việc gì đó).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000029"),
                QuestionType = "multiple_choice",
                Prompt = "By next December, we ________ at this university for four years.",
                Difficulty = 2,
                Level = "B2",
                Options = @"[
                    {""key"":""A"",""text"":""will study"",""is_correct"":false},
                    {""key"":""B"",""text"":""will have studied"",""is_correct"":true},
                    {""key"":""C"",""text"":""have studied"",""is_correct"":false},
                    {""key"":""D"",""text"":""are studying"",""is_correct"":false}
                ]",
                Explanation = "Dấu hiệu 'By next + thời gian' dùng thì Tương lai hoàn thành (will have studied).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000030"),
                QuestionType = "multiple_choice",
                Prompt = "Would you mind ________ the window? It's getting a bit stuffy.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""open"",""is_correct"":false},
                    {""key"":""B"",""text"":""opening"",""is_correct"":true},
                    {""key"":""C"",""text"":""to open"",""is_correct"":false},
                    {""key"":""D"",""text"":""opened"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc lịch sự: Would you mind + V-ing (bạn có phiền mở cửa sổ không?).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000031"),
                QuestionType = "multiple_choice",
                Prompt = "He had no sooner entered the room ________ the lights went out.",
                Difficulty = 3,
                Level = "B2",
                Options = @"[
                    {""key"":""A"",""text"":""when"",""is_correct"":false},
                    {""key"":""B"",""text"":""than"",""is_correct"":true},
                    {""key"":""C"",""text"":""then"",""is_correct"":false},
                    {""key"":""D"",""text"":""that"",""is_correct"":false}
                ]",
                Explanation = "Cặp liên từ: no sooner ... than ... (vừa mới ... thì ...).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000032"),
                QuestionType = "multiple_choice",
                Prompt = "The police asked the witness to give a detailed ________ of the suspect.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""describe"",""is_correct"":false},
                    {""key"":""B"",""text"":""description"",""is_correct"":true},
                    {""key"":""C"",""text"":""descriptive"",""is_correct"":false},
                    {""key"":""D"",""text"":""describing"",""is_correct"":false}
                ]",
                Explanation = "Sau tính từ 'detailed' cần một danh từ (description: bản mô tả).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000033"),
                QuestionType = "multiple_choice",
                Prompt = "I can't afford ________ a new car right now.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""buy"",""is_correct"":false},
                    {""key"":""B"",""text"":""to buy"",""is_correct"":true},
                    {""key"":""C"",""text"":""buying"",""is_correct"":false},
                    {""key"":""D"",""text"":""bought"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: afford to do something (có đủ khả năng tài chính để làm gì).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000034"),
                QuestionType = "multiple_choice",
                Prompt = "She talks ________ she knew everything about artificial intelligence.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""as if"",""is_correct"":true},
                    {""key"":""B"",""text"":""although"",""is_correct"":false},
                    {""key"":""C"",""text"":""because"",""is_correct"":false},
                    {""key"":""D"",""text"":""even"",""is_correct"":false}
                ]",
                Explanation = "'As if' (như thể là) dùng trong mệnh đề giả định (she knew).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("22222222-2222-2222-2222-000000000035"),
                QuestionType = "multiple_choice",
                Prompt = "It is essential that every participant ________ on time tomorrow.",
                Difficulty = 3,
                Level = "B2",
                Options = @"[
                    {""key"":""A"",""text"":""is"",""is_correct"":false},
                    {""key"":""B"",""text"":""be"",""is_correct"":true},
                    {""key"":""C"",""text"":""was"",""is_correct"":false},
                    {""key"":""D"",""text"":""will be"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc giả định thức (subjunctive mood): It is essential that S + (should) be.",
                Points = 10,
                Status = "published"
            }
        };
    }
}
