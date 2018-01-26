// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

HOSTFXR_UTILITY::HOSTFXR_UTILITY()
{
}

HOSTFXR_UTILITY::~HOSTFXR_UTILITY()
{
}

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
    PCWSTR              pwzExePath,
    ASPNETCORE_CONFIG *pConfig
)
{
    HRESULT             hr = S_OK;
    STRU                struDllPath;
    STRU                struArguments;
    DWORD               dwPosition;

    if (FAILED(hr))
    {
        goto Finished;
    }

    if (FAILED(hr = struDllPath.Copy(pwzExePath)))
    {
        goto Finished;
    }

    dwPosition = struDllPath.LastIndexOf(L'.', 0);
    if (dwPosition == -1)
    {
        hr = E_FAIL;
        goto Finished;
    }

    struDllPath.QueryStr()[dwPosition] = L'\0';

    if (FAILED(hr = struDllPath.SyncWithBuffer()) ||
        FAILED(hr = struDllPath.Append(L".dll")))
    {
        goto Finished;
    }

    if (!UTILITY::CheckIfFileExists(struDllPath.QueryStr()))
    {
        // Treat access issue as File not found
        hr = ERROR_FILE_NOT_FOUND;
        goto Finished;
    }

    if (FAILED(hr = struArguments.Copy(struDllPath)) ||
        FAILED(hr = struArguments.Append(L" ")) ||
        FAILED(hr = struArguments.Append(pConfig->QueryArguments())))
    {
        goto Finished;
    }

    if (FAILED(hr = SetHostFxrArguments(struArguments.QueryStr(), pwzExePath, pConfig)))
    {
        goto Finished;
    }

Finished:

    return hr;
}

HRESULT
HOSTFXR_UTILITY::GetHostFxrParameters(
    ASPNETCORE_CONFIG *pConfig
)
{
    HRESULT                     hr = S_OK;
    STRU                        struSystemPathVariable;
    STRU                        struHostFxrPath;
    STRU                        struExeLocation;
    STRU                        struHostFxrSearchExpression;
    STRU                        struHighestDotnetVersion;
    std::vector<std::wstring>   vVersionFolders;
    DWORD                       dwPosition;

    // Convert the process path an absolute path.
    hr = UTILITY::ConvertPathToFullPath(
        pConfig->QueryProcessPath()->QueryStr(),
        pConfig->QueryApplicationPhysicalPath()->QueryStr(),
        &struExeLocation
    );

    if (FAILED(hr))
    {
        goto Finished;
    }

    if (UTILITY::CheckIfFileExists(struExeLocation.QueryStr()))
    {
        // Check if hostfxr is in this folder, if it is, we are a standalone application,
        // else we assume we received an absolute path to dotnet.exe
        hr = UTILITY::ConvertPathToFullPath(L".\\hostfxr.dll", pConfig->QueryApplicationPhysicalPath()->QueryStr(), &struHostFxrPath);
        if (FAILED(hr))
        {
            goto Finished;
        }

        if (UTILITY::CheckIfFileExists(struHostFxrPath.QueryStr()))
        {
            // Standalone application
            if (FAILED(hr = pConfig->SetHostFxrFullPath(struHostFxrPath.QueryStr())))
            {
                goto Finished;
            }

            hr = GetStandaloneHostfxrParameters(struExeLocation.QueryStr(), pConfig);
            goto Finished;
        }
    }
    else
    {
        if (FAILED(hr = HOSTFXR_UTILITY::FindDotnetExePath(&struExeLocation))) {
            goto Finished;
        }

    }

    if (FAILED(hr = struExeLocation.SyncWithBuffer()) ||
        FAILED(hr = struHostFxrPath.Copy(struExeLocation)))
    {
        goto Finished;
    }

    dwPosition = struHostFxrPath.LastIndexOf(L'\\', 0);
    if (dwPosition == -1)
    {
        hr = E_FAIL;
        goto Finished;
    }

    struHostFxrPath.QueryStr()[dwPosition] = L'\0';

    if (FAILED(hr = struHostFxrPath.SyncWithBuffer()) ||
        FAILED(hr = struHostFxrPath.Append(L"\\")))
    {
        goto Finished;
    }

    hr = struHostFxrPath.Append(L"host\\fxr");
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (!UTILITY::DirectoryExists(&struHostFxrPath))
    {
        // error, not found in folder
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    // Find all folders under host\\fxr\\ for version numbers.
    hr = struHostFxrSearchExpression.Copy(struHostFxrPath);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = struHostFxrSearchExpression.Append(L"\\*");
    if (FAILED(hr))
    {
        goto Finished;
    }

    // As we use the logic from core-setup, we are opting to use std here.
    // TODO remove all uses of std?
    UTILITY::FindDotNetFolders(struHostFxrSearchExpression.QueryStr(), &vVersionFolders);

    if (vVersionFolders.size() == 0)
    {
        // no core framework was found
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    hr = UTILITY::FindHighestDotNetVersion(vVersionFolders, &struHighestDotnetVersion);
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (FAILED(hr = struHostFxrPath.Append(L"\\"))
        || FAILED(hr = struHostFxrPath.Append(struHighestDotnetVersion.QueryStr()))
        || FAILED(hr = struHostFxrPath.Append(L"\\hostfxr.dll")))
    {
        goto Finished;
    }

    if (!UTILITY::CheckIfFileExists(struHostFxrPath.QueryStr()))
    {
        hr = ERROR_FILE_INVALID;
        goto Finished;
    }

    if (FAILED(hr = SetHostFxrArguments(pConfig->QueryArguments()->QueryStr(), struExeLocation.QueryStr(), pConfig)))
    {
        goto Finished;
    }

    if (FAILED(hr = pConfig->SetHostFxrFullPath(struHostFxrPath.QueryStr())))
    {
        goto Finished;
    }

Finished:

    return hr;
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
HOSTFXR_UTILITY::SetHostFxrArguments(
    PCWSTR pwzArgumentsFromConfig,
    PCWSTR pwzExePath,
    ASPNETCORE_CONFIG* pConfig
)
{
    HRESULT     hr = S_OK;
    INT         argc = 0;
    PCWSTR*     argv = NULL;
    LPWSTR*     pwzArgs = NULL;
    STRU        struTempPath;
    DWORD         dwArgsProcessed = 0;

    pwzArgs = CommandLineToArgvW(pwzArgumentsFromConfig, &argc);

    if (pwzArgs == NULL)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Failure;
    }

    if (argc < 1)
    {
        // Invalid arguments
        hr = E_INVALIDARG;
        goto Failure;
    }

    argv = new PCWSTR[argc + 2];
    if (argv == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }

    argv[0] = SysAllocString(pwzExePath);

    if (argv[0] == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }
    dwArgsProcessed++;

    argv[1] = SysAllocString(L"exec");
    if (argv[1] == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }
    dwArgsProcessed++;

    // Try to convert the application dll from a relative to an absolute path
    // Don't record this failure as pwzArgs[0] may already be an absolute path to the dll.
    if (SUCCEEDED(UTILITY::ConvertPathToFullPath(pwzArgs[0], pConfig->QueryApplicationPhysicalPath()->QueryStr(), &struTempPath)))
    {
        argv[2] = SysAllocString(struTempPath.QueryStr());
    }
    else
    {
        argv[2] = SysAllocString(pwzArgs[0]);
    }
    if (argv[2] == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Failure;
    }
    dwArgsProcessed++;

    for (INT i = 1; i < argc; i++)
    {
        argv[i + 2] = SysAllocString(pwzArgs[i]);
        if (argv[i + 2] == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Failure;
        }
        dwArgsProcessed++;
    }

    pConfig->SetHostFxrArguments(argc + 2, argv);
    goto Finished;

Failure:
    if (argv != NULL)
    {
        for (DWORD i = 0; i < dwArgsProcessed; i++)
        {
            SysFreeString((BSTR)argv[i]);
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
//
// Invoke where.exe to find the location of dotnet.exe
// Copies contents of dotnet.exe to a temp file
// Respects path ordering.

HRESULT
HOSTFXR_UTILITY::FindDotnetExePath(
    _Out_ STRU* struDotnetPath
)
{
    HRESULT             hr = S_OK;
    STARTUPINFOW        startupInfo = { 0 };
    PROCESS_INFORMATION processInformation = { 0 };
    SECURITY_ATTRIBUTES securityAttributes;
    STRU                struDotnetSubstring;
    STRU                struDotnetLocationsString;
    LPWSTR              pwzDotnetName = NULL;
    DWORD               dwExitCode;
    DWORD               dwNumBytesRead;
    DWORD               dwFilePointer;
    DWORD               dwBinaryType;
    INT                 index = 0;
    INT                 prevIndex = 0;
    BOOL                fResult = FALSE;
    BOOL                fIsWow64Process;
    BOOL                fIsCurrentProcess64Bit;
    BOOL                fFound = FALSE;
    CHAR                pzFileContents[READ_BUFFER_SIZE];
    HANDLE              hStdOutReadPipe = INVALID_HANDLE_VALUE;
    HANDLE              hStdOutWritePipe = INVALID_HANDLE_VALUE;

    securityAttributes.nLength = sizeof(securityAttributes);
    securityAttributes.lpSecurityDescriptor = NULL;
    securityAttributes.bInheritHandle = TRUE;

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

    // Set stdout and error to redirect to the temp file.
    startupInfo.cb = sizeof(startupInfo);
    startupInfo.dwFlags |= STARTF_USESTDHANDLES;
    startupInfo.hStdOutput = hStdOutWritePipe;
    startupInfo.hStdError = hStdOutWritePipe;

    // CreateProcess requires a mutable string to be passed to commandline
    // See https://blogs.msdn.microsoft.com/oldnewthing/20090601-00/?p=18083/

    pwzDotnetName = SysAllocString(L"\"where.exe\" dotnet.exe");
    if (pwzDotnetName == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }
    fResult = CreateProcessW(NULL,
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

    if (!fResult)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }

    if (WaitForSingleObject(processInformation.hProcess, 2000) != WAIT_OBJECT_0) // 2 seconds
    {
        TerminateProcess(processInformation.hProcess, 2);
        hr = HRESULT_FROM_WIN32(ERROR_TIMEOUT);
        goto Finished;
    }

    //
    // where.exe will return 1 if the file is not found
    // and 2 if there was an error. Check if the exit code is 1 and set
    // a new hr result saying it couldn't find dotnet.exe
    // 
    if (!GetExitCodeProcess(processInformation.hProcess, &dwExitCode) ||
        dwExitCode != 0)
    {
        if (dwExitCode == 1)
        {
            hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        }
        else
        {
            hr = HRESULT_FROM_WIN32(GetLastError());
        }
        goto Finished;
    }

    // Reset file pointer to the beginning of the file. 
    dwFilePointer = SetFilePointer(hStdOutReadPipe, 0, NULL, FILE_BEGIN);
    if (dwFilePointer == INVALID_SET_FILE_POINTER)
    {
        hr = HRESULT_FROM_WIN32(ERROR_FILE_INVALID);
        goto Finished;
    }

    //
    // As the call to where.exe succeeded (dotnet.exe was found), ReadFile should not hang.
    // TODO consider putting ReadFile in a separate thread with a timeout to guarantee it doesn't block.
    //
    if (!ReadFile(hStdOutReadPipe, pzFileContents, READ_BUFFER_SIZE, &dwNumBytesRead, NULL))
    {
        hr = HRESULT_FROM_WIN32(ERROR_FILE_INVALID);
        goto Finished;
    }
    if (dwNumBytesRead >= READ_BUFFER_SIZE)
    {
        // This shouldn't ever be this large. We could continue to call ReadFile in a loop,
        // however I'd rather error out here and report an issue.
        hr = HRESULT_FROM_WIN32(ERROR_FILE_TOO_LARGE);
        goto Finished;
    }

    if (FAILED(hr = struDotnetLocationsString.CopyA(pzFileContents, dwNumBytesRead)))
    {
        goto Finished;
    }

    // Check the bitness of the currently running process
    // matches the dotnet.exe found. 
    if (!IsWow64Process(GetCurrentProcess(), &fIsWow64Process))
    {
        // Calling IsWow64Process failed
        hr = HRESULT_FROM_WIN32(GetLastError());
        goto Finished;
    }
    if (fIsWow64Process)
    {
        // 32 bit mode
        fIsCurrentProcess64Bit = FALSE;
    }
    else
    {
        SYSTEM_INFO systemInfo;
        GetNativeSystemInfo(&systemInfo);
        fIsCurrentProcess64Bit = systemInfo.wProcessorArchitecture == PROCESSOR_ARCHITECTURE_AMD64;
    }

    while (!fFound)
    {
        index = struDotnetLocationsString.IndexOf(L"\r\n", prevIndex);
        if (index == -1)
        {
            break;
        }
        if (FAILED(hr = struDotnetSubstring.Copy(struDotnetLocationsString.QueryStr(), index - prevIndex)))
        {
            goto Finished;
        }
        prevIndex = index;

        if (!GetBinaryTypeW(struDotnetSubstring.QueryStr(), &dwBinaryType))
        {
            // TODO should we ignore this failure and continue to trying the next dotnet.exe?
            hr = HRESULT_FROM_WIN32(GetLastError());
            goto Finished;
        }
        if (fIsCurrentProcess64Bit == (dwBinaryType == SCS_64BIT_BINARY)) {
            // Found a valid dotnet.
            struDotnetPath->Copy(struDotnetSubstring);
            fFound = TRUE;
        }
    }

    if (!fFound)
    {
        hr = HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
        goto Finished;
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

    return hr;
}
