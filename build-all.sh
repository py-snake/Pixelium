#!/bin/bash

# Pixelium Multi-Platform Build Script
# Builds binaries for Linux, Windows, and macOS

set -e  # Exit on error

echo "=========================================="
echo "Pixelium Multi-Platform Build Script"
echo "=========================================="
echo ""

# Configuration
PROJECT_UI="Pixelium.UI/Pixelium.UI.csproj"
OUTPUT_DIR="./builds"
VERSION=$(date +%Y%m%d-%H%M%S)

# Parse command line arguments
BUILD_MODE="all"
if [ "$1" == "quick" ]; then
    BUILD_MODE="quick"
    echo "📋 Quick build mode: Building for current platform only"
elif [ "$1" == "help" ] || [ "$1" == "-h" ] || [ "$1" == "--help" ]; then
    echo "Usage: ./build-all.sh [mode]"
    echo ""
    echo "Modes:"
    echo "  all     - Build for all platforms (default)"
    echo "  quick   - Build only for current platform"
    echo "  help    - Show this help message"
    echo ""
    exit 0
fi

# Clean previous builds
echo "🧹 Cleaning previous builds..."
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

# Create README for distribution
create_readme() {
    local output_path=$1
    local runtime=$2

    cat > "$output_path/README.txt" << 'EOF'
========================================
Pixelium Image Editor
========================================

A high-performance image editing application built with .NET 9.0
Features: Filters, Layers, Undo/Redo, LUT-based processing

========================================
Running the Application
========================================

LINUX:
  1. Extract the archive
  2. Make executable: chmod +x Pixelium.UI
  3. Run: ./Pixelium.UI

WINDOWS:
  1. Extract the archive
  2. Double-click Pixelium.UI.exe
  (You may need to allow it in Windows Defender)

MACOS:
  1. Extract the archive
  2. Make executable: chmod +x Pixelium.UI
  3. Run: ./Pixelium.UI
  (You may need to allow it in System Preferences > Security)

========================================
Features
========================================

IMAGE OPERATIONS:
  • Grayscale, Invert, Flip (Horizontal/Vertical)

ADJUSTMENTS:
  • Gamma Correction
  • Logarithmic Transform
  • Histogram Equalization

FILTERS:
  • Box Filter (Average Blur)
  • Gaussian Blur
  • Sobel Edge Detection
  • Laplace Edge Detection
  • Harris Corner Detection

LAYERS:
  • Multiple layer support
  • Layer visibility toggle
  • Layer opacity control
  • Merge layers
  • Non-destructive editing mode

KEYBOARD SHORTCUTS:
  Ctrl+N - New Project
  Ctrl+O - Open Image
  Ctrl+S - Save Image
  Ctrl+Z - Undo
  Ctrl+Y - Redo
  Ctrl+G - Grayscale
  Ctrl+I - Invert
  Ctrl+H - Flip Horizontal
  Ctrl+F - Fit to Screen
  Ctrl++ - Zoom In
  Ctrl+- - Zoom Out
  Ctrl+0 - Zoom 100%

========================================
System Requirements
========================================

LINUX (x64/ARM64):
  • Fedora 39+, Ubuntu 22.04+, or compatible
  • .NET runtime is self-contained (no installation needed)

WINDOWS (x64/ARM64):
  • Windows 10 version 1809+ or Windows 11
  • .NET runtime is self-contained (no installation needed)

MACOS (x64/ARM64):
  • macOS 11.0+ (Big Sur) for Intel
  • macOS 11.0+ for Apple Silicon (M1/M2/M3)
  • .NET runtime is self-contained (no installation needed)

========================================
License & Credits
========================================

Pixelium Image Editor
Built with .NET 9.0, Avalonia UI, and SkiaSharp

For more information, visit the project repository.

========================================
EOF

    # Add platform-specific info
    echo "" >> "$output_path/README.txt"
    echo "Build Information:" >> "$output_path/README.txt"
    echo "  Platform: $runtime" >> "$output_path/README.txt"
    echo "  Build Date: $(date '+%Y-%m-%d %H:%M:%S')" >> "$output_path/README.txt"
    echo "  Version: $VERSION" >> "$output_path/README.txt"
}

# Create README for Wine-compatible distribution
create_readme_wine() {
    local output_path=$1
    local runtime=$2

    cat > "$output_path/README.txt" << 'EOF'
========================================
Pixelium Image Editor (Wine-Compatible)
========================================

This is a WINE-COMPATIBLE Windows build with separate DLL files.

IMPORTANT: This build is designed for running under Wine on Linux/macOS.
If you're on actual Windows 10/11, use the standard single-file build instead.

A high-performance image editing application built with .NET 9.0
Features: Filters, Layers, Undo/Redo, LUT-based processing

========================================
Running Under Wine (Linux/macOS)
========================================

LINUX:
  1. Extract the archive
  2. Install Wine if not already installed:
     - Ubuntu/Debian: sudo apt install wine
     - Fedora: sudo dnf install wine
  3. Run: wine Pixelium.UI.exe

MACOS:
  1. Extract the archive
  2. Install Wine via Homebrew: brew install wine-stable
  3. Run: wine Pixelium.UI.exe

WINDOWS (Real Windows 10/11):
  Use the standard single-file build for better performance.
  This Wine-compatible build works but is not optimized for native Windows.

========================================
Why This Build Exists
========================================

The standard Windows build uses PublishSingleFile which embeds all DLLs
and native libraries (including SkiaSharp) into a single .exe file.

Wine has compatibility issues with this approach, particularly when
extracting and loading embedded native libraries at runtime.

This build keeps all DLLs separate, which Wine handles much better.

Trade-offs:
  ✅ Works under Wine without SkiaSharp DLL errors
  ✅ Easier to debug (can see all DLLs)
  ❌ More files to distribute (~50 DLL files)
  ❌ Slightly slower startup on native Windows

========================================
Features
========================================

IMAGE OPERATIONS:
  • Grayscale, Invert, Flip (Horizontal/Vertical)

ADJUSTMENTS:
  • Gamma Correction
  • Logarithmic Transform
  • Histogram Equalization

FILTERS:
  • Box Filter (Average Blur)
  • Gaussian Blur
  • Sobel Edge Detection
  • Laplace Edge Detection
  • Harris Corner Detection

LAYERS:
  • Multiple layer support
  • Layer visibility toggle
  • Layer opacity control
  • Merge layers
  • Non-destructive editing mode

KEYBOARD SHORTCUTS:
  Ctrl+N - New Project
  Ctrl+O - Open Image
  Ctrl+S - Save Image
  Ctrl+Z - Undo
  Ctrl+Y - Redo
  Ctrl+G - Grayscale
  Ctrl+I - Invert
  Ctrl+H - Flip Horizontal
  Ctrl+F - Fit to Screen
  Ctrl++ - Zoom In
  Ctrl+- - Zoom Out
  Ctrl+0 - Zoom 100%

========================================
System Requirements
========================================

WINE (Linux/macOS):
  • Wine 8.0+ recommended for .NET 9.0 support
  • Native Linux build recommended for best performance

WINDOWS (x64/ARM64):
  • Windows 10 version 1809+ or Windows 11
  • .NET runtime is self-contained (no installation needed)

========================================
License & Credits
========================================

Pixelium Image Editor
Built with .NET 9.0, Avalonia UI, and SkiaSharp

For more information, visit the project repository.

========================================
EOF

    # Add platform-specific info
    echo "" >> "$output_path/README.txt"
    echo "Build Information:" >> "$output_path/README.txt"
    echo "  Platform: $runtime (Wine-compatible)" >> "$output_path/README.txt"
    echo "  Build Type: Multi-file with separate DLLs" >> "$output_path/README.txt"
    echo "  Build Date: $(date '+%Y-%m-%d %H:%M:%S')" >> "$output_path/README.txt"
    echo "  Version: $VERSION" >> "$output_path/README.txt"
}

# Function to build for a specific runtime
build_runtime() {
    local runtime=$1
    local output_path="$OUTPUT_DIR/$runtime/Pixelium"

    echo ""
    echo "📦 Building Pixelium for $runtime (single-file)..."

    dotnet publish "$PROJECT_UI" \
        --runtime "$runtime" \
        --configuration Release \
        --self-contained true \
        --output "$output_path" \
        -p:PublishSingleFile=true \
        -p:IncludeNativeLibrariesForSelfExtract=true \
        -p:PublishTrimmed=false \
        -p:DebugType=None \
        -p:DebugSymbols=false

    if [ $? -eq 0 ]; then
        echo "✅ Successfully built Pixelium for $runtime (single-file)"

        # Create README
        create_readme "$output_path" "$runtime"

        # Create archive
        cd "$OUTPUT_DIR/$runtime"
        if [[ "$runtime" == win-* ]]; then
            zip -r "../Pixelium-${runtime}-${VERSION}.zip" .
        else
            tar -czf "../Pixelium-${runtime}-${VERSION}.tar.gz" .
        fi
        cd - > /dev/null

        echo "📦 Created archive: Pixelium-${runtime}-${VERSION}"

        # For Windows platforms, also create Wine-compatible build (multi-file with DLLs)
        if [[ "$runtime" == win-* ]]; then
            build_runtime_wine "$runtime"
        fi
    else
        echo "❌ Failed to build Pixelium for $runtime"
        return 1
    fi
}

# Function to build Wine-compatible version (multi-file with DLLs)
build_runtime_wine() {
    local runtime=$1
    local output_path="$OUTPUT_DIR/${runtime}-wine/Pixelium"

    echo ""
    echo "🍷 Building Pixelium for $runtime (Wine-compatible with DLLs)..."

    dotnet publish "$PROJECT_UI" \
        --runtime "$runtime" \
        --configuration Release \
        --self-contained true \
        --output "$output_path" \
        -p:PublishSingleFile=false \
        -p:PublishTrimmed=false \
        -p:DebugType=None \
        -p:DebugSymbols=false

    if [ $? -eq 0 ]; then
        echo "✅ Successfully built Pixelium for $runtime (Wine-compatible)"

        # Create README with Wine-specific info
        create_readme_wine "$output_path" "$runtime"

        # Create archive
        cd "$OUTPUT_DIR/${runtime}-wine"
        zip -r "../Pixelium-${runtime}-wine-${VERSION}.zip" .
        cd - > /dev/null

        echo "📦 Created Wine-compatible archive: Pixelium-${runtime}-wine-${VERSION}"
    else
        echo "❌ Failed to build Wine-compatible version for $runtime"
        return 1
    fi
}

# Detect current platform for quick build
detect_platform() {
    local os=$(uname -s)
    local arch=$(uname -m)

    case "$os" in
        Linux)
            case "$arch" in
                x86_64) echo "linux-x64" ;;
                aarch64) echo "linux-arm64" ;;
                *) echo "linux-x64" ;;
            esac
            ;;
        Darwin)
            case "$arch" in
                x86_64) echo "osx-x64" ;;
                arm64) echo "osx-arm64" ;;
                *) echo "osx-arm64" ;;
            esac
            ;;
        MINGW*|MSYS*|CYGWIN*)
            case "$arch" in
                x86_64) echo "win-x64" ;;
                aarch64) echo "win-arm64" ;;
                *) echo "win-x64" ;;
            esac
            ;;
        *)
            echo "linux-x64"
            ;;
    esac
}

echo ""
echo "=========================================="
if [ "$BUILD_MODE" == "quick" ]; then
    echo "Building Pixelium UI for Current Platform"
else
    echo "Building Pixelium UI for All Platforms"
fi
echo "=========================================="

if [ "$BUILD_MODE" == "quick" ]; then
    # Quick build: only current platform
    CURRENT_PLATFORM=$(detect_platform)
    echo "Detected platform: $CURRENT_PLATFORM"
    build_runtime "$CURRENT_PLATFORM"
else
    # Full build: all platforms
    build_runtime "linux-x64"
    build_runtime "linux-arm64"
    build_runtime "win-x64"
    build_runtime "win-arm64"
    build_runtime "osx-x64"
    build_runtime "osx-arm64"
fi

# Clean up temporary runtime directories
echo ""
echo "🧹 Cleaning up temporary directories..."
rm -rf "$OUTPUT_DIR"/linux-*
rm -rf "$OUTPUT_DIR"/win-*
rm -rf "$OUTPUT_DIR"/osx-*

# Calculate statistics
TOTAL_SIZE=0
ARCHIVE_COUNT=0

if command -v du &> /dev/null; then
    for file in "$OUTPUT_DIR"/*.zip "$OUTPUT_DIR"/*.tar.gz; do
        if [ -f "$file" ]; then
            SIZE=$(du -h "$file" | cut -f1)
            ARCHIVE_COUNT=$((ARCHIVE_COUNT + 1))
        fi
    done
fi

# Summary
echo ""
echo "=========================================="
echo "✅ BUILD COMPLETE!"
echo "=========================================="
echo ""
echo "📦 Release archives created in: $OUTPUT_DIR"
echo ""
if [ -d "$OUTPUT_DIR" ] && [ "$(ls -A $OUTPUT_DIR 2>/dev/null)" ]; then
    echo "Available builds:"
    echo ""
    for file in "$OUTPUT_DIR"/*.zip "$OUTPUT_DIR"/*.tar.gz; do
        if [ -f "$file" ]; then
            SIZE=$(du -h "$file" 2>/dev/null | cut -f1 || echo "?")
            echo "  📄 $(basename "$file") ($SIZE)"
        fi
    done
    echo ""
    echo "Total archives: $ARCHIVE_COUNT"
else
    echo "⚠️  No archives found"
fi
echo ""
echo "Build Information:"
echo "  Version: $VERSION"
echo "  Mode: $BUILD_MODE"
echo "  Output: $(pwd)/$OUTPUT_DIR"
echo ""
if [ "$BUILD_MODE" == "all" ]; then
    echo "Platform details:"
    echo "  • linux-x64: Linux 64-bit (Intel/AMD)"
    echo "  • linux-arm64: Linux ARM 64-bit (Raspberry Pi, ARM servers)"
    echo "  • win-x64: Windows 64-bit (Intel/AMD) - Single-file executable"
    echo "  • win-x64-wine: Windows 64-bit for Wine - Multi-file with DLLs"
    echo "  • win-arm64: Windows ARM 64-bit (Surface Pro X, Qualcomm) - Single-file"
    echo "  • win-arm64-wine: Windows ARM for Wine - Multi-file with DLLs"
    echo "  • osx-x64: macOS Intel (Intel Macs)"
    echo "  • osx-arm64: macOS Apple Silicon (M1/M2/M3/M4)"
    echo ""
    echo "Note: Wine-compatible builds include separate DLLs to work around"
    echo "      Wine's issues with single-file .NET executables containing"
    echo "      embedded native libraries (SkiaSharp)."
    echo ""
fi
echo "Distribution:"
echo "  Standard builds (Linux, macOS, Windows):"
echo "    - Pixelium.UI executable (self-contained, single-file)"
echo "    - README.txt with instructions"
echo "    - All required libraries embedded"
echo ""
echo "  Wine-compatible builds (win-*-wine):"
echo "    - Pixelium.UI.exe with separate DLLs (~50 files)"
echo "    - Works under Wine on Linux/macOS"
echo "    - README.txt with Wine-specific instructions"
echo ""
echo "Next steps:"
echo "  1. Test the build for your platform"
echo "  2. Distribute the archives to users"
echo "  3. Users just extract and run - no installation required!"
echo ""
