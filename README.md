# Azure AD + Duende IdentityServer + MVC Client Demo

This demo shows how to set up:
- **MVC Client** (port 5002) → **Duende IdentityServer** (port 5001) → **Microsoft Entra ID**

## Architecture

```
[ MVC Client ] --OIDC--> [ Duende IdentityServer ] --OIDC--> [ Microsoft Entra ID ]
    :5002                       :5001                          (Azure)
```

- Duende IdentityServer acts as your central OpenID Connect (OIDC) authority
- Microsoft Entra ID (formerly Azure Active Directory) is configured as an external identity provider
- MVC client authenticates only with your Duende IdentityServer, which in turn redirects to Entra ID for login

## Prerequisites

1. .NET 8 SDK
2. Azure AD tenant with app registrations configured

## Azure AD Setup

### 1. Create App Registration for IdentityServer

1. Go to **Azure Portal** → **Azure Active Directory** → **App registrations**
2. Click **New registration**
3. Name: `DemoIdentityServer`
4. Redirect URI: `https://localhost:5001/signin-oidc`
5. Click **Register**
6. Note down the **Application (client) ID** and **Directory (tenant) ID**
7. Go to **Certificates & secrets** → **New client secret**
8. Copy the secret value immediately (you won't see it again)

### 2. Configure App Registration

1. Go to **Authentication**
2. Add redirect URI: `https://localhost:5001/signin-oidc`
3. Add logout URL: `https://localhost:5001/signout-callback-oidc`
4. Enable **ID tokens** under **Implicit grant and hybrid flows**

## Configuration

### Update IdentityServer Configuration

Update `IdentityServer/appsettings.json`:

```json
{
  "AzureAd": {
    "TenantId": "YOUR_TENANT_ID",
    "ClientId": "YOUR_CLIENT_ID", 
    "ClientSecret": "YOUR_CLIENT_SECRET"
  }
}
```

Replace:
- `YOUR_TENANT_ID` with your Azure AD tenant ID
- `YOUR_CLIENT_ID` with your Azure AD app registration client ID
- `YOUR_CLIENT_SECRET` with your Azure AD app registration client secret

## Running the Application

### Start Both Applications

**Option 1: Using Visual Studio**
- Set both projects as startup projects
- Press F5

**Option 2: Using Command Line**

Terminal 1 (IdentityServer):
```bash
cd IdentityServer
dotnet run --urls https://localhost:5001
```

Terminal 2 (MVC Client):
```bash
cd MvcClient
dotnet run --urls https://localhost:5002
```

## Testing the Flow

1. Navigate to `https://localhost:5002`
2. Click **Log In**
3. You'll be redirected to IdentityServer (`https://localhost:5001`)
4. IdentityServer will redirect you to Azure AD login
5. After successful Azure AD authentication, you'll be redirected back to IdentityServer
6. Finally, you'll be redirected back to the MVC client with authentication

## URLs

- **MVC Client**: https://localhost:5002
- **IdentityServer**: https://localhost:5001
- **IdentityServer Discovery**: https://localhost:5001/.well-known/openid_configuration

## Key Components

### IdentityServer (Port 5001)
- Duende IdentityServer with Azure AD as external provider
- Acts as OIDC authority for the MVC client
- Handles the federation with Azure AD

### MVC Client (Port 5002)
- Standard ASP.NET Core MVC application
- Configured to use IdentityServer for authentication
- Shows authenticated user information and claims

## Features Demonstrated

- ✅ OIDC authentication flow
- ✅ External identity provider integration (Azure AD)
- ✅ Claims-based authentication
- ✅ Secure token handling
- ✅ Sign out functionality

## Troubleshooting

### Common Issues

1. **Certificate errors**: Make sure to trust the development certificates:
   ```bash
   dotnet dev-certs https --trust
   ```

2. **Port conflicts**: Ensure ports 5001 and 5002 are available

3. **Azure AD configuration**: Double-check redirect URIs match exactly

4. **HTTPS required**: Both applications must run on HTTPS in production

## Production Considerations

- Use proper certificates (not development certificates)
- Store secrets securely (Azure Key Vault, etc.)
- Configure proper CORS policies
- Enable logging and monitoring
- Use persistent storage for IdentityServer configuration
