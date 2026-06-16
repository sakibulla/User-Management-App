-- =============================================================
-- UserManagementApp — Database Setup Script (PostgreSQL)
-- Run this once against your PostgreSQL database if you prefer
-- manual setup instead of EF Core's EnsureCreated().
-- =============================================================

-- IMPORTANT: Create the database (run this connected to "postgres" db)
-- CREATE DATABASE usermgmt;

-- Connect to usermgmt, then run everything below:

-- NOTE: Main users table
CREATE TABLE IF NOT EXISTS users (
    id              SERIAL PRIMARY KEY,           -- primary key (auto-increment)
    name            VARCHAR(200)    NOT NULL,
    email           VARCHAR(320)    NOT NULL,     -- uniqueness enforced below, NOT here
    password_hash   TEXT            NOT NULL,
    status          VARCHAR(20)     NOT NULL DEFAULT 'unverified',  -- unverified|active|blocked
    registered_at   TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    last_login_at   TIMESTAMPTZ,                  -- NULL = never logged in
    verification_token TEXT                       -- NULL after email is verified
);

-- =============================================================
-- IMPORTANT: UNIQUE INDEX — this is the core uniqueness guarantee.
-- This is NOT the primary key. It is a separate unique index on email.
-- It guarantees email uniqueness at the storage level regardless of
-- how many concurrent application instances write simultaneously.
-- Per assignment: "YOUR STORAGE SHOULD GUARANTEE E-MAIL UNIQUENESS
-- INDEPENDENTLY OF HOW MANY SOURCES PUSH DATA INTO IT SIMULTANEOUSLY."
-- NOTA BENE: Application code does NOT check for duplicate emails.
-- =============================================================
CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email_unique ON users (email);

-- NOTE: Index on last_login_at to speed up ORDER BY last_login_at DESC
CREATE INDEX IF NOT EXISTS ix_users_last_login ON users (last_login_at DESC NULLS LAST);

-- NOTE: Index on status for fast filtering (e.g. DELETE WHERE status = 'unverified')
CREATE INDEX IF NOT EXISTS ix_users_status ON users (status);

-- Verify the unique index was created
SELECT
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'users'
ORDER BY indexname;
