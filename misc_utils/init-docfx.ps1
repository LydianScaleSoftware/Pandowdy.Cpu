#!/usr/bin/env pwsh
# Initialize DocFX documentation project
# Run this once to set up the docfx_project folder

$ErrorActionPreference = "Stop"

Write-Host "Initializing DocFX documentation project..." -ForegroundColor Cyan

# Check if docfx is installed
$docfxInstalled = Get-Command docfx -ErrorAction SilentlyContinue
if (-not $docfxInstalled)
{
    Write-Host "ERROR: docfx not found. Install it first:" -ForegroundColor Red
    Write-Host "  dotnet tool install -g docfx" -ForegroundColor Yellow
    exit 1
}

# Get solution root
$solutionRoot = Split-Path $PSScriptRoot -Parent
$docfxPath = Join-Path $solutionRoot "docfx_project"

if (Test-Path $docfxPath)
{
    Write-Host "WARNING: docfx_project already exists at: $docfxPath" -ForegroundColor Yellow
    $response = Read-Host "Do you want to recreate it? (y/N)"
    if ($response -ne 'y' -and $response -ne 'Y')
    {
        Write-Host "Initialization cancelled." -ForegroundColor Yellow
        exit 0
    }
    Remove-Item -Path $docfxPath -Recurse -Force
}

# Initialize DocFX with yes to all prompts
Write-Host "Creating docfx_project..." -ForegroundColor Green
Push-Location $solutionRoot
try
{
    docfx init --yes --output docfx_project
    
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "ERROR: DocFX initialization failed" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}
finally
{
    Pop-Location
}

Write-Host ""
Write-Host "DocFX project initialized successfully!" -ForegroundColor Green
Write-Host "Location: $docfxPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Edit docfx_project/docfx.json to configure your documentation" -ForegroundColor White
Write-Host "  2. Run: .\misc_utils\start-docfx-server.ps1" -ForegroundColor White
Write-Host "  3. Navigate to: http://localhost:8080" -ForegroundColor White
