#!/usr/bin/env bash
# Build a self-contained single-file binary for a target platform.
# Usage: ./scripts/build.sh <RID> <VERSION>
set -euo pipefail

RID="${1:?Usage: build.sh <runtime-id> <version>}"
VERSION="${2:?Usage: build.sh <runtime-id> <version>}"
PROJECT="CLI/CLI.csproj"
OUTPUT="artifacts/${RID}"

echo "=== Building adb-installer ${VERSION} for ${RID} ==="

# # Best-effort icon generation from Logo.svg for all target platforms
# PYTHON_CMD=""
# if command -v python3 >/dev/null 2>&1; then
#   PYTHON_CMD="python3"
# elif command -v python >/dev/null 2>&1; then
#   PYTHON_CMD="python"
# fi

# if [ -n "${PYTHON_CMD}" ]; then
#   echo "=== Generating icons from Logo.svg ==="
#   if ! ${PYTHON_CMD} scripts/generate_icons.py --input Logo.svg --output CLI/Assets/icons; then
#     echo "WARNING: icon generation failed; continuing build without updated icons." >&2
#   fi
# else
#   echo "WARNING: Python not found; skipping icon generation." >&2
# fi

dotnet publish "${PROJECT}" \
  --configuration Release \
  --runtime "${RID}" \
  --self-contained true \
  --no-restore \
  -p:PublishSingleFile=true \
  -p:DebugType=embedded \
  -p:Deterministic=true \
  -p:ContinuousIntegrationBuild=true \
  -p:Version="${VERSION}" \
  -p:InformationalVersion="${VERSION}+$(git rev-parse --short HEAD)" \
  --output "${OUTPUT}"

echo "=== Output ==="
ls -lh "${OUTPUT}/"