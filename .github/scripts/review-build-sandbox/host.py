"""Fork-only, one-shot AWF probe. Never execute target files on the host."""

import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import stat
import subprocess
import sys
import threading
import time
import xml.etree.ElementTree as ET

ROOT = Path("/var/lib/review-build-phase1")
SOURCE = ROOT / "work"
CONTROL = ROOT / "control"
OUT = ROOT / "out"
EVIDENCE = Path(os.environ["RUNNER_TEMP"]) / "review-build-evidence"
AWF_STATE = Path(os.environ["RUNNER_TEMP"]) / "review-build-awf"
SHA = "d9476ed1cb52fc9501e36d5e0cf1b4796b67aefd"
TREE = "601a8b72acdb4c02d32e41f1c0b5069181646fa9"
SDK = "11.0.100-rc.1.26420.103"
CLASS = "Microsoft.AspNetCore.ResponseCaching.Tests.ResponseCachingPolicyProviderTests"
PINS = {
    "agent": "sha256:a9861b2fad1d29c586bbbf98840a61056e30e2720d8d9ec2c0def9049566bd8a",
    "squid": "sha256:be808c16b535ebabd070eebe086fe6bffd5bb86b2e5fe8e5cdad30068eb9c0ac",
    "api-proxy": "sha256:f2925ff32dc972a5d8b90a7785b6ca68052efd2575fe896556ac457e22e86dcc",
    "cli-proxy": "sha256:4584f030659ee51958223a3c6156e8119c6a94affbfcd92ce75f204702c52641",
}
# HTTPS only. Azure Artifacts redirects package downloads to vsblob storage.
DOMAINS = [
    "https://pkgs.dev.azure.com", "https://*.vsblob.vsassets.io",
    "https://builds.dotnet.microsoft.com", "https://ci.dot.net",
    "https://dotnetcli.azureedge.net", "https://dotnetcli.blob.core.windows.net",
    "https://data.nuget.org", "https://api.nuget.org", "https://globalcdn.nuget.org",
]
receipt = {
    "schema": 1, "repository": "dotnet/aspnetcore", "pull_request": 69210,
    "head_sha": SHA, "tree_sha": TREE, "sdk": SDK,
    "workflow_repository": os.environ["GITHUB_REPOSITORY"],
    "workflow_sha": os.environ["GITHUB_SHA"],
    "run_id": os.environ["GITHUB_RUN_ID"], "run_attempt": os.environ["GITHUB_RUN_ATTEMPT"],
    "launcher_sha256": hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    "guest_sha256": hashlib.sha256(Path(__file__).with_name("guest.py").read_bytes()).hexdigest(),
    "implementation_sha": os.environ["PILOT_IMPLEMENTATION_SHA"],
    "awf_version": "v0.28.57", "image_pins": PINS, "allow_domains": DOMAINS,
    "resources": [], "acceptance": {}, "status": "failed",
}
clean_env = {
    "PATH": "/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin",
    "HOME": str(ROOT / "home"), "GIT_CONFIG_GLOBAL": "/dev/null",
    "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0",
}
process = None
log_thread = None
oversize_log = threading.Event()
started = time.monotonic()


def run(args, **kwargs):
    return subprocess.run(args, check=True, **kwargs)


def text(args):
    return subprocess.check_output(args, text=True).strip()


def sample():
    data = {
        "seconds": round(time.monotonic() - started, 3),
        "disk_free_bytes": shutil.disk_usage(ROOT).free,
        "memory_available_kb": next(
            int(line.split()[1]) for line in Path("/proc/meminfo").read_text().splitlines()
            if line.startswith("MemAvailable:")
        ),
    }
    result = subprocess.run(
        ["docker", "stats", "--no-stream", "--format", "{{json .}}", "awf-agent"],
        text=True, capture_output=True, check=False,
    )
    if result.returncode == 0 and result.stdout.strip():
        data["container"] = json.loads(result.stdout)
    receipt["resources"].append(data)


def capture_log(pipe):
    remaining = 8 * 1024 * 1024
    with (EVIDENCE / "awf.log").open("wb") as log:
        while data := pipe.read(65536):
            log.write(data[:remaining])
            if len(data) > remaining:
                oversize_log.set()
            remaining = max(0, remaining - len(data))


def bounded(name, limit=8 * 1024 * 1024):
    path = OUT / name
    fd = os.open(path, os.O_RDONLY | os.O_NOFOLLOW | os.O_NONBLOCK)
    with os.fdopen(fd, "rb") as file:
        info = os.fstat(file.fileno())
        if not stat.S_ISREG(info.st_mode) or info.st_size > limit or info.st_nlink != 1:
            raise ValueError(f"Rejected outbox file: {name}")
        data = file.read(limit + 1)
        if len(data) > limit:
            raise ValueError(f"Oversized outbox file: {name}")
        (EVIDENCE / name).write_bytes(data)
        return data


def audit_container():
    container = json.loads(text(["docker", "inspect", "awf-agent"]))[0]
    names = [value.split("=", 1)[0] for value in container["Config"]["Env"]]
    forbidden = [
        name for name in names
        if re.search(r"TOKEN|SECRET|PASSWORD|CREDENTIAL|API_KEY", name, re.I)
        and name != "AWF_ONE_SHOT_TOKENS"
    ]
    if forbidden:
        raise RuntimeError("Credential environment names found in container")
    allowed_system = {
        "/usr", "/bin", "/sbin", "/lib", "/lib64", "/opt", "/sys", "/dev", "/dev/null",
        "/etc/ssl", "/etc/ca-certificates", "/etc/alternatives", "/etc/passwd", "/etc/group",
        "/etc/resolv.conf", "/etc/hosts", "/etc/localtime", "/etc/nsswitch.conf",
        "/etc/ld.so.cache", "/etc/pki/ca-trust/extracted", "/etc/pki/tls/certs", "/tmp",
    }
    mounts = container["Mounts"]
    for mount in mounts:
        source = Path(mount["Source"])
        if mount["Type"] != "bind":
            if mount["Type"] != "tmpfs":
                raise RuntimeError("Unexpected non-bind mount")
            continue
        if str(source) not in allowed_system and not (
            source.is_relative_to(ROOT) or source.is_relative_to(AWF_STATE)
            or str(source) == str(AWF_STATE) + "-chroot-home"
        ):
            raise RuntimeError(f"Unexpected mount source: {source}")
        if str(source) in allowed_system - {"/tmp", "/dev/null"} and mount["RW"]:
            raise RuntimeError("Writable system mount")
        if str(source) == str(CONTROL) and mount["RW"]:
            raise RuntimeError("Writable trusted control mount")
    if not any(m["Source"] == str(CONTROL) and not m["RW"] for m in mounts):
        raise RuntimeError("Missing read-only control mount")
    if container["HostConfig"]["Privileged"] or container["HostConfig"]["NetworkMode"] == "host":
        raise RuntimeError("Unexpected privileged/host-network container")
    if container["HostConfig"]["Memory"] != 6 * 1024 ** 3:
        raise RuntimeError("Unexpected memory limit")
    services = text(["docker", "ps", "--format", "{{.Names}}"]).splitlines()
    if any("cli-proxy" in name or "mcp" in name for name in services):
        raise RuntimeError("Unexpected CLI/MCP sidecar")
    # v0.28.57 always starts this sidecar. It must have zero usable credentials.
    proxy = json.loads(text(["docker", "inspect", "awf-api-proxy"]))[0]
    proxy_env = dict(value.split("=", 1) for value in proxy["Config"]["Env"] if "=" in value)
    if any(value for key, value in proxy_env.items()
           if re.search(r"TOKEN|SECRET|PASSWORD|CREDENTIAL|API_KEY|PRIVATE_KEY", key, re.I)):
        raise RuntimeError("Mandatory API proxy has credential material")
    expected_proxy = f"ghcr.io/github/gh-aw-firewall/api-proxy:0.28.57@{PINS['api-proxy']}"
    if proxy["Config"]["Image"] != expected_proxy:
        raise RuntimeError("Unexpected API proxy image")
    receipt["container_audit"] = {
        "environment_names": names, "mounts": mounts, "services": services,
        "credential_free_api_proxy_environment_names": list(proxy_env),
        "image_id": container["Image"], "host_config": {
            key: container["HostConfig"][key]
            for key in ("Memory", "Privileged", "NetworkMode", "CapAdd", "CapDrop")
        },
    }


try:
    if os.environ["GITHUB_REPOSITORY"] != "PureWeen/aspnetcore":
        raise RuntimeError("Fork-only guard")
    if os.environ["GITHUB_RUN_ATTEMPT"] != "1":
        raise RuntimeError("No automatic/manual reruns: use a newly approved dispatch")
    EVIDENCE.mkdir()
    AWF_STATE.mkdir(mode=0o700)
    if text(["git", "rev-parse", "HEAD"]) != os.environ["PILOT_IMPLEMENTATION_SHA"]:
        raise RuntimeError("Probe helper revision mismatch")
    if hashlib.sha256(Path(shutil.which("awf")).read_bytes()).hexdigest() != (
        "2c9f800e56fad5ee974f3fdc5610b2a5b5632655a25b55d7bbae0c3d6dff165a"
    ):
        raise RuntimeError("AWF binary differs from reviewed release checksum")
    run(["sudo", "mkdir", "-p", str(ROOT)])
    run(["sudo", "chown", f"{os.getuid()}:{os.getgid()}", str(ROOT)])
    for path in (SOURCE, CONTROL, OUT, ROOT / "home"):
        path.mkdir()
    shutil.copyfile(Path(__file__).with_name("guest.py"), CONTROL / "guest.py")
    (CONTROL / "read-only-canary").write_text("read-only\n")
    Path("/home/runner/phase1-host-only").write_text("harmless host-only sentinel\n")
    # No checkout action, Git credential helper, hook, submodule, LFS or PR script.
    run(["git", "-c", "core.hooksPath=/dev/null", "init", "--quiet", str(SOURCE)], env=clean_env)
    git = ["git", "-C", str(SOURCE), "-c", "core.hooksPath=/dev/null"]
    run(git + ["fetch", "--quiet", "--depth=1", "https://github.com/dotnet/aspnetcore.git", SHA], env=clean_env)
    run(git + ["checkout", "--quiet", "--detach", "FETCH_HEAD"], env=clean_env)
    if text(git + ["rev-parse", "HEAD"]) != SHA or text(git + ["rev-parse", "HEAD^{tree}"]) != TREE:
        raise RuntimeError("Target revision mismatch")
    if json.loads((SOURCE / "global.json").read_text())["sdk"]["version"] != SDK:
        raise RuntimeError("Target SDK changed")
    receipt["acquisition_seconds"] = round(time.monotonic() - started, 3)
    # Pin pulls explicitly; setup action's pull-images fallback to latest is not used.
    for name in ("agent", "squid", "api-proxy"):
        run(["docker", "pull", f"ghcr.io/github/gh-aw-firewall/{name}:0.28.57@{PINS[name]}"])
    config = {
        "network": {"allowDomains": DOMAINS, "isolation": True},
        "container": {
            "imageTag": "0.28.57," + ",".join(f"{key}={value}" for key, value in PINS.items()),
            "agentTimeout": 30,
        },
    }
    config_path = EVIDENCE / "awf-config.json"
    config_path.write_text(json.dumps(config, indent=2))
    sample()
    if receipt["resources"][-1]["disk_free_bytes"] < 12 * 1024 ** 3:
        raise RuntimeError("Less than 12 GiB initial disk headroom")
    command = [
        "sudo", "env", "-i", *[f"{key}={value}" for key, value in clean_env.items()],
        f"SUDO_UID={os.getuid()}", f"SUDO_GID={os.getgid()}",
        f"GITHUB_WORKSPACE={SOURCE}", shutil.which("awf"),
        "--config", str(config_path), "--work-dir", str(AWF_STATE),
        "--container-workdir", str(SOURCE), "--memory-limit", "6g", "--skip-pull",
        "--mount", f"{CONTROL}:{CONTROL}:ro", "--mount", f"{OUT}:{OUT}:rw",
        "--", "python3", str(CONTROL / "guest.py"),
    ]
    receipt["awf_command"] = command
    process = subprocess.Popen(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT)
    log_thread = threading.Thread(target=capture_log, args=(process.stdout,), daemon=True)
    log_thread.start()
    deadline = time.monotonic() + 180
    while not (OUT / "ready").exists():
        if oversize_log.is_set():
            raise RuntimeError("AWF log exceeded 8 MiB")
        if process.poll() is not None:
            raise RuntimeError("AWF/preflight exited before approval")
        if time.monotonic() > deadline:
            raise RuntimeError("AWF/preflight startup timeout")
        time.sleep(2)
    audit_container()
    probes = json.loads(bounded("probes.json"))
    if not (
        probes["allowed_exit"] == 0 and probes["explicit_proxy_denial"]
        and not probes["exposed_paths"] and not probes["credential_env_names"]
        and probes["control_read_only"] and probes["uid"] != 0
    ):
        raise RuntimeError("Pre-target probe evidence failed")
    receipt["probes"] = probes
    (OUT / "approved").touch()
    receipt["acceptance"]["container_gate"] = True
    deadline = time.monotonic() + 30 * 60
    while process.poll() is None:
        if oversize_log.is_set():
            raise RuntimeError("AWF log exceeded 8 MiB")
        if time.monotonic() > deadline:
            raise RuntimeError("30-minute worker budget exceeded")
        sample()
        if receipt["resources"][-1]["disk_free_bytes"] < 4 * 1024 ** 3:
            raise RuntimeError("Less than 4 GiB disk reserve")
        if receipt["resources"][-1]["memory_available_kb"] < 512 * 1024:
            raise RuntimeError("Less than 512 MiB VM memory reserve")
        time.sleep(15)
    receipt["awf_exit"] = process.returncode
    log_thread.join(timeout=10)
    if log_thread.is_alive() or oversize_log.is_set():
        raise RuntimeError("AWF log was oversized or did not close")
    if process.returncode:
        raise RuntimeError(f"AWF/target failed: exit {process.returncode}")
    phases = json.loads(bounded("phases.json"))
    receipt.update({"probes": probes, "phases": phases})
    for name in ("bootstrap", "sdk", "restore", "build", "test"):
        bounded(f"{name}.log")
    if SDK not in (EVIDENCE / "sdk.log").read_text().splitlines():
        raise RuntimeError("Executed SDK does not match target pin")
    xml = bounded("tests.xml")
    if b"<!DOCTYPE" in xml or b"<!ENTITY" in xml:
        raise RuntimeError("Rejected XML declaration")
    assemblies = ET.fromstring(xml).findall(".//assembly")
    if len(assemblies) != 1:
        raise RuntimeError("Expected exactly one test assembly")
    assembly = assemblies[0]
    if not assembly.get("name", "").endswith("Microsoft.AspNetCore.ResponseCaching.Tests.dll"):
        raise RuntimeError("Unexpected test assembly")
    tests = assembly.findall(".//test")
    executed = [test for test in tests if test.get("result") in ("Pass", "Fail")]
    vary = [test for test in executed if "IsResponseCacheable_VaryHeaderByStar_NotAllowed" in test.get("name", "")]
    if not executed or len(vary) != 5 or any(test.get("type") != CLASS for test in tests):
        raise RuntimeError("Missing intended test class or five Vary-star theory rows")
    if any(test.get("result") != "Pass" for test in executed):
        raise RuntimeError("Tests failed")
    receipt["tests"] = {
        "executed": len(executed), "skipped": len(tests) - len(executed),
        "vary_star_rows": len(vary), "assembly": assembly.attrib,
    }
    receipt["acceptance"].update({
        "nonzero_tests": True, "five_regression_rows": True, "pinned_sdk": True,
        "allow_and_deny": probes["allowed_exit"] == 0 and probes["explicit_proxy_denial"],
        "no_credentials_or_control_paths": not probes["exposed_paths"] and not probes["credential_env_names"],
        "exact_head_and_tree": True, "disk_reserve": True,
        "memory_reserve": True,
    })
    receipt["status"] = "passed"
except Exception as error:
    receipt["error"] = f"{type(error).__name__}: {error}"
finally:
    if process is not None and process.poll() is None:
        subprocess.run(["docker", "kill", "awf-agent"], check=False, capture_output=True)
        try:
            process.wait(timeout=60)
        except subprocess.TimeoutExpired:
            process.kill()
            process.wait()
    if log_thread is not None:
        log_thread.join(timeout=10)
    if EVIDENCE.exists():
        for name in ("failure.txt", "probes.json", "phases.json", "bootstrap.log", "sdk.log",
                     "restore.log", "build.log", "test.log", "tests.xml"):
            if (OUT / name).exists() and not (EVIDENCE / name).exists():
                try:
                    bounded(name)
                except (OSError, ValueError) as error:
                    receipt.setdefault("rejected_outputs", []).append(f"{name}: {error}")
        receipt["total_seconds"] = round(time.monotonic() - started, 3)
        receipt["file_sha256"] = {
            path.name: hashlib.sha256(path.read_bytes()).hexdigest()
            for path in EVIDENCE.iterdir() if path.is_file()
        }
        (EVIDENCE / "receipt.json").write_text(json.dumps(receipt, indent=2))
    print(json.dumps({"status": receipt["status"], "error": receipt.get("error"),
                      "tests": receipt.get("tests"), "seconds": receipt.get("total_seconds")}))
sys.exit(0 if receipt["status"] == "passed" else 1)
