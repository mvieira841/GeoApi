# 🌍 GeoApi

A modern **.NET 9 Minimal API** for managing geographical data (Countries and Cities). This project is a production-ready template built following the **iDesign layered architecture**, emphasizing separation of concerns, clean dependencies, and high testability.

## ✨ Key Features
* **iDesign Architecture:** Strictly layered solution (Host, Service, Resource Access, Interface).
* **Authentication & Authorization:** JWT-based registration/login using ASP.NET Core Identity with role-based security (Admin/User).
* **Full CRUD API:** Complete Create/Read/Update/Delete for Countries and nested CRUD for Cities.
* **Rich Querying:** Server-side pagination, sorting, and filtering on all collection endpoints.
* **Automated Validation:** Cross-cutting validation via Minimal API `IEndpointFilter` and **FluentValidation**.
* **Clean Error Handling:** `FluentResults` for service-layer responses and a global exception handler.
* **Reliable Testing:** Full test suite (Unit, Integration, Acceptance) using **Testcontainers** for isolated SQL Server databases.
* **Modern .NET:** Strongly-typed configuration (`IOptions`), Serilog for structured logging, and C# 13 features.

## 📂 Project Structure
The solution follows the **iDesign layered architecture**:
* **`GeoApi.Abstractions` (Interface Layer):** Contains all contracts, including interfaces (`IManager`, `IRepository`), DTOs (Requests/Responses), entities, and enums. This layer has no dependencies.
* **`GeoApi.Manager` (Service Layer):** Implements business logic in `Manager` classes (e.g., `CountryManager`) and includes all `FluentValidation` validators. Depends only on `Abstractions`.
* **`GeoApi.Access` (Resource Access Layer):** Handles communication with external resources:
  1. **Persistence:** Includes `DbContext` and repository implementations (e.g., `CountryRepository`).
  2. **Authentication/Authorization:** Manages user data, registration, and login via **ASP.NET Core Identity**.  
  Depends only on `Abstractions`.
* **`GeoApi.Host` (Host Layer):** Main executable project that composes the application, exposes services as a Minimal API, and handles dependency injection and endpoint registration.
* **`GeoApi.Tests.Unit` (Testing):** Unit tests for the Service layer using NSubstitute for mocking dependencies.
* **`GeoApi.Tests.Integration` (Testing):** Integration tests verifying interactions between layers (e.g., repository logic) against a live SQL Server container managed by Testcontainers.
* **`GeoApi.Tests.Acceptance` (Testing):** In-memory acceptance tests validating the full API pipeline (HTTP request → Database) using Testcontainers.

## 🛠️ Technology Stack
* **.NET 9** (C# 13)
* **Minimal APIs**
* **Entity Framework Core** (SQL Server)
* **ASP.NET Core Identity**
* **FluentValidation**
* **FluentResults**
* **Serilog**
* **JWT Bearer Authentication**
* **Testing:**
  * **xUnit**
  * **NSubstitute**
  * **WebApplicationFactory**
  * **Testcontainers**

## 🚀 Getting Started
### Prerequisites
* [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
* SQL Server instance (LocalDB, SQL Express, or Docker)
* [Visual Studio 2022](https://visualstudio.microsoft.com/vs/) or [VS Code](https://code.visualstudio.com/)
* [.NET EF Core Tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)  
  Install using:  
  ```sh
  dotnet tool install --global dotnet-ef

### 1. Installation
```sh
git clone https://github.com/mvieira841/GeoApi.git
cd GeoApi
dotnet restore
```

### 2. Configuration
Edit `appsettings.json` in the `GeoApi.Host` project.

**Connection String:**
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=GeoApiDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}
```

**JWT Settings:**
```json
"JwtSettings": {
  "Issuer": "https://localhost:5001",
  "Audience": "https://localhost:5001",
  "Key": "REPLACE_THIS_WITH_A_SECURE_RANDOM_KEY",
  "DurationInHours": 1
}
```

> **Security Note:** Store sensitive data such as connection strings and JWT keys in **User Secrets** or a secure vault (e.g., Azure Key Vault).

**Serilog Example:**
```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "WriteTo": [{ "Name": "Console" }],
  "Enrich": ["FromLogContext", "WithMachineName"]
}
```

### 3. Apply Database Migrations
```sh
dotnet ef database update --project GeoApi.Access --startup-project GeoApi.Host
```

### 4. Run the API
```sh
dotnet run --project GeoApi.Host
```
Access the API at `https://localhost:5001` and view the Swagger UI at `https://localhost:5001/swagger`.

## 🧪 Testing
This project uses a 3-tier testing strategy: **Unit**, **Integration**, and **Acceptance**.
> **Note:** Docker Desktop (or another OCI-compatible runtime) must be running.

### Run All Tests
```sh
dotnet test
```

### 1. Acceptance Tests (Shared, Seeded Database)
* **Project:** `GeoApi.Tests.Acceptance`
* **Strategy:** Launches one SQL Server container for all tests, applies migrations, and seeds data via `DataSeeder`.
* **Purpose:** Tests the full API flow with predefined users and sample data.

### 2. Integration Tests (Transactional Isolation)
* **Project:** `GeoApi.Tests.Integration`
* **Strategy:** Launches one SQL Server container per run without seeding. Each test runs in a transaction rolled back afterward.
* **Purpose:** Ensures clean, isolated data for each test case—ideal for repository or service logic.

## 🗺️ API Endpoints
### Authentication (`/auth`)
| Method | Path             | Description                                  | Access |
| :----- | :--------------- | :------------------------------------------- | :----- |
| `POST` | `/auth/register` | Registers a new user (default role: `User`). | Public |
| `POST` | `/auth/login`    | Logs in and returns a JWT token.             | Public |

**Register Request**
```json
{
  "firstName": "Test",
  "lastName": "User",
  "userName": "testuser",
  "email": "user@example.com",
  "password": "Password123!"
}
```

**Login Request**
```json
{
  "userName": "testuser",
  "password": "Password123!"
}
```

### Countries 
`/api/v1/countries`

| Method   | Path    | Description                              | Access  |
| :------- | :------ | :--------------------------------------- | :------ |
| `GET`    | `/`     | Retrieves a paginated list of countries. | `User`  |
| `POST`   | `/`     | Creates a new country.                   | `Admin` |
| `GET`    | `/{id}` | Retrieves a country by ID.               | `User`  |
| `PUT`    | `/{id}` | Updates an existing country.             | `Admin` |
| `DELETE` | `/{id}` | Deletes a country.                       | `Admin` |

### Cities
`/api/v1/countries/{countryId}/cities`

| Method   | Path    | Description                              | Access  |
| :------- | :------ | :--------------------------------------- | :------ |
| `GET`    | `/`     | Retrieves cities for a specific country. | `User`  |
| `POST`   | `/`     | Creates a new city for a country.        | `Admin` |
| `GET`    | `/{id}` | Retrieves a city by ID.                  | `User`  |
| `PUT`    | `/{id}` | Updates an existing city.                | `Admin` |
| `DELETE` | `/{id}` | Deletes a city.                          | `Admin` |

## 🧾 License
This project is licensed under the [MIT License](LICENSE).