# Testing Organization Customizations

This guide walks through testing the multi-organization features in BlazorChat.

## Prerequisites

1. **Azure Cosmos DB** - Ensure you have a Cosmos DB instance configured in `appsettings.json`
2. **Azure Login** - Run `az login` to authenticate (required for DefaultAzureCredential)
3. **.NET 10 SDK** installed

---

## Step 1: Start the Application

```bash
cd src/chat
dotnet run
```

The application will start on:
- **HTTPS**: https://localhost:5001
- **HTTP**: http://localhost:5000

---

## Step 2: Register a Test User

1. Navigate to: https://localhost:5001
2. Click **Register** in the top-right
3. Create a test account:
   - Email: `admin@test.com`
   - Password: `Test123!`
4. Check the console/logs for the email confirmation link (or if email confirmation is disabled, you can log in directly)

---

## Step 3: Access the Admin Panel

> **Note**: By default, no users have the `GlobalAdmin` role. You'll need to seed this or manually assign it.

### Option A: Use the Seeded Admin (if seeding is enabled)
The `OrgSeeder.cs` creates a default organization. Check if you have admin access.

### Option B: Manually Grant Admin Role
Run in your database or add to seed data:
```sql
-- For SQLite Identity database
INSERT INTO AspNetUserRoles (UserId, RoleId) 
SELECT u.Id, r.Id 
FROM AspNetUsers u, AspNetRoles r 
WHERE u.Email = 'admin@test.com' AND r.Name = 'GlobalAdmin';
```

Or temporarily bypass authorization by commenting out `[Authorize(Policy = "GlobalAdmin")]` in `OrganizationList.razor`.

---

## Step 4: Create a New Organization

1. Navigate to: https://localhost:5001/admin/organizations
2. Click **"Create New"** button
3. Fill in the **General** tab:
   - **Organization Name**: `Fabrikam`
   - **Slug**: `fabrikam` (URL-safe, lowercase)
   - **Disabled**: Leave unchecked
4. Fill in the **Branding** tab:
   - **Primary Color**: Pick a color (e.g., `#FF5722` for orange)
   - **Secondary Color**: Pick a secondary color
   - **Logo URL**: (optional) `https://example.com/logo.png`
   - **Header HTML**: (optional) `<div style="text-align:center;">Welcome to Fabrikam!</div>`
5. Fill in the **AI Configuration** tab (optional):
   - **Model Deployment**: `gpt-4o`
   - **System Prompt**: `You are Fabrikam's helpful assistant.`
6. Click **Save**

---

## Step 5: Test the Organization-Specific Chat URL

1. Navigate to: https://localhost:5001/org/fabrikam/chat
2. You should see:
   - The chat interface loads
   - Organization branding (colors) may be applied
   - Header HTML appears if configured
3. Create a new conversation and send a test message

---

## Step 6: Verify Organization Isolation

1. Create another organization:
   - **Name**: `Contoso`
   - **Slug**: `contoso`
   - **Primary Color**: `#2196F3` (blue)
2. Navigate to: https://localhost:5001/org/contoso/chat
3. Verify:
   - Different branding colors appear
   - Conversations from Fabrikam do NOT appear in Contoso

---

## Step 7: Test Organization Theming

### Verify Theme Service

1. In `fabrikam` org, the primary color should affect:
   - Buttons
   - App bar
   - Links
2. In `contoso` org, different colors should appear

### Verify Custom CSS (if configured)

Add custom CSS in the organization settings:
```css
--mud-palette-primary: #FF5722;
```

---

## Step 8: Test Health Endpoints

Verify the health check endpoints are working:

```bash
# Liveness - Is the app alive?
curl -k https://localhost:5001/liveness

# Startup - Are dependencies initialized?
curl -k https://localhost:5001/startup

# Readiness - Ready to accept traffic?
curl -k https://localhost:5001/readiness
```

Expected responses:
- `Healthy` - All checks passed
- `Degraded` - Some checks have warnings (e.g., AI not configured)
- `Unhealthy` - Critical failures

---

## Step 9: Test Rate Limiting

Send multiple rapid requests to verify rate limiting:

```bash
# Send 25 rapid requests (should hit the 20/min AI limit)
for i in {1..25}; do
  curl -k -X POST https://localhost:5001/api/chat/send \
    -H "Content-Type: application/json" \
    -d '{"message":"test"}' &
done
```

After 20 requests in a minute, you should receive HTTP 429 (Too Many Requests).

---

## Step 10: Test Input Validation

1. Try to create an organization with:
   - **Invalid slug**: `UPPER-case` or `has spaces` → Should show error
   - **Empty name**: → Should show "Name is required"
   - **Very long name** (>100 chars): → Should show validation error

2. In chat, try sending:
   - Empty message → Send button should be disabled
   - Very long message (>32,000 chars) → Should be rejected or truncated

---

## Troubleshooting

### "Organization not found" error
- Verify the slug exists in the admin panel
- Check Cosmos DB `organizations` container for the document

### Conversations not loading
- Check browser console for errors
- Verify Cosmos DB `conversations` container has proper partition key
- Check application logs: `dotnet run` output

### Theme not applying
- Verify `ThemeService` is registered in `Program.cs`
- Check if `OrganizationStyleProvider.razor` is included in the layout
- Inspect browser DevTools for CSS variables

### Admin panel access denied
- Verify user has `GlobalAdmin` role
- Check `AspNetUserRoles` table in SQLite database
- Temporarily disable `[Authorize]` for testing

---

## Quick Reference: Organization URLs

| URL Pattern | Description |
|-------------|-------------|
| `/admin/organizations` | Organization management (admin only) |
| `/org/{slug}/chat` | Chat for specific organization |
| `/org/{slug}/chat/{conversationId}` | Direct link to conversation |
| `/chat` | Default chat (no organization) |

---

## API Endpoints

| Endpoint | Method | Description |
|----------|--------|-------------|
| `GET /api/organization` | GET | List all organizations |
| `POST /api/organization` | POST | Create organization |
| `PUT /api/organization/{id}` | PUT | Update organization |
| `DELETE /api/organization/{id}` | DELETE | Disable organization |
