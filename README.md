# CommunityOS (Phase 1A)

CommunityOS is a **strict multi-tenant** (tenant = country) social MVP.

- Backend: **ASP.NET Core Web API** (target: .NET 8)
- ORM: **EF Core** (code-first) + **SQL Server** (primary)
- Dev fallback (this environment): **SQLite** (only when SQL Server isn't reachable)
- Frontend: **React + TypeScript + React Router** (Vite)

---

## 1) Run in this Emergent environment
Services are managed by **supervisor**.

### Start / restart
```bash
sudo supervisorctl restart all
# or individually
sudo supervisorctl restart backend
sudo supervisorctl restart frontend

sudo supervisorctl status
```

### Ports
- Frontend: **http://localhost:3000** (Preview gateway routes here)
- Backend: **http://localhost:8001**

Frontend calls the backend via Vite proxy:
- Frontend requests `GET/POST /api/...`
- Vite proxies `/api` → `http://127.0.0.1:8001`

### Health checks
- Frontend: `GET http://localhost:3000/`
- Backend: `GET http://localhost:8001/` → `CommunityOS API running`
- Backend: `GET http://localhost:8001/api/health`

### Logs
```bash
tail -n 200 /var/log/supervisor/backend.out.log
tail -n 200 /var/log/supervisor/backend.err.log

tail -n 200 /var/log/supervisor/frontend.out.log
tail -n 200 /var/log/supervisor/frontend.err.log
```

---

## 2) Run locally on your machine (recommended: SQL Server via Docker)

### Prerequisites
- .NET SDK **8.x**
- Node.js 18+ (or 20+), Yarn
- Docker Desktop (for SQL Server)

### Start SQL Server
From repo root:
```bash
docker compose up -d
```

SQL Server will be available at `localhost:1433` with:
- user: `sa`
- password: `YourStrong!Passw0rd`

### Backend (local)
```bash
cd backend

# set connection string (example)
export ConnectionStrings__SqlServer="Server=localhost,1433;Database=CommunityOS;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"

dotnet restore

dotnet tool restore

# apply migrations
DOTNET_ENVIRONMENT=Development dotnet tool run dotnet-ef database update \
  --project CommunityOS.Infrastructure/CommunityOS.Infrastructure.csproj \
  --startup-project CommunityOS.Api/CommunityOS.Api.csproj

# run API
DOTNET_ENVIRONMENT=Development dotnet run --project CommunityOS.Api/CommunityOS.Api.csproj --urls http://0.0.0.0:8001
```

### Frontend (local)
```bash
cd frontend
yarn

yarn dev --host 0.0.0.0 --port 3000
```
Then open `http://localhost:3000`.

---

## 3) API base paths & key endpoints
Backend base:
- Direct: `http://localhost:8001/api/v1/...`
- Via frontend (preferred in Preview/dev): `http://localhost:3000/api/v1/...`

### Auth
- `POST /api/v1/auth/register` (tenant selection via `TenantCode` = `SE` or `IT`)
- `POST /api/v1/auth/login`
- `GET  /api/v1/me`
- `PUT  /api/v1/me/profile`

### Members
- `GET /api/v1/members?q=`
- `GET /api/v1/members/{id}`

### Feed / posts
- `GET  /api/v1/posts?page=&pageSize=`
- `POST /api/v1/posts` (text + 0–10 image URLs + optional `groupId`)
- `GET  /api/v1/posts/{id}`
- `PUT  /api/v1/posts/{id}` (owner only)
- `DELETE /api/v1/posts/{id}` (soft delete)
- `POST /api/v1/posts/{id}/images`

### Comments
- `GET  /api/v1/posts/{id}/comments?page=&pageSize=`
- `POST /api/v1/posts/{id}/comments` (supports replies via `parentCommentId`)
- `PUT  /api/v1/comments/{id}`
- `DELETE /api/v1/comments/{id}` (soft delete)

### Reactions + bookmarks
- `POST   /api/v1/posts/{id}/like`
- `DELETE /api/v1/posts/{id}/like`
- `POST   /api/v1/posts/{id}/bookmark`
- `DELETE /api/v1/posts/{id}/bookmark`

### Groups
- `GET  /api/v1/groups`
- `POST /api/v1/groups`
- `GET  /api/v1/groups/{id}`
- `GET  /api/v1/groups/{id}/posts?page=&pageSize=`
- `POST /api/v1/groups/{id}/join` (public only)
- `POST /api/v1/groups/{id}/leave`

### Notifications
- `GET  /api/v1/notifications?page=&pageSize=`
- `POST /api/v1/notifications/{id}/read`

### Moderation (Admin/Moderator/TenantOwner/PlatformOwner)
- `POST /api/v1/reports`
- `GET  /api/v1/mod/reports?page=&pageSize=`
- `POST /api/v1/mod/reports/{id}/action`
- `POST /api/v1/mod/users/{id}/suspend`
- `POST /api/v1/mod/users/{id}/ban`

---

## 4) Seed tenants & credentials
Tenants:
- Sweden (SE): `TenantId = 11111111-1111-1111-1111-111111111111`
- Italy  (IT): `TenantId = 22222222-2222-2222-2222-222222222222`

Default password for all seeded users:
- `Password123!`

Seeded users:
- PlatformOwner:
  - email: `owner@platform.local`
  - tenantCode: `SE` (role bypasses tenant filters)

- Sweden Admin:
  - email: `admin.se@community.local`
  - tenantCode: `SE`

- Sweden Members:
  - `member1.se@community.local` (SE)
  - `member2.se@community.local` (SE)

- Italy Admin:
  - email: `admin.it@community.local`
  - tenantCode: `IT`

- Italy Members:
  - `member1.it@community.local` (IT)
  - `member2.it@community.local` (IT)

Seed logic is in:
- `backend/CommunityOS.Api/Services/DbMigrator.cs`

---

## 5) Strict multi-tenancy (how it works)
Tenant = Country.

### Tenant resolution
- JWT includes a `tenant_id` claim and a role claim.
- `TenantMiddleware` reads these claims and sets a scoped `TenantProvider`.
- Requests without `tenant_id` are rejected.

Key files:
- `backend/CommunityOS.Api/Middleware/TenantMiddleware.cs`
- `backend/CommunityOS.Api/Services/TenantProvider.cs`

### Enforcement
- **EF Core global query filters** enforce tenant scoping automatically for all tenant-scoped entities.
- **SaveChanges override** sets `TenantId` automatically on create, and blocks cross-tenant updates.

Key file:
- `backend/CommunityOS.Infrastructure/Data/AppDbContext.cs`

PlatformOwner:
- `role == PlatformOwner` bypasses tenant query filters.

---

## 6) EF Core migrations
Migrations live at:
- `backend/CommunityOS.Infrastructure/Data/Migrations/`

Included migrations:
- `20260217002151_InitialCreate`
- `20260217005341_AddGroupPosts`

Apply migrations (local SQL Server):
```bash
cd backend

dotnet tool restore

export ConnectionStrings__SqlServer="Server=localhost,1433;Database=CommunityOS;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;"

DOTNET_ENVIRONMENT=Development dotnet tool run dotnet-ef database update \
  --project CommunityOS.Infrastructure/CommunityOS.Infrastructure.csproj \
  --startup-project CommunityOS.Api/CommunityOS.Api.csproj
```

---

## 7) Known limitations / notes
### SQLite fallback (this environment)
- SQL Server is the primary intended provider.
- In this hosted environment, when SQL Server isn't reachable, the API falls back to SQLite for sanity checks.
- Because SQLite has limitations translating some `DateTimeOffset` sorts, some endpoints order in-memory.

### MVP limitations
- Simple in-memory rate limiting (per tenant+user+action). Not distributed.
- Mention resolution is simplified (matches @username to email local-part within tenant).

---

## 8) Repo tree & key paths
Top-level:
- `backend/` .NET solution
- `frontend/` React TS app
- `docker-compose.yml` SQL Server for local dev

Backend highlights:
- `backend/CommunityOS.Api/Program.cs` (DI, EF provider selection, JWT)
- `backend/CommunityOS.Api/Middleware/TenantMiddleware.cs`
- `backend/CommunityOS.Infrastructure/Data/AppDbContext.cs` (query filters + SaveChanges enforcement)
- `backend/CommunityOS.Api/Controllers/` (all endpoints)
- `backend/CommunityOS.Domain/Entities/` (data model)

Frontend highlights:
- `frontend/vite.config.ts` (allowedHosts + /api proxy)
- `frontend/src/lib/api.ts` (JWT + API calls)
- `frontend/src/lib/AppContext.tsx` (session)
- `frontend/src/pages/*` (Phase 1A pages)