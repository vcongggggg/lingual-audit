# Mezon Notifications Inventory

## 1. Inventory Catalog

| Alert Key | Type | Destination | Trigger Condition | Template Summary |
| :--- | :--- | :--- | :--- | :--- |
| `NOTIF_DAILY_WORD` | Channel | Clan Main Channel | 08:00 AM (Configured Schedule) | "🌟 Từ vựng hôm nay: **[Word]** ([IPA]) - [Meaning]..." |
| `NOTIF_CLAN_QUIZ` | Channel | Clan Quiz Channel | 20:00 PM (Configured Schedule) | "⚡ ĐỐ VUI CLAN BẮT ĐẦU! Ai là người trả lời nhanh nhất?..." |
| `NOTIF_STREAK_WARNING` | DM | Direct Message | 21:00 PM if `today_xp < 20` | "🔥 Chuỗi học tập [N] ngày của bạn sắp mất! Vào học 5 phút ngay." |
| `NOTIF_SRS_DUE` | DM | Direct Message | `due_cards >= 10` | "📚 Bạn có [N] thẻ nhớ cần ôn tập hôm nay để củng cố trí nhớ." |
| `NOTIF_DUEL_INVITE` | DM / In-App | Direct Message | Challenger launches Word Duel | "⚔️ [User] đã gửi lời thách đấu Word Duel 5 câu với bạn!" |
| `NOTIF_WEEKLY_PODIUM` | Channel | Clan Main Channel | Sunday 23:59 (Finalized Leaderboard) | "🏆 VINH DANH TOP 3 BẢNG XẾP HẠNG TUẦN NÀY: 🥇 [User1], 🥈 [User2]..." |

---

## 2. Dynamic Schedule Management
All automated broadcast schedules are maintained dynamically in `bot_configuration.schedules` (JSONB) and evaluated by background cron workers in `Lingual.Modules.MezonBot`.
