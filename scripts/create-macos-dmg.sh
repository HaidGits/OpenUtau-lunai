#!/usr/bin/env bash
set -euo pipefail

ARCH_NAME="${1:?usage: create-macos-dmg.sh <arch-name> e.g. osx-x64}"

PUBLISH_DIR="bin/${ARCH_NAME}/publish"
APP="${PUBLISH_DIR}/OpenUtau-Lunai.app"
OUT="OpenUtau-Lunai-${ARCH_NAME}.dmg"

resolve_app_bundle() {
  if [[ -d "${APP}" ]]; then
    return 0
  fi

  # Legacy / alternate names from Dotnet.Bundle (project folder or CFBundleName/DisplayName).
  local candidate
  for candidate in \
      "${PUBLISH_DIR}/OpenUtau.app" \
      "${PUBLISH_DIR}/OpenUtau Lunai.app" \
      "${PUBLISH_DIR}/OpenUtau-Lunai.app"; do
    if [[ -d "${candidate}" ]]; then
      if [[ "${candidate}" != "${APP}" ]]; then
        mv "${candidate}" "${APP}"
      fi
      return 0
    fi
  done

  # Last resort: any single .app under publish.
  local apps=()
  while IFS= read -r -d '' candidate; do
    apps+=("${candidate}")
  done < <(find "${PUBLISH_DIR}" -maxdepth 1 -type d -name '*.app' -print0 2>/dev/null || true)

  if [[ ${#apps[@]} -eq 1 ]]; then
    mv "${apps[0]}" "${APP}"
    return 0
  fi

  return 1
}

if [[ ! -d "${PUBLISH_DIR}" ]]; then
  echo "Publish directory not found: ${PUBLISH_DIR}" >&2
  echo "Contents of bin/${ARCH_NAME}:" >&2
  ls -la "bin/${ARCH_NAME}" 2>/dev/null || true
  exit 1
fi

if ! resolve_app_bundle; then
  echo "App bundle not found under ${PUBLISH_DIR} (expected OpenUtau-Lunai.app)" >&2
  echo "Contents of ${PUBLISH_DIR}:" >&2
  ls -la "${PUBLISH_DIR}" 2>/dev/null || true
  exit 1
fi

PLIST="${APP}/Contents/Info.plist"

cp OpenUtau/Assets/OpenUtau.icns "${APP}/Contents/Resources/"

# create-dmg reads Info.plist as XML; Dotnet.Bundle may emit binary or BOM-prefixed XML.
plutil -convert xml1 "${PLIST}"
python3 -c "
from pathlib import Path
p = Path('${PLIST}')
data = p.read_bytes()
if data.startswith(b'\\xef\\xbb\\xbf'):
    p.write_bytes(data[3:])
"

rm -f "${OUT}" *.dmg
hdiutil detach "/Volumes/OpenUtau Lunai" -force 2>/dev/null || true
hdiutil detach "/Volumes/OpenUtau" -force 2>/dev/null || true

npm install -g create-dmg
create-dmg --no-code-sign "${APP}"
mv *.dmg "${OUT}"
# Ad-hoc sign when no Developer ID is available (e.g. GitHub Actions).
codesign -fvs - "${OUT}" || true
