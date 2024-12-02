#!/usr/bin/env bash

set -euo pipefail

#
# variables
#

RESET="\033[0m"
RED="\033[0;31m"
YELLOW="\033[0;33m"
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
target_os_name=''
ci=false
binary_log=false
exclude_ci_binary_log=false
verbosity='minimal'
run_restore=''
run_build=true
run_pack=false
run_publish=false
run_tests=false
run_sign=false
build_all=false
build_deps=true
only_build_repo_tasks=false
build_repo_tasks=true
build_managed=''
build_native=''
build_nodejs=''
build_java=''
build_installers=''
build_projects=''
target_arch='x64'
configuration=''
runtime_source_feed=''
runtime_source_feed_key=''

if [ "$(uname)" = "Darwin" ]; then
    target_os_name='osx'
elif [ "$(uname)" = "FreeBSD" ]; then
    target_os_name='freebsd'
else
    target_os_name='linux'
fi

msbuild_args=()

#
# Functions
#
__usage() {
    echo "Usage: $(basename "${BASH_SOURCE[0]}") [options] [[--] <Arguments>...]

Arguments:
    <Arguments>...                    Arguments passed to the command. Variable number of arguments allowed.

Options:
    --configuration|-c                The build configuration (Debug, Release). Default=Debug
    --arch                            The CPU architecture to build for (x64, arm, arm64). Default=$target_arch
    --os-name                         The base runtime identifier to build for (linux, osx, linux-musl). Default=$target_os_name

    --[no-]restore                    Run restore.
    --[no-]build                      Compile projects. (Implies --no-restore)
    --[no-]pack                       Produce packages.
    --[no-]test                       Run tests.
    --[no-]publish                    Run publish.
    --[no-]sign                       Run code signing.

    --projects                        A list of projects to build. (Must be an absolute path.)
                                      Globbing patterns are supported, such as \"$(pwd)/**/*.csproj\".
    --no-build-deps                   Do not build project-to-project references and only build the specified project.
    --no-build-repo-tasks             Suppress building RepoTasks.
    --only-build-repo-tasks           Only build RepoTasks.

    --all                             Build all project types.
    --[no-]build-native               Build native projects (C, C++). Ignored in most cases i.e. with `dotnet msbuild`.
    --[no-]build-managed              Build managed projects (C#, F#, VB).
    --[no-]build-nodejs               Build NodeJS projects (TypeScript, JS).
    --[no-]build-java                 Build Java projects.
    --[no-]build-installers           Build installers.

    --ci                              Apply CI specific settings and environment variables.
    --binarylog|-bl                   Use a binary logger
    --excludeCIBinarylog              Don't output binary log by default in CI builds (short: -nobl).
    --verbosity|-v                    MSBuild verbosity: q[uiet], m[inimal], n[ormal], d[etailed], and diag[nostic]

    --runtime-source-feed             Additional feed that can be used when downloading .NET runtimes and SDKs
    --runtime-source-feed-key         Key for feed that can be used when downloading .NET runtimes and SDKs

Description:
    This build script installs required tools and runs an MSBuild command on this repository
    This script can be used to invoke various targets, such as targets to produce packages
    build projects, run tests, and generate code.
"

    if [[ "${1:-}" != '--no-exit' ]]; then
        exit 2
    fi
}

__error() {
    echo -e "${RED}error: $*${RESET}" 1>&2
}

__warn() {
    echo -e "${YELLOW}warning: $*${RESET}"
}

#
# main
#

while [[ $# -gt 0 ]]; do
    opt="$(echo "${1/#--/-}" | awk '{print tolower($0)}')"
    case "$opt" in
        -\?|-h|-help)
            __usage --no-exit
            exit 0
            ;;
        -configuration|-c)
            shift
            configuration="${1:-}"
            [ -z "$configuration" ] && __error "Missing value for parameter --configuration" && __usage
            ;;
        -arch)
            shift
            target_arch="${1:-}"
            [ -z "$target_arch" ] && __error "Missing value for parameter --arch" && __usage
            ;;
        -os-name|-osname)
            shift
            target_os_name="${1:-}"
            [ -z "$target_os_name" ] && __error "Missing value for parameter --os-name" && __usage
            ;;
        -restore|-r)
            run_restore=true
            ;;
        -no-restore|-norestore)
            run_restore=false
            ;;
        -build|-b)
            run_build=true
            ;;
        -no-build|-nobuild)
            run_build=false
            # --no-build implies --no-restore
            [ -z "$run_restore" ] && run_restore=false
            ;;
        -no-build-deps|-nobuilddeps)
            build_deps=false
            ;;
        -pack)
            run_pack=true
            ;;
        -no-pack|-nopack)
            run_pack=false
            ;;
        -publish)
            run_publish=true
            ;;
        -no-publish|-nopublish)
            run_publish=false
            ;;
        -test|-t)
            run_tests=true
            ;;
        -no-test|-notest)
            run_tests=false
            ;;
        -sign)
            run_sign=true
            ;;
        -no-sign|-nosign)
            run_sign=false
            ;;
        -projects)
            shift
            build_projects="${1:-}"
            [ -z "$build_projects" ] && __error "Missing value for parameter --projects" && __usage
            ;;
        -all)
            build_all=true
            ;;
        -build-managed|-buildmanaged)
            build_managed=true
            ;;
        -no-build-managed|-nobuildmanaged)
            build_managed=false
            ;;
        -build-nodejs|-buildnodejs)
            build_nodejs=true
            ;;
        -no-build-nodejs|-nobuildnodejs)
            build_nodejs=false
            ;;
        -build-java|-buildjava)
            build_java=true
            ;;
        -no-build-java|-nobuildjava)
            build_java=false
            ;;
        -build-native|-buildnative)
            build_native=true
            ;;
        -no-build-native|-nobuildnative)
            build_native=false
            ;;
        -build-installers|-buildinstallers)
            build_installers=true
            ;;
        -no-build-installers|-nobuildinstallers)
            build_installers=false
            ;;
        -no-build-repo-tasks|-nobuildrepotasks)
            build_repo_tasks=false
            ;;
        -only-build-repo-tasks|-onlybuildrepotasks)
            only_build_repo_tasks=true
            ;;
        -arch)
            shift
            target_arch="${1:-}"
            [ -z "$target_arch" ] && __error "Missing value for parameter --arch" && __usage
            ;;
        -ci)
            ci=true
            ;;
        -binarylog|-bl)
            binary_log=true
            ;;
        -excludeCIBinarylog|-nobl)
            exclude_ci_binary_log=true
            ;;
        -dotnet-runtime-source-feed|-dotnetruntimesourcefeed|-runtime-source-feed|-runtimesourcefeed)
            shift
            [ -z "${1:-}" ] && __error "Missing value for parameter --runtime-source-feed" && __usage
            runtime_source_feed="${1:-}"
            ;;
        -dotnet-runtime-source-feed-key|-dotnetruntimesourcefeedkey|-runtime-source-feed-key|-runtimesourcefeedkey)
            shift
            [ -z "${1:-}" ] && __error "Missing value for parameter --runtime-source-feed-key" && __usage
            runtime_source_feed_key="${1:-}"
            ;;
        *)
            msbuild_args[${#msbuild_args[*]}]="$1"
            ;;
    esac
    shift
done

if [ "$build_all" = true ]; then
    msbuild_args[${#msbuild_args[*]}]="-p:BuildAllProjects=true"
fi

if [ ! -z "$build_projects" ]; then
    [[ "$build_projects" == /* ]] || build_projects="$DIR/$build_projects"
    msbuild_args[${#msbuild_args[*]}]="-p:ProjectToBuild=$build_projects"
elif [ "$build_all" != true ] && [ -z "$build_managed$build_nodejs$build_java$build_native$build_installers" ]; then
    # This goal of this is to pick a sensible default for `build.sh` with zero arguments.
    # We believe the most common thing our contributors will work on is C#, so if no other build group was picked, build the C# projects.
    __warn "No default group of projects was specified, so building the 'managed' and its dependent subset of projects. Run ``build.sh --help`` for more details."
    build_managed=true
elif [ "$build_all" != true ] && [ -z "$build_managed" ] && ! [[ "$build_nodejs$build_java$build_native$build_installers" =~ "true" ]]; then
    # If only negative options were chosen, assume --build-managed.
    build_managed=true
fi

if [ "$build_deps" = false ]; then
    msbuild_args[${#msbuild_args[*]}]="-p:BuildProjectReferences=false"
fi

if [ "$build_managed" = true ] || ([ "$build_all" = true ] && [ "$build_managed" != false ]); then
    if [ -z "$build_nodejs" ]; then
        if [ -x "$(command -v node)" ]; then
            __warn "Building of C# project is enabled and has dependencies on NodeJS projects. Building of NodeJS projects is enabled since node is detected on PATH."
            __warn "Note that if you are running Source Build, building NodeJS projects will be disabled later on."
            build_nodejs=true
        else
            __warn "Building of NodeJS projects is disabled since node is not detected on Path and no BuildNodeJs or NoBuildNodeJs setting is set explicitly."
            build_nodejs=false
        fi
    fi

    if [ "$build_nodejs" = false ]; then
        __warn "Some managed projects depend on NodeJS projects. Building NodeJS is disabled so the managed projects will fallback to using the output from previous builds. The output may not be correct or up to date."
    fi
fi

# Only set these MSBuild properties if they were explicitly set by build parameters.
[ ! -z "$build_java" ] && msbuild_args[${#msbuild_args[*]}]="-p:BuildJava=$build_java"
[ ! -z "$build_native" ] && msbuild_args[${#msbuild_args[*]}]="-p:BuildNative=$build_native"
[ ! -z "$build_nodejs" ] && msbuild_args[${#msbuild_args[*]}]="-p:BuildNodeJSUnlessSourcebuild=$build_nodejs"
[ ! -z "$build_managed" ] && msbuild_args[${#msbuild_args[*]}]="-p:BuildManaged=$build_managed"
[ ! -z "$build_installers" ] && msbuild_args[${#msbuild_args[*]}]="-p:BuildInstallers=$build_installers"

# Run restore by default unless --no-restore or --no-build was specified.
[ -z "$run_restore" ] && run_restore=true

msbuild_args[${#msbuild_args[*]}]="-p:Restore=$run_restore"
msbuild_args[${#msbuild_args[*]}]="-p:Build=$run_build"
if [ "$run_build" = false ]; then
    msbuild_args[${#msbuild_args[*]}]="-p:NoBuild=true"
fi
msbuild_args[${#msbuild_args[*]}]="-p:Pack=$run_pack"
msbuild_args[${#msbuild_args[*]}]="-p:Publish=$run_publish"
msbuild_args[${#msbuild_args[*]}]="-p:Test=$run_tests"
msbuild_args[${#msbuild_args[*]}]="-p:Sign=$run_sign"

msbuild_args[${#msbuild_args[*]}]="-p:TargetArchitecture=$target_arch"
msbuild_args[${#msbuild_args[*]}]="-p:TargetOsName=$target_os_name"

if [ -z "$configuration" ]; then
    if [ "$ci" = true ]; then
        configuration='Release'
    else
        configuration='Debug'
    fi
fi
msbuild_args[${#msbuild_args[*]}]="-p:Configuration=$configuration"

# Set up additional runtime args
toolset_build_args=()
if [ ! -z "$runtime_source_feed$runtime_source_feed_key" ]; then
    runtimeFeedArg="/p:DotNetRuntimeSourceFeed=$runtime_source_feed"
    runtimeFeedKeyArg="/p:DotNetRuntimeSourceFeedKey=$runtime_source_feed_key"
    msbuild_args[${#msbuild_args[*]}]=$runtimeFeedArg
    msbuild_args[${#msbuild_args[*]}]=$runtimeFeedKeyArg
    toolset_build_args[${#toolset_build_args[*]}]=$runtimeFeedArg
    toolset_build_args[${#toolset_build_args[*]}]=$runtimeFeedKeyArg
fi

# Initialize global variables need to be set before the import of Arcade is imported
restore=$run_restore

# Disable node reuse - Workaround perpetual issues in node reuse and custom task assemblies
nodeReuse=false
export MSBUILDDISABLENODEREUSE=1

# Ensure passing neither --bl nor --nobl on CI avoids errors in tools.sh. This is needed because we set both variables
# to false by default i.e. they always exist. (We currently avoid binary logs but that is made visible in the YAML.)
if [[ "$ci" == true && "$exclude_ci_binary_log" == false ]]; then
    binary_log=true
fi

# increase file descriptor limit on macOS
if [ "$(uname)" = "Darwin" ]; then
    ulimit -n 10000
fi

# tools.sh expects the remaining arguments to be available via the $properties string array variable
# TODO: Remove when https://github.com/dotnet/source-build/issues/4337 is implemented.
properties=$msbuild_args

# Import Arcade
. "$DIR/common/tools.sh"

# Add default .binlog location if not already on the command line. tools.sh does not handle this; it just checks
# $binary_log, $ci and $exclude_ci_binary_log values for an error case.
if [[ "$binary_log" == true ]]; then
    found=false
    for arg in "${msbuild_args[@]}"; do
        opt="$(echo "${arg/#--/-}" | awk '{print tolower($0)}')"
        if [[ "$opt" == [-/]bl:* || "$opt" == [-/]binarylogger:* ]]; then
            found=true
            break
        fi
    done
    if [[ "$found" == false ]]; then
        msbuild_args[${#msbuild_args[*]}]="/bl:$log_dir/Build.binlog"
    fi
    toolset_build_args[${#toolset_build_args[*]}]="/bl:$log_dir/Build.repotasks.binlog"
elif [[ "$ci" == true ]]; then
    # Ensure the artifacts/log directory isn't empty to avoid warnings.
    touch "$log_dir/empty.log"
fi

# Capture MSBuild crash logs
export MSBUILDDEBUGPATH="$log_dir"

# Set this global property so Arcade will always initialize the toolset. The error message you get when you build on a clean machine
# with -norestore is not obvious about what to do to fix it. As initialization takes very little time, we think always initializing
# the toolset is a better default behavior.
_tmp_restore=$restore
restore=true

InitializeToolset

restore=$_tmp_restore=

if [ "$build_repo_tasks" = true ]; then
    MSBuild $_InitializeToolset \
        -p:RepoRoot="$repo_root" \
        -p:Projects="$DIR/tools/RepoTasks/RepoTasks.csproj" \
        -p:Configuration=Release \
        -p:Restore=$run_restore \
        -p:Build=true \
        -clp:NoSummary \
        ${toolset_build_args[@]+"${toolset_build_args[@]}"}
fi

if [ "$only_build_repo_tasks" != true ]; then
    # This incantation avoids unbound variable issues if msbuild_args is empty
    # https://stackoverflow.com/questions/7577052/bash-empty-array-expansion-with-set-u
    MSBuild $_InitializeToolset -p:RepoRoot="$repo_root" ${msbuild_args[@]+"${msbuild_args[@]}"}
fi

ExitWithExitCode 0
