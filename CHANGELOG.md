# Changelog

All notable changes to the Telegram Chat Viewer project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2025-01-06

### 🎉 Major Features Added
- **Built-in Audio Player** - Voice messages now play directly in the app using NAudio
  - Full support for OGG Vorbis files (Telegram's default voice format)
  - Support for MP3, WAV, and other common audio formats
  - Play/pause controls with visual feedback
  - Automatic playback stopping and resource cleanup
  - Fallback to external player for unsupported formats

### 🎨 Enhanced User Experience
- **Improved Quote/Reply System**
  - Bigger, more readable text (increased font size to 12px)
  - Color-coded backgrounds matching the original sender's color
  - Enhanced styling with rounded corners and better spacing
  - Removed redundant sender name prefixes for cleaner display
  - Now shows "SenderName: QuotedMessage" format for clarity
  - Increased quote height (140px) for longer messages

- **Simplified Loading Configuration**
  - Removed redundant "Massive Load" checkbox
  - Set 5000 as the default chunk size for all file sizes
  - Automatic massive load determination based on chunk size
  - More user-friendly defaults that work well for typical users

- **Borderless Media Display**
  - Removed borders from photos, GIFs, and videos for cleaner look
  - Preserved interactive UI elements only where needed
  - Modern, streamlined visual presentation

### 🔧 Performance Optimizations
- **Universal 5000 Chunk Size Default**
  - Consistent 5000 message chunk size regardless of file size
  - Only loading strategy changes based on file size (Progressive vs Streaming)
  - Better balance between performance and user experience

### 🐛 Bug Fixes
- **Audio Playback Issues**
  - Fixed silent voice message playback (was using unsupported MediaElement for OGG)
  - Added comprehensive audio codec support through NAudio
  - Proper error handling and graceful fallbacks

- **Version Consistency**
  - Synchronized version numbers across all UI elements
  - Fixed mismatched version displays in splash screen and about dialog

### 🛠️ Technical Improvements
- **Enhanced Audio Engine**
  - Added NAudio (v2.2.1) and NAudio.Vorbis (v1.5.0) dependencies
  - Multi-tier audio playback system with intelligent format detection
  - Proper resource management and memory cleanup

- **Better Loading Logic**
  - Updated PerformanceOptimizer to use consistent 5000 chunk size
  - Removed hardware-based chunk size overrides for better UX
  - Maintained performance optimizations for loading strategies

### 📦 Release Information
- **File Size**: ~164MB (self-contained executable)
- **Target Framework**: .NET 8.0 Windows
- **Architecture**: x64
- **Dependencies**: All bundled in single executable

---

## [0.3.0] - 2025-01-05

### 🎨 User Interface Improvements
- Enhanced quote system with full readability
- Removed truncation limits for quoted messages
- Added service action support in quotes

### 🖼️ Media Enhancements
- Borderless media display for photos and videos
- Built-in video player with thumbnail previews
- Improved media element styling

### ⚡ Performance Updates
- Optimized message loading algorithms
- Enhanced virtual scrolling implementation
- Better memory management for large files

---

## [0.2.0] - Previous Release

### Initial Features
- Basic Telegram chat file loading
- Message display with formatting support
- Media file handling
- Search functionality
- Theme support (light/dark)
- Performance optimizations for large files

---

## [0.1.x] - Early Development

### Foundation
- Initial project setup
- Core message parsing
- Basic UI implementation
- File loading infrastructure 