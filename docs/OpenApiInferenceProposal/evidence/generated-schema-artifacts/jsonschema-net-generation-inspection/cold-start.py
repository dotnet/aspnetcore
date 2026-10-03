import statistics
import subprocess
import sys
import time

command = sys.argv[1:]
samples = []
for _ in range(30):
    start = time.perf_counter_ns()
    subprocess.run(command, check=True, stdout=subprocess.DEVNULL)
    samples.append(time.perf_counter_ns() - start)

print(f"count={len(samples)}")
print(f"median_ns={statistics.median(samples):.0f}")
print(f"mean_ns={statistics.mean(samples):.0f}")
print(f"stdev_ns={statistics.stdev(samples):.0f}")
