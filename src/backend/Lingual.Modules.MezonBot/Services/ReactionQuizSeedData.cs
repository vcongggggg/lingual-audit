using System;
using System.Collections.Generic;
using Lingual.Modules.MezonBot.Domain;

namespace Lingual.Modules.MezonBot.Services;

public static class ReactionQuizSeedData
{
    public static List<QuizQuestion> GetSeedQuestions()
    {
        return new List<QuizQuestion>
        {
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-000000000001"),
                QuestionType = "multiple_choice",
                Prompt = "She ________ to the library every Sunday afternoon.",
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
                Id = Guid.Parse("11111111-1111-1111-1111-000000000002"),
                QuestionType = "multiple_choice",
                Prompt = "We have lived in this city ________ 2018.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""since"",""is_correct"":true},
                    {""key"":""B"",""text"":""for"",""is_correct"":false},
                    {""key"":""C"",""text"":""at"",""is_correct"":false},
                    {""key"":""D"",""text"":""in"",""is_correct"":false}
                ]",
                Explanation = "'Since' được dùng trước mốc thời gian xác định (năm 2018) trong thì hiện tại hoàn thành.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-000000000003"),
                QuestionType = "multiple_choice",
                Prompt = "The weather today is much ________ than yesterday.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""good"",""is_correct"":false},
                    {""key"":""B"",""text"":""better"",""is_correct"":true},
                    {""key"":""C"",""text"":""best"",""is_correct"":false},
                    {""key"":""D"",""text"":""well"",""is_correct"":false}
                ]",
                Explanation = "Dạng so sánh hơn của tính từ bất quy tắc 'good' là 'better'.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-000000000004"),
                QuestionType = "multiple_choice",
                Prompt = "Could you please ________ me the document on the table?",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""pass"",""is_correct"":true},
                    {""key"":""B"",""text"":""passed"",""is_correct"":false},
                    {""key"":""C"",""text"":""passing"",""is_correct"":false},
                    {""key"":""D"",""text"":""passes"",""is_correct"":false}
                ]",
                Explanation = "Sau trợ động từ khiếm khuyết 'Could you please', động từ giữ nguyên thể không to.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-000000000005"),
                QuestionType = "multiple_choice",
                Prompt = "If it ________ tomorrow, we will stay at home.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""rain"",""is_correct"":false},
                    {""key"":""B"",""text"":""rains"",""is_correct"":true},
                    {""key"":""C"",""text"":""rained"",""is_correct"":false},
                    {""key"":""D"",""text"":""will rain"",""is_correct"":false}
                ]",
                Explanation = "Trong câu điều kiện loại 1, mệnh đề 'If' dùng thì hiện tại đơn (it rains).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-000000000006"),
                QuestionType = "multiple_choice",
                Prompt = "He is very interested ________ learning foreign languages.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""on"",""is_correct"":false},
                    {""key"":""B"",""text"":""in"",""is_correct"":true},
                    {""key"":""C"",""text"":""at"",""is_correct"":false},
                    {""key"":""D"",""text"":""with"",""is_correct"":false}
                ]",
                Explanation = "Cụm tính từ cố định: 'be interested in something' (quan tâm, thích thú điều gì).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-000000000007"),
                QuestionType = "multiple_choice",
                Prompt = "They decided ________ a new laptop for their online classes.",
                Difficulty = 1,
                Level = "A2",
                Options = @"[
                    {""key"":""A"",""text"":""buying"",""is_correct"":false},
                    {""key"":""B"",""text"":""to buy"",""is_correct"":true},
                    {""key"":""C"",""text"":""buy"",""is_correct"":false},
                    {""key"":""D"",""text"":""bought"",""is_correct"":false}
                ]",
                Explanation = "Cấu trúc: 'decide to do something' (quyết định làm việc gì đó).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-000000000008"),
                QuestionType = "multiple_choice",
                Prompt = "I don't have ________ cash left in my wallet.",
                Difficulty = 1,
                Level = "A1",
                Options = @"[
                    {""key"":""A"",""text"":""many"",""is_correct"":false},
                    {""key"":""B"",""text"":""some"",""is_correct"":false},
                    {""key"":""C"",""text"":""any"",""is_correct"":true},
                    {""key"":""D"",""text"":""an"",""is_correct"":false}
                ]",
                Explanation = "Trong câu phủ định với danh từ không đếm được 'cash', ta sử dụng 'any'.",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-000000000009"),
                QuestionType = "multiple_choice",
                Prompt = "Listen! The children ________ in the garden.",
                Difficulty = 1,
                Level = "A1",
                Options = @"[
                    {""key"":""A"",""text"":""play"",""is_correct"":false},
                    {""key"":""B"",""text"":""are playing"",""is_correct"":true},
                    {""key"":""C"",""text"":""played"",""is_correct"":false},
                    {""key"":""D"",""text"":""playing"",""is_correct"":false}
                ]",
                Explanation = "Dấu hiệu 'Listen!' cho thấy hành động đang diễn ra tại thời điểm nói -> thì hiện tại tiếp diễn (are playing).",
                Points = 10,
                Status = "published"
            },
            new()
            {
                Id = Guid.Parse("11111111-1111-1111-1111-000000000010"),
                QuestionType = "multiple_choice",
                Prompt = "That was the most ________ presentation I have ever attended.",
                Difficulty = 2,
                Level = "B1",
                Options = @"[
                    {""key"":""A"",""text"":""interest"",""is_correct"":false},
                    {""key"":""B"",""text"":""interesting"",""is_correct"":true},
                    {""key"":""C"",""text"":""interested"",""is_correct"":false},
                    {""key"":""D"",""text"":""interests"",""is_correct"":false}
                ]",
                Explanation = "Tính từ đuôi '-ing' (interesting) dùng để miêu tả tính chất hấp dẫn của sự vật (presentation).",
                Points = 10,
                Status = "published"
            }
        };
    }
}

