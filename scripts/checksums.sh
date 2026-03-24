#!/usr/bin/env bash
# Compute SHA-256 checksums for all release archives.
# Usage: ./scripts/checksums.sh <dist-directory>
set -euo pipefail

DIST="${1:?Usage: checksums.sh <dist-directory>}"
SUMS="${DIST}/SHA256SUMS"

: > "${SUMS}"

(
  cd "${DIST}"
  for file in *.zip *.tar.gz; do
    [ -f "${file}" ] || continue
    sha256sum "${file}" >> SHA256SUMS
  done
)

echo "=== SHA256SUMS ==="
cat "${SUMS}"