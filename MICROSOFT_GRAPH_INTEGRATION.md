# Microsoft Graph Integration - Implementation Summary

## Overview
This document explains the Microsoft Graph API integration implemented in the MVC Client application, including the authentication flows and how to resolve common issues.

## Current Implementation

### Authentication Flows

#### 1. App-Only Authentication (Currently Implemented)
- **Used for**: Getting all users, searching users, getting user details
- **How it works**: Uses Azure AD app registration's client secret to authenticate as the application
- **Permissions Required**: Application permissions (e.g., `User.Read.All`)
- **Token Source**: Azure AD directly (Client Credentials flow)
- **Methods**: `GetUsersAsync()`, `GetUserByIdAsync()`, `SearchUsersAsync()`, `GetCurrentUserAsync()`

#### 2. Delegated Authentication (Partially Implemented)
- **Intended for**: Accessing Graph API as the authenticated user
- **Current Status**: Framework in place, but requires On-Behalf-Of (OBO) flow
- **How it should work**: Exchange IdentityServer token for Microsoft Graph token
- **Permissions Required**: Delegated permissions (e.g., `User.Read`)
- **Methods**: `GetCurrentUserWithUserTokenAsync()` (currently falls back to app-only)

## Files Implemented

### Services
- **`IGraphService.cs`**: Interface defining Graph API operations
- **`GraphService.cs`**: Implementation with comprehensive error handling and authentication flows

### Controllers
- **`UsersController.cs`**: Handles user listing, searching, details, and profile views

### Views
- **`Views/Users/Index.cshtml`**: User listing with search functionality
- **`Views/Users/Details.cshtml`**: Individual user details
- **`Views/Users/Me.cshtml`**: Current user's profile

### Navigation
- **`Views/Shared/_Layout.cshtml`**: Updated to include user management links

## Error Resolution

### "invalid_scope" Error

**Problem**: When trying to use IdentityServer access tokens directly with Microsoft Graph API.

**Root Cause**: IdentityServer tokens are not Microsoft Graph tokens. They are meant for your local application, not for Microsoft Graph.

**Current Solution**: The application now uses app-only authentication for all Graph API calls, which works correctly.

**Long-term Solution (Optional)**: Implement On-Behalf-Of (OBO) flow to exchange IdentityServer tokens for Graph tokens.

### Key Changes Made to Fix the Issue

1. **Removed problematic code** that was trying to use IdentityServer tokens directly with Graph API
2. **Added clear error handling** to distinguish between app-only and delegated authentication failures
3. **Updated the "My Profile" feature** to gracefully fall back from delegated to app-only authentication
4. **Added comprehensive documentation** in code comments explaining the authentication flows

## How the Integration Works Now

### User Flow
1. User logs in via IdentityServer (Azure AD authentication)
2. User navigates to "All Users" or "My Profile"
3. Application uses app-only authentication to call Microsoft Graph API
4. Results are displayed with clear indication of authentication method used

### Technical Flow
1. **App-Only Authentication**: Application uses its client secret to get an access token from Azure AD
2. **Graph API Calls**: Uses this app token to call Microsoft Graph API with application permissions
3. **User Identification**: For "My Profile", matches the logged-in user's UPN from IdentityServer claims to Graph API data

## Configuration Required

### Azure AD App Registration
Ensure your Azure AD app registration has:

1. **API Permissions**:
   - Microsoft Graph: `User.Read.All` (Application permission)
   - Grant admin consent for the organization

2. **Authentication**:
   - Client secret configured
   - Appropriate redirect URIs for IdentityServer

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

## Testing the Integration

1. **Start both applications**:
   ```bash
   # Terminal 1
   cd IdentityServer
   dotnet run
   
   # Terminal 2  
   cd MvcClient
   dotnet run
   ```

2. **Test user functionality**:
   - Navigate to MvcClient (typically https://localhost:5002)
   - Log in with Azure AD credentials
   - Click "All Users" to see user listing with search
   - Click "My Profile" to see your own profile information
   - Test search functionality

## Future Enhancements (Optional)

### Implementing On-Behalf-Of (OBO) Flow
If you want true delegated permissions (user acting as themselves):

1. **Azure AD Configuration**:
   - Add Microsoft Graph scopes to your app registration
   - Configure OBO flow permissions

2. **Code Changes**:
   - Implement token exchange in `GetUserGraphClientAsync()`
   - Use Azure.Identity's `OnBehalfOfCredential`
   - Handle scope and consent requirements

3. **Benefits**:
   - Users see only data they have permission to access
   - More granular security model
   - Audit trails show individual user actions

## Current Limitations

1. **App-Only Permissions**: All Graph API calls use application permissions, so users see all organization data they're authorized to see through the app
2. **No User-Specific Scoping**: Cannot limit data based on individual user permissions in Graph API
3. **Token Scope**: IdentityServer tokens cannot be used directly with Microsoft Graph

## Summary

The current implementation successfully integrates Microsoft Graph API using app-only authentication, providing:
- ✅ User listing and search functionality
- ✅ Individual user profile views  
- ✅ Current user profile display
- ✅ Comprehensive error handling
- ✅ Clear authentication flow documentation
- ✅ Graceful fallback mechanisms

The "invalid_scope" error has been resolved by using the appropriate authentication flow for each scenario.
