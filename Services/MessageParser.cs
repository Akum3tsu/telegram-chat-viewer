using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TelegramChatViewer.Models;
using System.Threading;

namespace TelegramChatViewer.Services
{
    public class MessageParser
    {
        private readonly Logger _logger;

        public MessageParser(Logger logger)
        {
            _logger = logger;
        }

        public async Task<(List<TelegramMessage> messages, string chatName)> LoadChatFileAsync(string filePath)
        {
            _logger.Info($"Loading chat file: {filePath}");

            try
            {
                var fileInfo = new FileInfo(filePath);
                var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
                
                _logger.Info($"File size: {fileSizeMB:F2} MB");

                // For large files, use streaming approach
                if (fileSizeMB > 50)
                {
                    _logger.Info("Using streaming parser for large file");
                    var streamingResult = await LoadChatFileStreamingAsync(filePath);
                    var allMessages = new List<TelegramMessage>();
                    
                    await foreach (var chunk in streamingResult.messageChunks)
                    {
                        allMessages.AddRange(chunk);
                    }
                    
                    return (allMessages, streamingResult.chatName);
                }

                string jsonContent;
                using (var reader = new StreamReader(filePath, System.Text.Encoding.UTF8))
                {
                    jsonContent = await reader.ReadToEndAsync();
                }

                // First, let's analyze the JSON structure
                JToken rootToken;
                try
                {
                    rootToken = JToken.Parse(jsonContent);
                    _logger.Info($"JSON root type: {rootToken.Type}");
                }
                catch (JsonException ex)
                {
                    _logger.Error($"Invalid JSON format: {ex.Message}");
                    
                    // Show preview of the file content for debugging
                    var preview = jsonContent.Length > 500 ? jsonContent.Substring(0, 500) + "..." : jsonContent;
                    _logger.Error($"File preview: {preview}");
                    
                    throw new InvalidOperationException($"Invalid JSON format: {ex.Message}");
                }

                // Try to parse as TelegramChat first
                if (rootToken.Type == JTokenType.Object)
                {
                    var rootObj = (JObject)rootToken;
                    _logger.Info($"Root object has {rootObj.Properties().Count()} properties");
                    
                    foreach (var prop in rootObj.Properties().Take(5)) // Log first 5 properties
                    {
                        _logger.Info($"Property: {prop.Name} = {prop.Value.Type}");
                    }

                    if (rootObj.ContainsKey("messages"))
                    {
                        try
                        {
                            var chat = JsonConvert.DeserializeObject<TelegramChat>(jsonContent);
                            if (chat?.Messages != null)
                            {
                                _logger.Info($"Successfully loaded chat: {chat.Name} with {chat.Messages.Count} messages");
                                return (chat.Messages, chat.Name);
                            }
                        }
                        catch (JsonException ex)
                        {
                            _logger.Error($"Failed to parse as TelegramChat - JsonException: {ex.Message}");
                            
                            // JsonException from Newtonsoft.Json doesn't have Path, LineNumber, LinePosition
                            // Let's just log the full exception details
                            _logger.Error($"Full JsonException: {ex}");
                        }
                        catch (Exception ex)
                        {
                            _logger.Error($"Failed to parse as TelegramChat - General Exception: {ex.Message}");
                            _logger.Error($"Exception Type: {ex.GetType().Name}");
                            if (ex.InnerException != null)
                            {
                                _logger.Error($"Inner Exception: {ex.InnerException.Message}");
                            }
                        }
                    }
                    else
                    {
                        _logger.Warning("Root object does not contain 'messages' property");
                    }
                }

                // Try to parse as direct message array
                if (rootToken.Type == JTokenType.Array)
                {
                    var rootArray = (JArray)rootToken;
                    _logger.Info($"Root array has {rootArray.Count} elements");
                    
                    if (rootArray.Count > 0)
                    {
                        _logger.Info($"First element type: {rootArray[0].Type}");
                    }

                    try
                    {
                        var messages = JsonConvert.DeserializeObject<List<TelegramMessage>>(jsonContent);
                        if (messages != null)
                        {
                            _logger.Info($"Successfully loaded message array with {messages.Count} messages");
                            return (messages, "Imported Chat");
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.Error($"Failed to parse as message array - JsonException: {ex.Message}");
                        _logger.Error($"Full JsonException: {ex}");
                    }
                    catch (Exception ex)
                    {
                        _logger.Error($"Failed to parse as message array - General Exception: {ex.Message}");
                        _logger.Error($"Exception Type: {ex.GetType().Name}");
                        if (ex.InnerException != null)
                        {
                            _logger.Error($"Inner Exception: {ex.InnerException.Message}");
                        }
                    }
                }

                // If we get here, neither parsing method worked
                var errorMessage = $"Invalid JSON structure. Expected 'messages' array or direct message array.\n" +
                                 $"Found root type: {rootToken.Type}";
                
                if (rootToken.Type == JTokenType.Object)
                {
                    var rootObj = (JObject)rootToken;
                    var properties = string.Join(", ", rootObj.Properties().Select(p => p.Name));
                    errorMessage += $"\nRoot object properties: {properties}";
                }

                _logger.Error(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            catch (Exception ex)
            {
                _logger.Error($"Error loading chat file: {filePath}", ex);
                throw;
            }
        }

        // New optimized streaming method
        public async Task<(IAsyncEnumerable<List<TelegramMessage>> messageChunks, string chatName, int totalMessages)> LoadChatFileStreamingAsync(
            string filePath, 
            int chunkSize = 1000, 
            CancellationToken cancellationToken = default)
        {
            _logger.Info($"Loading chat file with streaming: {filePath} (chunk_size: {chunkSize})");

            var fileInfo = new FileInfo(filePath);
            var fileSizeMB = fileInfo.Length / (1024.0 * 1024.0);
            _logger.Info($"File size: {fileSizeMB:F2} MB");

            string chatName = "Imported Chat";
            int totalMessages = 0;

            // First pass: analyze structure and count messages
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536))
            using (var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8))
            using (var jsonReader = new JsonTextReader(reader))
            {
                var (detectedChatName, messageCount) = await AnalyzeJsonStructureAsync(jsonReader);
                chatName = detectedChatName;
                totalMessages = messageCount;
            }

            _logger.Info($"Detected {totalMessages} messages in chat: {chatName}");

            // Second pass: stream messages in chunks
            async IAsyncEnumerable<List<TelegramMessage>> StreamMessages()
            {
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536);
                using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
                using var jsonReader = new JsonTextReader(reader);

                var currentChunk = new List<TelegramMessage>();
                var messageCount = 0;

                await foreach (var message in ParseMessagesStreamingAsync(jsonReader, cancellationToken))
                {
                    currentChunk.Add(message);
                    messageCount++;

                    if (currentChunk.Count >= chunkSize)
                    {
                        _logger.Debug($"Yielding chunk: messages {messageCount - chunkSize + 1}-{messageCount}");
                        yield return new List<TelegramMessage>(currentChunk);
                        currentChunk.Clear();
                        
                        // Allow UI updates and check for cancellation
                        await Task.Delay(1, cancellationToken);
                    }
                }

                // Yield remaining messages
                if (currentChunk.Count > 0)
                {
                    _logger.Debug($"Yielding final chunk: {currentChunk.Count} messages");
                    yield return currentChunk;
                }
            }

            return (StreamMessages(), chatName, totalMessages);
        }

        private async Task<(string chatName, int messageCount)> AnalyzeJsonStructureAsync(JsonTextReader jsonReader)
        {
            string chatName = "Imported Chat";
            int messageCount = 0;
            bool inMessagesArray = false;
            int depth = 0;

            while (await jsonReader.ReadAsync())
            {
                switch (jsonReader.TokenType)
                {
                    case JsonToken.StartObject:
                        depth++;
                        break;
                        
                    case JsonToken.EndObject:
                        depth--;
                        if (depth == 1 && inMessagesArray)
                        {
                            messageCount++;
                        }
                        break;
                        
                    case JsonToken.StartArray:
                        if (depth == 1 && jsonReader.Path == "messages")
                        {
                            inMessagesArray = true;
                        }
                        else if (depth == 0)
                        {
                            // Root array - direct message array format
                            inMessagesArray = true;
                        }
                        break;
                        
                    case JsonToken.EndArray:
                        if (jsonReader.Path == "messages" || depth == 0)
                        {
                            inMessagesArray = false;
                        }
                        break;
                        
                    case JsonToken.PropertyName:
                        if (depth == 1 && jsonReader.Value?.ToString() == "name")
                        {
                            if (await jsonReader.ReadAsync() && jsonReader.TokenType == JsonToken.String)
                            {
                                chatName = jsonReader.Value?.ToString() ?? "Imported Chat";
                            }
                        }
                        break;
                }
            }

            return (chatName, messageCount);
        }

        private async IAsyncEnumerable<TelegramMessage> ParseMessagesStreamingAsync(
            JsonTextReader jsonReader, 
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            bool inMessagesArray = false;
            int depth = 0;
            var serializer = JsonSerializer.CreateDefault();

            while (await jsonReader.ReadAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (jsonReader.TokenType)
                {
                    case JsonToken.StartArray:
                        if (depth == 1 && jsonReader.Path == "messages")
                        {
                            inMessagesArray = true;
                        }
                        else if (depth == 0)
                        {
                            // Root array - direct message array format
                            inMessagesArray = true;
                        }
                        break;
                        
                    case JsonToken.EndArray:
                        if (jsonReader.Path == "messages" || depth == 0)
                        {
                            inMessagesArray = false;
                        }
                        break;
                        
                    case JsonToken.StartObject:
                        if (inMessagesArray)
                        {
                            TelegramMessage message = null;
                            try
                            {
                                var messageToken = await JToken.ReadFromAsync(jsonReader, cancellationToken);
                                message = messageToken.ToObject<TelegramMessage>(serializer);
                            }
                            catch (JsonException ex)
                            {
                                _logger.Warning($"Failed to parse message at position {jsonReader.LineNumber}: {ex.Message}");
                                // Continue parsing other messages
                            }
                            
                            if (message != null)
                            {
                                yield return message;
                            }
                        }
                        else
                        {
                            depth++;
                        }
                        break;
                        
                    case JsonToken.EndObject:
                        if (!inMessagesArray)
                        {
                            depth--;
                        }
                        break;
                }
            }
        }

        public async Task<(IAsyncEnumerable<List<TelegramMessage>> chunks, string chatName, int totalMessages)> LoadChatFileChunkedAsync(string filePath, int chunkSize = 1000)
        {
            _logger.Info($"Loading chat file in chunks: {filePath} (chunk_size: {chunkSize})");

            // Use streaming approach for better memory efficiency
            return await LoadChatFileStreamingAsync(filePath, chunkSize);
        }

        public static string FormatServiceMessage(string actor, string action, string title = "", List<string> members = null)
        {
            members ??= new List<string>();

            return action switch
            {
                "create_group" => $"{actor} created the group \"{title}\"",
                "edit_group_title" => $"{actor} changed the group name to \"{title}\"",
                "edit_group_photo" => $"{actor} changed the group photo",
                "delete_group_photo" => $"{actor} removed the group photo",
                "invite_members" => members.Count switch
                {
                    0 => $"{actor} invited someone to the group",
                    1 => $"{actor} invited {members[0]} to the group",
                    _ => $"{actor} invited {string.Join(", ", members)} to the group"
                },
                "remove_members" => members.Count switch
                {
                    0 => $"{actor} removed someone from the group",
                    1 => $"{actor} removed {members[0]} from the group",
                    _ => $"{actor} removed {string.Join(", ", members)} from the group"
                },
                "join_group_by_link" => $"{actor} joined the group via invite link",
                "migrate_to_supergroup" => $"{actor} upgraded this group to a supergroup",
                "migrate_from_group" => $"This supergroup was upgraded from a basic group",
                "pin_message" => $"{actor} pinned a message",
                "unpin_message" => $"{actor} unpinned a message",
                "clear_history" => $"{actor} cleared the chat history",
                "phone_call" => $"{actor} made a call",
                "missed_call" => $"Missed call from {actor}",
                _ => $"{actor} performed action: {action}"
            };
        }

        public static string GetReplyPreviewText(TelegramMessage originalMessage, int maxLength = 50)
        {
            if (originalMessage == null)
                return "Original message not found";

            var text = originalMessage.PlainText;

            if (string.IsNullOrEmpty(text))
            {
                if (originalMessage.HasMedia)
                {
                    return originalMessage.MediaType switch
                    {
                        "photo" => "📷 Photo",
                        "video" => "🎬 Video",
                        "sticker" => "😀 Sticker",
                        "voice" => "🎤 Voice message",
                        "video_message" => "📹 Video message",
                        "animation" => "🎞️ GIF",
                        _ => "📎 Media"
                    };
                }
                return "Message";
            }

            if (text.Length <= maxLength)
                return text;

            return text.Substring(0, maxLength - 3) + "...";
        }

        public static MediaInfo GetMediaInfo(TelegramMessage message)
        {
            if (!message.HasMedia)
                return null;

            var mediaInfo = new MediaInfo();

            if (!string.IsNullOrEmpty(message.Photo))
            {
                mediaInfo.Type = "photo";
                mediaInfo.FilePath = message.Photo;
            }
            else if (!string.IsNullOrEmpty(message.File))
            {
                mediaInfo.Type = message.MediaType ?? "file";
                mediaInfo.FilePath = message.File;
            }

            mediaInfo.ThumbnailPath = message.Thumbnail ?? "";
            mediaInfo.Width = message.Width ?? 0;
            mediaInfo.Height = message.Height ?? 0;
            mediaInfo.FileSize = message.FileSize ?? 0;
            mediaInfo.Duration = message.DurationSeconds ?? 0;
            mediaInfo.StickerEmoji = message.StickerEmoji ?? "";
            mediaInfo.MimeType = message.MimeType ?? "";

            return mediaInfo;
        }

        public static string FormatFileSize(long sizeBytes)
        {
            if (sizeBytes == 0)
                return "";

            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double size = sizeBytes;
            int unitIndex = 0;

            while (size >= 1024 && unitIndex < units.Length - 1)
            {
                size /= 1024;
                unitIndex++;
            }

            return $"{size:F1} {units[unitIndex]}";
        }

        public static string FormatDuration(int seconds)
        {
            if (seconds == 0)
                return "";

            var timeSpan = TimeSpan.FromSeconds(seconds);

            if (timeSpan.Hours > 0)
                return $"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
            else
                return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
        }

        public TelegramMessage FindOriginalMessage(List<TelegramMessage> messages, int replyToId)
        {
            return messages.FirstOrDefault(m => m.Id == replyToId);
        }

        public List<TelegramMessage> SearchMessages(List<TelegramMessage> messages, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new List<TelegramMessage>();

            var lowerSearchTerm = searchTerm.ToLowerInvariant();

            return messages.Where(message =>
                message.PlainText.ToLowerInvariant().Contains(lowerSearchTerm) ||
                message.DisplaySender.ToLowerInvariant().Contains(lowerSearchTerm)
            ).ToList();
        }

        // Optimized method for very large files - loads only metadata first
        public async Task<ChatMetadata> GetChatMetadataAsync(string filePath)
        {
            _logger.Info($"Analyzing chat metadata: {filePath}");

            var fileInfo = new FileInfo(filePath);
            var metadata = new ChatMetadata
            {
                FilePath = filePath,
                FileSizeBytes = fileInfo.Length,
                FileSizeMB = fileInfo.Length / (1024.0 * 1024.0)
            };

            using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536);
            using var reader = new StreamReader(fileStream, System.Text.Encoding.UTF8);
            using var jsonReader = new JsonTextReader(reader);

            var (chatName, messageCount, dateRange) = await AnalyzeJsonStructureDetailedAsync(jsonReader);
            
            metadata.ChatName = chatName;
            metadata.TotalMessages = messageCount;
            metadata.DateRange = dateRange;
            metadata.EstimatedMemoryUsageMB = (messageCount * 2.0) / 1024; // Rough estimate: 2KB per message

            _logger.Info($"Metadata analysis complete: {messageCount} messages, {metadata.FileSizeMB:F1}MB file");
            return metadata;
        }

        private async Task<(string chatName, int messageCount, (DateTime start, DateTime end))> AnalyzeJsonStructureDetailedAsync(JsonTextReader jsonReader)
        {
            string chatName = "Imported Chat";
            int messageCount = 0;
            bool inMessagesArray = false;
            int depth = 0;
            DateTime firstDate = DateTime.MaxValue;
            DateTime lastDate = DateTime.MinValue;

            while (await jsonReader.ReadAsync())
            {
                switch (jsonReader.TokenType)
                {
                    case JsonToken.StartObject:
                        depth++;
                        break;
                        
                    case JsonToken.EndObject:
                        depth--;
                        if (depth == 1 && inMessagesArray)
                        {
                            messageCount++;
                        }
                        break;
                        
                    case JsonToken.StartArray:
                        if (depth == 1 && jsonReader.Path == "messages")
                        {
                            inMessagesArray = true;
                        }
                        else if (depth == 0)
                        {
                            inMessagesArray = true;
                        }
                        break;
                        
                    case JsonToken.EndArray:
                        if (jsonReader.Path == "messages" || depth == 0)
                        {
                            inMessagesArray = false;
                        }
                        break;
                        
                    case JsonToken.PropertyName:
                        if (depth == 1 && jsonReader.Value?.ToString() == "name")
                        {
                            if (await jsonReader.ReadAsync() && jsonReader.TokenType == JsonToken.String)
                            {
                                chatName = jsonReader.Value?.ToString() ?? "Imported Chat";
                            }
                        }
                        else if (depth == 2 && inMessagesArray && jsonReader.Value?.ToString() == "date_unixtime")
                        {
                            if (await jsonReader.ReadAsync() && jsonReader.TokenType == JsonToken.Integer)
                            {
                                if (long.TryParse(jsonReader.Value?.ToString(), out var unixTime))
                                {
                                    var date = DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;
                                    if (date < firstDate) firstDate = date;
                                    if (date > lastDate) lastDate = date;
                                }
                            }
                        }
                        break;
                }
            }

            var dateRange = firstDate != DateTime.MaxValue ? (firstDate, lastDate) : (DateTime.MinValue, DateTime.MinValue);
            return (chatName, messageCount, dateRange);
        }

        // Adaptive chunk size based on available memory and file size
        public int GetOptimalChunkSize(long fileSizeBytes, long availableMemoryBytes)
        {
            var fileSizeMB = fileSizeBytes / (1024.0 * 1024.0);
            var availableMemoryMB = availableMemoryBytes / (1024.0 * 1024.0);

            // Conservative approach: use 10% of available memory for message chunks
            var targetMemoryUsageMB = Math.Min(availableMemoryMB * 0.1, 100); // Cap at 100MB
            
            // Estimate 2KB per message on average
            var estimatedChunkSize = (int)(targetMemoryUsageMB * 1024 / 2);
            
            // Apply bounds based on file size
            if (fileSizeMB < 10) return Math.Min(estimatedChunkSize, 1000);      // Small files: up to 1K messages
            if (fileSizeMB < 50) return Math.Min(estimatedChunkSize, 2000);      // Medium files: up to 2K messages  
            if (fileSizeMB < 200) return Math.Min(estimatedChunkSize, 5000);     // Large files: up to 5K messages
            
            return Math.Max(1000, Math.Min(estimatedChunkSize, 10000));          // Very large files: 1K-10K messages
        }
    }

    // Metadata class for chat analysis
    public class ChatMetadata
    {
        public string FilePath { get; set; } = "";
        public string ChatName { get; set; } = "";
        public int TotalMessages { get; set; }
        public long FileSizeBytes { get; set; }
        public double FileSizeMB { get; set; }
        public double EstimatedMemoryUsageMB { get; set; }
        public (DateTime start, DateTime end) DateRange { get; set; }
        
        public bool IsLargeFile => FileSizeMB > 50;
        public bool IsVeryLargeFile => FileSizeMB > 200;
        public bool HasManyMessages => TotalMessages > 10000;
        
        public string GetRecommendedStrategy()
        {
            if (IsVeryLargeFile || TotalMessages > 50000)
                return "Use streaming with virtual scrolling and aggressive chunking";
            if (IsLargeFile || TotalMessages > 10000)
                return "Use streaming with progressive loading";
            return "Standard loading is suitable";
        }
    }
} 