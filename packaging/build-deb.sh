#!/usr/bin/env bash
#
# Builds a .deb package for makeBreak from the self-contained linux-x64 publish output.
# Usage: ./build-deb.sh [version]
set -euo pipefail

PROJECT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="$PROJECT_DIR/publish/linux-x64"
ICON="$PROJECT_DIR/Assets/ikona.png"

APP_NAME="makebreak"
DISPLAY_NAME="makeBreak"
VERSION="${1:-1.0.0}"
ARCH="amd64"
PKG_NAME="${APP_NAME}_${VERSION}_${ARCH}"
DEST="/opt/$APP_NAME"

if [[ ! -d "$PUBLISH_DIR" ]] || [[ ! -f "$PUBLISH_DIR/makeBreak" ]]; then
  echo "ERROR: publish output not found. Run: dotnet publish -c Release -r linux-x64 --self-contained true first."
  exit 1
fi
if [[ ! -f "$ICON" ]]; then
  echo "ERROR: icon not found at $ICON"
  exit 1
fi

# ---- Build directory skeleton ----
BUILD_DIR="/tmp/makebreak-deb/${PKG_NAME}"
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR/DEBIAN"
mkdir -p "$BUILD_DIR$DEST"
mkdir -p "$BUILD_DIR/usr/share/applications"
mkdir -p "$BUILD_DIR/usr/share/pixmaps"

# ---- Control file ----
SIZE_KB="$(du -sk "$PUBLISH_DIR" | cut -f1)"
cat > "$BUILD_DIR/DEBIAN/control" <<EOF
Package: ${APP_NAME}
Version: ${VERSION}
Section: utils
Priority: optional
Architecture: ${ARCH}
Installed-Size: ${SIZE_KB}
Maintainer: makeBreak <dev@makebreak.local>
Description: makeBreak - break time organizer for the desktop
 Organizes and enforces break times while working at the computer.
EOF

cat > "$BUILD_DIR/DEBIAN/postinst" <<EOF
#!/bin/sh
set -e
chmod +x ${DEST}/makeBreak
update-desktop-database /usr/share/applications >/dev/null 2>&1 || true
exit 0
EOF
chmod 755 "$BUILD_DIR/DEBIAN/postinst"

# ---- Application files ----
cp -a "$PUBLISH_DIR/." "$BUILD_DIR$DEST/"

# ---- Desktop entry ----
cat > "$BUILD_DIR/usr/share/applications/${APP_NAME}.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=${DISPLAY_NAME}
Comment=Organizes and enforces break times while working at the computer
Exec=${DEST}/makeBreak
Icon=${APP_NAME}
Terminal=false
Categories=Utility;Office;
Keywords=break;rest;timer;
EOF

# ---- Icon ----
cp "$ICON" "$BUILD_DIR/usr/share/pixmaps/${APP_NAME}.png"

# ---- Build .deb ----
DEB_PATH="$PROJECT_DIR/packaging/${PKG_NAME}.deb"
mkdir -p "$PROJECT_DIR/packaging"
rm -f "$DEB_PATH"
dpkg-deb --build --root-owner-group "$BUILD_DIR" "$DEB_PATH"
rm -rf "$BUILD_DIR"

echo "Done: $DEB_PATH"