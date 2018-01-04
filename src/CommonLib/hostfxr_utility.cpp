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
    ASPNETCORE_CONFIG *pConfig
)
{
    HRESULT             hr = S_OK;
    STRU                struExePath;
    STRU                struDllPath;
    STRU                struArguments;
    DWORD               dwPosition;

    hr = UTILITY::ConvertPathToFullPath(pConfig->QueryProcessPath()->QueryStr(),
        pConfig->QueryApplicationPhysicalPath()->QueryStr(),
        &struExePath);
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (FAILED(hr = struDllPath.Copy(struExePath)))
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

    if (FAILED(hr = SetHostFxrArguments(&struArguments, &struExePath, pConfig)))
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
    STRU                        strDotnetExeLocation;
    STRU                        strHostFxrSearchExpression;
    STRU                        strHighestDotnetVersion;
    std::vector<std::wstring>   vVersionFolders;
    DWORD                       dwPosition;
    DWORD                       dwPathLength = MAX_PATH;
    DWORD                       dwDotnetLength = 0;
    BOOL                        fFound = FALSE;

    if (UTILITY::CheckIfFileExists(pConfig->QueryProcessPath()->QueryStr()))
    {
        hr = UTILITY::ConvertPathToFullPath(L"hostfxr.dll", pConfig->QueryApplicationPath()->QueryStr(), &struHostFxrPath);
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

            hr = GetStandaloneHostfxrParameters(pConfig);
            goto Finished;
        }
        else
        {
            hr = UTILITY::ConvertPathToFullPath(
                pConfig->QueryProcessPath()->QueryStr(), 
                pConfig->QueryApplicationPath()->QueryStr(), 
                &strDotnetExeLocation
            );
            if (FAILED(hr))
            {
                goto Finished;
            }
        }
    }

    if (FAILED(hr = strDotnetExeLocation.Resize(MAX_PATH)))
    {
        goto Finished;
    }

    while (!fFound)
    {
        dwDotnetLength = SearchPath(NULL, L"dotnet", L".exe", dwPathLength, strDotnetExeLocation.QueryStr(), NULL);
        if (dwDotnetLength == 0)
        {
            hr = GetLastError();
            // Could not find dotnet
            goto Finished;
        }
        else if (dwDotnetLength == dwPathLength)
        {
            // Increase size
            dwPathLength *= 2;
            if (FAILED(hr = strDotnetExeLocation.Resize(dwPathLength)))
            {
                goto Finished;
            }
        }
        else
        {
            fFound = TRUE;
        }
    }

    if (FAILED(hr = strDotnetExeLocation.SyncWithBuffer())
        || FAILED(hr = struHostFxrPath.Copy(strDotnetExeLocation)))
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

    if (FAILED(hr = struHostFxrPath.SyncWithBuffer())
        || FAILED(hr = struHostFxrPath.Append(L"\\")))
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
    hr = strHostFxrSearchExpression.Copy(struHostFxrPath);
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = strHostFxrSearchExpression.Append(L"\\*");
    if (FAILED(hr))
    {
        goto Finished;
    }

    // As we use the logic from core-setup, we are opting to use std here.
    // TODO remove all uses of std?
    UTILITY::FindDotNetFolders(strHostFxrSearchExpression.QueryStr(), &vVersionFolders);

    if (vVersionFolders.size() == 0)
    {
        // no core framework was found
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    hr = UTILITY::FindHighestDotNetVersion(vVersionFolders, &strHighestDotnetVersion);
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (FAILED(hr = struHostFxrPath.Append(L"\\"))
        || FAILED(hr = struHostFxrPath.Append(strHighestDotnetVersion.QueryStr()))
        || FAILED(hr = struHostFxrPath.Append(L"\\hostfxr.dll")))
    {
        goto Finished;
    }

    if (!UTILITY::CheckIfFileExists(struHostFxrPath.QueryStr()))
    {
        hr = ERROR_FILE_INVALID;
        goto Finished;
    }
    
    if (FAILED(hr = SetHostFxrArguments(pConfig->QueryArguments(), &strDotnetExeLocation, pConfig)))
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
// argv[2] = first argument specified in the arguments portion of aspnetcore config. 
// 
HRESULT
HOSTFXR_UTILITY::SetHostFxrArguments(
    STRU* struArgumentsFromConfig,
    STRU* pstruExePath,
    ASPNETCORE_CONFIG* pConfig
)
{
    HRESULT     hr = S_OK;
    INT         argc = 0;
    PCWSTR*     argv = NULL;
    LPWSTR*     pwzArgs = NULL;

    pwzArgs = CommandLineToArgvW(struArgumentsFromConfig->QueryStr(), &argc);

    if (pwzArgs == NULL)
    {
        goto Finished;
    }

    argv = new PCWSTR[argc + 2];
    if (argv == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    argv[0] = SysAllocString(pstruExePath->QueryStr());
    argv[1] = SysAllocString(L"exec");

    for (INT i = 0; i < argc; i++)
    {
        argv[i + 2] = SysAllocString(pwzArgs[i]);
    }

    pConfig->SetHostFxrArguments(argc + 2, argv);

Finished:
    if (pwzArgs != NULL)
    {
        LocalFree(pwzArgs);
        DBG_ASSERT(pwzArgs == NULL);
    }
    return hr;
}