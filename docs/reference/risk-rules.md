# Lingual Risk Rules & Security Guidelines

## 1. Gamification Anti-Exploit Rules
1. **XP Duplication Prevention:**
   - Every transaction in `xp_ledger` requires either a unique `idempotency_key` or enforces `uq_xp_ledger_source` on `(source_type, source_id)`.
   - Replaying completion API calls cannot yield duplicate XP.
2. **Speed-Run Anomaly Detection:**
   - Lesson completions with elapsed time `< 10 seconds` or quiz answers with response times `< 200 ms` are flagged for botting anomalies.
3. **Streak Integrity:**
   - Streaks advance only when a learner accumulates at least `20 XP` in their local calendar day (`users.timezone`).
   - Streak freeze is consumed automatically at `00:00:01` if learner was inactive and `freeze_balance > 0`.

---

## 2. API Security & Rate Limiting
- **Sliding-Window Rate Limiting:** Configured on ASP.NET Core:
  - Public endpoints: 60 requests/minute per IP.
  - Quiz/Duel submission endpoints: 20 requests/minute per authenticated user.
  - LLM AI Tutor endpoints: 10 requests/minute per user (mitigating token exhaustion).
- **Webhook Signature Verification:** All incoming Mezon webhook payloads must be verified against HMAC SHA-256 signatures before being queued.
- **Audit Immutability:** Sensitive operational changes are written to `audit_logs` with strict `REVOKE UPDATE, DELETE` applied for the service role.
