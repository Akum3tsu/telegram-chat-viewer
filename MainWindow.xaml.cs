using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TelegramChatViewer.Models;
using TelegramChatViewer.Services;

namespace TelegramChatViewer
{
    public partial class MainWindow : Window
    {
        private readonly Logger _logger;
        private readonly MessageParser _messageParser;
        private readonly PerformanceOptimizer _performanceOptimizer;
        private readonly ParallelMessageParser _parallelMessageParser;
        
        // Application state
        private List<TelegramMessage> _allMessages = new List<TelegramMessage>();
        private List<TelegramMessage> _currentMessages = new List<TelegramMessage>();
        private string _chatName = "";
        private string _jsonFileDirectory = "";
        
        // Performance settings
        private bool _useVirtualScrolling = true;
        private bool _infiniteScrollEnabled = false;
        private bool _isLoadingMore = false;
        private bool _isInitialSetup = false;
        
        // Enhanced scroll management
        private DispatcherTimer _scrollThrottleTimer;
        private double _scrollThreshold = 0.85;
        private DateTime _lastScrollTrigger = DateTime.MinValue;
        private const int ScrollCooldownMs = 500;
        private const int ScrollThrottleMs = 50;
        
        // Progressive loading state
        private int _progressiveLoadSize = 500;
        
        // Performance optimization caches
        private readonly Dictionary<string, Brush> _resourceCache = new Dictionary<string, Brush>();
        
        // Search state
        private List<TelegramMessage> _searchResults = new List<TelegramMessage>();
        private int _currentSearchIndex = 0;
        private DispatcherTimer _searchTimer;
        private string _currentSearchTerm = "";
        
        // Member colors
        private readonly Dictionary<string, Brush> _memberColors = new Dictionary<string, Brush>();
        
        // Color palettes for light/dark themes
        private readonly Brush[] _lightColorPalette = {
            new SolidColorBrush(Color.FromRgb(0xB8, 0x1C, 0x1C)),
            new SolidColorBrush(Color.FromRgb(0x1B, 0x5E, 0x20)),
            new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x7E)),
            new SolidColorBrush(Color.FromRgb(0x8A, 0x4F, 0x00)),
            new SolidColorBrush(Color.FromRgb(0x6A, 0x1B, 0x9A))
        };

        private readonly Brush[] _darkColorPalette = {
            new SolidColorBrush(Color.FromRgb(0xFF, 0x8A, 0x8A)),
            new SolidColorBrush(Color.FromRgb(0x8A, 0xFF, 0x8A)),
            new SolidColorBrush(Color.FromRgb(0x8A, 0x8A, 0xFF)),
            new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x80)),
            new SolidColorBrush(Color.FromRgb(0xFF, 0x8A, 0xFF))
        };

        private Brush[] _colorPalette => _isLightMode ? _lightColorPalette : _darkColorPalette;

        // Layout options
        private bool _useAlternatingLayout = true;
        private bool _isLightMode = true;
        private bool _useMassiveLoad = false;

        // User-based alternating layout tracking
        private string _lastMessageSender = "";
        private bool _currentSideIsRight = false;

        // Multi-message selection
        private bool _multiSelectMode = false;
        private readonly List<UIElement> _selectedElements = new List<UIElement>();

        // Theme brushes
        private readonly SolidColorBrush _lightBackground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
        private readonly SolidColorBrush _lightSecondaryBackground = new SolidColorBrush(Color.FromRgb(245, 245, 245));
        private readonly SolidColorBrush _lightAccent = new SolidColorBrush(Color.FromRgb(42, 171, 238));
        private readonly SolidColorBrush _lightText = new SolidColorBrush(Color.FromRgb(51, 51, 51));
        private readonly SolidColorBrush _lightSecondaryText = new SolidColorBrush(Color.FromRgb(140, 140, 140));

        // Performance-optimized loading for massive datasets
        private readonly int _massiveLoadBatchSize = 250; // Process messages in smaller batches
        private readonly int _uiUpdateInterval = 100; // Update UI every 100 messages
        private volatile bool _cancelLoading = false;

        public MainWindow()
        {
            try
            {
                // Debug startup step by step
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "debug_startup.log"), $"[{DateTime.Now}] MainWindow constructor starting...\n");
                
                InitializeComponent();
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "debug_startup.log"), $"[{DateTime.Now}] InitializeComponent completed\n");
                
                _logger = new Logger();
                _performanceOptimizer = new PerformanceOptimizer(_logger);
                _messageParser = new MessageParser(_logger);
                _parallelMessageParser = new ParallelMessageParser(_logger, _performanceOptimizer);
                
                // Log hardware capabilities
                _performanceOptimizer.LogPerformanceReport();
                
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "debug_startup.log"), $"[{DateTime.Now}] Logger, Performance Optimizer, and Parsers created\n");
                
                InitializeSearchTimer();
                InitializeScrollThrottleTimer();
                InitializeThemeResources();
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "debug_startup.log"), $"[{DateTime.Now}] Timers and theme resources initialized\n");
                
                var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                {
                    this.Title = $"Telegram Chat Viewer v{version.Major}.{version.Minor}.{version.Build}";
                }
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "debug_startup.log"), $"[{DateTime.Now}] Version and title set\n");
                
                this.Closing += MainWindow_Closing;
                this.Loaded += MainWindow_Loaded;
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "debug_startup.log"), $"[{DateTime.Now}] Event handlers attached\n");
                
                _logger.Info("Telegram Chat Viewer (C#) initialized successfully");
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "debug_startup.log"), $"[{DateTime.Now}] MainWindow constructor completed successfully\n");
            }
            catch (Exception ex)
            {
                try
                {
                    File.WriteAllText(Path.Combine(AppContext.BaseDirectory, "constructor_error.log"), $"[{DateTime.Now}] MainWindow constructor error: {ex}\n");
                }
                catch
                {
                    try
                    {
                        File.WriteAllText(Path.Combine(Path.GetTempPath(), "telegram_constructor_error.log"), $"[{DateTime.Now}] MainWindow constructor error: {ex}\n");
                    }
                    catch { }
                }
                throw;
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _logger.Info("MainWindow_Loaded event called");
                this.Visibility = Visibility.Visible;
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
                this.Focus();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error in MainWindow_Loaded: {ex.Message}", ex);
                MessageBox.Show($"Error in MainWindow_Loaded: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _logger?.LogApplicationExit();
        }

        private void InitializeSearchTimer()
        {
            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)
            };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                PerformSearch();
            };
        }

        private void InitializeScrollThrottleTimer()
        {
            _scrollThrottleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ScrollThrottleMs)
            };
            _scrollThrottleTimer.Tick += (s, e) =>
            {
                _scrollThrottleTimer.Stop();
                CheckInfiniteScroll();
            };
        }

        private void InitializeThemeResources()
        {
            try
            {
                var resources = this.Resources;
                
                resources["PrimaryBackground"] = _lightBackground;
                resources["SecondaryBackground"] = _lightSecondaryBackground;
                resources["MessageInBackground"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                resources["MessageOutBackground"] = new SolidColorBrush(Color.FromRgb(227, 242, 253));
                resources["ServiceBackground"] = new SolidColorBrush(Color.FromRgb(0xE5, 0xF3, 0xDF));
                resources["ReplyBackground"] = new SolidColorBrush(Color.FromRgb(0xF0, 0xF8, 0xEB));
                resources["AccentColor"] = _lightAccent;
                resources["PrimaryText"] = _lightText;
                resources["SecondaryText"] = _lightSecondaryText;
                resources["ServiceText"] = new SolidColorBrush(Color.FromRgb(0x7E, 0x9E, 0x87));
                resources["ReplyText"] = new SolidColorBrush(Color.FromRgb(0x70, 0x8B, 0x75));
                resources["WelcomeText"] = new SolidColorBrush(Color.FromRgb(0x2C, 0x55, 0x30));
                resources["WelcomeSecondary"] = new SolidColorBrush(Color.FromRgb(0x70, 0x8B, 0x75));
                resources["MessageAltBackground1"] = new SolidColorBrush(Color.FromRgb(0xE9, 0xF5, 0xE3));
                resources["MessageAltBackground2"] = new SolidColorBrush(Color.FromRgb(0xD6, 0xE8, 0xCE));
                
                _logger.Info("Theme resources initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error initializing theme resources: {ex.Message}", ex);
            }
        }

        // Event handler implementations
        private async void AlternatingLayoutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _useAlternatingLayout = ((MenuItem)sender).IsChecked;
            _logger.Info($"Alternating layout: {(_useAlternatingLayout ? "enabled" : "disabled")}");
            
            // Refresh UI if messages are loaded
            if (_currentMessages.Count > 0)
            {
                await RenderBasicMessages();
            }
        }

        private void LightModeMenuItem_Click(object sender, RoutedEventArgs e)
        {
            _isLightMode = ((MenuItem)sender).IsChecked;
            _logger.Info($"Light mode: {(_isLightMode ? "enabled" : "disabled")}");
            SwitchTheme(_isLightMode);
        }

        private void OpenLogMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logDirectory = _logger.GetLogDirectory();
                if (Directory.Exists(logDirectory))
                {
                    Process.Start("explorer.exe", logDirectory);
                    _logger.Info($"Opened log directory: {logDirectory}");
                }
                else
                {
                    MessageBox.Show($"Log directory not found: {logDirectory}", "Directory Not Found", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to open log directory", ex);
                MessageBox.Show($"Failed to open log directory: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
            
            ClearSearchButton.IsEnabled = !string.IsNullOrWhiteSpace(SearchTextBox.Text);
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _searchTimer.Stop();
                PerformSearch();
            }
            else if (e.Key == Key.F3)
            {
                if (Keyboard.Modifiers == ModifierKeys.Shift)
                {
                    PrevSearchButton_Click(sender, e);
                }
                else
                {
                    NextSearchButton_Click(sender, e);
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                ClearSearchButton_Click(sender, e);
                e.Handled = true;
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            // Ctrl+Shift+A for multi-select mode
            if (e.Key == Key.A && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                ToggleMultiSelectMode();
                e.Handled = true;
            }
            // Ctrl+C to copy selected text in multi-select mode
            else if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control && _multiSelectMode)
            {
                CopySelectedText();
                e.Handled = true;
            }
            // Escape to exit multi-select mode
            else if (e.Key == Key.Escape && _multiSelectMode)
            {
                ExitMultiSelectMode();
                e.Handled = true;
            }
            
            base.OnKeyDown(e);
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            PerformSearch();
        }

        private void ClearSearchButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            ClearSearch();
        }

        private void PrevSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_searchResults.Any() && _currentSearchIndex > 0)
            {
                _currentSearchIndex--;
                JumpToSearchResult(_currentSearchIndex);
            }
        }

        private void NextSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (_searchResults.Any() && _currentSearchIndex < _searchResults.Count - 1)
            {
                _currentSearchIndex++;
                JumpToSearchResult(_currentSearchIndex);
            }
        }

        private async void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select Telegram Chat Export",
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                CheckFileExists = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // Get file info for the configuration dialog
                var fileInfo = new FileInfo(openFileDialog.FileName);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                
                // Estimate message count (rough estimate based on file size)
                var estimatedMessageCount = (int)(fileSizeMB * 1000); // Rough estimate
                
                // Get hardware-optimized configuration suggestion
                var suggestedConfig = _performanceOptimizer.GetOptimizedLoadingConfig(fileSizeMB, estimatedMessageCount);
                
                // Show loading configuration dialog with suggestions
                var configDialog = new LoadingConfigDialog(openFileDialog.FileName, fileSizeMB, estimatedMessageCount, _isLightMode);
                configDialog.SetSuggestedConfiguration(suggestedConfig);
                
                if (configDialog.ShowDialog() == true && !configDialog.WasCancelled)
                {
                    // Apply the selected configuration
                    ApplyLoadingConfiguration(configDialog.Result);
                    await LoadChatFile(openFileDialog.FileName);
                }
            }
        }

        private void ApplyLoadingConfiguration(LoadingConfig config)
        {
            // Apply chunk size
            _progressiveLoadSize = config.ChunkSize;
            
            // Apply performance options
            _useMassiveLoad = config.UseMassiveLoad;
            _useVirtualScrolling = config.UseVirtualScrolling;
            
            // Apply loading strategy
            if (config.LoadingStrategy == LoadingStrategy.LoadAll)
            {
                _infiniteScrollEnabled = false;
            }
            else
            {
                _infiniteScrollEnabled = true;
            }

            // Apply UI options
            _useAlternatingLayout = config.UseAlternatingLayout;

            _logger.Info($"Loading configuration applied: Strategy={config.LoadingStrategy}, ChunkSize={_progressiveLoadSize}, MassiveLoad={_useMassiveLoad}, VirtualScrolling={_useVirtualScrolling}");
        }

        // Core functionality methods
        private async void CheckInfiniteScroll()
        {
            if (!_infiniteScrollEnabled || _isLoadingMore || _isInitialSetup)
                return;
                
            var scrollViewer = ChatScrollViewer;
            if (scrollViewer?.ScrollableHeight > 0)
            {
                var scrollPosition = scrollViewer.VerticalOffset / scrollViewer.ScrollableHeight;
                
                // Trigger load more when scrolled to 85% of the content
                if (scrollPosition > _scrollThreshold)
                {
                    _logger.Info($"Infinite scroll triggered at position {scrollPosition:F3}");
                    await LoadMoreMessagesAsync();
                }
            }
        }

        private async Task LoadMoreMessagesAsync()
        {
            if (_isLoadingMore || _allMessages.Count <= _currentMessages.Count)
                return;

            _isLoadingMore = true;
            _cancelLoading = false;
            InfiniteScrollLoader.Visibility = Visibility.Visible;

            try
            {
                var currentCount = _currentMessages.Count;
                var remainingMessages = _allMessages.Count - currentCount;
                var loadCount = Math.Min(_progressiveLoadSize, remainingMessages);
                
                _logger.Info($"Starting to load {loadCount:N0} more messages (batch processing enabled for large loads)");

                // For very large loads, use optimized batch processing
                if (loadCount > 5000)
                {
                    await LoadMessagesBatchOptimized(currentCount, loadCount);
                }
                else
                {
                    // Standard loading for smaller chunks
                    var newMessages = _allMessages.Skip(currentCount).Take(loadCount).ToList();
                    _currentMessages.AddRange(newMessages);
                    await RenderNewMessages(newMessages, currentCount);
                }

                // Update status
                StatusLabel.Text = $"{_chatName} • {_currentMessages.Count:N0}/{_allMessages.Count:N0} messages";
                ProgressLabel.Text = "";

                _logger.Info($"Loaded {loadCount:N0} more messages. Total displayed: {_currentMessages.Count:N0}/{_allMessages.Count:N0}");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to load more messages", ex);
                ProgressLabel.Text = "Loading failed";
            }
            finally
            {
                _isLoadingMore = false;
                _cancelLoading = false;
                InfiniteScrollLoader.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadMessagesBatchOptimized(int startIndex, int totalCount)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var processedCount = 0;
            var renderBatch = new List<TelegramMessage>();
            
            // Pre-calculate date separators to minimize processing during rendering
            var dateSeparatorCache = new HashSet<DateTime>();
            var lastDate = startIndex > 0 ? _currentMessages[startIndex - 1].ParsedDate.Date : DateTime.MinValue;

            for (int i = startIndex; i < startIndex + totalCount && !_cancelLoading; i++)
            {
                var message = _allMessages[i];
                var messageDate = message.ParsedDate.Date;
                
                if (messageDate != lastDate)
                {
                    dateSeparatorCache.Add(messageDate);
                    lastDate = messageDate;
                }

                renderBatch.Add(message);
                processedCount++;

                // Process in smaller batches for better responsiveness
                if (renderBatch.Count >= _massiveLoadBatchSize || processedCount == totalCount)
                {
                    // Add messages to current list
                    _currentMessages.AddRange(renderBatch);

                    // Render batch with optimized UI updates
                    await RenderMessagesBatchOptimized(renderBatch, i - renderBatch.Count + 1, dateSeparatorCache);

                    // Update progress more frequently for better UX
                    var progress = (double)processedCount / totalCount * 100;
                    ProgressLabel.Text = $"Loading messages... {progress:F0}% ({processedCount:N0}/{totalCount:N0})";

                    // Yield more frequently to prevent UI freezing
                    await Task.Delay(10);

                    renderBatch.Clear();
                    dateSeparatorCache.Clear();

                    // Log progress for very large loads
                    if (processedCount % 10000 == 0 || processedCount == totalCount)
                    {
                        _logger.Info($"Batch loading progress: {processedCount:N0}/{totalCount:N0} ({progress:F1}%) - {stopwatch.ElapsedMilliseconds}ms elapsed");
                    }
                }
            }

            stopwatch.Stop();
            _logger.Info($"Optimized batch loading completed: {processedCount:N0} messages in {stopwatch.ElapsedMilliseconds:N0}ms");
        }

        private async Task RenderMessagesBatchOptimized(List<TelegramMessage> messages, int startIndex, HashSet<DateTime> dateSeparators)
        {
            var currentDate = startIndex > 0 ? _currentMessages[startIndex - 1].ParsedDate.Date : DateTime.MinValue;
            int messageIndex = startIndex;
            var elementsToAdd = new List<UIElement>();

            // Pre-create UI elements in memory before adding to visual tree
            foreach (var message in messages)
            {
                var messageDate = message.ParsedDate.Date;
                
                // Add date separator if needed
                if (dateSeparators.Contains(messageDate) && messageDate != currentDate)
                {
                    currentDate = messageDate;
                    elementsToAdd.Add(CreateDateSeparatorElement(messageDate));
                }

                // Create message element
                if (message.IsServiceMessage)
                {
                    elementsToAdd.Add(CreateServiceMessageElement(message, messageIndex));
                }
                else
                {
                    elementsToAdd.Add(CreateBasicMessageElement(message, messageIndex));
                }

                messageIndex++;
            }

            // Add all elements to the UI tree in batches to prevent freezing
            var batchCount = 0;
            foreach (var element in elementsToAdd)
            {
                MessagesContainer.Children.Add(element);
                batchCount++;

                // Yield to UI thread more frequently during massive loads
                if (batchCount % _uiUpdateInterval == 0)
                {
                    await Task.Delay(1);
                }
            }
        }

        // Optimized element creation methods (pre-create without adding to tree)
        private UIElement CreateDateSeparatorElement(DateTime date)
        {
            var dateText = date.ToString("MMMM dd, yyyy");
            
            var dateContainer = new Border
            {
                Background = GetCachedResource("PrimaryBackground"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 15, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var dateLabel = new TextBlock
            {
                Text = dateText,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Foreground = GetCachedResource("SecondaryText"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            dateContainer.Child = dateLabel;
            return dateContainer;
        }

        private UIElement CreateServiceMessageElement(TelegramMessage message, int messageIndex)
        {
            var serviceText = MessageParser.FormatServiceMessage(
                message.Actor, message.Action, message.Title, message.Members);

            var serviceContainer = new Border
            {
                Background = GetCachedResource("ServiceBackground"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(10, 8, 10, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var serviceLabel = new TextBlock
            {
                Text = serviceText,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Foreground = GetCachedResource("ServiceText"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            serviceContainer.Child = serviceLabel;
            return serviceContainer;
        }

        private UIElement CreateBasicMessageElement(TelegramMessage message, int messageIndex)
        {
            var memberContainer = new StackPanel
            {
                Margin = new Thickness(5, 8, 5, 2)
            };

            // Apply alternating layout if enabled
            bool alignRight = GetMessageAlignment(message, messageIndex);

            // Member header
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 0, 0, 3)
            };

            // Align header based on message alignment
            if (alignRight)
            {
                headerPanel.HorizontalAlignment = HorizontalAlignment.Right;
                headerPanel.Margin = new Thickness(0, 0, 10, 3);
            }

            var memberColor = GetMemberColor(message.DisplaySender);
            var memberLabel = new TextBox
            {
                Text = message.DisplaySender,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = memberColor,
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                IsTabStop = false,
                Cursor = Cursors.IBeam,
                FocusVisualStyle = null
            };

            headerPanel.Children.Add(memberLabel);
            memberContainer.Children.Add(headerPanel);

            // Message bubble
            var bubble = CreateBasicMessageBubble(message);
            var messageContainer = new Grid
            {
                Margin = new Thickness(0, 1, 0, 1)
            };

            if (alignRight)
            {
                messageContainer.HorizontalAlignment = HorizontalAlignment.Right;
                bubble.HorizontalAlignment = HorizontalAlignment.Right;
                bubble.Margin = new Thickness(50, 0, 5, 0);
            }
            else
            {
                messageContainer.HorizontalAlignment = HorizontalAlignment.Left;
                bubble.HorizontalAlignment = HorizontalAlignment.Left;
                bubble.Margin = new Thickness(5, 0, 50, 0);
            }

            messageContainer.Children.Add(bubble);
            memberContainer.Children.Add(messageContainer);
            return memberContainer;
        }

        private async Task RenderNewMessages(List<TelegramMessage> messages, int startIndex)
        {
            // Use optimized rendering for large batches
            if (messages.Count > 1000)
            {
                await RenderMessagesBatchOptimized(messages, startIndex, new HashSet<DateTime>());
                return;
            }

            // Standard rendering for smaller batches
            var currentDate = startIndex > 0 ? _currentMessages[startIndex - 1].ParsedDate.Date : DateTime.MinValue;
            int messageIndex = startIndex;

            foreach (var message in messages)
            {
                var messageDate = message.ParsedDate.Date;
                if (messageDate != currentDate)
                {
                    currentDate = messageDate;
                    AddDateSeparator(messageDate);
                }

                if (message.IsServiceMessage)
                {
                    AddServiceMessage(message, messageIndex);
                }
                else
                {
                    AddBasicMessage(message, messageIndex);
                }

                messageIndex++;

                // More frequent yielding for better responsiveness
                if (messageIndex % 25 == 0)
                {
                    await Task.Delay(1);
                }
            }
        }

        private void PerformSearch()
        {
            var searchTerm = SearchTextBox.Text.Trim();
            
            if (string.IsNullOrEmpty(searchTerm))
            {
                ClearSearch();
                return;
            }

            if (_allMessages.Count == 0)
            {
                SearchResultsLabel.Text = "No messages loaded";
                return;
            }

            _currentSearchTerm = searchTerm;
            _searchResults = _messageParser.SearchMessages(_allMessages, searchTerm);

            if (_searchResults.Any())
            {
                _currentSearchIndex = 0;
                SearchResultsLabel.Text = $"Result 1 of {_searchResults.Count}";
                
                PrevSearchButton.Visibility = Visibility.Visible;
                NextSearchButton.Visibility = Visibility.Visible;
                
                UpdateSearchNavigationButtons();
                JumpToSearchResult(0);
            }
            else
            {
                SearchResultsLabel.Text = "No results found";
                PrevSearchButton.Visibility = Visibility.Collapsed;
                NextSearchButton.Visibility = Visibility.Collapsed;
            }
        }

        private void ClearSearch()
        {
            _searchResults.Clear();
            _currentSearchIndex = 0;
            _currentSearchTerm = "";
            SearchResultsLabel.Text = "";
            
            PrevSearchButton.Visibility = Visibility.Collapsed;
            NextSearchButton.Visibility = Visibility.Collapsed;
        }

        private void JumpToSearchResult(int resultIndex)
        {
            if (!_searchResults.Any() || resultIndex >= _searchResults.Count)
                return;

            var result = _searchResults[resultIndex];
            var messageIndex = _allMessages.IndexOf(result);

            if (messageIndex >= 0)
            {
                SearchResultsLabel.Text = $"Result {resultIndex + 1} of {_searchResults.Count}";
                UpdateSearchNavigationButtons();
                _logger.Info($"Jumping to search result {resultIndex + 1}: message {messageIndex}");
            }
        }

        private void UpdateSearchNavigationButtons()
        {
            PrevSearchButton.IsEnabled = _currentSearchIndex > 0;
            NextSearchButton.IsEnabled = _currentSearchIndex < _searchResults.Count - 1;
        }

        private async Task LoadChatFile(string filePath)
        {
            try
            {
                _isInitialSetup = false;
                _jsonFileDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath));
                _logger.Info($"JSON file directory set to: {_jsonFileDirectory}");

                var fileInfo = new FileInfo(filePath);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                _logger.Info($"File size: {fileSizeMB:F2} MB");

                ShowLoadingUI(true, "Loading messages...");
                UpdateProgress(30, "Loading messages...");

                // Use parallel parser for better performance on high-end hardware
                var (messages, chatName) = await _parallelMessageParser.LoadChatFileAsync(filePath);
                _allMessages = messages;
                _chatName = chatName;
                
                UpdateProgress(70, $"Loaded {messages.Count:N0} messages...");

                _memberColors.Clear();

                UpdateProgress(90, "Setting up UI...");
                await SetupChatUI();

                UpdateProgress(100, "Complete!");
                _logger.Info("Chat loading completed successfully");

                await Task.Delay(1000);
                ShowLoadingUI(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading chat file: {filePath}", ex);
                ShowLoadingUI(false);
                MessageBox.Show($"Failed to load chat file:\n\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task SetupChatUI()
        {
            _isInitialSetup = true;
            
            // Enable infinite scroll for large chats
            _infiniteScrollEnabled = _allMessages.Count > 1000;
            
            // Show only first chunk of messages if infinite scroll is enabled  
            if (_infiniteScrollEnabled)
            {
                var initialLoadSize = Math.Min(_progressiveLoadSize, _allMessages.Count);
                _currentMessages = _allMessages.Take(initialLoadSize).ToList();
                _logger.Info($"Large dataset detected ({_allMessages.Count:N0} messages). Loading initial {initialLoadSize:N0} messages with infinite scroll.");
            }
            else
            {
                _currentMessages = _allMessages;
                _logger.Info($"Small dataset ({_allMessages.Count:N0} messages). Loading all messages at once.");
            }

            StatusLabel.Text = $"{_chatName} • {_currentMessages.Count:N0}/{_allMessages.Count:N0} messages";

            WelcomePanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;

            // Add scroll event handler for infinite scroll
            if (_infiniteScrollEnabled)
            {
                ChatScrollViewer.ScrollChanged += ChatScrollViewer_ScrollChanged;
            }

            // Use optimized rendering for initial load
            await RenderBasicMessagesOptimized();
            
            _lastScrollTrigger = DateTime.MinValue;
            
            await Task.Delay(500);
            _isInitialSetup = false;
            _logger.Info($"Initial setup complete. Infinite scroll: {_infiniteScrollEnabled}, Displaying: {_currentMessages.Count:N0}/{_allMessages.Count:N0}");
        }

        private void ChatScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!_infiniteScrollEnabled || _isLoadingMore || _isInitialSetup)
                return;

            _scrollThrottleTimer.Stop();
            _scrollThrottleTimer.Start();
        }

        private async Task RenderBasicMessages()
        {
            MessagesContainer.Children.Clear();
            ResetAlternatingLayoutState();

            var currentDate = DateTime.MinValue;
            int messageIndex = 0;

            foreach (var message in _currentMessages)
            {
                var messageDate = message.ParsedDate.Date;
                if (messageDate != currentDate)
                {
                    currentDate = messageDate;
                    AddDateSeparator(messageDate);
                }

                if (message.IsServiceMessage)
                {
                    AddServiceMessage(message, messageIndex);
                }
                else
                {
                    AddBasicMessage(message, messageIndex);
                }

                messageIndex++;

                if (messageIndex % 100 == 0)
                {
                    await Task.Delay(1);
                }
            }

            await Task.Delay(100);
            ChatScrollViewer.ScrollToTop();
        }

        private async Task RenderBasicMessagesOptimized()
        {
            MessagesContainer.Children.Clear();
            ResetAlternatingLayoutState();
            
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _logger.Info($"Starting optimized rendering of {_currentMessages.Count:N0} messages");

            // For very large initial loads, use batch processing
            if (_currentMessages.Count > 5000)
            {
                await RenderMessagesBatchOptimized(_currentMessages, 0, new HashSet<DateTime>());
            }
            else
            {
                // Standard rendering for moderate loads with better yielding
                var currentDate = DateTime.MinValue;
                int messageIndex = 0;

                foreach (var message in _currentMessages)
                {
                    var messageDate = message.ParsedDate.Date;
                    if (messageDate != currentDate)
                    {
                        currentDate = messageDate;
                        AddDateSeparator(messageDate);
                    }

                    if (message.IsServiceMessage)
                    {
                        AddServiceMessage(message, messageIndex);
                    }
                    else
                    {
                        AddBasicMessage(message, messageIndex);
                    }

                    messageIndex++;

                    // More frequent yielding for initial loads
                    if (messageIndex % 50 == 0)
                    {
                        await Task.Delay(1);
                        
                        // Update progress for large initial loads
                        if (_currentMessages.Count > 1000)
                        {
                            var progress = (double)messageIndex / _currentMessages.Count * 100;
                            ProgressLabel.Text = $"Rendering messages... {progress:F0}%";
                        }
                    }
                }
            }

            stopwatch.Stop();
            _logger.Info($"Message rendering completed in {stopwatch.ElapsedMilliseconds:N0}ms");
            
            ProgressLabel.Text = "";
            await Task.Delay(100);
            ChatScrollViewer.ScrollToTop();
        }

        private void AddDateSeparator(DateTime date)
        {
            var dateText = date.ToString("MMMM dd, yyyy");
            
            var dateContainer = new Border
            {
                Background = GetCachedResource("PrimaryBackground"),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(0, 15, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var dateLabel = new TextBlock
            {
                Text = dateText,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12,
                Foreground = GetCachedResource("SecondaryText"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            dateContainer.Child = dateLabel;
            MessagesContainer.Children.Add(dateContainer);
        }

        private void AddServiceMessage(TelegramMessage message, int messageIndex)
        {
            var serviceText = MessageParser.FormatServiceMessage(
                message.Actor, message.Action, message.Title, message.Members);

            var serviceContainer = new Border
            {
                Background = GetCachedResource("ServiceBackground"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 6, 12, 6),
                Margin = new Thickness(10, 8, 10, 8),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var serviceLabel = new TextBlock
            {
                Text = serviceText,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 11,
                FontStyle = FontStyles.Italic,
                Foreground = GetCachedResource("ServiceText"),
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            serviceContainer.Child = serviceLabel;
            MessagesContainer.Children.Add(serviceContainer);
        }

        private void AddBasicMessage(TelegramMessage message, int messageIndex)
        {
            var memberContainer = new StackPanel
            {
                Margin = new Thickness(5, 8, 5, 2)
            };

            // Apply alternating layout if enabled
            bool alignRight = GetMessageAlignment(message, messageIndex);

            // Member header
            var headerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 0, 0, 3)
            };

            // Align header based on message alignment
            if (alignRight)
            {
                headerPanel.HorizontalAlignment = HorizontalAlignment.Right;
                headerPanel.Margin = new Thickness(0, 0, 10, 3);
            }

            var memberColor = GetMemberColor(message.DisplaySender);
            var memberLabel = new TextBox
            {
                Text = message.DisplaySender,
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = memberColor,
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                IsTabStop = false,
                Cursor = Cursors.IBeam,
                FocusVisualStyle = null
            };

            headerPanel.Children.Add(memberLabel);
            memberContainer.Children.Add(headerPanel);

            // Message bubble
            var bubble = CreateBasicMessageBubble(message);
            var messageContainer = new Grid
            {
                Margin = new Thickness(0, 1, 0, 1)
            };

            if (alignRight)
            {
                messageContainer.HorizontalAlignment = HorizontalAlignment.Right;
                bubble.HorizontalAlignment = HorizontalAlignment.Right;
                bubble.Margin = new Thickness(50, 0, 5, 0);
            }
            else
            {
                messageContainer.HorizontalAlignment = HorizontalAlignment.Left;
                bubble.HorizontalAlignment = HorizontalAlignment.Left;
                bubble.Margin = new Thickness(5, 0, 50, 0);
            }

            messageContainer.Children.Add(bubble);
            memberContainer.Children.Add(messageContainer);
            MessagesContainer.Children.Add(memberContainer);
        }

        private Border CreateBasicMessageBubble(TelegramMessage message)
        {
            var bubbleBackground = message.IsOutgoing 
                ? GetCachedResource("MessageOutBackground")
                : GetCachedResource("MessageInBackground");

            var bubble = new Border
            {
                Background = bubbleBackground,
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(12, 8, 12, 8),
                MaxWidth = 600
            };

            var contentPanel = new StackPanel();

            // Forwarded message indicator
            if (message.IsForwarded)
            {
                var forwardedLabel = new TextBox
                {
                    Text = $"Forwarded from {message.ForwardedFrom}",
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Foreground = GetCachedResource("SecondaryText"),
                    Margin = new Thickness(0, 0, 0, 4),
                    IsReadOnly = true,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    IsTabStop = false,
                    Cursor = Cursors.IBeam,
                    FocusVisualStyle = null
                };
                contentPanel.Children.Add(forwardedLabel);
            }

            // Reply indicator
            if (message.IsReply)
            {
                var replyBorder = new Border
                {
                    BorderBrush = _lightAccent,
                    BorderThickness = new Thickness(3, 0, 0, 0),
                    Padding = new Thickness(8, 4, 4, 4),
                    Margin = new Thickness(0, 0, 0, 6),
                    Background = _isLightMode
                        ? new SolidColorBrush(Color.FromArgb(30, 42, 171, 238))
                        : new SolidColorBrush(Color.FromArgb(30, 42, 171, 238))
                };

                // Try to find the original message and show its content preview
                var replyText = GetReplyPreviewText(message.ReplyToMessageId);
                var replyTextBlock = new TextBox
                {
                    Text = replyText,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 11,
                    FontStyle = FontStyles.Italic,
                    Foreground = _lightAccent,
                    TextWrapping = TextWrapping.Wrap,
                    MaxHeight = 40, // Limit height for long messages
                    IsReadOnly = true,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    IsTabStop = false,
                    Cursor = Cursors.IBeam,
                    FocusVisualStyle = null
                };

                replyBorder.Child = replyTextBlock;
                contentPanel.Children.Add(replyBorder);
            }

            // Media content (voice, sticker, video, etc.)
            if (message.HasMedia)
            {
                var mediaElement = CreateMediaElement(message);
                if (mediaElement != null)
                {
                    contentPanel.Children.Add(mediaElement);
                }
            }

            // Rich text content with formatting
            if (message.FormattedText.Any())
            {
                var textBlock = CreateFormattedTextBlock(message.FormattedText);
                if (textBlock != null)
                {
                    contentPanel.Children.Add(textBlock);
                }
            }
            else if (!string.IsNullOrEmpty(message.PlainText))
            {
                var textBlock = new TextBox
                {
                    Text = message.PlainText,
                    FontFamily = new FontFamily("Segoe UI"),
                    FontSize = 13,
                    Foreground = GetCachedResource("PrimaryText"),
                    TextWrapping = TextWrapping.Wrap,
                    IsReadOnly = true,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    IsTabStop = false,
                    Cursor = Cursors.IBeam,
                    FocusVisualStyle = null
                };
                contentPanel.Children.Add(textBlock);
            }

            // Timestamp
            var timeLabel = new TextBox
            {
                Text = message.ParsedDate.ToString("HH:mm"),
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 10,
                Foreground = GetCachedResource("SecondaryText"),
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 4, 0, 0),
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                IsTabStop = false,
                Cursor = Cursors.IBeam,
                FocusVisualStyle = null
            };

            contentPanel.Children.Add(timeLabel);
            bubble.Child = contentPanel;

            return bubble;
        }

        private UIElement CreateFormattedTextBlock(List<TelegramChatViewer.Models.FormattedTextPart> parts)
        {
            if (!parts.Any()) return null;

            var richTextBox = new RichTextBox
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 13,
                Foreground = GetCachedResource("PrimaryText"),
                IsReadOnly = true,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                IsTabStop = false,
                Cursor = Cursors.IBeam,
                FocusVisualStyle = null
            };

            var document = new FlowDocument();
            var paragraph = new Paragraph();

            foreach (var part in parts)
            {
                var run = new Run(part.Text);

                switch (part.Type.ToLower())
                {
                    case "bold":
                        run.FontWeight = FontWeights.Bold;
                        break;
                    case "italic":
                        run.FontStyle = FontStyles.Italic;
                        break;
                    case "strikethrough":
                        run.TextDecorations = TextDecorations.Strikethrough;
                        break;
                    case "underline":
                        run.TextDecorations = TextDecorations.Underline;
                        break;
                    case "code":
                        run.FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace");
                        run.Background = _isLightMode 
                            ? new SolidColorBrush(Color.FromRgb(240, 240, 240))
                            : new SolidColorBrush(Color.FromRgb(45, 45, 45));
                        run.Foreground = _isLightMode
                            ? new SolidColorBrush(Color.FromRgb(199, 37, 78))
                            : new SolidColorBrush(Color.FromRgb(255, 125, 125));
                        break;
                    case "pre":
                        run.FontFamily = new FontFamily("Consolas, Monaco, 'Courier New', monospace");
                        run.Background = _isLightMode
                            ? new SolidColorBrush(Color.FromRgb(245, 245, 245))
                            : new SolidColorBrush(Color.FromRgb(40, 40, 40));
                        run.Foreground = _isLightMode
                            ? new SolidColorBrush(Color.FromRgb(51, 51, 51))
                            : new SolidColorBrush(Color.FromRgb(220, 220, 220));
                        break;
                    case "text_link":
                    case "link":
                        run.Foreground = _lightAccent;
                        run.TextDecorations = TextDecorations.Underline;
                        run.Cursor = Cursors.Hand;
                        // Store the URL for potential click handling
                        run.ToolTip = part.Href;
                        break;
                    case "mention":
                    case "hashtag":
                        run.Foreground = _lightAccent;
                        run.Cursor = Cursors.Hand;
                        break;
                    case "bot_command":
                        run.Foreground = _lightAccent;
                        run.FontWeight = FontWeights.SemiBold;
                        run.Cursor = Cursors.Hand;
                        break;
                    case "email":
                        run.Foreground = _lightAccent;
                        run.TextDecorations = TextDecorations.Underline;
                        run.Cursor = Cursors.Hand;
                        break;
                    case "phone":
                        run.Foreground = _lightAccent;
                        run.Cursor = Cursors.Hand;
                        break;
                    case "spoiler":
                        run.Background = _isLightMode
                            ? new SolidColorBrush(Color.FromRgb(128, 128, 128))
                            : new SolidColorBrush(Color.FromRgb(100, 100, 100));
                        run.Foreground = run.Background; // Hide text initially
                        run.ToolTip = "Spoiler: " + part.Text;
                        run.Cursor = Cursors.Hand;
                        break;
                    case "plain":
                    default:
                        // Default styling
                        break;
                }

                paragraph.Inlines.Add(run);
            }

            document.Blocks.Add(paragraph);
            richTextBox.Document = document;
            return richTextBox;
        }

        private void SwitchTheme(bool isLightMode)
        {
            _isLightMode = isLightMode;
            ClearResourceCache();
            
            var resources = this.Resources;
            
            if (isLightMode)
            {
                resources["PrimaryBackground"] = _lightBackground;
                resources["SecondaryBackground"] = _lightSecondaryBackground;
                resources["AccentColor"] = _lightAccent;
                resources["PrimaryText"] = _lightText;
                resources["SecondaryText"] = _lightSecondaryText;
            }
            else
            {
                // Basic dark theme
                resources["PrimaryBackground"] = new SolidColorBrush(Color.FromRgb(32, 32, 32));
                resources["SecondaryBackground"] = new SolidColorBrush(Color.FromRgb(43, 43, 43));
                resources["AccentColor"] = _lightAccent;
                resources["PrimaryText"] = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                resources["SecondaryText"] = new SolidColorBrush(Color.FromRgb(176, 176, 176));
            }
            
            _memberColors.Clear();
            this.InvalidateVisual();
            this.UpdateLayout();
            
            _logger.Info($"Theme switched to {(isLightMode ? "light" : "dark")} mode");
        }

        private Brush GetMemberColor(string memberName)
        {
            if (!_memberColors.TryGetValue(memberName, out var color))
            {
                var colorIndex = Math.Abs(memberName.GetHashCode()) % _colorPalette.Length;
                color = _colorPalette[colorIndex];
                _memberColors[memberName] = color;
            }
            return color;
        }

        private Brush GetCachedResource(string resourceKey)
        {
            if (!_resourceCache.TryGetValue(resourceKey, out var brush))
            {
                brush = FindResource(resourceKey) as Brush;
                if (brush != null)
                {
                    _resourceCache[resourceKey] = brush;
                }
            }
            return brush;
        }

        private void ShowLoadingUI(bool show, string status = "Loading...")
        {
            LoadingOverlay.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show)
            {
                LoadingStatusText.Text = status;
                LoadingProgressBar.Value = 0;
            }
        }

        private void UpdateProgress(double value, string status = "")
        {
            LoadingProgressBar.Value = value;
            if (!string.IsNullOrEmpty(status))
            {
                LoadingStatusText.Text = status;
                
                // Show progress percentage in the smaller label below
                if (value < 100 && !string.IsNullOrEmpty(status))
                {
                    ProgressLabel.Text = $"Loading... {value:F0}%";
                    ProgressLabel.Visibility = Visibility.Visible;
                }
                else
                {
                    ProgressLabel.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void ClearResourceCache()
        {
            _resourceCache.Clear();
        }

        private string GetReplyPreviewText(int? replyToMessageId)
        {
            if (!replyToMessageId.HasValue)
                return "Reply to message";

            // Find the original message in the loaded messages
            var originalMessage = _allMessages.FirstOrDefault(m => m.Id == replyToMessageId.Value);
            if (originalMessage == null)
                return $"Reply to message #{replyToMessageId}";

            // Create preview text based on message content
            string preview = "";
            
            if (!string.IsNullOrEmpty(originalMessage.PlainText))
            {
                preview = originalMessage.PlainText;
            }
            else if (originalMessage.HasMedia)
            {
                switch (originalMessage.MediaType?.ToLower())
                {
                    case "voice_message":
                    case "voice_note":
                        preview = "🎵 Voice message";
                        break;
                    case "sticker":
                        preview = $"😀 Sticker {originalMessage.StickerEmoji}";
                        break;
                    case "animation":
                        preview = "🎞️ GIF";
                        break;
                    case "video":
                        preview = "📹 Video";
                        break;
                    case "photo":
                        preview = "📷 Photo";
                        break;
                    default:
                        preview = "📎 Media";
                        break;
                }
            }
            else
            {
                preview = "Message";
            }

            // Limit preview length
            if (preview.Length > 50)
            {
                preview = preview.Substring(0, 47) + "...";
            }

            return $"↩️ {originalMessage.DisplaySender}: {preview}";
        }

        private UIElement CreateMediaElement(TelegramMessage message)
        {
            var mediaContainer = new Border
            {
                Background = GetCachedResource("SecondaryBackground"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 8),
                MaxWidth = 400
            };

            var mediaPanel = new StackPanel();

            switch (message.MediaType?.ToLower())
            {
                case "voice_message":
                case "voice_note":
                    mediaPanel.Children.Add(CreateVoiceMessageElement(message));
                    break;
                
                case "sticker":
                    mediaPanel.Children.Add(CreateStickerElement(message));
                    break;
                
                case "animation":
                    mediaPanel.Children.Add(CreateAnimationElement(message));
                    break;
                
                case "video":
                    mediaPanel.Children.Add(CreateVideoElement(message));
                    break;
                
                case "photo":
                    mediaPanel.Children.Add(CreatePhotoElement(message));
                    break;
                
                default:
                    if (!string.IsNullOrEmpty(message.File))
                    {
                        mediaPanel.Children.Add(CreateGenericFileElement(message));
                    }
                    break;
            }

            if (mediaPanel.Children.Count > 0)
            {
                mediaContainer.Child = mediaPanel;
                return mediaContainer;
            }

            return null;
        }

        private UIElement CreateVoiceMessageElement(TelegramMessage message)
        {
            var voicePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // Voice icon
            var voiceIcon = new TextBlock
            {
                Text = "🎵",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            // Voice info
            var voiceInfo = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = "Voice Message",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = GetCachedResource("PrimaryText")
            };

            var durationText = new TextBlock
            {
                Text = message.DurationSeconds.HasValue ? 
                    $"Duration: {TimeSpan.FromSeconds(message.DurationSeconds.Value):mm\\:ss}" : 
                    "Voice message",
                FontSize = 11,
                Foreground = GetCachedResource("SecondaryText")
            };

            var fileNameText = new TextBlock
            {
                Text = Path.GetFileName(message.File),
                FontSize = 10,
                Foreground = GetCachedResource("SecondaryText"),
                FontStyle = FontStyles.Italic
            };

            voiceInfo.Children.Add(titleText);
            voiceInfo.Children.Add(durationText);
            voiceInfo.Children.Add(fileNameText);

            voicePanel.Children.Add(voiceIcon);
            voicePanel.Children.Add(voiceInfo);

            return voicePanel;
        }

        private UIElement CreateStickerElement(TelegramMessage message)
        {
            var stickerPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // Sticker emoji
            var stickerIcon = new TextBlock
            {
                Text = !string.IsNullOrEmpty(message.StickerEmoji) ? message.StickerEmoji : "😀",
                FontSize = 24,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            // Sticker info
            var stickerInfo = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = "Sticker",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = GetCachedResource("PrimaryText")
            };

            var dimensionsText = new TextBlock
            {
                Text = message.Width.HasValue && message.Height.HasValue ? 
                    $"{message.Width}x{message.Height}" : "Sticker",
                FontSize = 11,
                Foreground = GetCachedResource("SecondaryText")
            };

            var fileNameText = new TextBlock
            {
                Text = Path.GetFileName(message.File),
                FontSize = 10,
                Foreground = GetCachedResource("SecondaryText"),
                FontStyle = FontStyles.Italic
            };

            stickerInfo.Children.Add(titleText);
            stickerInfo.Children.Add(dimensionsText);
            stickerInfo.Children.Add(fileNameText);

            stickerPanel.Children.Add(stickerIcon);
            stickerPanel.Children.Add(stickerInfo);

            return stickerPanel;
        }

        private UIElement CreateAnimationElement(TelegramMessage message)
        {
            var animationPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // Animation icon
            var animationIcon = new TextBlock
            {
                Text = "🎞️",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            // Animation info
            var animationInfo = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = "GIF Animation",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = GetCachedResource("PrimaryText")
            };

            var detailsText = new TextBlock
            {
                Text = GetMediaDetailsText(message),
                FontSize = 11,
                Foreground = GetCachedResource("SecondaryText")
            };

            var fileNameText = new TextBlock
            {
                Text = Path.GetFileName(message.File),
                FontSize = 10,
                Foreground = GetCachedResource("SecondaryText"),
                FontStyle = FontStyles.Italic
            };

            animationInfo.Children.Add(titleText);
            animationInfo.Children.Add(detailsText);
            animationInfo.Children.Add(fileNameText);

            animationPanel.Children.Add(animationIcon);
            animationPanel.Children.Add(animationInfo);

            return animationPanel;
        }

        private UIElement CreateVideoElement(TelegramMessage message)
        {
            var videoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // Video icon
            var videoIcon = new TextBlock
            {
                Text = "📹",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            // Video info
            var videoInfo = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = "Video",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = GetCachedResource("PrimaryText")
            };

            var detailsText = new TextBlock
            {
                Text = GetMediaDetailsText(message),
                FontSize = 11,
                Foreground = GetCachedResource("SecondaryText")
            };

            var fileNameText = new TextBlock
            {
                Text = Path.GetFileName(message.File),
                FontSize = 10,
                Foreground = GetCachedResource("SecondaryText"),
                FontStyle = FontStyles.Italic
            };

            videoInfo.Children.Add(titleText);
            videoInfo.Children.Add(detailsText);
            videoInfo.Children.Add(fileNameText);

            videoPanel.Children.Add(videoIcon);
            videoPanel.Children.Add(videoInfo);

            return videoPanel;
        }

        private UIElement CreatePhotoElement(TelegramMessage message)
        {
            var photoPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // Photo icon
            var photoIcon = new TextBlock
            {
                Text = "📷",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            // Photo info
            var photoInfo = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = "Photo",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = GetCachedResource("PrimaryText")
            };

            var dimensionsText = new TextBlock
            {
                Text = message.Width.HasValue && message.Height.HasValue ? 
                    $"{message.Width}x{message.Height}" : "Photo",
                FontSize = 11,
                Foreground = GetCachedResource("SecondaryText")
            };

            var fileNameText = new TextBlock
            {
                Text = !string.IsNullOrEmpty(message.Photo) ? 
                    Path.GetFileName(message.Photo) : 
                    Path.GetFileName(message.File),
                FontSize = 10,
                Foreground = GetCachedResource("SecondaryText"),
                FontStyle = FontStyles.Italic
            };

            photoInfo.Children.Add(titleText);
            photoInfo.Children.Add(dimensionsText);
            photoInfo.Children.Add(fileNameText);

            photoPanel.Children.Add(photoIcon);
            photoPanel.Children.Add(photoInfo);

            return photoPanel;
        }

        private UIElement CreateGenericFileElement(TelegramMessage message)
        {
            var filePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            // File icon
            var fileIcon = new TextBlock
            {
                Text = "📎",
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };

            // File info
            var fileInfo = new StackPanel();
            
            var titleText = new TextBlock
            {
                Text = "File",
                FontWeight = FontWeights.SemiBold,
                FontSize = 12,
                Foreground = GetCachedResource("PrimaryText")
            };

            var sizeText = new TextBlock
            {
                Text = message.FileSize.HasValue ? 
                    FormatFileSize(message.FileSize.Value) : "File",
                FontSize = 11,
                Foreground = GetCachedResource("SecondaryText")
            };

            var fileNameText = new TextBlock
            {
                Text = Path.GetFileName(message.File),
                FontSize = 10,
                Foreground = GetCachedResource("SecondaryText"),
                FontStyle = FontStyles.Italic
            };

            fileInfo.Children.Add(titleText);
            fileInfo.Children.Add(sizeText);
            fileInfo.Children.Add(fileNameText);

            filePanel.Children.Add(fileIcon);
            filePanel.Children.Add(fileInfo);

            return filePanel;
        }

        private string GetMediaDetailsText(TelegramMessage message)
        {
            var details = new List<string>();

            if (message.Width.HasValue && message.Height.HasValue)
            {
                details.Add($"{message.Width}x{message.Height}");
            }

            if (message.DurationSeconds.HasValue)
            {
                details.Add($"{TimeSpan.FromSeconds(message.DurationSeconds.Value):mm\\:ss}");
            }

            if (message.FileSize.HasValue)
            {
                details.Add(FormatFileSize(message.FileSize.Value));
            }

            return details.Any() ? string.Join(" • ", details) : "Media file";
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }

        private bool GetMessageAlignment(TelegramMessage message, int messageIndex)
        {
            if (!_useAlternatingLayout)
            {
                return message.IsOutgoing;
            }

            var currentSender = message.DisplaySender;
            
            // If this is the first message or we're starting fresh, reset state
            if (messageIndex == 0 || string.IsNullOrEmpty(_lastMessageSender))
            {
                _lastMessageSender = currentSender;
                _currentSideIsRight = false; // Start with left
                return false;
            }

            // If same user as last message, keep the same side
            if (currentSender == _lastMessageSender)
            {
                return _currentSideIsRight;
            }

            // Different user - alternate the side
            _currentSideIsRight = !_currentSideIsRight;
            _lastMessageSender = currentSender;
            
            return _currentSideIsRight;
        }

        private void ResetAlternatingLayoutState()
        {
            _lastMessageSender = "";
            _currentSideIsRight = false;
        }

        private void ToggleMultiSelectMode()
        {
            if (_multiSelectMode)
            {
                ExitMultiSelectMode();
            }
            else
            {
                EnterMultiSelectMode();
            }
        }

        private void EnterMultiSelectMode()
        {
            _multiSelectMode = true;
            _selectedElements.Clear();
            
            // Change window title to indicate multi-select mode
            Title = "Telegram Chat Viewer - Multi-Select Mode (Ctrl+Shift+A to exit)";
            
            // Add click handlers to all message elements
            AddMultiSelectHandlers();
            
            _logger.Info("Entered multi-select mode");
        }

        private void ExitMultiSelectMode()
        {
            _multiSelectMode = false;
            
            // Restore original title
            Title = "Telegram Chat Viewer";
            
            // Clear selection and remove handlers
            ClearSelection();
            RemoveMultiSelectHandlers();
            
            _logger.Info("Exited multi-select mode");
        }

        private void AddMultiSelectHandlers()
        {
            foreach (UIElement child in MessagesContainer.Children)
            {
                if (child is StackPanel messageContainer)
                {
                    messageContainer.MouseLeftButtonDown += MessageContainer_MouseLeftButtonDown;
                    messageContainer.Background = Brushes.Transparent; // Make sure it can receive clicks
                }
            }
        }

        private void RemoveMultiSelectHandlers()
        {
            foreach (UIElement child in MessagesContainer.Children)
            {
                if (child is StackPanel messageContainer)
                {
                    messageContainer.MouseLeftButtonDown -= MessageContainer_MouseLeftButtonDown;
                }
            }
        }

        private void MessageContainer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (!_multiSelectMode) return;

            var element = sender as UIElement;
            if (element == null) return;

            if (_selectedElements.Contains(element))
            {
                // Deselect
                _selectedElements.Remove(element);
                element.Opacity = 1.0;
            }
            else
            {
                // Select
                _selectedElements.Add(element);
                element.Opacity = 0.7; // Visual indication of selection
            }

            e.Handled = true;
        }

        private void ClearSelection()
        {
            foreach (var element in _selectedElements)
            {
                element.Opacity = 1.0;
            }
            _selectedElements.Clear();
        }

        private void CopySelectedText()
        {
            if (!_selectedElements.Any()) return;

            var textBuilder = new StringBuilder();
            
            // Sort selected elements by their position in the UI
            var sortedElements = _selectedElements
                .Cast<StackPanel>()
                .OrderBy(el => MessagesContainer.Children.IndexOf(el))
                .ToList();

            foreach (var messageContainer in sortedElements)
            {
                var messageText = ExtractTextFromMessageContainer(messageContainer);
                if (!string.IsNullOrEmpty(messageText))
                {
                    textBuilder.AppendLine(messageText);
                }
            }

            if (textBuilder.Length > 0)
            {
                Clipboard.SetText(textBuilder.ToString().Trim());
                _logger.Info($"Copied {_selectedElements.Count} selected messages to clipboard");
                
                // Visual feedback
                Title = $"Telegram Chat Viewer - Copied {_selectedElements.Count} messages!";
                Task.Delay(2000).ContinueWith(_ => Dispatcher.Invoke(() => 
                {
                    if (_multiSelectMode)
                        Title = "Telegram Chat Viewer - Multi-Select Mode (Ctrl+Shift+A to exit)";
                    else
                        Title = "Telegram Chat Viewer";
                }));
            }
        }

        private string ExtractTextFromMessageContainer(StackPanel messageContainer)
        {
            var textBuilder = new StringBuilder();

            foreach (var child in messageContainer.Children)
            {
                if (child is StackPanel headerPanel)
                {
                    // Extract sender name
                    foreach (var headerChild in headerPanel.Children)
                    {
                        if (headerChild is TextBox senderName)
                        {
                            textBuilder.Append($"{senderName.Text}: ");
                            break;
                        }
                    }
                }
                else if (child is Grid messageGrid)
                {
                    // Extract message content
                    foreach (var gridChild in messageGrid.Children)
                    {
                        if (gridChild is Border bubble)
                        {
                            var messageText = ExtractTextFromBubble(bubble);
                            textBuilder.Append(messageText);
                        }
                    }
                }
            }

            return textBuilder.ToString().Trim();
        }

        private string ExtractTextFromBubble(Border bubble)
        {
            var textBuilder = new StringBuilder();
            
            if (bubble.Child is StackPanel contentPanel)
            {
                foreach (var child in contentPanel.Children)
                {
                    if (child is TextBox textBox)
                    {
                        if (!string.IsNullOrEmpty(textBox.Text) && textBox.FontSize > 10) // Skip timestamps
                        {
                            textBuilder.AppendLine(textBox.Text);
                        }
                    }
                    else if (child is RichTextBox richTextBox)
                    {
                        var textRange = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
                        textBuilder.AppendLine(textRange.Text);
                    }
                    else if (child is Border replyBorder && replyBorder.Child is TextBox replyText)
                    {
                        textBuilder.AppendLine($"Reply: {replyText.Text}");
                    }
                }
            }

            return textBuilder.ToString().Trim();
        }
    }
} 