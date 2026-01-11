# Development Scripts

## Create Test User

### Option 1: Using PowerShell (Windows)
```powershell
.\SeedTestUser.ps1
```

With custom credentials:
```powershell
.\SeedTestUser.ps1 -Email "mytest@example.com" -Password "MyPassword123!"
```

### Option 2: Using Bash (Linux/Mac/WSL)
```bash
chmod +x create-test-user.sh
./create-test-user.sh
```

With custom credentials:
```bash
./create-test-user.sh "mytest@example.com" "MyPassword123!"
```

### Option 3: Automatic on Startup (Development Only)

Set environment variables before running the app:
```powershell
# PowerShell
$env:SEED_TEST_USER="true"
$env:TEST_USER_EMAIL="test@example.com"
$env:TEST_USER_PASSWORD="Test123!"
dotnet run --project ../src
```

```bash
# Bash
export SEED_TEST_USER="true"
export TEST_USER_EMAIL="test@example.com"
export TEST_USER_PASSWORD="Test123!"
dotnet run --project ../src
```

### Option 4: Using the Registration Page

Simply run the app and navigate to the registration page:
```
https://localhost:5001/Account/Register
```

**Note:** The app currently requires email confirmation. The test user created via scripts has `EmailConfirmed = true` automatically.

## Default Test Credentials

When using the scripts without parameters:
- **Email:** test@example.com
- **Password:** Test123!

## SQLite Database

SQLite is an embedded database and doesn't require a separate server process. The database file is located at:
```
src/Data/app.db
```

You can view the database using:
- [DB Browser for SQLite](https://sqlitebrowser.org/)
- VS Code SQLite extension
- Command line: `sqlite3 src/Data/app.db`

## Resetting the Database

To start fresh:
```bash
cd ../src
rm Data/app.db
dotnet ef database update
```
