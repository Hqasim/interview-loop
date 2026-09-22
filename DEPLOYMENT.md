# Deployment Guide

InterviewLoop deploys at zero infrastructure cost: Next.js on AWS Amplify Hosting, the .NET API
on AWS Lambda behind a Function URL, PostgreSQL on Neon's always-free tier, and Google Gemini for
AI grading. See the [README](./README.md#architecture) for the architecture diagram.

**Prerequisites**

| Requirement | Used for |
|---|---|
| GitHub account | Source repo, Amplify's build trigger |
| [Google AI Studio](https://aistudio.google.com/) account | Gemini API key |
| [Neon](https://neon.tech) account | Managed PostgreSQL |
| AWS account | Lambda + Amplify Hosting |
| AWS CLI v2 | Deploying and configuring the backend |
| .NET 10 SDK + [Amazon.Lambda.Tools](https://github.com/aws/aws-extensions-for-dotnet-cli) | Building and deploying the Lambda package |
| Docker | Local PostgreSQL only |

## 1. Push to GitHub

Push the repository to GitHub. Amplify Hosting (§6) deploys directly from this remote.

## 2. Gemini API key

1. Create an API key at [Google AI Studio](https://aistudio.google.com/apikey) (no billing
   required on the free tier).
2. Set it locally and confirm real grading works before deploying:
   ```bash
   cd backend/InterviewLoop.Api
   dotnet user-secrets init
   dotnet user-secrets set "Gemini:ApiKey" "<your-key>"
   dotnet run
   ```
   Submit an attempt and confirm the response is real AI feedback, not the `[Demo mode]`
   placeholder the app falls back to when no key is configured.

## 3. Neon PostgreSQL

1. Create a project at [neon.tech](https://neon.tech).
2. Neon issues a connection string in libpq URI form:
   ```
   postgresql://<user>:<password>@<host>/<database>?sslmode=require&channel_binding=require
   ```
   Npgsql (the .NET driver used here) requires the equivalent key/value form:
   ```
   Host=<host>;Port=5432;Database=<database>;Username=<user>;Password=<password>;SSL Mode=Require;Trust Server Certificate=true
   ```
   If the password contains percent-encoded characters (e.g. `%40` for `@`), decode them —
   Npgsql expects the literal character, not the URI encoding.
3. Set the connection string and confirm migrations apply cleanly against the real database:
   ```bash
   dotnet user-secrets set "ConnectionStrings:Postgres" "<converted-connection-string>"
   dotnet run
   ```
   Startup runs `Database.Migrate()` automatically — the log shows `Applying migration
   '...InitialCreate'` on first run against a fresh database, or `already up to date` on
   subsequent runs.

## 4. AWS account and IAM user

1. Create an AWS account at [aws.amazon.com](https://aws.amazon.com). A payment method is
   required even for free-tier-only usage; nothing is charged while usage stays within the free
   tier. Consider setting a [Budgets zero-spend alert](https://console.aws.amazon.com/billing/home#/budgets)
   as a safety net.
2. Create an IAM user for CLI/deploy use rather than using the root account:
   IAM → Users → Create user → attach the managed policy `AdministratorAccess`. (For tighter
   scoping later, `AWSLambda_FullAccess` and `AdministratorAccess-Amplify` cover everything this
   guide uses.)
3. Generate an access key: the user's page → Security credentials → Create access key →
   Command Line Interface (CLI).
4. Install the [AWS CLI](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)
   and configure it:
   ```bash
   aws configure
   ```
   Use region `us-east-1` — it matches the region pinned in
   `backend/InterviewLoop.Api/aws-lambda-tools-defaults.json`.
5. Verify:
   ```bash
   aws sts get-caller-identity
   ```

## 5. Deploy the backend to Lambda

The API runs as a Lambda function behind a **Function URL** — a built-in public HTTPS endpoint,
with no API Gateway in front of it. Lambda's free tier (1M requests + 400,000 GB-seconds/month)
is permanent, not a 12-month trial, and skipping API Gateway avoids that service's cost once its
own free tier expires.

### 5.1 Runtime compatibility

The project targets .NET 10. Confirm a `dotnet10` managed Lambda runtime is listed on
[AWS's supported runtimes page](https://docs.aws.amazon.com/lambda/latest/dg/lambda-runtimes.html)
before deploying — a package built for net10.0 will not run on an older managed runtime. If
`dotnet10` isn't available, either multi-target the project to also build `net8.0` for deployment,
or switch to a container-image deploy using the `public.ecr.aws/lambda/dotnet:10` base image.

`backend/InterviewLoop.Api/aws-lambda-tools-defaults.json` already specifies `"function-runtime":
"dotnet10"`.

### 5.2 Deploy the function

```bash
dotnet tool install -g Amazon.Lambda.Tools
cd backend/InterviewLoop.Api
dotnet lambda deploy-function
```

This builds a deployment package and creates a function named `interview-loop-api`. On first
deploy, if no execution role is configured, the CLI prompts interactively:

```
Select IAM Role that to provide AWS credentials to your code:
1) *** Create new IAM Role ***
2) ...
```

Choose **Create new IAM Role**, name it `interview-loop-lambda-role`, and attach the managed
policy `AWSLambdaBasicExecutionRole` (grants CloudWatch Logs write access — the only permission
this function needs, since the database is external to AWS and the AI calls go to Google).
Subsequent deploys reuse the same role automatically. To skip the prompt entirely, add
`"function-role": "<role-arn>"` to `aws-lambda-tools-defaults.json`.

### 5.3 Expose it over HTTPS

```bash
aws lambda create-function-url-config \
  --function-name interview-loop-api \
  --auth-type NONE

aws lambda add-permission \
  --function-name interview-loop-api \
  --statement-id FunctionURLAllowPublicAccess \
  --action lambda:InvokeFunctionUrl \
  --principal "*" \
  --function-url-auth-type NONE

aws lambda add-permission \
  --function-name interview-loop-api \
  --statement-id FunctionURLInvokeAllowPublicAccess \
  --action lambda:InvokeFunction \
  --principal "*" \
  --invoked-via-function-url
```

`auth-type NONE` makes the endpoint publicly callable, which the rate limiter on
`POST /api/attempts` (1 request / 3s / IP) is in place to protect. Both `add-permission` calls
are required: since an AWS policy change in October 2025, a Function URL needs grants for both
`lambda:InvokeFunctionUrl` and `lambda:InvokeFunction`, or every call returns `403 Forbidden`
regardless of auth type. Verify both are present with `aws lambda get-policy --function-name
interview-loop-api`.

The first command's output includes `FunctionUrl`. Verify it directly:
```bash
curl https://<function-url>/api/prompts
```

### 5.4 Environment variables

```bash
aws lambda update-function-configuration \
  --function-name interview-loop-api \
  --environment "Variables={ConnectionStrings__Postgres='<neon-connection-string>',Gemini__ApiKey='<gemini-key>',Gemini__Model='gemini-3.5-flash-lite',Cors__AllowedOrigins__0='<amplify-url>'}"
```

ASP.NET Core's configuration provider maps `__` to the `:` section separator used in
`appsettings.json`, since `:` isn't valid in most shells. `Cors__AllowedOrigins__0` isn't known
until the frontend is deployed (§6) — set it in §7. Environment variable changes take effect on
the next invocation with no redeploy required.

### 5.5 Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `Task timed out after 30.00 seconds` | Gemini grading retries transient errors with backoff (up to ~2.6s across two retries). If this recurs, increase `function-timeout` in `aws-lambda-tools-defaults.json` and redeploy. |
| Missing detail in error responses | Check CloudWatch Logs (function → Monitor → View logs) — every server-side error is logged there, including the technical detail behind a user-facing message. |
| First request after idle time is slow | Expected cold start: .NET runtime init, EF Core, and `Database.Migrate()` against a Neon instance that may itself be resuming from idle. |

## 6. Deploy the frontend to Amplify

1. Amplify console → Create new app → Host web app → GitHub → authorize and select this
   repository and its default branch.
2. This repository is a monorepo. When prompted, set the app root to `frontend`. Amplify reads
   build settings from `amplify.yml` at the repository root.
3. Add environment variable `NEXT_PUBLIC_API_URL` = `<lambda-function-url>/api`.
4. Deploy. Amplify runs Provision → Build → Deploy → Verify. On success it issues a URL of the
   form `https://<branch>.<app-id>.amplifyapp.com`.

## 7. Configure CORS

The backend's CORS policy only allows requests from origins listed in `Cors:AllowedOrigins`.
Set the Lambda environment variable from §5.4:
```
Cors__AllowedOrigins__0 = https://<branch>.<app-id>.amplifyapp.com
```
No trailing slash. Takes effect immediately, no redeploy required.

## 8. Verify the deployment

- Home page loads the seeded prompt list (frontend → Lambda → Neon).
- Submitting a solution returns real Gemini feedback, not demo-mode placeholder text.
- Reset restores starter code; Reformat runs Monaco's formatter on JS/TS prompts.
- History lists a submitted attempt and its detail view renders correctly.
- Two rapid submissions trigger the client-side cooldown; bypassing it directly (e.g. via curl)
  returns `429` with a readable message.
- A simulated failure (stopped backend, offline network) surfaces a toast notification rather
  than a blank screen.

## 9. Rotating credentials

The Gemini key and Neon connection string live in local `dotnet user-secrets` (development) and
Lambda environment variables (production). Update both on rotation:

1. Set the new value locally (`dotnet user-secrets set ...`) and confirm the app runs correctly
   against it before touching production.
2. Push it to Lambda. `update-function-configuration --environment` replaces the entire variable
   set rather than merging, so read the current set first:

   ```powershell
   $secrets = Get-Content "$env:APPDATA\Microsoft\UserSecrets\<UserSecretsId>\secrets.json" -Raw | ConvertFrom-Json
   $current = (aws lambda get-function-configuration --function-name interview-loop-api `
     --query "Environment.Variables" --output json | ConvertFrom-Json)

   $merged = @{}
   $current.PSObject.Properties | ForEach-Object { $merged[$_.Name] = $_.Value }
   $merged["Gemini__ApiKey"] = $secrets.'Gemini:ApiKey'
   $merged["ConnectionStrings__Postgres"] = $secrets.'ConnectionStrings:Postgres'

   $tempFile = "$env:TEMP\lambda-env-config.json"
   [System.IO.File]::WriteAllText($tempFile, (@{ Variables = $merged } | ConvertTo-Json -Compress), [System.Text.UTF8Encoding]::new($false))

   aws lambda update-function-configuration --function-name interview-loop-api --environment "file://$tempFile" | Out-Null
   Remove-Item $tempFile -Force
   ```

   The config is written as UTF-8 without a byte-order mark and passed via `file://` rather than
   inline shorthand syntax — a BOM breaks the AWS CLI's JSON parser, and inline shorthand breaks
   on special characters that commonly appear in generated passwords.

3. Confirm without exposing values:
   ```bash
   aws lambda get-function-configuration --function-name interview-loop-api --query "keys(Environment.Variables)"
   ```
   This returns variable names only. Running `get-function-configuration` without a `--query`
   filter prints values in plaintext — avoid it.
4. Smoke test the live URL to confirm the rotated credentials work end to end.

## 10. Tearing down

1. Amplify console → app → Actions → Delete app.
2. Lambda console → function → Actions → Delete function (also removes its Function URL).
3. IAM → delete the `interview-loop-lambda-role` role and, if applicable, the deploy user.
4. Neon console → delete the project.
5. Google AI Studio → revoke the Gemini API key.

## Future improvements

- Automated CD: a GitHub Actions job using OIDC to assume an AWS role and deploy on merge to
  `main`, replacing the manual deploy commands above.
- Sentry for error monitoring and structured logging.
- A custom domain in front of the Amplify URL.
