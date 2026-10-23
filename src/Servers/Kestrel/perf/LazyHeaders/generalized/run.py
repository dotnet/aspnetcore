import argparse
import datetime
import hashlib
import json
import os
import pathlib
import subprocess
import time
import traceback
import urllib.error
import urllib.request


def read_json(url):
    with urllib.request.urlopen(url, timeout=5) as response:
        return json.load(response)


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2) + "\n")


def now():
    return datetime.datetime.now(datetime.timezone.utc).isoformat()


def manifest(root):
    return {str(p.relative_to(root)): digest(p) for p in sorted(root.rglob("*")) if p.is_file()}


parser = argparse.ArgumentParser()
for name in ["controller", "repo", "head", "base", "runtime", "wrapper", "output"]:
    parser.add_argument("--" + name, type=pathlib.Path, required=True)
parser.add_argument("--source", required=True, help="Immutable aspnetcore commit containing this workload")
parser.add_argument("--repetitions", type=int, default=5)
parser.add_argument("--warmup", type=int, default=15)
parser.add_argument("--duration", type=int, default=30)
parser.add_argument("--case", action="append", dest="cases")
parser.add_argument("--resume", action="store_true")
parser.add_argument("--record-errors", action="store_true",
                    help="Retain socket errors and continue fixed cases; bad responses still fail")
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=args.resume)
assert len(args.source) == 40 and all(c in "0123456789abcdef" for c in args.source)
assert args.repetitions > 0 and args.warmup > 0 and args.duration > 0
assert os.environ.get("TMPDIR") == "/tmp/lhp-7a143fff", "Use the profile's same short TMPDIR for controller/agent/load/app"
config = args.repo / "src/Servers/Kestrel/perf/lazy-headers.crank.yml"
profile = args.repo / "src/Servers/Kestrel/perf/LazyHeaders/loopback-poc.yml"
script = args.repo / "src/Servers/Kestrel/perf/lazy-headers.wrk.lua"
core = "Microsoft.AspNetCore.Server.Kestrel.Core.dll"
hashes = {"base": digest(args.base / core), "head": digest(args.head / core)}
payloads = {"base": manifest(args.base), "head": manifest(args.head)}
assert payloads["base"].keys() == payloads["head"].keys()
assert [p for p in payloads["head"] if payloads["head"][p] != payloads["base"][p]] == [core]
assert hashes["base"] == "7175498ad5d77f11fa7672fe89bf4a51ad7a8b8410ed1cbc4c85906ee1fe27f5"
with urllib.request.urlopen(
    f"https://raw.githubusercontent.com/dotnet/aspnetcore/{args.source}/src/Servers/Kestrel/perf/lazy-headers.wrk.lua",
    timeout=30,
) as response:
    assert hashlib.sha256(response.read()).hexdigest() == digest(script)
cases = args.cases or ["ignore:stable", "ignore:changing", "read-all:stable", "read-all:changing"]
plan = []
variants = ["base", "off", "on"]
for repetition in range(args.repetitions):
    order = variants[repetition % 3:] + variants[:repetition % 3]
    for case in cases[::1 if repetition % 2 == 0 else -1]:
        access, mode = case.split(":")
        assert access in {"ignore", "read-one", "read-all", "enumerate"}
        assert mode in {"stable", "changing", "duplicate-known", "duplicate-unknown", "budget"}
        for variant in order:
            plan.append(dict(repetition=repetition + 1, access=access, mode=mode, variant=variant))
if args.resume:
    assert json.loads((args.output / "plan.json").read_text()) == plan
    previous = json.loads((args.output / "inputs.json").read_text())
    assert previous["coreSha256"] == hashes
    assert previous["source"] == args.source
    assert previous["warmupSeconds"] == args.warmup and previous["measurementSeconds"] == args.duration
    assert previous["configSha256"] == digest(config) and previous["profileSha256"] == digest(profile)
else:
    write_json(args.output / "plan.json", plan)
    write_json(args.output / "payload-manifests.json", payloads)
    write_json(args.output / "runtime-manifest.json", manifest(args.runtime / "shared"))
    write_json(args.output / "inputs.json", {
        "startedUtc": now(), "source": args.source, "headPayload": str(args.head), "basePayload": str(args.base),
        "coreSha256": hashes, "controllerSha256": digest(args.controller),
        "configSha256": digest(config), "profileSha256": digest(profile), "scriptSha256": digest(script),
        "runnerSha256": digest(pathlib.Path(__file__)),
        "wrapperManifest": manifest(args.wrapper),
        "wrkSha256": digest(pathlib.Path(os.environ["TMPDIR"]) / ".crank/wrk-linux-amd64"),
        "warmupSeconds": args.warmup, "measurementSeconds": args.duration,
        "connections": 16, "threads": 2, "timeoutSeconds": 10,
        "sharedLoopback": True, "serverRuntimeRoot": str(args.runtime),
        "recordErrors": args.record_errors,
    })
agent = "http://127.0.0.1:15010"
assert read_json(agent + "/jobs") == [], "Agent already has jobs"


def verify_result(folder):
    document = json.loads((folder / "result.json").read_text())
    assert document["returnCode"] == 0
    jobs = document["jobResults"]["jobs"]
    assert jobs["application"]["measurements"] and jobs["load"]["measurements"]
    metrics = jobs["load"]["results"]
    assert metrics["http/requests"] > 0 and metrics["http/rps/mean"] > 0
    for percentile in ["50", "75", "90", "99"]:
        assert metrics[f"http/latency/{percentile}"] > 0
    assert metrics["http/requests/badresponses"] == 0
    if not args.record_errors:
        assert metrics["http/requests/errors"] == 0
    identity = json.loads((folder / "loaded-identity.json").read_text())
    expected = hashes["base" if json.loads((folder / "execution.json").read_text())["variant"] == "base" else "head"]
    assert identity["coreSha256"] == expected
    return metrics


def run_all():
    for index, run in enumerate(plan):
        name = f"{index + 1:03d}-{run['variant']}-{run['access']}-{run['mode']}"
        folder = args.output / name
        if folder.exists():
            if (folder / "result.json").exists():
                verify_result(folder)
                continue
            interrupted = args.output / "interrupted"
            interrupted.mkdir(exist_ok=True)
            folder.rename(interrupted / (name + "-" + str(time.time_ns())))
        folder.mkdir()
        payload = args.base if run["variant"] == "base" else args.head
        command = [
            "dotnet", str(args.controller), "--config", str(config), "--config", str(profile),
            "--scenario", "lazy-headers", "--profile", "loopback-poc",
            "--application.source.localFolder", str(payload),
            "--load.source.localFolder", str(args.wrapper), "--load.source.project", "", "--load.project", "",
            "--variable", f"pocDotnetRoot={args.runtime}", "--variable", f"wrkWrapperPublish={args.wrapper}",
            "--variable", f"access={run['access']}",
            "--variable", f"lazy={'true' if run['variant'] == 'on' else 'false'}",
            "--variable", f"headerMode={run['mode']}",
            "--variable", f"headerScript=https://raw.githubusercontent.com/dotnet/aspnetcore/{args.source}/src/Servers/Kestrel/perf/lazy-headers.wrk.lua",
            "--load.variables.warmup", str(args.warmup), "--load.variables.duration", str(args.duration),
            "--load.variables.connections", "16", "--load.variables.threads", "2", "--load.variables.timeout", "10",
        ]
        with (folder / "render.log").open("w") as log:
            subprocess.run(command + ["--debug", str(folder / "resolved.json")], stdout=log, stderr=subprocess.STDOUT, check=True)
        resolved = json.loads((folder / "resolved.json").read_text())
        assert resolved["Jobs"]["application"]["Sources"]["default"]["LocalFolder"] == str(payload)
        assert resolved["Jobs"]["load"]["Sources"]["default"]["LocalFolder"] == str(args.wrapper)
        assert resolved["Jobs"]["application"]["EnvironmentVariables"]["DOTNET_ROOT"] == str(args.runtime)
        assert f"-d {args.duration}s" in resolved["Jobs"]["load"]["Arguments"]
        record = {**run, "startedUtc": now(), "command": command}
        write_json(folder / "execution.json", record)
        identity = None
        deadline = time.monotonic() + 600
        with (folder / "controller.log").open("w") as log:
            process = subprocess.Popen(command + ["--json", str(folder / "result.json")], stdout=log, stderr=subprocess.STDOUT)
            try:
                while process.poll() is None:
                    if time.monotonic() > deadline:
                        raise TimeoutError(f"Crank run exceeded 600 seconds: {name}")
                    if identity is None:
                        for queued in read_json(agent + "/jobs"):
                            try:
                                job = read_json(agent + f"/jobs/{queued['id']}")
                            except urllib.error.HTTPError as error:
                                if error.code != 404:
                                    raise
                                continue
                            if job.get("executable") != "LazyHeadersApp" or not job.get("processId"):
                                continue
                            pid = job.get("activeProcessId") or job["processId"]
                            maps = pathlib.Path(f"/proc/{pid}/maps").read_text()
                            paths = {line.split(maxsplit=5)[-1] for line in maps.splitlines() if "/" in line}
                            loaded_core = next((pathlib.Path(p) for p in paths if p.endswith("/" + core)), None)
                            if loaded_core is not None:
                                runtime = next(p for p in paths if p.endswith("/libcoreclr.so"))
                                assert str(args.runtime) in runtime, runtime
                                assert digest(loaded_core) == hashes["base" if run["variant"] == "base" else "head"]
                                identity = {
                                    "timestampUtc": now(), "pid": pid, "corePath": str(loaded_core),
                                    "coreSha256": digest(loaded_core), "runtimePath": runtime,
                                    "runtimeSha256": digest(pathlib.Path(runtime)),
                                }
                                write_json(folder / "loaded-identity.json", identity)
                    time.sleep(1)
            finally:
                if process.poll() is None:
                    process.terminate()
                    process.wait(timeout=20)
        record.update(endedUtc=now(), exitCode=process.returncode)
        write_json(folder / "execution.json", record)
        assert process.returncode == 0, f"Controller failed: {name}"
        assert identity is not None, f"No loaded server identity: {name}"
        metrics = verify_result(folder)
        assert read_json(agent + "/jobs") == [], f"Jobs remain after cleanup: {name}"
        print(f"{name}: {metrics['http/rps/mean']} RPS; bad={metrics['http/requests/badresponses']}, socket-errors={metrics['http/requests/errors']}", flush=True)
    write_json(args.output / "completed.json", {"endedUtc": now(), "runs": len(plan)})


try:
    run_all()
except Exception:
    (args.output / ("runner-error-" + str(time.time_ns()) + ".txt")).write_text(traceback.format_exc())
    raise
