# Kredar Backend — Project Structure Guide

## Tech Stack
- **Framework:** ASP.NET Core (.NET 10)
- **Database:** PostgreSQL via Entity Framework Core
- **Auth:** JWT (JSON Web Tokens)
- **API Docs:** Swagger / OpenAPI
- **Deployment:** Docker

## Running the Project

```bash
docker compose up
```

API will be available at `http://localhost:8080`
Swagger docs at `http://localhost:8080/swagger`

---

## Folder Structure

```
src/Kredar.API/
├── Config/
├── Data/
├── Auth/
├── Tenants/
├── ApiKeys/
├── Customers/
├── DedicatedAccounts/
├── Transactions/
├── Reconciliation/
├── Statements/
├── Webhooks/
├── Nomba/
├── Audit/
└── Common/
```

---

## Folder Breakdown

### `Config/`
Holds settings classes that read values from `appsettings.json`.
- `JwtSettings.cs` — reads JWT secret, issuer, expiry time
- `NombaSettings.cs` — reads Nomba API base URL and credentials

---

### `Data/`
Everything related to the database.
- `AppDbContext.cs` — the main database connection. Every table must be registered here.
- `Migrations/` — auto-generated SQL files created by EF Core. Never edit these manually. Run `dotnet ef migrations add <Name>` to generate a new one when you change the database schema.

---

### `Auth/`
Handles developer/business registration and login.

| File | Responsibility |
|---|---|
| `AuthController.cs` | Exposes `POST /api/auth/register` and `POST /api/auth/login` |
| `AuthService.cs` | Business logic — hash password, verify credentials, create tenant |
| `JwtService.cs` | Generates JWT tokens returned after successful login |
| `Dto/` | Request and response shapes (RegisterRequest, LoginRequest, AuthResponse) |

---

### `Tenants/`
A **Tenant** is a business or developer that signs up on Kredar. Every customer, account, and transaction belongs to a tenant. This is what makes Kredar multi-tenant — AjoVault would be one tenant, a school fees platform would be another.

- `Tenant.cs` — the database entity (maps to the `tenants` table)
- `TenantRepository.cs` — queries the database for tenant records

---

### `ApiKeys/`
When a tenant registers, they generate API keys to authenticate their server-to-server calls to Kredar.

- Create and revoke API keys
- The secret is shown only once on creation
- Revoked keys are immediately blocked
- All key actions are audit logged

**Endpoints:** `POST /api/keys`, `DELETE /api/keys/{id}`

---

### `Customers/`
A **Customer** belongs to a Tenant. For example, if AjoVault is a tenant, each AjoVault user is a customer on Kredar. Every customer gets their own Dedicated Virtual Account.

- Full CRUD — create, list, update, archive customers
- Customers are isolated per tenant — Tenant A cannot see Tenant B's customers

**Endpoints:** `POST /api/customers`, `GET /api/customers`, `GET /api/customers/{id}`, `PATCH /api/customers/{id}`

---

### `DedicatedAccounts/`
The core of Kredar. Every customer gets **one permanent virtual account number** tied to their identity. Unlike a regular virtual account which is temporary and per-transaction, a dedicated virtual account stays with the customer forever.

- First built with mock account numbers
- Later replaced with real Nomba Virtual Account API calls
- Duplicate accounts per customer are prevented

**Endpoints:** `POST /api/accounts`, `GET /api/accounts/{id}`

---

### `Transactions/`
Every payment that lands in a dedicated account becomes a transaction record.

- Paginated transaction history per account
- Filterable by date and status
- Statuses: `MATCHED`, `UNDERPAID`, `OVERPAID`, `DUPLICATE`, `UNMATCHED`

**Endpoints:** `GET /api/accounts/{id}/transactions`, `GET /api/transactions`

---

### `Reconciliation/`
The **brain of the entire system**. When Nomba notifies us that money arrived in an account, this engine:
1. Finds which customer owns that account
2. Checks if the amount matches what was expected
3. Marks the transaction as `MATCHED`, `UNDERPAID`, or `OVERPAID`
4. Detects and blocks duplicate payments
5. Flags payments from unknown accounts
6. Logs every matching attempt with full details

This is what makes Kredar valuable — zero manual reconciliation.

---

### `Statements/`
Generates a summary of all transactions for a customer within a date range — like a mini bank statement.

**Endpoint:** `GET /api/accounts/{id}/statement?from=2026-01-01&to=2026-06-30`

---

### `Webhooks/`
Two things happen here:

1. **Inbound** — Nomba sends payment events to us
2. **Outbound** — We forward enriched events to each tenant's registered URL

Features:
- Tenants register their HTTPS endpoint URL
- Events are signed with HMAC for security
- Failed deliveries are automatically retried (up to 5 times)
- All delivery attempts are logged

**Endpoints:** `POST /api/webhooks/register`

---

### `Nomba/`
All communication with the Nomba API lives here.

- `NombaClient.cs` — makes HTTP calls to Nomba's API
- `NombaTokenService.cs` — handles OAuth 2.0 token and auto-refreshes it every 50 minutes
- `NombaWebhookController.cs` — the endpoint that receives payment events from Nomba (`POST /api/nomba/webhook`)

---

### `Audit/`
Every important action is logged here for accountability and traceability.

Logged events include:
- Tenant registered / logged in
- API key created or revoked
- Customer created or archived
- Dedicated account provisioned
- Reconciliation completed
- Webhook registered

**This is a judging criterion for the hackathon — do not skip it.**

---

### `Common/`
Shared utilities used across the entire project.

| File | Purpose |
|---|---|
| `ApiResponse.cs` | Wraps every response as `{ isSuccess, message, data }` for consistency |
| `TenantContext.cs` | Reads the tenant ID from the JWT on every authenticated request |
| `GlobalExceptionHandler.cs` | Catches all errors in one place — no repeated try/catch in every controller |

---

## The Pattern Every Feature Follows

```
Controller  →  Service  →  Repository
(HTTP in)      (logic)     (DB out)
```

- **Controller** — receives the HTTP request, validates input, calls the service, returns the response
- **Service** — contains all business logic
- **Repository** — talks to the database via EF Core

---

## Branching Strategy

| Branch | Purpose |
|---|---|
| `dev` | Active development — all work happens here |
| `staging` | Pre-production testing |
| `main` | Production / final submission |

**Flow:** `dev` → `staging` → `main`
