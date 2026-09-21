# InterviewLoop

AI-graded mock coding interviews. Pick a prompt, solve it in an in-browser editor, and get
structured AI feedback on correctness, complexity, and clarity — plus a history of past attempts.

Built as a portfolio project to demonstrate:

- **Frontend:** Next.js (App Router), TypeScript, Redux Toolkit + RTK Query, Monaco Editor
- **Backend:** C# / .NET 10 Web API, EF Core, PostgreSQL (Npgsql)
- **AI grading:** Google Gemini API (falls back to demo-mode placeholder feedback if no API key is configured, so the app is fully clickable without one)

## Project structure

```
backend/InterviewLoop.Api/         .NET 10 Web API (prompts, attempts, Gemini grading)
backend/InterviewLoop.Api.Tests/   xUnit tests (grading service + controllers)
frontend/                          Next.js app
docker-compose.yml                 Local PostgreSQL for development
```

## Running locally

**1. Start Postgres:**

```bash
docker compose up -d
```

**2. Run the backend** (applies EF Core migrations and seeds prompts automatically on startup):

```bash
cd backend/InterviewLoop.Api
dotnet run
```

Runs on `http://localhost:5080` by default. Without a Gemini API key configured it serves
placeholder "demo mode" feedback so you can exercise the whole flow immediately. To enable real
AI grading, set an API key via user-secrets or an environment variable:

```bash
dotnet user-secrets set "Gemini:ApiKey" "<your-key>"
# or
export GEMINI__APIKEY="<your-key>"
```

**3. Run the frontend:**

```bash
cd frontend
npm install
npm run dev
```

Runs on `http://localhost:3000` and expects the API at `http://localhost:5080/api`
(configurable via `NEXT_PUBLIC_API_URL` in `frontend/.env.local`).

## Running tests

```bash
dotnet test InterviewLoop.slnx
```

Covers the Gemini response parsing/error-handling (including "thinking" models that split
reasoning and the answer into separate parts) and the Prompts/Attempts controllers end-to-end
against an in-memory database, without needing Postgres or a real Gemini API key running.

## Deployment

Target is a $0-cost deployment: Next.js on AWS Amplify Hosting, the .NET API on AWS Lambda via
a Function URL, and PostgreSQL on Neon's always-free tier — all within permanently-free usage
tiers rather than a 12-month trial. See [DEPLOYMENT.md](./DEPLOYMENT.md) for the step-by-step
checklist (account setup steps only you can do, plus the exact commands to run once you have
credentials).
