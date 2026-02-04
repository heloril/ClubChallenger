# Facebook Integration Architecture

## System Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    ClubChallenger Application                │
│                                                               │
│  ┌──────────────┐         ┌─────────────┐                   │
│  │ Races.cshtml │────────▶│RacesModel.cs│                   │
│  │  (View)      │         │ (Page Model)│                   │
│  └──────────────┘         └──────┬──────┘                   │
│        │                          │                          │
│        │ User Clicks              │ Calls                    │
│        │ "Share" Button           │                          │
│        │                          ▼                          │
│        │                  ┌───────────────┐                 │
│        │                  │FacebookService│                 │
│        │                  └───────┬───────┘                 │
│        │                          │                          │
│        └──────────────────────────┘                          │
│                                   │                          │
└───────────────────────────────────┼──────────────────────────┘
                                    │
                                    │ HTTPS
                                    │
                        ┌───────────▼──────────┐
                        │  Facebook Graph API  │
                        │     (v18.0)          │
                        └───────────┬──────────┘
                                    │
                        ┌───────────▼──────────┐
                        │   Your Facebook Page │
                        │   (Race Results)     │
                        └──────────────────────┘
```

## Data Flow

### 1. User Initiates Share

```
User ───► [Share Button] ───► OnPostShareToFacebookAsync()
                                       │
                                       ├─ Get Race Details
                                       ├─ Get Top 3 Results
                                       ├─ Build Summary Text
                                       └─ Call FacebookService
```

### 2. Facebook Service Processing

```
FacebookService.PostRaceResultsAsync()
    │
    ├─ Validate Configuration
    ├─ Format Message
    │   └─ Add emojis, links, formatting
    │
    ├─ Choose Post Type
    │   ├─ Text + Link (if no image)
    │   └─ Photo + Caption (if image provided)
    │
    ├─ Make HTTP POST to Facebook
    │   └─ Endpoint: /v18.0/{pageId}/feed or /photos
    │
    └─ Return Response
        ├─ Success: Post ID and URL
        └─ Error: Error message
```

### 3. Facebook Posts to Page

```
Facebook Graph API
    │
    ├─ Validates Access Token
    ├─ Checks Permissions
    ├─ Creates Post
    │   ├─ Message text
    │   ├─ Link preview
    │   └─ Optional image
    │
    └─ Returns Post ID
```

## Authentication Flow

### Getting Page Access Token (One-Time Setup)

```
1. Developer ──────────┐
                       │
2. Create Facebook App │
                       │
3. Configure OAuth ────┤
                       │
4. Generate User Token │
   (Short-lived)       │
                       │
5. Exchange for        │
   Long-lived Token ───┤
                       │
6. Get Page Token      │
   (Never expires) ────┘
                       │
7. Store in Config ────┘
```

### Using Page Access Token (Runtime)

```
Application Startup
    │
    ├─ Load Configuration
    │   └─ Read PageAccessToken from:
    │       ├─ User Secrets (dev)
    │       ├─ Environment Variables (prod)
    │       └─ Azure Key Vault (prod)
    │
    └─ Inject into FacebookService
        │
        └─ Use for all API calls
            └─ Token never expires!
```

## Configuration Hierarchy

```
┌─────────────────────────────────────┐
│      appsettings.json                │
│  (Default/Template values)           │
│  ┌─────────────────────────────────┐│
│  │ Facebook:                        ││
│  │   AppId: "YOUR_APP_ID"          ││
│  │   AppSecret: "YOUR_APP_SECRET"  ││
│  │   PageId: "YOUR_PAGE_ID"        ││
│  │   PageAccessToken: "YOUR_TOKEN" ││
│  └─────────────────────────────────┘│
└────────────┬────────────────────────┘
             │
             │ Overridden by ↓
             │
┌────────────▼────────────────────────┐
│      User Secrets (Development)     │
│  Stored in:                          │
│  %APPDATA%\Microsoft\UserSecrets\   │
│  ┌─────────────────────────────────┐│
│  │ {                                ││
│  │   "Facebook:AppId": "123..."    ││
│  │   "Facebook:AppSecret": "abc.." ││
│  │   ...                            ││
│  │ }                                ││
│  └─────────────────────────────────┘│
└────────────┬────────────────────────┘
             │
             │ OR (Production) ↓
             │
┌────────────▼────────────────────────┐
│   Environment Variables (Production)│
│  ┌─────────────────────────────────┐│
│  │ Facebook__AppId=123...          ││
│  │ Facebook__AppSecret=abc...      ││
│  │ Facebook__PageId=456...         ││
│  │ Facebook__PageAccessToken=...   ││
│  └─────────────────────────────────┘│
└─────────────────────────────────────┘
```

## Component Dependencies

```
┌─────────────────────┐
│   Program.cs        │
│  (DI Container)     │
└──────────┬──────────┘
           │ Registers
           │
           ├─────────────────┐
           │                 │
           ▼                 ▼
┌──────────────────┐  ┌─────────────────┐
│ FacebookService  │  │ FacebookSettings│
│                  │◄─┤  (IOptions)     │
└──────────────────┘  └─────────────────┘
           │                 ▲
           │ Uses            │ Bound from
           │                 │
           ▼                 │
┌──────────────────┐  ┌─────┴──────────┐
│   HttpClient     │  │ Configuration   │
│                  │  │  (appsettings,  │
│                  │  │   secrets, env) │
└──────────────────┘  └─────────────────┘
```

## Facebook API Endpoints Used

### Text Post
```
POST https://graph.facebook.com/v18.0/{PAGE_ID}/feed

Headers:
  Content-Type: application/json

Body:
  {
    "message": "Race results text...",
    "link": "https://yoursite.com/races?id=123"
  }

Query:
  access_token={PAGE_ACCESS_TOKEN}
```

### Photo Post
```
POST https://graph.facebook.com/v18.0/{PAGE_ID}/photos

Headers:
  Content-Type: multipart/form-data

Body (multipart):
  - source: [binary image data]
  - caption: "Race results text..."

Query:
  access_token={PAGE_ACCESS_TOKEN}
```

## Security Layers

```
┌─────────────────────────────────────────────┐
│ 1. Source Control                            │
│    ✓ .gitignore includes secrets            │
│    ✓ appsettings.json has placeholders only │
└────────────┬────────────────────────────────┘
             │
┌────────────▼────────────────────────────────┐
│ 2. Development                               │
│    ✓ User Secrets stored outside project    │
│    ✓ Not committed to Git                   │
└────────────┬────────────────────────────────┘
             │
┌────────────▼────────────────────────────────┐
│ 3. Production                                │
│    ✓ Environment variables                  │
│    ✓ Azure Key Vault / AWS Secrets Manager  │
│    ✓ Managed identities                     │
└────────────┬────────────────────────────────┘
             │
┌────────────▼────────────────────────────────┐
│ 4. Transport                                 │
│    ✓ HTTPS only                             │
│    ✓ TLS 1.2+                               │
└────────────┬────────────────────────────────┘
             │
┌────────────▼────────────────────────────────┐
│ 5. Facebook API                              │
│    ✓ Token validation                       │
│    ✓ Permission checks                      │
│    ✓ Rate limiting                          │
└─────────────────────────────────────────────┘
```

## Error Handling Flow

```
Try to Post to Facebook
    │
    ├─ Configuration Check
    │   └─ Token missing? ──► Return error to user
    │
    ├─ Build HTTP Request
    │   └─ Invalid data? ──► Return error to user
    │
    ├─ Send to Facebook
    │   ├─ Network error? ──► Log + Return error
    │   ├─ 401 Unauthorized? ──► "Invalid token"
    │   ├─ 403 Forbidden? ──► "Permission denied"
    │   ├─ 429 Rate limit? ──► "Too many requests"
    │   └─ Other error? ──► Log + Return error
    │
    └─ Success
        ├─ Parse response
        ├─ Get Post ID
        ├─ Log success
        └─ Return success to user
```

## Typical Post Lifecycle

```
1. Race Processed ──► Database Updated
                           │
2. User navigates to ──────┘
   Races page
                           │
3. User clicks Share ──────┘
   (Confirmation dialog)
                           │
4. Form submitted ─────────┘
   (POST request)
                           │
5. RacesModel receives ────┘
   request
                           │
6. Fetches race data ──────┘
   from database
                           │
7. Builds summary ─────────┘
   (top 3, count, etc)
                           │
8. Calls FacebookService ──┘
                           │
9. HTTP POST to Facebook ──┘
                           │
10. Facebook processes ────┘
    and publishes
                           │
11. Returns Post ID ───────┘
                           │
12. Success message ───────┘
    shown to user
                           │
13. Post visible on ───────┘
    Facebook page
```

## Permissions Model

```
┌────────────────────────────────────────┐
│          Facebook App                  │
│  (Your ClubChallenger App)             │
└────────────┬───────────────────────────┘
             │
             │ Has permissions:
             │
┌────────────▼───────────────────────────┐
│  pages_show_list                       │
│  Read list of pages you manage         │
└────────────┬───────────────────────────┘
             │
┌────────────▼───────────────────────────┐
│  pages_read_engagement                 │
│  Read page data and engagement         │
└────────────┬───────────────────────────┘
             │
┌────────────▼───────────────────────────┐
│  pages_manage_posts                    │
│  Create, edit, and delete posts        │
└────────────┬───────────────────────────┘
             │
             │ Can act on behalf of:
             │
┌────────────▼───────────────────────────┐
│        Your Facebook Page              │
│  (Running Club Page)                   │
│  - Create posts                        │
│  - Upload photos                       │
│  - Include links                       │
└────────────────────────────────────────┘
```

## Summary

This architecture provides:
- ✅ **Clean separation** - UI, business logic, API service
- ✅ **Security** - Secrets never in source control
- ✅ **Flexibility** - Easy to extend to other platforms
- ✅ **Maintainability** - Well-structured, documented code
- ✅ **Testability** - Services can be mocked
- ✅ **Production-ready** - Error handling, logging, configuration
