#!/usr/bin/env bash
# Create a distributable archive from build artifacts.
# Usage: ./scripts/package.sh <RID> <VERSION>
set -euo pipefail

RID="${1:?Usage: package.sh <runtime-id> <version>}"
VERSION="${2:?Usage: package.sh <runtime-id> <version>}"
SRC="artifacts/${RID}"
NAME="adb-installer-${VERSION}-${RID}"

mkdir -p dist
DIST_ABS="$(cd dist && pwd)"

case "${RID}" in
  win-*)
    ARCHIVE="${DIST_ABS}/${NAME}.zip"
    if command -v zip >/dev/null 2>&1; then
      (cd "${SRC}" && zip -r "${ARCHIVE}" .)
    elif command -v 7z >/dev/null 2>&1; then
      (cd "${SRC}" && 7z a -tzip "${ARCHIVE}" .)
    else
      echo "ERROR: Neither zip nor 7z found on PATH." >&2; exit 1
    fi
    ;;
  *)
    ARCHIVE="${DIST_ABS}/${NAME}.tar.gz"
    tar -czf "${ARCHIVE}" -C "${SRC}" .
    ;;
esac

# Copy buildinfo alongside archive for easy inspection without extracting
cp "${SRC}/buildinfo.json" "dist/${NAME}.buildinfo.json"

echo "Packaged: ${ARCHIVE}"