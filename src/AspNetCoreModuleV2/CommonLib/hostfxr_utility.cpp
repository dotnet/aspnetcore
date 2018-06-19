// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include <string>

namespace fs = std::experimental::filesystem;

//
// Runs a standalone appliction.
// The folder structure looks like this:
// Application/
//   hostfxr.dll
//   Application.exe
//   Application.dll
//   etc.
// We get the full path to hostfxr.dll and Application.dll and run hostfxr_main,
// passing in Application.dll.
// Assuming we don't need Application.exe as the dll is the actual application.
//
HRESULT
HOSTFXR_UTILITY::GetStandaloneHostfxrParameters(
        PCWSTR              pwzExeAbsolutePath, // includes .exe file extension.
        PCWSTR				pcwzApplicationPhysicalPath,
        PCWSTR              pcwzArguments,
        HANDLE              hEventLog,
        _Inout_ STRU*		pStruHostFxrDllLocation,
        _Out_ DWORD*		pdwArgCount,
        _Out_ BSTR**		ppwzArgv
)
{
    HRESULT hr = S_OK;

    const fs::path exePath(pwzExeAbsolutePath);

    if (!exePath.has_extension())
    {
        return false;
    }

    const fs::path physicalPath(pcwzApplicationPhysicalPath);
    const fs::path hostFxrLocation = physicalPath / "hostfxr.dll";

    if (!is_regular_file(hostFxrLocation))
    {
        fs::path runtimeConfigLocation = exePath;
        runtimeConfigLocation.replace_extension(L".runtimeconfig.json");

        if (!is_regular_file(runtimeConfigLocation))
        {
            EVENTLOG(hEventLog, INPROCESS_FULL_FRAMEWORK_APP, pcwzApplicationPhysicalPath, 0);
            return E_FAIL;
        }

        EVENTLOG(hEventLog, APPLICATION_EXE_NOT_FOUND, pcwzApplicationPhysicalPath, 0);
        return E_FAIL;
    }

    fs::path dllPath = exePath;
    dllPath.replace_extension(".dll");

    if (!is_regular_file(dllPath))
    {
        return E_FAIL;
    }

    auto arguments = std::wstring(dllPath) + L" " + pcwzArguments;

    if (FAILED(hr = pStruHostFxrDllLocation->Copy(hostFxrLocation.c_str())))
    {
        return hr;
    }

    return ParseHostfxrArguments(
        arguments.c_str(),
        pwzExeAbsolutePath,
        pcwzApplicationPhysicalPath,
        hEventLog,
        pdwArgCount,
        ppwzArgv);
}

BOOL
HOSTFXR_UTILITY::IsDotnetExecutable(const std::experimental::filesystem::path & dotnetPath)
{
    auto name = dotnetPath.filename();
    name.replace_extension("");
    return _wcsnicmp(name.c_str(), L"dotnet", 6) == 0;
}

HRESULT
HOSTFXR_UTILITY::GetHostFxrParameters(
    _In_ HANDLE         hEventLog,
    _In_ PCWSTR         pcwzProcessPath,
    _In_ PCWSTR         pcwzApplicationPhysicalPath,
    _In_ PCWSTR         pcwzArguments,
    _Inout_ STRU       *pStruHostFxrDllLocation,
    _Inout_ STRU       *pStruExeAbsolutePath,
    _Out_ DWORD        *pdwArgCount,
    _Out_ BSTR        **pbstrArgv
)
{
    HRESULT                     hr = S_OK;

    const fs::path applicationPhysicalPath = pcwzApplicationPhysicalPath;
    fs::path processPath = ExpandEnvironmentVariables(pcwzProcessPath);
    std::wstring arguments = ExpandEnvironmentVariables(pcwzArguments);

    if (processPath.is_relative())
    {
        processPath = applicationPhysicalPath / processPath;
    }

    // Check if the absolute path is to dotnet or not.
    if (IsDotnetExecutable(processPath))
    {
        //
        // The processPath ends with dotnet.exe or dotnet
        // like: C:\Program Files\dotnet\dotnet.exe, C:\Program Files\dotnet\dotnet, dotnet.exe, or dotnet.
        // Get the absolute path to dotnet. If the path is already an absolute path, it will return that path
        //
        // Make sure to append the dotnet.exe path correctly here (pass in regular path)?
        auto fullProcessPath = GetAbsolutePathToDotnet(processPath);
        if (!fullProcessPath.has_value())
        {
            return E_FAIL;
        }

        processPath = fullProcessPath.value();

        auto hostFxrPath = GetAbsolutePathToHostFxr(processPath, hEventLog);
        if (!hostFxrPath.has_value())
        {
            return E_FAIL;
        }

        if (FAILED(hr = HOSTFXR_UTILITY::ParseHostfxrArguments(
            arguments.c_str(),
            processPath.c_str(),
            pcwzApplicationPhysicalPath,
            hEventLog,
            pdwArgCount,
            pbstrArgv)))
        {
            return hr;
        }

        if (FAILED(hr = pStruHostFxrDllLocation->Copy(hostFxrPath->c_str())))
        {
            return hr;
        }

        if (FAILED(hr = pStruExeAbsolutePath->Copy(processPath.c_str())))
        {
            return hr;
        }
    }
    else
    {
        //
        // The processPath is a path to the application executable
        // like: C:\test\MyApp.Exe or MyApp.Exe
        // Check if the file exists, and if it does, get the parameters for a standalone application
        //
        if (is_regular_file(processPath))
        {
            if (FAILED(hr = GetStandaloneHostfxrParameters(
                processPath.c_str(),
                pcwzApplicationPhysicalPath,
                arguments.c_str(),
                hEventLog,
                pStruHostFxrDllLocation,
                pdwArgCount,
                pbstrArgv)))
            {
                return hr;
            }

            if (FAILED(hr = pStruExeAbsolutePath->Copy(processPath.c_str())))
            {
                return hr;
            }

        }
        else
        {
            //
            // If the processPath file does not exist and it doesn't include dotnet.exe or dotnet
            // then it is an invalid argument.
            //
            hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
            UTILITY::LogEventF(hEventLog, ASPNETCORE_EVENT_INVALID_PROCESS_PATH_LEVEL, ASPNETCORE_EVENT_INVALID_PROCESS_PATH, ASPNETCORE_EVENT_INVALID_PROCESS_PATH_MSG, processPath.c_str(), hr);
            return hr;
        }
    }

    return S_OK;
}

//
// Forms the argument list in HOSTFXR_PARAMETERS.
// Sets the ArgCount and Arguments.
// Arg structure:
// argv[0] = Path to exe activating hostfxr.
// argv[1] = L"exec"
// argv[2] = absolute path to dll.
//
HRESULT
HOSTFXR_UTILITY::ParseHostfxrArguments(
    PCWSTR              pwzArgumentsFromConfig,
    PCWSTR              pwzExePath,
    PCWSTR              pcwzApplicationPhysicalPath,
    HANDLE              hEventLog,
    _Out_ DWORD*        pdwArgCount,
    _Out_ BSTR**        pbstrArgv
)
{
    UNREFERENCED_PARAMETER(hEventLog); // TODO use event log to set errors.

    DBG_ASSERT(dwArgCount != NULL);
    DBG_ASSERT(pwzArgv != NULL);
    DBG_ASSERT(pwzExePath != NULL);

    HRESULT     hr = S_OK;
    INT         argc = 0;
    BSTR*       argv = NULL;
    LPWSTR*     pwzArgs = NULL;
    STRU        struTempPath;
    INT         intArgsProcessed = 0;

    // If we call CommandLineToArgvW with an empty string, argc is 5 for some interesting reason.
    // Protectively guard against this by check if the string is null or empty.
    if (pwzArgumentsFromConfig == NULL || wcscmp(pwzArgumentsFromConfig, L"") == 0)
    {
        hr = E_INVALIDARG;
        goto Finished;
    }

    pwzArgs = CommandLineToArgvW(pwzArgumentsFromConfig, &argc);

    if (pwzArgs == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Failure;
    }

    argv = new BSTR[argc + 1];

    argv[0] = SysAllocString(pwzExePath);
    if (argv[0] == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }
    // Try to convert the application dll from a relative to an absolute path
    // Don't record this failure as pwzArgs[0] may already be an absolute path to the dll.
    for (intArgsProcessed = 0; intArgsProcessed < argc; intArgsProcessed++)
    {
        DBG_ASSERT(pwzArgs[intArgsProcessed] != NULL);
        struTempPath.Copy(pwzArgs[intArgsProcessed]);
        if (struTempPath.EndsWith(L".dll"))
        {
            if (SUCCEEDED(UTILITY::ConvertPathToFullPath(pwzArgs[intArgsProcessed], pcwzApplicationPhysicalPath, &struTempPath)))
            {
                argv[intArgsProcessed + 1] = SysAllocString(struTempPath.QueryStr());
            }
            else
            {
                argv[intArgsProcessed + 1] = SysAllocString(pwzArgs[intArgsProcessed]);
            }
            if (argv[intArgsProcessed + 1] == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto Failure;
            }
        }
        else
        {
            argv[intArgsProcessed + 1] = SysAllocString(pwzArgs[intArgsProcessed]);
            if (argv[intArgsProcessed + 1] == NULL)
            {
                hr = E_OUTOFMEMORY;
                goto Failure;
            }
        }
    }

    *pbstrArgv = argv;
    *pdwArgCount = argc + 1;

    goto Finished;

Failure:
    if (argv != NULL)
    {
        // intArgsProcess - 1 here as if we fail to allocated the ith string
        // we don't want to free it.
        for (INT i = 0; i < intArgsProcessed - 1; i++)
        {
            SysFreeString(argv[i]);
        }
    }

    delete[] argv;

Finished:
    if (pwzArgs != NULL)
    {
        LocalFree(pwzArgs);
        DBG_ASSERT(pwzArgs == NULL);
    }
    return hr;
}

std::optional<fs::path>
HOSTFXR_UTILITY::GetAbsolutePathToDotnet(
     const fs::path & requestedPath
)
{
    //
    // If we are given an absolute path to dotnet.exe, we are done
    //
    if (is_regular_file(requestedPath))
    {
        return std::make_optional(requestedPath);
    }

    auto pathWithExe = requestedPath;
    pathWithExe.concat(L".exe");

    if (is_regular_file(pathWithExe))
    {
        return std::make_optional(pathWithExe);
    }

    // At this point, we are calling where.exe to find dotnet.
    // If we encounter any failures, try getting dotnet.exe from the
    // backup location.
    // Only do it if no path is specified
    if (!requestedPath.has_parent_path())
    {
        return std::nullopt;
    }

    const auto dotnetViaWhere = InvokeWhereToFindDotnet();
    if (dotnetViaWhere.has_value())
    {
        return dotnetViaWhere;
    }

    return GetAbsolutePathToDotnetFromProgramFiles();
}

std::optional<fs::path>
HOSTFXR_UTILITY::GetAbsolutePathToHostFxr(
    const fs::path & dotnetPath,
    HANDLE hEventLog
)
{
    std::vector<std::wstring> versionFolders;
    const auto hostFxrBase = dotnetPath.parent_path() / "host" / "fxr";

    if (!is_directory(hostFxrBase))
    {
        EVENTLOG(hEventLog, HOSTFXR_DIRECTORY_NOT_FOUND, hostFxrBase.c_str(), HRESULT_FROM_WIN32(ERROR_BAD_ENVIRONMENT));

        return std::nullopt;
    }

    auto searchPattern = std::wstring(hostFxrBase) + L"\\*";
    FindDotNetFolders(searchPattern.c_str(), versionFolders);

    if (versionFolders.empty())
    {
        EVENTLOG(hEventLog, HOSTFXR_DIRECTORY_NOT_FOUND, hostFxrBase.c_str(), HRESULT_FROM_WIN32(ERROR_BAD_ENVIRONMENT));

        return std::nullopt;
    }

    const auto highestVersion = FindHighestDotNetVersion(versionFolders);
    const auto hostFxrPath = hostFxrBase  / highestVersion / "hostfxr.dll";

    if (!is_regular_file(hostFxrPath))
    {
        EVENTLOG(hEventLog, HOSTFXR_DLL_NOT_FOUND, hostFxrPath.c_str(), HRESULT_FROM_WIN32(ERROR_FILE_INVALID));

        return std::nullopt;
    }

    return std::make_optional(hostFxrPath);
}

//
// Tries to call where.exe to find the location of dotnet.exe.
// Will check that the bitness of dotnet matches the current
// worker process bitness.
// Returns true if a valid dotnet was found, else false.
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
    HANDLE              hStdOutReadPipe = INVALID_HANDLE_VALUE;
    HANDLE              hStdOutWritePipe = INVALID_HANDLE_VALUE;
    LPWSTR              pwzDotnetName = NULL;
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
    BOOL                fProcessCreationResult = FALSE;
    BOOL                fResult = FALSE;
    std::optional<fs::path> result;

    // Set the security attributes for the read/write pipe
    securityAttributes.nLength = sizeof(securityAttributes);
    securityAttributes.lpSecurityDescriptor = NULL;
    securityAttributes.bInheritHandle = TRUE;

    // Create a read/write pipe that will be used for reading the result of where.exe
    if (!CreatePipe(&hStdOutReadPipe, &hStdOutWritePipe, &securityAttributes, 0))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }
    if (!SetHandleInformation(hStdOutReadPipe, HANDLE_FLAG_INHERIT, 0))
    {
        hr = ERROR_FILE_INVALID;
        goto Finished;
    }

    // Set the stdout and err pipe to the write pipes.
    startupInfo.cb = sizeof(startupInfo);
    startupInfo.dwFlags |= STARTF_USESTDHANDLES;
    startupInfo.hStdOutput = hStdOutWritePipe;
    startupInfo.hStdError = hStdOutWritePipe;

    // CreateProcess requires a mutable string to be passed to commandline
    // See https://blogs.msdn.microsoft.com/oldnewthing/20090601-00/?p=18083/
    pwzDotnetName = SysAllocString(L"\"where.exe\" dotnet.exe");
    if (pwzDotnetName == NULL)
    {
        goto Finished;
    }

    // Create a process to invoke where.exe
    fProcessCreationResult = CreateProcessW(NULL,
        pwzDotnetName,
        NULL,
        NULL,
        TRUE,
        CREATE_NO_WINDOW,
        NULL,
        NULL,
        &startupInfo,
        &processInformation
    );

    if (!fProcessCreationResult)
    {
        goto Finished;
    }

    // Wait for where.exe to return, waiting 2 seconds.
    if (WaitForSingleObject(processInformation.hProcess, 2000) != WAIT_OBJECT_0)
    {
        // Timeout occured, terminate the where.exe process and return.
        TerminateProcess(processInformation.hProcess, 2);
        hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
        goto Finished;
    }

    //
    // where.exe will return 0 on success, 1 if the file is not found
    // and 2 if there was an error. Check if the exit code is 1 and set
    // a new hr result saying it couldn't find dotnet.exe
    //
    if (!GetExitCodeProcess(processInformation.hProcess, &dwExitCode))
    {
        goto Finished;
    }

    //
    // In this block, if anything fails, we will goto our fallback of
    // looking in C:/Program Files/
    //
    if (dwExitCode != 0)
    {
        goto Finished;
    }

    // Where succeeded.
    // Reset file pointer to the beginning of the file.
    dwFilePointer = SetFilePointer(hStdOutReadPipe, 0, NULL, FILE_BEGIN);
    if (dwFilePointer == INVALID_SET_FILE_POINTER)
    {
        goto Finished;
    }

    //
    // As the call to where.exe succeeded (dotnet.exe was found), ReadFile should not hang.
    // TODO consider putting ReadFile in a separate thread with a timeout to guarantee it doesn't block.
    //
    if (!ReadFile(hStdOutReadPipe, pzFileContents, READ_BUFFER_SIZE, &dwNumBytesRead, NULL))
    {
        goto Finished;
    }

    if (dwNumBytesRead >= READ_BUFFER_SIZE)
    {
        // This shouldn't ever be this large. We could continue to call ReadFile in a loop,
        // however if someone had this many dotnet.exes on their machine.
        goto Finished;
    }

    hr = HRESULT_FROM_WIN32(GetLastError());
    if (FAILED(hr = struDotnetLocationsString.CopyA(pzFileContents, dwNumBytesRead)))
    {
        goto Finished;
    }

    // Check the bitness of the currently running process
    // matches the dotnet.exe found.
    if (!IsWow64Process(GetCurrentProcess(), &fIsWow64Process))
    {
        // Calling IsWow64Process failed
        goto Finished;
    }
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

    while (TRUE)
    {
        index = struDotnetLocationsString.IndexOf(L"\r\n", prevIndex);
        if (index == -1)
        {
            break;
        }
        if (FAILED(hr = struDotnetSubstring.Copy(&struDotnetLocationsString.QueryStr()[prevIndex], index - prevIndex)))
        {
            goto Finished;
        }
        // \r\n is two wchars, so add 2 here.
        prevIndex = index + 2;

        if (GetBinaryTypeW(struDotnetSubstring.QueryStr(), &dwBinaryType) &&
            fIsCurrentProcess64Bit == (dwBinaryType == SCS_64BIT_BINARY))
        {
            // The bitness of dotnet matched with the current worker process bitness.
            result = std::make_optional(struDotnetSubstring.QueryStr());
            fResult = TRUE;
            break;
        }
    }

Finished:

    if (hStdOutReadPipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(hStdOutReadPipe);
    }
    if (hStdOutWritePipe != INVALID_HANDLE_VALUE)
    {
        CloseHandle(hStdOutWritePipe);
    }
    if (processInformation.hProcess != INVALID_HANDLE_VALUE)
    {
        CloseHandle(processInformation.hProcess);
    }
    if (processInformation.hThread != INVALID_HANDLE_VALUE)
    {
        CloseHandle(processInformation.hThread);
    }
    if (pwzDotnetName != NULL)
    {
        SysFreeString(pwzDotnetName);
    }

    return result;
}

std::optional<fs::path>
HOSTFXR_UTILITY::GetAbsolutePathToDotnetFromProgramFiles()
{
    const auto programFilesDotnet = fs::path(ExpandEnvironmentVariables(L"%ProgramFiles%")) / "dotnet" / "dotnet.exe";
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
            // TODO using max instead of std::max works
            max_ver = max(max_ver, fx_ver);
        }
    }

    return max_ver.as_str();
}

VOID
HOSTFXR_UTILITY::FindDotNetFolders(
    _In_ PCWSTR pszPath,
    _Out_ std::vector<std::wstring> & pvFolders
)
{
    HANDLE handle = NULL;
    WIN32_FIND_DATAW data = { 0 };

    handle = FindFirstFileExW(pszPath, FindExInfoStandard, &data, FindExSearchNameMatch, NULL, 0);
    if (handle == INVALID_HANDLE_VALUE)
    {
        return;
    }

    do
    {
        std::wstring folder(data.cFileName);
        pvFolders.push_back(folder);
    } while (FindNextFileW(handle, &data));

    FindClose(handle);
}

std::wstring
HOSTFXR_UTILITY::ExpandEnvironmentVariables(const std::wstring & str)
{
    DWORD requestedSize = ExpandEnvironmentStringsW(str.c_str(), nullptr, 0);
    if (requestedSize == 0)
    {
        throw std::system_error(GetLastError(), std::system_category(), "ExpandEnvironmentVariables");
    }

    std::wstring expandedStr;
    do
    {
        expandedStr.resize(requestedSize);
        requestedSize = ExpandEnvironmentStringsW(str.c_str(), &expandedStr[0], requestedSize);
        if (requestedSize == 0)
        {
            throw std::system_error(GetLastError(), std::system_category(), "ExpandEnvironmentVariables");
        }
    } while (expandedStr.size() != requestedSize);

    // trim null character as ExpandEnvironmentStringsW returns size including null character
    expandedStr.resize(requestedSize - 1);

    return expandedStr;
}
