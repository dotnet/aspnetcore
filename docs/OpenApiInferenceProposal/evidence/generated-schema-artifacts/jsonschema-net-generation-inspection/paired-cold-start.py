import statistics
import subprocess
import sys
import time

separator = sys.argv.index("--")
baseline_command = sys.argv[1:separator]
generated_command = sys.argv[separator + 1:]
baseline_samples = []
generated_samples = []

def measure(command):
    start = time.perf_counter_ns()
    subprocess.run(command, check=True, stdout=subprocess.DEVNULL)
    return time.perf_counter_ns() - start

for index in range(30):
    if index % 2 == 0:
        baseline_samples.append(measure(baseline_command))
        generated_samples.append(measure(generated_command))
    else:
        generated_samples.append(measure(generated_command))
        baseline_samples.append(measure(baseline_command))

deltas = [
    generated - baseline
    for generated, baseline in zip(generated_samples, baseline_samples)
]

for name, samples in (
    ("baseline", baseline_samples),
    ("generated", generated_samples),
    ("delta", deltas),
):
    print(f"{name}_median_ns={statistics.median(samples):.0f}")
    print(f"{name}_mean_ns={statistics.mean(samples):.0f}")
    print(f"{name}_stdev_ns={statistics.stdev(samples):.0f}")
