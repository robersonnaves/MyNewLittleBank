#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Reset database and recreate migrations for MyNewLittleBank
.DESCRIPTION
    Drops the database, removes all existing migrations, creates a new initial migration, and updates the database
.PARAMETER Force
    Skip confirmation prompts
.PARAMETER Backup
    Create a backup before dropping the database
.PARAMETER ConnectionString
    Override the default connection string from appsettings.json
.EXAMPLE
    .\reset-database.ps1
    .\reset-database.ps1 -Force
    .\reset-database.ps1 -Backup
    .\reset-database.ps1 -ConnectionString "Host=localhost;Port=5432;Database=mynewlittlebank;Username=postgres;Password=S3gr3d0123"
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$Force,
    
    [Parameter()]
    [switch]$Backup,
    
    [Parameter()]
    [string]$ConnectionString
)

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $PSCommandPath))
$ApiProject = Join-Path $RepoRoot 'src\API\API.csproj'
$InfraDbProject = Join-Path $RepoRoot 'src\Infra.Database\Infra.Database.csproj'
$MigrationsFolder = Join-Path $RepoRoot 'src\Infra.Database\Migrations'
$AppsettingsPath = Join-Path $RepoRoot 'src\API\appsettings.json'

# Validate projects exist
if (-not (Test-Path $ApiProject)) {
    Write-Host "Error: API project not found at $ApiProject" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $InfraDbProject)) {
    Write-Host "Error: Infra.Database project not found at $InfraDbProject" -ForegroundColor Red
    exit 1
}

# Get connection string
if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
    if (Test-Path $AppsettingsPath) {
        $appsettings = Get-Content $AppsettingsPath -Raw | ConvertFrom-Json
        $ConnectionString = $appsettings.ConnectionStrings.DefaultConnection
    }
    
    if ([string]::IsNullOrWhiteSpace($ConnectionString)) {
        Write-Host "Error: Could not find connection string in appsettings.json" -ForegroundColor Red
        exit 1
    }
}

Write-Host "`n=== MyNewLittleBank Database Reset ===" -ForegroundColor Cyan
Write-Host "Connection String: $ConnectionString" -ForegroundColor Yellow
Write-Host ""

# Parse connection string
$connParams = @{}
$ConnectionString -split ';' | ForEach-Object {
    if ($_ -match '(.+?)=(.+)') {
        $connParams[$matches[1].Trim()] = $matches[2].Trim()
    }
}

$dbHost = $connParams['Host']
$dbPort = $connParams['Port']
$dbName = $connParams['Database']
$dbUser = $connParams['Username']
$dbPassword = $connParams['Password']

# Confirm action
if (-not $Force) {
    Write-Host "WARNING: This will:" -ForegroundColor Yellow
    Write-Host "  1. Drop database '$dbName'" -ForegroundColor Yellow
    Write-Host "  2. Delete all migrations in '$MigrationsFolder'" -ForegroundColor Yellow
    Write-Host "  3. Create a new InitialCreate migration" -ForegroundColor Yellow
    Write-Host "  4. Update the database with the new migration" -ForegroundColor Yellow
    Write-Host ""
    $confirmation = Read-Host "Are you sure you want to continue? (yes/no)"
    if ($confirmation -ne 'yes') {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
        exit 0
    }
}

# Check if dotnet ef tool is installed
Write-Host "`nChecking dotnet ef tool..." -ForegroundColor Cyan
try {
    $efVersion = dotnet ef --version 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet ef not found"
    }
    Write-Host "✓ dotnet ef found: $efVersion" -ForegroundColor Green
} catch {
    Write-Host "Error: dotnet ef tool not installed. Installing..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Error: Failed to install dotnet ef tool" -ForegroundColor Red
        exit 1
    }
}

# Backup database if requested
if ($Backup) {
    Write-Host "`nCreating database backup..." -ForegroundColor Cyan
    $timestamp = Get-Date -Format 'yyyyMMdd_HHmmss'
    $backupFile = Join-Path $RepoRoot "backup_${dbName}_${timestamp}.sql"
    
    $env:PGPASSWORD = $dbPassword
    pg_dump -h $dbHost -p $dbPort -U $dbUser -d $dbName -f $backupFile 2>&1
    
    if ($LASTEXITCODE -eq 0 -and (Test-Path $backupFile)) {
        Write-Host "✓ Backup created: $backupFile" -ForegroundColor Green
    } else {
        Write-Host "Warning: Backup failed, but continuing..." -ForegroundColor Yellow
    }
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

# Drop database
Write-Host "`nDropping database '$dbName'..." -ForegroundColor Cyan
$env:PGPASSWORD = $dbPassword
try {
    # Try to drop database (connecting to postgres database)
    $dropQuery = "DROP DATABASE IF EXISTS `"$dbName`";"
    psql -h $dbHost -p $dbPort -U $dbUser -d postgres -c $dropQuery 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✓ Database dropped successfully" -ForegroundColor Green
    } else {
        Write-Host "Warning: Could not drop database (it may not exist)" -ForegroundColor Yellow
    }
} finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
}

# Remove existing migrations
Write-Host "`nRemoving existing migrations..." -ForegroundColor Cyan
if (Test-Path $MigrationsFolder) {
    $migrationFiles = Get-ChildItem -Path $MigrationsFolder -File
    if ($migrationFiles.Count -gt 0) {
        Remove-Item -Path "$MigrationsFolder\*" -Force
        Write-Host "✓ Removed $($migrationFiles.Count) migration file(s)" -ForegroundColor Green
    } else {
        Write-Host "✓ No migrations to remove" -ForegroundColor Green
    }
} else {
    Write-Host "✓ Migrations folder doesn't exist" -ForegroundColor Green
}

# Create new initial migration
Write-Host "`nCreating new InitialCreate migration..." -ForegroundColor Cyan
dotnet ef migrations add InitialCreate `
    --project $InfraDbProject `
    --startup-project $ApiProject `
    --context MyNewLittleBankContext `
    --output-dir Migrations

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to create migration" -ForegroundColor Red
    exit 1
}
Write-Host "✓ Migration created successfully" -ForegroundColor Green

# Update database
Write-Host "`nUpdating database..." -ForegroundColor Cyan
dotnet ef database update `
    --project $InfraDbProject `
    --startup-project $ApiProject `
    --context MyNewLittleBankContext `
    --connection $ConnectionString

if ($LASTEXITCODE -ne 0) {
    Write-Host "Error: Failed to update database" -ForegroundColor Red
    exit 1
}

Write-Host "`n✓ Database reset complete!" -ForegroundColor Green
Write-Host "`nSummary:" -ForegroundColor Cyan
Write-Host "  - Database: $dbName" -ForegroundColor White
Write-Host "  - Host: ${dbHost}:${dbPort}" -ForegroundColor White
Write-Host "  - Migrations: New InitialCreate migration created" -ForegroundColor White
