# HL7 Processor Deployment Guide

## Environment Variables Required

### Development
Copy `.env.example` to `.env` and configure:

```bash
# Required Environment Variables
JWT_SECRET_KEY=your-jwt-secret-key-at-least-32-characters-long-and-secure
HL7_CONNECTION_STRING=Server=(localdb)\mssqllocaldb;Database=HL7ProcessorDb;Trusted_Connection=true;MultipleActiveResultSets=true
```

### Production (Azure App Service)

#### Application Settings (Environment Variables)
Configure these in Azure Portal > App Service > Configuration > Application Settings:

```
JWT_SECRET_KEY=<your-secure-jwt-secret-from-key-vault>
HL7_CONNECTION_STRING=<azure-sql-connection-string>
ASPNETCORE_ENVIRONMENT=Production
```

#### Azure SQL Database Connection String Format
```
Server=tcp:your-server.database.windows.net,1433;Database=HL7ProcessorDb;User ID=your-user;Password=your-password;Encrypt=true;Connection Timeout=30;
```

## Azure Key Vault Integration (Recommended for Production)

### 1. Create Key Vault and Store Secrets
```bash
# Create Key Vault
az keyvault create --name hl7-processor-kv --resource-group your-rg --location eastus

# Store JWT Secret
az keyvault secret set --vault-name hl7-processor-kv --name JWT-SECRET-KEY --value "your-secure-secret"

# Store Database Connection String
az keyvault secret set --vault-name hl7-processor-kv --name HL7-CONNECTION-STRING --value "your-connection-string"
```

### 2. Configure App Service Managed Identity
```bash
# Enable system-assigned managed identity
az webapp identity assign --name your-app-name --resource-group your-rg

# Grant access to Key Vault
az keyvault set-policy --name hl7-processor-kv --object-id <managed-identity-object-id> --secret-permissions get
```

### 3. Reference Key Vault in App Settings
```
JWT_SECRET_KEY=@Microsoft.KeyVault(VaultName=hl7-processor-kv;SecretName=JWT-SECRET-KEY)
HL7_CONNECTION_STRING=@Microsoft.KeyVault(VaultName=hl7-processor-kv;SecretName=HL7-CONNECTION-STRING)
```

## Security Checklist

### Before Production Deployment
- [ ] JWT secret moved to Key Vault or secure environment variable
- [ ] Database connection string uses SQL authentication (not Windows auth)
- [ ] Demo authentication credentials removed from code
- [ ] HTTPS enforced (HSTS enabled)
- [ ] Allowed hosts configured for production domain
- [ ] Logging level set to Warning/Error for production
- [ ] Application Insights configured for monitoring

### Application Settings Validation
The application will fail to start if required environment variables are missing:
- `JWT_SECRET_KEY` - Required for JWT token signing
- `HL7_CONNECTION_STRING` - Required for database connectivity

### Demo vs Production Authentication
- **Development**: Can use demo credentials if API is not available
- **Production**: Must use real authentication API endpoints
  - `/api/auth/login` - Login endpoint
  - `/api/auth/logout` - Logout endpoint  
  - `/api/auth/validate` - Token validation
  - `/api/auth/user` - Current user info

## Deployment Commands

### Local Development
```bash
# Set environment variables
export JWT_SECRET_KEY="your-dev-secret-at-least-32-chars"
export HL7_CONNECTION_STRING="Server=(localdb)\mssqllocaldb;Database=HL7ProcessorDb;Trusted_Connection=true"

# Run application
dotnet run --project src/HL7Processor.Web
```

### Azure App Service Deployment
```bash
# Build and publish
dotnet publish src/HL7Processor.Web -c Release -o ./publish

# Deploy to Azure (using Azure CLI)
az webapp deployment source config-zip --resource-group your-rg --name your-app-name --src publish.zip
```

## Database Migration
```bash
# Update database schema
dotnet ef database update --project src/HL7Processor.Infrastructure --startup-project src/HL7Processor.Web
```