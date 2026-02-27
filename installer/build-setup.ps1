# ============================================================
#  LiteBright  — Build setup installer
#  Usage:  .\build-setup.ps1
# ============================================================

$ErrorActionPreference = "Stop"
$root    = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "BrightnessController.csproj"
$pubDir  = Join-Path $root "bin\publish-setup"
$outDir  = Join-Path $root "bin\installer-output"
$iss     = Join-Path $PSScriptRoot "LiteBright.iss"

# ── 1. Find Inno Setup compiler ────────────────────────────────────────────
$isccCmd = Get-Command ISCC -ErrorAction SilentlyContinue
$iscc = @(
    "C:\Inno Setup 6\ISCC.exe",
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe",
    $(if ($isccCmd) { $isccCmd.Source } else { $null })
) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1

if (-not $iscc) {
    Write-Host ""
    Write-Host "  ERROR: Inno Setup 6 not found." -ForegroundColor Red
    Write-Host "  Download from:  https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

# ── 2. Publish self-contained app ──────────────────────────────────────────
Write-Host ">> Publishing self-contained app..." -ForegroundColor Cyan

Stop-Process -Name "BrightnessController" -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

& "C:\Program Files\dotnet\dotnet.exe" publish $project `
    -c Release -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $pubDir `
    --nologo

if ($LASTEXITCODE -ne 0) { Write-Host "Publish failed." -ForegroundColor Red; exit 1 }

# ── 3. Compile installer ───────────────────────────────────────────────────
Write-Host ">> Compiling installer with Inno Setup..." -ForegroundColor Cyan

New-Item -ItemType Directory -Force -Path $outDir | Out-Null
& $iscc $iss

if ($LASTEXITCODE -ne 0) { Write-Host "Inno Setup compile failed." -ForegroundColor Red; exit 1 }

# ── 4. Done ────────────────────────────────────────────────────────────────
$setup = Get-ChildItem $outDir -Filter "*.exe" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
Write-Host ""
Write-Host "  Setup installer ready:" -ForegroundColor Green
Write-Host "  $($setup.FullName)" -ForegroundColor White
Write-Host "  Size: $([math]::Round($setup.Length/1MB, 1)) MB" -ForegroundColor Gray
Write-Host ""
