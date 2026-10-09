# Lingual database

Relational schema for PostgreSQL 13+. The schema is split by product module; run files in this order against the same database:

1. `user.sql` — Mezon identity, onboarding, placement, and global roles.
2. `curriculum.sql` — courses, units, lessons (with vocabulary_ids JSONB), and vocabulary items (with examples JSONB).
3. `learning.sql` — lesson progress (with session history JSONB), SRS cards (with review history JSONB), XP ledger, and user streaks (with freeze history & daily activity JSONB).
4. `quiz.sql` — reusable question bank and quiz sessions/answers.
5. `community.sql` — single-Clan Mezon Bot configuration (with schedules JSONB), Clan quiz sessions (with responses JSONB), and Clan moderator grants (`clan_moderator_grants`). There is no `clans` entity table.
6. `competition.sql` — Word Duel matches (with questions, answers, and scores JSONB) and weekly leaderboards.
7. `assistant.sql` — LingLing AI roleplay scenarios and conversation history (with messages JSONB).
8. `analytics.sql` — Phase 2 post-MVP analytics (metrics are queryable directly from `xp_ledger` and `lesson_progress`).
9. `audit.sql` — sensitive operations audit trail (append-only log for role changes, moderation, clan bot config).

`user.sql` is the prerequisite for all modules. `curriculum.sql` precedes `learning.sql` and `quiz.sql`; `community.sql` precedes `competition.sql`. `audit.sql` references `users`. `analytics.sql` is post-MVP. Alternatively, run `00_init_all.sql` to initialize all 22 tables and 11 triggers in a single transaction. Scripts use `IF NOT EXISTS` for tables/indexes so they can be re-applied during development, but are not a versioned migration system. Back up production data and use explicit migrations for schema changes.

Design notes:

- Security & Audit: `audit_logs` is an append-only log (ADM-09, ADM-12). In production, enforce non-repudiation by restricting mutations for the application service role:
  ```sql
  REVOKE UPDATE, DELETE ON audit_logs FROM lingual_user;
  ```
- Extensions & Triggers: `pgcrypto` is required for `gen_random_uuid()`. 11 tables with `updated_at` timestamps utilize the `set_updated_at()` PL/pgSQL trigger function to update automatically on row modification.
- JSONB Foreign Key Integrity (Fail-Closed): `check_jsonb_uuid_refs()` trigger validates that all UUIDs in `lessons.vocabulary_ids` exist in `vocabulary_items(id)` and `duel_matches.question_ids` exist in `quiz_questions(id)`. Items must never be HARD-DELETED; soft-archive via `status = 'archived'`.
- Single Placement Test In-Progress: Partial unique index `uq_placement_test_in_progress` guarantees a learner cannot open concurrent placement evaluations.
- Temporal Constraints: Enforced via `CHECK (closes_at > opened_at)` on `clan_quiz_sessions`, and `CHECK (completed_at IS NULL OR completed_at >= started_at)` on `placement_tests` and `duel_matches`.

Implementation & Concurrency Guidelines (Backend .NET 8 / SQL):
1. **Atomic Array Appends (Anti-Lost Update):** Never read-modify-write JSONB arrays in memory. Execute atomic SQL updates using PostgreSQL's `||` operator:
   ```sql
   -- Clan quiz response (atomic append, serialized row lock)
   UPDATE clan_quiz_sessions
   SET responses = responses || @response::jsonb
   WHERE id = @sessionId AND status = 'open' AND closes_at > now();
   
   -- AI chat messages
   UPDATE ai_conversations
   SET messages = messages || @msg::jsonb, total_tokens = total_tokens + @tokens
   WHERE id = @conversationId;
   
   -- Word duel participant answers
   UPDATE duel_matches
   SET challenger_answers = challenger_answers || @answer::jsonb
   WHERE id = @matchId AND status = 'in_progress';
   ```
2. **First-Use Upsert Pattern:** Always use `INSERT ... ON CONFLICT (user_id) DO NOTHING` for `user_streaks`, `lesson_progress`, and `srs_cards` to prevent race conditions during first activity.
3. **Atomic Streak Freeze Consumption:**
   ```sql
   UPDATE user_streaks
   SET freeze_balance = freeze_balance - 1,
       freeze_history = freeze_history || jsonb_build_object('used_at', now(), 'reason', @reason)
   WHERE user_id = @userId AND freeze_balance > 0;
   -- affected rows = 1 indicates success; 0 indicates insufficient balance.
   ```
- The Bot is configured for exactly one Mezon Clan. Its external Clan ID is stored in the singleton `bot_configuration` row; member snapshots do not carry a Clan foreign key. OAuth access/refresh tokens and webhook secrets belong in a secrets manager, not these tables.
- Timestamps are `TIMESTAMPTZ`; streak/activity dates are persisted in the learner's configured timezone (default `Asia/Ho_Chi_Minh`).
- XP is an append-only ledger; leaderboard totals can be derived from it. Weekly snapshots preserve published results.
- Weekly leaderboard ranks learners within the configured Clan in `weekly_leaderboards`. XP awards (5 new word, 3 successful review, 10 Clan quiz plus 5 speed bonus, 25 lesson, 30/10 duel) and the daily 20 XP streak threshold are application rules recorded in the XP ledger.
- Global `moderator` and `admin` permissions use `users(role)` (Simple RBAC).
- Mutable external profile fields are snapshots. Synchronize them from Mezon events/API.
- JSONB is limited to variable question payloads and AI context; core entities remain relational.
