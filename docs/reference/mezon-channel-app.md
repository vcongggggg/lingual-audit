# Mezon Channel App & Mini-App Integration

## 1. Overview
The Lingual web experience is delivered as a **Channel Mini-App** directly embedded into the Mezon Desktop / Web client iframe interface, built with **Next.js 14 App Router** and TailwindCSS.

---

## 2. Mezon Bridge & Authentication
1. **SSO Handshake:**
   - The Mezon client injects a short-lived `mezon_auth_token` into the mini-app context via `window.mezonBridge` or URL search params.
   - Next.js frontend sends this token to `POST /api/v1/auth/mezon`.
   - Backend validates the token against Mezon OAuth2 endpoints and issues a JWT Bearer token with claims (`userId`, `role`, `mezonUserId`).
2. **Context Synchronization:**
   - Dark/Light theme matches the parent Mezon client theme setting.
   - Language locale follows `preferred_locale` (default `vi-VN`).

---

## 3. Mini-App Core Views
- `/dashboard`: Daily streak summary, SRS review widget, upcoming Clan events.
- `/curriculum`: Interactive CEFR course map and lesson launcher.
- `/duel`: 1vs1 Realtime Word Duel arena via SignalR.
- `/leaderboard`: Weekly Clan leaderboard and podium.
- `/tutor`: Roleplay chat and grammar correction with Mascot LingLing (Google Gemini 1.5 Flash).
