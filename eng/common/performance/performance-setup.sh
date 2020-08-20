#!/usr/bin/env bash

source_directory=$BUILD_SOURCESDIRECTORY
core_root_directory=
baseline_core_root_directory=
architecture=x64
framework=net5.0
compilation_mode=tiered
repository=$BUILD_REPOSITORY_NAME
branch=$BUILD_SOURCEBRANCH
commit_sha=$BUILD_SOURCEVERSION
build_number=$BUILD_BUILDNUMBER
internal=false
compare=false
mono_dotnet=
kind="micro"
llvm=false
monointerpreter=false
monoaot=false
run_categories="Libraries Runtime"
csproj="src\benchmarks\micro\MicroBenchmarks.csproj"
configurations="CompliationMode=$compilation_mode RunKind=$kind"
run_from_perf_repo=false
use_core_run=true
use_baseline_core_run=true
using_mono=false
wasm_runtime_loc=
using_wasm=false

while (($# > 0)); do
  lowerI="$(echo $1 | awk '{print tolower($0)}')"
  case $lowerI in
    --sourcedirectory)
      source_directory=$2
      shift 2
      ;;
    --corerootdirectory)
      core_root_directory=$2
      shift 2
      ;;
    --baselinecorerootdirectory)
      baseline_core_root_directory=$2
      shift 2
      ;;
    --architecture)
      architecture=$2
      shift 2
      ;;
    --framework)
      framework=$2
      shift 2
      ;;
    --compilationmode)
      compilation_mode=$2
      shift 2
      ;;
    --repository)
      repository=$2
      shift 2
      ;;
    --branch)
      branch=$2
      shift 2
      ;;
    --commitsha)
      commit_sha=$2
      shift 2
      ;;
    --buildnumber)
      build_number=$2
      shift 2
      ;;
    --kind)
      kind=$2
      configurations="CompilationMode=$compilation_mode RunKind=$kind"
      shift 2
      ;;
    --runcategories)
      run_categories=$2
      shift 2
      ;;
    --csproj)
      csproj=$2
      shift 2
      ;;
    --internal)
      internal=true
      shift 1
      ;;
    --llvm)
      llvm=true
      shift 1
      ;;
    --monointerpreter)
      monointerpreter=true
      shift 1
      ;;
    --monoaot)
      monoaot=true
      shift 1
      ;;
    --monodotnet)
      mono_dotnet=$2
      shift 2
      ;;
    --wasm)
      wasm_runtime_loc=$2
      shift 2
      ;;
    --compare)
      compare=true
      shift 1
      ;;
    --configurations)
      configurations=$2
      shift 2
      ;;
    --help)
      echo "Common settings:"
      echo "  --corerootdirectory <value>    Directory where Core_Root exists, if running perf testing with --corerun"
      echo "  --architecture <value>         Architecture of the testing being run"
      echo "  --configurations <value>       List of key=value pairs that will be passed to perf testing infrastructure."
      echo "                                 ex: --configurations \"CompilationMode=Tiered OptimzationLevel=PGO\""
      echo "  --help                         Print help and exit"
      echo ""
      echo "Advanced settings:"
      echo "  --framework <value>            The framework to run, if not running in master"
      echo "  --compliationmode <value>      The compilation mode if not passing --configurations"
      echo "  --sourcedirectory <value>      The directory of the sources. Defaults to env:BUILD_SOURCESDIRECTORY"
      echo "  --repository <value>           The name of the repository in the <owner>/<repository name> format. Defaults to env:BUILD_REPOSITORY_NAME"
      echo "  --branch <value>               The name of the branch. Defaults to env:BUILD_SOURCEBRANCH"
      echo "  --commitsha <value>            The commit sha1 to run against. Defaults to env:BUILD_SOURCEVERSION"
      echo "  --buildnumber <value>          The build number currently running. Defaults to env:BUILD_BUILDNUMBER"
      echo "  --csproj                       The relative path to the benchmark csproj whose tests should be run. Defaults to src\benchmarks\micro\MicroBenchmarks.csproj"
      echo "  --kind <value>                 Related to csproj. The kind of benchmarks that should be run. Defaults to micro"
      echo "  --runcategories <value>        Related to csproj. Categories of benchmarks to run. Defaults to \"coreclr corefx\""
      echo "  --internal                     If the benchmarks are running as an official job."
      echo "  --monodotnet                   Pass the path to the mono dotnet for mono performance testing."
      echo "  --wasm                         Path to the unpacked wasm runtime pack."
      echo ""
      exit 0
      ;;
  esac
done

if [ "$repository" == "dotnet/performance" ] || [ "$repository" == "dotnet-performance" ]; then
    run_from_perf_repo=true
fi

if [ -z "$configurations" ]; then
    configurations="CompilationMode=$compilation_mode"
fi

if [ -z "$core_root_directory" ]; then
    use_core_run=false
fi

if [ -z "$baseline_core_root_directory" ]; then
    use_baseline_core_run=false
fi

payload_directory=$source_directory/Payload
performance_directory=$payload_directory/performance
workitem_directory=$source_directory/workitem
extra_benchmark_dotnet_arguments="--iterationCount 1 --warmupCount 0 --invocationCount 1 --unrollFactor 1 --strategy ColdStart --stopOnFirstError true"
perflab_arguments=
queue=Ubuntu.1804.Amd64.Open
creator=$BUILD_DEFINITIONNAME
helix_source_prefix="pr"

if [[ "$compare" == true ]]; then
  extra_benchmark_dotnet_arguments=
  perflab_arguments=

  # No open queues for arm64
  if [[ "$architecture" = "arm64" ]]; then
    echo "Compare not available for arm64"
    exit 1
  fi

  queue=Ubuntu.1804.Amd64.Tiger.Perf.Open
fi

if [[ "$internal" == true ]]; then
    perflab_arguments="--upload-to-perflab-container"
    helix_source_prefix="official"
    creator=
    extra_benchmark_dotnet_arguments=
    
    if [[ "$architecture" = "arm64" ]]; then
        queue=Ubuntu.1804.Arm64.Perf
    else
        queue=Ubuntu.1804.Amd64.Tiger.Perf
    fi
fi

if [[ "$mono_dotnet" != "" ]]; then
    configurations="$configurations LLVM=$llvm MonoInterpreter=$monointerpreter MonoAOT=$monoaot"
fi

if [[ "$wasm_runtime_loc" != "" ]]; then
    configurations="CompilationMode=wasm RunKind=micro"
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --category-exclusion-filter NoInterpreter NoWASM"
fi

if [[ "$monointerpreter" == "true" ]]; then
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --category-exclusion-filter NoInterpreter"
fi

common_setup_arguments="--channel master --queue $queue --build-number $build_number --build-configs $configurations --architecture $architecture"
setup_arguments="--repository https://github.com/$repository --branch $branch --get-perf-hash --commit-sha $commit_sha $common_setup_arguments"


# Get the tools section from the global.json.
# This grabs the LKG version number of dotnet and passes it to our scripts
dotnet_version=`cat global.json | python3 -c 'import json,sys;obj=json.load(sys.stdin);print(obj["tools"]["dotnet"])'`
setup_arguments="--dotnet-versions $dotnet_version $setup_arguments"


if [[ "$run_from_perf_repo" = true ]]; then
    payload_directory=
    workitem_directory=$source_directory
    performance_directory=$workitem_directory
    setup_arguments="--perf-hash $commit_sha $common_setup_arguments"
else
    git clone --branch master --depth 1 --quiet https://github.com/dotnet/performance $performance_directory
    
    docs_directory=$performance_directory/docs
    mv $docs_directory $workitem_directory
fi

if [[ "$wasm_runtime_loc" != "" ]]; then
    using_wasm=true
    wasm_dotnet_path=$payload_directory/dotnet-wasm
    mv $wasm_runtime_loc $wasm_dotnet_path
    extra_benchmark_dotnet_arguments="$extra_benchmark_dotnet_arguments --wasmMainJS \$HELIX_CORRELATION_PAYLOAD/dotnet-wasm/runtime-test.js --wasmEngine /home/helixbot/.jsvu/v8 --customRuntimePack \$HELIX_CORRELATION_PAYLOAD/dotnet-wasm"
fi

if [[ "$mono_dotnet" != "" ]]; then
    using_mono=true
    mono_dotnet_path=$payload_directory/dotnet-mono
    mv $mono_dotnet $mono_dotnet_path
fi

if [[ "$use_core_run" = true ]]; then
    new_core_root=$payload_directory/Core_Root
    mv $core_root_directory $new_core_root
fi

if [[ "$use_baseline_core_run" = true ]]; then
  new_baseline_core_root=$payload_directory/Baseline_Core_Root
  mv $baseline_core_root_directory $new_baseline_core_root
fi

ci=true

_script_dir=$(pwd)/eng/common
. "$_script_dir/pipeline-logging-functions.sh"

# Make sure all of our variables are available for future steps
Write-PipelineSetVariable -name "UseCoreRun" -value "$use_core_run" -is_multi_job_variable false
Write-PipelineSetVariable -name "UseBaselineCoreRun" -value "$use_baseline_core_run" -is_multi_job_variable false
Write-PipelineSetVariable -name "Architecture" -value "$architecture" -is_multi_job_variable false
Write-PipelineSetVariable -name "PayloadDirectory" -value "$payload_directory" -is_multi_job_variable false
Write-PipelineSetVariable -name "PerformanceDirectory" -value "$performance_directory" -is_multi_job_variable false
Write-PipelineSetVariable -name "WorkItemDirectory" -value "$workitem_directory" -is_multi_job_variable false
Write-PipelineSetVariable -name "Queue" -value "$queue" -is_multi_job_variable false
Write-PipelineSetVariable -name "SetupArguments" -value "$setup_arguments" -is_multi_job_variable false
Write-PipelineSetVariable -name "Python" -value "$python3" -is_multi_job_variable false
Write-PipelineSetVariable -name "PerfLabArguments" -value "$perflab_arguments" -is_multi_job_variable false
Write-PipelineSetVariable -name "ExtraBenchmarkDotNetArguments" -value "$extra_benchmark_dotnet_arguments" -is_multi_job_variable false
Write-PipelineSetVariable -name "BDNCategories" -value "$run_categories" -is_multi_job_variable false
Write-PipelineSetVariable -name "TargetCsproj" -value "$csproj" -is_multi_job_variable false
Write-PipelineSetVariable -name "RunFromPerfRepo" -value "$run_from_perf_repo" -is_multi_job_variable false
Write-PipelineSetVariable -name "Creator" -value "$creator" -is_multi_job_variable false
Write-PipelineSetVariable -name "HelixSourcePrefix" -value "$helix_source_prefix" -is_multi_job_variable false
Write-PipelineSetVariable -name "Kind" -value "$kind" -is_multi_job_variable false
Write-PipelineSetVariable -name "_BuildConfig" -value "$architecture.$kind.$framework" -is_multi_job_variable false
Write-PipelineSetVariable -name "Compare" -value "$compare" -is_multi_job_variable false
Write-PipelineSetVariable -name "MonoDotnet" -value "$using_mono" -is_multi_job_variable false
Write-PipelineSetVariable -name "WasmDotnet" -value "$using_wasm" -is_multi_job_variable false
