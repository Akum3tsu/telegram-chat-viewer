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
            
            // Use parallel processing only for files that benefit from it
            var fileInfo = new FileInfo(filePath);
            var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
            
            if (!settings.UseParallelParsing || fileSizeMB < 10)
            {
                _logger.Info($"Using standard parser (file too small: {fileSizeMB:F1}MB)");
                return await _fallbackParser.LoadChatFileAsync(filePath);
            }
            
            try
            {
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
            _logger.Info("Analyzing file structure for parallel processing...");
            
            string chatName = "Imported Chat";
            var segments = new List<FileSegment>();
            
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, 
                FileShare.Read, bufferSize: settings.IOBufferSize);
            using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
            
            // Read initial portion to determine format and extract chat name
            var initialContent = new char[4096];
            var bytesRead = await reader.ReadAsync(initialContent, 0, initialContent.Length);
            var content = new string(initialContent, 0, bytesRead);
            
            // Extract chat name from initial content
            if (content.Contains("\"name\""))
            {
                try
                {
                    var nameMatch = System.Text.RegularExpressions.Regex.Match(content, @"""name""\s*:\s*""([^""]+)""");
                    if (nameMatch.Success)
                    {
                        chatName = nameMatch.Groups[1].Value;
                    }
                }
                catch { }
            }
            
            // Reset stream and scan for message boundaries
            fileStream.Seek(0, SeekOrigin.Begin);
            reader.DiscardBufferedData();
            
            var segmentSize = Math.Max(fileStream.Length / settings.MaxParallelTasks, 1024 * 1024); // Min 1MB per segment
            var currentSegment = new FileSegment { StartPosition = 0 };
            var position = 0L;
            var buffer = new byte[65536];
            var inMessageArray = false;
            var braceDepth = 0;
            var inString = false;
            var escapeNext = false;
            
            while (position < fileStream.Length)
            {
                var read = await fileStream.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0) break;
                
                for (int i = 0; i < read; i++)
                {
                    var c = (char)buffer[i];
                    position++;
                    
                    if (escapeNext)
                    {
                        escapeNext = false;
                        continue;
                    }
                    
                    if (c == '\\' && inString)
                    {
                        escapeNext = true;
                        continue;
                    }
                    
                    if (c == '"' && !escapeNext)
                    {
                        inString = !inString;
                        continue;
                    }
                    
                    if (inString) continue;
                    
                    switch (c)
                    {
                        case '[':
                            if (braceDepth == 1) inMessageArray = true;
                            break;
                        case '{':
                            braceDepth++;
                            break;
                        case '}':
                            braceDepth--;
                            // Message boundary in array
                            if (braceDepth == 1 && inMessageArray && position - currentSegment.StartPosition > segmentSize)
                            {
                                currentSegment.EndPosition = position;
                                currentSegment.MessageCount = EstimateMessageCount(currentSegment);
                                segments.Add(currentSegment);
                                
                                currentSegment = new FileSegment { StartPosition = position };
                            }
                            break;
                    }
                }
            }
            
            // Add final segment
            if (currentSegment.StartPosition < position)
            {
                currentSegment.EndPosition = position;
                currentSegment.MessageCount = EstimateMessageCount(currentSegment);
                segments.Add(currentSegment);
            }
            
            _logger.Info($"File analysis complete: {segments.Count} segments, chat: {chatName}");
            return (chatName, segments);
        }

        private async Task<List<TelegramMessage>> ParseSegmentsInParallelAsync(
            string filePath, 
            List<FileSegment> segments, 
            PerformanceOptimizer.OptimalSettings settings)
        {
            _logger.Info($"Parsing {segments.Count} segments in parallel using {settings.MaxParallelTasks} tasks...");
            
            var allMessages = new ConcurrentBag<(int segmentIndex, List<TelegramMessage> messages)>();
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
                        var segmentMessages = await ParseSegmentAsync(filePath, segment, settings);
                        allMessages.Add((segmentIndex, segmentMessages));
                        
                        if (segmentIndex % 5 == 0) // Progress logging
                        {
                            _logger.Info($"Completed segment {segmentIndex + 1}/{segments.Count} " +
                                        $"({segmentMessages.Count:N0} messages)");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Failed to parse segment {segmentIndex}: {ex.Message}");
                        allMessages.Add((segmentIndex, new List<TelegramMessage>()));
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }));
            }
            
            await Task.WhenAll(tasks);
            
            // Reconstruct messages in correct order
            var orderedResults = allMessages.OrderBy(x => x.segmentIndex).ToList();
            var result = new List<TelegramMessage>();
            
            foreach (var (_, messages) in orderedResults)
            {
                result.AddRange(messages);
            }
            
            _logger.Info($"Parallel parsing complete: {result.Count:N0} total messages from {segments.Count} segments");
            return result;
        }

        private async Task<List<TelegramMessage>> ParseSegmentAsync(
            string filePath, 
            FileSegment segment, 
            PerformanceOptimizer.OptimalSettings settings)
        {
            var messages = new List<TelegramMessage>();
            
            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, 
                FileShare.Read, bufferSize: settings.IOBufferSize);
            
            fileStream.Seek(segment.StartPosition, SeekOrigin.Begin);
            using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
            using var jsonReader = new JsonTextReader(reader);
            
            var serializer = JsonSerializer.CreateDefault();
            var remainingBytes = segment.EndPosition - segment.StartPosition;
            var buffer = new char[Math.Min(remainingBytes, 32768)];
            
            try
            {
                while (await jsonReader.ReadAsync() && fileStream.Position < segment.EndPosition)
                {
                    if (jsonReader.TokenType == JsonToken.StartObject)
                    {
                        try
                        {
                            var messageToken = await JToken.ReadFromAsync(jsonReader);
                            var message = messageToken.ToObject<TelegramMessage>(serializer);
                            
                            if (message != null)
                            {
                                messages.Add(message);
                            }
                        }
                        catch (JsonException)
                        {
                            // Skip malformed message and continue
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning($"Error parsing segment at position {segment.StartPosition}: {ex.Message}");
            }
            
            return messages;
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