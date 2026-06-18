# Unique Index Demonstration Guide (For Grading Video)

**CRITICAL**: This requirement MUST be demonstrated in your video submission.

> "PLEASE, MAKE SURE THAT YOUR VIDEO CONTAINS DEMONSTRATION OF THE INDEX IN THE DATABASE (e.g., as a screenshot from RDBMS with listed indices) AS WELL AS PLACE IN THE CODE THAT CATCHES THE CORRESPONDING ERROR"

---

## Video Requirements

Your demonstration video must show:

1. ✅ **Database UI** - Screenshot showing the unique index exists
2. ✅ **Code** - File showing error handling that catches the constraint violation
3. ✅ **Live Test** - Register with duplicate email and see error message

---

## Part 1: Show Database Index in pgAdmin

### Method 1: Using pgAdmin (GUI)

1. **Open pgAdmin** (or connect to PostgreSQL)
2. **Navigate**: Servers → PostgreSQL → Databases → usermgmt → Schemas → public → Tables → users
3. **Click on "Indexes"**
4. **You should see**: `ix_users_email_unique`
5. **Screenshot this** or record screen showing:
   - Index name: `ix_users_email_unique`
   - Table: `users`
   - Status: Present and active

### Method 2: Using SQL Query (psql)

```bash
# Connect to database
psql -U postgres -d usermgmt

# Run this query:
SELECT indexname, indexdef, tablename 
FROM pg_indexes 
WHERE tablename = 'users' 
AND indexname = 'ix_users_email_unique';
```

**Output you should see**:
```
        indexname         |                                 indexdef                                  | tablename
-------------------------+----------------------------------------------------------------------------+-----------
 ix_users_email_unique   | CREATE UNIQUE INDEX ix_users_email_unique ON public.users USING btree (email) | users
(1 row)
```

**Screenshot or record this output** showing:
- Index name: `ix_users_email_unique`
- Type: UNIQUE
- Column: `email`
- Method: BTREE

---

## Part 2: Show Code That Catches the Error

### File: `Controllers/AccountController.cs`

**Location**: Lines 103-107 (in Register method POST action)

```csharp
catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("ix_users_email_unique") == true
                                || ex.InnerException?.Message.Contains("duplicate key") == true)
{
    // IMPORTANT: This is the ONLY place email uniqueness is surfaced to the user.
    // The check is done purely by catching the DB constraint violation.
    ModelState.AddModelError("Email", "An account with this email already exists.");
    return View(model);
}
```

**Show in video**:
1. Open the file: `Controllers/AccountController.cs`
2. Zoom in on lines 103-107
3. Highlight or point to:
   - `DbUpdateException` catch
   - Check for `ix_users_email_unique` in error message
   - User-friendly error message
4. Explain: "When the database unique index prevents a duplicate email, we catch that specific error and show a user-friendly message instead of crashing."

---

## Part 3: Live Demonstration

### Test: Duplicate Email Prevention

**Steps to record**:

1. **Start app** (if not running)
   ```bash
   dotnet run
   ```

2. **Register first user**:
   - Navigate to: http://localhost:5000/account/register
   - Fill form:
     - Name: `John Doe`
     - Email: `duplicate@gmail.com`
     - Password: `password123`
   - Click "Register"
   - See success message

3. **Try to register same email**:
   - Navigate back to: http://localhost:5000/account/register
   - Fill form:
     - Name: `Jane Doe`
     - Email: `duplicate@gmail.com` (SAME email)
     - Password: `password456`
   - Click "Register"
   - **See error**: "An account with this email already exists."
   - Point out: This error is caught from the database unique index violation

4. **Verify in database**:
   - Open terminal/pgAdmin
   - Run query:
     ```sql
     SELECT COUNT(*) FROM users WHERE email = 'duplicate@gmail.com';
     ```
   - Show result: `1` (only one user, not two)
   - This proves the constraint prevented insertion

---

## Full Video Script (What to Say)

```
[Show pgAdmin/psql with index]

"Here we can see the unique index 'ix_users_email_unique' on the users table, 
column 'email'. This index is created at the DATABASE LEVEL, not in the application code.
It guarantees that no two users can have the same email address, even if registration 
requests come in simultaneously."

[Navigate to source code]

"In the application code, we catch the database constraint violation exception. 
When someone tries to register with an email that already exists, the database 
rejects it, and we catch that error specifically - looking for 
'ix_users_email_unique' in the error message."

[Show the error handling code]

"Then we convert that technical database error into a user-friendly message: 
'An account with this email already exists.'"

[Show live registration form]

"Now let me demonstrate this in action. I'll register a user with email: duplicate@gmail.com"

[Register user 1]

"Now I'll try to register again with the same email address."

[Try to register user 2 with same email]

"As you can see, the form shows the error message. And if we check the database..."

[Query the database]

"...we can confirm there's only ONE user with that email, not two. 
The database constraint prevented the duplicate from being inserted."
```

---

## Video Timeline (Recommended)

- **00:00-01:30**: Show database index in pgAdmin/psql
- **01:30-02:30**: Show source code (AccountController.cs) catching the error
- **02:30-03:00**: Register first user successfully
- **03:00-03:30**: Try duplicate registration, see error message
- **03:30-04:00**: Query database to confirm only one user exists

---

## Checklist for Video

Before recording, verify:

- [ ] PostgreSQL is running
- [ ] App is running (`dotnet run`)
- [ ] appsettings.json has valid database connection
- [ ] Index exists in database (`SELECT ... FROM pg_indexes`)
- [ ] Code file open and visible (AccountController.cs lines 103-107)
- [ ] Can reach http://localhost:5000
- [ ] Registration form works
- [ ] Verification email configured (or just show dummy message)
- [ ] pgAdmin or psql connection working

---

## Common Issues When Demonstrating

### Issue: Index doesn't exist in database
**Solution**: 
- Run `dotnet run` once to trigger EF Core migrations
- Database is auto-created and seeded with the index
- Verify with: `SELECT * FROM pg_indexes WHERE tablename = 'users'`

### Issue: Error message doesn't show
**Solution**:
- Make sure `appsettings.json` has correct database connection
- Try registering with an email that doesn't exist first (should work)
- Then try duplicate (should show error)
- Check browser console (F12) for any JavaScript errors

### Issue: Database shows 2 records with same email
**Solution**:
- The unique index constraint failed (you would have seen the error)
- Delete test users and try again: `DELETE FROM users WHERE email = 'test@gmail.com'`
- Ensure app was restarted after database was cleaned

---

## Evidence Checklist

Your video must show (for grading):

- [ ] **Index in Database**: pgAdmin/psql screenshot showing `ix_users_email_unique`
- [ ] **Index Definition**: Show it's UNIQUE and on the email column
- [ ] **Code**: Show `Controllers/AccountController.cs` lines 103-107
- [ ] **Error Catch**: Show the catch block for `DbUpdateException`
- [ ] **User Error Message**: Show "An account with this email already exists."
- [ ] **Live Test**: Register duplicate and see error
- [ ] **Database Verification**: Query showing only 1 record exists

---

## Grading Criteria

✅ **Will receive full credit if**:
- Video shows the unique index in the database UI
- Video shows the code catching the constraint violation
- Live demonstration shows error when registering duplicate
- Database is verified to only have one user after duplicate attempt

❌ **Will lose points if**:
- No database index shown
- No error handling code shown
- Error message is a generic/technical message (not user-friendly)
- Code checks for duplicate email (instead of relying on database constraint)
- Soft-delete used instead of physical deletion

---

## Quick Reference Commands

```bash
# Check if index exists
psql -U postgres -d usermgmt -c "SELECT indexname FROM pg_indexes WHERE tablename = 'users';"

# Show index details
psql -U postgres -d usermgmt -c "SELECT indexname, indexdef FROM pg_indexes WHERE tablename = 'users' AND indexname LIKE '%email%';"

# Count users by email
psql -U postgres -d usermgmt -c "SELECT email, COUNT(*) FROM users GROUP BY email HAVING COUNT(*) > 1;"

# Delete test users
psql -U postgres -d usermgmt -c "DELETE FROM users WHERE email LIKE '%duplicate%';"

# Start app for demo
cd c:\Users\User\Downloads\UserManagementApp && dotnet run
```

---

## Ready to Submit?

After your video is complete, you should have evidence of:

1. ✅ Database unique index visualization
2. ✅ Source code catching the constraint error  
3. ✅ Live demonstration of duplicate prevention
4. ✅ Database state verification (only 1 user despite 2 registration attempts)

This will satisfy the requirement: "DEMONSTRATION OF THE INDEX IN THE DATABASE AS WELL AS PLACE IN THE CODE THAT CATCHES THE CORRESPONDING ERROR"

