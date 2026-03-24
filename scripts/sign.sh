#!/usr/bin/env bash
# GPG-sign the SHA256SUMS file (detached armored signature).
# Expects: GPG_PRIVATE_KEY and GPG_PASSPHRASE env vars.
# Usage: ./scripts/sign.sh <dist-directory>
set -euo pipefail

DIST="${1:?Usage: sign.sh <dist-directory>}"
SUMS="${DIST}/SHA256SUMS"

if [ ! -f "${SUMS}" ]; then
  echo "ERROR: ${SUMS} not found — run checksums.sh first." >&2
  exit 1
fi
if [ -z "${GPG_PRIVATE_KEY:-}" ]; then
  echo "ERROR: GPG_PRIVATE_KEY env var is not set." >&2
  exit 1
fi

# Temporary isolated keyring — cleaned up on exit
export GNUPGHOME="$(mktemp -d)"
trap 'rm -rf "${GNUPGHOME}"' EXIT

echo "${GPG_PRIVATE_KEY}" | gpg --batch --import 2>/dev/null

KEY_ID="$(gpg --list-secret-keys --keyid-format long 2>/dev/null \
  | grep '^sec' | head -1 | awk '{print $2}' | cut -d'/' -f2)"

if [ -z "${KEY_ID}" ]; then
  echo "ERROR: Could not determine GPG key ID after import." >&2
  exit 1
fi

gpg --batch --yes \
  --pinentry-mode loopback \
  --passphrase "${GPG_PASSPHRASE:-}" \
  --local-user "${KEY_ID}" \
  --detach-sign --armor \
  "${SUMS}"

echo "Signed: ${SUMS}.asc"
gpg --verify "${SUMS}.asc" "${SUMS}"