# Cekok

> **Cekok** — Self-hosted auto-deploy orchestrator. Build once on the master server, distribute to all.

---

## Overview

Cekok adalah aplikasi **CI/CD ringan berbasis web** yang di-host sendiri di atas server Linux. Satu server master menjalankan seluruh proses: clone repo, build artifact, lalu distribusikan ke beberapa server target via SCP + SSH. Tidak bergantung pada layanan cloud eksternal.

**Stack:**
- Backend — .NET 10 ASP.NET Core (Minimal API)
- Frontend — Nuxt 3 (Vue 3 + Vite)
- Database — SQLite via EF Core Code First
- Job Queue — Hangfire
- SSH/SCP — SSH.NET library
- Monorepo — satu Git repository

---

## Arsitektur

```
cekok/
├── backend/          ← .NET 10 ASP.NET Core
└── frontend/         ← Nuxt 3
```

Master server (`10.0.0.1`) menjalankan backend Cekok sekaligus sebagai build server. Target servers (`10.0.0.2`, `10.0.0.3`, dst.) hanya menerima artifact dan menjalankan service.

```
Master (10.0.0.1)
  └── Cekok API + Dashboard
        ├── git pull / build      ← lokal di master
        ├── SCP ──────────────→   10.0.0.2  → systemctl restart
        ├── SCP ──────────────→   10.0.0.3  → systemctl restart
        └── SCP ──────────────→   10.0.0.4  → systemctl restart
```

---

## Struktur Monorepo

```
cekok/
├── .gitignore
├── README.md
│
├── backend/
│   ├── Cekok.sln
│   └── src/
│       └── Cekok.Api/
│           ├── Cekok.Api.csproj
│           ├── Program.cs
│           ├── appsettings.json
│           ├── appsettings.Development.json
│           │
│           ├── Controllers/
│           │   ├── AuthController.cs
│           │   ├── UsersController.cs
│           │   ├── ServersController.cs
│           │   ├── ApplicationsController.cs
│           │   ├── DeployController.cs
│           │   ├── NginxController.cs
│           │   └── ScheduleController.cs
│           │
│           ├── Services/
│           │   ├── AuthService.cs
│           │   ├── UserService.cs
│           │   ├── DeployService.cs
│           │   ├── SshService.cs
│           │   ├── ScpService.cs
│           │   ├── BuildService.cs
│           │   ├── NginxService.cs
│           │   ├── EncryptionService.cs
│           │   └── HealthCheckService.cs
│           │
│           ├── Middleware/
│           │   └── ServerAccessMiddleware.cs  ← enforce server-level access per request
│           │
│           ├── Jobs/
│           │   └── ScheduledDeployJob.cs
│           │
│           ├── Models/
│           │   ├── User.cs
│           │   ├── UserServerAccess.cs
│           │   ├── RefreshToken.cs
│           │   ├── Server.cs
│           │   ├── Application.cs
│           │   ├── DeployTarget.cs
│           │   ├── DeployJob.cs
│           │   ├── DeployLog.cs
│           │   └── NginxConfig.cs
│           │
│           ├── Data/
│           │   └── CekokDbContext.cs
│           │
│           └── Templates/
│               ├── nginx-reverse-proxy.conf
│               ├── nginx-static-site.conf
│               └── nginx-upstream.conf
│
└── frontend/
    ├── package.json
    ├── nuxt.config.ts
    ├── tsconfig.json
    │
    ├── pages/
    │   ├── index.vue              ← redirect ke /dashboard
    │   ├── login.vue              ← form login
    │   ├── dashboard.vue
    │   ├── servers.vue
    │   ├── schedules.vue
    │   ├── history.vue
    │   └── admin/
    │       ├── users.vue          ← list user (admin only)
    │       └── users/[id].vue     ← edit user + server access
    │
    ├── components/
    │   ├── AppList.vue
    │   ├── AppDetailPanel.vue
    │   ├── ServerCard.vue
    │   ├── DeployLog.vue
    │   ├── CronEditor.vue
    │   ├── AddAppModal.vue
    │   └── admin/
    │       ├── UserTable.vue
    │       └── ServerAccessPicker.vue  ← checklist server per user
    │
    ├── composables/
    │   ├── useAuth.ts             ← login, logout, refresh token
    │   ├── useApps.ts
    │   ├── useServers.ts          ← hanya server yg accessible oleh user
    │   ├── useDeployLog.ts        ← SSE stream
    │   └── useTheme.ts
    │
    ├── middleware/
    │   ├── auth.ts                ← redirect ke /login jika belum login
    │   └── admin.ts               ← redirect jika bukan admin
    │
    └── layouts/
        ├── default.vue            ← sidebar + topbar (authenticated)
        └── auth.vue               ← layout polos untuk halaman login
```

---

## Database Schema

```sql
-- Users
users (
  id            TEXT PRIMARY KEY,
  username      TEXT NOT NULL UNIQUE,
  display_name  TEXT NOT NULL,
  password_hash TEXT NOT NULL,         -- BCrypt
  role          TEXT NOT NULL,         -- admin | operator | viewer
  is_active     INTEGER DEFAULT 1,
  created_at    TEXT NOT NULL,
  last_login_at TEXT
)

-- Mapping user → server (many-to-many)
user_server_access (
  id            TEXT PRIMARY KEY,
  user_id       TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  server_id     TEXT NOT NULL REFERENCES servers(id) ON DELETE CASCADE,
  can_deploy    INTEGER DEFAULT 1,     -- boleh trigger deploy
  can_manage    INTEGER DEFAULT 0,     -- boleh edit config server / nginx
  granted_by    TEXT NOT NULL REFERENCES users(id),
  granted_at    TEXT NOT NULL,
  UNIQUE(user_id, server_id)
)

-- Refresh tokens (JWT)
refresh_tokens (
  id            TEXT PRIMARY KEY,
  user_id       TEXT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  token_hash    TEXT NOT NULL UNIQUE,  -- SHA-256 dari token
  expires_at    TEXT NOT NULL,
  created_at    TEXT NOT NULL,
  revoked_at    TEXT                   -- NULL = masih aktif
)

-- Audit log aksi user
audit_logs (
  id            INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id       TEXT NOT NULL REFERENCES users(id),
  action        TEXT NOT NULL,         -- deploy | rollback | server.add | user.create, dst.
  target_type   TEXT,                  -- server | application | user
  target_id     TEXT,
  detail        TEXT,                  -- JSON
  ip_address    TEXT,
  created_at    TEXT NOT NULL
)

-- Server registry
servers (
  id            TEXT PRIMARY KEY,
  name          TEXT NOT NULL,
  ip            TEXT NOT NULL,
  ssh_port      INTEGER DEFAULT 22,
  ssh_user      TEXT NOT NULL,
  ssh_password_enc TEXT NOT NULL,   -- AES-256-GCM encrypted
  role          TEXT NOT NULL,      -- master | app-server | proxy | db-server
  tags          TEXT,               -- JSON array
  nginx_installed INTEGER DEFAULT 0,
  created_at    TEXT NOT NULL
)

-- Registered apps
applications (
  id            TEXT PRIMARY KEY,
  name          TEXT NOT NULL,
  type          TEXT NOT NULL,      -- dotnet | nuxt | vue | next | angular | static
  repo_url      TEXT NOT NULL,
  branch        TEXT DEFAULT 'main',
  build_cmd     TEXT,
  output_dir    TEXT,
  schedule_cron TEXT,
  schedule_enabled INTEGER DEFAULT 0,
  created_at    TEXT NOT NULL
)

-- Deploy target per app per server
deploy_targets (
  id            TEXT PRIMARY KEY,
  app_id        TEXT NOT NULL REFERENCES applications(id),
  server_id     TEXT NOT NULL REFERENCES servers(id),
  deploy_dir    TEXT NOT NULL,
  service_name  TEXT,
  port          INTEGER,
  health_check_url TEXT
)

-- Deploy job history
deploy_jobs (
  id                  TEXT PRIMARY KEY,
  app_id              TEXT NOT NULL REFERENCES applications(id),
  triggered_by        TEXT NOT NULL,        -- manual | schedule | webhook
  triggered_by_user   TEXT REFERENCES users(id),  -- NULL jika schedule/webhook
  status              TEXT NOT NULL,        -- queued | running | success | failed
  commit_hash         TEXT,
  commit_msg          TEXT,
  started_at          TEXT,
  finished_at         TEXT
)

-- Per-target log lines
deploy_logs (
  id            INTEGER PRIMARY KEY AUTOINCREMENT,
  job_id        TEXT NOT NULL REFERENCES deploy_jobs(id),
  server_id     TEXT,               -- NULL = master/build phase
  timestamp     TEXT NOT NULL,
  level         TEXT NOT NULL,      -- info | cmd | success | warn | error
  message       TEXT NOT NULL
)

-- Nginx config snapshot per server
nginx_configs (
  id            TEXT PRIMARY KEY,
  server_id     TEXT NOT NULL REFERENCES servers(id),
  site_name     TEXT NOT NULL,
  template_type TEXT NOT NULL,      -- reverse_proxy | static_site | upstream
  config_content TEXT NOT NULL,
  last_deployed_at TEXT
)
```

---

## User Management

### Roles

| Role | Deskripsi |
|------|-----------|
| `admin` | Akses penuh — kelola user, server, app, deploy semua server |
| `operator` | Deploy & manage app, tapi hanya pada server yang di-assign |
| `viewer` | Hanya bisa lihat dashboard & log, tidak bisa trigger deploy |

`admin` selalu punya akses ke semua server tanpa perlu entry di `user_server_access`. Untuk `operator` dan `viewer`, akses dikontrol per server.

---

### Server Access Matrix

```
                  10.0.0.1  10.0.0.2  10.0.0.3  10.0.0.4
admin (budi)         ✓         ✓         ✓         ✓       ← semua server otomatis
operator (andi)      –         ✓         ✓         –       ← hanya srv-2, srv-3
operator (sari)      –         –         ✓         ✓       ← hanya srv-3, srv-4
viewer (tono)        –         ✓         –         –       ← hanya lihat srv-2
```

Kolom `can_deploy` dan `can_manage` di `user_server_access` memungkinkan granularitas lebih:

```
andi  → srv-2: can_deploy=true,  can_manage=false   ← bisa deploy, tidak bisa edit nginx
andi  → srv-3: can_deploy=true,  can_manage=true    ← akses penuh ke srv-3
sari  → srv-3: can_deploy=false, can_manage=false   ← hanya viewer di srv-3
```

---

### Auth Flow (JWT + Refresh Token)

```
[Login]
  POST /api/auth/login  { username, password }
        │
        ├── verify password (BCrypt)
        ├── generate access_token  (JWT, expire 15 menit)
        ├── generate refresh_token (opaque, expire 7 hari, simpan hash ke DB)
        └── return { access_token, refresh_token, user: { id, username, role } }

[Request terautentikasi]
  Header: Authorization: Bearer {access_token}
        │
        └── JWT middleware validasi + inject ClaimsPrincipal

[Refresh]
  POST /api/auth/refresh  { refresh_token }
        │
        ├── lookup hash di refresh_tokens, cek belum expired / revoked
        ├── generate access_token baru
        ├── rotate refresh_token (revoke lama, buat baru)
        └── return { access_token, refresh_token }

[Logout]
  POST /api/auth/logout
        └── revoke refresh_token (set revoked_at)
```

---

### Server Access Enforcement

Setiap request yang menyentuh resource server tertentu divalidasi oleh `ServerAccessMiddleware`:

```
Request: POST /api/deploy/{appId}
        │
        ├── ambil app → ambil deploy_targets → kumpulkan server_id
        ├── ambil user dari JWT claim
        │
        ├── jika role = admin          → ALLOW semua target
        ├── jika role = operator/viewer
        │     └── query user_server_access WHERE user_id = ? AND server_id IN (target_ids)
        │           ├── ada entry + can_deploy = true  → ALLOW target tsb
        │           └── tidak ada / can_deploy = false → REMOVE target dari job
        │                                                 (deploy hanya ke server yg diizinkan)
        │
        └── jika semua target di-remove → 403 Forbidden

Request: GET /api/servers
        │
        ├── admin    → return semua server
        └── operator/viewer → return hanya server yang ada di user_server_access-nya
```

Middleware dipasang sebagai ASP.NET Core `IAuthorizationMiddlewareResultHandler` atau policy-based authorization.

---

### Package Tambahan (Backend)

```bash
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package BCrypt.Net-Next
```

---

## Deploy Pipeline

Setiap deploy job menjalankan langkah berikut secara berurutan:

```
[1] Pre-check
    └── cek free disk di master (abort jika < 500MB)

[2] Git
    ├── git clone  (jika belum ada)
    └── git pull origin {branch}

[3] Build  (di master, lokal)
    ├── .NET   → dotnet publish -c Release -o /tmp/cekok-builds/{appId}/{jobId}/
    ├── Nuxt   → npm ci && npm run build  → .output/
    ├── Vue    → npm run build            → dist/
    └── Static → copy as-is

[4] Artifact validation
    └── cek output dir tidak kosong

[5] Deploy ke semua targets  (Task.WhenAll — paralel)
    ├── SSH connect  (SSH.NET, password auth, AES-decrypt on the fly)
    ├── backup:  mv {deployDir} {deployDir}.bak.{timestamp}
    ├── mkdir -p {deployDir}
    ├── SCP:    upload semua file dari /tmp/cekok-builds/{appId}/{jobId}/
    ├── restart: systemctl restart {serviceName}
    ├── tunggu 3 detik
    └── health check: curl http://localhost:{port}/health
          ├── 200 OK  → mark target = SUCCESS
          └── fail    → rollback target ini saja
                        mv {deployDir}.bak.{timestamp} {deployDir}
                        systemctl restart {serviceName}
                        mark target = FAILED

[6] Cleanup
    └── rm -rf /tmp/cekok-builds/{appId}/{jobId}/

[7] Post-deploy
    ├── simpan hasil ke deploy_logs
    └── kirim notifikasi (Slack webhook / email)  ← opsional
```

---

## API Endpoints (rencana)

**Auth** _(public)_

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| POST | `/api/auth/login` | Login, return JWT + refresh token |
| POST | `/api/auth/refresh` | Rotate refresh token |
| POST | `/api/auth/logout` | Revoke refresh token |
| GET | `/api/auth/me` | Info user yang sedang login |

**User Management** _(admin only)_

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| GET | `/api/users` | List semua user |
| POST | `/api/users` | Buat user baru |
| PUT | `/api/users/{id}` | Update user (nama, role, status) |
| DELETE | `/api/users/{id}` | Hapus user |
| PUT | `/api/users/{id}/password` | Reset password user |
| GET | `/api/users/{id}/server-access` | List server access user |
| PUT | `/api/users/{id}/server-access` | Set server access (replace all) |
| POST | `/api/users/{id}/server-access/{serverId}` | Grant akses ke server tertentu |
| DELETE | `/api/users/{id}/server-access/{serverId}` | Revoke akses ke server tertentu |

**Servers** _(filtered by user access)_

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| GET | `/api/servers` | List server (admin: semua, lainnya: hanya yg diizinkan) |
| POST | `/api/servers` | Tambah server _(admin only)_ |
| DELETE | `/api/servers/{id}` | Hapus server _(admin only)_ |
| POST | `/api/servers/{id}/test-connection` | Test SSH |

**Applications**

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| GET | `/api/applications` | List app (filtered berdasarkan server access) |
| POST | `/api/applications` | Tambah app _(admin/operator)_ |
| PUT | `/api/applications/{id}` | Update app config |
| DELETE | `/api/applications/{id}` | Hapus app _(admin only)_ |

**Deploy**

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| POST | `/api/deploy/{appId}` | Trigger deploy (hanya ke server yang diizinkan user) |
| GET | `/api/deploy/{appId}/status` | Status deploy terakhir |
| GET | `/api/deploy/{appId}/logs` | Stream log SSE (filtered by accessible servers) |
| GET | `/api/deploy/history` | Riwayat deploy |
| POST | `/api/deploy/{jobId}/rollback` | Rollback _(admin/operator)_ |

**Nginx**

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| GET | `/api/nginx/{serverId}/status` | Status nginx |
| POST | `/api/nginx/{serverId}/install` | Install nginx _(requires can_manage)_ |
| POST | `/api/nginx/{serverId}/reload` | Reload config _(requires can_manage)_ |
| POST | `/api/nginx/{serverId}/deploy-config` | Upload + apply config _(requires can_manage)_ |

**Schedule & Audit**

| Method | Endpoint | Deskripsi |
|--------|----------|-----------|
| GET | `/api/schedule` | List semua schedule |
| PUT | `/api/schedule/{appId}` | Update cron schedule |
| GET | `/api/audit` | Audit log _(admin only)_ |

---

## Inisialisasi Project

### Prerequisites

```bash
# Master server (10.0.0.1)
dotnet --version    # 10.0+
node --version      # 20+
git --version
```

### 1. Clone / Init repo

```bash
git init cekok
cd cekok
```

### 2. Backend — .NET 10

```bash
mkdir -p backend/src
cd backend

# buat solution
dotnet new sln -n Cekok

# buat project
dotnet new webapi -n Cekok.Api -o src/Cekok.Api --use-minimal-apis
dotnet sln add src/Cekok.Api/Cekok.Api.csproj

# tambah packages
cd src/Cekok.Api
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet add package Hangfire.Core
dotnet add package Hangfire.SQLite
dotnet add package Hangfire.AspNetCore
dotnet add package SSH.NET
dotnet add package Serilog.AspNetCore
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
dotnet add package BCrypt.Net-Next

cd ../../..
```

### 3. Frontend — Nuxt 3

```bash
# dari root monorepo
npx nuxi@latest init frontend
cd frontend

# tambah dependencies
npm install @pinia/nuxt
npm install -D @nuxtjs/tailwindcss   # opsional, atau pure CSS
```

### 4. .gitignore (root)

```gitignore
# .NET
backend/**/bin/
backend/**/obj/
backend/**/*.user
backend/src/Cekok.Api/cekok.db
backend/src/Cekok.Api/cekok.db-shm
backend/src/Cekok.Api/cekok.db-wal

# .NET secrets
backend/**/appsettings.*.json
!backend/**/appsettings.Development.json

# Node / Nuxt
frontend/node_modules/
frontend/.nuxt/
frontend/.output/
frontend/dist/

# Build artifacts
/tmp/cekok-builds/

# OS
.DS_Store
Thumbs.db
```

### 5. Jalankan (development)

```bash
# terminal 1 — backend
cd backend/src/Cekok.Api
dotnet run

# terminal 2 — frontend
cd frontend
npm run dev
```

Backend berjalan di `http://localhost:5000`, frontend di `http://localhost:3000`.

---

## Environment & Konfigurasi

### Backend — `appsettings.json`

```json
{
  "Cekok": {
    "MasterIp": "10.0.0.1",
    "BuildDir": "/tmp/cekok-builds",
    "MinFreeDiskMb": 500
  },
  "Encryption": {
    "Secret": ""
  },
  "Jwt": {
    "Secret": "",
    "Issuer": "cekok",
    "Audience": "cekok-ui",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  },
  "ConnectionStrings": {
    "Default": "Data Source=cekok.db"
  },
  "Hangfire": {
    "ConnectionString": "Data Source=cekok-jobs.db"
  }
}
```

> Dua secret **wajib diisi** via environment variable di production, bukan di file:
> - `CEKOK_ENCRYPTION_SECRET` — untuk enkripsi password SSH server target
> - `CEKOK_JWT_SECRET` — untuk signing JWT access token (min. 32 karakter)

### Frontend — `nuxt.config.ts`

```typescript
export default defineNuxtConfig({
  devtools: { enabled: true },
  runtimeConfig: {
    public: {
      apiBase: process.env.NUXT_PUBLIC_API_BASE || 'http://localhost:5000'
    }
  }
})
```

---

## Deployment Cekok itu sendiri

Cekok di-deploy ke master server (`10.0.0.1`) sebagai systemd service.

```bash
# publish backend
cd backend/src/Cekok.Api
dotnet publish -c Release -o /var/www/cekok-api

# build frontend
cd frontend
npm run build
# copy .output/ ke /var/www/cekok-fe/

# systemd service — /etc/systemd/system/cekok.service
[Unit]
Description=Cekok Deploy Orchestrator
After=network.target

[Service]
WorkingDirectory=/var/www/cekok-api
ExecStart=/var/www/cekok-api/Cekok.Api
Restart=always
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=CEKOK_ENCRYPTION_SECRET=ganti_dengan_secret_kuat
Environment=CEKOK_JWT_SECRET=ganti_dengan_jwt_secret_min_32_char

[Install]
WantedBy=multi-user.target
```

```bash
systemctl enable cekok
systemctl start cekok
```

---

## Roadmap

| Fase | Fitur |
|------|-------|
| v0.1 | Server registry, SSH connect test, app CRUD |
| v0.2 | Auth — JWT login, refresh token, BCrypt password |
| v0.3 | User management — CRUD user, role (admin/operator/viewer) |
| v0.4 | Server access control — user_server_access, middleware enforcement |
| v0.5 | Deploy pipeline (build + SCP + restart) untuk .NET |
| v0.6 | Multi-target paralel deploy + per-target log stream (SSE) |
| v0.7 | Nuxt / Vue / static build support |
| v0.8 | Cron schedule via Hangfire |
| v0.9 | Nginx manager (install, config template, reload) |
| v0.10 | Rollback (auto + manual) + audit log |
| v0.11 | Notifikasi (Slack webhook) |
| v1.0 | Dashboard UI final + production hardening |

---

*Cekok — karena deploy harusnya semudah "cekok" obat: satu telan, langsung jalan.*
