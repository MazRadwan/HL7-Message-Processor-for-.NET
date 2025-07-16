# HL7 Processor & Integration Platform

A **.NET 8**-based, end-to-end toolkit for parsing, transforming, routing and persisting **HL7 v2** healthcare messages.  
Built for hospitals, laboratories and integrators who need modern, test-driven interoperability without vendor lock-in.

<p align="center">
  <img src="docs/architecture-diagram.svg" width="650" alt="HL7 Processor – High-Level Architecture"/>
</p>

---
## ✨ Key Features

| Area | Highlights |
|------|------------|
| **Parsing & Validation** | • Robust HL7 v2 parser (dynamic delimiters, escape handling)<br/>• Segment/field/component models with Data-Annotation validation<br/>• Customizable validation levels (lenient ⇄ strict) |
| **Transformation & Mapping** | • Declarative field-mapping DSL<br/>• Bidirectional HL7 ↔ JSON/XML/FHIR R4 conversion<br/>• Rule engine (conditional, calculated, lookup) |
| **Transport** | • Async MLLP **client & server** with ACK/NACK handling<br/>• Connection pooling, idle-timeout management |
| **Storage** | • EF Core persistence (SQL Server by default)<br/>• Repository pattern + LINQ querying<br/>• Data-retention service & archiving hook |
| **Monitoring API** | • ASP.NET Core Web API (`/api`) for message submission & querying<br/>• SignalR hub for real-time UI updates |
| **Security** | • JWT-based auth (ASP.NET Identity-ready)<br/>• Audit logging via EF Core interceptors |
| **Dev Experience** | • Clean, SOLID core library<br/>• 200+ unit/integration tests (xUnit / FluentAssertions)<br/>• One-command Docker compose for DB/demo server |

---
## 🌍 Project Layout

```
src/
 ├─ HL7Processor.Core/          # Domain + business logic
 ├─ HL7Processor.Infrastructure/ # EF Core, repositories, audit
 ├─ HL7Processor.Api/           # ASP.NET Core REST & SignalR
 └─ HL7Processor.Console/       # CLI & diagnostics

tests/
 ├─ HL7Processor.Tests/         # Unit & integration tests
 └─ ...
```

---
## 🚀 Getting Started

### Prerequisites
* .NET 8 SDK
* SQL Server (localdb, Docker or remote) **– optional**

### Clone & Build
```bash
# clone
git clone https://github.com/your-org/hl7-processor.git && cd hl7-processor

# restore & build
dotnet build -c Release
```

### Run API (development)
```bash
# update connection string in src/HL7Processor.Api/appsettings.Development.json if needed
cd src/HL7Processor.Api

dotnet run

# 👉 API:   https://localhost:5001/api
# 👉 Swagger: https://localhost:5001/swagger
```

### Run Tests
```bash
cd tests

dotnet test                 # unit + integration

dotnet test --collect:"XPlat Code Coverage"  # coverage report
```

### Docker Quick-Start *(optional)*
```bash
docker compose up -d  # spins up SQL Server + API
```

---
## ⚙️ Configuration

| File | Purpose |
|------|---------|
| `appsettings.json` | Global defaults |
| `appsettings.Development.json` | Overrides for dev environment |
| `appsettings.Production.json`  | Production settings (no secrets) |
| **Environment Variables** | `ConnectionStrings__Hl7Db`, `Jwt__SecretKey`, … |

> Use **dotnet user-secrets** in development to avoid committing secrets.

---
## 🛠 Development Workflow
1. **Branch** from `main` → `feature/<name>`
2. Write code & **unit tests** (run `dotnet test`)
3. `git commit -s` with Conventional Commits
4. Open Pull Request → CI runs tests + analyzers
5. Code review & squash-merge

---
## 📈 Roadmap
- [ ] FHIR R4 resource enrichment
- [ ] RabbitMQ / Azure Service Bus adapters
- [ ] Dashboard UI (Blazor)
- [ ] Kubernetes Helm chart

---
## 🤝 Contributing
Contributions are welcome!  Please read [`CONTRIBUTING.md`](docs/CONTRIBUTING.md) first and open an issue to discuss major changes.

---
## 🔒 Security
If you discover a security vulnerability please **do not** create a public issue.  Email `security@your-org.com` and we will respond promptly.

---
## 📄 License

This project is licensed under the **MIT License** – see [`LICENSE`](LICENSE) for details.