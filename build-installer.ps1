# Build script for Server Backup Tool Installer
# Publishes the tool and API, packages them as versioned embedded ZIP resources,
# then publishes the installer as a self-contained single-file executable.
#
# Usage: .\build-installer.ps1
# Output: Server Backup Tool.Installer\bin\Release\net10.0\win-x64\publish\SBTInstaller.exe

param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"

$SolutionDir = $PSScriptRoot
$ToolProject = Join-Path $SolutionDir "Server Backup Tool\Server Backup Tool.csproj"
$ApiProject = Join-Path $SolutionDir "Server Backup Tool.API\Server Backup Tool.API.csproj"
$InstallerProject = Join-Path $SolutionDir "Server Backup Tool.Installer\Server Backup Tool.Installer.csproj"

$ToolPublishDir = Join-Path $SolutionDir "Server Backup Tool\bin\$Configuration\net10.0\$Runtime\publish"
$ApiPublishDir = Join-Path $SolutionDir "Server Backup Tool.API\bin\$Configuration\net10.0\$Runtime\publish"
$ResourcesDir = Join-Path $SolutionDir "Server Backup Tool.Installer\Resources"

Write-Host "=== Building Server Backup Tool Installer ===" -ForegroundColor Cyan
Write-Host ""

# Step 1: Publish the Server Backup Tool.
Write-Host "[1/6] Publishing Server Backup Tool..." -ForegroundColor Yellow
dotnet publish $ToolProject -c $Configuration -r $Runtime --self-contained true
if ($LASTEXITCODE -ne 0) { throw "Failed to publish Server Backup Tool." }

# Step 2: Publish the Server Backup Tool API.
Write-Host "[2/6] Publishing Server Backup Tool API..." -ForegroundColor Yellow
dotnet publish $ApiProject -c $Configuration -r $Runtime --self-contained true
if ($LASTEXITCODE -ne 0) { throw "Failed to publish Server Backup Tool API." }

# Step 3: Read versions from published assemblies.
Write-Host "[3/6] Reading assembly versions..." -ForegroundColor Yellow

$ToolDll = Join-Path $ToolPublishDir "Server Backup Tool.dll"
$ApiDll = Join-Path $ApiPublishDir "Server Backup Tool.API.dll"

$ToolVersion = [System.Reflection.AssemblyName]::GetAssemblyName($ToolDll).Version.ToString(3)
$ApiVersion = [System.Reflection.AssemblyName]::GetAssemblyName($ApiDll).Version.ToString(3)

Write-Host "  Tool version: $ToolVersion"
Write-Host "  API version:  $ApiVersion"

# Step 4: Package published outputs as versioned ZIP resources.
Write-Host "[4/6] Packaging binaries as embedded resources..." -ForegroundColor Yellow

if (Test-Path $ResourcesDir) {
    Remove-Item $ResourcesDir -Recurse -Force -Confirm:$false
}

New-Item -ItemType Directory -Path $ResourcesDir -Force | Out-Null

$ToolZip = Join-Path $ResourcesDir "Tool_$ToolVersion.zip"
$ApiZip = Join-Path $ResourcesDir "API_$ApiVersion.zip"

$ConfigExclusions = @("*.config", "appsettings.json", "appsettings.*.json")

$ToolFiles = Get-ChildItem -Path $ToolPublishDir -Exclude $ConfigExclusions
Compress-Archive -Path $ToolFiles.FullName -DestinationPath $ToolZip -Force
Write-Host "  Created Tool_$ToolVersion.zip ($('{0:N1} MB' -f ((Get-Item $ToolZip).Length / 1MB)))"

$ApiFiles = Get-ChildItem -Path $ApiPublishDir -Exclude $ConfigExclusions
Compress-Archive -Path $ApiFiles.FullName -DestinationPath $ApiZip -Force
Write-Host "  Created API_$ApiVersion.zip ($('{0:N1} MB' -f ((Get-Item $ApiZip).Length / 1MB)))"

# Step 5: Publish the Installer with embedded resources.
Write-Host "[5/6] Publishing Installer..." -ForegroundColor Yellow
dotnet publish $InstallerProject -c $Configuration -r $Runtime --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
if ($LASTEXITCODE -ne 0) { throw "Failed to publish Installer." }

# Step 6: Report output.
$InstallerExe = Join-Path $SolutionDir "Server Backup Tool.Installer\bin\$Configuration\net10.0\$Runtime\publish\SBTInstaller.exe"

if (Test-Path $InstallerExe) {
    $Size = (Get-Item $InstallerExe).Length / 1MB
    Write-Host ""
    Write-Host "[6/6] Build complete!" -ForegroundColor Green
    Write-Host "  Output:       $InstallerExe"
    Write-Host "  Size:         $("{0:N1} MB" -f $Size)"
    Write-Host "  Tool version: $ToolVersion"
    Write-Host "  API version:  $ApiVersion"
} else {
    Write-Host ""
    Write-Host "[6/6] Build complete. Check publish output directory." -ForegroundColor Yellow
}
