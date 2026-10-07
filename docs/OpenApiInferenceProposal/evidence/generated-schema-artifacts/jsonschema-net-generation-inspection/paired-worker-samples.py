import statistics
import subprocess
import sys

command = sys.argv[1:]
compile_elapsed = []
compile_allocated = []
image_elapsed = []
image_allocated = []

def measure(operation):
    result = subprocess.run(
        command + ["--image-worker", operation],
        check=True,
        capture_output=True,
        text=True,
    )
    elapsed, allocated = result.stdout.strip().split(",")
    return int(elapsed), int(allocated)

for index in range(30):
    operations = ("compile", "image") if index % 2 == 0 else ("image", "compile")
    samples = {operation: measure(operation) for operation in operations}
    compile_sample = samples["compile"]
    image_sample = samples["image"]
    compile_elapsed.append(compile_sample[0])
    compile_allocated.append(compile_sample[1])
    image_elapsed.append(image_sample[0])
    image_allocated.append(image_sample[1])

for name, samples in (
    ("compile_elapsed", compile_elapsed),
    ("image_elapsed", image_elapsed),
    ("elapsed_delta", [compile - image for compile, image in zip(compile_elapsed, image_elapsed)]),
    ("compile_allocated", compile_allocated),
    ("image_allocated", image_allocated),
    ("allocated_delta", [compile - image for compile, image in zip(compile_allocated, image_allocated)]),
):
    print(f"{name}_median={statistics.median(samples):.0f}")
    print(f"{name}_mean={statistics.mean(samples):.0f}")
    print(f"{name}_stdev={statistics.stdev(samples):.0f}")
