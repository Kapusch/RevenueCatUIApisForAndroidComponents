#!/usr/bin/env python3
"""Check the packed nupkg layout, its dependencies and the JNI contract with the facade AAR."""

from __future__ import annotations

import json
import re
import subprocess
import sys
import tempfile
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
PROJECT = ROOT / "src/Kapusch.RevenueCatUIApisForAndroidComponents"
PACKAGE_ID = "Kapusch.RevenueCatUI.Android"
FACADE_CLASS = "com/kapusch/revenuecatui/androidinterop/PaywallInterop"


def main() -> int:
    packages = sorted((ROOT / "artifacts/nuget").glob(f"{PACKAGE_ID}.*.nupkg"))
    if not packages:
        sys.exit("No package in artifacts/nuget; run dotnet pack first.")
    package = packages[-1]
    lock = json.loads((ROOT / "DependencyLocks/native-artifacts.json").read_text(encoding="utf-8"))
    errors = []
    with zipfile.ZipFile(package) as archive:
        names = set(archive.namelist())
        required = [
            "buildTransitive/Kapusch.RevenueCatUI.Android.targets",
            "native/aar/krcui-release.aar",
            "THIRD_PARTY_NOTICES.md",
            "licenses/purchases-android/LICENSE",
            "licenses/apache-2.0/LICENSE",
            "licenses/commonmark-java/LICENSE",
            "nuget-readme.md",
        ] + [f"native/deps/{name}" for name in lock]
        errors += [f"missing {name}" for name in required if name not in names]
        extra = sorted(n for n in names if n.startswith("native/deps/") and n[len("native/deps/"):] not in lock)
        errors += [f"unpinned native file {name}" for name in extra]
        if not any(re.fullmatch(r"lib/net10\.0-android[\d.]*/Kapusch\.RevenueCatUI\.Android\.dll", n) for n in names):
            errors.append("missing lib/net10.0-android*/Kapusch.RevenueCatUI.Android.dll")
        nuspec = archive.read(next(n for n in names if n.endswith(".nuspec"))).decode("utf-8")
        if not re.search(r'id="Kebechet\.Maui\.RevenueCat\.Android" version="\[[^"]+\)"', nuspec):
            errors.append("nuspec must depend on a bounded Kebechet.Maui.RevenueCat.Android range")

        # Every JNI method the managed code looks up must exist with the same signature.
        with tempfile.TemporaryDirectory() as temp:
            archive.extract("native/aar/krcui-release.aar", temp)
            with zipfile.ZipFile(Path(temp) / "native/aar/krcui-release.aar") as aar:
                aar.extract("classes.jar", temp)
            javap = subprocess.check_output(
                ["javap", "-s", "-cp", str(Path(temp) / "classes.jar"), FACADE_CLASS.replace("/", ".")], text=True
            )
    sources = "\n".join(p.read_text(encoding="utf-8") for p in PROJECT.rglob("*.cs") if "/obj/" not in str(p))
    lookups = re.findall(r'(?:GetStaticMethodID\(\s*Class,\s*|CallBoolean\(\s*)"(\w+)",\s*"([^"]+)"', sources)
    declared = set(re.findall(r"public static final \S+ (\w+)\([^)]*\);\n\s+descriptor: (\S+)", javap))
    for name, signature in lookups:
        if (name, signature) not in declared:
            errors.append(f"JNI method {name}{signature} is not a public static method of {FACADE_CLASS}")
    if not lookups:
        errors.append("no JNI lookups found in the managed sources")

    csproj = (PROJECT / "Kapusch.RevenueCatUIApisForAndroidComponents.csproj").read_text(encoding="utf-8")
    project_version = re.search(r"<Version>([^<]+)</Version>", csproj).group(1)
    sample = (ROOT / "samples/Smoke/Smoke.csproj").read_text(encoding="utf-8")
    if f'Include="{PACKAGE_ID}" Version="{project_version}"' not in sample:
        errors.append(f"samples/Smoke/Smoke.csproj must reference {PACKAGE_ID} {project_version}")

    if errors:
        print(f"Package check failed for {package.name}:\n  " + "\n  ".join(errors), file=sys.stderr)
        return 1
    print(f"Package check passed ({package.name}, {len(lookups)} JNI methods, {len(lock)} pinned native files)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
