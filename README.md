# Linear Clone

A small Linear-style issue tracker with a .NET API, Angular frontend, and SQL Server database.

## Tech Stack

- Backend: .NET 10, ASP.NET Core, EF Core, SQL Server
- Frontend: Angular
- API docs: OpenAPI rendered with Scalar
- Local database: SQL Server 2022 via Docker Compose

## Prerequisites

- .NET 10 SDK
- Node.js and npm
- Docker Desktop
- Optional: `dotnet-ef` for EF Core migrations

Install the EF Core CLI if you do not already have it:

```bash
dotnet tool install --global dotnet-ef
```

## First-Time Setup

### 1. Configure SQL Server

Create a local `.env` file from the example:

```bash
cp .env.example .env
```

Edit `.env` and set a strong SQL Server password:

```env
MSSQL_SA_PASSWORD=YourStrongPassword123!
```

SQL Server requires a complex password. Use at least 8 characters with uppercase, lowercase, number, and symbol characters.

### 2. Start SQL Server

```bash
docker compose up -d
```

The database container is exposed on `localhost:1433`.

### 3. Configure the Backend Connection String

The API reads the `Default` connection string from configuration. For local development, store it in .NET user secrets:

```bash
cd backend/src/Api
dotnet user-secrets set "ConnectionStrings:Default" "Server=localhost,1433;Database=LinearClone;User Id=sa;Password=YourStrongPassword123!;TrustServerCertificate=True"
```

Use the same password you placed in `.env`.

### 4. Restore and Create the Database

From the API project directory:

```bash
dotnet restore
dotnet ef database update --project ../Infrastructure --startup-project .
```

The app seeds an Engineering team and default workflow states when the API starts and the database is empty.

### 5. Run the Backend

From `backend/src/Api`:

```bash
dotnet run --launch-profile http
```

Backend URLs:

- API: `http://localhost:5109`
- Scalar API docs: `http://localhost:5109/scalar/v1`
- OpenAPI JSON: `http://localhost:5109/openapi/v1.json`

### 6. Run the Frontend

In a second terminal:

```bash
cd frontend
npm install
npm start
```

Open:

```text
http://localhost:4200
```

The Angular dev server proxies `/api/*` to `http://localhost:5109` via `frontend/proxy.conf.json`.

## Daily Development

Start the database:

```bash
docker compose up -d
```

Run the backend:

```bash
cd backend/src/Api
dotnet run --launch-profile http
```

Run the frontend:

```bash
cd frontend
npm start
```

Stop the database:

```bash
docker compose stop
```

Remove the database container and volume:

```bash
docker compose down -v
```

## Useful Commands

Run backend tests:

```bash
dotnet test backend/tests/Api.Tests/LinearClone.Api.Tests.csproj
```

Build the frontend:

```bash
cd frontend
npm run build
```

Run frontend tests:

```bash
cd frontend
npm test
```

Apply EF Core migrations:

```bash
cd backend/src/Api
dotnet ef database update --project ../Infrastructure --startup-project .
```

Create a new EF Core migration:

```bash
cd backend/src/Api
dotnet ef migrations add MigrationName --project ../Infrastructure --startup-project .
```
