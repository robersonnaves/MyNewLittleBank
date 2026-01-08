#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Start all MyNewLittleBank services (infrastructure and application)
.DESCRIPTION
    Starts all services via docker-compose with optional Mock.Transactions control
.PARAMETER MockTransactionsEnabled
    Whether to run Mock.Transactions. Default: true
.EXAMPLE
    .\start-all.ps1
    .\start-all.ps1 -MockTransactionsEnabled $false
    $env:MOCK_TRANSACTIONS_ENABLED='false'; .\start-all.ps1
#>

[CmdletBinding()]
param(
    [Parameter()]
    [bool]$MockTransactionsEnabled = $(
        if ($env:MOCK_TRANSACTIONS_ENABLED -eq 'false') { $false } else { $true }
    )
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $PSCommandPath
$ComposeBase = Join-Path $ScriptDir '..\docker-compose.yml'
$ComposeLinux = Join-Path $ScriptDir '..\docker-compose.linux.yml'
$ComposeOverride = Join-Path $ScriptDir '..\docker-compose.override.yml'
$Engine = if ($env:CONTAINER_ENGINE) { $env:CONTAINER_ENGINE } else { 'podman' }
$ProjectName = 'mynewlittlebank'

# Docker-compose handles all process management

try {
    $platform = if ($IsLinux) { 'linux' } elseif ($IsMacOS) { 'macos' } elseif ($IsWindows) { 'windows' } else { 'unknown' }
    Write-Host "Detected platform: $platform" -ForegroundColor Cyan
    Write-Host "Using container engine: $Engine" -ForegroundColor Cyan

    # Build compose file arguments (parity with bash script)
    $ComposeArgs = @('-f', $ComposeBase)
    if ($IsLinux -and (Test-Path $ComposeLinux)) {
        Write-Host "Using Linux-specific overrides (SELinux)" -ForegroundColor Cyan
        $ComposeArgs += @('-f', $ComposeLinux)
    }
    if (Test-Path $ComposeOverride) {
        Write-Host "Using local overrides" -ForegroundColor Cyan
        $ComposeArgs += @('-f', $ComposeOverride)
    }

    Write-Host "Starting all services..." -ForegroundColor Cyan
    
    # Ensure network exists before starting stacks
    Write-Host "Ensuring bank-net network exists..." -ForegroundColor Cyan
    & $Engine network create bank-net 2>$null
    if ($LASTEXITCODE -ne 0 -and $LASTEXITCODE -ne 125) {
        # Error code 125 means network already exists, which is OK
        Write-Host "Note: Network creation returned code $LASTEXITCODE (may already exist)" -ForegroundColor Yellow
    }
    
    if ($MockTransactionsEnabled) {
        Write-Host "Mock.Transactions enabled - starting via docker-compose" -ForegroundColor Green
        & $Engine compose $ComposeArgs -p $ProjectName up -d --build
    } else {
        Write-Host "Mock.Transactions disabled - scaling to 0" -ForegroundColor Yellow
        & $Engine compose $ComposeArgs -p $ProjectName up -d --build --scale mock-transactions=0
    }
    
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start services"
    }

    Write-Host "Waiting for services to be ready..." -ForegroundColor Cyan
    Start-Sleep -Seconds 5

    Write-Host "`nAll services started." -ForegroundColor Green
    Write-Host "Mock.Transactions status: $(if ($MockTransactionsEnabled) { 'ENABLED (running in compose)' } else { 'DISABLED' })" -ForegroundColor Cyan
    
    # Start observability stack
    Write-Host "\nStarting observability stack..." -ForegroundColor Cyan
    $ObservabilityComposeFile = Join-Path $ScriptDir '..\docker-compose.observability.yml'
    & $Engine compose -f $ObservabilityComposeFile -p "${ProjectName}-observability" up -d
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Warning: Failed to start observability stack" -ForegroundColor Yellow
    } else {
        Write-Host "Observability stack started successfully" -ForegroundColor Green
    }
    
    Write-Host "\nService status:" -ForegroundColor Cyan
    & $Engine compose $ComposeArgs -p $ProjectName ps
    Write-Host "\nObservability stack status:" -ForegroundColor Cyan
    & $Engine compose -f $ObservabilityComposeFile -p "${ProjectName}-observability" ps
} catch {
    Write-Host "`nError: $_" -ForegroundColor Red
    exit 1
}

