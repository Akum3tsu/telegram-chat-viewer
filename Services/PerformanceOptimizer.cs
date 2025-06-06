using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading.Tasks;
using TelegramChatViewer.Models;

namespace TelegramChatViewer.Services
{
    public class PerformanceOptimizer
    {
        private readonly Logger _logger;
        private HardwareProfile _hardwareProfile;
        
        public PerformanceOptimizer(Logger logger)
        {
            _logger = logger;
            _hardwareProfile = AnalyzeHardware();
        }

        public class HardwareProfile
        {
            public int LogicalCores { get; set; }
            public int PhysicalCores { get; set; }
            public long TotalMemoryMB { get; set; }
            public long AvailableMemoryMB { get; set; }
            public bool HasSSD { get; set; }
            public string CPUName { get; set; } = "";
            public PerformanceTier Tier { get; set; }
            public OptimalSettings RecommendedSettings { get; set; } = new();
        }

        public enum PerformanceTier
        {
            Basic,      // <= 4 cores, <= 8GB RAM
            Standard,   // 4-8 cores, 8-16GB RAM
            High,       // 8-12 cores, 16-32GB RAM
            Extreme     // 12+ cores, 32+ GB RAM
        }

        public class OptimalSettings
        {
            public int MaxParallelTasks { get; set; }
            public int OptimalChunkSize { get; set; }
            public int IOBufferSize { get; set; }
            public int MemoryPoolSize { get; set; }
            public bool UseMemoryMapping { get; set; }
            public bool UseParallelParsing { get; set; }
            public bool UseAdvancedCaching { get; set; }
            public bool UseBulkOperations { get; set; }
        }

        private HardwareProfile AnalyzeHardware()
        {
            var profile = new HardwareProfile();
            
            try
            {
                // CPU Information
                profile.LogicalCores = Environment.ProcessorCount;
                profile.PhysicalCores = GetPhysicalCoreCount();
                profile.CPUName = GetCPUName();
                
                // Memory Information
                var memInfo = GC.GetGCMemoryInfo();
                profile.TotalMemoryMB = memInfo.TotalAvailableMemoryBytes / (1024 * 1024);
                profile.AvailableMemoryMB = GetAvailableMemoryMB();
                
                // Storage Information
                profile.HasSSD = DetectSSD();
                
                // Determine performance tier
                profile.Tier = DeterminePerformanceTier(profile);
                
                // Calculate optimal settings
                profile.RecommendedSettings = CalculateOptimalSettings(profile);
                
                _logger.Info($"Hardware Analysis Complete: " +
                           $"Tier={profile.Tier}, Cores={profile.LogicalCores}/{profile.PhysicalCores}, " +
                           $"RAM={profile.TotalMemoryMB}MB, SSD={profile.HasSSD}");
                
            }
            catch (Exception ex)
            {
                _logger.Warning($"Hardware analysis failed, using defaults: {ex.Message}");
                profile = GetDefaultProfile();
            }
            
            return profile;
        }

        private int GetPhysicalCoreCount()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("Select NumberOfCores from Win32_Processor");
                var cores = 0;
                foreach (var obj in searcher.Get())
                {
                    cores += int.Parse(obj["NumberOfCores"].ToString());
                }
                return cores > 0 ? cores : Environment.ProcessorCount / 2;
            }
            catch
            {
                return Environment.ProcessorCount / 2; // Assume hyperthreading
            }
        }

        private string GetCPUName()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("Select Name from Win32_Processor");
                foreach (var obj in searcher.Get())
                {
                    return obj["Name"]?.ToString()?.Trim() ?? "Unknown CPU";
                }
            }
            catch { }
            return "Unknown CPU";
        }

        private long GetAvailableMemoryMB()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("Select AvailableBytes from Win32_PerfRawData_PerfOS_Memory");
                foreach (var obj in searcher.Get())
                {
                    var availableBytes = Convert.ToInt64(obj["AvailableBytes"]);
                    return availableBytes / (1024 * 1024);
                }
            }
            catch { }
            
            // Fallback: estimate based on working set
            var workingSet = Environment.WorkingSet / (1024 * 1024);
            return Math.Max(1024, GC.GetGCMemoryInfo().TotalAvailableMemoryBytes / (1024 * 1024) - workingSet);
        }

        private bool DetectSSD()
        {
            try
            {
                var appDir = AppDomain.CurrentDomain.BaseDirectory;
                var drive = Path.GetPathRoot(appDir);
                
                using var searcher = new ManagementObjectSearcher($"SELECT MediaType FROM Win32_PhysicalMedia WHERE Tag LIKE '%{drive?.Replace("\\", "")}%'");
                foreach (var obj in searcher.Get())
                {
                    var mediaType = obj["MediaType"]?.ToString();
                    // SSD detection heuristics
                    return mediaType?.Contains("SSD") == true || 
                           mediaType?.Contains("Solid") == true ||
                           string.IsNullOrEmpty(mediaType); // Modern drives often don't report media type
                }
            }
            catch { }
            
            return true; // Assume SSD in modern systems
        }

        private PerformanceTier DeterminePerformanceTier(HardwareProfile profile)
        {
            var score = 0;
            
            // CPU scoring
            if (profile.LogicalCores >= 16) score += 3;
            else if (profile.LogicalCores >= 8) score += 2;
            else if (profile.LogicalCores >= 4) score += 1;
            
            // Memory scoring  
            if (profile.TotalMemoryMB >= 32000) score += 3;
            else if (profile.TotalMemoryMB >= 16000) score += 2;
            else if (profile.TotalMemoryMB >= 8000) score += 1;
            
            // Storage bonus
            if (profile.HasSSD) score += 1;
            
            return score switch
            {
                >= 6 => PerformanceTier.Extreme,
                >= 4 => PerformanceTier.High,
                >= 2 => PerformanceTier.Standard,
                _ => PerformanceTier.Basic
            };
        }

        private OptimalSettings CalculateOptimalSettings(HardwareProfile profile)
        {
            var settings = new OptimalSettings();
            
            switch (profile.Tier)
            {
                case PerformanceTier.Extreme:
                    settings.MaxParallelTasks = Math.Min(profile.LogicalCores, 12);
                    settings.OptimalChunkSize = 20000;
                    settings.IOBufferSize = 1048576; // 1MB
                    settings.MemoryPoolSize = Math.Min((int)(profile.AvailableMemoryMB * 0.3), 2048); // Up to 2GB
                    settings.UseMemoryMapping = true;
                    settings.UseParallelParsing = true;
                    settings.UseAdvancedCaching = true;
                    settings.UseBulkOperations = true;
                    break;
                    
                case PerformanceTier.High:
                    settings.MaxParallelTasks = Math.Min(profile.LogicalCores, 8);
                    settings.OptimalChunkSize = 10000;
                    settings.IOBufferSize = 524288; // 512KB
                    settings.MemoryPoolSize = Math.Min((int)(profile.AvailableMemoryMB * 0.25), 1024); // Up to 1GB
                    settings.UseMemoryMapping = true;
                    settings.UseParallelParsing = true;
                    settings.UseAdvancedCaching = true;
                    settings.UseBulkOperations = true;
                    break;
                    
                case PerformanceTier.Standard:
                    settings.MaxParallelTasks = Math.Min(profile.LogicalCores, 4);
                    settings.OptimalChunkSize = 5000;
                    settings.IOBufferSize = 262144; // 256KB
                    settings.MemoryPoolSize = Math.Min((int)(profile.AvailableMemoryMB * 0.2), 512); // Up to 512MB
                    settings.UseMemoryMapping = profile.HasSSD;
                    settings.UseParallelParsing = true;
                    settings.UseAdvancedCaching = false;
                    settings.UseBulkOperations = true;
                    break;
                    
                case PerformanceTier.Basic:
                default:
                    settings.MaxParallelTasks = Math.Min(profile.LogicalCores, 2);
                    settings.OptimalChunkSize = 2000;
                    settings.IOBufferSize = 131072; // 128KB
                    settings.MemoryPoolSize = Math.Min((int)(profile.AvailableMemoryMB * 0.15), 256); // Up to 256MB
                    settings.UseMemoryMapping = false;
                    settings.UseParallelParsing = false;
                    settings.UseAdvancedCaching = false;
                    settings.UseBulkOperations = false;
                    break;
            }
            
            return settings;
        }

        private HardwareProfile GetDefaultProfile()
        {
            return new HardwareProfile
            {
                LogicalCores = Environment.ProcessorCount,
                PhysicalCores = Environment.ProcessorCount / 2,
                TotalMemoryMB = 8192, // Assume 8GB
                AvailableMemoryMB = 4096,
                HasSSD = true,
                CPUName = "Unknown CPU",
                Tier = PerformanceTier.Standard,
                RecommendedSettings = new OptimalSettings
                {
                    MaxParallelTasks = 4,
                    OptimalChunkSize = 5000,
                    IOBufferSize = 262144,
                    MemoryPoolSize = 512,
                    UseMemoryMapping = true,
                    UseParallelParsing = true,
                    UseAdvancedCaching = false,
                    UseBulkOperations = true
                }
            };
        }

        public HardwareProfile GetHardwareProfile() => _hardwareProfile;
        
        public LoadingConfig GetOptimizedLoadingConfig(double fileSizeMB, int estimatedMessageCount)
        {
            var config = new LoadingConfig();
            var settings = _hardwareProfile.RecommendedSettings;
            
            // Adjust chunk size based on file size and hardware
            if (fileSizeMB > 500 || estimatedMessageCount > 200000)
            {
                config.ChunkSize = Math.Max(settings.OptimalChunkSize, 15000);
                config.LoadingStrategy = LoadingStrategy.Streaming;
            }
            else if (fileSizeMB > 100 || estimatedMessageCount > 50000)
            {
                config.ChunkSize = settings.OptimalChunkSize;
                config.LoadingStrategy = LoadingStrategy.Streaming;
            }
            else if (fileSizeMB > 20 || estimatedMessageCount > 10000)
            {
                config.ChunkSize = Math.Min(settings.OptimalChunkSize, 8000);
                config.LoadingStrategy = LoadingStrategy.Progressive;
            }
            else
            {
                config.ChunkSize = Math.Min(settings.OptimalChunkSize, 3000);
                config.LoadingStrategy = LoadingStrategy.LoadAll;
            }
            
            // Enable advanced features for high-end hardware
            config.UseMassiveLoad = settings.UseBulkOperations;
            config.UseVirtualScrolling = estimatedMessageCount > 5000;
            config.UseAlternatingLayout = true; // Always enable alternating layout for better readability
            
            _logger.Info($"Generated optimized config for {_hardwareProfile.Tier} hardware: " +
                        $"Strategy={config.LoadingStrategy}, ChunkSize={config.ChunkSize}, " +
                        $"MassiveLoad={config.UseMassiveLoad}");
            
            return config;
        }

        public void LogPerformanceReport()
        {
            var profile = _hardwareProfile;
            var settings = profile.RecommendedSettings;
            
            _logger.Info("=== PERFORMANCE HARDWARE REPORT ===");
            _logger.Info($"CPU: {profile.CPUName}");
            _logger.Info($"Cores: {profile.LogicalCores} logical, {profile.PhysicalCores} physical");
            _logger.Info($"Memory: {profile.TotalMemoryMB:N0} MB total, {profile.AvailableMemoryMB:N0} MB available");
            _logger.Info($"Storage: {(profile.HasSSD ? "SSD" : "HDD")} detected");
            _logger.Info($"Performance Tier: {profile.Tier}");
            _logger.Info("=== OPTIMIZATIONS ENABLED ===");
            _logger.Info($"Max Parallel Tasks: {settings.MaxParallelTasks}");
            _logger.Info($"Optimal Chunk Size: {settings.OptimalChunkSize:N0}");
            _logger.Info($"I/O Buffer: {settings.IOBufferSize / 1024:N0} KB");
            _logger.Info($"Memory Pool: {settings.MemoryPoolSize:N0} MB");
            _logger.Info($"Memory Mapping: {settings.UseMemoryMapping}");
            _logger.Info($"Parallel Parsing: {settings.UseParallelParsing}");
            _logger.Info($"Advanced Caching: {settings.UseAdvancedCaching}");
            _logger.Info($"Bulk Operations: {settings.UseBulkOperations}");
            _logger.Info("=====================================");
        }
    }
} 