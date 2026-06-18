#!/bin/bash
# This script initializes the database for Railway deployment
# Run this once when deploying to create the schema

set -e

echo "Waiting for database to be ready..."
sleep 10

echo "Creating database tables..."
dotnet UserManagementApp.dll --init-db

echo "Database initialization complete!"
