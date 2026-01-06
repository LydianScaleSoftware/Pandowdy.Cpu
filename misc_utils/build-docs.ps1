#!/usr/bin/env pwsh
# Build DocFX documentation without serving
# This script builds the documentation site to _site folder

$ErrorActionPreference = "Stop"

Write-Host "Building Pandowdy documentation..." -ForegroundColor Cyan

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

# Build only the main projects to generate XML documentation
Write-Host "Building projects to generate XML documentation..." -ForegroundColor Yellow

$projectsToBuild = @(
    "Pandowdy.EmuCore\Pandowdy.EmuCore.csproj",
    "Pandowdy.UI\Pandowdy.UI.csproj",
    "Pandowdy\Pandowdy.csproj"
)

foreach ($project in $projectsToBuild)
{
    $projectPath = Join-Path $solutionRoot $project
    Write-Host "  Building $project..." -ForegroundColor Gray
    dotnet build $projectPath -c Release -v quiet
    
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "ERROR: Build failed for $project" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

Write-Host "Building DocFX documentation..." -ForegroundColor Green

Push-Location $docfxPath
try
{
    # Generate metadata from XML files
    docfx metadata docfx.json
    
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "ERROR: DocFX metadata generation failed" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
    # Build the documentation site
    docfx build docfx.json
    
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "ERROR: DocFX build failed" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    
    Write-Host ""
    Write-Host "Documentation built successfully!" -ForegroundColor Green
    Write-Host "Output: docfx_project\_site" -ForegroundColor Cyan
    Write-Host "To view locally, run: .\misc_utils\start-docfx-server.ps1" -ForegroundColor Yellow
}
finally
{
    Pop-Location
}
