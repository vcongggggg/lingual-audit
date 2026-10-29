# Lingual Notification Architecture

## 1. Notification Strategy
Lingual delivers notifications primarily through the **Mezon Platform Ecosystem**:
1. **Public Clan Channel Broadcasts:** Community-wide announcements (Word of the Day, Clan Quiz sessions, Weekly Leaderboard podiums).
2. **Direct Bot Messages (DM):** Personal reminders (Streak at risk, SRS review cards due, 1vs1 Duel challenge invitations).
3. **In-App Toast Alerts:** Realtime WebSocket notifications delivered directly within the Next.js 14 Channel Mini-App.

---

## 2. Notification Dispatch Pipeline
```text
[Domain Event]
      │
      ▼
[Notification Orchestrator] ─── Check Learner Notification Preferences & Timezone
      │
      ├── Clan Broadcast? ──► Mezon Bot Gateway ──► Specific Clan Channel
      │
      └── Private Alert?   ──► Mezon DM API / SignalR Hub ──► Learner Device
```

---

## 3. Rate Limiting & Quiet Hours
- Reminders honor the learner's configured timezone (`users.timezone`, default `Asia/Ho_Chi_Minh`).
- **Quiet Hours Policy:** No automated reminder notifications are dispatched between 22:30 and 07:00 local time unless explicitly triggered by an active user action.
