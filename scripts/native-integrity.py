#!/usr/bin/env python3
"""Check the shipped native dependencies against DependencyLocks/native-artifacts.json.

Usage: native-integrity.py <deps folder> <lock file>
The first run on a new dependency set writes the lock; commit it after reviewing the file list.
"""

import hashlib
import json
import sys
from pathlib import Path

folder, lock = map(Path, sys.argv[1:])
files = {p.name: hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(folder.iterdir()) if p.is_file()}
if not files:
    raise SystemExit("No native dependencies were copied")
if lock.exists():
    expected = json.loads(lock.read_text())
    if expected != files:
        added = sorted(set(files) - set(expected))
        removed = sorted(set(expected) - set(files))
        changed = sorted(n for n in set(files) & set(expected) if files[n] != expected[n])
        raise SystemExit(f"Native dependency integrity mismatch: added={added} removed={removed} changed={changed}")
else:
    lock.parent.mkdir(parents=True, exist_ok=True)
    lock.write_text(json.dumps(files, indent=2) + "\n")
    print(f"Wrote {lock} for {len(files)} files; review and commit it")
print(f"Native artifact integrity verified ({len(files)} files)")
