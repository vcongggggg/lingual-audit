# Lingual Data Model Reference

## 1. Architectural Philosophy
The Lingual data model follows the **Core MVP 22-Table Architecture** streamlined under the guidance of Mentor **Mai Hồng Mận**:
- **Relational Integrity:** Core identities, progress, financial/gamification ledgers, and foreign keys remain strict relational tables.
- **JSONB Consolidation:** Dynamic, variable-length, or document-oriented payloads (session histories, quiz answers, question choices, bot schedules, LLM chat messages) are consolidated into structured `JSONB` columns.
- **Idempotency & Safety:** All point/XP awards are strictly audited via `xp_ledger` with unique constraints against duplicate insertion.

---

## 2. Module Inventory (22 Tables)

| Module | Table | Purpose |
| :--- | :--- | :--- |
| **01. Identity** | `users` | Primary user identity, Mezon SSO identifier, and Simple RBAC role (`learner`, `moderator`, `admin`). |
| | `learner_profiles` | Personalized goals, daily commitment, CEFR level, and onboarding state. |
| | `placement_tests` | CEFR placement evaluation session with complete `answers_detail` (JSONB). |
| **02. Curriculum** | `courses` | Language courses aligned with CEFR standards (A1–B2). |
| | `units` | Thematic units within a course. |
| | `lessons` | Granular study lessons containing mapped `vocabulary_ids` (JSONB). |
| | `vocabulary_items` | Lexical terms, IPA, meaning, audio URLs, and sample `examples` (JSONB). |
| **03. Learning & SRS** | `lesson_progress` | Completion status and attempt histories (`session_history` JSONB). |
| | `srs_cards` | SuperMemo-2 Spaced Repetition flashcards with review logs (`review_history` JSONB). |
| | `xp_ledger` | Immutable financial-grade audit ledger for learner experience points. |
| | `user_streaks` | Day streaks, freeze balances, freeze history, and daily heatmap activity (JSONB). |
| **04. Quiz Engine** | `quizzes` | Quiz headers and configuration. |
| | `quiz_questions` | Question prompts, difficulty, points, and choice options (`options` JSONB). |
| | `quiz_attempts` | Examination submissions with question-by-question `answers_detail` (JSONB). |
| **05. Community Bot** | `bot_configuration` | Singleton configuration for the Mezon Clan Bot including automated cron `schedules` (JSONB). |
| | `clan_quiz_sessions` | Realtime interactive Clan quiz events with participant `responses` (JSONB). |
| | `clan_moderator_grants` | Scoped Clan Moderator authorizations separate from global system roles. |
| **06. Competition** | `duel_matches` | 1vs1 Word Duel sessions, question IDs, player answers, and calculated scores (JSONB). |
| | `weekly_leaderboards` | Historical weekly XP standing snapshots and finalized rankings. |
| **07. AI Assistant** | `ai_scenarios` | Structured roleplay and correction prompts for Mascot LingLing. |
| | `ai_conversations` | Dialogue sessions preserving conversation threads (`messages` JSONB). |
| **09. Audit** | `audit_logs` | Append-only security audit trail for administrative and moderative actions. |

---

## 3. Database Initializer & Migration
All tables, constraints, partial indexes, and automatic timestamp triggers (`set_updated_at()`) are encapsulated in:
- `docs/database/sql/00_init_all.sql`
- `docs/database/lingual_full_schema.dbml` (Visual diagram on [dbdiagram.io](https://dbdiagram.io/d))
