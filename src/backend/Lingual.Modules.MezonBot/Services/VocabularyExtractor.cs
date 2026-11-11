using System;
using System.Text.RegularExpressions;

namespace Lingual.Modules.MezonBot.Services;

public static class VocabularyExtractor
{
    private static readonly Regex BoldTermRegex = new(@"\*\*([^*]+)\*\*\s*[:\-–—=]\s*(.+)", RegexOptions.Compiled);
    private static readonly Regex SeparatorRegex = new(@"^([^:\-–—=]{1,50})\s*[:\-–—=]\s*(.+)$", RegexOptions.Compiled);

    public static (string Term, string Meaning) ExtractTermAndMeaning(string? rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return (string.Empty, string.Empty);
        }

        var text = rawText.Trim();

        // 1. Kiểm tra định dạng có in đậm: **word**: meaning hoặc **word** - meaning
        var boldMatch = BoldTermRegex.Match(text);
        if (boldMatch.Success)
        {
            var term = boldMatch.Groups[1].Value.Trim();
            var meaning = boldMatch.Groups[2].Value.Trim();
            if (!string.IsNullOrWhiteSpace(term))
            {
                return (term, string.IsNullOrWhiteSpace(meaning) ? term : meaning);
            }
        }

        // 2. Kiểm tra dòng đầu tiên nếu tin nhắn có nhiều dòng
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var firstLine = lines.Length > 0 ? lines[0].Trim() : text;

        var sepMatch = SeparatorRegex.Match(firstLine);
        if (sepMatch.Success)
        {
            var term = sepMatch.Groups[1].Value.Trim();
            var meaning = sepMatch.Groups[2].Value.Trim();
            if (!string.IsNullOrWhiteSpace(term))
            {
                return (term, string.IsNullOrWhiteSpace(meaning) ? term : meaning);
            }
        }

        // 3. Nếu là một từ đơn hoặc cụm ngắn dưới 60 ký tự
        if (firstLine.Length <= 60)
        {
            return (firstLine, firstLine);
        }

        // 4. Nếu là đoạn văn dài, lấy cụm từ khóa đầu tiên (tối đa 4 từ) làm Term
        var words = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return (string.Empty, string.Empty);
        }
        var keyTerm = string.Join(" ", words.Take(Math.Min(4, words.Length)));
        return (keyTerm, firstLine);
    }
}

