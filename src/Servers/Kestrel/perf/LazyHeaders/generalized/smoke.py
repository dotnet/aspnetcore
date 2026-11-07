import argparse
import hashlib
import http.client
import json
import os
import pathlib
import socket
import subprocess
import time

parser = argparse.ArgumentParser()
for name in ["head", "base", "runtime", "output"]:
    parser.add_argument("--" + name, type=pathlib.Path, required=True)
args = parser.parse_args()
args.output.mkdir(parents=True, exist_ok=False)
records = []
names = ["Accept", "Accept-Language", "User-Agent", "X-Example", "X-Request-Nonce"]
prefixes = ["application/poc-", "en-x-poc-", "Mozilla/5.0 changing-user-agent-", "example-value-", "nonce-value-"]
cases = []
for sequence in [0, 1, 2]:
    cases.append([(n, p + f"{sequence:012d}") for n, p in zip(names, prefixes)])
cases.append([])
cases.append(cases[1] + [("Accept", "application/second")])
cases.append(cases[2] + [("x-example", "second-example")])
cases.append(cases[1] + [("X-Budget", "b" * 4000)])
cases.append([(n, "f" * 257) for n in names])
for variant in ["base", "off", "on"]:
    payload = args.base if variant == "base" else args.head
    expected_hash = hashlib.sha256((payload / "Microsoft.AspNetCore.Server.Kestrel.Core.dll").read_bytes()).hexdigest()
    for access in ["ignore", "read-one", "read-all", "enumerate"]:
        log_path = args.output / f"{variant}-{access}.log"
        with log_path.open("w") as log:
            command = [str(args.runtime / "dotnet"), str(payload / "LazyHeadersApp.dll"),
                       "--urls", "http://127.0.0.1:15401", "--access", access,
                       "--lazy", "true" if variant == "on" else "false"]
            process = subprocess.Popen(command, stdout=log, stderr=subprocess.STDOUT,
                                       env={**os.environ, "DOTNET_ROOT": str(args.runtime), "TMPDIR": "/tmp/lhp-7a143fff"})
            try:
                deadline = time.monotonic() + 30
                while "Application started. lazy=" not in log_path.read_text():
                    assert process.poll() is None, log_path.read_text()
                    assert time.monotonic() < deadline, log_path.read_text()
                    time.sleep(0.1)
                maps = pathlib.Path(f"/proc/{process.pid}/maps").read_text()
                loaded = next(pathlib.Path(line.split(maxsplit=5)[-1]) for line in maps.splitlines()
                              if line.endswith("/Microsoft.AspNetCore.Server.Kestrel.Core.dll"))
                assert loaded == payload / "Microsoft.AspNetCore.Server.Kestrel.Core.dll"
                assert hashlib.sha256(loaded.read_bytes()).hexdigest() == expected_hash
                with socket.create_connection(("127.0.0.1", 15401), timeout=30) as connection:
                    for index, values in enumerate(cases):
                        all_headers = [("Host", "localhost"), ("Content-Length", "4")] + values
                        request = ("POST / HTTP/1.1\r\n" + "".join(f"{n}: {v}\r\n" for n, v in all_headers) + "\r\nbody").encode("ascii")
                        for offset in range(0, len(request), 32):
                            connection.sendall(request[offset:offset + 32])
                        response = http.client.HTTPResponse(connection)
                        response.begin()
                        assert response.status == 200
                        assert response.read() == b"Hello, World!"
                        selected = []
                        if access == "read-one":
                            selected = [v for n, v in values if n.lower() == "user-agent"]
                        elif access == "read-all":
                            selected = [v for n, v in values if n.lower() in {n.lower() for n in names}]
                        elif access == "enumerate":
                            selected = [v for _, v in all_headers]
                        length = sum(len(v) for v in selected)
                        checksum = sum(sum(v.encode("ascii")) for v in selected)
                        assert int(response.getheader("X-Observed-Length")) == length
                        assert int(response.getheader("X-Observed-Checksum")) == checksum
                        records.append(dict(variant=variant, access=access, case=index,
                                            observedLength=length, checksum=checksum, coreSha256=expected_hash))
            finally:
                process.terminate()
                process.wait(timeout=20)
(args.output / "results.json").write_text(json.dumps(records, indent=2) + "\n")
print(f"{len(records)} real-socket requests passed, exact body/length/checksum; 12 processes, eight persistent requests each.")
