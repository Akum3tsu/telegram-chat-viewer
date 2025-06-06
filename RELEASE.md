# Release Process - Telegram Chat Viewer

This document explains how to create releases using the automated GitHub Actions workflow.

## 🚀 Automated Release Process

The project uses GitHub Actions to automatically build and create releases when you push version tags.

### **Workflow Features**
✅ **Automatic Building** - Builds the application with .NET 8.0  
✅ **Single-File Executable** - Creates standalone `.exe` file  
✅ **Portable Version** - Framework-dependent version for smaller size  
✅ **Source Archive** - Complete source code zip  
✅ **Release Notes** - Automatically generated with feature highlights  
✅ **Multiple Formats** - Executable, portable, and source downloads  

## 📋 Creating a Release

### **Method 1: Using the Release Script (Recommended)**

1. **Run the release script**:
   ```powershell
   .\create-release.ps1 -Version "1.2.0" -Push
   ```

2. **The script will**:
   - Update `version.txt` with the new version
   - Create a git tag (e.g., `v1.2.0`)
   - Push the tag to GitHub
   - Trigger the GitHub Actions workflow

### **Method 2: Manual Process**

1. **Update version.txt**:
   ```
   echo "1.2.0" > version.txt
   ```

2. **Commit the version change**:
   ```bash
   git add version.txt
   git commit -m "Prepare release v1.2.0"
   ```

3. **Create and push the tag**:
   ```bash
   git tag -a v1.2.0 -m "Release v1.2.0"
   git push origin main
   git push origin v1.2.0
   ```

## 🏗️ What the Workflow Does

### **1. Build Process**
- Sets up .NET 8.0 environment
- Restores NuGet packages
- Builds in Release configuration
- Runs tests (if any exist)

### **2. Publishing**
- **Single-File Executable**: Self-contained `.exe` with all dependencies
- **Portable Version**: Requires .NET 8.0 Runtime (smaller download)
- **Source Archive**: Complete source code

### **3. Release Creation**
- Creates a GitHub Release with the tag name
- Uploads all build artifacts
- Generates comprehensive release notes
- Includes download links and usage instructions

## 📦 Release Artifacts

Each release includes:

### **Downloads**
| File | Description | Size | Requirements |
|------|-------------|------|--------------|
| `TelegramChatViewer-vX.X.X-win-x64.exe` | Single-file executable | ~15MB | Windows 10/11 x64 |
| `TelegramChatViewer-vX.X.X-portable.zip` | Portable version | ~2MB | .NET 8.0 Runtime |
| `TelegramChatViewer-vX.X.X-source.zip` | Source code | ~1MB | Development |

### **Documentation**
- `README.md` - Main documentation
- `FEATURES.md` - Detailed feature documentation

## 🔧 Workflow Configuration

The workflow is defined in `.github/workflows/dotnet-desktop.yml` and:

### **Triggers On**
- Push to `main` branch (builds but doesn't release)
- Push tags starting with `v` (creates release)
- Pull requests (builds and tests)

### **Build Matrix**
- **Platform**: Windows Latest
- **Configuration**: Release only
- **Runtime**: win-x64 (Windows 64-bit)

### **Environment Variables**
```yaml
Solution_Name: TelegramChatViewer.csproj
Project_Directory: .
```

## 🛠️ Customizing Releases

### **Version Numbering**
Follow semantic versioning: `MAJOR.MINOR.PATCH`
- **MAJOR**: Breaking changes
- **MINOR**: New features (backward compatible)
- **PATCH**: Bug fixes

### **Release Notes**
The workflow automatically generates release notes. To customize:
1. Edit the `body:` section in `.github/workflows/dotnet-desktop.yml`
2. Add changelog information
3. Update feature descriptions

### **Build Options**
Modify publish settings in the workflow:
```yaml
- name: Publish Windows x64
  run: |
    dotnet publish $env:Solution_Name `
      --configuration Release `
      --runtime win-x64 `
      --self-contained true `
      -p:PublishSingleFile=true `
      -p:EnableCompressionInSingleFile=true
```

## 📊 Monitoring Builds

### **GitHub Actions**
- View build progress: `https://github.com/Akum3tsu/telegram-chat-viewer/actions`
- Check logs for any build failures
- Download artifacts before they're published

### **Release Status**
- **In Progress**: Yellow circle - Build is running
- **Success**: Green checkmark - Release created successfully  
- **Failed**: Red X - Check logs for errors

## 🚨 Troubleshooting

### **Common Issues**

1. **Build Failures**
   - Check .NET version compatibility
   - Verify project file syntax
   - Review dependency versions

2. **Missing Artifacts**
   - Ensure paths in workflow are correct
   - Check if files exist after build
   - Verify artifact upload configuration

3. **Tag Issues**
   - Tags must start with `v` (e.g., `v1.0.0`)
   - Don't reuse existing tag names
   - Ensure proper semantic versioning

### **Manual Recovery**
If automatic release fails:
1. Delete the problematic tag: `git tag -d v1.0.0 && git push origin :refs/tags/v1.0.0`
2. Fix the issue
3. Re-create the tag and push again

## 🎯 Best Practices

### **Before Creating a Release**
✅ **Test thoroughly** with sample JSON files  
✅ **Update documentation** if features changed  
✅ **Verify version number** follows semantic versioning  
✅ **Check all features** work as expected  
✅ **Review commit history** since last release  

### **Release Timing**
- Create releases for significant feature additions
- Include bug fixes in patch releases
- Group related features in minor releases
- Save breaking changes for major releases

### **Quality Assurance**
- Test the built executable on a clean Windows machine
- Verify all text selection features work
- Test with large JSON files (performance)
- Ensure UI themes work correctly

---

## Summary

The automated release process makes it easy to distribute new versions of the Telegram Chat Viewer. Simply run the release script or create a tag, and GitHub Actions handles the rest - building, packaging, and publishing your release with professional documentation and multiple download options. 