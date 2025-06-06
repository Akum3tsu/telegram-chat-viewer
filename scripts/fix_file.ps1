# PowerShell script to fix the corrupted MainWindow.xaml.cs file

Write-Host "Fixing corrupted MainWindow.xaml.cs file..."

# Read the file content
$content = Get-Content "MainWindow.xaml.cs" -Raw

# Remove all the misplaced ChunkSizeMenuItem_Click method blocks
# This pattern matches the entire method block that's incorrectly placed
$pattern1 = '(?s)private void ChunkSizeMenuItem_Click\(object sender, RoutedEventArgs e\)\s*\{\s*var menuItem = sender as MenuItem;.*?UpdateChunkSizeStatus\(\);\s*\}\s*\}\s*'
$content = $content -replace $pattern1, ''

# Remove all the misplaced UpdateChunkSizeStatus method blocks
$pattern2 = '(?s)private void UpdateChunkSizeStatus\(\)\s*\{\s*var statusSuffix = "".*?StatusLabel\.Text = .*?;\s*\}\s*\}\s*'
$content = $content -replace $pattern2, ''

# Remove any remaining broken method fragments
$content = $content -replace 'private void ChunkSizeMenuItem_Click.*?\n', ''
$content = $content -replace 'private void UpdateChunkSizeStatus.*?\n', ''

# Remove any stray closing braces that might have been left behind
$content = $content -replace '\}\s*\}\s*\n\s*\}\s*\n', "`n"

# Find the last method (ClearResourceCache) and add our methods before the final closing braces
$insertPoint = $content.LastIndexOf("private void ClearResourceCache()")
if ($insertPoint -gt 0) {
    # Find the end of ClearResourceCache method
    $methodEnd = $content.IndexOf("}", $content.IndexOf("_resourceCache.Clear();", $insertPoint))
    $insertAfter = $content.IndexOf("}", $methodEnd) + 1
    
    # The methods to insert
    $newMethods = @"

        private void ChunkSizeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var menuItem = sender as MenuItem;
            if (menuItem == null) return;

            // Get the chunk size from the Tag
            if (int.TryParse(menuItem.Tag?.ToString(), out int chunkSize))
            {
                _progressiveLoadSize = chunkSize;
                _logger.Info(`$"Chunk size changed to {chunkSize:N0} messages");

                // Update all chunk size menu items
                ChunkSize500MenuItem.IsChecked = chunkSize == 500;
                ChunkSize1000MenuItem.IsChecked = chunkSize == 1000;
                ChunkSize10000MenuItem.IsChecked = chunkSize == 10000;
                ChunkSize50000MenuItem.IsChecked = chunkSize == 50000;

                // Update status to show current settings
                UpdateChunkSizeStatus();
            }
        }

        private void UpdateChunkSizeStatus()
        {
            var statusSuffix = "";
            if (_progressiveLoadSize >= 50000)
                statusSuffix = " (Ultra Fast Mode)";
            else if (_progressiveLoadSize >= 10000)
                statusSuffix = " (Fast Mode)";
            else if (_progressiveLoadSize >= 1000)
                statusSuffix = " (Normal+ Mode)";

            if (!string.IsNullOrEmpty(_chatName))
            {
                StatusLabel.Text = `$"{_chatName} • {_currentMessages.Count:N0}/{_allMessages.Count:N0} messages • Chunk: {_progressiveLoadSize:N0}{statusSuffix}";
            }
        }
"@
    
    # Insert the methods
    $content = $content.Substring(0, $insertAfter) + $newMethods + $content.Substring($insertAfter)
}

# Write the fixed content back to the file
Set-Content "MainWindow.xaml.cs" $content -Encoding UTF8

Write-Host "File fixed! Attempting to build..." 