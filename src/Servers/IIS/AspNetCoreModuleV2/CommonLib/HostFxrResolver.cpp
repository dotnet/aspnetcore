// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "HostFxrResolver.h"

#include <atlcomcli.h>
#include "fx_ver.h"
#include "debugutil.h"
#include "exceptions.h"
#include "HandleWrapper.h"
#include "Environment.h"
#include "StringHelpers.h"
#include "RegistryKey.h"
#include "ModuleHelpers.h"
#include "GlobalVersionUtility.h"

namespace fs = std::filesystem;

typedef INT(*get_hostfxr_path) (PWSTR buffer, size_t* bufferSize, PCWSTR assemblyPath);

void
HostFxrResolver::GetHostFxrParameters(
    const fs::path     &processPath,
    const fs::path     &applicationPhysicalPath,
    const std::wstring &applicationArguments,
    fs::path           &hostFxrDllPath,
    fs::path           &dotnetExePath,
    std::vector<std::wstring> &arguments,
    ErrorContext&      errorContext,
    HMODULE            aspNetCoreModule
)
{
    LOG_INFOF(L"Resolving hostfxr parameters for application: '%ls' arguments: '%ls' path: '%ls'",
        processPath.c_str(),
        applicationArguments.c_str(),
        applicationPhysicalPath.c_str());
    arguments = std::vector<std::wstring>();

    fs::path expandedProcessPath = Environment::ExpandEnvironmentVariables(processPath);
    const auto expandedApplicationArguments = Environment::ExpandEnvironmentVariables(applicationArguments);

    LOG_INFOF(L"Known dotnet.exe location: '%ls'", dotnetExePath.c_str());

    if (!expandedProcessPath.has_extension())
    {
        // The only executable extension inprocess supports
        expandedProcessPath.replace_extension(".exe");
    }
    else if (!ends_with(expandedProcessPath, L".exe", true))
    {
        throw InvalidOperationException(format(L"Process path '%s' doesn't have '.exe' extension.", expandedProcessPath.c_str()));
    }

    // call load dll and see what happens :)
    // TODO make sure we figure out a way to load this dll sxs
    // TODO error handling
    std::wstring modulePath = GlobalVersionUtility::GetModuleName(aspNetCoreModule);

    modulePath = GlobalVersionUtility::RemoveFileNameFromFolderPath(modulePath);

    auto moduleHandle = LoadLibrary(modulePath.append(L"\\nethost.dll").c_str());
    auto getHostfxrPath = ModuleHelpers::GetKnownProcAddress<get_hostfxr_path>(moduleHandle, "get_hostfxr_path");

    // Check if the absolute path is to dotnet or not.
    // Things to figure out:
    // 1. what do we do with the dotnet path? Any reason for us to care?
    //  need to care to make sure a newer shim still has fast load times with old handler
    //  just create a reverse function?
    // 2. Does this work with registry keys?
    if (IsDotnetExecutable(expandedProcessPath))
    {
        LOG_INFOF(L"Process path '%ls' is dotnet, treating application as portable", expandedProcessPath.c_str());

        if (applicationArguments.empty())
        {
            throw InvalidOperationException(L"Application arguments are empty.");
        }

        std::wstring test;
        size_t size = 500;
        test.resize(size);

        AppendArguments(
            expandedApplicationArguments,
            applicationPhysicalPath,
            arguments,
            true);

        // TODO need to check if args[0] is a dll or not.
        getHostfxrPath(test.data(), &size, arguments[0].c_str());

        test.resize(size); // todo maybe +1 for nullchar
        hostFxrDllPath = test;
        dotnetExePath = GetAbsolutePathToDotnetFromHostfxr(hostFxrDllPath);
        arguments.insert(arguments.begin(), dotnetExePath);
    }
    else
    {
        LOG_INFOF(L"Process path '%ls' is not dotnet, treating application as standalone or portable with bootstrapper", expandedProcessPath.c_str());

        auto executablePath = expandedProcessPath;

        if (executablePath.is_relative())
        {
            executablePath = applicationPhysicalPath / expandedProcessPath;
        }

        //
        // The processPath is a path to the application executable
        // like: C:\test\MyApp.Exe or MyApp.Exe
        // Check if the file exists, and if it does, get the parameters for a standalone application
        //
        if (is_regular_file(executablePath))
        {
            auto applicationDllPath = executablePath;
            applicationDllPath.replace_extension(".dll");

            LOG_INFOF(L"Checking application.dll at '%ls'", applicationDllPath.c_str());
            if (!is_regular_file(applicationDllPath))
            {
                errorContext.subStatusCode = 38;
                errorContext.errorReason = "Application DLL not found. Confirm the application dll is present. Single-file deployments are not supported in IIS.";
                errorContext.generalErrorType = "ANCM Application DLL Not Found";
                errorContext.detailedErrorContent = format("Application DLL was not found at %s.", to_multi_byte_string(applicationDllPath, CP_UTF8).c_str());
                throw InvalidOperationException(
                    format(L"Application DLL was not found at %s. Confirm the application dll is present. Single-file deployments are not supported in IIS.",
                        applicationDllPath.c_str()));
            }

            hostFxrDllPath = executablePath.parent_path() / "hostfxr.dll";
            LOG_INFOF(L"Checking hostfxr.dll at '%ls'", hostFxrDllPath.c_str());
            if (is_regular_file(hostFxrDllPath))
            {
                LOG_INFOF(L"hostfxr.dll found app local at '%ls', treating application as standalone", hostFxrDllPath.c_str());
                // For standalone apps we need .exe to be argv[0], dll would be discovered next to it
                arguments.push_back(executablePath);
            }
            else
            {
                LOG_INFOF(L"hostfxr.dll found app local at '%ls', treating application as portable with launcher", hostFxrDllPath.c_str());
                std::wstring test;
                size_t size = 500;
                test.resize(size);

                // passing "dotnet" here because we don't know where dotnet.exe should come from
                // so trying all fallbacks is appropriate
                // For portable with launcher apps we need dotnet.exe to be argv[0] and .dll be argv[1]

                getHostfxrPath(test.data(), &size, applicationDllPath.c_str());

                test.resize(size); // todo maybe +1 for nullchar
                hostFxrDllPath = test;

                dotnetExePath = GetAbsolutePathToDotnetFromHostfxr(hostFxrDllPath);
                arguments.push_back(dotnetExePath);
                arguments.push_back(applicationDllPath);
            }

            AppendArguments(
                expandedApplicationArguments,
                applicationPhysicalPath,
                arguments);
        }
        else
        {
            //
            // If the processPath file does not exist and it doesn't include dotnet.exe or dotnet
            // then it is an invalid argument.
            //
            throw InvalidOperationException(format(L"Executable was not found at '%s'", executablePath.c_str()));
        }
    }
}

void
HostFxrResolver::AppendArguments(
    const std::wstring &applicationArguments,
    const fs::path     &applicationPhysicalPath,
    std::vector<std::wstring> &arguments,
    bool expandDllPaths
)
{
    if (applicationArguments.empty())
    {
        return;
    }

    // don't throw while trying to expand arguments
    std::error_code ec;

    // Try to treat entire arguments section as a single path
    if (expandDllPaths)
    {
        fs::path argumentAsPath = applicationArguments;
        if (is_regular_file(argumentAsPath, ec))
        {
            LOG_INFOF(L"Treating '%ls' as a single path argument", applicationArguments.c_str());
            arguments.push_back(argumentAsPath);
            return;
        }

        if (argumentAsPath.is_relative())
        {
            argumentAsPath = applicationPhysicalPath / argumentAsPath;
            if (is_regular_file(argumentAsPath, ec))
            {
                LOG_INFOF(L"Converted argument '%ls' to '%ls'", applicationArguments.c_str(), argumentAsPath.c_str());
                arguments.push_back(argumentAsPath);
                return;
            }
        }
    }

    int argc = 0;
    auto pwzArgs = std::unique_ptr<LPWSTR[], LocalFreeDeleter>(CommandLineToArgvW(applicationArguments.c_str(), &argc));
    if (!pwzArgs)
    {
        throw InvalidOperationException(format(L"Unable parse command line arguments '%s'", applicationArguments.c_str()));
    }

    for (int intArgsProcessed = 0; intArgsProcessed < argc; intArgsProcessed++)
    {
        std::wstring argument = pwzArgs[intArgsProcessed];

        // Try expanding arguments ending in .dll to a full paths
        if (expandDllPaths && ends_with(argument, L".dll", true))
        {
            fs::path argumentAsPath = argument;
            if (argumentAsPath.is_relative())
            {
                argumentAsPath = applicationPhysicalPath / argumentAsPath;
                if (is_regular_file(argumentAsPath, ec))
                {
                    LOG_INFOF(L"Converted argument '%ls' to '%ls'", argument.c_str(), argumentAsPath.c_str());
                    argument = argumentAsPath;
                }
            }
        }

        arguments.push_back(argument);
    }
}

fs::path
HostFxrResolver::GetAbsolutePathToDotnetFromHostfxr(const fs::path& hostfxrPath)
{
    return hostfxrPath.parent_path().parent_path().parent_path().parent_path() / "dotnet.exe";
}

BOOL
HostFxrResolver::IsDotnetExecutable(const std::filesystem::path& dotnetPath)
{
    // TODO this probably should check the path is dotnet for program files.
    return ends_with(dotnetPath, L"dotnet.exe", true);
}
