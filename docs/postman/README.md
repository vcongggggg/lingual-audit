# Lingual Postman suites

## Active branch suite

Import `Lingual_Sprint1_Postman_Collection.json` and `Lingual_Local_Environment.json` into Postman. The default `baseUrl` is `http://127.0.0.1:5000` for Docker Compose. For `dotnet run` with the `http` launch profile, set it to `http://127.0.0.1:5112`.

Start PostgreSQL and Redis before running the content requests. Run the collection in order so the vocabulary and quiz list requests can save IDs used by the review and quiz-attempt requests. The review and quiz-attempt requests write records and XP to the local database; their repeated requests check idempotency.

Coverage in the active-branch suite:

- Health, development Swagger document, and the sample weather controller.
- Vocabulary and quiz retrieval, filters, and lower limit bounds.
- Review and quiz-attempt success, payload validation, and duplicate idempotency keys.
- Leaderboard response and lower limit bound.

## Branch-only suite

`Lingual_Branch_Features_Postman_Collection.json` contains manual requests for Mezon SSO from `origin/minh/feat/database-migration-and-mezon-sso` and AI grammar checking from `origin/cong/feature/bot-mezon-ai-tutor`. Those routes are not present in the checked-out branch. SSO calls need valid Mezon OAuth credentials, and grammar checks need the AI provider configured.

Product documents describe additional planned endpoints such as courses, placement, duels, and AI conversations. They are not included as runnable requests because no matching route is registered in the active API branch or the inspected feature branches.
