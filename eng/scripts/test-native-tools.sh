#!/bin/sh

set -eu

mkdir -p "$HOME"
cd /payload
test -s libaspnetcoretools.so

check_help()
{
    tool=$1
    expected_exit=$2
    expected_usage=$3
    exit_code=0
    output=$("./$tool" --help 2>&1) || exit_code=$?
    printf '%s\n' "$output"
    if [ "$exit_code" -ne "$expected_exit" ]; then
        echo "$tool --help returned $exit_code; expected $expected_exit." >&2
        exit 1
    fi
    printf '%s\n' "$output" | grep -F "$expected_usage"
    echo "PASS: $tool --help"
}

check_help dotnet-dev-certs 0 'Usage: dotnet dev-certs'
check_help dotnet-user-secrets 2 'Usage: dotnet user-secrets'
check_help dotnet-user-jwts 0 'Usage: dotnet user-jwts'

./dotnet-dev-certs https --export-path /tmp/native-abi.pfx --password native-abi-test
test -s /tmp/native-abi.pfx
./dotnet-dev-certs https --check
echo 'PASS: DevCertsCertificateRoundtrip'

./dotnet-user-secrets set NativeAbi passed --id native-abi-test
./dotnet-user-secrets list --id native-abi-test > /tmp/native-abi-secrets.txt
grep -Fx 'NativeAbi = passed' /tmp/native-abi-secrets.txt
echo 'PASS: UserSecretsRoundtrip'
