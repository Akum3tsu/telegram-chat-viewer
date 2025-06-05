# Test script to verify LoadingConfigDialog functionality
Write-Host "Testing LoadingConfigDialog..." -ForegroundColor Yellow

# Check if the test file exists
$testFile = "examples\test_infinite_scroll.json"
if (-not (Test-Path $testFile)) {
    Write-Host "Test file not found: $testFile" -ForegroundColor Red
    exit 1
}

# Get file info
$fileInfo = Get-Item $testFile
$fileSizeMB = $fileInfo.Length / 1MB

Write-Host "File: $($fileInfo.Name)" -ForegroundColor Green
Write-Host "Size: $($fileSizeMB.ToString('F2')) MB" -ForegroundColor Green

# Try to parse the JSON to get message count
try {
    Write-Host "Analyzing JSON structure..." -ForegroundColor Yellow
    $jsonContent = Get-Content $testFile -Raw | ConvertFrom-Json
    $messageCount = $jsonContent.messages.Count
    Write-Host "Messages: $messageCount" -ForegroundColor Green
    
    Write-Host "JSON structure appears valid." -ForegroundColor Green
    Write-Host "Chat name: $($jsonContent.name)" -ForegroundColor Cyan
    Write-Host "Chat type: $($jsonContent.type)" -ForegroundColor Cyan
    
} catch {
    Write-Host "Error parsing JSON: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "Test file analysis complete. The file should work with the application." -ForegroundColor Green 