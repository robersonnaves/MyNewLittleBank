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
$Engine = if ($env:CONTAINER_ENGINE) { $env:CONTAINER_ENGINE } else { 'podman' }

$env:COMPOSE_PROJECT_NAME = 'mynewlittlebank'

# Detect platform
$OSPlatform = if ($IsLinux) { 
    'linux' 
} elseif ($IsMacOS) { 
    'macos' 
} elseif ($IsWindows) { 
    'windows' 
} else { 
    'unknown' 
}

$OSArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLower()

Write-Host "Detected platform: $OSPlatform/$OSArch" -ForegroundColor Cyan
Write-Host "Using container engine: $Engine" -ForegroundColor Cyan
Write-Host "Profile: $Profile" -ForegroundColor Cyan

# Compose files setup
$ComposeBase = Join-Path $ScriptDir '..\docker-compose.yml'
$ComposeLinux = Join-Path $ScriptDir '..\docker-compose.linux.yml'
$ComposeOverride = Join-Path $ScriptDir '..\docker-compose.override.yml'

$ComposeFiles = @('-f', $ComposeBase)

# Add Linux override if on Linux and file exists
if ($OSPlatform -eq 'linux' -and (Test-Path $ComposeLinux)) {
    Write-Host "Using Linux-specific overrides (SELinux)" -ForegroundColor Cyan
    $ComposeFiles += @('-f', $ComposeLinux)
}

# Add local override if exists
if (Test-Path $ComposeOverride) {
    Write-Host "Using local overrides" -ForegroundColor Cyan
    $ComposeFiles += @('-f', $ComposeOverride)
}

if ($Profile -eq 'ci') {
    Write-Host "Cleaning up existing CI containers..." -ForegroundColor Yellow
    try {
        if ($Engine -eq 'docker') {
            & $Engine compose $ComposeFiles --profile $Profile down -v --remove-orphans
        } else {
            & $Engine compose $ComposeFiles down -v --remove-orphans
        }
    } catch {
        Write-Warning "Cleanup failed or no containers to remove: $_"
    }
}

Write-Host "Starting compose services..." -ForegroundColor Green
if ($Engine -eq 'docker') {
    & $Engine compose $ComposeFiles --profile $Profile up -d
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start compose services"
    }
    & $Engine compose $ComposeFiles --profile $Profile ps
} else {
    & $Engine compose $ComposeFiles up -d
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to start compose services"
    }
    & $Engine compose $ComposeFiles ps
}

Write-Host "Bootstrap complete!" -ForegroundColor Green
