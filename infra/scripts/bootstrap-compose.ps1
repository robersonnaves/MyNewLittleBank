#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Bootstrap docker compose infrastructure for MyNewLittleBank
.DESCRIPTION
    Starts docker/podman compose services with specified profile (local or ci)
.PARAMETER Profile
    The compose profile to use (local or ci). Default: local
.EXAMPLE
    .\bootstrap-compose.ps1
    .\bootstrap-compose.ps1 -Profile ci
#>

[CmdletBinding()]
param(
    [Parameter(Position = 0)]
    [ValidateSet('local', 'ci')]
    [string]$Profile = 'local'
)

$ErrorActionPreference = 'Stop'

$ScriptDir = Split-Path -Parent $PSCommandPath
$ComposeFile = Join-Path $ScriptDir '..\docker-compose.yml'
$Engine = if ($env:CONTAINER_ENGINE) { $env:CONTAINER_ENGINE } else { 'podman' }

$env:COMPOSE_PROJECT_NAME = 'mynewlittlebank'

Write-Host "Using container engine: $Engine" -ForegroundColor Cyan
Write-Host "Profile: $Profile" -ForegroundColor Cyan

if ($Profile -eq 'ci') {
    Write-Host "Cleaning up existing CI containers..." -ForegroundColor Yellow
    try {
        if ($Engine -eq 'docker') {
            & $Engine compose -f $ComposeFile --profile $Profile down -v --remove-orphans
        } else {
            & $Engine compose -f $ComposeFile down -v --remove-orphans
        }
    } catch {
        Write-Warning "Cleanup failed or no containers to remove: $_"
    }
}

Write-Host "Starting compose services..." -ForegroundColor Green
if ($Engine -eq 'docker') {
    & $Engine compose -f $ComposeFile --profile $Profile up -d
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start compose services"
    }
    & $Engine compose -f $ComposeFile --profile $Profile ps
} else {
    & $Engine compose -f $ComposeFile up -d
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start compose services"
    }
    & $Engine compose -f $ComposeFile ps
}

Write-Host "Bootstrap complete!" -ForegroundColor Green
