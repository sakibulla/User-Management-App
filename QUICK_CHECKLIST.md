# Email Verification - Quick Checklist

## Before You Start
- [ ] Gmail account ready
- [ ] App running on http://localhost:5000
- [ ] PostgreSQL running

## Setup (Do Once)

### 1. Gmail App Password
- [ ] Go to https://myaccount.google.com/security
- [ ] Enable 2-Factor Authentication (if not already)
- [ ] Go to https://myaccount.google.com/apppasswords
- [ ] Select Mail → Windows Computer
- [ ] Copy 16-character password (remove spaces)

### 2. Configure App
- [ ] Open `appsettings.json`
- [ ] Set `Email.SmtpUser` to your Gmail address
- [ ] Set `Email.SmtpPass` to the 16-character password
- [ ] Save file
- [ ] Restart app: `dotnet run`

### 3. Verify Configuration
- [ ] Open: http://localhost:5000/api/test/check-email-config
- [ ] Response shows: `"configured": true`
- [ ] If not configured, check appsettings.json again

## Testing (Repeat for each feature)

### Test 1: Send Test Email
- [ ] Open: http://localhost:5000/api/test/send-test-email?to=your-email@gmail.com
- [ ] Response shows: `"success": true`
- [ ] Check Gmail inbox for test email

### Test 2: Register User
- [ ] Go to: http://localhost:5000/account/register
- [ ] Fill form:
  - [ ] Name: Test User
  - [ ] Email: testuser123@gmail.com (unique)
  - [ ] Password: test123
- [ ] Click Register
- [ ] See: "Check your email to verify..."

### Test 3: Check Database
- [ ] Open terminal
- [ ] Run: `psql -h localhost -U postgres -d usermgmt`
- [ ] Run: `SELECT id, email, status FROM users WHERE email = 'testuser123@gmail.com';`
- [ ] Status should be: `unverified`

### Test 4: Receive Email
- [ ] Check Gmail inbox (wait 5-10 seconds if needed)
- [ ] Find email from your Gmail address
- [ ] Subject: "Verify your email address"
- [ ] Contains verification link

### Test 5: Click Verification Link
- [ ] Open the email
- [ ] Click "Verify Email" link
- [ ] See: "Email verified successfully!"
- [ ] Redirected to login page

### Test 6: Verify Status Changed
- [ ] Run: `SELECT id, email, status FROM users WHERE email = 'testuser123@gmail.com';`
- [ ] Status should now be: `active`
- [ ] VerificationToken should be: `NULL` (cleared)

### Test 7: Click Link Again (Should Fail)
- [ ] Try clicking the same verification link again
- [ ] See: "Verification link is invalid or has already been used."
- [ ] This is correct behavior ✅

### Test 8: Login with Unverified Email
- [ ] Register new user with email like `testuser456@gmail.com`
- [ ] **Do NOT** click verification link
- [ ] Go to: http://localhost:5000/account/login
- [ ] Log in with that email and password
- [ ] Should successfully log in ✅
- [ ] Can access admin panel

### Test 9: Block User (Verify Email Doesn't Unblock)
- [ ] Register with email `testuser789@gmail.com`
- [ ] Do NOT verify
- [ ] Log in as admin user
- [ ] Go to: http://localhost:5000/users
- [ ] Select `testuser789@gmail.com` user
- [ ] Click "Block" button
- [ ] User blocked, redirected to login
- [ ] Click the unverified email's verification link from inbox
- [ ] See: "Email verified successfully!"
- [ ] Check DB: `SELECT status FROM users WHERE email = 'testuser789@gmail.com';`
- [ ] Status is still: `blocked` (NOT changed to `active`) ✅

## Common Issues Checklist

### Email Not Sent
- [ ] Check appsettings.json SmtpUser is not empty
- [ ] Check appsettings.json SmtpPass is not empty
- [ ] Restart app after changing config
- [ ] Check Gmail App Password has no spaces
- [ ] Check internet connection
- [ ] Check firewall allows port 587

### "Authentication failed"
- [ ] Verify Gmail App Password is correct
- [ ] Regenerate new App Password if unsure
- [ ] Check 2-Factor Authentication is enabled on Gmail
- [ ] Check you're using 16-char password, not regular Gmail password

### Email Arrives but Verification Link Broken
- [ ] Make sure app is still running
- [ ] Try clicking link again
- [ ] Check browser address bar for correct URL format
- [ ] Check app logs for errors (look in console)

### Verification Link Says "Already Used"
- [ ] This is correct - tokens are one-time use
- [ ] Register again to get a new token

## Success Criteria

✅ **Email verification is working if**:
1. Configuration endpoint shows `configured: true`
2. Test email arrives in Gmail inbox within 10 seconds
3. New user registration creates `unverified` user in database
4. Verification email sent with working link
5. Clicking link changes status from `unverified` to `active`
6. Second click on link shows "already used" error
7. Unverified users can still log in
8. Blocked users can verify but stay blocked
9. Verification token is cleared from database after use

## Files to Know

- `appsettings.json` - Gmail SMTP configuration (update this)
- `Services/EmailService.cs` - Email sending code
- `Controllers/AccountController.cs` - Registration & verification logic
- `Controllers/TestController.cs` - Test endpoints (dev only)
- `EMAIL_SETUP_GUIDE.md` - Detailed Gmail setup
- `EMAIL_TESTING_GUIDE.md` - Detailed test scenarios
- `EMAIL_VERIFICATION_SUMMARY.md` - Full overview

## When Everything Works

You're ready to:
1. ✅ Disable TestController endpoints (remove from production)
2. ✅ Deploy to Railway
3. ✅ Use real emails for production users
4. ✅ Monitor email sending in production logs

---

**Status**: All systems operational ✅
**Next Step**: Start with "Setup" section above
