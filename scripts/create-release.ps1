# Telegram Chat Viewer - Release Creation Script
# This script helps create properly tagged releases for GitHub Actions

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$Message = "",
    
    [Parameter(Mandatory=$false)]
    [switch]$Push = $false
)

# Validate version format (semantic versioning)
if ($Version -notmatch '^v?\d+\.\d+\.\d+$') {
    Write-Error "Version must be in format 'x.y.z' or 'vx.y.z'"
    exit 1
}

# Ensure version starts with 'v'
if (-not $Version.StartsWith('v')) {
    $Version = "v$Version"
}

# Get the numeric version (without 'v')
$NumericVersion = $Version.Substring(1)

Write-Host "Creating release for version: $Version" -ForegroundColor Green

# Update version.txt file
Write-Host "Updating version.txt..." -ForegroundColor Yellow
Set-Content -Path "version.txt" -Value $NumericVersion -NoNewline

# Check if there are any uncommitted changes
$status = git status --porcelain
if ($status) {
    Write-Host "Uncommitted changes detected. Adding version.txt..." -ForegroundColor Yellow
    git add version.txt
    
    if ($Message -eq "") {
        $Message = "Prepare release $Version"
    }
    
    git commit -m $Message
}

# Create and push the tag
Write-Host "Creating git tag: $Version" -ForegroundColor Yellow
git tag -a $Version -m "Release $Version"

if ($Push) {
    Write-Host "Pushing to GitHub..." -ForegroundColor Yellow
    git push origin main
    git push origin $Version
    
    Write-Host "✅ Release $Version has been pushed to GitHub!" -ForegroundColor Green
    Write-Host "GitHub Actions will now build and create the release automatically." -ForegroundColor Cyan
    Write-Host "Check: https://github.com/Akum3tsu/telegram-chat-viewer/actions" -ForegroundColor Cyan
} else {
    Write-Host "Tag created locally. To push to GitHub, run:" -ForegroundColor Cyan
    Write-Host "  git push origin main" -ForegroundColor White
    Write-Host "  git push origin $Version" -ForegroundColor White
    Write-Host ""
    Write-Host "Or run this script again with -Push parameter:" -ForegroundColor Cyan
    Write-Host "  .\create-release.ps1 -Version $Version -Push" -ForegroundColor White
}

Write-Host ""
Write-Host "Release Summary:" -ForegroundColor Green
Write-Host "  Version: $Version" -ForegroundColor White
Write-Host "  Numeric: $NumericVersion" -ForegroundColor White
Write-Host "  Tag: Created" -ForegroundColor White
Write-Host "  Pushed: $(if ($Push) { 'Yes' } else { 'No' })" -ForegroundColor White 