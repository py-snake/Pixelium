#!/bin/bash

# Script to convert and add application icon to Pixelium
# Usage: ./add-icon.sh /path/to/your/icon.jpg

set -e

if [ -z "$1" ]; then
    echo "Usage: ./add-icon.sh /path/to/your/icon.jpg"
    echo ""
    echo "Converts a JPG/PNG image to application icons and configures the project."
    echo "Input image should be square (recommended 1024x1024 or 512x512)"
    exit 1
fi

INPUT_IMAGE="$1"
ASSETS_DIR="Pixelium.UI/Assets"

if [ ! -f "$INPUT_IMAGE" ]; then
    echo "Error: Input image '$INPUT_IMAGE' not found!"
    exit 1
fi

echo "================================================"
echo "Pixelium Icon Setup"
echo "================================================"
echo ""
echo "Input image: $INPUT_IMAGE"
echo "Output directory: $ASSETS_DIR"
echo ""

# Check image dimensions
echo "Checking image dimensions..."
DIMENSIONS=$(identify -format "%wx%h" "$INPUT_IMAGE" 2>/dev/null || echo "unknown")
echo "Image size: $DIMENSIONS"
echo ""

# Create assets directory if it doesn't exist
mkdir -p "$ASSETS_DIR"

echo "Converting to PNG formats..."

# Create PNG in multiple sizes for different uses
echo "  - Creating 256x256 PNG (window icon)..."
magick "$INPUT_IMAGE" -resize 256x256 "$ASSETS_DIR/icon-256.png"

echo "  - Creating 128x128 PNG..."
magick "$INPUT_IMAGE" -resize 128x128 "$ASSETS_DIR/icon-128.png"

echo "  - Creating 64x64 PNG..."
magick "$INPUT_IMAGE" -resize 64x64 "$ASSETS_DIR/icon-64.png"

echo "  - Creating 48x48 PNG..."
magick "$INPUT_IMAGE" -resize 48x48 "$ASSETS_DIR/icon-48.png"

echo "  - Creating 32x32 PNG..."
magick "$INPUT_IMAGE" -resize 32x32 "$ASSETS_DIR/icon-32.png"

echo "  - Creating 16x16 PNG..."
magick "$INPUT_IMAGE" -resize 16x16 "$ASSETS_DIR/icon-16.png"

echo ""
echo "Creating ICO file (Windows executable icon)..."

# Create multi-resolution ICO file for Windows
magick "$INPUT_IMAGE" \
    \( -clone 0 -resize 256x256 \) \
    \( -clone 0 -resize 128x128 \) \
    \( -clone 0 -resize 64x64 \) \
    \( -clone 0 -resize 48x48 \) \
    \( -clone 0 -resize 32x32 \) \
    \( -clone 0 -resize 16x16 \) \
    -delete 0 -colors 256 "$ASSETS_DIR/icon.ico"

echo ""
echo "✅ Icon files created successfully:"
ls -lh "$ASSETS_DIR"/icon* | awk '{print "   " $9 " (" $5 ")"}'

echo ""
echo "================================================"
echo "Icon Setup Complete!"
echo "================================================"
echo ""
echo "Created files:"
echo "  • icon.ico - Windows executable icon (multi-resolution)"
echo "  • icon-*.png - PNG icons in various sizes"
echo ""
echo "The project configuration will be updated to use these icons."
echo ""
