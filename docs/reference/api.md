# Lingual API Reference & Endpoints

## 1. Overview
The Lingual backend is implemented using **ASP.NET Core 8** under a **Modular Monolith** architecture.
- **Base Route:** `/api/v1`
- **SignalR Hub:** `/hubs/game`
- **Swagger Documentation:** Available locally at `http://localhost:5000/swagger`

---

## 2. Core REST Endpoints

### 2.1 Identity & Profile (`/api/v1/users`, `/api/v1/auth`)
- `POST /api/v1/auth/mezon`: Exchanges Mezon SSO OAuth token for Lingual JWT Bearer token.
- `GET /api/v1/users/me`: Retrieves authenticated user profile, permissions, and learner stats.
- `PUT /api/v1/users/me/profile`: Updates learning goals, daily commitment, and target CEFR level.

### 2.2 Curriculum & Learning (`/api/v1/courses`, `/api/v1/learning`)
- `GET /api/v1/courses`: Returns available CEFR language courses.
- `GET /api/v1/courses/{courseId}/units`: Lists units and associated lessons.
- `POST /api/v1/learning/lessons/{lessonId}/complete`: Submits lesson completion and triggers XP award.
- `GET /api/v1/learning/srs/due`: Fetches cards ready for SM-2 Spaced Repetition review.
- `POST /api/v1/learning/srs/review`: Records review outcome (again, hard, good, easy) and updates intervals.

### 2.3 Quiz Engine (`/api/v1/quizzes`)
- `GET /api/v1/quizzes/{quizId}`: Loads quiz metadata and questions.
- `POST /api/v1/quizzes/{quizId}/attempts`: Starts a new timed quiz attempt.
- `POST /api/v1/quizzes/attempts/{attemptId}/submit`: Submits final answers and receives instant score breakdown.

### 2.4 Word Duel & Competition (`/api/v1/competition`)
- `POST /api/v1/competition/duels/challenge`: Sends a 1vs1 challenge to a Clan member.
- `GET /api/v1/competition/leaderboard/weekly`: Fetches top Clan rankings for the current active week.

### 2.5 AI Assistant LingLing (`/api/v1/ai`)
- `GET /api/v1/ai/scenarios`: Lists published conversation and roleplay scenarios.
- `POST /api/v1/ai/conversations`: Starts a new dialogue session.
- `POST /api/v1/ai/conversations/{id}/messages`: Sends user message to Google Gemini 1.5 Flash and returns Mascot LingLing's response with grammar coaching.

---

## 3. SignalR Realtime Hub (`/hubs/game`)
- **Events (Server -> Client):**
  - `DuelMatched(matchId, opponent)`
  - `QuestionDispatched(questionIndex, payload)`
  - `DuelResultAnnounced(matchId, winnerId, scores)`
  - `ClanQuizStarted(sessionId, question)`
- **Methods (Client -> Server):**
  - `JoinDuelQueue()`
  - `SubmitDuelAnswer(matchId, questionId, selectedKey, responseTimeMs)`
