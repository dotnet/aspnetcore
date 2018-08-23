// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "hostfxr_utility.h"

#include <atlcomcli.h>
#include "fx_ver.h"
#include "debugutil.h"
#include "exceptions.h"
#include "HandleWrapper.h"
#include "Environment.h"
#include "StringHelpers.h"

namespace fs = std::filesystem;

void
HOSTFXR_UTILITY::GetHostFxrParameters(
    const fs::path     &processPath,
    const fs::path     &applicationPhysicalPath,
    const std::wstring &applicationArguments,
    fs::path           &hostFxrDllPath,
    fs::path           &dotnetExePath,
    std::vector<std::wstring> &arguments
)
{
    LOG_INFOF("Resolving hostfxr parameters for application: '%S' arguments: '%S' path: '%S'",
        processPath.c_str(),
        applicationArguments.c_str(),
        applicationPhysicalPath.c_str());

    fs::path expandedProcessPath = Environment::ExpandEnvironmentVariables(processPath);
    const auto expandedApplicationArguments = Environment::ExpandEnvironmentVariables(applicationArguments);

    LOG_INFOF("Expanded hostfxr parameters for application: '%S' arguments: '%S'",
        expandedProcessPath.c_str(),
        expandedApplicationArguments.c_str());

    LOG_INFOF("Known dotnet.exe location: '%S'", dotnetExePath.c_str());

    if (!expandedProcessPath.has_extension())
    {
        // The only executable extension inprocess supports
        expandedProcessPath.replace_extension(".exe");
    }
    else if (!ends_with(expandedProcessPath, L".exe", true))
    {
        throw StartupParametersResolutionException(format(L"Process path '%s' doesn't have '.exe' extension.", expandedProcessPath.c_str()));
    }

    // Check if the absolute path is to dotnet or not.
    if (IsDotnetExecutable(expandedProcessPath))
    {
        LOG_INFOF("Process path '%S' is dotnet, treating application as portable", expandedProcessPath.c_str());

        if (dotnetExePath.empty())
        {
            dotnetExePath = GetAbsolutePathToDotnet(applicationPhysicalPath, expandedProcessPath);
        }

        hostFxrDllPath = GetAbsolutePathToHostFxr(dotnetExePath);

        ParseHostfxrArguments(
            expandedApplicationArguments,
            dotnetExePath,
            applicationPhysicalPath,
            arguments,
            true);
    }
    else
    {
        LOG_INFOF("Process path '%S' is not dotnet, treating application as standalone or portable with bootstrapper", expandedProcessPath.c_str());

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

            LOG_INFOF("Checking application.dll at %S", applicationDllPath.c_str());
            if (!is_regular_file(applicationDllPath))
            {
                throw StartupParametersResolutionException(format(L"Application .dll was not found at %s", applicationDllPath.c_str()));
            }

            hostFxrDllPath = executablePath.parent_path() / "hostfxr.dll";
            LOG_INFOF("Checking hostfxr.dll at %S", hostFxrDllPath.c_str());
            if (is_regular_file(hostFxrDllPath))
            {
                LOG_INFOF("hostfxr.dll found app local at '%S', treating application as standalone", hostFxrDllPath.c_str());
            }
            else
            {
                LOG_INFOF("hostfxr.dll found app local at '%S', treating application as portable with launcher", hostFxrDllPath.c_str());

                // passing "dotnet" here because we don't know where dotnet.exe should come from
                // so trying all fallbacks is appropriate
                if (dotnetExePath.empty())
                {
                    dotnetExePath = GetAbsolutePathToDotnet(applicationPhysicalPath, L"dotnet");
                }
                executablePath = dotnetExePath;
                hostFxrDllPath = GetAbsolutePathToHostFxr(dotnetExePath);
            }

            ParseHostfxrArguments(
                applicationDllPath.generic_wstring() + L" " + expandedApplicationArguments,
                executablePath,
                applicationPhysicalPath,
                arguments);
        }
        else
        {
            //
            // If the processPath file does not exist and it doesn't include dotnet.exe or dotnet
            // then it is an invalid argument.
            //
            throw StartupParametersResolutionException(format(L"Executable was not found at '%s'", executablePath.c_str()));
        }
    }
}

BOOL
HOSTFXR_UTILITY::IsDotnetExecutable(const std::filesystem::path & dotnetPath)
{
    return ends_with(dotnetPath, L"dotnet.exe", true);
}

void
HOSTFXR_UTILITY::ParseHostfxrArguments(
    const std::wstring &applicationArguments,
    const fs::path     &applicationExePath,
    const fs::path     &applicationPhysicalPath,
    std::vector<std::wstring> &arguments,
    bool expandDllPaths
)
{
    LOG_INFOF("Resolving hostfxr_main arguments application: '%S' arguments: '%S' path: %S", applicationExePath.c_str(), applicationArguments.c_str(), applicationPhysicalPath.c_str());

    arguments = std::vector<std::wstring>();
    arguments.push_back(applicationExePath);

    if (applicationArguments.empty())
    {
        throw StartupParametersResolutionException(L"Application arguments are empty.");
    }

    int argc = 0;
    auto pwzArgs = std::unique_ptr<LPWSTR[], LocalFreeDeleter>(CommandLineToArgvW(applicationArguments.c_str(), &argc));
    if (!pwzArgs)
    {
        throw StartupParametersResolutionException(format(L"Unable parse command line argumens '%s' or '%s'", applicationArguments.c_str()));
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
                if (exists(argumentAsPath))
                {
                    LOG_INFOF("Converted argument '%S' to %S", argument.c_str(), argumentAsPath.c_str());
                    argument = argumentAsPath;
                }
            }
        }

        arguments.push_back(argument);
    }

    for (size_t i = 0; i < arguments.size(); i++)
    {
        LOG_INFOF("Argument[%d] = %S", i, arguments[i].c_str());
    }
}

// The processPath ends with dotnet.exe or dotnet
// like: C:\Program Files\dotnet\dotnet.exe, C:\Program Files\dotnet\dotnet, dotnet.exe, or dotnet.
// Get the absolute path to dotnet. If the path is already an absolute path, it will return that path
fs::path
HOSTFXR_UTILITY::GetAbsolutePathToDotnet(
     const fs::path & applicationPath,
     const fs::path & requestedPath
)
{
    LOG_INFOF("Resolving absolute path to dotnet.exe from %S", requestedPath.c_str());

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
        LOG_INFOF("Found dotnet.exe at %S", processPath.c_str());

        return processPath;
    }

    // At this point, we are calling where.exe to find dotnet.
    // If we encounter any failures, try getting dotnet.exe from the
    // backup location.
    // Only do it if no path is specified
    if (requestedPath.has_parent_path())
    {
        LOG_INFOF("Absolute path to dotnet.exe was not found at %S", requestedPath.c_str());

        throw StartupParametersResolutionException(format(L"Could not find dotnet.exe at '%s'", processPath.c_str()));
    }

    const auto dotnetViaWhere = InvokeWhereToFindDotnet();
    if (dotnetViaWhere.has_value())
    {
        LOG_INFOF("Found dotnet.exe via where.exe invocation at %S", dotnetViaWhere.value().c_str());

        return dotnetViaWhere.value();
    }

    const auto programFilesLocation = GetAbsolutePathToDotnetFromProgramFiles();
    if (programFilesLocation.has_value())
    {
        LOG_INFOF("Found dotnet.exe in Program Files at %S", programFilesLocation.value().c_str());

        return programFilesLocation.value();
    }

    LOG_INFOF("dotnet.exe not found");
    throw StartupParametersResolutionException(format(
        L"Could not find dotnet.exe at '%s' or using the system PATH environment variable."
        " Check that a valid path to dotnet is on the PATH and the bitness of dotnet matches the bitness of the IIS worker process.",
        processPath.c_str()));
}

fs::path
HOSTFXR_UTILITY::GetAbsolutePathToHostFxr(
    const fs::path & dotnetPath
)
{
    std::vector<std::wstring> versionFolders;
    const auto hostFxrBase = dotnetPath.parent_path() / "host" / "fxr";

    LOG_INFOF("Resolving absolute path to hostfxr.dll from %S", dotnetPath.c_str());

    if (!is_directory(hostFxrBase))
    {
        throw StartupParametersResolutionException(format(L"Unable to find hostfxr directory at %s", hostFxrBase.c_str()));
    }

    FindDotNetFolders(hostFxrBase, versionFolders);

    if (versionFolders.empty())
    {
        throw StartupParametersResolutionException(format(L"Hostfxr directory '%s' doesn't contain any version subdirectories", hostFxrBase.c_str()));
    }

    const auto highestVersion = FindHighestDotNetVersion(versionFolders);
    const auto hostFxrPath = hostFxrBase  / highestVersion / "hostfxr.dll";

    if (!is_regular_file(hostFxrPath))
    {
        throw StartupParametersResolutionException(format(L"hostfxr.dll not found at '%s'", hostFxrPath.c_str()));
    }

    LOG_INFOF("hostfxr.dll located at %S", hostFxrPath.c_str());
    return hostFxrPath;
}

//
// Tries to call where.exe to find the location of dotnet.exe.
// Will check that the bitness of dotnet matches the current
// worker process bitness.
// Returns true if a valid dotnet was found, else false.R
//
std::optional<fs::path>
HOSTFXR_UTILITY::InvokeWhereToFindDotnet()
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
    BOOL                fIsWow64Process;
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

    LOG_INFO("Invoking where.exe to find dotnet.exe");

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

    LOG_INFOF("where.exe invocation returned: %S", struDotnetLocationsString.QueryStr());

    // Check the bitness of the currently running process
    // matches the dotnet.exe found.
    FINISHED_LAST_ERROR_IF (!IsWow64Process(GetCurrentProcess(), &fIsWow64Process));

    if (fIsWow64Process)
    {
        // 32 bit mode
        fIsCurrentProcess64Bit = FALSE;
    }
    else
    {
        // Check the SystemInfo to see if we are currently 32 or 64 bit.
        SYSTEM_INFO systemInfo;
        GetNativeSystemInfo(&systemInfo);
        fIsCurrentProcess64Bit = systemInfo.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64;
    }

    LOG_INFOF("Current process bitness type detected as isX64=%d", fIsCurrentProcess64Bit);

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

        LOG_INFOF("Processing entry %S", struDotnetSubstring.QueryStr());

        if (LOG_LAST_ERROR_IF(!GetBinaryTypeW(struDotnetSubstring.QueryStr(), &dwBinaryType)))
        {
            continue;
        }

        LOG_INFOF("Binary type %d", dwBinaryType);

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
HOSTFXR_UTILITY::GetAbsolutePathToDotnetFromProgramFiles()
{
    const auto programFilesDotnet = fs::path(Environment::ExpandEnvironmentVariables(L"%ProgramFiles%")) / "dotnet" / "dotnet.exe";
    return is_regular_file(programFilesDotnet) ? std::make_optional(programFilesDotnet) : std::nullopt;
}

std::wstring
HOSTFXR_UTILITY::FindHighestDotNetVersion(
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
HOSTFXR_UTILITY::FindDotNetFolders(
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
