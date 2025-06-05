using System;
using System.IO;
using System.Reflection;

namespace TelegramChatViewer.Services
{
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly string _logDirectory;
        private readonly object _lockObject = new object();
        private const long MaxLogFileSize = 10 * 1024 * 1024; // 10 MB

        public Logger()
        {
            // Get the directory where the executable is located (works with single-file apps)
            _logDirectory = AppContext.BaseDirectory;
            
            // Ensure the directory exists
            Directory.CreateDirectory(_logDirectory);
            
            // Create log file path in the same directory as the executable
            _logFilePath = Path.Combine(_logDirectory, "telegram_chat_viewer.log");
            
            // Initialize log file with application start
            Info($"=== Telegram Chat Viewer Started - Version {GetApplicationVersion()} ===");
            Info($"Log file location: {_logFilePath}");
        }

        public void Info(string message)
        {
            WriteLog("INFO", message);
        }

        public void Warning(string message)
        {
            WriteLog("WARNING", message);
        }

        public void Error(string message, Exception exception = null)
        {
            var fullMessage = exception != null ? $"{message} - {exception}" : message;
            WriteLog("ERROR", fullMessage);
        }

        public void Debug(string message)
        {
#if DEBUG
            WriteLog("DEBUG", message);
#endif
        }

        private void WriteLog(string level, string message)
        {
            lock (_lockObject)
            {
                try
                {
                    // Check if log rotation is needed
                    RotateLogIfNeeded();
                    
                    var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                    
                    // Also write to console in debug mode
#if DEBUG
                    Console.WriteLine(logEntry);
#endif
                }
                catch (Exception ex)
                {
                    // Try to write to console if file logging fails
                    try
                    {
                        Console.WriteLine($"[LOGGER ERROR] Failed to write to log file: {ex.Message}");
                        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
                    }
                    catch
                    {
                        // Ignore console errors to prevent infinite loops
                    }
                }
            }
        }

        private void RotateLogIfNeeded()
        {
            try
            {
                if (File.Exists(_logFilePath))
                {
                    var fileInfo = new FileInfo(_logFilePath);
                    if (fileInfo.Length > MaxLogFileSize)
                    {
                        // Rename current log file with timestamp
                        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var backupPath = Path.Combine(_logDirectory, $"telegram_chat_viewer_{timestamp}.log");
                        File.Move(_logFilePath, backupPath);
                        
                        // Clean up old backup files (keep only last 5)
                        CleanupOldLogFiles();
                    }
                }
            }
            catch
            {
                // Ignore rotation errors and continue with current file
            }
        }

        private void CleanupOldLogFiles()
        {
            try
            {
                var logFiles = Directory.GetFiles(_logDirectory, "telegram_chat_viewer_*.log");
                if (logFiles.Length > 5)
                {
                    Array.Sort(logFiles);
                    for (int i = 0; i < logFiles.Length - 5; i++)
                    {
                        File.Delete(logFiles[i]);
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private string GetApplicationVersion()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        public string GetLogLocation()
        {
            return _logFilePath;
        }

        public string GetLogDirectory()
        {
            return _logDirectory;
        }

        public void LogApplicationExit()
        {
            Info("=== Telegram Chat Viewer Shutdown ===");
        }
    }
} 