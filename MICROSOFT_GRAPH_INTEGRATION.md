# Microsoft Graph Integration - Implementation Summary

## Overview
This document explains the Microsoft Graph API integration implemented in the MVC Client application, including the authentication flows and how to resolve common issues.

## Current Implementation

### Authentication Flows

#### 1. App-Only Authentication (Client Credentials Flow)
- **Used for**: Getting all users, searching users, getting user details (fallback mode)
- **How it works**: Uses Azure AD app registration's client secret to authenticate as the application
- **Permissions Required**: Application permissions (e.g., `User.Read.All`)
- **Token Source**: Azure AD directly (Client Credentials flow)
- **Methods**: `GetUsersAsync()`, `GetUserByIdAsync()`, `SearchUsersAsync()`, `GetCurrentUserAsync()`

#### 2. Delegated Authentication (On-Behalf-Of Flow) ✅ NEW!
- **Used for**: Accessing Graph API as the authenticated user
- **How it works**: Exchanges IdentityServer token for Microsoft Graph token using OBO flow
- **Permissions Required**: Delegated permissions (e.g., `User.Read`, `User.ReadBasic.All`)
- **Token Source**: Exchange IdentityServer token → Azure AD Graph token
- **Methods**: `GetCurrentUserWithUserTokenAsync()`, `GetUsersWithUserTokenAsync()`
- **STATUS**: ✅ Implemented with On-Behalf-Of flow support

## Features Implemented

### Token Switching UI ✅ NEW!
The Users page now includes a toggle between authentication methods:

- **App Token**: Uses application permissions (sees all users the app is authorized for)
- **User Token**: Uses delegated permissions (sees users the logged-in user can access)

### Enhanced Error Handling
- Clear distinction between app-only and delegated authentication failures
- Graceful fallback from user token to app token when OBO flow fails
- Helpful permission error messages with guidance

## Files Implemented

### Services
- **`IGraphService.cs`**: Interface with new `GetUsersWithUserTokenAsync()` method
- **`GraphService.cs`**: Implementation with full OBO flow support using `OnBehalfOfCredential`

### Controllers
- **`UsersController.cs`**: Enhanced Index action with `useUserToken` parameter for switching authentication methods

### Views
- **`Views/Users/Index.cshtml`**: Updated with token switching UI and authentication method display

### Packages Added
- **`Microsoft.Identity.Web`**: Provides `OnBehalfOfCredential` for OBO flow implementation

## Configuration Required

### Azure AD App Registration

#### API Permissions
Your Azure AD app registration must have:

**Application Permissions (for app-only mode)**:
- `User.Read.All` - Read all users' full profiles
- Grant admin consent for the organization

**Delegated Permissions (for user token mode)**:
- `User.Read` - Sign in and read user profile  
- `User.ReadBasic.All` - Read all users' basic profiles
- Grant admin consent for the organization

#### Authentication
- Client secret configured
- Appropriate redirect URIs for IdentityServer
- **✅ NEW**: Support for On-Behalf-Of flow must be enabled

### Application Settings
In `appsettings.Development.json`:
```json
{
  "AzureAd": {
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id", 
    "ClientSecret": "your-client-secret"
  }
}
```

## How to Test the New Functionality

### 1. Start Applications
```bash
# Terminal 1
cd IdentityServer
dotnet run

# Terminal 2  
cd MvcClient
dotnet run
```

### 2. Test Authentication Methods
1. Navigate to MvcClient (typically https://localhost:5002)
2. Log in with Azure AD credentials
3. Go to "All Users" page
4. **✅ NEW**: Use the toggle buttons at the top:
   - **App Token**: Shows users via application permissions
   - **User Token**: Shows users via your personal delegated permissions

### 3. Compare Results
- **App Token mode**: May show more users (based on app permissions)
- **User Token mode**: Shows users you personally have access to read
- Error handling demonstrates permission differences

## Technical Implementation Details

### On-Behalf-Of Flow Implementation
```csharp
// In GetUserGraphClientAsync()
var oboCredential = new OnBehalfOfCredential(
    tenantId,
    clientId,
    clientSecret,
    identityServerToken);

var userGraphClient = new GraphServiceClient(oboCredential, 
    new[] { "https://graph.microsoft.com/.default" });
```

### Token Exchange Process
1. User logs in via IdentityServer (gets IdentityServer token)
2. When accessing Graph API with user token:
   - Extract IdentityServer access token from HTTP context
   - Use `OnBehalfOfCredential` to exchange for Graph token
   - Create new GraphServiceClient with user's permissions
   - Make Graph API calls as the authenticated user

### Error Handling
- **OBO Flow Failures**: Clear error messages about configuration issues
- **Permission Errors**: Distinction between app vs user permission problems
- **Graceful Fallback**: User token methods fall back to app token when OBO fails

## Benefits of User Token Authentication

### Security Benefits
- **Principle of Least Privilege**: Users only see data they have personal access to
- **Audit Trail**: Graph API calls are attributed to individual users
- **Permission Boundaries**: Respects individual user permissions in Azure AD

### User Experience
- **Personalized Results**: Users see data relevant to their role/permissions
- **Transparent Authentication**: Clear indication of which token type is being used
- **Flexible Access**: Can switch between app-wide and personal data views

## Troubleshooting

### "OBO flow failed" Errors
1. **Check Azure AD App Registration**:
   - Ensure client secret is valid
   - Verify delegated permissions are granted
   - Confirm admin consent is provided

2. **Check Token Configuration**:
   - Verify IdentityServer is saving tokens (`SaveTokens = true`)
   - Ensure access tokens are available in HTTP context

3. **Check Logs**:
   - Review application logs for detailed OBO flow error messages
   - Check Azure AD sign-in logs for permission issues

### Permission Errors
- **User Token Mode**: User may not have `User.ReadBasic.All` permission
- **App Token Mode**: App may not have `User.Read.All` permission
- **Solution**: Ensure both application and delegated permissions are properly configured

## Current Status

✅ **Fully Implemented**:
- App-only authentication (Client Credentials)
- Delegated authentication (On-Behalf-Of flow)
- Token switching UI
- Comprehensive error handling
- Graceful fallback mechanisms

✅ **Working Features**:
- User listing with both authentication methods
- Individual user profiles
- Search functionality
- Clear authentication method indication
- Permission error guidance

The Microsoft Graph integration now supports both application-only and delegated user authentication flows, providing flexibility and security appropriate for different use cases.
