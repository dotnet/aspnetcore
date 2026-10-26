import hashlib
import json
from pathlib import Path, PurePosixPath
import re
import sys


def _relative(value):
    if not isinstance(value, str) or not value or any(
        character in "\\:" or ord(character) < 32 or ord(character) == 127 for character in value
    ):
        raise ValueError("Invalid criteria path.")
    path = PurePosixPath(value)
    if path.is_absolute() or not path.parts or ".." in path.parts or path.as_posix() != value:
        raise ValueError("Criteria paths must be canonical and relative.")
    return path


def _contained_file(root, relative):
    current = root
    for part in _relative(relative).parts:
        current = current / part
        if current.is_symlink() or current.resolve(strict=True) != current.absolute():
            raise ValueError("Criteria paths must not contain links.")
    if not current.is_file() or not current.resolve().is_relative_to(root):
        raise ValueError("Criteria file is outside its root.")
    return current


def materialize(manifest_path, workspace, expected_head, expected_guidance):
    if any(not isinstance(value, str) or re.fullmatch("[a-f0-9]{40}", value) is None
           for value in (expected_head, expected_guidance)):
        raise ValueError("Expected criteria revisions must be immutable commit SHAs.")
    manifest_path = Path(manifest_path)
    if manifest_path.is_symlink():
        raise ValueError("The criteria manifest must not be a link.")
    manifest_bytes = manifest_path.read_bytes()
    manifest = json.loads(manifest_bytes.decode("utf-8"))
    if not isinstance(manifest, dict) or type(manifest.get("version")) is not int \
            or manifest["version"] != 2 or manifest.get("ready") is not True:
        raise ValueError("Criteria require a ready version-2 manifest.")
    target = manifest.get("target")
    guidance = manifest.get("guidance")
    if not isinstance(target, dict) or target.get("head") != expected_head \
            or not isinstance(guidance, dict) or guidance.get("commit") != expected_guidance:
        raise ValueError("The criteria revisions do not match the frozen invocation.")
    if guidance.get("root") != "guidance" or manifest.get("suffix") != ".source":
        raise ValueError("The frozen criteria layout is unsupported.")
    guides = manifest.get("guides")
    policies = manifest.get("policies")
    exclusions = manifest.get("exclusions")
    if not all(isinstance(value, list) for value in (guides, policies, exclusions)):
        raise ValueError("Criteria collections must be arrays.")

    manifest_sha = hashlib.sha256(manifest_bytes).hexdigest()
    boundary = "FROZEN_REVIEW_CRITERIA_" + manifest_sha
    pieces = [
        b"# Frozen review criteria\n\n"
        b"This caller-owned supplement does not change your assigned coordinator or worker role.\n"
        b"For a per-guide worker, apply only its assigned guide and matching policy.guide clauses.\n"
        b"Apply the explicit exclusions without removing or rewriting any delivered criteria.\n"
        b"Do not regenerate these criteria in task prompts. Native canonical reads remain required.\n"
        b"Bodies are exact UTF-8 bytes; byte lengths exclude the surrounding separator newlines.\n",
        ("\nBEGIN_" + boundary + "\n").encode("ascii"),
        (json.dumps({"head": expected_head, "guidance": expected_guidance}, sort_keys=True) + "\n").encode("ascii"),
    ]
    inventory = []

    def append_body(kind, identity, body):
        encoded = body.encode("utf-8")
        metadata = {
            **identity, "utf8Bytes": len(encoded),
            "sha256": hashlib.sha256(encoded).hexdigest(),
        }
        marker = (kind + " " + json.dumps(metadata, sort_keys=True)).encode("ascii")
        if boundary.encode("ascii") in encoded:
            raise ValueError("Criteria contain their reserved outer boundary.")
        pieces.extend([b"\nBEGIN_" + marker + b"\n", encoded, b"\nEND_" + marker + b"\n"])
        inventory.append({"kind": kind, **{
            key: value for key, value in metadata.items() if key not in ("scope", "reason")
        }})

    root = manifest_path.parent.resolve(strict=True)
    guide_paths = set()
    for guide in guides:
        if not isinstance(guide, dict):
            raise ValueError("Malformed guide metadata.")
        name = _relative(guide.get("path")).as_posix()
        if name in guide_paths:
            raise ValueError("Duplicate routed guide.")
        guide_paths.add(name)
        body = _contained_file(root, "guidance/" + name + ".source").read_bytes().decode("utf-8")
        if not body:
            raise ValueError("A routed guide is empty.")
        append_body("GUIDE", {"path": name}, body)

    for index, policy in enumerate(policies):
        if not isinstance(policy, dict) or not isinstance(policy.get("guide"), str) \
                or policy["guide"] not in guide_paths \
                or not isinstance(policy.get("anchor"), str) or not policy["anchor"] \
                or not isinstance(policy.get("body"), str) or not policy["body"]:
            raise ValueError("Malformed policy or guide association.")
        name = _relative(policy.get("path")).as_posix()
        append_body("POLICY", {
            "index": index, "guide": policy["guide"], "path": name, "anchor": policy["anchor"],
        }, policy["body"])

    for index, exclusion in enumerate(exclusions):
        if not isinstance(exclusion, dict) or not exclusion \
                or not set(exclusion).issubset({"scope", "reason", "source", "body"}) \
                or not all(isinstance(value, str) and value for value in exclusion.values()):
            raise ValueError("Malformed explicit exclusion.")
        metadata = {key: value for key, value in exclusion.items() if key != "body"}
        append_body("EXCLUSION", {"index": index, **metadata}, exclusion.get("body", ""))
    pieces.append(("\nEND_" + boundary + "\n").encode("ascii"))
    content = b"".join(pieces)

    workspace = Path(workspace).resolve(strict=True)
    directory = workspace / ".github"
    if directory.is_symlink() or directory.exists() and directory.resolve() != directory.absolute():
        raise ValueError("The caller instruction directory must not be a link.")
    directory.mkdir(exist_ok=True)
    destination = directory / "copilot-instructions.md"
    with destination.open("xb") as stream:
        stream.write(content)
    return {
        "path": ".github/copilot-instructions.md", "manifestSha256": manifest_sha,
        "utf8Bytes": len(content), "sha256": hashlib.sha256(content).hexdigest(),
        "criteria": inventory,
    }


if __name__ == "__main__":
    print(json.dumps(materialize(*sys.argv[1:]), sort_keys=True))
