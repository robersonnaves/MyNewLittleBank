#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Stop all MyNewLittleBank services
.DESCRIPTION
    Stops services via docker-compose with optional stack selection
.PARAMETER ObservabilityOnly
    Stop only the observability stack
.PARAMETER MainOnly
    Stop only the main application stack
.PARAMETER SkipObservability
    Skip stopping the observability stack
.PARAMETER SkipMain
    Skip stopping the main application stack
.EXAMPLE
    .\stop-all.ps1
    .\stop-all.ps1 -ObservabilityOnly
    .\stop-all.ps1 -MainOnly
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$ObservabilityOnly,
    
    [Parameter()]
    [switch]$MainOnly,
    
    [Parameter()]
    [switch]$SkipObservability,
    
    [Parameter()]
    [switch]$SkipMain
)

$ErrorActionPreference = 'Stop'

# Determine which stacks to stop
$StopMain = $true
$StopObservability = $true

if ($ObservabilityOnly) {
    $StopMain = $false
    $StopObservability = $true
}

if ($MainOnly) {
    $StopMain = $true
    $StopObservability = $false
}

if ($SkipObservability) {
    $StopObservability = $false
}

if ($SkipMain) {
    $StopMain = $false
}

# Validate combinations
if (-not $StopMain -and -not $StopObservability) {
    Write-Host "Warning: Both stacks disabled. Nothing to stop." -ForegroundColor Cyan
    exit 0
}

$ScriptDir = Split-Path -Parent $PSCommandPath
$ComposeBase = Join-Path $ScriptDir '..\docker-compose.yml'
$ComposeLinux = Join-Path $ScriptDir '..\docker-compose.linux.yml'
$ComposeOverride = Join-Path $ScriptDir '..\docker-compose.override.yml'
$Engine = if ($env:CONTAINER_ENGINE) { $env:CONTAINER_ENGINE } else { 'podman' }
$ProjectName = 'mynewlittlebank'

try {
    # Build compose file arguments
    $ComposeArgs = @('-f', $ComposeBase)
    if ($IsLinux -and (Test-Path $ComposeLinux)) {
        $ComposeArgs += @('-f', $ComposeLinux)
    }
    if (Test-Path $ComposeOverride) {
        $ComposeArgs += @('-f', $ComposeOverride)
    }

    Write-Host "Stopping services..." -ForegroundColor Cyan
    
    if ($StopMain) {
        Write-Host "Stopping main application stack..." -ForegroundColor Cyan
        & $Engine compose $ComposeArgs -p $ProjectName down
        Write-Host "Main application stack stopped." -ForegroundColor Green
    } else {
        Write-Host "Main application stack skipped." -ForegroundColor Cyan
    }
    
    if ($StopObservability) {
        Write-Host "Stopping observability stack..." -ForegroundColor Cyan
        $ObservabilityComposeFile = Join-Path $ScriptDir '..\docker-compose.observability.yml'
        & $Engine compose -f $ObservabilityComposeFile -p "${ProjectName}-observability" down
        Write-Host "Observability stack stopped." -ForegroundColor Green
    } else {
        Write-Host "Observability stack skipped." -ForegroundColor Cyan
    }
    
    Write-Host "`nDone." -ForegroundColor Green
} catch {
    Write-Host "`nError: $_" -ForegroundColor Red
    exit 1
}
