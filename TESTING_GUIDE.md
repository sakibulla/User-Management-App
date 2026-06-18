# Email Verification & Full Feature Testing Guide

## Pre-Deployment Testing

### 1. Local Setup (Development)

#### Database:
```bash
# Start PostgreSQL
# Default connection: localhost:5432, user: postgres, password: Jakboi2002, database: usermgmt

# Verify the unique index exists:
psql -U postgres -d usermgmt -c "SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'users';"
```

#### Email Configuration (appsettings.json):
```json
{
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": "587",
    "SmtpUser": "your-email@gmail.com",
    "SmtpPass": "your-app-password",
    "FromName": "User Management App"
  }
}
```

#### Start App:
```bash
cd c:\Users\User\Downloads\UserManagementApp
dotnet run
# App starts on http://localhost:5000
```

---

## Test Scenarios

### Test 1: User Registration & Email Verification

**Objective**: Verify email is sent and token-based verification works

**Steps**:
1. Navigate to http://localhost:5000/account/register
2. Fill form:
   - Name: "Test User"
   - Email: "testuser@gmail.com" (or any email you can receive)
   - Password: "password123"
3. Click "Register"
4. See success message: "Your account has been created. Check your email to verify..."
5. Check your email inbox for verification email from "User Management App"
6. Click the verification link in the email
7. See success: "Email verified successfully! Your account is now active."
8. Verify in database:
   ```sql
   SELECT email, status, verification_token FROM users WHERE email = 'testuser@gmail.com';
   -- Expected: status='active', verification_token=NULL
   ```

**Expected Result**: ✅ Email received, link works, status changed to active

---

### Test 2: Duplicate Email Prevention (Unique Index)

**Objective**: Verify unique index prevents duplicate emails

**Steps**:
1. Register first user: `duplicate@gmail.com`
2. Try to register second user with same email
3. See error: "An account with this email already exists."
4. Database shows error from `ix_users_email_unique` constraint
5. Verify in database:
   ```sql
   SELECT COUNT(*) FROM users WHERE email = 'duplicate@gmail.com';
   -- Expected: 1 (only one user)
   ```

**Expected Result**: ✅ Duplicate prevented at database level, user-friendly error shown

---

### Test 3: User Can Log In Before Email Verification

**Objective**: Verify users don't need to verify email to log in

**Steps**:
1. Register new user: `unverified@gmail.com` with password "testpass"
2. **Don't click verification email**
3. Go to http://localhost:5000/account/login
4. Log in with `unverified@gmail.com` / `testpass`
5. Should successfully log in to user management table
6. Check user status in table: should show "Unverified" (gray badge)

**Expected Result**: ✅ Can log in with unverified email

---

### Test 4: Table Display & Sorting

**Objective**: Verify table shows all users sorted by last login

**Steps**:
1. Log in as a user
2. Check user management table
3. Verify columns exist:
   - ✅ Selection checkbox (no label)
   - ✅ Name (with registration date)
   - ✅ Email
   - ✅ Status (badge)
   - ✅ Last login time
4. Log in/out with different users to change last login times
5. Refresh table
6. Most recently logged-in user should be at the top
7. Never-logged-in users at the bottom

**Expected Result**: ✅ Table sorted by last login descending

---

### Test 5: Multiple User Selection

**Objective**: Verify checkbox selection works correctly

**Steps**:
1. In user management table, click individual user checkboxes
2. Verify selection count updates: "X selected"
3. Click header checkbox (select all)
4. All rows should be checked
5. Click header checkbox again (deselect all)
6. All rows should be unchecked
7. Select some users (not all)
8. Header checkbox shows indeterminate state (partially filled)

**Expected Result**: ✅ All checkbox interactions work correctly

---

### Test 6: User Blocking

**Objective**: Verify blocking prevents login and blocks toolbar actions

**Steps**:
1. Register 2 users: User A and User B
2. Log in as User A
3. Select User B in the table
4. Click "Block" button
5. User B should now show status "Blocked" (red badge)
6. Log out (as User A)
7. Try to log in as User B
8. See error: "Your account has been blocked. Please contact an administrator."

**Expected Result**: ✅ Blocked user cannot log in

---

### Test 7: User Blocking Self (Auto-redirect)

**Objective**: Verify user is redirected when they block themselves

**Steps**:
1. Log in as User C
2. Select yourself + another user
3. Click "Block"
4. **Automatic redirect to login page**
5. See message: "Your account has been blocked or deleted. Please contact an administrator."

**Expected Result**: ✅ Auto-redirect when user blocks self

---

### Test 8: User Deletion

**Objective**: Verify physical deletion (not soft delete)

**Steps**:
1. Log in as User D
2. Select a user (not yourself)
3. Click "Delete" (trash icon)
4. User disappears from table immediately
5. Verify in database:
   ```sql
   SELECT COUNT(*) FROM users WHERE email = 'deleted-user@gmail.com';
   -- Expected: 0 (user completely removed)
   ```

**Expected Result**: ✅ User physically deleted from database

---

### Test 9: Delete Unverified Users

**Objective**: Verify delete-unverified removes all unverified accounts

**Steps**:
1. Register User E (don't verify email)
2. Register User F (verify email)
3. Click "Delete Unverified" button (person-x icon)
4. User E (unverified) should disappear
5. User F (verified/active) should remain
6. Verify in database:
   ```sql
   SELECT COUNT(*) FROM users WHERE status = 'unverified';
   -- Expected: 0
   ```

**Expected Result**: ✅ Only unverified users deleted

---

### Test 10: User Status Middleware Check

**Objective**: Verify blocked users can't access protected pages

**Steps**:
1. Log in as User G
2. Get the authenticated cookie (stays logged in)
3. From another session, block User G
4. User G tries to access /users (admin table)
5. **Automatically redirected to /account/login**
6. See message about being blocked/deleted

**Expected Result**: ✅ Middleware checks status on each request

---

### Test 11: Toolbar Button States

**Objective**: Verify buttons enable/disable based on selection

**Steps**:
1. Load user management table
2. No users selected:
   - ✅ Block button: disabled
   - ✅ Unblock button: disabled
   - ✅ Delete button: disabled
   - ✅ Delete Unverified: enabled (always)
3. Select 1 user:
   - ✅ Block button: enabled
   - ✅ Unblock button: enabled
   - ✅ Delete button: enabled
4. Select multiple users:
   - ✅ All selection buttons enabled

**Expected Result**: ✅ Buttons correctly enable/disable

---

### Test 12: Unique Index Direct Verification (For Grading)

**Objective**: Demonstrate unique index in database

**Steps**:

#### Via pgAdmin:
1. Open pgAdmin (or connect via psql)
2. Navigate to: Servers → PostgreSQL → Databases → usermgmt → Schemas → public → Tables → users → Indexes
3. You should see: `ix_users_email_unique`
4. Right-click → Properties → See:
   - **Name**: `ix_users_email_unique`
   - **Index type**: BTREE
   - **Unique**: Yes
   - **Columns**: email

#### Via SQL:
```sql
SELECT indexname, indexdef FROM pg_indexes 
WHERE tablename = 'users' AND indexname = 'ix_users_email_unique';

-- Output:
-- ix_users_email_unique | CREATE UNIQUE INDEX ix_users_email_unique ON public.users USING btree (email)
```

#### Code Reference:
- **File**: `Data/AppDbContext.cs` (lines 28-32)
- Shows: `entity.HasIndex(u => u.Email).IsUnique().HasDatabaseName("ix_users_email_unique")`

#### Error Catching:
- **File**: `Controllers/AccountController.cs` (lines 103-107)
- Shows: Catches `DbUpdateException` when index constraint violated
- **User sees**: "An account with this email already exists."

**Expected Result**: ✅ Unique index exists, errors caught properly

---

## Responsive Design Testing

### Desktop (1920x1080):
- [ ] Table displays normally
- [ ] All columns visible
- [ ] Toolbar buttons aligned horizontally
- [ ] No horizontal scrolling needed

### Tablet (768px):
- [ ] Table responsive
- [ ] Toolbar may wrap but functional
- [ ] All buttons still clickable

### Mobile (375px):
- [ ] Table scrolls horizontally if needed
- [ ] Toolbar buttons stack vertically
- [ ] Touch interactions work (checkboxes, buttons)
- [ ] Text readable (no overflow)

---

## Browser Compatibility

Test on:
- [ ] Chrome/Chromium
- [ ] Firefox
- [ ] Edge
- [ ] Safari (if available)

Expected: All work identically with Bootstrap 5 & modern JavaScript

---

## Performance & Security

### Security Checks:
- [ ] Antiforgery tokens on all form posts
- [ ] Passwords bcrypt-hashed (never plain text)
- [ ] Email sent via TLS (StartTls on port 587)
- [ ] Authentication cookie: HttpOnly, SameSite=Lax
- [ ] Middleware checks user status before each request
- [ ] No SQL injection (using EF Core parameterized queries)

### Performance:
- [ ] Table loads <500ms
- [ ] Email sent in background (non-blocking registration)
- [ ] Toolbar actions complete <2 seconds
- [ ] Database queries efficient (using AsNoTracking)

---

## Video Recording Checklist

For final demonstration, record the following in order:

1. **[0:00-1:00]** Show database indexes in pgAdmin/psql
   - Demonstrate unique index on email

2. **[1:00-2:00]** Show code that catches unique constraint error
   - File: AccountController.cs
   - Show try/catch block

3. **[2:00-3:30]** Registration flow
   - Register new user
   - Show verification email received
   - Click email link
   - Show status changed in table

4. **[3:30-4:30]** Duplicate email test
   - Try registering with same email
   - Show error message
   - Show query showing only 1 record

5. **[4:30-5:30]** Login & table display
   - Log in as verified user
   - Show table sorted by last login
   - Show all columns present

6. **[5:30-6:30]** Multiple selection
   - Click individual checkboxes
   - Click select all
   - Show selection count

7. **[6:30-8:00]** User blocking
   - Select and block another user
   - Show status changed
   - Log out and try login as blocked user
   - Show error

8. **[8:00-8:30]** Block self (auto-redirect)
   - Select self + one other user
   - Click block
   - Show automatic redirect to login

9. **[8:30-9:00]** Delete user
   - Delete a user
   - Show user disappears from table

10. **[9:00-9:30]** Delete unverified
    - Click delete unverified
    - Show unverified users removed

**Total Duration**: ~10 minutes

---

## Troubleshooting

### Email Not Received
- Check spam folder
- Verify Gmail account has 2FA enabled
- Verify App Password is correct (16 chars)
- Check email logs: `dotnet run` shows "Verification email sent to..."

### "Email not sent" message
- appsettings.json missing SmtpUser/SmtpPass
- Update with Gmail credentials

### Verification link doesn't work
- Token may have expired
- Ensure `verification_token` is in database
- Check URL format matches generated link

### Database connection refused
- PostgreSQL not running
- Check connection string matches your setup
- Port 5432 not accessible

### Toolbar buttons don't work
- Refresh page (cache issue)
- Check browser console for errors
- Verify antiforgery token present in HTML

