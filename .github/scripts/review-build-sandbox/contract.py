"""Trusted profiles and exact command contract shared by execution and ingestion."""

ROOT = "/var/lib/review-build-sandbox"
PROFILES = ({
    "id": "response-caching",
    "changed_root": "src/Middleware/ResponseCaching/",
    "project": "src/Middleware/ResponseCaching/test/Microsoft.AspNetCore.ResponseCaching.Tests.csproj",
    "assembly": "Microsoft.AspNetCore.ResponseCaching.Tests",
},)


def commands(profile):
    common = [
        "bash", "./eng/build.sh", "--projects", ROOT + "/work/" + profile["project"],
        "--configuration", "Debug", "--no-build-native", "--no-build-nodejs",
        "--no-build-java", "--no-build-installers", "--msbuildMultiThreaded", "false",
    ]
    return {
        "bootstrap": ["bash", "-c", "source ./activate.sh && bash ./eng/common/dotnet.sh --info"],
        "sdk": ["bash", "-c", "source ./activate.sh && dotnet --version"],
        "restore": common + ["--restore", "--no-build", "--no-test"],
        "build": common + ["--no-restore", "--build", "--no-test"],
        "test": common + ["--no-restore", "--no-build", "--test"],
    }


def validate_test_classes(tests, expected_assembly):
    prefix = expected_assembly + "."
    sentinel = ("AlwaysTestTests.SuccessfulTests", "GuaranteedUnquarantinedTest")
    if any(not test.get("type", "").startswith(prefix)
           and (test.get("type"), test.get("method")) != sentinel for test in tests):
        raise RuntimeError("Unexpected test class")
    # The repository's shared always-pass sentinel cannot establish product execution.
    if not any(test.get("type", "").startswith(prefix) and test.get("result") in ("Pass", "Fail")
               for test in tests):
        raise RuntimeError("No executed product tests")
