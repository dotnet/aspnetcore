versionSuffix=$1
dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
projects=(
    ./src/Microsoft.AspNetCore.NodeServices
    ./src/Microsoft.AspNetCore.SpaServices
    ./src/Microsoft.AspNetCore.AngularServices
    ./src/Microsoft.AspNetCore.ReactServices
)

if [ -z "$versionSuffix" ]; then
    echo "Usage: pack-local.sh <versionsuffix>"
    echo "Example: pack-local.sh beta-000001"
    exit 1
fi

pushd $dir > /dev/null

for proj in "${projects[@]}"; do
    dotnet pack $proj --version-suffix $versionSuffix -o ./artifacts/
done

popd > /dev/null
