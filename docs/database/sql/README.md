# Lingual database

Relational schema for PostgreSQL 13+. The schema is split by product module; run files in this order against the same database:

1. `user.sql` — Mezon identity, onboarding, placement, and global roles.
2. `curriculum.sql` — courses, units, lessons (with vocabulary_ids JSONB), and vocabulary items (with examples JSONB).
3. `learning.sql` — lesson progress (with session history JSONB), SRS cards (with review history JSONB), XP ledger, and user streaks (with freeze history & daily activity JSONB).
4. `quiz.sql` — reusable question bank and quiz sessions/answers.
5. `community.sql` — single-Clan Mezon Bot configuration (with schedules JSONB) and Clan quiz sessions (with responses JSONB). There is no `clans` entity table.
6. `competition.sql` — Word Duel matches (with questions, answers, and scores JSONB) and weekly leaderboards.
7. `assistant.sql` — LingLing AI roleplay scenarios and conversation history (with messages JSONB).
8. `analytics.sql` — Phase 2 post-MVP analytics (metrics are queryable directly from `xp_ledger` and `lesson_progress`).

`user.sql` is the prerequisite for all modules. `curriculum.sql` precedes `learning.sql` and `quiz.sql`; `community.sql` precedes `competition.sql`. `analytics.sql` is last. Scripts use `IF NOT EXISTS` for tables/indexes so they can be re-applied during development, but are not a versioned migration system. Back up production data and use explicit migrations for schema changes.

Design notes:

- The Bot is configured for exactly one Mezon Clan. Its external Clan ID is stored in the singleton `bot_configuration` row; member snapshots do not carry a Clan foreign key. OAuth access/refresh tokens and webhook secrets belong in a secrets manager, not these tables.
- Timestamps are `TIMESTAMPTZ`; streak/activity dates are persisted in the learner's configured timezone (default `Asia/Ho_Chi_Minh`).
- XP is an append-only ledger; leaderboard totals can be derived from it. Weekly snapshots preserve published results.
- Weekly leaderboard ranks learners within the configured Clan in `weekly_leaderboards`. XP awards (5 new word, 3 successful review, 10 Clan quiz plus 5 speed bonus, 25 lesson, 30/10 duel) and the daily 20 XP streak threshold are application rules recorded in the XP ledger.
- Global `moderator` and `admin` permissions use `users(role)` (Simple RBAC).
- Mutable external profile fields are snapshots. Synchronize them from Mezon events/API.
- JSONB is limited to variable question payloads and AI context; core entities remain relational.
