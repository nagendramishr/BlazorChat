#!/bin/bash
# Bash script to create a test user in the BlazorChat app

EMAIL="${1:-test@example.com}"
PASSWORD="${2:-Test123!}"
PROJECT_PATH="../src"

echo "Creating test user: $EMAIL"
echo "Password: $PASSWORD"
echo ""

# Set environment variables
export SEED_TEST_USER="true"
export TEST_USER_EMAIL="$EMAIL"
export TEST_USER_PASSWORD="$PASSWORD"

cd "$PROJECT_PATH" || exit 1

echo "Running database migrations..."
dotnet ef database update

echo ""
echo "Seeding test user..."
dotnet run --no-build

echo ""
echo "Test user created successfully!"
echo "Email: $EMAIL"
echo "Password: $PASSWORD"

# Clean up
unset SEED_TEST_USER
unset TEST_USER_EMAIL
unset TEST_USER_PASSWORD
