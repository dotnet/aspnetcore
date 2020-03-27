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

namespace fs = std::filesystem;

void
HostFxrResolver::GetHostFxrParameters(
    const fs::path     &processPath,
    const fs::path     &applicationPhysicalPath,
    const std::wstring &applicationArguments,
    fs::path           &hostFxrDllPath,
    fs::path           &dotnetExePath,
    std::vector<std::wstring> &arguments,
    ErrorContext&      errorContext
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

    // Check if the absolute path is to dotnet or not.
    if (IsDotnetExecutable(expandedProcessPath))
    {
        LOG_INFOF(L"Process path '%ls' is dotnet, treating application as portable", expandedProcessPath.c_str());

        if (applicationArguments.empty())
        {
            throw InvalidOperationException(L"Application arguments are empty.");
        }

        if (dotnetExePath.empty())
        {
            dotnetExePath = GetAbsolutePathToDotnet(applicationPhysicalPath, expandedProcessPath);
        }

        hostFxrDllPath = GetAbsolutePathToHostFxr(dotnetExePath);

        arguments.push_back(dotnetExePath);
        AppendArguments(
            expandedApplicationArguments,
            applicationPhysicalPath,
            arguments,
            true);
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
                errorContext.errorReason = "The app couldn't be found. Confirm the app's main DLL is present. Single-file deployments are not supported in IIS.";
                errorContext.generalErrorType = "Failed to locate ASP.NET Core app";
                errorContext.detailedErrorContent = format("Application was not found at %s.", to_multi_byte_string(applicationDllPath, CP_UTF8).c_str());
                throw InvalidOperationException(
                    format(L"The app couldn't be found at %s. Confirm the app's main DLL is present. Single-file deployments are not supported in IIS.",
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

                // passing "dotnet" here because we don't know where dotnet.exe should come from
                // so trying all fallbacks is appropriate
                if (dotnetExePath.empty())
                {
                    dotnetExePath = GetAbsolutePathToDotnet(applicationPhysicalPath, L"dotnet");
                }
                hostFxrDllPath = GetAbsolutePathToHostFxr(dotnetExePath);

                // For portable with launcher apps we need dotnet.exe to be argv[0] and .dll be argv[1]
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

BOOL
HostFxrResolver::IsDotnetExecutable(const std::filesystem::path& dotnetPath)
{
    std::wstring filename = dotnetPath.filename().wstring();
    return equals_ignore_case(filename, L"dotnet.exe");
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

// The processPath ends with dotnet.exe or dotnet
// like: C:\Program Files\dotnet\dotnet.exe, C:\Program Files\dotnet\dotnet, dotnet.exe, or dotnet.
// Get the absolute path to dotnet. If the path is already an absolute path, it will return that path
fs::path
HostFxrResolver::GetAbsolutePathToDotnet(
     const fs::path & applicationPath,
     const fs::path & requestedPath
)
{
    LOG_INFOF(L"Resolving absolute path to dotnet.exe from '%ls'", requestedPath.c_str());

    auto processPath = requestedPath;
    if (processPath.is_relative())
    {
        processPath = applicationPath / processPath;
    }

    //
    // If we are given an absolute path to dotnet.exe, we are done
    //
    if (is_regular_file(processPath))
    {
        LOG_INFOF(L"Found dotnet.exe at '%ls'", processPath.c_str());

        return processPath;
    }

    // At this point, we are calling where.exe to find dotnet.
    // If we encounter any failures, try getting dotnet.exe from the
    // backup location.
    // Only do it if no path is specified
    if (requestedPath.has_parent_path())
    {
        LOG_INFOF(L"Absolute path to dotnet.exe was not found at '%ls'", requestedPath.c_str());

        throw InvalidOperationException(format(L"Could not find dotnet.exe at '%s'", processPath.c_str()));
    }

    const auto dotnetViaWhere = InvokeWhereToFindDotnet();
    if (dotnetViaWhere.has_value())
    {
        LOG_INFOF(L"Found dotnet.exe via where.exe invocation at '%ls'", dotnetViaWhere.value().c_str());

        return dotnetViaWhere.value();
    }

    auto isWow64Process = Environment::IsRunning64BitProcess();

    std::wstring regKeySubSection;

    if (isWow64Process)
    {
        regKeySubSection = L"SOFTWARE\\WOW6432Node\\dotnet\\Setup\\InstalledVersions\\x64";
    }
    else
    {
        regKeySubSection = L"SOFTWARE\\dotnet\\Setup\\InstalledVersions\\x86";
    }

    const auto installationLocation = RegistryKey::TryGetString(
        HKEY_LOCAL_MACHINE,
        regKeySubSection,
        L"InstallLocation");

    if (installationLocation.has_value())
    {
        LOG_INFOF(L"InstallLocation registry key is set to '%ls'", installationLocation.value().c_str());

        auto const installationLocationDotnet = fs::path(installationLocation.value()) / "dotnet.exe";

        if (is_regular_file(installationLocationDotnet))
        {
            LOG_INFOF(L"Found dotnet.exe in InstallLocation at '%ls'", installationLocationDotnet.c_str());
            return installationLocationDotnet;
        }
    }

    const auto programFilesLocation = GetAbsolutePathToDotnetFromProgramFiles();
    if (programFilesLocation.has_value())
    {
        LOG_INFOF(L"Found dotnet.exe in Program Files at '%ls'", programFilesLocation.value().c_str());

        return programFilesLocation.value();
    }

    LOG_INFOF(L"dotnet.exe not found");
    throw InvalidOperationException(format(
        L"Could not find dotnet.exe at '%s' or using the system PATH environment variable."
        " Check that a valid path to dotnet is on the PATH and the bitness of dotnet matches the bitness of the IIS worker process.",
        processPath.c_str()));
}

fs::path
HostFxrResolver::GetAbsolutePathToHostFxr(
    const fs::path & dotnetPath
)
{
    std::vector<std::wstring> versionFolders;
    const auto hostFxrBase = dotnetPath.parent_path() / "host" / "fxr";

    LOG_INFOF(L"Resolving absolute path to hostfxr.dll from '%ls'", dotnetPath.c_str());

    if (!is_directory(hostFxrBase))
    {
        throw InvalidOperationException(format(L"Unable to find hostfxr directory at %s", hostFxrBase.c_str()));
    }

    FindDotNetFolders(hostFxrBase, versionFolders);

    if (versionFolders.empty())
    {
        throw InvalidOperationException(format(L"Hostfxr directory '%s' doesn't contain any version subdirectories", hostFxrBase.c_str()));
    }

    const auto highestVersion = FindHighestDotNetVersion(versionFolders);
    const auto hostFxrPath = hostFxrBase  / highestVersion / "hostfxr.dll";

    if (!is_regular_file(hostFxrPath))
    {
        throw InvalidOperationException(format(L"hostfxr.dll not found at '%s'", hostFxrPath.c_str()));
    }

    LOG_INFOF(L"hostfxr.dll located at '%ls'", hostFxrPath.c_str());
    return hostFxrPath;
}

//
// Tries to call where.exe to find the location of dotnet.exe.
// Will check that the bitness of dotnet matches the current
// worker process bitness.
// Returns true if a valid dotnet was found, else false.R
//
std::optional<fs::path>
HostFxrResolver::InvokeWhereToFindDotnet()
{
    HRESULT             hr = S_OK;
    // Arguments to call where.exe
    STARTUPINFOW        startupInfo = { 0 };
    PROCESS_INFORMATION processInformation = { 0 };
    SECURITY_ATTRIBUTES securityAttributes;

    CHAR                pzFileContents[READ_BUFFER_SIZE];
    HandleWrapper<InvalidHandleTraits>     hStdOutReadPipe;
    HandleWrapper<InvalidHandleTraits>     hStdOutWritePipe;
    HandleWrapper<InvalidHandleTraits>     hProcess;
    HandleWrapper<InvalidHandleTraits>     hThread;
    CComBSTR            pwzDotnetName = NULL;
    DWORD               dwFilePointer;
    BOOL                fIsCurrentProcess64Bit;
    DWORD               dwExitCode;
    STRU                struDotnetSubstring;
    STRU                struDotnetLocationsString;
    DWORD               dwNumBytesRead;
    DWORD               dwBinaryType;
    INT                 index = 0;
    INT                 prevIndex = 0;
    std::optional<fs::path> result;

    // Set the security attributes for the read/write pipe
    securityAttributes.nLength = sizeof(securityAttributes);
    securityAttributes.lpSecurityDescriptor = NULL;
    securityAttributes.bInheritHandle = TRUE;

    LOG_INFO(L"Invoking where.exe to find dotnet.exe");

    // Create a read/write pipe that will be used for reading the result of where.exe
    FINISHED_LAST_ERROR_IF(!CreatePipe(&hStdOutReadPipe, &hStdOutWritePipe, &securityAttributes, 0));
    FINISHED_LAST_ERROR_IF(!SetHandleInformation(hStdOutReadPipe, HANDLE_FLAG_INHERIT, 0));

    // Set the stdout and err pipe to the write pipes.
    startupInfo.cb = sizeof(startupInfo);
    startupInfo.dwFlags |= STARTF_USESTDHANDLES;
    startupInfo.hStdOutput = hStdOutWritePipe;
    startupInfo.hStdError = hStdOutWritePipe;

    // CreateProcess requires a mutable string to be passed to commandline
    // See https://blogs.msdn.microsoft.com/oldnewthing/20090601-00/?p=18083/
    pwzDotnetName = L"\"where.exe\" dotnet.exe";

    // Create a process to invoke where.exe
    FINISHED_LAST_ERROR_IF(!CreateProcessW(NULL,
        pwzDotnetName,
        NULL,
        NULL,
        TRUE,
        CREATE_NO_WINDOW,
        NULL,
        NULL,
        &startupInfo,
        &processInformation
    ));

    // Store handles into wrapper so they get closed automatically
    hProcess = processInformation.hProcess;
    hThread = processInformation.hThread;

    // Wait for where.exe to return
    WaitForSingleObject(processInformation.hProcess, INFINITE);

    //
    // where.exe will return 0 on success, 1 if the file is not found
    // and 2 if there was an error. Check if the exit code is 1 and set
    // a new hr result saying it couldn't find dotnet.exe
    //
    FINISHED_LAST_ERROR_IF (!GetExitCodeProcess(processInformation.hProcess, &dwExitCode));

    //
    // In this block, if anything fails, we will goto our fallback of
    // looking in C:/Program Files/
    //
    if (dwExitCode != 0)
    {
        FINISHED_IF_FAILED(E_FAIL);
    }

    // Where succeeded.
    // Reset file pointer to the beginning of the file.
    dwFilePointer = SetFilePointer(hStdOutReadPipe, 0, NULL, FILE_BEGIN);
    if (dwFilePointer == INVALID_SET_FILE_POINTER)
    {
        FINISHED_IF_FAILED(E_FAIL);
    }

    //
    // As the call to where.exe succeeded (dotnet.exe was found), ReadFile should not hang.
    // TODO consider putting ReadFile in a separate thread with a timeout to guarantee it doesn't block.
    //
    FINISHED_LAST_ERROR_IF (!ReadFile(hStdOutReadPipe, pzFileContents, READ_BUFFER_SIZE, &dwNumBytesRead, NULL));

    if (dwNumBytesRead >= READ_BUFFER_SIZE)
    {
        // This shouldn't ever be this large. We could continue to call ReadFile in a loop,
        // however if someone had this many dotnet.exes on their machine.
        FINISHED_IF_FAILED(E_FAIL);
    }

    FINISHED_IF_FAILED(struDotnetLocationsString.CopyA(pzFileContents, dwNumBytesRead));

    LOG_INFOF(L"where.exe invocation returned: '%ls'", struDotnetLocationsString.QueryStr());

    fIsCurrentProcess64Bit = Environment::IsRunning64BitProcess();

    LOG_INFOF(L"Current process bitness type detected as isX64=%d", fIsCurrentProcess64Bit);

    while (TRUE)
    {
        index = struDotnetLocationsString.IndexOf(L"\r\n", prevIndex);
        if (index == -1)
        {
            break;
        }

        FINISHED_IF_FAILED(struDotnetSubstring.Copy(&struDotnetLocationsString.QueryStr()[prevIndex], index - prevIndex));
        // \r\n is two wchars, so add 2 here.
        prevIndex = index + 2;

        LOG_INFOF(L"Processing entry '%ls'", struDotnetSubstring.QueryStr());

        if (LOG_LAST_ERROR_IF(!GetBinaryTypeW(struDotnetSubstring.QueryStr(), &dwBinaryType)))
        {
            continue;
        }

        LOG_INFOF(L"Binary type %d", dwBinaryType);

        if (fIsCurrentProcess64Bit == (dwBinaryType == SCS_64BIT_BINARY))
        {
            // The bitness of dotnet matched with the current worker process bitness.
            return std::make_optional(struDotnetSubstring.QueryStr());
        }
    }

    Finished:
    return result;
}

std::optional<fs::path>
HostFxrResolver::GetAbsolutePathToDotnetFromProgramFiles()
{
    const auto programFilesDotnet = fs::path(Environment::ExpandEnvironmentVariables(L"%ProgramFiles%")) / "dotnet" / "dotnet.exe";
    return is_regular_file(programFilesDotnet) ? std::make_optional(programFilesDotnet) : std::nullopt;
}

std::wstring
HostFxrResolver::FindHighestDotNetVersion(
    _In_ std::vector<std::wstring> & vFolders
)
{
    fx_ver_t max_ver(-1, -1, -1);
    for (const auto& dir : vFolders)
    {
        fx_ver_t fx_ver(-1, -1, -1);
        if (fx_ver_t::parse(dir, &fx_ver, false))
        {
            max_ver = max(max_ver, fx_ver);
        }
    }

    return max_ver.as_str();
}

VOID
HostFxrResolver::FindDotNetFolders(
    const std::filesystem::path &path,
    _Out_ std::vector<std::wstring> &pvFolders
)
{
    WIN32_FIND_DATAW data = {};
    const auto searchPattern = std::wstring(path) + L"\\*";
    HandleWrapper<FindFileHandleTraits> handle = FindFirstFileExW(searchPattern.c_str(), FindExInfoStandard, &data, FindExSearchNameMatch, nullptr, 0);
    if (handle == INVALID_HANDLE_VALUE)
    {
        LOG_LAST_ERROR();
        return;
    }

    do
    {
        pvFolders.emplace_back(data.cFileName);
    } while (FindNextFileW(handle, &data));
}
