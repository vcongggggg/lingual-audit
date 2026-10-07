# Lingual Audit Logs Specification

## 1. Overview
The **Audit Logging** system provides a tamper-evident, append-only operational trail for administrative actions, privilege elevations, Clan moderation events, and sensitive configuration adjustments.

This specification fulfills compliance requirements **ADM-09** and **ADM-12** documented in `docs/experience/role/admin.md` and `docs/experience/role-permission.md`.

---

## 2. Relational Schema (`audit_logs`)

```sql
CREATE TABLE IF NOT EXISTS audit_logs (
    id                 UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    actor_id           UUID REFERENCES users(id) ON DELETE SET NULL,
    action             VARCHAR(60) NOT NULL,
    entity_type        VARCHAR(60) NOT NULL,
    entity_id          UUID,
    reason             TEXT,
    metadata           JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at         TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS idx_audit_logs_actor ON audit_logs(actor_id, created_at DESC);
CREATE INDEX IF NOT EXISTS idx_audit_logs_entity ON audit_logs(entity_type, entity_id, created_at DESC);
```

---

## 3. Immutability & Access Control
- **Append-Only Policy:** Application users, moderators, and even system services cannot edit or delete audit records.
- **Production Privilege Revocation:**
  ```sql
  REVOKE UPDATE, DELETE ON audit_logs FROM lingual_user;
  ```
- **Actor Integrity:** If an actor account is deleted, `actor_id` is retained as `SET NULL` while the historical metadata preserves `actor_snapshot` for non-repudiation.

---

## 4. Standard Audit Event Catalog

| Action Identifier | Entity Type | Trigger Context | Required Metadata Fields |
| :--- | :--- | :--- | :--- |
| `ROLE_ASSIGNED` | `users` | Admin promotes learner to moderator/admin | `old_role`, `new_role`, `assigned_by` |
| `CLAN_MOD_GRANTED` | `clan_moderator_grants` | Admin grants Clan Moderator scope | `clan_id`, `expires_at`, `grant_id` |
| `CLAN_MOD_REVOKED` | `clan_moderator_grants` | Admin revokes Clan Moderator scope | `clan_id`, `revoked_by`, `reason` |
| `BOT_CONFIG_UPDATED` | `bot_configuration` | Changing Clan quiz or schedule params | `changes`, `client_ip` |
| `USER_SUSPENDED` | `users` | Moderator/Admin suspends an abusive user | `ban_duration_hours`, `evidence_url` |
| `XP_MANUALLY_ADJUSTED` | `xp_ledger` | Admin corrects corrupted learner XP | `xp_delta`, `ticket_id`, `justification` |

---

## 5. Query Patterns
- **Filter by Actor:**
  ```sql
  SELECT * FROM audit_logs WHERE actor_id = :userId ORDER BY created_at DESC LIMIT 50;
  ```
- **Inspect Specific Entity History:**
  ```sql
  SELECT * FROM audit_logs WHERE entity_type = 'users' AND entity_id = :targetUserId ORDER BY created_at DESC;
  ```
