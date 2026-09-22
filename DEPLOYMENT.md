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

## 4. Create an AWS account and an IAM user

**Why:** the backend runs on Lambda and the frontend on Amplify Hosting — both are AWS services,
so everything from here on happens inside an AWS account. This section gets you from "no AWS
account" to "a working, safe-to-use CLI login."

### 4.1 Create the account

1. Go to [aws.amazon.com](https://aws.amazon.com) → **Create an AWS Account**.
2. You'll need an email, a phone number for verification, and — this catches people off guard —
   **a credit/debit card**, even though everything we're doing stays in the free tier. AWS
   requires a card on file for every account, free tier or not. Nothing is charged as long as
   you stay under the free-tier limits described below.
3. Pick the **Basic support plan (free)** when asked.

### 4.2 Set a billing safety net (5 minutes, worth doing before anything else)

Since a card is attached, it's worth a small trip-wire in case something unexpected happens
(e.g. you accidentally leave a non-free-tier resource running):

1. Console → search **"Budgets"** → **AWS Budgets** → **Create budget**.
2. Choose **Zero spend budget** (alerts you the moment you're charged *anything* above $0) — or
   a **Cost budget** set to $1/month if you'd rather set your own threshold.
3. Add your email as an alert recipient.

This is the single best guardrail for a $0-cost project: you'll get an email within a day if
anything ever drifts off the free tier, instead of finding out a month later.

### 4.3 Create an IAM user (don't use the root login day-to-day)

The account's root login can do *anything*, including delete the account — AWS's own advice is
to lock it away (enable MFA on it, then basically never use it again) and instead create a
regular **IAM user** for yourself to work as.

1. Console → **IAM** → **Users** → **Create user**.
2. Name it something like `hamzah-cli`. You don't need console (password) access for this user —
   just check **"Provide user access to the AWS Management Console"** if you'd also like to
   browse the console logged in as this user (recommended, so you're never using root).
3. **Attach permissions**: choose **"Attach policies directly"** and attach the
   **`AdministratorAccess`** managed policy. That's it — one policy, no JSON to write. This is a
   deliberate simplification for a solo personal AWS account: the billing alert from 4.2 is your
   real safety net, and not having to debug an `AccessDenied` error mid-deploy is worth more
   right now than fine-grained permissions. (If you ever want to lock this down later, the two
   AWS-managed policies to look at are `AWSLambda_FullAccess` and `AdministratorAccess-Amplify`
   — but that's an optional future cleanup, not something to figure out now.)
4. **Create access key**: on the user's page → **Security credentials** tab → **Create access
   key** → choose **Command Line Interface (CLI)** as the use case → create. You'll see an
   **Access Key ID** and a **Secret Access Key** — the secret is shown **exactly once**. Copy
   both somewhere safe now (a password manager, not a repo file).

### 4.4 Install and configure the AWS CLI

1. Install it: [AWS CLI install guide](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)
   (Windows: download and run the MSI installer, or `winget install Amazon.AWSCLI`).
2. Configure it with the keys from 4.3:
   ```bash
   aws configure
   ```
   It asks four things:
   - `AWS Access Key ID` → paste the Access Key ID
   - `AWS Secret Access Key` → paste the Secret Access Key
   - `Default region name` → use `us-east-1` (matches the region already set in
     `backend/InterviewLoop.Api/aws-lambda-tools-defaults.json` — keeping everything in one
     region avoids cross-region confusion and is required for a Lambda deploy to "just work"
     with that file's defaults)
   - `Default output format` → `json` (or leave blank)
3. Verify it worked:
   ```bash
   aws sts get-caller-identity
   ```
   Expected output is a small JSON block with your `Account` number, and a `UserId`/`Arn` that
   names the IAM user you just created (not `root`). If this fails, `aws configure` again and
   double-check you copied the keys correctly (no extra whitespace).

Once this returns cleanly, tell me — I can run the actual `aws`/`dotnet lambda` commands with
you from here rather than you copy-pasting everything solo.

## 5. Deploy the backend to Lambda

**What we're building:** the .NET API running as a Lambda function, reachable over plain HTTPS
via a **Function URL** (a built-in Lambda feature — a public HTTPS endpoint with no API Gateway
in front of it). We chose this specifically because Lambda's own free tier (1M requests +
400,000 GB-seconds of compute, per month, **forever**, not a 12-month trial) covers this
portfolio's traffic completely, and skipping API Gateway avoids that service's charges once *its*
free tier expires after 12 months.

### 5.1 Runtime compatibility checkpoint (read this before deploying)

This is the one part of the whole deployment that isn't just "run a command" — it's worth
understanding *why* it might not be a one-liner.

The project targets **.NET 10**. AWS Lambda's *managed runtimes* (the `dotnet8`-style runtime
identifiers you pick when deploying a plain .zip package) have historically lagged behind new
.NET releases by a few months. A .zip deployment package built for net10.0 **will not run** on a
Lambda managed runtime that only understands .NET 8 — the CLR versions aren't interchangeable.

1. Check [AWS's supported Lambda runtimes page](https://docs.aws.amazon.com/lambda/latest/dg/lambda-runtimes.html)
   for whether `dotnet10` (or newer) is listed. There's no clean CLI command that lists this
   (`aws lambda list-runtimes` doesn't exist) — the docs page, or the runtime dropdown when
   manually creating a function in the Lambda console, are the two ways to check.
2. **If `dotnet10` is available:** update `"function-runtime"` in
   `backend/InterviewLoop.Api/aws-lambda-tools-defaults.json` from its current placeholder value
   (`dotnet8`) to `dotnet10`, and use the zip-based deploy in 5.2 below as-is.
3. **If it isn't available yet**, you have three options — tell me which you'd like and I'll
   help implement it (none of these are in the repo yet, so this is a small follow-up task, not
   something to do silently):
   - **Multi-target the project** to also build a `net8.0` output alongside `net10.0`, and deploy
     that build to Lambda. Keeps the managed-runtime deploy path, adds a bit of csproj
     complexity, and the deployed function technically runs on .NET 8 while local dev stays on
     .NET 10.
   - **Switch to a container-image deploy**, using AWS's `public.ecr.aws/lambda/dotnet:10` base
     image, which bundles its own .NET 10 runtime inside the container — this sidesteps Lambda's
     managed-runtime list entirely, so it's not blocked by what AWS has "officially" added yet.
     It needs a `Dockerfile` and pushing the image to **ECR** (Elastic Container Registry).
     ECR's free tier (500MB storage) is only free for 12 months, not forever — but a single
     small ASP.NET Core container image is realistically a few hundred MB, so even after 12
     months the storage cost is a few cents a month, not a real departure from "~$0."
   - **Wait for AWS to add it** — Lambda .NET runtime support usually lands within a few months
     of a .NET release, and `dotnet8` still works fine as an interim deploy target if you're
     okay with option 1 in the meantime.

### 5.2 Deploy the function

```bash
dotnet tool install -g Amazon.Lambda.Tools
cd backend/InterviewLoop.Api
dotnet lambda deploy-function
```

This builds a .zip deployment package from the project, uploads it, and creates a Lambda
function named `interview-loop-api` (from `aws-lambda-tools-defaults.json`). Every Lambda
function also needs an **execution role** — a separate IAM role (not the IAM user you're deployed
as) that lets the function write to CloudWatch Logs. You don't need to create this by hand: the
CLI creates it for you interactively the first time you deploy. You'll see a prompt like:

```
Select IAM Role that to provide AWS credentials to your code:
1) *** Create new IAM Role ***
2) ...
```

1. Choose **`*** Create new IAM Role ***`**.
2. It asks for a role name — type `interview-loop-lambda-role`.
3. It then shows a searchable list of AWS managed policies to attach. Search for and select
   **`AWSLambdaBasicExecutionRole`** — the only one this function needs (it grants exactly
   `logs:CreateLogGroup` / `CreateLogStream` / `PutLogEvents`, since our function doesn't call
   any other AWS service — the database is Neon, the AI calls are to Google).
4. Confirm, and the deploy continues. Future deploys (`dotnet lambda deploy-function` again)
   reuse the same role automatically without asking.

The command finishes with a summary showing the function's ARN. That means the code is deployed
— it isn't reachable over HTTP yet, though, which is what the next step is for.

### 5.3 Expose it over HTTPS with a Function URL

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

What each piece does:
- `create-function-url-config --auth-type NONE`: creates the public HTTPS endpoint itself.
  `NONE` means no AWS-level authentication — anyone with the URL can call it. That's intentional
  for a public portfolio demo (recruiters need to hit it without credentials), and it's exactly
  what the rate limiter we built earlier (`POST /api/attempts`, 1 request/3s per IP) is there
  to protect against abuse of.
- **Two separate `add-permission` calls are required**, not one — this is a commonly-missed
  gotcha, and as of an AWS policy change in October 2025 it's stricter than it used to be:
  without *both* of these, the Function URL returns `403 Forbidden` even with `auth-type NONE`,
  because Lambda's *resource policy* (separate from the Function URL's auth type) must explicitly
  grant both `lambda:InvokeFunctionUrl` (permission to hit the URL at all) and
  `lambda:InvokeFunction` (permission to actually invoke the function code) to the public
  principal (`*`). Missing either one produces the exact same 403, so if you still get one after
  running both commands, double check neither was skipped (`aws lambda get-policy --function-name
  interview-loop-api` shows both statements when it's set up correctly).

The first command's output includes a `FunctionUrl` field — that's your backend's public base
URL, e.g. `https://abc123xyz.lambda-url.us-east-1.on.aws/`. Test it directly before touching the
frontend:
```bash
curl https://<your-function-url>/api/prompts
```
You should get the same JSON array of 10 seeded prompts you saw locally.

### 5.4 Set environment variables

Console path: Lambda function → **Configuration** tab → **Environment variables** → **Edit**.
CLI equivalent:
```bash
aws lambda update-function-configuration \
  --function-name interview-loop-api \
  --environment "Variables={ConnectionStrings__Postgres='<neon-connection-string>',Gemini__ApiKey='<your-gemini-key>',Gemini__Model='gemini-3.5-flash-lite',Cors__AllowedOrigins__0='<amplify-url-from-step-6>'}"
```

Why the double underscores (`__`) instead of `:`: ASP.NET Core's environment-variable
configuration provider uses `__` as the section separator, because `:` isn't valid inside an
environment variable name on most shells/OSes. `ConnectionStrings__Postgres` is the environment
equivalent of `appsettings.json`'s `"ConnectionStrings": { "Postgres": "..." }`.

- `ConnectionStrings__Postgres` — the same Neon connection string you already validated locally.
- `Gemini__ApiKey` — same Gemini key.
- `Gemini__Model` — `gemini-3.5-flash-lite`.
- `Cors__AllowedOrigins__0` — you won't know this until step 6 is done; come back and set it
  then (step 7 covers exactly this).

Environment variable changes take effect on the *next* invocation automatically — no redeploy of
the code package needed.

### 5.5 Troubleshooting

- **`Task timed out after 30.00 seconds`** in CloudWatch Logs — our Gemini grading service
  retries transient errors with backoff (up to ~2.6s of delay across 2 retries) plus normal
  model response time; 30s should be comfortable headroom, but if you see this, bump
  `"function-timeout"` in `aws-lambda-tools-defaults.json` and redeploy.
- **Logs**: Lambda console → your function → **Monitor** tab → **View CloudWatch logs**. This is
  where every `logger.LogError(...)` call in the backend ends up — the same detail that shows up
  in the `Details` field of an error response, plus anything that doesn't reach the HTTP
  response at all (e.g. a cold-start failure).
- **Cold starts**: the first request after a period of inactivity takes noticeably longer
  (initializing the .NET runtime + EF Core + running `Database.Migrate()` against Neon). This is
  normal for Lambda + a "sleeping" serverless database and not something to chase down.

## 6. Deploy the frontend to Amplify

**What we're building:** Amplify Hosting is AWS's managed build-and-host service for frontend
frameworks — it watches your GitHub repo, runs the build (`amplify.yml`, already in this repo),
and serves the result over HTTPS with a free `*.amplifyapp.com` subdomain. It supports Next.js's
server-rendered routes (not just static export), which matters here since `/prompts/[id]` and
`/history/[id]` are dynamic.

1. Console → **Amplify** → **Create new app** (or **Host web app**).
2. Choose **GitHub** as the source, and authorize AWS to access your GitHub account when
   prompted — you can scope this to just the `interview-loop` repo during the GitHub
   authorization step rather than granting access to all your repos.
3. Select the `interview-loop` repo and the `master` branch.
4. **This repo is a monorepo** (backend + frontend in one repo) — Amplify needs to know the
   frontend lives in a subdirectory. When prompted, choose the **monorepo** option and set the
   app root to `frontend`. If Amplify's build settings screen doesn't show it detecting
   `amplify.yml` automatically, you can paste its contents directly into the build settings
   editor on this screen (the file's already correct for this exact setup — see `amplify.yml` at
   the repo root).
5. **Environment variables** (same screen, or App settings → Environment variables afterward):
   add `NEXT_PUBLIC_API_URL` = `<your Lambda Function URL from step 5.3>/api` (include the
   trailing `/api` — that's the base path every API call in the frontend is built on top of).
6. Review and deploy. Amplify runs through **Provision → Build → Deploy → Verify** — for a
   Next.js app this size, expect roughly 3-5 minutes. Click into any failed stage to see its
   full log if something goes wrong (most first-time failures are a missing environment variable
   or a build command that doesn't match `amplify.yml`).
7. Once it's green, Amplify gives you a URL like `https://master.d1a2b3c4d5e6f7.amplifyapp.com`
   — that's your live frontend.

A custom domain is possible later (Amplify → Domain management) but isn't required — the free
`amplifyapp.com` subdomain is perfectly shareable with recruiters.

## 7. Close the loop on CORS

**Why this step exists:** the backend's CORS policy (`Program.cs`) only allows requests from
origins explicitly listed in `Cors:AllowedOrigins` — this is what stops a random website from
calling your API from a browser. Locally that's `http://localhost:3000`; in production it needs
to be your real Amplify URL, which you only found out in step 6.

Go back to the Lambda's environment variables (5.4) and set:
```
Cors__AllowedOrigins__0 = https://master.d1a2b3c4d5e6f7.amplifyapp.com
```
(your actual Amplify URL from step 6, no trailing slash). As in 5.4, this takes effect on the
next invocation automatically — no code redeploy needed, just the environment variable update.

## 8. Smoke test

Open the Amplify URL and walk through the full feature set we built:

- [ ] Home page loads the 10 seeded prompts (confirms frontend → Lambda → Neon is wired end to
      end).
- [ ] Open a prompt, submit a solution, and get back **real** Gemini feedback — not the
      "[Demo mode]" placeholder text (confirms the `Gemini__ApiKey` env var made it through).
- [ ] Click **Reset** — the editor reverts to the starter code.
- [ ] Click **Reformat** on a JS/TS prompt — Monaco reformats the code.
- [ ] `/history` shows the attempt you just submitted, and clicking into it shows the full
      code + feedback.
- [ ] Submit twice in quick succession — the Submit button should disable itself for ~3s; if you
      bypass that (e.g. via curl) you should get a clean `429` with a friendly message, not a
      raw error dump.
- [ ] Temporarily break something (e.g. stop the Lambda function, or submit with the network
      tab throttled to "Offline") and confirm a toast appears bottom-right with a readable
      message, not a blank screen or console-only error.

That's the whole $0-cost pipeline validated end-to-end — this is the state you'd share with a
recruiter.

## 9. Rotating credentials

If you ever need to replace the Gemini key or Neon connection string (a leak, a routine
rotation, whatever), the source of truth locally is your `dotnet user-secrets` store, and the
source of truth in production is the Lambda's environment variables. Update both, in this order:

### 9.1 Get the new value into local user-secrets first

Same commands as the original setup (§2 for Gemini, §3 for Neon) — `dotnet user-secrets set
"Gemini:ApiKey" "<new-key>"` or the connection-string conversion script from §3. Then run
`dotnet run` locally and confirm it starts cleanly before touching production — it's much
cheaper to catch a typo locally than on the live site.

### 9.2 Push the new value(s) to Lambda

**Important:** `aws lambda update-function-configuration --environment` *replaces the entire
variable set* — it doesn't merge. If you only pass the one variable you're rotating, you'll wipe
out the other three. Always read the current set first, merge in the new value, then write the
whole merged set back.

Run this from `backend/InterviewLoop.Api` (adjust the `UserSecretsId` path if yours differs — it's
the `<UserSecretsId>` value in `InterviewLoop.Api.csproj`):

```powershell
# 1. Make sure the AWS CLI is reachable in this shell
aws --version
# If that fails with "not recognized", the CLI is installed but not on this shell's PATH yet -
# find it and add it for this session:
#   $env:PATH += ";C:\Users\<you>\AppData\Local\Programs\Amazon\AWSCLIV2\"

# 2. Read your local secrets (never printed - only assigned to a variable)
$secretsPath = "$env:APPDATA\Microsoft\UserSecrets\<your-UserSecretsId>\secrets.json"
$secrets = Get-Content $secretsPath -Raw | ConvertFrom-Json

# 3. Read the CURRENT Lambda env vars (also never printed - piped straight into a variable)
$currentVars = (aws lambda get-function-configuration --function-name interview-loop-api `
  --query "Environment.Variables" --output json | ConvertFrom-Json)

# 4. Merge: start from what's live, overwrite with whatever changed locally
$merged = @{}
$currentVars.PSObject.Properties | ForEach-Object { $merged[$_.Name] = $_.Value }
$merged["Gemini__ApiKey"] = $secrets.'Gemini:ApiKey'
$merged["ConnectionStrings__Postgres"] = $secrets.'ConnectionStrings:Postgres'
# (leave Gemini__Model and Cors__AllowedOrigins__0 untouched - already in $merged from step 4)

# 5. Write to a BOM-free temp file (a UTF-8 BOM breaks the AWS CLI's JSON parser and - worse -
#    makes it echo the whole file, secrets included, into the error message)
$envConfig = @{ Variables = $merged } | ConvertTo-Json -Compress
$tempFile = "$env:TEMP\lambda-env-config.json"
[System.IO.File]::WriteAllText($tempFile, $envConfig, [System.Text.UTF8Encoding]::new($false))

# 6. Push it, suppressing output (so a failure can't dump secrets to your terminal either)
$null = aws lambda update-function-configuration --function-name interview-loop-api `
  --environment "file://$tempFile" 2>$null
$exitCode = $LASTEXITCODE
Remove-Item $tempFile -Force
if ($exitCode -eq 0) { Write-Output "SUCCESS" } else { Write-Output "FAILED exit code $exitCode" }
```

### 9.3 Verify without exposing the values

```powershell
aws lambda get-function-configuration --function-name interview-loop-api --query "keys(Environment.Variables)"
```
This should print the four variable **names** (`Gemini__ApiKey`, `ConnectionStrings__Postgres`,
`Gemini__Model`, `Cors__AllowedOrigins__0`) — never their values. Deliberately avoid running
`get-function-configuration` without a `--query "keys(...)"` filter, since the unfiltered output
includes the actual values in plaintext.

Changes take effect on the next invocation automatically — no code redeploy needed. Do a real
smoke test against the live URL afterward (submit an attempt, confirm real Gemini feedback) to
confirm the rotated credentials actually work end to end.

## 10. Tearing it down (if you ever want to)

Since this is a portfolio project rather than something with ongoing users, it's fine to leave
it running indefinitely (it's genuinely $0 at this traffic level) — but if you ever want to fully
remove it:

1. Amplify console → your app → **Actions** → **Delete app**.
2. Lambda console → your function → **Actions** → **Delete function** (this also removes its
   Function URL).
3. IAM console → delete the `interview-loop-lambda-role` role (Roles) and, if you're done with
   AWS entirely, the IAM user you created in 4.3.
4. Neon console → delete the project.
5. [Google AI Studio](https://aistudio.google.com/) → revoke/delete the Gemini API key.

---

Once this is live, later ideas (not required): wire a GitHub Actions deploy job using OIDC to
assume an AWS role (avoids storing long-lived AWS keys as GitHub secrets), or add Sentry for
error monitoring per the resume's stack.
