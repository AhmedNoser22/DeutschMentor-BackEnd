# DeutschMentor – Backend (.NET 10)

REST API for DeutschMentor, a platform that teaches German from A1 to C1 to Arabic speakers (interface and explanations in English, with Arabic translations).
A learner unlocks the next level only by passing the exam of the previous one and earns a certificate for every level passed.
There is no AI anywhere: the mentor and the writing checker are plain, documented rules.

## Contents

1. [Tech stack](#tech-stack)
2. [Architecture](#architecture)
3. [Getting started](#getting-started)
4. [Configuration](#configuration)
5. [Features and rules](#features-and-rules)
6. [Content packs and the importer](#content-packs-and-the-importer)
7. [API reference](#api-reference)
8. [Data model](#data-model)
9. [Security](#security)
10. [Testing](#testing)
11. [Deployment](#deployment)
12. [Troubleshooting](#troubleshooting)
13. [Third-party licenses and data sources](#third-party-licenses-and-data-sources)
14. [Known limitations](#known-limitations)

## Tech stack

| Area | Choice |
|---|---|
| Runtime | .NET 10, ASP.NET Core controllers |
| Database | SQL Server, EF Core (code first, migrations) |
| Auth | JWT access tokens, rotating refresh tokens, Google Sign-In (ID token validation) |
| Passwords | BCrypt (enhanced, work factor 12) |
| Email | MailKit over SMTP (port 587, STARTTLS) |
| PDF | QuestPDF (Community license) |
| API docs | Built-in OpenAPI + Scalar at `/scalar` (Development only) |
| Tests | xUnit |

## Architecture

Clean Architecture. Dependencies point inwards only.

```
backend/
├── DeutschMentor.slnx
├── src/
│   ├── DeutschMentor.Domain          entities, enums, domain exceptions (no dependencies)
│   ├── DeutschMentor.Application     use cases, DTOs, interfaces, business rules (depends on Domain)
│   ├── DeutschMentor.Infrastructure  EF Core, SMTP, JWT, Google, PDF, background jobs (depends on Application)
│   └── DeutschMentor.Api             controllers, middleware, composition root (depends on Application + Infrastructure)
└── tests/DeutschMentor.Tests         unit tests for the business rules
```

Why `IApplicationDbContext` exists: `Application` must not reference `Infrastructure` (that would be a circular reference).
Application owns the abstraction, Infrastructure implements it, and DI maps both to the same `ApplicationDbContext` object.

Feature folders inside `Application`: `Authentication`, `Learning`, `Vocabulary`, `Grammar`, `Exercises`, `Reading`, `Writing`, `Exams`, `Certificates`, `Review`, `Drills`, `Progress`, `Content` (importer), `Admin`.

Errors are domain exceptions (`BadRequest`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`, `TooManyRequests`) carrying a stable `code`.
`GlobalExceptionHandler` turns them into `{ "code": "...", "message": "..." }` with the matching HTTP status. Clients branch on `code`, never on the message.

## Getting started

Prerequisites: .NET 10 SDK, SQL Server (Docker or LocalDB), `dotnet-ef` tool (`dotnet tool install --global dotnet-ef`).

```bash
# SQL Server in Docker (optional)
docker run -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=Your_Strong_Pass123" -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest

cd backend
dotnet build
dotnet ef migrations add <Name> --project src/DeutschMentor.Infrastructure --startup-project src/DeutschMentor.Api --output-dir Persistence/Migrations
dotnet run --project src/DeutschMentor.Api --launch-profile https
```

* In Development the migrations are applied automatically on start-up and the five levels (A1–C1) are created.
* The API listens on `https://localhost:7080` (set in `launchSettings.json`). Run `dotnet dev-certs https --trust` once.
* Interactive docs: `https://localhost:7080/scalar`.
* An administrator is created from `Seed:AdminEmail` / `Seed:AdminPassword` if no such user exists.
* Learning content is **not** seeded. Import the content packs from the Admin panel (see below).

## Configuration

Non-secret defaults live in `appsettings.json`. Secrets go in `appsettings.Development.json` (git-ignored) or in user-secrets locally, and in environment variables in production (`Section__Key`).

| Key | Purpose |
|---|---|
| `ConnectionStrings:Default` | SQL Server connection string |
| `Jwt:Issuer`, `Jwt:Audience` | Must be non-empty. They are written into every token and validated on every request |
| `Jwt:Key` | At least 32 characters. Startup fails otherwise |
| `Jwt:AccessTokenMinutes` | Default 15 |
| `Security:CodePepper` | Secret used to hash the 6-digit email codes |
| `Google:ClientId` | OAuth client ID used to validate Google ID tokens (the audience) |
| `Smtp:Host`, `Smtp:Port`, `Smtp:UseStartTls` | SMTP server, normally port 587 with STARTTLS |
| `Smtp:User`, `Smtp:Password`, `Smtp:FromAddress`, `Smtp:FromName` | SMTP credentials and sender |
| `Cors:Origins` | Allowed front-end origins (array). Example env var: `Cors__Origins__0` |
| `Seed:AdminEmail`, `Seed:AdminPassword` | Initial administrator |

Never commit secrets. The Google Client ID is public by design (it ships to every browser). The Client Secret is never used.

## Features and rules

### Authentication and accounts

* **Registration never creates a user.** `register` stores a row in `PendingRegistrations` (password already hashed) and emails a 6-digit code. The `Users` row is created only when `confirm-email` succeeds, in one transaction. Unconfirmed rows are purged by a background job after 24 hours.
* Codes are hashed (HMAC-SHA256 with the pepper), valid 10 minutes, limited to 5 wrong attempts, with a 60-second resend cooldown.
* Logging in with an unconfirmed account returns `email_not_confirmed` so the client can send the user to the code screen.
* **Google sign-in:** the ID token is validated (signature and audience). Existing users with the same verified email are linked, new users are created already confirmed.
* **Password reset:** same code mechanism. `forgot-password` never reveals whether the email exists. A successful reset revokes all refresh tokens.
* **Tokens:** 15-minute JWT access token and a 30-day refresh token. Refresh tokens are stored hashed and **rotated** on every use; presenting an already-used token revokes the whole family.
* **Rate limiting:** policy `auth` (30 requests/minute/IP) on authentication, account and public verification endpoints.
* **Account lock:** `Users.IsActive = false` blocks sign-in, refresh, and any request with an existing access token (checked on every authenticated request).
* Profile: change name, change/set password (Google-only accounts can set one). Changing the password revokes all other devices.

### Learning path

`Level → Module → Unit → Lesson`.

* Level 1 (A1) is always open. A level opens when the user has a `LevelPass` for the previous level.
* Inside an open level, lessons unlock in order: the first unfinished lesson is `Current`, later ones are `Locked`. Lessons of a level passed through the placement test are `Open`.
* A lesson has five steps (Vocabulary, Grammar, Practice, Reading, Writing). Vocabulary and Grammar are marked when opened; Practice, Reading and Writing are marked when completed. Finishing needs all five (enforced by the server).
* Only `Published` lessons are visible. All access checks are server-side.

### Exercises, reading, writing

* Five exercise types: multiple choice, true/false, fill in the blank (several accepted answers, case/space insensitive), ordering, matching.
* Correct answers are never sent to the client before answering. The server grades, stores every attempt, and returns the correct answer plus an English and Arabic explanation.
* Reading passages are dialogues or texts whose questions are normal exercises.
* **Writing is graded by rules, not AI:** each task defines groups of accepted words/phrases. Score = 80% required-group coverage + 20% length against the minimum word count. It does **not** judge grammar and says so. The model answer is shown only after submitting. A learner can request teacher review (`Pending`), and an admin answers with a score and feedback.

### Exams and placement

* A level exam opens only after **every lesson of the level** is finished. Pass mark **70%** (per level). After a failed attempt the exam can be retaken after **24 hours** (counted from the end of the attempt).
* Questions come from a separate exam pool (`ExamQuestions`), never from lesson exercises, so practice feedback cannot leak the answers. Up to 40 questions, balanced across Vocabulary, Grammar, Reading and Writing; time is 1.5 minutes per question (minimum 10).
* The **timer is server-authoritative** (`ExpiresAtUtc`). Answers are saved during the exam, so a refresh or a closed phone resumes with the real remaining time. An expired attempt is graded automatically from the saved answers (60-second grace for the auto-submit).
* Results are broken down per skill. Passing creates the `LevelPass`, issues the certificate and moves the user to the next level, atomically.
* **Placement test:** offered once, only to users who chose it at registration and have no progress. 4 questions per level. The user starts at the first level where they score below 70%; earlier levels get a `LevelPass` of source `Placement` (no certificate). C1 is never granted by placement. Users can skip it and start at A1.

### Certificates

* One certificate per level passed by **exam**, with a code like `DM-A1-2026-7H4KQ9XD` (8 random characters from an unambiguous alphabet, unique index).
* Name and score are stored at issue time. `GET /api/certificates/{id}/pdf` renders an A4 landscape PDF with QuestPDF.
* **Public verification** (`GET /api/certificates/verify/{code}`) needs no login and returns only the name, level, score and date.
* Optional: for Arabic names in the PDF, put `NotoSansArabic-Regular.ttf` in `src/DeutschMentor.Infrastructure/Assets/Fonts/` and add `<Content Include="Assets\Fonts\*.ttf" CopyToOutputDirectory="PreserveNewest" />` to the Infrastructure project.

### Spaced review (SM-2 style)

Cards come from words the user added with "Add to review". Rating buttons and the resulting interval:

| Rating | New card | Second review | Later |
|---|---|---|---|
| Again | 1 day, ease −0.2, repetitions reset | same | same |
| Hard | 2 days | 3 days | ×1.2 |
| Good | 3 days | 6 days | ×ease |
| Easy | 5 days | 9 days | ×ease×1.3 |

Ease starts at 2.5, stays between 1.3 and 3.0, intervals are capped at 365 days. "Due today" means due before the end of the user's **local** day (the client sends `X-Timezone-Offset` in minutes). Every rating is logged in `ReviewLogs`.

### Progress, streak, mentor, achievements

* **Streak** is computed from real activity, not a stored counter. A day counts if the user answered an exercise, rated a review card, did the article drill, submitted writing, started or finished a lesson, or started an exam. The current streak stays alive until the local day ends.
* **Daily plan** (computed on request): warm-up with new words of the current lesson, due review cards, the current lesson, and the article drill. Items without value are removed.
* **Mentor** (rules, in priority order, max two messages plus a holiday note): article mistakes in the last 7 days, a failed exam in the last 14 days with its weak skills, practice accuracy below 60%, a large review backlog, a streak at risk, a good streak, and German public holidays (fixed-date ones).
* **Achievements** are evaluated when Today or the achievements page loads and stored once: lessons finished, words recalled, reviews, streak lengths, a perfect exam, and a certificate per level.
* **Article drill:** 10 nouns with articles from unlocked lessons; words missed in the last 14 days come first. Every answer is stored.

### Administration

Role `Admin` only (`[Authorize(Roles = "Admin")]`).

* Statistics, lesson list with search/filter, publish/unpublish (a lesson needs vocabulary to be published), delete (drafts without student progress only).
* **Content import** with a dry-run check (see next section).
* **Writing review queue** for learners who asked for a teacher.
* **Exam pool overview** per level and skill.
* **User management:** list every user (search by name/email, filter by role and status), see level, sign-in method, last login, lessons finished and certificates, and **lock or unlock** any normal user. Admin accounts and your own account cannot be locked. Locking revokes all refresh tokens and rejects existing access tokens immediately.

## Content packs and the importer

Content is delivered as JSON "packs", one file per level (`content/a1.json` … `c1.json`).

```jsonc
{
  "schemaVersion": 1,
  "level": "A1",
  "modules": [{ "order": 1, "title": "...", "titleEn": "...", "units": [{ "order": 1, "title": "...", "titleEn": "...", "lessons": [{
    "order": 1, "code": "A1-1-01", "title": "...", "titleEn": "...", "titleAr": "...", "minutes": 10,
    "vocabulary": [{ "german": "Tisch", "article": "der", "pos": "Noun", "english": "table", "arabic": "...", "example": "...", "exampleEn": "..." }],
    "grammar":    [{ "title": "...", "summary": "...", "table": { "headers": [], "rows": [[]], "highlight": [1] }, "noteEn": "...", "noteAr": "..." }],
    "exercises":  [{ "type": "MultipleChoice|TrueFalse|FillBlank|Ordering|Matching", "prompt": "...", "options": [], "correct": "...", "answer": true,
                     "blanks": [[]], "solution": [], "pairs": [[]], "explainEn": "...", "explainAr": "..." }],
    "reading":    [{ "title": "...", "lines": [{ "speaker": "...", "text": "..." }], "questions": [ /* exercises */ ] }],
    "writing":    [{ "title": "...", "prompt": "...", "sample": "...", "minWords": 6, "pass": 60, "groups": [{ "label": "...", "anyOf": [] }] }]
  }] }] }],
  "exam": [{ "skill": "Vocabulary|Grammar|Reading|Writing", /* same fields as an exercise */ }]
}
```

Exercise fields by type: `MultipleChoice` (`options`, `correct`), `TrueFalse` (`answer`), `FillBlank` (`blanks`, one `___` per blank), `Ordering` (`solution`), `Matching` (`pairs` of `[left, right]`).

**Validation** (identical rules in the C# importer and in `tools/audit_pack.py`): structure and uniqueness; lesson code `<LEVEL>-<module>-<nn>` whose module number equals the module order; nouns must have an article and a capital letter; Arabic fields must contain Arabic and English fields must not; multiple-choice options unique and containing the correct one; blank count equals `___` count; table rows consistent; and **every writing sample answer must pass its own rules**. Any error means nothing is saved.

**Import behaviour:** upsert. Lessons are matched by `code`; vocabulary, grammar, exercises, passages and writing tasks by their order inside the lesson; exam questions by skill, type, prompt and payload. Nothing is ever deleted, so student progress and review cards stay intact. New lessons are `Draft` unless imported with "publish".

**Quality workflow:** write the pack → `python tools/audit_pack.py content/*.json` (also checks noun genders against the Wiktionary-derived `german-nouns` dataset) → human review of German and Arabic → import as draft → publish lesson by lesson.

The automatic checks catch structural and gender errors. They cannot judge the quality of a German sentence or an Arabic translation, so a human review is required before publishing.

## API reference

Base path `/api`. All endpoints except those marked **public** need `Authorization: Bearer <token>`.

| Area | Endpoints |
|---|---|
| Auth (public) | `POST auth/register`, `confirm-email`, `resend-code`, `login`, `google`, `refresh`, `logout`, `forgot-password`, `reset-password` |
| Account | `GET account/me`, `PUT account/me`, `POST account/change-password` |
| Learning | `GET learning/levels`, `learning/levels/{code}`, `learning/lessons/{id}`; `POST learning/lessons/{id}/steps/{step}`, `learning/lessons/{id}/finish` |
| Vocabulary | `GET vocabulary` (search/filter/paging), `vocabulary/lesson/{lessonId}`; `POST/DELETE vocabulary/{id}/review` |
| Grammar | `GET grammar`, `grammar/{id}`, `grammar/lesson/{lessonId}` |
| Exercises | `GET exercises/lesson/{lessonId}`; `POST exercises/{id}/answer` |
| Reading | `GET reading/lesson/{lessonId}` |
| Writing | `GET writing/lesson/{lessonId}`; `POST writing/tasks/{taskId}/submissions`, `writing/submissions/{id}/request-review` |
| Exams | `GET exams`; `POST exams/levels/{code}/start`, `exams/placement/start`, `exams/placement/skip`; `GET exams/attempts/{id}`; `PUT exams/attempts/{id}/answers/{questionId}`; `POST exams/attempts/{id}/submit`; `GET exams/attempts/{id}/result` |
| Certificates | `GET certificates`, `certificates/{id}`, `certificates/{id}/pdf`; `GET certificates/verify/{code}` (**public**) |
| Review | `GET review/summary`, `review/queue`; `POST review/{id}/rate` |
| Drills | `GET drills/articles`; `POST drills/articles/answer` |
| Progress | `GET progress/today`, `progress/achievements` |
| Admin | `GET admin/stats`, `admin/lessons`, `admin/writing/pending`, `admin/exam-pool`; `POST admin/lessons/{id}/publish`, `unpublish`; `DELETE admin/lessons/{id}`; `POST admin/import/check`, `admin/import?publish=`; `POST admin/writing/{id}/review` |
| Admin users | `GET admin/users` (search, role, status, page); `POST admin/users/{id}/lock`, `admin/users/{id}/unlock` |

Conventions: enums are serialised as strings, dates are UTC with a `Z` suffix, errors are `{ code, message }`, and clients send `X-Timezone-Offset` (minutes east of UTC).

## Data model

Identity: `Users`, `PendingRegistrations`, `PasswordResetCodes`, `RefreshTokens`.
Curriculum: `Levels`, `Modules`, `Units`, `Lessons`, `VocabularyItems`, `GrammarTopics`, `ReadingPassages`, `Exercises`, `WritingTasks`.
Learner data: `UserLessonProgress` (steps as a bitmask), `LevelPasses`, `ExerciseAttempts`, `WritingSubmissions`, `ReviewItems`, `ReviewLogs`, `DrillAttempts`, `UserAchievements`.
Exams: `ExamQuestions`, `ExamAttempts` (a filtered unique index allows one in-progress attempt per user/kind/level), `ExamAnswers`, `Certificates`.

Enums are stored as strings. Deleting a user cascades to their data. Curriculum deletes are restricted so a level is never removed with its content by accident.

## Security

* BCrypt password hashing; refresh tokens and email codes are stored hashed.
* JWT issuer, audience, signature and lifetime are validated; a locked or deleted account is rejected on every request.
* Correct exercise and exam answers are never sent before submission; exam time is enforced by the server.
* Rate limiting, CORS allow-list, no secrets in the repository, generic responses for unknown emails.
* Public verification returns the minimum data and uses unguessable codes.
* The Google Client ID is public by design; keep Client Secrets, JWT key, SMTP password and the database password in environment variables.

## Testing

```bash
cd backend
dotnet test
```

The unit tests cover the spaced-review scheduler, streak calculation, local-day math, exercise grading, writing evaluation, exam building, mentor rules and the content validator. The tests project needs `InternalsVisibleTo` for `DeutschMentor.Tests` in the Application project.

Manual end-to-end checklist: register with a code, wrong code ×5, Google sign-in, password reset, import `a1.json`, finish A1 lessons, take the exam (fail, then pass), download the certificate and verify it in a private window, placement test with a new account, writing review by the admin, lock a user and confirm the user is signed out.

## Deployment

* Provide every secret as an environment variable (`ConnectionStrings__Default`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Security__CodePepper`, `Google__ClientId`, `Smtp__*`, `Cors__Origins__0`).
* Apply migrations before the first start in production (`dotnet ef database update`); automatic migration runs in Development only.
* Use HTTPS only. Set `Cors__Origins__0` to the exact front-end origin, and register the same origin in the Google OAuth client.
* On Linux hosts QuestPDF may need `libfontconfig1`.

## Troubleshooting

| Symptom | Cause and fix |
|---|---|
| Every request returns 401 right after login | `Jwt:Issuer` or `Jwt:Audience` is empty so tokens have no `iss`/`aud`. Set both and sign in again |
| Browser blocks calls | The origin is missing from `Cors:Origins` |
| No emails arrive | Check `Smtp:*`; Gmail needs an App Password and 2-Step Verification |
| Google button fails | The front-end origin is missing from "Authorized JavaScript origins", or `Google:ClientId` differs from the front-end value |
| Import says "conflicts with existing content" | Another module/unit/lesson already uses that order. Align orders or clean the content tables |
| Dates shift by hours in the browser | A date reached the client without `Z`. Use `AsUtc()` when mapping dates to DTOs |

## Third-party licenses and data sources

* QuestPDF is used under its Community license. Check that your organisation qualifies, or buy a license.
* Content packs are original work. The audit script uses the `german-nouns` dataset (Wiktionary, CC BY-SA 4.0) and `wordfreq` for checks only; no dataset text is shipped to users.
* Do not copy textbooks or exam materials of publishers or institutes (Goethe, Netzwerk, Schritte and similar).

## Known limitations

* The amount of sample content is small (about 21 lessons and 96 exam questions). More packs are added the same way.
* Import accepts JSON only (no CSV). Single words or exercises are edited by editing the pack and re-importing.
* Writing is rule-based and does not judge grammar.
* Public holidays in the mentor are fixed-date ones only.
* Certificates are completion certificates of the platform, not official CEFR certificates (Goethe, telc, ÖSD).
