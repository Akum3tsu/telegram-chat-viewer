# Version Manager for Telegram Chat Viewer
# Handles semantic versioning (major.minor.patch)

param(
    [Parameter(Position=0)]
    [ValidateSet("get", "major", "minor", "patch", "set")]
    [string]$Action = "get",
    
    [Parameter(Position=1)]
    [string]$Version = ""
)

$VersionFile = "version.txt"
$ProjectFile = "TelegramChatViewer.csproj"

function Get-CurrentVersion {
    if (Test-Path $VersionFile) {
        $version = Get-Content $VersionFile -Raw
        return $version.Trim()
    } else {
        return "0.0.1"
    }
}

function Set-Version {
    param([string]$NewVersion)
    
    # Validate version format
    if ($NewVersion -notmatch '^\d+\.\d+\.\d+$') {
        Write-Error "Invalid version format. Use major.minor.patch (e.g., 1.2.3)"
        return $false
    }
    
    # Update version.txt
    $NewVersion | Out-File -FilePath $VersionFile -Encoding UTF8 -NoNewline
    
    # Update project file
    Update-ProjectVersion $NewVersion
    
    Write-Host "Version updated to: $NewVersion" -ForegroundColor Green
    return $true
}

function Update-ProjectVersion {
    param([string]$Version)
    
    if (Test-Path $ProjectFile) {
        $content = Get-Content $ProjectFile -Raw
        
        # Update or add Version property
        if ($content -match '<Version>.*?</Version>') {
            $content = $content -replace '<Version>.*?</Version>', "<Version>$Version</Version>"
        } elseif ($content -match '<PropertyGroup>') {
            $content = $content -replace '(<PropertyGroup>)', "`$1`n    <Version>$Version</Version>"
        } else {
            # Add PropertyGroup if it doesn't exist
            $content = $content -replace '(<Project[^>]*>)', "`$1`n  <PropertyGroup>`n    <Version>$Version</Version>`n  </PropertyGroup>"
        }
        
        # Update or add AssemblyVersion
        if ($content -match '<AssemblyVersion>.*?</AssemblyVersion>') {
            $content = $content -replace '<AssemblyVersion>.*?</AssemblyVersion>', "<AssemblyVersion>$Version</AssemblyVersion>"
        } elseif ($content -match '<Version>.*?</Version>') {
            $content = $content -replace '(<Version>.*?</Version>)', "`$1`n    <AssemblyVersion>$Version</AssemblyVersion>"
        }
        
        # Update or add FileVersion
        if ($content -match '<FileVersion>.*?</FileVersion>') {
            $content = $content -replace '<FileVersion>.*?</FileVersion>', "<FileVersion>$Version</FileVersion>"
        } elseif ($content -match '<AssemblyVersion>.*?</AssemblyVersion>') {
            $content = $content -replace '(<AssemblyVersion>.*?</AssemblyVersion>)', "`$1`n    <FileVersion>$Version</FileVersion>"
        }
        
        $content | Out-File -FilePath $ProjectFile -Encoding UTF8
        Write-Host "Updated $ProjectFile with version $Version" -ForegroundColor Cyan
    }
}

function Increment-Version {
    param([string]$Type)
    
    $currentVersion = Get-CurrentVersion
    $parts = $currentVersion.Split('.')
    
    if ($parts.Length -ne 3) {
        Write-Error "Invalid current version format: $currentVersion"
        return $false
    }
    
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]
    
    switch ($Type) {
        "major" {
            $major++
            $minor = 0
            $patch = 0
        }
        "minor" {
            $minor++
            $patch = 0
        }
        "patch" {
            $patch++
        }
    }
    
    $newVersion = "$major.$minor.$patch"
    return Set-Version $newVersion
}

function Show-VersionInfo {
    $currentVersion = Get-CurrentVersion
    Write-Host "Current Version: $currentVersion" -ForegroundColor Yellow
    
    $parts = $currentVersion.Split('.')
    if ($parts.Length -eq 3) {
        $major = [int]$parts[0]
        $minor = [int]$parts[1]
        $patch = [int]$parts[2]
        
        Write-Host ""
        Write-Host "Next versions would be:" -ForegroundColor Cyan
        Write-Host "  Major: $($major + 1).0.0" -ForegroundColor White
        Write-Host "  Minor: $major.$($minor + 1).0" -ForegroundColor White
        Write-Host "  Patch: $major.$minor.$($patch + 1)" -ForegroundColor White
    }
}

# Main execution
switch ($Action) {
    "get" {
        Show-VersionInfo
    }
    "major" {
        Write-Host "Incrementing MAJOR version..." -ForegroundColor Yellow
        if (Increment-Version "major") {
            Show-VersionInfo
        }
    }
    "minor" {
        Write-Host "Incrementing MINOR version..." -ForegroundColor Yellow
        if (Increment-Version "minor") {
            Show-VersionInfo
        }
    }
    "patch" {
        Write-Host "Incrementing PATCH version..." -ForegroundColor Yellow
        if (Increment-Version "patch") {
            Show-VersionInfo
        }
    }
    "set" {
        if ([string]::IsNullOrEmpty($Version)) {
            Write-Error "Please provide a version number. Example: .\version-manager.ps1 set 1.2.3"
            exit 1
        }
        Write-Host "Setting version to $Version..." -ForegroundColor Yellow
        if (Set-Version $Version) {
            Show-VersionInfo
        }
    }
    default {
        Write-Host "Usage: .\version-manager.ps1 [action] [version]" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Actions:" -ForegroundColor Yellow
        Write-Host "  get     - Show current version (default)"
        Write-Host "  major   - Increment major version (x.0.0)"
        Write-Host "  minor   - Increment minor version (x.y.0)"
        Write-Host "  patch   - Increment patch version (x.y.z)"
        Write-Host "  set     - Set specific version"
        Write-Host ""
        Write-Host "Examples:" -ForegroundColor Green
        Write-Host "  .\version-manager.ps1"
        Write-Host "  .\version-manager.ps1 patch"
        Write-Host "  .\version-manager.ps1 minor"
        Write-Host "  .\version-manager.ps1 major"
        Write-Host "  .\version-manager.ps1 set 1.2.3"
    }
} 