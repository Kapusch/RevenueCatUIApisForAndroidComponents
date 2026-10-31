#!/usr/bin/env bash
# Builds the JNI facade AAR and copies the pinned native dependencies that have no managed
# .NET binding into build/. RevenueCat core, Compose, AndroidX and Kotlin come from NuGet.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO="$(cd "$ROOT/../../../.." && pwd)"
mkdir -p "$REPO/artifacts/nuget"
SDK="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-$HOME/Library/Android/sdk}}"
[[ -d "$SDK" ]] || { echo 'Android SDK is required' >&2; exit 1; }
export GRADLE_USER_HOME="${GRADLE_USER_HOME:-$ROOT/.gradle/user-home}"
rm -rf "$ROOT/build"
mkdir -p "$ROOT/.gradle" "$ROOT/build/aar" "$ROOT/build/deps"
printf 'sdk.dir=%s\n' "$SDK" > "$ROOT/local.properties"
GRADLE="$ROOT/.gradle/gradle-8.11.1/bin/gradle"
if [[ ! -x "$GRADLE" ]]; then
  curl -fsSL https://services.gradle.org/distributions/gradle-8.11.1-bin.zip -o "$ROOT/.gradle/gradle.zip"
  echo 'f397b287023acdba1e9f6fc5ea72d22dd63669d59ed4a289a29b1a76eee151c6  '"$ROOT/.gradle/gradle.zip" | shasum -a 256 -c -
  unzip -q "$ROOT/.gradle/gradle.zip" -d "$ROOT/.gradle"
fi
"$GRADLE" --no-daemon -p "$ROOT" :revenuecatuiinterop:assembleRelease :revenuecatuiinterop:copyNativeDependencies :revenuecatuiinterop:listRuntimeArtifacts -q
cp "$ROOT/revenuecatuiinterop/build/outputs/aar/revenuecatuiinterop-release.aar" "$ROOT/build/aar/krcui-release.aar"
python3 "$REPO/scripts/native-integrity.py" "$ROOT/build/deps" "$REPO/DependencyLocks/native-artifacts.json"
