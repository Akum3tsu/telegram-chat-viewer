using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TelegramChatViewer.Models;

namespace TelegramChatViewer.Services
{
    public class VirtualScrollManager
    {
        private readonly ScrollViewer _scrollViewer;
        private readonly StackPanel _container;
        private readonly Logger _logger;

        private List<TelegramMessage> _allMessages = new List<TelegramMessage>();
        private readonly Dictionary<int, FrameworkElement> _renderedElements = new Dictionary<int, FrameworkElement>();
        
        // Enhanced virtual scrolling parameters
        private double _estimatedItemHeight = 80.0;
        private const int BufferSize = 30;
        private int _maxRenderedItems = 200; // Adaptive based on performance
        private const int MinRenderedItems = 50;
        
        // Visible range tracking
        private int _visibleStartIndex = 0;
        private int _visibleEndIndex = 0;
        private int _lastVisibleStart = -1;
        private int _lastVisibleEnd = -1;
        
        // Infinite scroll
        private bool _infiniteScrollEnabled = false;
        private Func<Task> _loadMoreCallback;
        private bool _isLoadingMore = false;
        private bool _hasMoreMessages = true;
        private const double InfiniteScrollThreshold = 0.90;

        // Performance optimization
        private DateTime _lastUpdateTime = DateTime.MinValue;
        private const int UpdateThrottleMs = 50; // Throttle updates to 20 FPS
        private readonly Queue<double> _renderTimes = new Queue<double>();
        private const int MaxRenderTimesSamples = 10;

        // Memory management
        private int _totalElementsCreated = 0;
        private int _elementsRecycled = 0;
        private readonly Queue<FrameworkElement> _recycledElements = new Queue<FrameworkElement>();
        private const int MaxRecycledElements = 100;

        public VirtualScrollManager(ScrollViewer scrollViewer, StackPanel container, Logger logger)
        {
            _scrollViewer = scrollViewer;
            _container = container;
            _logger = logger;
            
            _scrollViewer.ScrollChanged += OnScrollChanged;
            
            // Adaptive max rendered items based on available memory
            AdaptMaxRenderedItems();
        }

        private void AdaptMaxRenderedItems()
        {
            try
            {
                // Get available memory (simplified approach)
                var workingSet = GC.GetTotalMemory(false);
                var availableMemoryMB = (Environment.WorkingSet - workingSet) / (1024.0 * 1024.0);
                
                if (availableMemoryMB > 500) // High memory
                    _maxRenderedItems = 300;
                else if (availableMemoryMB > 200) // Medium memory
                    _maxRenderedItems = 200;
                else // Low memory
                    _maxRenderedItems = 100;
                    
                _logger.Info($"Adapted max rendered items to {_maxRenderedItems} based on available memory");
            }
            catch
            {
                _maxRenderedItems = 200; // Default fallback
            }
        }

        public void SetMessages(List<TelegramMessage> messages)
        {
            _allMessages = messages;
            ClearRenderedElements();
            
            // Recalculate estimated item height based on message content
            RecalculateEstimatedItemHeight();
            
            UpdateVirtualScrolling();
            
            _logger.Info($"Virtual scroll manager set with {messages.Count} messages, estimated height: {_estimatedItemHeight:F1}px");
        }

        private void RecalculateEstimatedItemHeight()
        {
            if (_allMessages.Count == 0) return;

            // Sample first few messages to estimate height
            var sampleSize = Math.Min(10, _allMessages.Count);
            var totalEstimatedHeight = 0.0;
            
            for (int i = 0; i < sampleSize; i++)
            {
                var message = _allMessages[i];
                var estimatedHeight = EstimateMessageHeight(message);
                totalEstimatedHeight += estimatedHeight;
            }
            
            _estimatedItemHeight = totalEstimatedHeight / sampleSize;
            
            // Ensure reasonable bounds
            _estimatedItemHeight = Math.Max(40, Math.Min(_estimatedItemHeight, 200));
        }

        private double EstimateMessageHeight(TelegramMessage message)
        {
            double baseHeight = 60; // Base message height
            
            // Add height for text content
            var textLength = message.PlainText?.Length ?? 0;
            var estimatedLines = Math.Max(1, textLength / 50); // ~50 chars per line
            baseHeight += (estimatedLines - 1) * 20; // 20px per additional line
            
            // Add height for media
            if (message.HasMedia)
                baseHeight += 100;
                
            // Add height for replies
            if (message.IsReply)
                baseHeight += 30;
                
            // Add height for service messages
            if (message.IsServiceMessage)
                baseHeight = 40;
                
            return Math.Min(baseHeight, 300); // Cap at 300px
        }

        public void AddMessages(List<TelegramMessage> newMessages)
        {
            _allMessages.AddRange(newMessages);
            UpdateVirtualScrolling();
        }

        public void EnableInfiniteScroll(Func<Task> loadMoreCallback)
        {
            _infiniteScrollEnabled = true;
            _loadMoreCallback = loadMoreCallback;
        }

        public void DisableInfiniteScroll()
        {
            _infiniteScrollEnabled = false;
            _loadMoreCallback = null;
        }

        public void SetLoadingMore(bool loading)
        {
            _isLoadingMore = loading;
        }

        public void SetHasMoreMessages(bool hasMore)
        {
            _hasMoreMessages = hasMore;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Throttle updates for better performance
            var now = DateTime.UtcNow;
            if ((now - _lastUpdateTime).TotalMilliseconds < UpdateThrottleMs)
                return;
            _lastUpdateTime = now;

            var startTime = DateTime.UtcNow;
            
            UpdateVirtualScrolling();
            
            // Track render performance
            var renderTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
            _renderTimes.Enqueue(renderTime);
            if (_renderTimes.Count > MaxRenderTimesSamples)
                _renderTimes.Dequeue();
                
            // Adapt performance based on render times
            AdaptPerformanceSettings();
            
            // Check for infinite scroll trigger
            if (_infiniteScrollEnabled && !_isLoadingMore && _hasMoreMessages && _loadMoreCallback != null)
            {
                var scrollPosition = _scrollViewer.VerticalOffset / Math.Max(1, _scrollViewer.ScrollableHeight);
                
                if (scrollPosition > InfiniteScrollThreshold)
                {
                    _logger.Info($"Infinite scroll triggered at position {scrollPosition:F3}");
                    _isLoadingMore = true;
                    // Fire and forget the async operation to avoid blocking the UI
                    _ = _loadMoreCallback();
                }
            }
        }

        private void AdaptPerformanceSettings()
        {
            if (_renderTimes.Count < 3) return;
            
            var avgRenderTime = _renderTimes.Average();
            
            // If rendering is slow, reduce max rendered items
            if (avgRenderTime > 100 && _maxRenderedItems > MinRenderedItems)
            {
                _maxRenderedItems = Math.Max(MinRenderedItems, _maxRenderedItems - 20);
                _logger.Debug($"Reduced max rendered items to {_maxRenderedItems} due to slow rendering ({avgRenderTime:F1}ms)");
            }
            // If rendering is fast, we can increase max rendered items
            else if (avgRenderTime < 30 && _maxRenderedItems < 300)
            {
                _maxRenderedItems = Math.Min(300, _maxRenderedItems + 10);
                _logger.Debug($"Increased max rendered items to {_maxRenderedItems} due to fast rendering ({avgRenderTime:F1}ms)");
            }
        }

        private void UpdateVirtualScrolling()
        {
            if (_allMessages.Count == 0)
                return;

            var viewportHeight = _scrollViewer.ViewportHeight;
            var scrollOffset = _scrollViewer.VerticalOffset;
            
            // Calculate visible range with improved accuracy
            var startIndex = Math.Max(0, (int)(scrollOffset / _estimatedItemHeight) - BufferSize);
            var visibleItems = (int)(viewportHeight / _estimatedItemHeight) + 1;
            var endIndex = Math.Min(_allMessages.Count - 1, startIndex + visibleItems + BufferSize);

            // Ensure we don't render too many items
            var totalItemsToRender = endIndex - startIndex + 1;
            if (totalItemsToRender > _maxRenderedItems)
            {
                var center = (startIndex + endIndex) / 2;
                startIndex = Math.Max(0, center - _maxRenderedItems / 2);
                endIndex = Math.Min(_allMessages.Count - 1, startIndex + _maxRenderedItems);
            }

            // Only update if range changed significantly
            if (Math.Abs(startIndex - _lastVisibleStart) < 5 && Math.Abs(endIndex - _lastVisibleEnd) < 5)
                return;

            _visibleStartIndex = startIndex;
            _visibleEndIndex = endIndex;
            _lastVisibleStart = startIndex;
            _lastVisibleEnd = endIndex;
            
            _logger.Debug($"Virtual scroll range updated: {startIndex}-{endIndex} (total: {totalItemsToRender})");
            RenderVisibleItems();
        }

        private void RenderVisibleItems()
        {
            // Remove items that are no longer visible (with recycling)
            var itemsToRemove = _renderedElements.Keys
                .Where(index => index < _visibleStartIndex || index > _visibleEndIndex)
                .ToList();

            foreach (var index in itemsToRemove)
            {
                if (_renderedElements.TryGetValue(index, out var element))
                {
                    _container.Children.Remove(element);
                    _renderedElements.Remove(index);
                    
                    // Recycle element if possible
                    if (_recycledElements.Count < MaxRecycledElements)
                    {
                        // Clear element content for reuse
                        ClearElementContent(element);
                        _recycledElements.Enqueue(element);
                        _elementsRecycled++;
                    }
                }
            }

            // Calculate spacer heights more accurately
            var topSpacerHeight = CalculateSpacerHeight(0, _visibleStartIndex);
            var bottomSpacerHeight = CalculateSpacerHeight(_visibleEndIndex + 1, _allMessages.Count - 1);

            // Update top spacer
            UpdateSpacer("TopSpacer", topSpacerHeight, true);

            // Render visible items
            for (int i = _visibleStartIndex; i <= _visibleEndIndex; i++)
            {
                if (!_renderedElements.ContainsKey(i))
                {
                    var element = CreateOrRecycleMessageElement(_allMessages[i], i);
                    _renderedElements[i] = element;
                    
                    // Find correct insertion point
                    var insertIndex = FindInsertionIndex(i);
                    
                    if (insertIndex < _container.Children.Count)
                        _container.Children.Insert(insertIndex, element);
                    else
                        _container.Children.Add(element);
                }
            }

            // Update bottom spacer
            UpdateSpacer("BottomSpacer", bottomSpacerHeight, false);
        }

        private double CalculateSpacerHeight(int startIndex, int endIndex)
        {
            if (startIndex > endIndex) return 0;
            
            double totalHeight = 0;
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (i < _allMessages.Count)
                    totalHeight += EstimateMessageHeight(_allMessages[i]);
            }
            return totalHeight;
        }

        private void UpdateSpacer(string tag, double height, bool isTop)
        {
            var spacer = _container.Children.Cast<FrameworkElement>()
                .FirstOrDefault(x => x.Tag?.ToString() == tag) as Border;
                
            if (height > 0)
            {
                if (spacer == null)
                {
                    spacer = new Border
                    {
                        Height = height,
                        Tag = tag
                    };
                    
                    if (isTop)
                        _container.Children.Insert(0, spacer);
                    else
                        _container.Children.Add(spacer);
                }
                else
                {
                    spacer.Height = height;
                }
            }
            else if (spacer != null)
            {
                _container.Children.Remove(spacer);
            }
        }

        private int FindInsertionIndex(int messageIndex)
        {
            var insertIndex = 1; // After top spacer
            foreach (var kvp in _renderedElements.OrderBy(x => x.Key))
            {
                if (kvp.Key < messageIndex && _container.Children.Contains(kvp.Value))
                {
                    insertIndex = _container.Children.IndexOf(kvp.Value) + 1;
                }
            }
            return insertIndex;
        }

        private FrameworkElement CreateOrRecycleMessageElement(TelegramMessage message, int index)
        {
            FrameworkElement element;
            
            // Try to recycle an element first
            if (_recycledElements.Count > 0)
            {
                element = _recycledElements.Dequeue();
                // Update element with new message data
                UpdateElementContent(element, message, index);
            }
            else
            {
                element = CreateMessageElement(message, index);
                _totalElementsCreated++;
            }
            
            return element;
        }

        private FrameworkElement CreateMessageElement(TelegramMessage message, int index)
        {
            // Create a simple placeholder for now - this would be replaced with actual message rendering
            var border = new Border
            {
                Height = EstimateMessageHeight(message),
                Background = System.Windows.Media.Brushes.LightGray,
                Margin = new Thickness(5),
                Tag = $"Message_{index}"
            };
            
            var textBlock = new TextBlock
            {
                Text = $"Message {index}: {(string.IsNullOrEmpty(message.PlainText) ? "[No text]" : message.PlainText.Substring(0, Math.Min(50, message.PlainText.Length)))}...",
                Margin = new Thickness(10),
                TextWrapping = TextWrapping.Wrap
            };
            
            border.Child = textBlock;
            return border;
        }

        private void UpdateElementContent(FrameworkElement element, TelegramMessage message, int index)
        {
            if (element is Border border && border.Child is TextBlock textBlock)
            {
                border.Height = EstimateMessageHeight(message);
                border.Tag = $"Message_{index}";
                textBlock.Text = $"Message {index}: {(string.IsNullOrEmpty(message.PlainText) ? "[No text]" : message.PlainText.Substring(0, Math.Min(50, message.PlainText.Length)))}...";
            }
        }

        private void ClearElementContent(FrameworkElement element)
        {
            if (element is Border border && border.Child is TextBlock textBlock)
            {
                textBlock.Text = "";
                border.Tag = null;
            }
        }

        private void ClearRenderedElements()
        {
            _renderedElements.Clear();
            _container.Children.Clear();
            _recycledElements.Clear();
            
            _logger.Info($"Virtual scroll cleared. Stats - Created: {_totalElementsCreated}, Recycled: {_elementsRecycled}");
        }

        public void ScrollToMessage(int messageIndex)
        {
            if (messageIndex < 0 || messageIndex >= _allMessages.Count)
                return;

            var targetOffset = 0.0;
            for (int i = 0; i < messageIndex; i++)
            {
                targetOffset += EstimateMessageHeight(_allMessages[i]);
            }

            _scrollViewer.ScrollToVerticalOffset(targetOffset);
        }

        public List<TelegramMessage> GetVisibleMessages()
        {
            var visibleMessages = new List<TelegramMessage>();
            for (int i = _visibleStartIndex; i <= _visibleEndIndex && i < _allMessages.Count; i++)
            {
                visibleMessages.Add(_allMessages[i]);
            }
            return visibleMessages;
        }

        // Performance metrics
        public string GetPerformanceStats()
        {
            var avgRenderTime = _renderTimes.Count > 0 ? _renderTimes.Average() : 0;
            return $"Rendered: {_renderedElements.Count}/{_allMessages.Count}, " +
                   $"Created: {_totalElementsCreated}, Recycled: {_elementsRecycled}, " +
                   $"Avg Render: {avgRenderTime:F1}ms, Max Items: {_maxRenderedItems}";
        }
    }
} 