"""Temporary, non-AI Phase 1 probe. All target execution stays in AWF."""

import json
import os
from pathlib import Path
import shutil
import stat
import subprocess
import sys
import time

ROOT = Path("/var/lib/review-build-phase1")
SOURCE = ROOT / "work"
OUT = ROOT / "out"
CONTROL = ROOT / "control"
CLASS = "Microsoft.AspNetCore.ResponseCaching.Tests.ResponseCachingPolicyProviderTests"
PROJECT = SOURCE / "src/Middleware/ResponseCaching/test/Microsoft.AspNetCore.ResponseCaching.Tests.csproj"
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
         "--output", "/dev/null", "--write-out", "%{http_code}", "http://example.com"],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False,
    )
    direct = subprocess.run(
        ["curl", "--noproxy", "*", "--silent", "--show-error", "--connect-timeout", "5",
         "--max-time", "8", "--output", "/dev/null", "--write-out", "%{http_code}",
         "http://example.com"],
        stdout=subprocess.PIPE, stderr=subprocess.PIPE, check=False,
    )
    explicit_denial = denied.returncode == 22 and denied.stdout == b"403"
    direct_denial = direct.returncode != 0 and direct.stdout == b"000"
    probes = {
        "allowed_exit": allowed.returncode, "denied_exit": denied.returncode,
        "explicit_proxy_denial": explicit_denial, "credential_env_names": forbidden,
        "denied_http_status": denied.stdout.decode("ascii", errors="replace"),
        "direct_exit": direct.returncode, "direct_egress_denied": direct_denial,
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
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
        "DOTNET_NOLOGO": "1",
        "NUGET_PACKAGES": str(ROOT / "home/.nuget/packages"),
        "NUGET_HTTP_CACHE_PATH": str(ROOT / "home/.nuget/http-cache"),
        "MSBUILDDISABLENODEREUSE": "1",
    })
    phase("bootstrap", ["bash", "-c", "source ./activate.sh && bash ./eng/common/dotnet.sh --info"], 600)
    phase("sdk", ["bash", "-c", "source ./activate.sh && dotnet --version"], 30)
    common = [
        "bash", "./eng/build.sh", "--projects", str(PROJECT), "--configuration", "Debug",
        "--no-build-native", "--no-build-nodejs", "--no-build-java", "--no-build-installers",
        "--msbuildMultiThreaded", "false",
    ]
    phase("restore", common + ["--restore", "--no-build", "--no-test"])
    phase("build", common + ["--no-restore", "--build", "--no-test"])
    os.environ["TestRunnerAdditionalArguments"] = f"-class {CLASS}"
    phase("test", common + [
        "--no-restore", "--no-build", "--test",
    ], 300)
    shutil.copyfile(
        SOURCE / "artifacts/TestResults/Debug/Microsoft.AspNetCore.ResponseCaching.Tests_net11.0_x64.xml",
        OUT / "tests.xml",
    )
except Exception as error:
    (OUT / "failure.txt").write_text(f"{type(error).__name__}: {error}\n")
    sys.exit(1)
