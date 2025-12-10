#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Run integration tests for MyNewLittleBank
.DESCRIPTION
    Bootstraps compose infrastructure, runs integration tests, and cleans up on CI
.PARAMETER Profile
    The compose profile to use (ci or local). Default: ci
.EXAMPLE
    .\run-integration-tests.ps1
    .\run-integration-tests.ps1 -Profile local
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('local', 'ci')]
    [string]$Profile = 'ci'
)

$ErrorActionPreference = 'Stop'

$RootDir = Split-Path -Parent (Split-Path -Parent $PSCommandPath)
$ScriptDir = Split-Path -Parent $PSCommandPath
$Engine = if ($env:CONTAINER_ENGINE) { $env:CONTAINER_ENGINE } else { 'podman' }
$ComposeFile = Join-Path $RootDir 'infra\docker-compose.yml'
$TestProject = Join-Path $RootDir 'tests\Integration\MyNewLittleBank.Tests.Integration.csproj'
$ResultsDir = Join-Path $RootDir 'artifacts\test-results'

Write-Host "Bootstrapping compose infrastructure..." -ForegroundColor Cyan
& "$ScriptDir\bootstrap-compose.ps1" -Profile $Profile

if ($LASTEXITCODE -ne 0) {
    throw "Failed to bootstrap compose infrastructure"
}

Write-Host "`nRunning integration tests..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null

try {
    dotnet test $TestProject `
        --filter "Category=Integration" `
        --logger "trx;LogFileName=integration.trx" `
        --results-directory $ResultsDir
    
    $testExitCode = $LASTEXITCODE
} finally {
    if ($Profile -eq 'ci') {
        Write-Host "`nCleaning up CI environment..." -ForegroundColor Yellow
        & $Engine compose -f $ComposeFile --profile $Profile down -v --remove-orphans
    }
}

if ($testExitCode -ne 0) {
    Write-Host "`nIntegration tests failed!" -ForegroundColor Red
    exit $testExitCode
}

Write-Host "`nIntegration tests completed successfully!" -ForegroundColor Green
