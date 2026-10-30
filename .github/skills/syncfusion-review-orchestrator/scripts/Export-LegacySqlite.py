#!/usr/bin/env python3

import argparse
import gc
import hashlib
import json
import pathlib
import re
import shutil
import sqlite3
import tempfile


def file_hash(path: pathlib.Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()

def source_hashes(database: pathlib.Path) -> dict[str, str | None]:
    paths = {
        "database": database,
        "wal": pathlib.Path(f"{database}-wal"),
        "shm": pathlib.Path(f"{database}-shm"),
    }
    return {
        name: file_hash(path) if path.exists() else None
        for name, path in paths.items()
    }


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Export named legacy SQLite tables without modifying the source database."
    )
    parser.add_argument("database", type=pathlib.Path)
    parser.add_argument("output", type=pathlib.Path)
    parser.add_argument("--table", action="append", required=True)
    args = parser.parse_args()

    for table in args.table:
        if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", table):
            raise SystemExit(f"Invalid table name: {table}")

    before = source_hashes(args.database)
    exported: dict[str, list[dict[str, object]]] = {}
    with tempfile.TemporaryDirectory(prefix="sfro-sqlite-") as temporary:
        snapshot_root = pathlib.Path(temporary)
        copied_database = snapshot_root / args.database.name
        for suffix in ("", "-wal", "-shm"):
            source_path = pathlib.Path(f"{args.database}{suffix}")
            if source_path.exists():
                shutil.copy2(source_path, pathlib.Path(f"{copied_database}{suffix}"))

        if before != source_hashes(args.database):
            raise SystemExit("The SQLite source changed while its snapshot was being copied.")

        uri = f"{copied_database.resolve().as_uri()}?mode=ro"
        source = sqlite3.connect(uri, uri=True)
        snapshot = sqlite3.connect(":memory:")
        try:
            source.backup(snapshot)
            source.close()
            snapshot.row_factory = sqlite3.Row
            for table in args.table:
                rows = snapshot.execute(f'SELECT * FROM "{table}"').fetchall()
                exported[table] = [dict(row) for row in rows]
        finally:
            source.close()
            snapshot.close()
            del source
            del snapshot
            gc.collect()

    if before != source_hashes(args.database):
        raise SystemExit("The SQLite source changed while it was being exported.")

    payload = {
        "schemaVersion": 1,
        "source": str(args.database.resolve()),
        "sourceHashes": before,
        "tables": exported,
    }
    args.output.write_text(
        json.dumps(payload, sort_keys=True, separators=(",", ":")) + "\n",
        encoding="utf-8",
        newline="\n",
    )


if __name__ == "__main__":
    main()
