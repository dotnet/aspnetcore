"""Trusted command runner. All target execution stays in the non-AI sandbox."""

import hashlib
from itertools import islice
import json
import os
from pathlib import Path
import re
import shutil
import stat
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
from contract import ROOT as ROOT_PATH, commands

ROOT = Path(ROOT_PATH)
SOURCE = ROOT / "work"
OUT = ROOT / "out"
CONTROL = ROOT / "control"
TOOLS = ROOT / "tools"
RESULTS = SOURCE / "artifacts" / "TestResults" / "Debug"
profile = json.loads((CONTROL / "profile.json").read_text())
phases = []


def record():
    (OUT / "phases.json").write_text(json.dumps(phases, indent=2))


def phase(name, command, timeout=900):
    started = time.monotonic()
    result = {"name": name, "command": command, "exit": None}
    phases.append(result)
    record()
    with (OUT / f"{name}.log").open("wb") as log:
        try:
            result["exit"] = subprocess.run(
                command, cwd=SOURCE, stdout=log, stderr=subprocess.STDOUT,
                timeout=timeout, check=False,
            ).returncode
        except subprocess.TimeoutExpired:
            result["exit"] = 124
    result["seconds"] = round(time.monotonic() - started, 3)
    record()
    if result["exit"]:
        raise RuntimeError(f"{name} failed: exit {result['exit']}")


def results_directory():
    for path in (SOURCE, RESULTS.parent.parent, RESULTS.parent, RESULTS):
        info = path.lstat()
        if not stat.S_ISDIR(info.st_mode) or (
            os.name == "nt" and info.st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT
        ):
            raise ValueError("linked_or_non_directory")


def retain_test_report():
    result = {"state": "absent", "reason": "no_report", "file_sha256": {}}
    try:
        results_directory()
        with os.scandir(RESULTS) as entries:
            inventory = list(islice(entries, 129))
        if len(inventory) > 128:
            return {**result, "state": "rejected", "reason": "directory_entry_limit"}
        reports = [
            Path(entry.path) for entry in inventory
            if re.fullmatch(re.escape(profile["assembly"]) + r"_net[0-9]+\.[0-9]+_x64\.xml", entry.name)
        ]
        if len(reports) > 1:
            return {**result, "state": "rejected", "reason": "multiple_reports"}
        if not reports:
            return result
        path = reports[0]
        info = path.lstat()
        if not stat.S_ISREG(info.st_mode) or info.st_nlink != 1 or (
            os.name == "nt" and info.st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT
        ):
            return {**result, "state": "rejected", "reason": "linked_or_non_regular"}
        limit = 8 * 1024 * 1024
        if info.st_size > limit:
            return {**result, "state": "rejected", "reason": "size_limit"}
        flags = os.O_RDONLY
        if os.name == "posix":
            flags |= os.O_NOFOLLOW | os.O_NONBLOCK
        with os.fdopen(os.open(path, flags), "rb") as file:
            opened = os.fstat(file.fileno())
            if (not stat.S_ISREG(opened.st_mode) or opened.st_nlink != 1
                    or (opened.st_dev, opened.st_ino) != (info.st_dev, info.st_ino)):
                return {**result, "state": "rejected", "reason": "changed_file"}
            data = file.read(limit + 1)
        results_directory()
        if len(data) > limit:
            return {**result, "state": "rejected", "reason": "size_limit"}
        if len(data) != info.st_size:
            return {**result, "state": "malformed_or_truncated", "reason": "changed_size"}
        if b"<!DOCTYPE" in data or b"<!ENTITY" in data or b"\0" in data:
            return {**result, "state": "rejected", "reason": "xml_declaration"}
        try:
            ET.fromstring(data)
        except ET.ParseError:
            return {**result, "state": "malformed_or_truncated", "reason": "xml_parse"}
        with (OUT / "tests.xml").open("xb") as file:
            file.write(data)
        return {
            "state": "copied", "reason": "single_report", "source": path.name,
            "file_sha256": {"tests.xml": hashlib.sha256(data).hexdigest()},
        }
    except FileNotFoundError:
        return {**result, "reason": "missing_directory_or_file"}
    except ValueError:
        return {**result, "state": "rejected", "reason": "linked_or_non_directory"}
    except OSError as error:
        return {**result, "state": "copy_failed", "reason": type(error).__name__}


def preflight():
    forbidden = [
        name for name in os.environ
        if any(word in name.upper() for word in ("TOKEN", "SECRET", "PASSWORD", "CREDENTIAL", "API_KEY"))
        and name != "AWF_ONE_SHOT_TOKENS"
    ]
    if forbidden:
        raise RuntimeError("Unexpected credential environment names: " + ", ".join(forbidden))
    masked_sockets = []
    for path in ("/var/run/docker.sock", "/run/docker.sock"):
        if Path(path).exists():
            info = Path(path).stat()
            if not stat.S_ISCHR(info.st_mode) or info.st_rdev != os.makedev(1, 3):
                raise RuntimeError("Docker socket is not masked by /dev/null")
            masked_sockets.append(path)
    forbidden_paths = [
        "/home/runner/.config/gh/hosts.yml", "/home/runner/.docker/config.json",
        "/home/runner/.copilot/config.json", "/home/runner/.gitconfig",
        "/home/runner/work/_temp/_runner_file_commands",
        "/home/runner/phase1-host-only",
    ]
    exposed = [path for path in forbidden_paths if Path(path).exists()]
    if exposed:
        raise RuntimeError("Host credential/control paths exposed: " + ", ".join(exposed))
    try:
        with (CONTROL / "read-only-canary").open("a") as file:
            file.write("must not succeed")
    except OSError:
        read_only = True
    else:
        raise RuntimeError("Trusted control mount is writable")
    allowed = subprocess.run(
        ["curl", "--fail", "--silent", "--show-error", "--max-time", "30",
         "https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-public/nuget/v3/index.json"],
        stdout=subprocess.DEVNULL, stderr=subprocess.PIPE, check=False,
    )
    denied = subprocess.run(
        ["curl", "--fail", "--silent", "--show-error", "--max-time", "15",
         "--proxy", os.environ["HTTP_PROXY"],
         "--output", "/dev/null", "--write-out", "%{http_code}", "http://example.com"],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False,
    )
    direct = subprocess.run(
        ["curl", "--noproxy", "*", "--silent", "--show-error", "--connect-timeout", "5",
         "--max-time", "8", "--output", "/dev/null", "--write-out", "%{http_code}",
         "http://1.1.1.1"],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False,
    )
    explicit_denial = denied.returncode == 22 and denied.stdout == b"403"
    direct_denial = direct.returncode in (7, 28) and direct.stdout == b"000"
    probes = {
        "allowed_exit": allowed.returncode, "denied_exit": denied.returncode,
        "explicit_proxy_denial": explicit_denial, "credential_env_names": forbidden,
        "denied_http_status": denied.stdout.decode("ascii", errors="replace"),
        "direct_exit": direct.returncode, "direct_egress_denied": direct_denial,
        "direct_target": "http://1.1.1.1",
        "denied_error": denied.stderr.decode("utf-8", errors="replace")[:512],
        "direct_error": direct.stderr.decode("utf-8", errors="replace")[:512],
        "exposed_paths": exposed, "control_read_only": read_only,
        "dev_null_masked_socket_paths": masked_sockets,
        "uid": os.getuid(), "gid": os.getgid(),
    }
    (OUT / "probes.json").write_text(json.dumps(probes, indent=2))
    if allowed.returncode or not explicit_denial or not direct_denial or os.getuid() == 0:
        raise RuntimeError("Network/identity acceptance failed")
    (OUT / "ready").touch()
    deadline = time.monotonic() + 120
    while not (OUT / "approved").exists():
        if time.monotonic() > deadline:
            raise RuntimeError("Host mount/environment approval timed out")
        time.sleep(1)


try:
    preflight()
    os.environ.update({
        "HOME": str(TOOLS),
        "DOTNET_CLI_HOME": str(TOOLS),
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
        "DOTNET_NOLOGO": "1",
        "NUGET_PACKAGES": str(TOOLS / ".nuget/packages"),
        "NUGET_HTTP_CACHE_PATH": str(TOOLS / ".nuget/http-cache"),
        "MSBUILDDISABLENODEREUSE": "1",
    })
    planned = commands(profile)
    for name, timeout in (("bootstrap", 600), ("sdk", 30), ("restore", 900), ("build", 900), ("test", 300)):
        if name == "test":
            for path in (RESULTS.parent.parent, RESULTS.parent, RESULTS):
                path.mkdir(exist_ok=True)
                info = path.lstat()
                if not stat.S_ISDIR(info.st_mode) or (
                    os.name == "nt" and info.st_file_attributes & stat.FILE_ATTRIBUTE_REPARSE_POINT
                ):
                    raise RuntimeError("Test result parent is linked or not a directory")
            with os.scandir(RESULTS) as entries:
                if next(entries, None) is not None:
                    raise RuntimeError("Test result directory is not empty before test execution")
        phase(name, planned[name], timeout)
    reports = list((SOURCE / "artifacts/TestResults/Debug").glob(profile["assembly"] + "_net*_x64.xml"))
    if len(reports) != 1:
        raise RuntimeError("Expected one test report for the selected assembly")
    shutil.copyfile(reports[0], OUT / "tests.xml")
except Exception as error:
    (OUT / "failure.txt").write_text(f"{type(error).__name__}: {error}\n")
    if phases and phases[-1]["name"] == "test" and phases[-1]["exit"] != 0:
        try:
            retention = retain_test_report()
        except Exception as retention_error:
            retention = {
                "state": "copy_failed", "reason": type(retention_error).__name__, "file_sha256": {},
            }
        try:
            (OUT / "retention.json").write_text(json.dumps(retention, indent=2))
        except OSError as retention_error:
            print("Test report retention metadata failed: " + type(retention_error).__name__, file=sys.stderr)
    sys.exit(1)
