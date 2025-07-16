# HL7 Message Processing System

A C# .NET-based HL7 message processing and integration system for healthcare interoperability.

## Project Structure

```
HL7/
├── src/
│   ├── HL7Processor.Core/        # Core library with business logic
│   └── HL7Processor.Console/     # Console application
├── tests/
│   └── HL7Processor.Tests/       # xUnit test projects
├── config/                       # Configuration files
├── docs/                        # Documentation
└── scripts/                     # Build and deployment scripts
```

## Getting Started

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or Visual Studio Code

### Installation
```bash
dotnet restore
```

### Development
```bash
dotnet run --project src/HL7Processor.Console
```

### Testing
```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

### Build
```bash
dotnet build
dotnet build --configuration Release
```

## Features

- HL7 message parsing and validation using C#
- Data transformation and mapping with LINQ
- MLLP protocol support with System.Net.Sockets
- Real-time message processing with background services
- ASP.NET Core web-based monitoring interface
- Comprehensive logging and auditing with ILogger

## Configuration

Configuration is managed through:
- `appsettings.json` - Base configuration
- `appsettings.Development.json` - Development environment settings
- `appsettings.Production.json` - Production environment settings
- Environment variables and user secrets for sensitive data

## Contributing

1. Follow the existing code style
2. Write tests for new features
3. Update documentation as needed
4. Submit pull requests for review

## License

MIT License