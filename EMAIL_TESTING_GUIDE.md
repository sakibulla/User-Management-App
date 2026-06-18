# Email Verification Testing Guide

## Prerequisites

1. **Gmail Account** with 2-Factor Authentication enabled
2. **Gmail App Password** generated (16-character password)
3. **App running** on `http://localhost:5000`
4. **PostgreSQL** running with the `usermgmt` database

## Setup Before Testing

### 1. Get Gmail App Password

Follow these steps:
1. Go to https://myaccount.google.com/security
2. Enable "2-Step Verification" if not already enabled
3. Go to https://myaccount.google.com/apppasswords
4. Select Mail → Windows Computer
5. Copy the 16-character password (remove spaces)

### 2. Update appsettings.json

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "abcdefghijklmnop",
    "FromName": "User Management App"
  }
}
```

### 3. Restart the App

```bash
dotnet run
```

---

## Test 1: Check Email Configuration

**Goal:** Verify Gmail SMTP is properly configured before sending emails.

### Steps:

1. **Open browser**:
   ```
   http://localhost:5000/api/test/check-email-config
   ```

2. **Expected response** (JSON):
   ```json
   {
     "configured": true,
     "message": "Email configuration looks good!",
     "config": {
       "smtpHost": "smtp.gmail.com",
       "smtpPort": "587",
       "smtpUser": "your-email@gmail.com",
       "smtpUserConfigured": true,
       "smtpPassConfigured": true,
       "fromName": "User Management App",
       "environment": "Production"
     }
   }
   ```

3. **If not configured**:
   - Check appsettings.json SmtpUser and SmtpPass are not empty
   - Make sure you've restarted the app after updating config

---

## Test 2: Send Test Email

**Goal:** Send a test verification email to confirm SMTP works.

### Steps:

1. **Open browser**:
   ```
   http://localhost:5000/api/test/send-test-email?to=your-test-email@gmail.com
   ```

2. **Expected response** (JSON):
   ```json
   {
     "success": true,
     "message": "Test email sent successfully!",
     "email": "your-test-email@gmail.com",
     "note": "Check your inbox (may take a few seconds)..."
   }
   ```

3. **If error**:
   ```json
   {
     "success": false,
     "message": "Failed to send email",
     "error": "... error details ..."
   }
   ```

### Troubleshooting:

- **"Authentication failed"**: Check Gmail App Password is correct (remove spaces)
- **"Connection timeout"**: Check internet connection
- **"No response"**: Check app logs in console for errors

4. **Check inbox**:
   - Open your Gmail inbox
   - You should receive an email from `your-email@gmail.com`
   - Subject: "Verify your email address"
   - Contains: "Verify Email" link (test link, won't work)

---

## Test 3: Register New User and Receive Verification Email

**Goal:** Complete user registration and receive a real verification email.

### Steps:

1. **Go to registration page**:
   ```
   http://localhost:5000/account/register
   ```

2. **Fill in the form**:
   - **Name**: Test User
   - **Email**: `testuser-<timestamp>@gmail.com` (use unique email like `testuser-123@gmail.com`)
   - **Password**: anything (even 1 character is allowed)

3. **Click "Register"**

4. **Expected result**:
   - Page shows: "Welcome, Test User! Your account has been created. Check your email to verify..."
   - Redirects to login page

5. **Check database** (verify user created as `unverified`):
   ```bash
   psql -h localhost -U postgres -d usermgmt
   SELECT id, name, email, status FROM users ORDER BY id DESC LIMIT 1;
   ```
   
   **Expected output**:
   ```
   id | name      | email                    | status
   ---+-----------+--------------------------+-----------
   5  | Test User | testuser-123@gmail.com   | unverified
   ```

6. **Check Gmail inbox**:
   - You should receive a verification email within 5-10 seconds
   - **From**: your-email@gmail.com
   - **To**: testuser-123@gmail.com
   - **Subject**: Verify your email address
   - **Body contains**: "Please verify your email address by clicking the link below:" + a clickable link

---

## Test 4: Click Verification Link and Activate Account

**Goal:** Verify that clicking the email link changes user status from `unverified` to `active`.

### Steps:

1. **Open the verification email**

2. **Click the "Verify Email" link**
   - Link format: `http://localhost:5000/account/verify?token=xxxxx`

3. **Expected result**:
   - Page shows: "Email verified successfully! Your account is now active."
   - Redirects to login page

4. **Verify database** (status should now be `active`):
   ```bash
   psql -h localhost -U postgres -d usermgmt
   SELECT id, name, email, status FROM users WHERE email = 'testuser-123@gmail.com';
   ```
   
   **Expected output**:
   ```
   id | name      | email                  | status
   ---+-----------+------------------------+--------
   5  | Test User | testuser-123@gmail.com | active
   ```

5. **Try clicking the link again**:
   - Should show error: "Verification link is invalid or has already been used."
   - This is correct - token is one-time use

---

## Test 5: Login with Unverified Email

**Goal:** Verify that users can log in even if their email is not verified.

### Steps:

1. **Register another test user** (follow Test 3, different email like `testuser-456@gmail.com`)
   - **Do NOT** click the verification link this time
   - Keep email as `unverified`

2. **Go to login**:
   ```
   http://localhost:5000/account/login
   ```

3. **Log in with**:
   - Email: testuser-456@gmail.com
   - Password: (whatever you used during registration)

4. **Expected result**:
   - Successfully logs in
   - Redirected to `/users` (admin table)
   - Can use the app normally

5. **Optional**: Go back and verify the email, status changes to `active`, functionality unchanged

---

## Test 6: Blocked User Cannot Verify

**Goal:** Verify that blocked users cannot change status during verification.

### Steps:

1. **Register user** (follow Test 3, email like `testuser-blocked@gmail.com`)
   - Keep email unverified
   - Note the token in the database or URL

2. **Block the user** (via admin panel):
   - Log in as another user
   - Go to `/users`
   - Select the user row
   - Click "Block"
   - User status is now `blocked`

3. **Click the verification link** (from step 1's email):
   - Page says: "Email verified successfully!..."
   - Redirects to login

4. **Check database**:
   ```bash
   SELECT id, email, status FROM users WHERE email = 'testuser-blocked@gmail.com';
   ```
   
   **Expected output**:
   ```
   id | email                   | status
   ---+-------------------------+--------
   7  | testuser-blocked@gmail.com | blocked
   ```
   
   **Important**: Status stayed `blocked` (did NOT change to `active`)
   - This proves verification link does not unblock users

---

## Test 7: Resend Email Multiple Times

**Goal:** Verify no errors when verification email sent multiple times to same address.

### Steps:

1. **Register user** with email `testuser-multi@gmail.com`

2. **Check Gmail inbox**, find the first verification email

3. **Go back to registration page**, register again with **same email**:
   - Expected result: Error "An account with this email already exists."
   - This is correct - unique email constraint works

---

## Common Issues & Solutions

### Issue 1: "Email not sent: SMTP credentials not configured"

**Cause**: `SmtpUser` or `SmtpPass` is empty in appsettings.json

**Solution**:
```json
{
  "Email": {
    "SmtpUser": "your-email@gmail.com",      // ← NOT empty
    "SmtpPass": "abcdefghijklmnop",         // ← NOT empty
    ...
  }
}
```

Restart app after updating.

### Issue 2: "Authentication failed" or "Invalid credentials"

**Cause**: Gmail App Password is incorrect or has spaces

**Solution**:
- Go to https://myaccount.google.com/apppasswords
- Generate a new one
- **Remove ALL SPACES** when copying
- Update appsettings.json
- Restart app

### Issue 3: Email arrives but link is wrong/broken

**Cause**: URL generation issue

**Solution**:
- Check that `Request.Scheme` and `Request.Host` are correct in email link
- Test email should show full URL like: `http://localhost:5000/account/verify?token=xxxxx`
- In production (Railway), make sure app knows its public URL

### Issue 4: "Connection timed out" or "Connection refused"

**Cause**: Cannot reach smtp.gmail.com or firewall blocking

**Solution**:
- Check internet connection
- Check if Gmail SMTP port 587 is open in your firewall
- Try from different network
- Check Gmail account not temporarily locked (log in to Gmail directly)

### Issue 5: Email verification link doesn't work (404 or redirect loop)

**Cause**: Token not found or app restarted since registration

**Solution**:
- Check token in database matches URL
- Make sure app is still running
- Restart app and try again
- Check browser console for errors (F12 → Console)

---

## Performance Notes

- **Email sending is asynchronous** - registration completes immediately, email sent in background
- **First email takes longer** - SMTP connection initialization
- **Subsequent emails faster** - connection reused
- **In production** - consider email queue for high volume

---

## Next Steps

Once email verification is working:

1. **Remove TestController.cs** (or mark as `[Obsolete]`) before production
2. **Configure in Railway**:
   - Add PostgreSQL plugin
   - Set Gmail App Password in Variables
   - Deploy app
3. **Test in production** with actual app URL
4. **Monitor email logs** in your app for issues

---

## Email Flow Diagram

```
User Registration
    ↓
Create user (status = unverified)
    ↓
Save to database
    ↓
Generate verification token
    ↓
Send email (async, in background)
    ↓
User receives email
    ↓
User clicks link
    ↓
Server validates token
    ↓
Update status to "active"
    ↓
Clear verification token
    ↓
Redirect to login
```

---

## Files Modified/Created

- ✅ `appsettings.json` - Gmail SMTP configuration
- ✅ `UserManagementApp.csproj` - MailKit updated to 4.8.0
- ✅ `Controllers/TestController.cs` - Test endpoints for email verification
- ✅ `Services/EmailService.cs` - Already set up for MailKit + Gmail
- ✅ `EMAIL_SETUP_GUIDE.md` - Step-by-step Gmail setup guide
- ✅ `EMAIL_TESTING_GUIDE.md` - This file

---

## Summary

All components are in place for email verification with Gmail SMTP:
- ✅ MailKit installed and configured
- ✅ Gmail App Password setup process documented
- ✅ Test endpoints for verification
- ✅ Async email sending
- ✅ Email verification status changes in database
- ✅ Blocked user handling
- ✅ One-time token usage

Ready to test! 🚀
