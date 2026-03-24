#!/usr/bin/env bash
# Generate build provenance metadata (buildinfo.json).
# Usage: ./scripts/buildinfo.sh <RID> <VERSION>
set -euo pipefail

RID="${1:?Usage: buildinfo.sh <runtime-id> <version>}"
VERSION="${2:?Usage: buildinfo.sh <runtime-id> <version>}"
PROJECT="CLI/CLI.csproj"
OUTPUT="artifacts/${RID}/buildinfo.json"

mkdir -p "artifacts/${RID}"

# ── Git ──
git_commit="$(git rev-parse HEAD)"
git_tree_state="clean"
if [ -n "$(git status --porcelain 2>/dev/null || true)" ]; then
  git_tree_state="dirty"
fi

# ── Compiler & timestamps ──
compiler_version="$(dotnet --version)"
build_timestamp="$(date -u '+%Y-%m-%dT%H:%M:%SZ')"
source_date_epoch="$(git log -1 --format=%ct HEAD)"

build_command="dotnet publish ${PROJECT} -c Release -r ${RID} --self-contained -p:PublishSingleFile=true -p:Version=${VERSION}"

# ── Dependencies ──
nuget_cache="${NUGET_PACKAGES:-${HOME}/.nuget/packages}"
DEPS_JSON="[]"

pkg_json="$(dotnet list "${PROJECT}" package --include-transitive --format json 2>/dev/null || echo '{}')"

if command -v jq >/dev/null 2>&1 && [ "${pkg_json}" != '{}' ]; then
  while IFS=$'\t' read -r pkg_id pkg_ver; do
    [ -z "${pkg_id}" ] && continue

    # Try to find NuGet's stored SHA-512 hash
    hash=""
    pkg_lower="$(printf '%s' "${pkg_id}" | tr '[:upper:]' '[:lower:]')"
    sha_file="${nuget_cache}/${pkg_lower}/${pkg_ver}/${pkg_lower}.${pkg_ver}.nupkg.sha512"
    if [ -f "${sha_file}" ]; then
      hash="sha512:$(tr -d '\n\r' < "${sha_file}")"
    fi

    DEPS_JSON="$(printf '%s' "${DEPS_JSON}" | jq \
      --arg n "${pkg_id}" --arg v "${pkg_ver}" --arg h "${hash}" \
      '. + [{"name":$n,"version":$v,"hash":$h}]')"
  done < <(printf '%s' "${pkg_json}" | jq -r '
    [.projects[]?.frameworks[]? |
     ((.topLevelPackages // []) + (.transitivePackages // []))[] |
     {id: .id, ver: .resolvedVersion}] |
    unique_by(.id) | sort_by(.id)[] |
    "\(.id)\t\(.ver)"
  ' 2>/dev/null)
fi

# ── Write JSON ──
jq -n \
  --arg gc  "${git_commit}" \
  --arg gts "${git_tree_state}" \
  --arg bc  "${build_command}" \
  --arg cv  "${compiler_version}" \
  --arg bt  "${build_timestamp}" \
  --arg sde "${source_date_epoch}" \
  --argjson deps "${DEPS_JSON}" \
  '{
    git_commit:        $gc,
    git_tree_state:    $gts,
    build_command:     $bc,
    compiler_version:  $cv,
    build_timestamp:   $bt,
    source_date_epoch: ($sde | tonumber),
    dependencies:      $deps
  }' > "${OUTPUT}"

echo "Generated: ${OUTPUT}"