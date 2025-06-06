--------------------------------------------

# DISCLAIMER

The entire project, code , examples and even everything below the disclaimer of this readme are the result of on-going vibe coding using [Cursor AI](https://www.cursor.com/) with less as possible action by a real person.

Goal of this approach is to explore and understand how much an AI today can do even with little to nothing help from an experienced (and especially a beginner) programmer.

Feel free to fork this project to your liking, hoping this will be helpful to somebody else project as well :)  

---------------------------------------------
# Telegram Chat Viewer

A high-performance WPF application for viewing and analyzing exported Telegram chat files with advanced text selection, multi-message copying, built-in media playback, and optimized rendering for massive datasets.

## 🚀 Features

### 📱 **Chat Viewing**
- **JSON Import**: Load exported Telegram chat files (JSON format)
- **Rich Message Display**: Support for text, media, replies, forwards, and service messages  
- **User-based Alternating Layout**: Messages alternate sides only when users change
- **Theme Support**: Light and dark mode with automatic resource management
- **Member Color Coding**: Unique colors for each chat participant
- **Enhanced Quote System**: Color-coded reply backgrounds matching original sender colors

### 🔍 **Search & Navigation**
- **Real-time Search**: Find messages with live search results
- **Search Navigation**: Navigate between search results with F3/Shift+F3
- **Keyboard Shortcuts**: Comprehensive keyboard navigation support

### ✂️ **Advanced Text Selection**
- **Individual Text Selection**: Select any text element (usernames, messages, timestamps)
- **Multi-Message Selection**: `Ctrl+Shift+A` to select multiple entire messages
- **Cross-Message Copying**: Copy conversations in a clean, readable format
- **Rich Text Support**: Select formatted text (bold, italic, code, links)

### ⚡ **Performance Optimization**
- **Infinite Scroll**: Efficiently handle massive chat files (100k+ messages)
- **Batch Processing**: Progressive loading with real-time progress feedback
- **Memory Management**: Optimized resource caching and cleanup
- **Simplified Loading**: Optimized defaults (5,000 chunk size) for better performance
- **Hardware Optimization**: Automatic performance tuning based on system capabilities

### 🎵 **Built-in Media Player**
- **Voice Messages**: Built-in audio player with OGG Vorbis support (Telegram's format)
- **Multiple Audio Formats**: Support for OGG, MP3, WAV, and other common formats
- **Fallback System**: Multi-tier audio system with graceful fallbacks
- **External Player Integration**: Click to open in system default player

### 🎨 **Enhanced Media Support**
- **Borderless Media Display**: Clean, modern presentation for photos and videos
- **Sticker Rendering**: Display actual sticker images (.webp) instead of just emoji
- **Videos/GIFs**: Integrated video player with controls
- **Photos**: High-quality image display with click-to-view functionality
- **Files**: Generic file display with size formatting and quick access

## 🛠️ Installation

### Prerequisites
- **Windows 10/11** (x64)
- **.NET 8.0 Runtime** (included in single-file executable)

### Option 1: Download Release (Recommended)
1. Download the latest release from [Releases](https://github.com/Akum3tsu/telegram-chat-viewer/releases)
2. Extract `TelegramChatViewer.exe` (~164MB self-contained)
3. Run the executable - no additional setup required!

### Option 2: Build from Source
```bash
# Clone the repository
git clone https://github.com/Akum3tsu/telegram-chat-viewer.git
cd telegram-chat-viewer

# Build release version
dotnet build -c Release

# Or publish self-contained executable
dotnet publish -c Release
```

The executable will be created at: `bin/Release/net8.0-windows/win-x64/publish/TelegramChatViewer.exe`

## 📋 Usage

### Loading Chat Files
1. **Click "Load Chat"** or use `Ctrl+O`
2. **Select your exported Telegram JSON file**
3. **Choose loading strategy** (automatically optimized):
   - **Progressive Loading**: For most files (default 5,000 chunk size)
   - **Streaming**: For very large files (automatic detection)

### Media Playback
#### **Voice Messages**
- **Click play button** on voice messages for built-in playback
- **Supports OGG Vorbis** (Telegram's default format)
- **Multiple fallbacks** for different audio formats
- **Click message** to open in external player if needed

#### **Photos & Videos**
- **Borderless display** for clean viewing
- **Click to expand** or open in external viewer
- **Automatic format detection** and optimization

#### **Stickers**
- **Full image rendering** from .webp files
- **Emoji context** displayed below sticker
- **Click to view** in external application

### Text Selection
#### **Individual Text Selection**
- **Click and drag** to select any text
- **Double-click** to select words
- **Triple-click** to select all text in an element
- **Ctrl+A** when focused on text controls

#### **Multi-Message Selection**
1. **Press `Ctrl+Shift+A`** to enter multi-select mode
2. **Click messages** to select/deselect them (they become semi-transparent)
3. **Press `Ctrl+C`** to copy all selected messages
4. **Press `Esc`** to exit multi-select mode

**Output Format:**
```
Alice Johnson: Hey everyone! I wanted to share this amazing article...
Bob Smith: That's fantastic! I love being able to copy multiple messages...
Charlie Brown: How do you use the multi-select feature?
```

### Search
- **Type in search box** for real-time results
- **Press Enter** or click Search button
- **Use F3/Shift+F3** to navigate results
- **Press Esc** to clear search

### Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| `Ctrl+O` | Open chat file |
| `Ctrl+Shift+A` | Toggle multi-select mode |
| `Ctrl+C` | Copy (in multi-select mode) |
| `F3` | Next search result |
| `Shift+F3` | Previous search result |
| `Esc` | Exit multi-select / Clear search |

## 🏗️ Architecture

### **Core Components**
- **MainWindow.xaml.cs**: Main application logic and UI management
- **ParallelMessageParser.cs**: Multithreaded JSON parsing and message processing
- **PerformanceOptimizer.cs**: Hardware-based performance tuning
- **Logger.cs**: Application logging and error tracking
- **TelegramMessage.cs**: Message data model

### **Performance Features**
- **Parallel Processing**: Multithreaded parsing for faster loading
- **Adaptive Chunking**: Dynamic chunk sizes based on system performance
- **Resource Caching**: Cache brushes and UI elements for faster rendering
- **Progressive Loading**: Load large files with real-time progress updates
- **Memory Optimization**: Efficient handling of massive datasets

### **Media Architecture**
- **NAudio Integration**: Professional audio library for voice message playback
- **Multi-tier Fallback**: Multiple audio backends for maximum compatibility
- **Borderless Design**: Clean media presentation without unnecessary borders
- **Format Support**: OGG Vorbis, MP3, WAV, WebP, JPEG, PNG, MP4, GIF

### **Text Selection Architecture**
- **TextBox with ReadOnly**: For selectable plain text elements
- **RichTextBox**: For rich formatted text with selection support
- **Multi-Select Handler**: Click-based message selection with visual feedback
- **Text Extraction**: Intelligent content parsing from UI elements

## 🔧 Technical Details

### **Dependencies**
- **.NET 8.0** (Windows Desktop)
- **WPF Framework** for UI
- **Newtonsoft.Json** for JSON parsing
- **CommunityToolkit.Mvvm** for MVVM patterns
- **NAudio** (v2.2.1) for audio playback
- **NAudio.Vorbis** (v1.5.0) for OGG Vorbis support

### **File Format Support**
- **Telegram JSON Export**: Standard format from Telegram Desktop
- **Message Types**: Text, media, service messages, replies, forwards
- **Audio Formats**: OGG Vorbis, MP3, WAV, WMA
- **Image Formats**: WebP, JPEG, PNG, GIF
- **Video Formats**: MP4, AVI, MOV
- **Media Metadata**: File sizes, dimensions, durations

### **Performance Specifications**
- **Tested up to**: 100,000+ messages (19+ MB files)
- **Loading Speed**: ~2-3 seconds for 50k messages
- **Memory Usage**: Optimized for large datasets
- **UI Responsiveness**: Non-blocking operations with progress feedback
- **Self-contained**: 164MB executable with all dependencies included

## 🐛 Troubleshooting

### **Common Issues**
1. **File won't load**: Ensure it's a valid Telegram JSON export
2. **Audio not playing**: Check if audio files are in the same directory as JSON
3. **Performance issues**: Application automatically optimizes for your hardware
4. **Stickers not showing**: Ensure sticker files (.webp) are in the correct folder
5. **Application crashes**: Check logs in application directory

### **Log Files**
- **Application logs**: Check the `logs/` directory
- **Startup errors**: `debug_startup.log` in application folder
- **Constructor errors**: `constructor_error.log` if startup fails

## 📈 Version History

See [CHANGELOG.md](CHANGELOG.md) for detailed version history and release notes.

**Current Version: 0.4.1**
- ✅ Built-in Audio Player with OGG support
- ✅ Enhanced Quote/Reply System
- ✅ Borderless Media Display
- ✅ Sticker Image Rendering
- ✅ Simplified Loading Configuration
- ✅ Automated Release Pipeline

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Make your changes**
4. **Test thoroughly**: `dotnet build -c Release`
5. **Commit your changes**: `git commit -m 'Add amazing feature'`
6. **Push to branch**: `git push origin feature/amazing-feature`
7. **Open a Pull Request**

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Telegram Desktop** for the JSON export format
- **Microsoft WPF Team** for the excellent UI framework
- **NAudio Project** for professional audio library
- **Community Contributors** for testing and feedback

## 📊 Project Status

- ✅ **Core Functionality**: Complete
- ✅ **Text Selection**: Complete  
- ✅ **Multi-Message Selection**: Complete
- ✅ **Performance Optimization**: Complete
- ✅ **Audio Player**: Complete
- ✅ **Enhanced Media Support**: Complete
- ✅ **Automated Releases**: Complete
- 🔄 **Testing**: Ongoing
- 🔄 **Documentation**: Continuously Updated

---

**Built with ❤️ for the Telegram community** 

*Version 0.4.1 - Enhanced Media Experience*
