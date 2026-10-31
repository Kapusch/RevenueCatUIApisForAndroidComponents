#!/usr/bin/env python3
"""Check that purchases-ui, the core binding range and the docs name the same RevenueCat release."""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src/Kapusch.RevenueCatUIApisForAndroidComponents"


def main() -> int:
    gradle = (PROJECT / "Native/Android/revenuecatuiinterop/build.gradle.kts").read_text(encoding="utf-8")
    match = re.search(r'implementation\("com\.revenuecat\.purchases:purchases-ui:(\d+)\.(\d+)\.(\d+)"\)', gradle)
    if not match:
        sys.exit("purchases-ui dependency not found in build.gradle.kts")
    major, minor, patch = (int(part) for part in match.groups())
    version = f"{major}.{minor}.{patch}"
    errors = []

    csproj = (PROJECT / "Kapusch.RevenueCatUIApisForAndroidComponents.csproj").read_text(encoding="utf-8")
    expected_range = f"[{version},{major}.{minor}.{patch + 1})"
    found = re.search(r'Include="Kebechet\.Maui\.RevenueCat\.Android" Version="([^"]+)"', csproj)
    if not found or found.group(1) != expected_range:
        errors.append(f"core binding range must be {expected_range}, found {found.group(1) if found else 'none'}")
    if f"purchases-ui-{version}.aar" not in csproj:
        errors.append(f"ValidateNativeAssets must check purchases-ui-{version}.aar")
    if f"purchases-ui {version}" not in csproj:
        errors.append(f"PackageReleaseNotes does not mention purchases-ui {version}")

    targets = (PROJECT / "buildTransitive/Kapusch.RevenueCatUI.Android.targets").read_text(encoding="utf-8")
    if f"purchases-ui-{version}.aar" not in targets:
        errors.append(f"buildTransitive targets must check purchases-ui-{version}.aar")

    lock = json.loads((ROOT / "DependencyLocks/native-artifacts.json").read_text(encoding="utf-8"))
    if f"purchases-ui-{version}.aar" not in lock:
        errors.append(f"native-artifacts.json does not pin purchases-ui-{version}.aar")
    if any(name.startswith("purchases-") and not name.startswith("purchases-ui-") for name in lock):
        errors.append("the core purchases AAR must come from the core binding, not this package")

    for name in ("THIRD_PARTY_NOTICES.md", "README.md", "CHANGELOG.md", "src/Kapusch.RevenueCatUIApisForAndroidComponents/nuget-readme.md"):
        if version not in (ROOT / name).read_text(encoding="utf-8"):
            errors.append(f"{name} does not mention purchases-ui {version}")

    if errors:
        print("Dependency lock check failed:\n  " + "\n  ".join(errors), file=sys.stderr)
        return 1
    print(f"Dependency lock check passed (purchases-ui {version}, {len(lock)} pinned native files)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
