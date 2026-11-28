#!/usr/bin/env python3

import argparse
import asyncio
import aiohttp
import gzip
import hashlib
import os
import re
import shutil
import subprocess
import sys
import tarfile
import tempfile
import zstandard

from collections import deque
from functools import cmp_to_key

async def download_file(session, url, dest_path, max_retries=3, retry_delay=2, timeout=60, checksum=None):
    """Asynchronous file download with retries."""
    attempt = 0
    while attempt < max_retries:
        try:
            async with session.get(url, timeout=aiohttp.ClientTimeout(total=timeout)) as response:
                if response.status == 200:
                    with open(dest_path, "wb") as f:
                        content = await response.read()

                        # verify checksum if provided
                        if checksum:
                            sha256 = hashlib.sha256(content).hexdigest()
                            if sha256 != checksum:
                                raise Exception(f"SHA256 mismatch for {url}: expected {checksum}, got {sha256}")

                        f.write(content)
                    print(f"Downloaded {url} at {dest_path}")
                    return
                elif response.status >= 500 or response.status in (408, 429):
                    response.raise_for_status()
                else:
                    raise Exception(f"Failed to download {url}, Status Code: {response.status}")
        except (asyncio.CancelledError, asyncio.TimeoutError, aiohttp.ClientError) as e:
            print(f"Error downloading {url}: {type(e).__name__} - {e}. Retrying...")

        attempt += 1
        if attempt < max_retries:
            await asyncio.sleep(retry_delay)

    raise Exception(f"Failed to download {url} after {max_retries} attempts.")

async def download_deb_files_parallel(mirror, packages, tmp_dir):
    """Download .deb files in parallel."""
    os.makedirs(tmp_dir, exist_ok=True)

    tasks = []
    timeout = aiohttp.ClientTimeout(total=60)
    async with aiohttp.ClientSession(timeout=timeout) as session:
        for pkg, info in packages.items():
            filename = info.get("Filename")
            if filename:
                url = f"{mirror}/{filename}"
                dest_path = os.path.join(tmp_dir, os.path.basename(filename))
                tasks.append(asyncio.create_task(download_file(session, url, dest_path, checksum=info.get("SHA256"))))

        await asyncio.gather(*tasks)

async def download_package_index_parallel(mirror, arch, suites, check_sig, keyring):
    """Download package index files for specified suites and components entirely in memory."""
    timeout = aiohttp.ClientTimeout(total=60)

    async with aiohttp.ClientSession(timeout=timeout) as session:
        tasks = [fetch_suite_indexes(session, mirror, arch, suite, ["main", "universe"], check_sig, keyring) for suite in suites]
        # Let every task finish before the session closes so one failure doesn't
        # surface as noisy "Session is closed" errors from the others.
        suite_results = await gather_all(*tasks)

    merged_content = ""
    for results in suite_results:
        for result in results:
            if isinstance(result, str):
                if merged_content:
                    merged_content += "\n\n"
                merged_content += result

    return merged_content

async def fetch_url(session, url, max_retries=3, retry_delay=2):
    """Fetch a URL with retries on transient failures. Returns None on 404."""
    for attempt in range(max_retries):
        try:
            async with session.get(url) as response:
                if response.status == 200:
                    return await response.read()
                if response.status == 404:
                    return None
                if response.status < 500 and response.status not in (408, 429):
                    raise Exception(f"Failed to download {url}, Status Code: {response.status}")
                print(f"Error downloading {url}: Status Code {response.status}. Retrying...")
        except (asyncio.TimeoutError, aiohttp.ClientError) as e:
            print(f"Error downloading {url}: {type(e).__name__} - {e}. Retrying...")

        if attempt < max_retries - 1:
            await asyncio.sleep(retry_delay * (attempt + 1))

    raise Exception(f"Failed to download {url} after {max_retries} attempts.")

async def gather_all(*coroutines):
    """Run coroutines concurrently, letting all finish before raising the first failure."""
    results = await asyncio.gather(*coroutines, return_exceptions=True)
    for result in results:
        if isinstance(result, BaseException):
            raise result
    return results

async def fetch_suite_indexes(session, mirror, arch, suite, components, check_sig, keyring, max_attempts=5, retry_delay=10):
    """Fetch the package indexes for one suite.

    With signature checking, every index is verified against a single signed
    Release snapshot. Mirrors behind load balancers can serve different
    snapshots on consecutive requests while they sync, so a mismatch restarts
    the whole suite from a fresh signed Release.
    """
    if not check_sig:
        return await gather_all(*(fetch_unverified_index(session, mirror, arch, suite, component) for component in components))

    for attempt in range(1, max_attempts + 1):
        try:
            release_content = await fetch_release_file(session, mirror, suite, keyring)
            if release_content is None:
                # Suites such as Debian's <codename>-security live in a separate
                # archive, so the mirror may not carry them at all.
                print(f"Skipped suite: {mirror}/dists/{suite} (doesn't exist)")
                return []
            return await gather_all(*(fetch_verified_index(session, mirror, arch, suite, component, release_content) for component in components))
        except Exception as e:
            if attempt == max_attempts:
                raise
            print(f"Inconsistent metadata for {suite} (attempt {attempt}/{max_attempts}): {e}. Retrying with a fresh Release file...")
            await asyncio.sleep(retry_delay * attempt)

async def fetch_unverified_index(session, mirror, arch, suite, component):
    path = f"{component}/binary-{arch}/Packages.gz"
    url = f"{mirror}/dists/{suite}/{path}"

    compressed_data = await fetch_url(session, url)
    if compressed_data is None:
        print(f"Skipped index: {url} (doesn't exist)")
        return None

    print(f"Downloaded index: {url}")
    return gzip.decompress(compressed_data).decode('utf-8')

async def fetch_verified_index(session, mirror, arch, suite, component, release_content):
    """Fetch Packages.gz and verify it against the sha256 recorded in the signed Release file."""
    path = f"{component}/binary-{arch}/Packages.gz"
    expected_sha = find_release_checksum(release_content, path)
    if expected_sha is None:
        print(f"Skipped index: {mirror}/dists/{suite}/{path} (not listed in Release)")
        return None

    urls = []
    # by-hash paths are immutable, so they can't change between fetching the
    # Release file and fetching the index.
    if re.search(r'^Acquire-By-Hash:\s*yes\s*$', release_content, re.MULTILINE | re.IGNORECASE):
        urls.append(f"{mirror}/dists/{suite}/{component}/binary-{arch}/by-hash/SHA256/{expected_sha}")
    urls.append(f"{mirror}/dists/{suite}/{path}")

    for url in urls:
        compressed_data = await fetch_url(session, url)
        if compressed_data is None:
            continue

        sha256 = hashlib.sha256(compressed_data).hexdigest()
        if sha256 != expected_sha:
            raise Exception(f"SHA256 mismatch for {url}: expected {expected_sha}, got {sha256}")

        print(f"Downloaded index: {url}")
        print(f"Checksum verified for {path}")
        return gzip.decompress(compressed_data).decode('utf-8')

    raise Exception(f"Index {path} listed in Release for {suite} was not found on the mirror")

async def fetch_release_file(session, mirror, suite, keyring):
    """Fetch the Release file for a suite and verify its signature.

    Prefer the inline-signed InRelease, which carries content and signature in
    one file. The detached Release and Release.gpg pair can come from two
    different snapshots when the mirror updates between requests.

    Returns None when the mirror doesn't carry the suite.
    """
    inrelease = await fetch_url(session, f"{mirror}/dists/{suite}/InRelease")
    if inrelease is not None:
        with tempfile.NamedTemporaryFile() as inrelease_file:
            inrelease_file.write(inrelease)
            inrelease_file.flush()

            print(f"Verifying signature of {suite}/InRelease.")
            # Use gpgv rather than gpg for verification. gpgv verifies against a
            # fixed keyring without involving gpg-agent or keyboxd, which makes it
            # robust on hosts running GnuPG 2.4+ (e.g. Azure Linux) where
            # "gpg --keyring" routes through keyboxd and can fail.
            result = subprocess.run(gpgv_command(keyring) + ["--output", "-", inrelease_file.name], stdout=subprocess.PIPE, stderr=subprocess.PIPE)

        if result.returncode != 0:
            raise Exception(f"Signature verification failed: {result.stderr.decode('utf-8')}")

        print("Signature verified successfully.")
        return result.stdout.decode('utf-8')

    release = await fetch_url(session, f"{mirror}/dists/{suite}/Release")
    if release is None:
        return None

    release_gpg = await fetch_url(session, f"{mirror}/dists/{suite}/Release.gpg")
    if release_gpg is None:
        raise Exception(f"Release for {suite} has no InRelease or Release.gpg signature")

    with tempfile.NamedTemporaryFile() as release_file, tempfile.NamedTemporaryFile() as release_gpg_file:
        release_file.write(release)
        release_file.flush()
        release_gpg_file.write(release_gpg)
        release_gpg_file.flush()

        print(f"Verifying signature of {suite}/Release with Release.gpg.")
        result = subprocess.run(gpgv_command(keyring) + [release_gpg_file.name, release_file.name], stdout=subprocess.PIPE, stderr=subprocess.PIPE)

    if result.returncode != 0:
        raise Exception(f"Signature verification failed: {result.stderr.decode('utf-8')}")

    print("Signature verified successfully.")
    return release.decode('utf-8')

def gpgv_command(keyring):
    command = ["gpgv"]
    if keyring:
        command += ["--keyring", keyring]
    return command

def find_release_checksum(content, path):
    """Returns the sha256 checksum of the specified path in the Release file, or None."""

    # data looks like this:
    # <checksum>  <size>  <path>
    matches = re.findall(r'^ (\S*) +(\S*) +(\S*)$', content, re.MULTILINE)

    for entry in matches:
        # the file has both md5 and sha256 checksums, we want sha256 which has a length of 64
        if entry[2] == path and len(entry[0]) == 64:
            return entry[0]

    return None

def parse_debian_version(version):
    """Parse a Debian package version into epoch, upstream version, and revision."""
    match = re.match(r'^(?:(\d+):)?([^-]+)(?:-(.+))?$', version)
    if not match:
        raise ValueError(f"Invalid Debian version format: {version}")
    epoch, upstream, revision = match.groups()
    return int(epoch) if epoch else 0, upstream, revision or ""

def compare_upstream_version(v1, v2):
    """Compare upstream or revision parts using Debian rules."""
    def tokenize(version):
        tokens = re.split(r'([0-9]+|[A-Za-z]+)', version)
        return [int(x) if x.isdigit() else x for x in tokens if x]

    tokens1 = tokenize(v1)
    tokens2 = tokenize(v2)

    for token1, token2 in zip(tokens1, tokens2):
        if type(token1) == type(token2):
            if token1 != token2:
                return (token1 > token2) - (token1 < token2)
        else:
            return -1 if isinstance(token1, str) else 1

    return len(tokens1) - len(tokens2)

def compare_debian_versions(version1, version2):
    """Compare two Debian package versions."""
    epoch1, upstream1, revision1 = parse_debian_version(version1)
    epoch2, upstream2, revision2 = parse_debian_version(version2)

    if epoch1 != epoch2:
        return epoch1 - epoch2

    result = compare_upstream_version(upstream1, upstream2)
    if result != 0:
        return result

    return compare_upstream_version(revision1, revision2)

def resolve_dependencies(packages, aliases, desired_packages):
    """Recursively resolves dependencies for the desired packages."""
    resolved = []
    to_process = deque(desired_packages)

    while to_process:
        current = to_process.popleft()
        resolved_package = current if current in packages else aliases.get(current, [None])[0]

        if not resolved_package:
            print(f"Error: Package '{current}' was not found in the available packages.")
            sys.exit(1)

        if resolved_package not in resolved:
            resolved.append(resolved_package)

            deps = packages.get(resolved_package, {}).get("Depends", "")
            if deps:
                deps = [dep.split(' ')[0] for dep in deps.split(', ') if dep]
                for dep in deps:
                    if dep not in resolved and dep not in to_process and dep in packages:
                        to_process.append(dep)

    return resolved

def parse_package_index(content):
    """Parses the Packages.gz file and returns package information."""
    packages = {}
    aliases = {}
    entries = re.split(r'\n\n+', content)

    for entry in entries:
        fields = dict(re.findall(r'^(\S+): (.+)$', entry, re.MULTILINE))
        if "Package" in fields:
            package_name = fields["Package"]
            version = fields.get("Version")
            filename = fields.get("Filename")
            depends = fields.get("Depends")
            provides = fields.get("Provides", None)
            sha256 = fields.get("SHA256")

            # Only update if package_name is not in packages or if the new version is higher
            if package_name not in packages or compare_debian_versions(version, packages[package_name]["Version"]) > 0:
                packages[package_name] = {
                    "Version": version,
                    "Filename": filename,
                    "Depends": depends,
                    "SHA256": sha256
                }

                # Update aliases if package provides any alternatives
                if provides:
                    provides_list = [x.strip() for x in provides.split(",")]
                    for alias in provides_list:
                        # Strip version specifiers
                        alias_name = re.sub(r'\s*\(=.*\)', '', alias)
                        if alias_name not in aliases:
                            aliases[alias_name] = []
                        if package_name not in aliases[alias_name]:
                            aliases[alias_name].append(package_name)

    return packages, aliases

def install_packages(mirror, packages_info, aliases, tmp_dir, extract_dir, ar_tool, desired_packages):
    """Downloads .deb files and extracts them."""
    resolved_packages = resolve_dependencies(packages_info, aliases, desired_packages)
    print(f"Resolved packages (including dependencies): {resolved_packages}")

    packages_to_download = {}

    for pkg in resolved_packages:
        if pkg in packages_info:
            packages_to_download[pkg] = packages_info[pkg]

        if pkg in aliases:
            for alias in aliases[pkg]:
                if alias in packages_info:
                    packages_to_download[alias] = packages_info[alias]

    asyncio.run(download_deb_files_parallel(mirror, packages_to_download, tmp_dir))

    package_to_deb_file_map = {}
    for pkg in resolved_packages:
        pkg_info = packages_info.get(pkg)
        if pkg_info:
            deb_filename = pkg_info.get("Filename")
            if deb_filename:
                deb_file_path = os.path.join(tmp_dir, os.path.basename(deb_filename))
                package_to_deb_file_map[pkg] = deb_file_path

    for pkg in reversed(resolved_packages):
        deb_file = package_to_deb_file_map.get(pkg)
        if deb_file and os.path.exists(deb_file):
            extract_deb_file(deb_file, tmp_dir, extract_dir, ar_tool)

    print("All done!")

def extract_deb_file(deb_file, tmp_dir, extract_dir, ar_tool):
    """Extract .deb file contents"""

    os.makedirs(extract_dir, exist_ok=True)

    with tempfile.TemporaryDirectory(dir=tmp_dir) as tmp_subdir:
        result = subprocess.run([ar_tool, "t", os.path.abspath(deb_file)], cwd=tmp_subdir, check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE)

        tar_filename = None
        for line in result.stdout.decode().splitlines():
            if line.startswith("data.tar"):
                tar_filename = line.strip()
                break

        if not tar_filename:
            raise FileNotFoundError(f"Could not find 'data.tar.*' in {deb_file}.")

        tar_file_path = os.path.join(tmp_subdir, tar_filename)
        print(f"Extracting {tar_filename} from {deb_file}..")

        with open(tar_file_path, "wb") as outfile:
            subprocess.run([ar_tool, "p", os.path.abspath(deb_file), tar_filename], check=True, stdout=outfile, stderr=subprocess.PIPE)

        file_extension = os.path.splitext(tar_file_path)[1].lower()

        if file_extension == ".xz":
            mode = "r:xz"
        elif file_extension == ".gz":
            mode = "r:gz"
        elif file_extension == ".zst":
            # zstd is not supported by standard library yet
            decompressed_tar_path = tar_file_path.replace(".zst", "")
            with open(tar_file_path, "rb") as zst_file, open(decompressed_tar_path, "wb") as decompressed_file:
                dctx = zstandard.ZstdDecompressor()
                dctx.copy_stream(zst_file, decompressed_file)

            tar_file_path = decompressed_tar_path
            mode = "r"
        else:
            raise ValueError(f"Unsupported compression format: {file_extension}")

        with tarfile.open(tar_file_path, mode) as tar:
            tar.extractall(path=extract_dir, filter=_rootfs_extraction_filter)

def _rootfs_extraction_filter(member, dest_path):
    """Tarfile extraction filter based on the 'data' filter that additionally
    rewrites absolute-target symlinks/hardlinks into rootfs-relative paths.
    """
    if (member.issym() or member.islnk()) and os.path.isabs(member.linkname):
        link_dir = os.path.dirname(member.name)
        new_linkname = os.path.relpath(member.linkname.lstrip('/'),
                                       start=link_dir or '.')
        member = member.replace(linkname=new_linkname, deep=False)
    return tarfile.data_filter(member, dest_path)

def finalize_setup(rootfsdir):
    lib_dir = os.path.join(rootfsdir, 'lib')
    usr_lib_dir = os.path.join(rootfsdir, 'usr', 'lib')

    if os.path.exists(lib_dir):
        if os.path.islink(lib_dir):
            os.remove(lib_dir)
        else:
            os.makedirs(usr_lib_dir, exist_ok=True)

            for item in os.listdir(lib_dir):
                src = os.path.join(lib_dir, item)
                dest = os.path.join(usr_lib_dir, item)

                if os.path.isdir(src):
                    shutil.copytree(src, dest, dirs_exist_ok=True)
                else:
                    shutil.copy2(src, dest)

            shutil.rmtree(lib_dir)

    os.symlink(usr_lib_dir, lib_dir)

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Generate rootfs for .NET runtime on Debian-like OS")
    parser.add_argument("--arch", required=True, help="Architecture (e.g., amd64, loong64, etc.)")
    parser.add_argument("--rootfsdir", required=True, help="Destination directory.")
    parser.add_argument('--suite', required=True, action='append', help='Specify one or more repository suites to collect index data.')
    parser.add_argument("--mirror", required=True, help="Mirror (e.g., http://ftp.debian.org/debian-ports etc.)")
    parser.add_argument("--artool", required=False, default="ar", help="ar tool to extract debs (e.g., ar, llvm-ar etc.)")
    parser.add_argument("--force-check-gpg", required=False, action='store_true', help="Verify the packages against signatures in Release file.")
    parser.add_argument("--keyring", required=False, default='', help="Keyring file to check signature of Release file.")
    parser.add_argument("packages", nargs="+", help="List of package names to be installed.")

    args = parser.parse_args()

    DESIRED_PACKAGES = args.packages + [ # base packages
        "dpkg",
        "busybox",
        "libc-bin",
        "base-files",
        "base-passwd",
        "debianutils"
    ]

    print(f"Creating rootfs. rootfsdir: {args.rootfsdir}, arch: {args.arch}, suites: {args.suite}, mirror: {args.mirror}")

    check_sig = args.force_check_gpg
    if check_sig and not args.keyring:
        print("ERROR: --force-check-gpg requires --keyring to specify a keyring file for signature verification.")
        print("Install the appropriate keyring package (e.g., debian-ports-archive-keyring, ubuntu-archive-keyring)")
        print("or pass --skipsigcheck to build-rootfs.sh to disable signature checking.")
        sys.exit(1)

    package_index_content = asyncio.run(download_package_index_parallel(args.mirror, args.arch, args.suite, check_sig, args.keyring))

    packages_info, aliases = parse_package_index(package_index_content)

    with tempfile.TemporaryDirectory() as tmp_dir:
        install_packages(args.mirror, packages_info, aliases, tmp_dir, args.rootfsdir, args.artool, DESIRED_PACKAGES)

    finalize_setup(args.rootfsdir)
