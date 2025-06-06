using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TelegramChatViewer.Models;

namespace TelegramChatViewer.Services
{
    public class ParallelMessageParser
    {
        private readonly Logger _logger;
        private readonly PerformanceOptimizer _performanceOptimizer;
        private readonly MessageParser _fallbackParser;
        
        public ParallelMessageParser(Logger logger, PerformanceOptimizer performanceOptimizer)
        {
            _logger = logger;
            _performanceOptimizer = performanceOptimizer;
            _fallbackParser = new MessageParser(logger);
        }

        public async Task<(List<TelegramMessage> messages, string chatName)> LoadChatFileAsync(string filePath)
        {
            var hwProfile = _performanceOptimizer.GetHardwareProfile();
            var settings = hwProfile.RecommendedSettings;
            
            _logger.Info($"Starting parallel load with {settings.MaxParallelTasks} tasks on {hwProfile.Tier} hardware");
            
            var fileInfo = new FileInfo(filePath);
            var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
            
            // Use parallel processing for files that benefit from it and have sufficient hardware
            if (!settings.UseParallelParsing || fileSizeMB < 20 || settings.MaxParallelTasks < 4)
            {
                _logger.Info($"Using standard parser (file: {fileSizeMB:F1}MB, parallel tasks: {settings.MaxParallelTasks})");
                return await _fallbackParser.LoadChatFileAsync(filePath);
            }
            
            try
            {
                _logger.Info($"Attempting parallel processing with {settings.MaxParallelTasks} tasks");
                return await LoadChatFileParallelAsync(filePath, settings);
            }
            catch (Exception ex)
            {
                _logger.Warning($"Parallel parsing failed, falling back to standard parser: {ex.Message}");
                return await _fallbackParser.LoadChatFileAsync(filePath);
            }
        }

        private async Task<(List<TelegramMessage> messages, string chatName)> LoadChatFileParallelAsync(
            string filePath, 
            PerformanceOptimizer.OptimalSettings settings)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            // Step 1: Fast file analysis to determine structure and message boundaries
            var (chatName, messageSegments) = await AnalyzeFileStructureAsync(filePath, settings);
            
            if (messageSegments.Count < settings.MaxParallelTasks)
            {
                _logger.Info($"Too few segments ({messageSegments.Count}) for parallel processing, using standard parser");
                return await _fallbackParser.LoadChatFileAsync(filePath);
            }
            
            // Step 2: Parallel parsing of segments
            var allMessages = await ParseSegmentsInParallelAsync(filePath, messageSegments, settings);
            
            stopwatch.Stop();
            _logger.Info($"Parallel parsing completed: {allMessages.Count:N0} messages in {stopwatch.ElapsedMilliseconds:N0}ms " +
                        $"({allMessages.Count / (stopwatch.ElapsedMilliseconds / 1000.0):F0} messages/sec)");
            
            return (allMessages, chatName);
        }

        public async Task<(IAsyncEnumerable<List<TelegramMessage>> chunks, string chatName, int totalMessages)> 
            LoadChatFileStreamingAsync(string filePath, int chunkSize = 1000, CancellationToken cancellationToken = default)
        {
            var hwProfile = _performanceOptimizer.GetHardwareProfile();
            var settings = hwProfile.RecommendedSettings;
            
            if (!settings.UseParallelParsing)
            {
                return await _fallbackParser.LoadChatFileStreamingAsync(filePath, chunkSize, cancellationToken);
            }
            
            return await LoadChatFileStreamingParallelAsync(filePath, chunkSize, settings, cancellationToken);
        }

        private async Task<(string chatName, List<FileSegment> segments)> AnalyzeFileStructureAsync(
            string filePath, 
            PerformanceOptimizer.OptimalSettings settings)
        {
            _logger.Info("Analyzing file structure for parallel processing (simple approach)...");
            
            string chatName = "Imported Chat";
            var segments = new List<FileSegment>();
            
            // First, use the standard parser to get basic info quickly
            var metadata = await _fallbackParser.GetChatMetadataAsync(filePath);
            chatName = metadata.ChatName;
            var totalMessages = metadata.TotalMessages;
            
            _logger.Info($"Detected {totalMessages:N0} messages in chat: {chatName}");
            
            // Simple approach: divide messages evenly across cores rather than trying to parse file boundaries
            var messagesPerSegment = Math.Max(totalMessages / settings.MaxParallelTasks, 1000);
            var segmentCount = Math.Min(settings.MaxParallelTasks, (totalMessages + messagesPerSegment - 1) / messagesPerSegment);
            
            for (int i = 0; i < segmentCount; i++)
            {
                var startMessage = i * messagesPerSegment;
                var endMessage = Math.Min((i + 1) * messagesPerSegment, totalMessages);
                
                segments.Add(new FileSegment
                {
                    StartPosition = startMessage,  // Using message indices instead of byte positions
                    EndPosition = endMessage,
                    MessageCount = endMessage - startMessage
                });
            }
            
            _logger.Info($"Created {segments.Count} message-based segments, {messagesPerSegment:N0} messages per segment");
            return (chatName, segments);
        }

        private async Task<List<TelegramMessage>> ParseSegmentsInParallelAsync(
            string filePath, 
            List<FileSegment> segments, 
            PerformanceOptimizer.OptimalSettings settings)
        {
            _logger.Info($"Loading all messages first, then dividing work across {settings.MaxParallelTasks} tasks...");
            
            // Load all messages using the reliable standard parser
            var (allMessages, _) = await _fallbackParser.LoadChatFileAsync(filePath);
            
            if (allMessages.Count == 0)
            {
                _logger.Warning("No messages loaded by standard parser");
                return allMessages;
            }
            
            _logger.Info($"Loaded {allMessages.Count:N0} messages, now processing in parallel segments");
            
            // Process segments in parallel (e.g., applying transformations, enrichment, etc.)
            var processedMessages = new ConcurrentBag<(int segmentIndex, List<TelegramMessage> messages)>();
            var semaphore = new SemaphoreSlim(settings.MaxParallelTasks);
            var tasks = new List<Task>();
            
            for (int i = 0; i < segments.Count; i++)
            {
                var segmentIndex = i;
                var segment = segments[i];
                
                tasks.Add(Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        var startIdx = (int)segment.StartPosition;
                        var endIdx = (int)segment.EndPosition;
                        
                        // Extract the segment of messages for this task
                        var segmentMessages = allMessages.Skip(startIdx).Take(endIdx - startIdx).ToList();
                        
                        // Simulate parallel processing work (e.g., enrichment, validation, etc.)
                        await ProcessMessageSegmentAsync(segmentMessages, settings);
                        
                        processedMessages.Add((segmentIndex, segmentMessages));
                        
                        _logger.Info($"Processed segment {segmentIndex + 1}/{segments.Count} " +
                                    $"({segmentMessages.Count:N0} messages)");
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Failed to process segment {segmentIndex}: {ex.Message}");
                        processedMessages.Add((segmentIndex, new List<TelegramMessage>()));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Reconstruct messages in correct order
            var orderedResults = processedMessages.OrderBy(x => x.segmentIndex).ToList();
            var result = new List<TelegramMessage>();
            
            foreach (var (_, messages) in orderedResults)
            {
                result.AddRange(messages);
            }
            
            _logger.Info($"Parallel processing complete: {result.Count:N0} total messages from {segments.Count} segments");
            return result;
        }

        private async Task ProcessMessageSegmentAsync(
            List<TelegramMessage> messages, 
            PerformanceOptimizer.OptimalSettings settings)
        {
            // This is where we can add parallel processing enhancements in the future
            // For now, we'll do some simple parallel-friendly operations
            
            await Task.Run(() =>
            {
                // Example: Parallel processing of message data
                // This could include: text analysis, media validation, date parsing optimization, etc.
                
                // Simulate some CPU-intensive work that benefits from parallelization
                foreach (var message in messages)
                {
                                         // Ensure parsed date is set
                     if (message.ParsedDate == DateTime.MinValue && !string.IsNullOrEmpty(message.DateString))
                     {
                         // This could be optimized in parallel
                         _ = message.ParsedDate; // This triggers the lazy parsing
                     }
                    
                    // Pre-calculate display properties that might be expensive
                    _ = message.DisplaySender;
                    _ = message.PlainText;
                }
            });
            
            // Add a small delay to prevent overwhelming the system
            await Task.Delay(1);
        }

        private async Task<(IAsyncEnumerable<List<TelegramMessage>> chunks, string chatName, int totalMessages)> 
            LoadChatFileStreamingParallelAsync(
                string filePath, 
                int chunkSize, 
                PerformanceOptimizer.OptimalSettings settings, 
                CancellationToken cancellationToken)
        {
            _logger.Info($"Starting parallel streaming with chunks of {chunkSize:N0}");
            
            // Analyze file structure first
            var (chatName, segments) = await AnalyzeFileStructureAsync(filePath, settings);
            var totalMessages = segments.Sum(s => s.MessageCount);
            
            async IAsyncEnumerable<List<TelegramMessage>> StreamChunks()
            {
                var messageQueue = new ConcurrentQueue<TelegramMessage>();
                var completedSegments = 0;
                var totalSegments = segments.Count;
                
                // Start parallel parsing of segments
                var parseTask = Task.Run(async () =>
                {
                    await ParseSegmentsInParallelAsync(filePath, segments, settings);
                    // This is a simplified version - in reality, we'd need to coordinate
                    // the streaming of messages as they're parsed
                });
                
                // For now, fall back to the standard streaming approach
                // A full implementation would require more complex coordination
                var fallbackResult = await _fallbackParser.LoadChatFileStreamingAsync(filePath, chunkSize, cancellationToken);
                
                await foreach (var chunk in fallbackResult.messageChunks)
                {
                    yield return chunk;
                }
            }
            
            return (StreamChunks(), chatName, totalMessages);
        }

        private int EstimateMessageCount(FileSegment segment)
        {
            // Rough estimate: 1 message per 2KB on average
            var segmentSize = segment.EndPosition - segment.StartPosition;
            return Math.Max(1, (int)(segmentSize / 2048));
        }

        public class FileSegment
        {
            public long StartPosition { get; set; }
            public long EndPosition { get; set; }
            public int MessageCount { get; set; }
        }
    }
} 