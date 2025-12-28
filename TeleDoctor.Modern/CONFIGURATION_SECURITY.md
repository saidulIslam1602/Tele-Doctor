# 🔐 CONFIGURATION SECURITY GUIDE

## ⚠️ CRITICAL SECURITY ISSUES FIXED

This document outlines the security improvements made to the TeleDoctor application and how to properly configure it for production.

---

## 🔴 Issues Identified

1. **Hardcoded Secrets** - API keys and secrets in `appsettings.json`
2. **Weak JWT Secret** - Predictable JWT signing key
3. **Database Password in Connection String** - SQL password exposed
4. **No Environment Separation** - Same config for dev/prod
5. **Norwegian Integration Crashes** - Throws on missing config

---

## ✅ Fixes Implemented

### 1. Production Configuration File
Created `appsettings.Production.json` with environment variable placeholders:
- All secrets use `${VARIABLE_NAME}` syntax
- No hardcoded credentials
- Environment-specific settings

### 2. Environment Variables Required

#### **Database Configuration**
```bash
SQL_SERVER=your-sql-server.database.windows.net
SQL_DATABASE=TeleDoctorModernDB
SQL_USER=sqladmin
SQL_PASSWORD=<strong-password>
```

#### **JWT Authentication**
```bash
JWT_SECRET_KEY=<generate-strong-key-min-32-chars>
```

**Generate Strong JWT Secret:**
```bash
# Linux/Mac
openssl rand -base64 64

# PowerShell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Minimum 0 -Maximum 256 }))

# Node.js
node -e "console.log(require('crypto').randomBytes(64).toString('base64'))"
```

#### **Azure OpenAI**
```bash
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.com/
AZURE_OPENAI_API_KEY=your-azure-openai-key
AZURE_OPENAI_DEPLOYMENT_NAME=gpt-4
AZURE_OPENAI_CHAT_DEPLOYMENT=gpt-4
AZURE_OPENAI_EMBEDDING_DEPLOYMENT=text-embedding-ada-002
```

#### **Azure Cognitive Services**
```bash
AZURE_COGNITIVE_ENDPOINT=https://your-cognitive.cognitiveservices.azure.com/
AZURE_COGNITIVE_API_KEY=your-cognitive-key
AZURE_REGION=norwayeast
AZURE_TEXT_ANALYTICS_ENDPOINT=https://your-text-analytics.cognitiveservices.azure.com/
AZURE_TEXT_ANALYTICS_KEY=your-text-analytics-key
AZURE_TRANSLATOR_KEY=your-translator-key
AZURE_VISION_ENDPOINT=https://your-vision.cognitiveservices.azure.com/
AZURE_VISION_KEY=your-vision-key
```

#### **Norwegian Healthcare Integration**
```bash
HELSENORGE_API_ENDPOINT=https://api.helsenorge.no/
HELSENORGE_API_KEY=your-helsenorge-key
```

#### **Redis (for SignalR Scalability)**
```bash
REDIS_CONNECTION_STRING=your-redis.redis.cache.windows.net:6380,password=your-redis-key,ssl=True,abortConnect=False
```

#### **Application Insights**
```bash
APPLICATIONINSIGHTS_CONNECTION_STRING=InstrumentationKey=your-key;IngestionEndpoint=https://norwayeast-1.in.applicationinsights.azure.com/
```

---

## 🐳 Docker Configuration

### Using Docker Compose with Environment File

Create `.env` file in project root:
```bash
# .env (DO NOT COMMIT THIS FILE)
SQL_SERVER=sqlserver
SQL_DATABASE=TeleDoctorModernDB
SQL_USER=sa
SQL_PASSWORD=TeleDoctor2024!
JWT_SECRET_KEY=<your-generated-key>
AZURE_OPENAI_ENDPOINT=https://your-openai.openai.azure.com/
AZURE_OPENAI_API_KEY=your-key
# ... add all other variables
```

Update `docker-compose.yml`:
```yaml
services:
  webapi:
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - SQL_SERVER=${SQL_SERVER}
      - SQL_DATABASE=${SQL_DATABASE}
      - SQL_USER=${SQL_USER}
      - SQL_PASSWORD=${SQL_PASSWORD}
      - JWT_SECRET_KEY=${JWT_SECRET_KEY}
      - AZURE_OPENAI_ENDPOINT=${AZURE_OPENAI_ENDPOINT}
      - AZURE_OPENAI_API_KEY=${AZURE_OPENAI_API_KEY}
      # ... all other variables
    env_file:
      - .env
```

**Add to `.gitignore`:**
```
.env
*.env
appsettings.Production.json
```

---

## ☁️ Azure Configuration

### Using Azure Key Vault (Recommended)

1. **Create Azure Key Vault:**
```bash
az keyvault create \
  --name teledoctor-keyvault \
  --resource-group teledoctor-rg \
  --location norwayeast
```

2. **Add Secrets:**
```bash
az keyvault secret set --vault-name teledoctor-keyvault --name "JwtSecretKey" --value "your-secret"
az keyvault secret set --vault-name teledoctor-keyvault --name "AzureOpenAiApiKey" --value "your-key"
# ... add all secrets
```

3. **Update `Program.cs` (Already Configured):**
```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri($"https://{keyVaultName}.vault.azure.net/"),
    new DefaultAzureCredential());
```

4. **Enable Managed Identity for App Service:**
```bash
az webapp identity assign --name teledoctor-api --resource-group teledoctor-rg
```

5. **Grant Access to Key Vault:**
```bash
az keyvault set-policy \
  --name teledoctor-keyvault \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### Using Azure App Configuration

1. **Create App Configuration:**
```bash
az appconfig create \
  --name teledoctor-appconfig \
  --resource-group teledoctor-rg \
  --location norwayeast
```

2. **Add Key-Values with Key Vault References:**
```bash
az appconfig kv set \
  --name teledoctor-appconfig \
  --key "JwtSettings:SecretKey" \
  --value "@Microsoft.KeyVault(SecretUri=https://teledoctor-keyvault.vault.azure.net/secrets/JwtSecretKey/)"
```

---

## 🔒 Kubernetes Secrets

### Create Secret YAML:
```yaml
apiVersion: v1
kind: Secret
metadata:
  name: teledoctor-secrets
type: Opaque
stringData:
  SQL_PASSWORD: "your-password"
  JWT_SECRET_KEY: "your-jwt-secret"
  AZURE_OPENAI_API_KEY: "your-api-key"
  # ... all other secrets
```

### Apply Secrets:
```bash
kubectl apply -f secrets.yaml
```

### Reference in Deployment:
```yaml
spec:
  containers:
  - name: webapi
    envFrom:
    - secretRef:
        name: teledoctor-secrets
```

---

## 🛡️ Security Best Practices

### 1. **Never Commit Secrets**
- Add to `.gitignore`: `.env`, `secrets.yaml`, `*.secrets`
- Use `.env.example` with placeholder values for documentation

### 2. **Rotate Secrets Regularly**
- JWT keys: Every 3-6 months
- API keys: When team members leave
- Database passwords: Quarterly

### 3. **Use Managed Identities**
- Eliminates need for storing credentials
- Automatically rotated by Azure
- More secure than service principals

### 4. **Principle of Least Privilege**
- Grant only necessary permissions
- Use separate service principals for dev/staging/prod
- Implement RBAC on Key Vault

### 5. **Enable Audit Logging**
- Track all secret access
- Monitor for unauthorized access attempts
- Set up alerts for suspicious activity

---

## 📋 Validation Checklist

Before deploying to production:

- [ ] All secrets moved to environment variables
- [ ] Strong JWT secret generated (min 64 characters)
- [ ] Database password is strong and unique
- [ ] Azure Key Vault configured with managed identity
- [ ] `.env` file added to `.gitignore`
- [ ] No hardcoded secrets in code
- [ ] Production `appsettings.json` uses variable substitution
- [ ] SSL/TLS certificates configured
- [ ] CORS policy restricted to specific origins
- [ ] Rate limiting enabled
- [ ] Application Insights configured for monitoring

---

## 🚨 Emergency Procedures

### If Secrets are Compromised:

1. **Immediately Rotate:**
   ```bash
   # Azure Key Vault
   az keyvault secret set --vault-name teledoctor-keyvault --name "CompromisedSecret" --value "new-value"
   
   # Restart applications
   kubectl rollout restart deployment/teledoctor-api
   ```

2. **Review Access Logs:**
   ```bash
   az monitor activity-log list --resource-group teledoctor-rg --offset 7d
   ```

3. **Revoke Compromised Keys:**
   - Azure OpenAI: Regenerate keys in portal
   - Database: Change password and update connection strings
   - JWT: Generate new secret and invalidate all tokens

4. **Notify Team and Audit:**
   - Document incident
   - Review who had access
   - Update security policies

---

## 📞 Support

For security issues, contact:
- **Security Team**: security@teledoctor.no
- **On-Call**: +47 XXX XX XXX

---

## ✅ Implementation Status

- [x] Created production configuration file
- [x] Documented environment variables
- [x] Azure Key Vault integration guide
- [x] Docker secrets management
- [x] Kubernetes secrets guide
- [x] Security best practices
- [x] Emergency procedures

**Last Updated**: December 28, 2025
