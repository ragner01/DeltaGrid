# Environment Variables Setup

This document describes the environment variables needed for the DeltaGrid IOC application.

## Local Development

### Docker Compose

Create a `.env` file in the `infrastructure/` directory with:

```bash
SQL_SERVER_PASSWORD=YourSecurePasswordHere
```

### Application Configuration

Create a `.env` file in the project root with:

```bash
# SQL Server Configuration
SQL_SERVER_PASSWORD=YourSecurePasswordHere

# Azure Configuration (for production)
AZURE_CLIENT_ID=
AZURE_TENANT_ID=
AZURE_SUBSCRIPTION_ID=
AZURE_KEY_VAULT_URL=

# Azure Container Registry
ACR_LOGIN_SERVER=
ACR_USERNAME=
ACR_PASSWORD=

# Database Connection Strings
DEV_SQL_CONNECTION_STRING=Server=localhost,1433;Database=DeltaGrid;User Id=sa;Password=${SQL_SERVER_PASSWORD};TrustServerCertificate=true;
STAGING_SQL_CONNECTION_STRING=
PROD_SQL_CONNECTION_STRING=

# Event Hubs / Kafka
EVENT_HUBS_CONNECTION_STRING=
KAFKA_BOOTSTRAP_SERVERS=localhost:9092

# Azure Search / Cognitive Search
AZURE_SEARCH_ENDPOINT=
AZURE_SEARCH_API_KEY=

# Application Insights
APPLICATIONINSIGHTS_CONNECTION_STRING=

# JWT Configuration
JWT_SECRET_KEY=
JWT_ISSUER=
JWT_AUDIENCE=

# PI System (if used)
PI_BEARER_TOKEN=

# Other API Keys
EXTERNAL_API_KEY=
```

## Important Security Notes

- **Never commit `.env` files to version control**
- All `.env` files are excluded via `.gitignore`
- Use Azure Key Vault for production secrets
- Use managed identities where possible in Azure
- Rotate secrets regularly according to security policy

