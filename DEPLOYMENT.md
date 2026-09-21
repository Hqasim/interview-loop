# Deployment checklist

This is the $0-cost deployment target from the project plan: Next.js on **AWS Amplify Hosting**,
the .NET API on **AWS Lambda** behind a **Function URL**, and Postgres on **Neon**'s always-free
tier. The steps below need your own accounts/credentials, so they're written as a checklist for
you — ping me at any step and I'll run the actual commands with you once you've got the
credentials in hand.

## 1. Push to GitHub

You're doing this part yourself — create `github.com/Hqasim/interview-loop` and push the existing
`master` branch.

## 2. Get a Gemini API key (free tier)

1. Go to [Google AI Studio](https://aistudio.google.com/) and create an API key (no billing
   required for the free tier).
2. Test it locally first:
   ```bash
   cd backend/InterviewLoop.Api
   dotnet user-secrets init
   dotnet user-secrets set "Gemini:ApiKey" "<your-key>"
   dotnet run
   ```
   Submit an attempt and confirm you get real feedback instead of the "[Demo mode]" placeholder.

## 3. Create a Neon Postgres project (always-free tier)

1. Sign up at [neon.tech](https://neon.tech), create a project.
2. Copy the connection string it gives you (starts with `postgresql://...`) and convert it to a
   .NET/Npgsql-style connection string, e.g.:
   ```
   Host=<your-host>.neon.tech;Port=5432;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
   ```
3. Point the backend at it and confirm migrations apply cleanly against the real cloud DB:
   ```bash
   dotnet user-secrets set "ConnectionStrings:Postgres" "<neon-connection-string>"
   dotnet run
   ```
   (Startup runs `db.Database.Migrate()` automatically — watch the log for `Applying migration '...InitialCreate'`.)

## 4. Create an AWS account (free tier)

1. Sign up at [aws.amazon.com](https://aws.amazon.com) if you don't already have an account.
2. Create an IAM user for yourself with programmatic access (don't use the root account for
   day-to-day deploys), and install/configure the AWS CLI:
   ```bash
   aws configure
   ```
3. Once `aws sts get-caller-identity` works from your machine, tell me — I can run the actual
   `aws`/`dotnet lambda` deploy commands here rather than you copy-pasting them.

## 5. Deploy the backend to Lambda

**Important checkpoint before this step:** this project currently targets **.NET 10**. AWS Lambda's
managed .NET runtimes have historically lagged a bit behind each .NET release. Check
[AWS's supported Lambda runtimes](https://docs.aws.amazon.com/lambda/latest/dg/lambda-runtimes.html)
before deploying:
- If a `dotnet10` (or newer) managed runtime is listed, update `function-runtime` in
  `backend/InterviewLoop.Api/aws-lambda-tools-defaults.json` (currently a placeholder value of
  `dotnet8`) and deploy via the zip-based path below.
- If not yet available, tell me and we'll switch to a container-image deploy instead (using AWS's
  `public.ecr.aws/lambda/dotnet` base image, which tends to pick up new .NET versions faster) —
  it's a small code change, not a redesign.

Once the runtime is confirmed:

```bash
dotnet tool install -g Amazon.Lambda.Tools
cd backend/InterviewLoop.Api
dotnet lambda deploy-function
```

Then create a Function URL for it (public, no IAM auth, since this is a public portfolio demo):

```bash
aws lambda create-function-url-config \
  --function-name interview-loop-api \
  --auth-type NONE

aws lambda add-permission \
  --function-name interview-loop-api \
  --action lambda:InvokeFunctionUrl \
  --principal "*" \
  --function-url-auth-type NONE \
  --statement-id FunctionURLAllowPublicAccess
```

Set the Lambda's environment variables (console or `aws lambda update-function-configuration
--environment`) to mirror what you tested locally in step 2 and 3:

- `ConnectionStrings__Postgres` = your Neon connection string
- `Gemini__ApiKey` = your Gemini key
- `Gemini__Model` = `gemini-2.5-flash` (or whatever you tested with)
- `Cors__AllowedOrigins__0` = your Amplify frontend URL (add this after step 6, once you know it)

## 6. Deploy the frontend to Amplify

1. In the AWS Amplify console, connect your GitHub repo.
2. When prompted, choose the monorepo option and set the app root to `frontend` (the repo's
   `amplify.yml` already has the right build spec for this).
3. Add an environment variable: `NEXT_PUBLIC_API_URL` = `<your Lambda Function URL>/api`.
4. Deploy, then note the Amplify-provided URL (e.g. `https://main.xxxxx.amplifyapp.com`).

## 7. Close the loop on CORS

Go back to the Lambda's environment variables and set `Cors__AllowedOrigins__0` to the Amplify
URL from step 6, then redeploy/update the function config so the new env var takes effect.

## 8. Smoke test

Open the Amplify URL, solve a prompt, confirm you get real Gemini feedback (not demo mode), and
check `/history` shows the attempt. That's the whole $0-cost pipeline validated end-to-end.

---

Once this is live, later ideas (not required): wire a GitHub Actions deploy job using OIDC to
assume an AWS role (avoids storing long-lived AWS keys as GitHub secrets), or add Sentry for
error monitoring per the resume's stack.
