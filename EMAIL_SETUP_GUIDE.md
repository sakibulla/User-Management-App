# Gmail Email Verification Setup Guide

## Step 1: Enable 2-Factor Authentication on Your Gmail Account

1. Go to https://myaccount.google.com/security
2. In the left menu, click **"2-Step Verification"**
3. Click **"Get Started"**
4. Follow the prompts to enable 2FA with your phone

## Step 2: Create a Gmail App Password

1. After 2FA is enabled, go back to https://myaccount.google.com/security
2. In the left menu, find **"App passwords"** (appears after 2FA is enabled)
3. Select:
   - **App**: Mail
   - **Device**: Windows Computer (or your device)
4. Google generates a 16-character password (example: `abcd efgh ijkl mnop`)
5. **Copy the password** (it's shown only once)

## Step 3: Update appsettings.json

Replace the placeholder values with your Gmail credentials:

```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "abcd efgh ijkl mnop",
    "FromName": "User Management App"
  }
}
```

**Example:**
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUser": "myapp@gmail.com",
    "SmtpPass": "zyxw vutsrq ponmlkj",
    "FromName": "User Management App"
  }
}
```

⚠️ **Important**: Never commit `appsettings.json` with real credentials to git. Use environment variables for production:

```bash
export EMAIL_SMTPUSER="myapp@gmail.com"
export EMAIL_SMTPPASS="zyxw vutsrq ponmlkj"
```

Then reference in code:
```csharp
var smtpUser = Environment.GetEnvironmentVariable("EMAIL_SMTPUSER") 
    ?? _config["Email:SmtpUser"];
```

## Step 4: Test Email Verification Flow

### Register a new user:
1. Go to http://localhost:5000/account/register
2. Enter a test email (Gmail address or any email)
3. Submit the form
4. Check the email inbox for a verification link
5. Click the link from the email
6. You should see "Email verified successfully! Your account is now active."

### Verify in database:
```sql
SELECT id, email, status, verification_token FROM users ORDER BY registered_at DESC LIMIT 1;
```

Expected output after clicking verify link:
- `status`: `active` (was `unverified`)
- `verification_token`: `NULL` (was a GUID)

## Step 5: Verify Unique Index in Database

Check that the email index was created:

```sql
-- List all indexes on the users table
SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'users';
```

You should see:
```
ix_users_email_unique | CREATE UNIQUE INDEX ix_users_email_unique ON public.users USING btree (email)
```

## Step 6: Test Duplicate Email Handling

1. Register with `test@gmail.com`
2. Try to register again with the same email
3. You should see: **"An account with this email already exists."**
4. This error comes from catching the database unique constraint violation

## Troubleshooting

### "Email not sent: SMTP credentials not configured"
- Update `appsettings.json` with real Gmail credentials
- Check that `SmtpUser` and `SmtpPass` are not empty

### "Failed to connect to smtp.gmail.com:587"
- Verify your internet connection
- Check that your Gmail account allows "Less secure apps" (or use App Password as shown above)
- Make sure 2FA is enabled before creating App Password

### "Invalid email or password" (Gmail SMTP)
- Verify the App Password is correct (16 characters with spaces)
- Ensure your Gmail account has 2FA enabled
- Try creating a new App Password and updating the config

### User not found when clicking verification link
- The token may have expired or been used twice
- Check that `verification_token` is stored in the database
- Verify the URL is correct in the email

## Email Verification Flow in Code

1. **Registration** (`POST /account/register`):
   - Creates user with status="unverified"
   - Generates random verification token (GUID)
   - Sends email asynchronously (non-blocking)
   - Returns success message immediately

2. **Email Sent** (`EmailService.SendVerificationEmailAsync`):
   - Connects to Gmail SMTP on port 587 with StartTLS
   - Authenticates with App Password
   - Sends HTML email with verification link containing token

3. **Email Click** (`GET /account/verify?token=...`):
   - Looks up user by verification token
   - Changes status from "unverified" to "active"
   - Clears the verification token (can't be reused)
   - Redirects to login with success message

4. **Login** (`POST /account/login`):
   - User can now log in (works for both "unverified" and "active")
   - Last login timestamp is updated

## Database Schema

```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    email VARCHAR(320) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    status VARCHAR(20) DEFAULT 'unverified',
    registered_at TIMESTAMP DEFAULT NOW(),
    last_login_at TIMESTAMP,
    verification_token VARCHAR(MAX)
);

-- UNIQUE INDEX (not a primary key)
CREATE UNIQUE INDEX ix_users_email_unique ON users(email);
```

This index guarantees email uniqueness at the database level, preventing race conditions even with concurrent registrations.

