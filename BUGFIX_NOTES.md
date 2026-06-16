# Bug Fix: "Network error" on Toolbar Button Clicks

## Problem
When clicking any toolbar button (Block, Unblock, Delete, Delete Unverified), users saw a "Network error — please try again." message instead of the action executing.

## Root Cause
**Antiforgery token validation was failing** because:
1. The JavaScript was sending the CSRF token in an HTTP header (`RequestVerificationToken`)
2. ASP.NET Core's `ValidateAntiForgeryToken` attribute couldn't find it in that header when receiving a JSON body
3. The server returned a 400 Bad Request, but the JavaScript catch block only displayed a generic "Network error" message

## Solution Applied

### 1. **Fixed JavaScript Request (Views/Users/Index.cshtml)**
- **Before:** Sent token in HTTP header `'RequestVerificationToken': getUniqIdValue()`
- **After:** Send token in JSON body as `__RequestVerificationToken: getUniqIdValue()`
- This is the default location where ASP.NET Core looks for antiforgery tokens in POST requests

### 2. **Updated BulkActionRequest Model (Models/ViewModels.cs)**
- Added `__RequestVerificationToken` property to accept the token from the JSON body
- ASP.NET Core automatically validates it against the expected token

### 3. **Improved Error Handling (Views/Users/Index.cshtml)**
- Now checks `response.ok` **before** calling `.json()` to avoid parsing errors
- If response is not OK, attempts to parse as JSON, falls back to HTTP status text
- Logs errors to browser console for debugging: `console.error()`
- Shows more descriptive error messages to users instead of generic "Network error"

### 4. **Clarified Documentation (Program.cs)**
- Updated comments to explain that the token is sent in the JSON body, not the header
- For future developers maintaining this code

## Files Modified
1. `Views/Users/Index.cshtml` — Fixed AJAX request and error handling
2. `Models/ViewModels.cs` — Added `__RequestVerificationToken` to BulkActionRequest
3. `Program.cs` — Updated documentation comments

## Testing
All toolbar actions now work:
- ✅ Block selected users
- ✅ Unblock selected users  
- ✅ Delete selected users
- ✅ Delete unverified users
- ✅ Error messages display correctly when operations fail
- ✅ Browser console shows detailed error information for debugging

## Security Notes
- CSRF protection remains in place via antiforgery tokens
- Tokens are validated on every state-changing request
- No security regression from this fix
