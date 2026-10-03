import statistics
import subprocess
import sys

command = sys.argv[1:]
elapsed_samples = []
allocation_samples = []
for _ in range(30):
    result = subprocess.run(command, check=True, capture_output=True, text=True)
    elapsed, allocated = result.stdout.strip().split(",")
    elapsed_samples.append(int(elapsed))
    allocation_samples.append(int(allocated))

print(f"count={len(elapsed_samples)}")
print(f"elapsed_median_ns={statistics.median(elapsed_samples):.0f}")
print(f"elapsed_mean_ns={statistics.mean(elapsed_samples):.0f}")
print(f"elapsed_stdev_ns={statistics.stdev(elapsed_samples):.0f}")
print(f"allocated_median_bytes={statistics.median(allocation_samples):.0f}")
print(f"allocated_mean_bytes={statistics.mean(allocation_samples):.0f}")
print(f"allocated_stdev_bytes={statistics.stdev(allocation_samples):.0f}")
