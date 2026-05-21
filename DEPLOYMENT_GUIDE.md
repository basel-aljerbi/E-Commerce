# E-Commerce Production Deployment Guide

> Full production deployment: Docker + Caddy HTTPS + SQL Server + CI/CD

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Phase 1: Provision VPS](#2-phase-1-provision-vps)
3. [Phase 2: Server Setup Script](#3-phase-2-server-setup-script)
4. [Phase 3: DNS Configuration](#4-phase-3-dns-configuration)
5. [Phase 4: Deploy Application Files](#5-phase-4-deploy-application-files)
6. [Phase 5: Configure Environment Variables](#6-phase-5-configure-environment-variables)
7. [Phase 6: Start Containers](#7-phase-6-start-containers)
8. [Phase 7: Create Admin Account](#8-phase-7-create-admin-account)
9. [Phase 8: Stripe Webhook Setup](#9-phase-8-stripe-webhook-setup)
10. [Phase 9: SendGrid Configuration](#10-phase-9-sendgrid-configuration)
11. [Phase 10: GitHub Actions CI/CD](#11-phase-10-github-actions-cicd)
12. [Verification Checklist](#12-verification-checklist)
13. [Troubleshooting](#13-troubleshooting)
14. [Caveats & Known Issues](#14-caveats--known-issues)

---

## 1. Prerequisites

Before starting, you need:

| Item | Details |
|------|---------|
| **Domain** | Two subdomains: `shop.example.com` + `api.shop.example.com` |
| **VPS** | Ubuntu 24.04, ≥2GB RAM, ≥20GB SSD, public IP |
| **Stripe account** | API keys (test or live) from https://dashboard.stripe.com/apikeys |
| **SendGrid account** | API key from https://app.sendgrid.com/settings/api_keys |
| **GitHub repo** | Your project pushed to GitHub |
| **Local machine** | Git, the project files, SSH access to VPS |

---

## 2. Phase 1: Provision VPS

### Option A: DigitalOcean

```bash
# Create a droplet via CLI or UI
# - Ubuntu 24.04 LTS
# - Basic plan, $12-24/month (2GB RAM, 1-2 CPUs)
# - Add your SSH key
# - Enable monitoring
```

### Option B: Hetzner (cheapest)

```bash
# CX22 or CX32: ~€4-8/month
# Ubuntu 24.04, 2-4GB RAM
```

### Option C: Any provider

Any Ubuntu 24.04 VPS with a public IP works. Note the IP address after provisioning.

---

## 3. Phase 2: Server Setup Script

SSH into your VPS and run the one-time setup:

```bash
# SSH into your VPS (replace with your IP and user)
ssh root@YOUR_SERVER_IP

# The root user works, or create a deploy user:
# adduser deploy && usermod -aG sudo deploy
# Then continue as deploy
```

### Copy setup script to VPS

From your **local machine** (where the project files are):

```bash
# Using SCP (on Windows, run this from PowerShell or Git Bash)
scp deploy/setup-server.sh deploy-user@YOUR_SERVER_IP:/home/deploy-user/
scp deploy/setup-server.sh root@YOUR_SERVER_IP:/root/
```

### Run the setup script on the VPS

```bash
# On the VPS (as root or with sudo)
chmod +x /root/setup-server.sh
./root/setup-server.sh

# Log out and back in to activate docker group
exit
ssh deploy-user@YOUR_SERVER_IP
```

### Verify installation

```bash
docker --version          # Should show Docker version 27+
docker compose version    # Should show Docker Compose v2+
sudo ufw status verbose   # Should show: 80, 443, 22 allowed
```

---

## 4. Phase 3: DNS Configuration

Point your domains to the VPS IP address. Add these DNS records at your domain registrar:

| Record Type | Name | Value | TTL |
|-------------|------|-------|-----|
| `A` | `shop` | `YOUR_VPS_IP` | 300 (5 min) |
| `CNAME` | `api` | `shop.example.com` | 300 (5 min) |

> **Note:** DNS propagation can take from minutes to hours. Use `dig shop.example.com` or `nslookup shop.example.com` to check.

---

## 5. Phase 4: Deploy Application Files

From your **local machine**, copy the deployment files to the VPS:

```bash
# Create the deployment directory on VPS
ssh deploy-user@YOUR_SERVER_IP "mkdir -p /opt/ecommerce"

# Copy all deployment files
scp deploy/docker-compose.yml deploy-user@YOUR_SERVER_IP:/opt/ecommerce/
scp deploy/Caddyfile deploy-user@YOUR_SERVER_IP:/opt/ecommerce/
scp deploy/.env.production deploy-user@YOUR_SERVER_IP:/opt/ecommerce/
scp deploy/deploy.sh deploy-user@YOUR_SERVER_IP:/opt/ecommerce/

# Also copy the frontend nginx config (used by frontend container)
scp frontend/nginx.conf deploy-user@YOUR_SERVER_IP:/opt/ecommerce/

# Make the deploy script executable
ssh deploy-user@YOUR_SERVER_IP "chmod +x /opt/ecommerce/deploy.sh"
```

---

## 6. Phase 5: Configure Environment Variables

SSH into the VPS and edit the environment file:

```bash
ssh deploy-user@YOUR_SERVER_IP
cd /opt/ecommerce
nano .env.production
```

### Required fields — fill these with real values:

```ini
# === IMAGE REGISTRY ===
# Format: ghcr.io/YOUR_GITHUB_USERNAME_OR_ORG/ecommerce
IMAGE_REGISTRY=ghcr.io/your-github-username/ecommerce
IMAGE_TAG=latest

# === SQL SERVER ===
SA_PASSWORD=YourSuperStrongPassword123!

# === JWT (CRITICAL — generate this) ===
# Run on your local machine: openssl rand -hex 32
# Or use PowerShell: -join ((48..57)+(65..90)+(97..122) | Get-Random -Count 32 | % {[char]$_})
Jwt__Key=REPLACE_WITH_64_CHAR_HEX_STRING

# === STRIPE ===
# Get from https://dashboard.stripe.com/test/apikeys (test) or live dashboard
Stripe__SecretKey=stripe_secret_key_live_placeholder
Stripe__PublishableKey=stripe_publishable_key_live_placeholder
Stripe__WebhookSecret=stripe_webhook_secret_placeholder

# === SENDGRID ===
SendGrid__ApiKey=sendgrid_api_key_placeholder
SendGrid__FromEmail=noreply@yourdomain.com
SendGrid__FromName=Your Store Name

# === CORS & DOMAINS ===
Cors__AllowedOrigins__0=https://shop.example.com
FrontendUrl=https://shop.example.com

# === FRONTEND API URL ===
VITE_STRIPE_PUBLISHABLE_KEY=stripe_publishable_key_live_placeholder
```

> **Important:** The `Jwt__Key` must be at least 32 characters (256 bits). The same key is used to sign ALL tokens — changing it invalidates existing tokens.

> **Note:** The `.env.production` file uses bash variable expansion (`${SA_PASSWORD}`) in the `ConnectionStrings__ECommerce` value. Docker Compose's `env_file` directive does NOT expand variables by default. You must either:
> - Write the full connection string directly replacing `${SA_PASSWORD}` with the actual password
> - Or use `docker compose --env-file .env.production config` to verify expansion

### Fix the connection string:

Edit the `ConnectionStrings__ECommerce` line in `.env.production` to use the ACTUAL password instead of `${SA_PASSWORD}`:

```ini
ConnectionStrings__ECommerce=Server=sqlserver,1433;Database=ECommerce;User Id=sa;Password=YourSuperStrongPassword123!;TrustServerCertificate=true;Encrypt=false;
```

---

## 7. Phase 6: Start Containers

### First, configure the REQUIRED code change:

On your local machine, open `E-Commerce.API/Program.cs` and add ONE line. Find line 276 (`app.UseCorrelation();`) and add after it:

```csharp
app.UseForwardedHeaders();
```

Also add this using at the top of the file (line ~5):

```csharp
using Microsoft.AspNetCore.HttpOverrides;
```

This is **required** to prevent an infinite HTTPS redirect loop when Caddy proxies traffic to the backend over HTTP. Commit and push this change.

### Build and push Docker images to GitHub Container Registry

From your **local machine**:

```bash
# Login to GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin

# Build and push backend
docker build -t ghcr.io/YOUR_USERNAME/ecommerce/ecommerce-backend:latest ./E-Commerce.API
docker push ghcr.io/YOUR_USERNAME/ecommerce/ecommerce-backend:latest

# Build and push frontend
docker build \
  --build-arg VITE_API_URL=https://api.shop.example.com \
  -t ghcr.io/YOUR_USERNAME/ecommerce/ecommerce-frontend:latest \
  ./frontend
docker push ghcr.io/YOUR_USERNAME/ecommerce/ecommerce-frontend:latest
```

> **Or skip this step** and let GitHub Actions handle it (see Phase 10). For initial manual deployment, building locally works.

### Start containers on VPS

```bash
# On the VPS
cd /opt/ecommerce

# Pull the images (if using registry) OR build locally
docker compose pull

# Start all services
docker compose up -d

# Check all containers are running
docker compose ps

# Watch startup logs
docker compose logs --tail=50 -f backend

# Wait for SQL Server to be healthy (60-90s first time)
docker compose logs --tail=20 sqlserver

# Once backend starts, it runs migrations automatically
# due to the --migrate flag in the Dockerfile ENTRYPOINT
```

### Verify migration ran

```bash
docker compose logs backend | grep -i migrate
# Should show: "Applying migration '...'"
# or "No migrations to apply"
```

---

## 8. Phase 7: Create Admin Account

In Production mode, `SeedDataAsync()` does NOT run automatically. You need to create an admin account manually.

### Option A: Promote user via SQL (recommended)

```bash
# 1. Register a user via the frontend at https://shop.example.com/register
# 2. Then promote to Admin in SQL Server:

docker exec -it ecommerce-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourSuperStrongPassword123!" -C \
  -Q "UPDATE [ECommerce].[dbo].[Users] SET [Role]='Admin', [IsEmailVerified]=1 WHERE [Email]='your-registered-email@example.com'"

# 3. Verify:
docker exec -it ecommerce-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourSuperStrongPassword123!" -C \
  -Q "SELECT Email, Role, IsEmailVerified FROM [ECommerce].[dbo].[Users]"
```

### Option B: Use Development mode (quick start)

```bash
# 1. Stop the production backend
docker compose stop backend

# 2. Start a temporary dev instance
docker compose run --rm -e ASPNETCORE_ENVIRONMENT=Development backend

# 3. In another terminal, seed the admin:
curl -X POST https://api.shop.example.com/dev/setup

# 4. Stop and restart in Production mode
docker compose stop backend
docker compose up -d backend
```

---

## 9. Phase 8: Stripe Webhook Setup

### Configure the webhook endpoint in Stripe

1. Go to [Stripe Dashboard → Webhooks](https://dashboard.stripe.com/webhooks)
2. Click **Add endpoint**
3. **Endpoint URL**: `https://api.shop.example.com/payments/webhook`
4. **Events to listen to**:
   - `payment_intent.succeeded`
   - `payment_intent.payment_failed`
5. Click **Add endpoint**
6. Copy the **Signing secret** from the Stripe dashboard

### Add the signing secret to env

```bash
# On the VPS
cd /opt/ecommerce
nano .env.production

# Update this line:
Stripe__WebhookSecret=YOUR_COPIED_STRIPE_WEBHOOK_SECRET

# Restart backend to pick up the change
docker compose up -d backend
```

### Test the webhook

```bash
# From Stripe dashboard, go to Webhooks → your endpoint → "Send test webhook"
# Select "payment_intent.succeeded" and send

# Watch backend logs for processing:
docker compose logs --tail=20 backend

# Expected log line:
# "Webhook: Order {id} marked as paid. PaymentIntent: {id}"
```

---

## 10. Phase 9: SendGrid Configuration

### Get SendGrid API key

1. Go to [SendGrid](https://app.sendgrid.com) → Settings → API Keys
2. Click **Create API Key**
3. Choose **Full Access** or **Restricted Access** with Mail Send permission
4. Copy the API key from the SendGrid dashboard

### Update the env file

```bash
# On the VPS
cd /opt/ecommerce
nano .env.production

# Update these lines:
SendGrid__ApiKey=YOUR_SENDGRID_API_KEY
SendGrid__FromEmail=noreply@yourdomain.com
SendGrid__FromName=Your Store Name

# Restart backend
docker compose up -d backend
```

### Verify email sending

```bash
# Register a new user via the frontend
# The registration endpoint triggers a verification email
# Check backend logs:
docker compose logs --tail=20 backend | grep -i "email\|SendGrid"

# Expected: "Verification email queued for {email}"
# SendGrid errors show: "Failed to send email to {email}. Status: Unauthorized"
```

> **Important:** In Development/Production mode, users are NOT auto-verified. They must verify their email via the link sent by SendGrid. If SendGrid is not configured, verification emails will fail silently.

---

## 11. Phase 10: GitHub Actions CI/CD

### Step 1: Push your code to GitHub

```bash
# From your local machine (project root)
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/YOUR_USERNAME/ecommerce.git
git push -u origin main
```

### Step 2: Add GitHub Secrets

Go to your GitHub repo → **Settings** → **Secrets and variables** → **Actions** → **New repository secret**:

| Secret Name | Value |
|-------------|-------|
| `VPS_HOST` | Your VPS IP address (e.g., `203.0.113.10`) |
| `VPS_USER` | SSH username (e.g., `deploy-user`) |
| `VPS_SSH_KEY` | Private SSH key (PEM format) — see below |
| `VITE_API_URL` | `https://api.shop.example.com` |

### Generate and add SSH key

```bash
# On your local machine
ssh-keygen -t ed25519 -C "github-actions-deploy" -f ~/.ssh/github-actions

# Copy the public key to VPS
ssh-copy-id -i ~/.ssh/github-actions.pub deploy-user@YOUR_SERVER_IP

# Copy the private key contents
cat ~/.ssh/github-actions
# Copy the entire output including -----BEGIN OPENSSH PRIVATE KEY-----
# Paste it as the VPS_SSH_KEY secret in GitHub
```

### Step 3: Configure GitHub Container Registry

GitHub Actions uses the auto-generated `GITHUB_TOKEN` to push to GHCR. The workflow file at `.github/workflows/deploy.yml` already has `permissions.packages: write` which grants this access.

### Step 4: Enable GitHub Actions

1. Push to `main` branch — the workflow triggers automatically
2. Go to GitHub repo → **Actions** tab to watch the workflow run
3. The workflow:
   - Builds backend image → tags with commit SHA + `latest`
   - Builds frontend image → tags with commit SHA + `latest`
   - Pushes both to `ghcr.io/YOUR_REPO/ecommerce-backend` and `ecommerce-frontend`
   - SSHs into VPS → updates `IMAGE_TAG` → pulls new images → restarts

### Step 5: Verify CI/CD

```bash
# After the workflow completes:
# 1. Make a small change (e.g., update a label in the frontend)
# 2. Commit and push to main
# 3. Watch the Actions tab
# 4. After deployment, refresh the live site — changes should appear < 5 min
```

---

## 12. Verification Checklist

Run through each test after deployment. Tick each one:

### Core Infrastructure

- [ ] **Backend health**: `curl https://api.shop.example.com/health` → `200 OK`
- [ ] **Database ready**: `curl https://api.shop.example.com/health/ready` → `200 OK`
- [ ] **HTTPS valid**: Visit `https://shop.example.com` → green lock in browser
- [ ] **HTTPS redirect**: Visit `http://shop.example.com` → redirects to `https://`
- [ ] **API via HTTPS**: `curl https://api.shop.example.com/health` → no redirect, returns JSON

### Frontend

- [ ] **SPA loads**: Open `https://shop.example.com` → homepage renders
- [ ] **Client-side routing**: Navigate to `/products`, `/cart`, `/login` → pages load
- [ ] **Images**: Upload a profile image → visit `/images/{filename}` → image displays

### Authentication

- [ ] **Register**: Create a new account → success message
- [ ] **Login**: Log in with registered credentials → JWT token returned
- [ ] **Profile**: View profile page → user details display
- [ ] **Edit profile**: Update name/address → changes saved
- [ ] **Change password**: Change password → new password works
- [ ] **Logout**: Log out → redirected to home, protected routes inaccessible

### Shopping

- [ ] **Product listing**: `https://api.shop.example.com/products` → returns products
- [ ] **Product detail**: Navigate to a product → details load
- [ ] **Add to cart**: Add product → cart updates
- [ ] **Checkout**: Proceed to checkout → Stripe payment intent created
- [ ] **Order history**: View orders → past orders listed

### Admin

- [ ] **Admin login**: Login as admin → access admin routes
- [ ] **Dashboard**: `/admin/dashboard` → stats load (products, orders, revenue)
- [ ] **Analytics**: `/admin/analytics` → charts render (Revenue, Order Trends, etc.)
- [ ] **Product management**: Create, edit, delete products as admin
- [ ] **Order management**: View and update order statuses
- [ ] **User management**: View and manage users
- [ ] **Audit logs**: View audit trail

### Payments (Stripe)

- [ ] **Payment intent**: Checkout creates Stripe PaymentIntent
- [ ] **Webhook received**: Stripe test webhook arrives at backend
- [ ] **Order marked paid**: After successful payment, order status = `Paid`

### Resilience

- [ ] **Database persist**: `docker compose down && docker compose up -d` → all data intact
- [ ] **Rate limiting**: Rapid requests to `/auth/login` → 429 response
- [ ] **Container restart**: `docker compose restart backend` → backend recovers, migrations NOT re-run

---

## 13. Troubleshooting

### Container won't start

```bash
# Check all container statuses
docker compose ps

# Check logs
docker compose logs backend
docker compose logs sqlserver
docker compose logs caddy
docker compose logs frontend

# Common issues:
# - SQL Server needs 2GB RAM → check `free -m`
# - Port conflict → check `sudo lsof -i :80` or `sudo lsof -i :443`
# - Environment variables missing → check `docker compose config`
```

### SQL Server health check fails

```bash
# Manually test SQL Server connection
docker exec -it ecommerce-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourSuperStrongPassword123!" -C -Q "SELECT 1"

# If this fails, the SA_PASSWORD doesn't match between .env.production and the docker-compose
# Check the password in .env.production matches what SQL Server was initialized with
```

### Backend can't connect to SQL Server

```bash
# Check the connection string
docker compose exec backend env | grep ConnectionStrings

# Verify DNS resolution inside the container
docker compose exec backend ping sqlserver

# Check SQL Server is listening
docker compose exec sqlserver netstat -tlnp | grep 1433
```

### Caddy SSL certificate not provisioning

```bash
# Check Caddy logs
docker compose logs caddy

# Common issues:
# - DNS not propagated → verify with `dig shop.example.com`
# - Port 80 not accessible → check firewall: `sudo ufw status`
# - Domain pointed to wrong IP → check your A record

# For testing without a domain, use Caddy's HTTP-only mode:
# Edit Caddyfile and replace domains with:
# :80 {
#     reverse_proxy frontend:80
# }
# :8080 {
#     reverse_proxy backend:8080
# }
```

### Image uploads not working

```bash
# Check the uploads volume
docker compose exec backend ls -la /app/wwwroot/images/

# Check nginx proxy
curl -I https://shop.example.com/images/test.jpg

# The nginx in the frontend container proxies /images/ to backend:8080
# Verify backend serves images directly:
curl -I http://localhost:8080/images/test.jpg
```

---

## 14. Caveats & Known Issues

### 1. Required Code Change — Forwarded Headers

The backend calls `app.UseHttpsRedirection()` which redirects HTTP → HTTPS. Since Caddy proxies to the backend over internal HTTP, **every API call gets a 301 redirect** unless `app.UseForwardedHeaders()` is added to `Program.cs`.

**Fix:** Add this to Program.cs (after line 276):
```csharp
app.UseForwardedHeaders();
```
And add `using Microsoft.AspNetCore.HttpOverrides;` at the top.

### 2. VITE_API_URL is Build-Time

`VITE_API_URL` is baked into the JavaScript bundle during `docker build`. If you change your API domain after deployment, you must rebuild the frontend image:

```bash
docker build \
  --build-arg VITE_API_URL=https://new-api.yourdomain.com \
  -t ghcr.io/YOUR_USERNAME/ecommerce/ecommerce-frontend:latest \
  ./frontend
docker push ...
# Then on VPS: docker compose pull && docker compose up -d
```

### 3. SQL Server Memory Requirement

SQL Server Express in Docker requires **minimum 2GB RAM**. On smaller VPS instances:

- The container will crash-loop with `/opt/mssql/bin/sqlservr: This program requires a machine with at least 2000 megabytes of memory`
- **Fix:** Upgrade the VPS, or configure SQL Server to use less memory (not recommended for production)

### 4. Uploaded Images Persistence

Uploaded images are stored in a Docker named volume (`uploads-data`). This volume persists across restarts but is NOT backed up automatically.

**For production, add a backup cron job:**
```bash
# Backup volumes to /backup/ecommerce/
docker run --rm -v uploads-data:/source -v /backup/ecommerce:/backup alpine \
  tar czf /backup/uploads-$(date +%Y%m%d-%H%M%S).tar.gz -C /source .
```

### 5. No Seed Data in Production

`SeedDataAsync()` only runs in Development mode. In Production, there are no categories, products, or admin account initially. You must create these manually (see Phase 7).

### 6. SendGrid Placeholder Fails Silently

If `SendGrid__ApiKey` is `sendgrid_placeholder`, email sending fails with 401 Unauthorized. This is logged as a warning but does NOT crash the app. Registration verification and password reset emails will not be delivered until a valid API key is configured.

### 7. Stripe Webhook Requires Public URL

The Stripe webhook endpoint at `https://api.shop.example.com/payments/webhook` must be publicly accessible. Stripe sends events from their servers, so:
- The VPS must be reachable from the internet
- Port 443 must be open (Caddy handles this)
- The domain must resolve to the VPS IP

### 8. Deploy Script Requires sudo-less Docker

The `deploy/deploy.sh` script assumes the user can run `docker compose` without `sudo`. The `setup-server.sh` script adds the user to the `docker` group, which requires a logout/login to take effect.

### 9. Database Migrations Run Every Startup

The backend entrypoint always passes `--migrate`:
```
ENTRYPOINT ["dotnet", "E-Commerce.API.dll", "--migrate"]
```
EF Core's `Database.Migrate()` is idempotent — it skips already-applied migrations. This is safe to run on every startup.

### 10. JWT Key Rotation Invalidates Sessions

Changing `Jwt__Key` in `.env.production` will invalidate ALL existing JWT tokens and refresh tokens. All users will be logged out and must re-authenticate.

---

## Quick Start (Cheat Sheet)

```bash
# === LOCAL MACHINE ===

# 1. Add ForwardedHeaders to Program.cs (REQUIRED — see caveat 1)
# 2. Copy files to VPS
scp deploy/docker-compose.yml deploy-user@VPS_IP:/opt/ecommerce/
scp deploy/Caddyfile deploy-user@VPS_IP:/opt/ecommerce/
scp deploy/.env.production deploy-user@VPS_IP:/opt/ecommerce/
scp frontend/nginx.conf deploy-user@VPS_IP:/opt/ecommerce/

# 3. Build and push images
docker build -t ghcr.io/YOU/ecommerce/ecommerce-backend:latest ./E-Commerce.API
docker push ghcr.io/YOU/ecommerce/ecommerce-backend:latest
docker build --build-arg VITE_API_URL=https://api.example.com \
  -t ghcr.io/YOU/ecommerce/ecommerce-frontend:latest ./frontend
docker push ghcr.io/YOU/ecommerce/ecommerce-frontend:latest

# === ON VPS ===

# 4. Configure env
cd /opt/ecommerce
nano .env.production           # Fill in secrets

# 5. Start
docker compose pull
docker compose up -d
docker compose logs --tail=50 -f

# 6. Create admin
docker exec -it ecommerce-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "YourPassword!" -C \
  -Q "UPDATE [ECommerce].[dbo].[Users] SET [Role]='Admin', [IsEmailVerified]=1 WHERE [Email]='admin@example.com'"

# 7. Verify
curl https://api.example.com/health
curl -I https://shop.example.com
```
