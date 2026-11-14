import argparse
import csv
import datetime
import json
import pathlib
import statistics

parser = argparse.ArgumentParser()
parser.add_argument("root", type=pathlib.Path)
parser.add_argument("--minimum-repetitions", type=int, default=5)
args = parser.parse_args()
root = args.root
inputs = json.loads((root / "inputs.json").read_text())
plan = json.loads((root / "plan.json").read_text())
completed = json.loads((root / "completed.json").read_text())
assert completed["runs"] == len(plan)
assert inputs["measurementSeconds"] == 30, "This analysis defines a 24-second interior of a 30-second load"
rows = []


def stamp(value):
    return datetime.datetime.fromisoformat(value.replace("Z", "+00:00"))


def spread(values):
    return dict(
        count=len(values), median=statistics.median(values), minimum=min(values),
        maximum=max(values), mean=statistics.mean(values),
        standard_deviation=statistics.stdev(values) if len(values) > 1 else None,
    ) if values else None


for index, run in enumerate(plan):
    folder = root / f"{index + 1:03d}-{run['variant']}-{run['access']}-{run['mode']}"
    result = json.loads((folder / "result.json").read_text())
    assert result["returnCode"] == 0
    jobs = result["jobResults"]["jobs"]
    load = jobs["load"]["results"]
    execution = json.loads((folder / "execution.json").read_text())
    identity = json.loads((folder / "loaded-identity.json").read_text())
    for key in ["repetition", "access", "mode", "variant"]:
        assert execution[key] == run[key]
    assert identity["coreSha256"] == inputs["coreSha256"]["base" if run["variant"] == "base" else "head"]
    samples = [sample for group in jobs["load"]["measurements"] for sample in group]
    end = min(stamp(s["timestamp"]) for s in samples if s["name"] == "http/rps/mean")
    # Approximate interior relative to wrapper completion; never request-normalize.
    start = end - datetime.timedelta(seconds=27)
    stop = end - datetime.timedelta(seconds=3)
    row = {**run, "run": folder.name, "rps": load["http/rps/mean"], "requests": load["http/requests"],
           "bad_responses": load["http/requests/badresponses"], "socket_errors": load["http/requests/errors"]}
    assert row["rps"] > 0 and row["requests"] > 0 and row["bad_responses"] == 0
    for percentile in ["50", "75", "90", "99"]:
        row[f"latency_p{percentile}_ms"] = load[f"http/latency/{percentile}"]
        assert row[f"latency_p{percentile}_ms"] > 0
    for job, prefix in [("application", "server"), ("load", "client")]:
        values = [s for group in jobs[job]["measurements"] for s in group
                  if start <= stamp(s["timestamp"]) <= stop]
        for metric, label in [
            ("runtime-counter/alloc-rate", "allocation_bytes_per_second"),
            ("runtime-counter/time-in-gc", "time_in_gc_percent"),
            ("runtime-counter/gen-0-gc-count", "gen0_per_second"),
            ("runtime-counter/gen-1-gc-count", "gen1_per_second"),
            ("runtime-counter/gen-2-gc-count", "gen2_per_second"),
            ("benchmarks/cpu/raw", "cpu_core_percent"),
            ("benchmarks/cpu/global", "global_cpu_percent"),
            ("benchmarks/working-set", "working_set_mb"),
        ]:
            selected = [float(s["value"]) for s in values if s["name"] == metric]
            row[f"{prefix}_{label}_sample_count"] = len(selected)
            row[f"{prefix}_{label}_mean"] = statistics.mean(selected) if selected else None
    row.update(trimmed_window_start=start.isoformat(), trimmed_window_end=stop.isoformat())
    assert row["server_allocation_bytes_per_second_sample_count"] >= 15
    rows.append(row)

with (root / "derived-runs.csv").open("w", newline="") as file:
    writer = csv.DictWriter(file, fieldnames=rows[0].keys(), lineterminator="\n")
    writer.writeheader()
    writer.writerows(rows)
summary = []
paired = []
for access, mode in sorted({(r["access"], r["mode"]) for r in rows}):
    for variant in ["base", "off", "on"]:
        group = [r for r in rows if (r["access"], r["mode"], r["variant"]) == (access, mode, variant)]
        assert len(group) >= args.minimum_repetitions
        item = dict(access=access, mode=mode, variant=variant, repetitions=len(group),
                    requests=sum(r["requests"] for r in group),
                    bad_responses=sum(r["bad_responses"] for r in group),
                    socket_errors=sum(r["socket_errors"] for r in group))
        for key in ["rps", "latency_p99_ms", "server_allocation_bytes_per_second_mean",
                    "server_gen0_per_second_mean", "server_gen1_per_second_mean", "server_gen2_per_second_mean",
                    "server_cpu_core_percent_mean", "client_cpu_core_percent_mean",
                    "server_time_in_gc_percent_mean", "server_working_set_mb_mean"]:
            item[key] = spread([r[key] for r in group if r[key] is not None])
        summary.append(item)
    base = {r["repetition"]: r for r in rows if (r["access"], r["mode"], r["variant"]) == (access, mode, "base")}
    for variant in ["off", "on"]:
        group = [r for r in rows if (r["access"], r["mode"], r["variant"]) == (access, mode, variant)]
        paired.append(dict(access=access, mode=mode, variant=variant,
                           rps_percent_change=spread([(r["rps"] / base[r["repetition"]]["rps"] - 1) * 100 for r in group])))
document = {
    "source": inputs["source"], "runs": len(rows), "requests": sum(r["requests"] for r in rows),
    "badResponses": sum(r["bad_responses"] for r in rows), "socketErrors": sum(r["socket_errors"] for r in rows),
    "method": "All predetermined cases retained, including socket errors; no outlier filtering. "
              "Approximate timestamp window from 27 to 3 seconds before first wrapper RPS event. "
              "Allocation rate units B/s, no bytes/request inference; missing samples null, not zero.",
    "groups": summary, "paired": paired,
}
(root / "derived-summary.json").write_text(json.dumps(document, indent=2) + "\n")
print(json.dumps(document, indent=2))
