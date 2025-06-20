# Changelog

All notable changes to the Telegram Chat Viewer project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.3] - 2025-01-28

### ✨ Major Search Enhancements
- **Complete Search Functionality** - Search now properly highlights and navigates to found messages
  - **Visual Highlighting**: Found messages now display with bright yellow background and orange glow effect
  - **Auto-Scrolling**: Search results automatically center in the viewport
  - **Navigation**: Previous/Next arrows now properly jump between search results
  - **Smart Loading**: Automatically loads more messages if search target isn't yet rendered (infinite scroll support)
  - **Visual Feedback**: 5-second highlight duration with automatic fade-out
  - **Keyboard Support**: F3/Shift+F3, Enter to search, Escape to clear

### 🎯 Enhanced User Experience
- **Search Result Visibility**: Impossible to miss found messages with bright highlighting
- **Smooth Navigation**: Seamless jumping between multiple search results
- **Real-time Feedback**: Visual confirmation of which message matched your search
- **Intelligent Loading**: Works with both small and large chat files (infinite scroll compatible)

### 🔧 Technical Improvements
- **Message-to-Element Mapping**: Efficient tracking system for UI elements and their source messages
- **Dynamic Loading**: Smart loading of additional messages when searching beyond currently rendered content
- **Memory Management**: Proper cleanup of highlight effects and element mappings
- **Async Navigation**: Non-blocking search result jumping with proper error handling

---

## [1.0.2] - 2025-01-28

### 🐛 Bug Fixes
- **Comprehensive Timestamp Fix** - Completely eliminated timezone conversion issues
  - **Universal Fix**: Works for all Telegram exports regardless of which date field is present
  - **Date-only exports**: Fixed exports that only have "date" field (missing "date_unixtime")
  - **No Timezone Conversion**: Timestamps now display exactly as they appear in source data
  - **DateTimeKind.Unspecified**: Forces all parsed dates to prevent any system timezone interference
  - Example: JSON `"2016-04-28T11:24:08"` now always displays as `11:24` regardless of user's timezone

### 🎨 UI Improvements
- **Splash Screen Version Fix** - Splash screen now correctly displays current version
  - Fixed hardcoded version display from v0.4.2 to dynamic version reading
  - Consistent version display across splash screen, status bar, and about dialog
  - Updated fallback version handling for better reliability

### 🔧 Technical Improvements
- **Enhanced Date Parsing Algorithm** - Bulletproof timestamp handling
  - Completely timezone-agnostic date parsing
  - Preserves exact timestamp data from any Telegram export format
  - Works consistently across different system configurations and timezones

---

## [1.0.1] - 2025-01-28

### 🐛 Bug Fixes
- **Timestamp Display Issue** - Fixed incorrect time display in message timestamps
  - Messages now show correct time matching the source JSON data
  - Fixed `ParsedDate` property to prioritize ISO date string format over Unix timestamp
  - Eliminated timezone conversion issues that caused 2-hour time difference
  - Example: Messages now correctly show `11:24` instead of `09:24` when source data is `2016-04-28T11:24:08`

### 🔧 Technical Improvements
- **Enhanced Date Parsing** - More reliable timestamp handling
  - Prioritized ISO 8601 date string format for accuracy
  - Improved fallback mechanism for Unix timestamp conversion
  - Better preservation of original timestamp data from Telegram exports

---

## [1.0.0] - 2025-01-28

### 🎉 First Official Release
- **Stable Release** - First production-ready version of Telegram Chat Viewer
- **Feature Complete** - All core functionality tested and optimized for daily use

### 🎨 Visual Improvements
- **Simplified Theme System** - Removed dark theme complexity, kept clean light theme
  - Removed theme toggle from View menu for simplified user experience
  - Optimized light theme colors for better readability
  - Consistent theming across all dialogs and windows
  - Improved resource management and performance

### 🛠️ Code Quality Improvements
- **Removed Theme Complexity** - Eliminated dark theme code paths
  - Simplified color palette management
  - Removed conditional theme switching logic
  - Cleaner, more maintainable codebase
  - Better performance due to reduced branching

### 📦 Release Notes
- **Self-Contained Executable** - No external dependencies required
- **Windows x64 Optimized** - Built specifically for Windows 10/11 x64
- **Production Ready** - Thoroughly tested for stability and performance
- **File Size**: ~164MB (all dependencies included)

---

## [0.4.2] - 2025-01-06

### 🐛 Bug Fixes
- **Search TextBox Text Cropping** - Fixed search input text being cropped and unreadable
  - Created specialized `SearchTextBoxStyle` with optimized padding (8,3 instead of 8,6)
  - Added proper vertical content alignment and centering
  - Improved text visibility within 30px height constraint
  - Enhanced template with better ScrollViewer positioning

- **OGG Audio Playback Issues** - Completely resolved "Could not initialize container!" errors
  - Implemented robust multi-tier fallback system for OGG voice messages
  - **Tier 1**: NAudio.Vorbis for inline playback (when possible)
  - **Tier 2**: Automatic external player fallback for problematic OGG files
  - **Tier 3**: MediaElement and system player fallbacks for other formats
  - Added smart UI feedback with temporary play state for external players
  - Enhanced error handling with graceful fallbacks instead of blocking dialogs
  - Added FFMpegCore dependency for future audio enhancement capabilities

- **Consecutive Message Grouping** - Fixed messages from same user showing duplicate headers
  - Corrected `ShouldShowSenderHeader` logic to use proper message indices
  - Fixed both optimized batch rendering and standard rendering paths
  - Updated `AddBasicMessage` to support consecutive message detection
  - Enhanced `messageIndex` calculation during batch processing

### 🔧 Technical Improvements
- **Enhanced Audio Error Handling** - Comprehensive logging and debugging for audio issues
- **Improved Resource Management** - Better cleanup and disposal of audio resources
- **UI Responsiveness** - Optimized search interface styling and layout
- **Code Quality** - Removed duplicate using directives and cleaned up syntax

### 📝 Documentation
- Updated README.md with current build instructions and feature list
- Enhanced error messages with specific guidance for different failure scenarios

## [0.4.1] - 2025-01-06

### 🐛 Bug Fixes
- **Sticker Rendering** - Fixed stickers to display actual images instead of just emoji
  - Stickers now render as proper images from .webp files
  - Added support for both main file and thumbnail fallback
  - Maintains borderless design for clean appearance
  - Click to open in external viewer functionality
  - Graceful fallback to emoji display if image can't be loaded
  - Added emoji label below sticker for context

### 🔧 Technical Improvements
- Enhanced sticker handling to match photo and animation rendering
- Improved error handling for missing or corrupted sticker files
- Better file path resolution for sticker assets

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
- Theme support (light)
- Performance optimizations for large files

---

## [0.1.x] - Early Development

### Foundation
- Initial project setup
- Core message parsing
- Basic UI implementation
- File loading infrastructure 