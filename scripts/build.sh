#!/usr/bin/env bash
# Build a self-contained single-file binary for a target platform.
# Usage: ./scripts/build.sh <RID> <VERSION>
set -euo pipefail

RID="${1:?Usage: build.sh <runtime-id> <version>}"
VERSION="${2:?Usage: build.sh <runtime-id> <version>}"
PROJECT="CLI/CLI.csproj"
OUTPUT="artifacts/${RID}"

echo "=== Building adb-installer ${VERSION} for ${RID} ==="

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