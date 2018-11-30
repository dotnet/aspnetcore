#!/usr/bin/env bash

source="${BASH_SOURCE[0]}"

# resolve $source until the file is no longer a symlink
while [[ -h "$source" ]]; do
  scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"
  source="$(readlink "$source")"
  # if $source was a relative symlink, we need to resolve it relative to the path where the
  # symlink file was located
  [[ $source != /* ]] && source="$scriptroot/$source"
done
scriptroot="$( cd -P "$( dirname "$source" )" && pwd )"

help=false
restore=false
build=false
rebuild=false
test=false
pack=false
publish=false
integration_test=false
performance_test=false
sign=false
public=false
ci=false

warnaserror=true
nodereuse=true

projects=''
configuration='Debug'
prepare_machine=false
verbosity='minimal'
properties=''

while (($# > 0)); do
  lowerI="$(echo $1 | awk '{print tolower($0)}')"
  case $lowerI in
    --build)
      build=true
      shift 1
      ;;
    --ci)
      ci=true
      shift 1
      ;;
    --configuration)
      configuration=$2
      shift 2
      ;;
    --help)
      echo "Common settings:"
      echo "  --configuration <value>  Build configuration Debug, Release"
      echo "  --verbosity <value>      Msbuild verbosity (q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic])"
      echo "  --help                   Print help and exit"
      echo ""
      echo "Actions:"
      echo "  --restore                Restore dependencies"
      echo "  --build                  Build solution"
      echo "  --rebuild                Rebuild solution"
      echo "  --test                   Run all unit tests in the solution"
      echo "  --sign                   Sign build outputs"
      echo "  --publish                Publish artifacts (e.g. symbols)"
      echo "  --pack                   Package build outputs into NuGet packages and Willow components"
      echo ""
      echo "Advanced settings:"
      echo "  --solution <value>       Path to solution to build"
      echo "  --ci                     Set when running on CI server"
      echo "  --prepareMachine         Prepare machine for CI run"
      echo ""
      echo "Command line arguments not listed above are passed through to MSBuild."
      exit 0
      ;;
    --pack)
      pack=true
      shift 1
      ;;
    --preparemachine)
      prepare_machine=true
      shift 1
      ;;
    --rebuild)
      rebuild=true
      shift 1
      ;;
    --restore)
      restore=true
      shift 1
      ;;
    --sign)
      sign=true
      shift 1
      ;;
    --solution)
      solution=$2
      shift 2
      ;;
    --projects)
      projects=$2
      shift 2
      ;;
    --test)
      test=true
      shift 1
      ;;
    --integrationtest)
      integration_test=true
      shift 1
      ;;
    --performancetest)
      performance_test=true
      shift 1
      ;;
    --publish)
      publish=true
      shift 1
      ;;
    --verbosity)
      verbosity=$2
      shift 2
      ;;
    --warnaserror)
      warnaserror=$2
      shift 2
      ;;
    --nodereuse)
      nodereuse=$2
      shift 2
      ;;
      *)
      properties="$properties $1"
      shift 1
      ;;
  esac
done

. "$scriptroot/tools.sh"

if [[ -z $projects ]]; then
  projects="$repo_root/*.sln"
fi

InitializeTools

build_log="$log_dir/Build.binlog"

MSBuild "$toolset_build_proj" \
  /bl:"$build_log" \
  /p:Configuration=$configuration \
  /p:Projects="$projects" \
  /p:RepoRoot="$repo_root" \
  /p:Restore=$restore \
  /p:Build=$build \
  /p:Rebuild=$rebuild \
  /p:Test=$test \
  /p:Pack=$pack \
  /p:IntegrationTest=$integration_test \
  /p:PerformanceTest=$performance_test \
  /p:Sign=$sign \
  /p:Publish=$publish \
  /p:ContinuousIntegrationBuild=$ci \
  $properties

lastexitcode=$?

if [[ $lastexitcode != 0 ]]; then
  echo "Build failed (exit code '$lastexitcode'). See log: $build_log"
fi

ExitWithExitCode $lastexitcode
