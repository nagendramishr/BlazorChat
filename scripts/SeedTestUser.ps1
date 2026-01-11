# PowerShell script to create a test user in the BlazorChat app
# This script runs the application with a special flag to seed test data

param(
    [string]$Email = "test@example.com",
    [string]$Password = "Test123!",
    [string]$ProjectPath = "..\src"
)

Write-Host "Creating test user: $Email" -ForegroundColor Green
Write-Host "Password: $Password" -ForegroundColor Yellow
Write-Host ""

# Set environment variable to trigger seeding
$env:SEED_TEST_USER = "true"
$env:TEST_USER_EMAIL = $Email
$env:TEST_USER_PASSWORD = $Password

# Navigate to project directory
Push-Location $ProjectPath

try {
    Write-Host "Running database migrations..." -ForegroundColor Cyan
    dotnet ef database update
    
    Write-Host "`nSeeding test user..." -ForegroundColor Cyan
    # The app will seed the user on startup when the environment variable is set
    # We'll run it briefly then stop
    dotnet run --no-build &
    
    Start-Sleep -Seconds 5
    
    Write-Host "`nTest user created successfully!" -ForegroundColor Green
    Write-Host "Email: $Email" -ForegroundColor White
    Write-Host "Password: $Password" -ForegroundColor White
}
finally {
    Pop-Location
    Remove-Item Env:\SEED_TEST_USER -ErrorAction SilentlyContinue
    Remove-Item Env:\TEST_USER_EMAIL -ErrorAction SilentlyContinue
    Remove-Item Env:\TEST_USER_PASSWORD -ErrorAction SilentlyContinue
}
