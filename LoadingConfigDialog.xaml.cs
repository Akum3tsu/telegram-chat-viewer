using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TelegramChatViewer
{
    public partial class LoadingConfigDialog : Window
    {
        public LoadingConfig Result { get; private set; }
        public bool WasCancelled { get; private set; } = true;

        private readonly double _fileSizeMB;
        private readonly int _messageCount;
        private readonly string _fileName;


        public LoadingConfigDialog(string filePath, double fileSizeMB, int messageCount, bool isLightMode = true)
        {
            try
            {
                InitializeComponent();
                
                _fileSizeMB = fileSizeMB;
                _messageCount = messageCount;
                _fileName = Path.GetFileName(filePath);

                // Apply theme before any UI initialization
                ApplyTheme();
                
                InitializeDialog();
                ApplySmartDefaults();
                UpdateRecommendations();
                UpdateEstimates();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error initializing loading dialog: {ex.Message}\n\nStack trace: {ex.StackTrace}", 
                    "Dialog Initialization Error", 
                    System.Windows.MessageBoxButton.OK, 
                    System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        public void SetSuggestedConfiguration(LoadingConfig suggestedConfig)
        {
            // Apply suggested configuration to the UI
            switch (suggestedConfig.LoadingStrategy)
            {
                case LoadingStrategy.LoadAll:
                    if (LoadAllRadio != null) LoadAllRadio.IsChecked = true;
                    break;
                case LoadingStrategy.Progressive:
                    if (ProgressiveRadio != null) ProgressiveRadio.IsChecked = true;
                    break;
                case LoadingStrategy.Streaming:
                    if (StreamingRadio != null) StreamingRadio.IsChecked = true;
                    break;
            }

            // Set chunk size
            if (ChunkSizeComboBox != null)
            {
                var chunkSizeMapping = new System.Collections.Generic.Dictionary<int, int>
                {
                    { 500, 0 }, { 1000, 1 }, { 2000, 2 }, { 5000, 3 }, { 10000, 4 }, { 50000, 5 }
                };
                
                var closestIndex = chunkSizeMapping.OrderBy(kvp => Math.Abs(kvp.Key - suggestedConfig.ChunkSize)).First().Value;
                ChunkSizeComboBox.SelectedIndex = closestIndex;
            }

            // Set performance options (massive load is now determined by chunk size >= 5000)
            if (VirtualScrollingCheckBox != null) VirtualScrollingCheckBox.IsChecked = suggestedConfig.UseVirtualScrolling;
            if (AlternatingLayoutCheckBox != null) AlternatingLayoutCheckBox.IsChecked = suggestedConfig.UseAlternatingLayout;

            // Update estimates after applying suggestions
            UpdateEstimates();
        }

        private void ApplyTheme()
        {
            var resources = this.Resources;
            
            // Apply light theme
            this.Background = resources["BackgroundBrush"] as Brush;
            resources["CurrentBackground"] = resources["BackgroundBrush"];
            resources["CurrentSecondaryBackground"] = resources["SecondaryBackgroundBrush"];
            resources["CurrentAccent"] = resources["AccentBrush"];
            resources["CurrentText"] = resources["TextBrush"];
            resources["CurrentSecondaryText"] = resources["SecondaryTextBrush"];
            resources["CurrentBorder"] = resources["BorderBrush"];
            resources["CurrentHover"] = resources["HoverBrush"];
        }

        private void InitializeDialog()
        {
            // Update file info
            if (FileInfoText != null)
            {
                FileInfoText.Text = $"File: {_fileName} ({_fileSizeMB:F1} MB, {_messageCount:N0} messages)";
            }

            // Set initial states based on file characteristics
            if (_fileSizeMB > 100 || _messageCount > 50000)
            {
                StreamingRadio.IsChecked = true;
                VirtualScrollingCheckBox.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 3; // 5000 (automatically enables massive load)
            }
            else if (_fileSizeMB > 50 || _messageCount > 10000)
            {
                ProgressiveRadio.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 3; // 5000 (default with massive load)
            }
            else if (_messageCount > 2000)
            {
                ProgressiveRadio.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 3; // 5000 (default)
            }
            else
            {
                LoadAllRadio.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 0; // 500 for small chats
            }

            // Enable alternating layout by default for better readability
            AlternatingLayoutCheckBox.IsChecked = true;
        }

        private void ApplySmartDefaults()
        {
            // Enable virtual scrolling for very large files
            if (_fileSizeMB > 100 || _messageCount > 50000)
            {
                VirtualScrollingCheckBox.IsChecked = true;
            }
        }

        private void UpdateRecommendations()
        {
            string recommendation;
            
            if (_fileSizeMB > 200 || _messageCount > 100000)
            {
                recommendation = "For this extremely large file, we strongly recommend Streaming Mode with Virtual Scrolling and Massive Load for optimal performance and minimal memory usage.";
                RecommendationsPanel.Background = System.Windows.Media.Brushes.DarkRed;
            }
            else if (_fileSizeMB > 100 || _messageCount > 50000)
            {
                recommendation = "For this very large file, we recommend Streaming Mode with Virtual Scrolling enabled to handle the size efficiently while maintaining good performance.";
                RecommendationsPanel.Background = System.Windows.Media.Brushes.DarkOrange;
            }
            else if (_fileSizeMB > 50 || _messageCount > 10000)
            {
                recommendation = "For this large chat, Progressive Loading with Massive Load will provide the best balance of performance and functionality.";
                RecommendationsPanel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 76, 117));
            }
            else if (_messageCount > 5000)
            {
                recommendation = "Progressive Loading is recommended for faster initial load while maintaining good search functionality.";
                RecommendationsPanel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(15, 76, 117));
            }
            else
            {
                recommendation = "This chat size is manageable with any loading strategy. Load All Messages will give you instant search capability.";
                RecommendationsPanel.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 139, 34));
            }

            if (RecommendationText != null)
            {
                RecommendationText.Text = recommendation;
            }
        }

        private void UpdateEstimates()
        {
            try
            {
                // Guard clause: prevent execution before UI elements are initialized
                if (MemoryEstimateText == null || LoadTimeEstimateText == null)
                    return;

                var config = GetCurrentConfig();
                
                // Memory estimate
                double memoryMB;
                if (config.LoadingStrategy == LoadingStrategy.LoadAll)
                {
                    memoryMB = _messageCount * 0.002; // ~2KB per message
                }
                else if (config.LoadingStrategy == LoadingStrategy.Streaming)
                {
                    memoryMB = Math.Min(config.ChunkSize * 0.002, 50); // Cap at 50MB
                }
                else // Progressive
                {
                    memoryMB = config.ChunkSize * 0.002 * 2; // Initial chunk + buffer
                }

                if (MemoryEstimateText != null)
                {
                    MemoryEstimateText.Text = $"~{memoryMB:F0}-{memoryMB * 1.5:F0} MB";
                }

                // Load time estimate
                string loadTime;
                if (config.LoadingStrategy == LoadingStrategy.LoadAll)
                {
                    if (_messageCount > 50000) loadTime = "30-60 seconds";
                    else if (_messageCount > 20000) loadTime = "10-30 seconds";
                    else if (_messageCount > 5000) loadTime = "3-10 seconds";
                    else loadTime = "1-3 seconds";
                }
                else if (config.LoadingStrategy == LoadingStrategy.Streaming)
                {
                    loadTime = "2-5 seconds";
                }
                else // Progressive
                {
                    if (_messageCount > 20000) loadTime = "3-8 seconds";
                    else if (_messageCount > 5000) loadTime = "2-5 seconds";
                    else loadTime = "1-3 seconds";
                }

                if (LoadTimeEstimateText != null)
                {
                    LoadTimeEstimateText.Text = loadTime;
                }
            }
            catch (Exception ex)
            {
                // Log the error but don't crash the dialog
                System.Diagnostics.Debug.WriteLine($"Error updating estimates: {ex.Message}");
            }
        }

        private LoadingConfig GetCurrentConfig()
        {
            var config = new LoadingConfig();

            // Loading strategy - with null checks
            if (LoadAllRadio?.IsChecked == true)
                config.LoadingStrategy = LoadingStrategy.LoadAll;
            else if (StreamingRadio?.IsChecked == true)
                config.LoadingStrategy = LoadingStrategy.Streaming;
            else
                config.LoadingStrategy = LoadingStrategy.Progressive;

            // Chunk size
            var selectedItem = ChunkSizeComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem?.Tag != null)
            {
                config.ChunkSize = int.Parse(selectedItem.Tag.ToString());
            }
            else
            {
                // Fallback to new default chunk size if no item is selected
                config.ChunkSize = 5000;
            }

            // Performance options - massive load determined by chunk size
            config.UseMassiveLoad = config.ChunkSize >= 5000;
            config.UseVirtualScrolling = VirtualScrollingCheckBox?.IsChecked == true;

            // UI options - with null checks
            config.UseAlternatingLayout = AlternatingLayoutCheckBox?.IsChecked == true;

            return config;
        }

        private void LoadingStrategy_Changed(object sender, RoutedEventArgs e)
        {
            // Guard clause: prevent execution before UI elements are initialized
            if (ChunkSizeComboBox == null || VirtualScrollingCheckBox == null || StreamingRadio == null)
                return;

            UpdateEstimates();
            
            // Auto-adjust related settings based on strategy
            if (StreamingRadio.IsChecked == true)
            {
                VirtualScrollingCheckBox.IsChecked = true;
                if (ChunkSizeComboBox.SelectedIndex < 2)
                    ChunkSizeComboBox.SelectedIndex = 2; // At least 2000 for streaming
            }
        }

        private void PerformanceOption_Changed(object sender, RoutedEventArgs e)
        {
            // Guard clause: prevent execution before UI elements are initialized
            if (ChunkSizeComboBox == null)
                return;

            UpdateEstimates();
        }

        private void UIOption_Changed(object sender, RoutedEventArgs e)
        {
            // UI options don't affect performance estimates
        }

        private void ChunkSize_Changed(object sender, SelectionChangedEventArgs e)
        {
            // Guard clause: prevent execution before UI elements are initialized
            if (ChunkSizeComboBox == null)
                return;

            UpdateEstimates();
            
            // Massive load is now automatically determined by chunk size >= 5000
            // No manual checkbox needed
        }

        private void ApplyRecommendations_Click(object sender, RoutedEventArgs e)
        {
            // Guard clause: prevent execution before UI elements are initialized
            if (StreamingRadio == null || ProgressiveRadio == null || LoadAllRadio == null || 
                VirtualScrollingCheckBox == null || ChunkSizeComboBox == null || AlternatingLayoutCheckBox == null)
                return;

            // Apply recommended settings based on file characteristics
            if (_fileSizeMB > 200 || _messageCount > 100000)
            {
                StreamingRadio.IsChecked = true;
                VirtualScrollingCheckBox.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 5; // 50000 (Ultra Fast, automatically massive)
                AlternatingLayoutCheckBox.IsChecked = true;
            }
            else if (_fileSizeMB > 100 || _messageCount > 50000)
            {
                StreamingRadio.IsChecked = true;
                VirtualScrollingCheckBox.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 4; // 10000 (Fast, automatically massive)
                AlternatingLayoutCheckBox.IsChecked = true;
            }
            else if (_fileSizeMB > 50 || _messageCount > 10000)
            {
                ProgressiveRadio.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 3; // 5000 (Massive, automatically massive)
                AlternatingLayoutCheckBox.IsChecked = true;
            }
            else if (_messageCount > 5000)
            {
                ProgressiveRadio.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 2; // 2000 (Aggressive)
                AlternatingLayoutCheckBox.IsChecked = true;
            }
            else if (_messageCount > 2000)
            {
                ProgressiveRadio.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 1; // 1000 (Balanced)
                AlternatingLayoutCheckBox.IsChecked = true;
            }
            else
            {
                LoadAllRadio.IsChecked = true;
                ChunkSizeComboBox.SelectedIndex = 0; // 500 (Conservative)
                AlternatingLayoutCheckBox.IsChecked = true;
            }

            UpdateEstimates();
        }

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            Result = GetCurrentConfig();
            WasCancelled = false;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            WasCancelled = true;
            DialogResult = false;
            Close();
        }
    }

    // Configuration classes
    public class LoadingConfig
    {
        public LoadingStrategy LoadingStrategy { get; set; } = LoadingStrategy.Progressive;
        public bool UseMassiveLoad { get; set; } = true; // Default to true since default chunk size is now 5000
        public bool UseVirtualScrolling { get; set; } = false;
        public int ChunkSize { get; set; } = 5000; // Changed from 1000 to 5000
        public bool UseAlternatingLayout { get; set; } = true;

    }

    public enum LoadingStrategy
    {
        LoadAll,
        Progressive,
        Streaming
    }
} 