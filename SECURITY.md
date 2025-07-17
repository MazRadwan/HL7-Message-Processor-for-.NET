# HL7 Processor Security Guidelines

## 🔐 Secrets Management

### Development Environment
- **JWT Secret**: Use environment variable `JWT_SECRET_KEY` with 32+ character random key
- **Database**: Use dedicated `hl7_app` user instead of `sa` for application connections
- **Connection Encryption**: Always use `Encrypt=true;TrustServerCertificate=true` even locally

### Production Environment
- **Azure Key Vault**: Store all secrets in Azure Key Vault
- **Managed Identity**: Use system-assigned managed identity for Key Vault access
- **Connection Strings**: Reference Key Vault secrets via App Service configuration

## 🚫 Security Don'ts

### Never Commit These to Source Control:
- Real JWT secrets or passwords
- Connection strings with credentials
- API keys or tokens
- SA passwords or any production credentials

### Example .env (Development Only):
```bash
# NEVER commit real values - these are examples
JWT_SECRET_KEY=your-secure-random-key-here
HL7_CONNECTION_STRING=Server=localhost,1433;Database=HL7ProcessorDb;User Id=hl7_app;Password=your-app-password;Encrypt=true;TrustServerCertificate=true
```

## 📊 Database Security

### User Separation
- **SA Account**: Admin tasks only, not for application connections
- **hl7_app**: Dedicated application user with db_owner rights to HL7ProcessorDb only
- **Principle of Least Privilege**: Grant minimum required permissions

### Connection Security
- **Always Encrypted**: Use `Encrypt=true` in all environments
- **Certificate Validation**: `TrustServerCertificate=true` for local dev, proper certs in production
- **No Shared Accounts**: Each environment gets unique credentials

## 🏗️ Development Practices

### Seed Data Protection
```csharp
// Only seed in development - never in production
if (!_environment.IsDevelopment())
{
    return; // Skip seeding
}
```

### Secret Rotation
- Rotate JWT secrets regularly
- Change all passwords shown in documentation/chat logs
- Use different passwords for each environment

## 🔍 Security Checklist

### Before Production Deployment:
- [ ] All secrets moved to Azure Key Vault
- [ ] Managed Identity configured for Key Vault access
- [ ] No hardcoded credentials in source code
- [ ] Connection strings use encrypted connections
- [ ] Seed data service disabled in production
- [ ] Application uses dedicated database user (not SA)
- [ ] All demo/development credentials changed
- [ ] Security headers configured (HSTS, etc.)

### Regular Maintenance:
- [ ] JWT secret rotation (quarterly)
- [ ] Database password updates (quarterly)
- [ ] Security dependency updates
- [ ] Access review and cleanup

## 🚨 Incident Response

### If Secrets Are Compromised:
1. **Immediate**: Rotate the compromised secret
2. **Update**: All environments using the secret
3. **Review**: Access logs for unauthorized usage
4. **Document**: Incident and remediation steps

### If Database Access Is Compromised:
1. **Disable**: Compromised user account
2. **Create**: New user with different credentials
3. **Update**: Application connection strings
4. **Audit**: Database access logs

## 📋 Compliance

### Data Protection
- Encrypt sensitive data at rest and in transit
- Implement proper access controls
- Log security events for audit trails
- Regular security assessments

### HL7 Specific
- PHI/PII protection in HL7 messages
- Access logging for healthcare data
- Retention policies for medical records
- HIPAA compliance considerations