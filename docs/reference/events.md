# Lingual Event-Driven Architecture Reference

## 1. Overview
Lingual utilizes an in-process **MediatR / Domain Event** model within the ASP.NET Core Modular Monolith for synchronous intra-module decoupling, combined with Mezon Webhook events for external triggers.

---

## 2. Platform Webhook Events (Mezon -> Lingual)
- `mezon.user_joined`: Fired when a learner joins the configured Clan; provisions user record and default profile.
- `mezon.message_created`: Evaluated by `Lingual.Modules.MezonBot` to handle bot commands (`/daily`, `/quiz`, `/stats`, `/duel`).
- `mezon.reaction_added`: Used for quick-vote answer selection during interactive Clan channel quizzes.

---

## 3. Domain Events Catalog (Intra-Module)

| Event Name | Emitting Module | Handling Modules | Business Effect |
| :--- | :--- | :--- | :--- |
| `LessonCompletedEvent` | `Modules.Learning` | `Gamification`, `MezonBot` | Awards 25 XP to `xp_ledger`, checks daily streak threshold (20 XP). |
| `SrsCardReviewedEvent` | `Modules.Learning` | `Gamification` | Awards 3 XP per successful recall card. |
| `DuelFinishedEvent` | `Modules.Gamification` | `MezonBot`, `Identity` | Awards 30 XP to winner, 10 XP to participant; broadcasts result embed to Clan channel. |
| `StreakMilestoneReached` | `Modules.Gamification` | `MezonBot`, `Learning` | Grants +1 Streak Freeze voucher on 7-day, 14-day, and 30-day milestones. |
| `SecurityAuditEvent` | All Modules | `Modules.Identity (Audit)` | Inserts record into `audit_logs` table asynchronously. |
