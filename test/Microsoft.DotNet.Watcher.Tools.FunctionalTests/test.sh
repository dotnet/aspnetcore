set -e

dotnet build

../../.build/dotnet/dotnet exec \
--depsfile bin/Debug/netcoreapp1.0/Microsoft.DotNet.Watcher.Tools.FunctionalTests.deps.json \
--runtimeconfig bin/Debug/netcoreapp1.0/Microsoft.DotNet.Watcher.Tools.FunctionalTests.runtimeconfig.json \
../../.build/dotnet-test-xunit/2.2.0-preview2-build1029/lib/netcoreapp1.0/dotnet-test-xunit.dll \
bin/Debug/netcoreapp1.0/Microsoft.DotNet.Watcher.Tools.FunctionalTests.dll \
$@