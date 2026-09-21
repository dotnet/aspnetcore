"""Prepare isolated Vally inputs; verify state without implementing guidance loading."""

import hashlib
import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import sys


ROOT = Path.cwd()
FIXTURES = ROOT / ".eval"
CROSS = ROOT / "docs/CrossCuttingGuidance.md"
BLAZOR = ROOT / "docs/BlazorComponentsGuidance.md"
POLICY = ROOT / "docs/Review Policy.md"


def git(*arguments):
    return subprocess.check_output(
        ["git", "-c", "core.hooksPath=/dev/null", *arguments],
        cwd=ROOT, text=True, stderr=subprocess.PIPE,
    ).strip()


def write(path, text):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(text, encoding="utf-8")


def snapshot():
    files = {}
    for path in sorted(ROOT.rglob("*")):
        if path.relative_to(ROOT).parts[0] in (".git", ".eval"):
            continue
        if path.is_symlink():
            files[str(path.relative_to(ROOT))] = {"link": os.readlink(path)}
        elif path.is_file():
            mode = stat.S_IMODE(path.stat().st_mode)
            digest = hashlib.sha256(path.read_bytes()).hexdigest() if mode & 0o444 else "unreadable"
            files[str(path.relative_to(ROOT))] = {"mode": mode, "sha256": digest}
    return {"branch": git("symbolic-ref", "HEAD"), "head": git("rev-parse", "HEAD"), "files": files}


def prepare(scenario):
    for source, destination in (
        ("CrossCuttingGuidance.md", CROSS),
        ("BlazorComponentsGuidance.md", BLAZOR),
        ("Policy.md", POLICY),
    ):
        write(destination, (FIXTURES / source).read_text(encoding="utf-8"))
    write(ROOT / "src/Components/Rules.md", "## Component contract\n\n- Keep COMPONENT-POLICY.\n")
    write(ROOT / "src/Example.cs", "// Local product code is not PR evidence.\n")
    write(ROOT / ".gitignore", ".eval/\n")
    (ROOT / "work/nested").mkdir(parents=True)
    (FIXTURES / "gh").chmod(0o755)

    if scenario == "historical":
        shutil.copytree(FIXTURES / "real", ROOT, dirs_exist_ok=True)
        target = json.loads((FIXTURES / "pr69401.json").read_text(encoding="utf-8"))
    else:
        path = "src/JSInterop/Example.cs" if scenario == "jsinterop" else "src/Example.cs"
        if scenario == "components":
            path = "src/Components/Example.cs"
        target = {
            "boundary": "Synthetic routing projection for Step 2 only.",
            "pr": "dotnet/aspnetcore#1", "head": "1" * 40,
            "base_repository": "dotnet/aspnetcore", "base_ref": "main",
            "base": "2" * 40, "files": [path],
        }
    write(FIXTURES / "target.json", json.dumps(target, indent=2))

    git("init", "-b", "eval-review")
    git("add", ".")
    git("-c", "user.name=Skill eval", "-c", "user.email=skill-eval@example.invalid",
        "-c", "commit.gpgsign=false", "commit", "-qm", "Prepare fixture")

    if scenario == "dirty":
        write(CROSS, CROSS.read_text(encoding="utf-8").replace("WORKSPACE-CRITERION", "UNCOMMITTED-CRITERION"))
        write(ROOT / "src/Example.cs", "// UNCOMMITTED-PRODUCT must not define PR scope.\n")
    elif scenario == "missing-guide":
        CROSS.unlink()
    elif scenario == "empty-guide":
        write(CROSS, "")
    elif scenario == "malformed-guide":
        write(CROSS, CROSS.read_text(encoding="utf-8") + "\n## Topics\n\n### Duplicate section\n\n- Invalid.\n")
    elif scenario == "unreadable-guide":
        CROSS.chmod(0)
        if os.access(CROSS, os.R_OK):
            raise RuntimeError("This user can still read mode-000 files; unreadable fixture is unavailable.")
    elif scenario == "missing-policy":
        POLICY.unlink()
    elif scenario == "empty-policy":
        write(POLICY, "")
    elif scenario == "missing-anchor":
        write(POLICY, "## Different heading\n\n- No delegated anchor exists.\n")
    elif scenario == "ambiguous-clauses":
        write(POLICY, "## Capture & reuse\n\nSee the appropriate policy elsewhere.\n\n## Repeated\n\n## Repeated\n")
    elif scenario == "out-of-root":
        write(CROSS, CROSS.read_text(encoding="utf-8").replace(
            "Review%20Policy.md#capture--reuse", "../../outside-policy.md#capture--reuse"))
    elif scenario == "jsinterop":
        (ROOT / "src/Components/Rules.md").unlink()
    elif scenario not in ("main", "components", "historical"):
        raise ValueError(f"Unknown fixture scenario: {scenario}")
    write(FIXTURES / "before.json", json.dumps(snapshot(), sort_keys=True))


def verify():
    before = json.loads((FIXTURES / "before.json").read_text(encoding="utf-8"))
    if snapshot() != before:
        raise AssertionError("The reviewer changed the branch, HEAD, or fixture files.")
    requests = FIXTURES / "requests.jsonl"
    if requests.exists() and requests.read_text(encoding="utf-8").strip():
        raise AssertionError("The reviewer requested remote guidance.")
    print("Workspace unchanged; no remote guidance requests.")


if __name__ == "__main__":
    if sys.argv[1] == "verify":
        verify()
    else:
        prepare(sys.argv[1])
