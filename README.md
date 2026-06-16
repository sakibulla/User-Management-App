# UserManagementApp — Task #5

ASP.NET Core 8 + PostgreSQL user management web application.

---

## Tech stack

| Layer      | Technology                        |
|------------|-----------------------------------|
| Backend    | C#, ASP.NET Core 8 MVC            |
| Database   | PostgreSQL (Npgsql EF Core)       |
| Auth       | Cookie authentication (built-in)  |
| Password   | BCrypt.Net-Next                   |
| Email      | MailKit (SMTP, async)             |
| Frontend   | Bootstrap 5 + Bootstrap Icons     |

---

## Quick start (local)

### Prerequisites
- .NET 8 SDK
- PostgreSQL 14+

### 1. Create the database

```sql
-- In psql:
CREATE DATABASE usermgmt;
```

Or run the full `db_setup.sql` script (creates table + all indexes).

### 2. Configure connection string

Edit `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=usermgmt;Username=postgres;Password=YOUR_PASSWORD"
}
```

### 3. Configure email (optional)

The app registers users immediately without email confirmation.
Verification email is sent **asynchronously** — if SMTP is not configured, it is silently skipped.

```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "your@gmail.com",
  "SmtpPass": "your-app-password",
  "FromName": "User Management App"
}
```

> For Gmail, use an [App Password](https://support.google.com/accounts/answer/185833) (not your main password).

### 4. Run

```bash
dotnet run
```

App starts on `http://localhost:5000` (or the port in launchSettings.json).

---

## Deployment (Railway / Render / Azure)

Set these environment variables in your hosting platform:

```
ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...
Email__SmtpUser=your@gmail.com
Email__SmtpPass=your-app-password
ASPNETCORE_ENVIRONMENT=Production
```

The app calls `db.Database.EnsureCreated()` on startup — tables and indexes are created automatically on first run.

---

## Assignment requirements checklist

| Requirement | Implementation |
|---|---|
| ✅ Unique index (NOT just PK) | `CREATE UNIQUE INDEX ix_users_email_unique ON users (email)` — in `AppDbContext.OnModelCreating` and `db_setup.sql` |
| ✅ Email uniqueness at storage level | DB constraint only — no `WHERE email = ?` check in code |
| ✅ Table looks like a table, toolbar looks like a toolbar | Bootstrap 5 table + toolbar above it |
| ✅ Data sorted by last login time | `OrderByDescending(u => u.LastLoginAt)` in `UsersController.Index` |
| ✅ Multiple selection via checkboxes | Row checkboxes + select-all header checkbox |
| ✅ Select all / deselect all is a checkbox | Header `<th>` contains only `<input type="checkbox" id="select-all">` |
| ✅ Middleware checks user exists & not blocked | `UserStatusMiddleware` runs on every request except login/register |
| ✅ Non-authenticated users → login/register only | `[Authorize]` on `UsersController` + cookie scheme redirects |
| ✅ Table columns: checkbox, name, email, last login, status | Rendered in `Users/Index.cshtml` |
| ✅ Block (button with text) | `<button>Block</button>` with icon prefix |
| ✅ Unblock / Delete / Delete-unverified (icons only) | Icon-only buttons using Bootstrap Icons |
| ✅ No buttons in data rows | Zero action buttons in `<tr>` rows |
| ✅ Toolbar always visible (never hidden) | Toolbar div is always rendered; buttons only enabled/disabled |
| ✅ Any user can block/delete any user including themselves | No ownership checks in `BulkAction` |
| ✅ Any non-empty password | `[MinLength(1)]` validation |
| ✅ Unverified users can log in | Status check only blocks `"blocked"` status |
| ✅ Deleted users are really deleted | `ExecuteDeleteAsync()` — no soft-delete flag |
| ✅ Registration succeeds immediately | Redirect to login with success message before email sends |
| ✅ Verification email sent asynchronously | `_ = _emailService.SendVerificationEmailAsync(...)` |
| ✅ Email click → status unverified→active (blocked stays blocked) | `AccountController.Verify` checks before updating |
| ✅ Blocked user cannot login | Status checked in `AccountController.Login` |
| ✅ Deleted user can re-register | Physical delete; unique index allows re-use of same email |
| ✅ Bootstrap CSS framework | Bootstrap 5 via CDN |
| ✅ Tooltips | `title=` attributes on all interactive elements |
| ✅ Status messages | Bootstrap toasts for AJAX actions; TempData alerts for page-load |
| ✅ Adequate error messages | Model validation + catch of DB unique constraint |
| ✅ No browser alerts | All feedback via Bootstrap toasts / alert divs |
| ✅ No wallpapers / animations | Plain `#f8f9fa` background; zero CSS animations |
| ✅ Proper text alignment | Right-aligned dates, center-aligned status, left-aligned name/email |
| ✅ Responsive (any resolution) | Bootstrap grid + responsive table wrapper |
| ✅ `getUniqIdValue` function | Defined in `wwwroot/js/site.js`; used in `Users/Index.cshtml` |
| ✅ `// important`, `// note`, `// nota bene` comments | Present throughout all C# and JS files |

---

## Key files

```
UserManagementApp/
├── Controllers/
│   ├── AccountController.cs   — Login, Register, Verify, Logout
│   └── UsersController.cs     — Admin table + BulkAction endpoint
├── Data/
│   └── AppDbContext.cs        — EF Core context; unique index defined here
├── Middleware/
│   └── UserStatusMiddleware.cs — Per-request blocked/deleted user check
├── Models/
│   ├── User.cs                — DB entity
│   └── ViewModels.cs          — Form models + table row DTO
├── Services/
│   └── EmailService.cs        — Async SMTP email via MailKit
├── Views/
│   ├── Account/Login.cshtml
│   ├── Account/Register.cshtml
│   ├── Users/Index.cshtml     — Admin table with toolbar + JS
│   └── Shared/_Layout.cshtml — Bootstrap layout + nav
├── wwwroot/js/site.js         — getUniqIdValue(), showToast() helpers
├── db_setup.sql               — Manual SQL with unique index
├── Program.cs                 — App bootstrap + middleware pipeline
└── appsettings.json           — Connection string + email config
```
