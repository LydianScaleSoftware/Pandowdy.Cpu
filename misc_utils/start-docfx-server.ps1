#!/usr/bin/env pwsh
# Start DocFX development server
# This script starts the DocFX server for local documentation viewing

$ErrorActionPreference = "Stop"

Write-Host "Starting DocFX documentation server..." -ForegroundColor Cyan

# Navigate to docfx project directory
$solutionRoot = Split-Path $PSScriptRoot -Parent
$docfxPath = Join-Path $solutionRoot "docfx_project"

if (-not (Test-Path $docfxPath))
{
    Write-Host "ERROR: docfx_project folder not found at: $docfxPath" -ForegroundColor Red
    Write-Host "Run '.\misc_utils\init-docfx.ps1' from the solution root first." -ForegroundColor Yellow
    exit 1
}

# Check if docfx is installed
$docfxInstalled = Get-Command docfx -ErrorAction SilentlyContinue
if (-not $docfxInstalled)
{
    Write-Host "ERROR: docfx not found. Install it first:" -ForegroundColor Red
    Write-Host "  dotnet tool install -g docfx" -ForegroundColor Yellow
    exit 1
}

Write-Host "Building and serving documentation..." -ForegroundColor Green
Write-Host "Navigate to: http://localhost:8080" -ForegroundColor Cyan
Write-Host "Press Ctrl+C to stop the server." -ForegroundColor Yellow
Write-Host ""

Push-Location $docfxPath
try
{
    docfx docfx.json --serve
}
finally
{
    Pop-Location
}
