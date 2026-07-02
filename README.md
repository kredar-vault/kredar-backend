# Kredar Backend

Kredar is a Dedicated Virtual Accounts (DVA) engine that enables businesses to create and manage virtual bank accounts for their customers. Built for the DevCareer x Nomba Hackathon 2026.

## Tech Stack

- .NET 10 / ASP.NET Core Web API
- PostgreSQL + Entity Framework Core
- Docker + Docker Compose
- JWT Authentication
- MailKit (Gmail SMTP)

## Features

- Multi-tenant architecture (each business is a tenant)
- Customer management with KYC document submission
- Dedicated Virtual Account (DVA) provisioning per customer
- Transaction tracking and reconciliation
- Team member management
- API key management
- Webhook support
- Health check endpoint

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/)

### Run with Docker

```bash
# Clone the repo
git clone https://github.com/kredar-vault/kredar-backend.git
cd kredar-backend

# Create your .env file (see .env.example)
cp .env.example .env

# Start the API and database
docker compose up -d
```

API will be available at `http://localhost:8080`  
Swagger docs at `http://localhost:8080/swagger`

### Environment Variables

Create a `.env` file in the root with the following:

```env
ConnectionStrings__DefaultConnection=Host=localhost;Port=5433;Database=kredar;Username=postgres;Password=postgres
JwtSettings__Secret=your_jwt_secret_min_32_chars
JwtSettings__Issuer=kredar-api
JwtSettings__Audience=kredar-clients
JwtSettings__ExpiryMinutes=60
EmailSettings__Host=smtp.gmail.com
EmailSettings__Port=587
EmailSettings__Username=your_email@gmail.com
EmailSettings__Password=your_app_password
EmailSettings__FromName=Kredar
EmailSettings__FromEmail=your_email@gmail.com
NombaSettings__BaseUrl=https://api.nomba.com
NombaSettings__ClientId=your_nomba_client_id
NombaSettings__ClientSecret=your_nomba_client_secret
NombaSettings__AccountId=your_nomba_account_id
AppSettings__BaseUrl=http://localhost:8080
```

## API Overview

| Tag | Description |
|-----|-------------|
| Auth | Register, login (OTP), email verification, token refresh |
| Tenant | Business profile management |
| Customer | Customer CRUD, status management |
| KYC | KYC document submission and review |
| Transactions | Transaction creation, listing, details |
| Team | Invite and manage team members |
| Health | API health check |

## Branches

| Branch | Purpose |
|--------|---------|
| `dev` | Active development |
| `staging` | Staging environment |
| `main` | Production |
