# Telegram Chat Viewer

A high-performance WPF application for viewing and analyzing exported Telegram chat files with advanced text selection, multi-message copying, and optimized rendering for massive datasets.

## 🚀 Features

### 📱 **Chat Viewing**
- **JSON Import**: Load exported Telegram chat files (JSON format)
- **Rich Message Display**: Support for text, media, replies, forwards, and service messages  
- **User-based Alternating Layout**: Messages alternate sides only when users change
- **Theme Support**: Light and dark mode with automatic resource management
- **Member Color Coding**: Unique colors for each chat participant

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
- **Massive Load Mode**: Special handling for datasets over 5,000 messages

### 🎨 **Media Support**
- **Voice Messages**: Display with duration and audio icon
- **Stickers**: Large emoji display with dimensions
- **Videos/GIFs**: Video player icons with file info
- **Photos**: Image icons with resolution details
- **Files**: Generic file display with size formatting

## 🛠️ Installation

### Prerequisites
- **Windows 10/11** (x64)
- **.NET 8.0 Runtime** (or included in single-file executable)

### Option 1: Download Release
1. Download the latest release from [Releases](https://github.com/Akum3tsu/telegram-chat-viewer/releases)
2. Extract and run `TelegramChatViewer.exe`

### Option 2: Build from Source
```bash
git clone git@github.com:Akum3tsu/telegram-chat-viewer.git
cd telegram-chat-viewer
./build.bat
```

The executable will be created at: `bin/Release/net8.0-windows/win-x64/TelegramChatViewer.exe`

## 📋 Usage

### Loading Chat Files
1. **Click "Load Chat"** or use `Ctrl+O`
2. **Select your exported Telegram JSON file**
3. **Choose loading options** for large files:
   - **Fast Mode**: For files under 1,000 messages
   - **Progressive**: For 1,000-5,000 messages  
   - **Massive**: For 5,000+ messages

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
- **MessageParser.cs**: JSON parsing and message processing
- **Logger.cs**: Application logging and error tracking
- **TelegramMessage.cs**: Message data model

### **Performance Features**
- **Batch Rendering**: Process messages in chunks of 250 for responsiveness
- **Resource Caching**: Cache brushes and UI elements for faster rendering
- **Progressive Loading**: Load large files with real-time progress updates
- **Memory Optimization**: Efficient handling of massive datasets

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

### **File Format Support**
- **Telegram JSON Export**: Standard format from Telegram Desktop
- **Message Types**: Text, media, service messages, replies, forwards
- **Media Metadata**: File sizes, dimensions, durations
- **User Information**: Names, IDs, profile data

### **Performance Specifications**
- **Tested up to**: 100,000 messages (19+ MB files)
- **Loading Speed**: ~2-3 seconds for 50k messages
- **Memory Usage**: Optimized for large datasets
- **UI Responsiveness**: Non-blocking operations with progress feedback

## 🐛 Troubleshooting

### **Common Issues**
1. **File won't load**: Ensure it's a valid Telegram JSON export
2. **Performance issues**: Use "Massive Load" mode for large files
3. **Text selection not working**: Make sure you're not in multi-select mode
4. **Application crashes**: Check logs in application directory

### **Log Files**
- **Application logs**: Check the `logs/` directory
- **Startup errors**: `debug_startup.log` in application folder
- **Constructor errors**: `constructor_error.log` if startup fails

## 🤝 Contributing

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/amazing-feature`
3. **Make your changes**
4. **Run tests**: `./build.bat`
5. **Commit your changes**: `git commit -m 'Add amazing feature'`
6. **Push to branch**: `git push origin feature/amazing-feature`
7. **Open a Pull Request**

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- **Telegram Desktop** for the JSON export format
- **Microsoft WPF Team** for the excellent UI framework
- **Community Contributors** for testing and feedback

## 📊 Project Status

- ✅ **Core Functionality**: Complete
- ✅ **Text Selection**: Complete  
- ✅ **Multi-Message Selection**: Complete
- ✅ **Performance Optimization**: Complete
- ✅ **Media Support**: Complete
- 🔄 **Testing**: Ongoing
- 🔄 **Documentation**: In Progress

---

**Built with ❤️ for the Telegram community** 