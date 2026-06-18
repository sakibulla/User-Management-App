# Gmail Setup - Visual Guide

## Overview
Your app needs to send verification emails. We'll use Gmail to do this.

---

## 🎯 Goal
Get a special password from Gmail that lets the app send emails.

---

## ⏱️ Time Required
**5 minutes**

---

## 🚀 Let's Do It

### STEP 1: Enable 2-Factor Authentication on Gmail

**Why?** Gmail requires this before we can get an app-specific password.

```
1. Open your browser
2. Go to: https://myaccount.google.com/security
3. Look for: "2-Step Verification"
4. Click it
5. Click: "Get Started"
6. Follow prompts (verify with your phone)
7. Done ✓
```

### STEP 2: Get Your App Password

**Now that 2FA is enabled, Gmail will let us create a special password.**

```
1. Back on https://myaccount.google.com/security
2. Scroll down to: "App passwords"
3. You'll see two dropdowns:
   - First dropdown: Select "Mail"
   - Second dropdown: Select "Windows Computer"
4. Click: "Generate"
5. Google shows you a password like:
   zyxw vutsrq ponmlkj
   ↑ THIS IS YOUR PASSWORD ↑
6. Copy the whole thing (including spaces)
```

### STEP 3: Update Your App

**Now put this password into your app's configuration.**

```
File to update:
c:\Users\User\Downloads\UserManagementApp\appsettings.json
```

**Find this in the file:**
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "REPLACE_WITH_YOUR_GMAIL@gmail.com",
  "SmtpPass": "REPLACE_WITH_APP_PASSWORD",
  "FromName": "User Management App"
}
```

**Replace with YOUR info:**
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "myemail@gmail.com",
  "SmtpPass": "zyxw vutsrq ponmlkj",
  "FromName": "User Management App"
}
```

**Example:**
- If your Gmail is `john.doe@gmail.com`, put that in SmtpUser
- If Gmail gave you `abcd efgh ijkl mnop`, put that in SmtpPass (with spaces)

### STEP 4: Restart Your App

```
1. Stop the app (Ctrl+C in terminal)
2. Run it again: dotnet run
3. You'll see: "Now listening on: http://localhost:5000"
```

### STEP 5: Test It!

```
1. Go to: http://localhost:5000/account/register
2. Register a user:
   - Name: Test User
   - Email: your-email@gmail.com (an email YOU can check)
   - Password: test123
3. Click Register
4. Check your email inbox
5. Look for email from "User Management App"
6. Click the link in the email
7. You should see: "Email verified successfully!"
```

---

## ✅ Done!

Your app can now send emails.

---

## 🆘 What If It Doesn't Work?

### Problem: No email received
**Check:**
1. Spam/Junk folder
2. Is SmtpUser your REAL Gmail address?
3. Is SmtpPass exactly what Gmail gave you (with spaces)?
4. Did you restart the app after updating appsettings.json?

### Problem: "Invalid email or password"
**Solution:**
1. Go back to Gmail
2. Create a NEW app password
3. Copy it and paste into appsettings.json
4. Restart app

### Problem: "2-Step Verification not showing"
**Solution:**
1. You need to enable it first
2. Go to https://myaccount.google.com/security
3. Find "2-Step Verification"
4. Click "Get Started"
5. Follow the phone verification
6. Then "App passwords" will appear

---

## 📝 Quick Reference

| What | Value |
|-----|-------|
| **SmtpHost** | `smtp.gmail.com` (don't change) |
| **SmtpPort** | `587` (don't change) |
| **SmtpUser** | Your Gmail address (e.g., myemail@gmail.com) |
| **SmtpPass** | App password from Gmail (16 chars with spaces) |
| **FromName** | "User Management App" (don't change) |

---

## 🔐 Security Note

The app password:
- Only lets the app send emails
- Doesn't give access to your Gmail account
- Is different from your normal Gmail password
- Can be revoked anytime from Gmail settings

---

## ✨ That's All!

Your email is now configured. Users will automatically receive verification emails when they register.

