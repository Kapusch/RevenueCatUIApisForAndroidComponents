#!/usr/bin/env python3
"""Check that every purchases-ui runtime module reaches the app: shipped by this package, or provided by
a NuGet package in the consumer's restore graph (Xamarin bindings tag their Maven artifact).

Usage: verify-runtime-coverage.py [project.assets.json of a consumer, default: samples/Smoke]
Run the native build first (it writes build/runtime-artifacts.txt and build/native-modules.txt).
"""

from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / "src/Kapusch.RevenueCatUIApisForAndroidComponents/Native/Android/build"
# Modules that legitimately have no provider here:
EXPECTED_UNCOVERED = {
    # Shipped inside the core binding (AndroidMavenLibrary), which carries no artifact tag.
    "com.revenuecat.purchases:purchases",
    # Source-retention annotations, used at compile time only.
    "dev.drewhamilton.poko:poko-annotations",
    # Comes with BillingClient through the core binding's dependency graph, not with purchases-ui.
    "com.google.android.gms:play-services-places-placereport",
}


def module(group: str, artifact: str) -> str:
    return f"{group}:{re.sub(r'-(android|jvm)$', '', artifact)}"


def main() -> int:
    assets_path = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "samples/Smoke/obj/project.assets.json"
    runtime = {}
    for line in (BUILD / "runtime-artifacts.txt").read_text(encoding="utf-8").splitlines():
        group, artifact, version = line.split()[0].split(":")
        runtime[module(group, artifact)] = version
    shipped = {module(*m.split(":")) for m in (BUILD / "native-modules.txt").read_text(encoding="utf-8").split()}

    assets = json.loads(assets_path.read_text(encoding="utf-8"))
    packages_root = Path(assets["project"]["restore"]["packagesPath"])
    provided = {}
    for key, info in assets["libraries"].items():
        if info.get("type") != "package":
            continue
        name, version = key.split("/")
        nuspec = packages_root / name.lower() / version.lower() / f"{name.lower()}.nuspec"
        if nuspec.is_file():
            for group, artifact in re.findall(r"artifact=([\w.\-]+):([\w.\-]+)", nuspec.read_text(encoding="utf-8")):
                provided[module(group, artifact)] = key

    missing = sorted(m for m in runtime if m not in shipped and m not in provided and m not in EXPECTED_UNCOVERED)
    both = sorted(m for m in shipped if m in provided)
    errors = [f"no provider for {m} {runtime[m]}" for m in missing]
    errors += [f"{m} is shipped and also provided by {provided[m]} (duplicate classes)" for m in both]
    if errors:
        print("Runtime coverage check failed:\n  " + "\n  ".join(errors), file=sys.stderr)
        return 1
    print(f"Runtime coverage check passed ({len(runtime)} modules: {len(shipped)} shipped, the rest provided by NuGet)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
