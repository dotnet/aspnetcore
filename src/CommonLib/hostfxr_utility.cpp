// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

HOSTFXR_UTILITY::HOSTFXR_UTILITY()
{
}


HOSTFXR_UTILITY::~HOSTFXR_UTILITY()
{
}

HRESULT
HOSTFXR_UTILITY::FindHostFxrDll(
    ASPNETCORE_CONFIG *pConfig,
    STRU* struHostFxrDllLocation,
    BOOL* fStandAlone
)
{
    HRESULT hr = S_OK;

    // If the process path isn't dotnet, assume we are a standalone appliction.
    // TODO: this should be a path equivalent check
    if (!(pConfig->QueryProcessPath()->Equals(L".\\dotnet")
        || pConfig->QueryProcessPath()->Equals(L"dotnet")
        || pConfig->QueryProcessPath()->Equals(L".\\dotnet.exe")
        || pConfig->QueryProcessPath()->Equals(L"dotnet.exe")))
    {
        // hostfxr is in the same folder, parse and use it.
        hr = GetStandaloneHostfxrLocation(struHostFxrDllLocation, pConfig);
        *fStandAlone = TRUE;
    }
    else
    {
        hr = GetPortableHostfxrLocation(struHostFxrDllLocation);
        fStandAlone = FALSE;
    }

    return hr;
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
HOSTFXR_UTILITY::GetStandaloneHostfxrLocation(
    STRU* struHostfxrPath,
    ASPNETCORE_CONFIG *pConfig
)
{
    HRESULT     hr = S_OK;
    HANDLE      hFileHandle = INVALID_HANDLE_VALUE;
    SECURITY_ATTRIBUTES saAttr;

    // Get the full path to the exe and check if it exists
    if (FAILED(hr = UTILITY::ConvertPathToFullPath(L"\\hostfxr.dll",
        pConfig->QueryApplicationPhysicalPath()->QueryStr(),
        struHostfxrPath)))
    {
        goto Finished;
    }


    saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
    saAttr.bInheritHandle = TRUE;
    saAttr.lpSecurityDescriptor = NULL;

    hFileHandle = CreateFile(struHostfxrPath->QueryStr(),
            GENERIC_READ,
            FILE_SHARE_READ,
            &saAttr,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            NULL);
    if (hFileHandle == INVALID_HANDLE_VALUE)
    {
        // Treat access isseu as File not found
        hr = ERROR_FILE_NOT_FOUND;
        goto Finished;
    }
    else
    {
        CloseHandle(hFileHandle);
    }

Finished:
    return hr;
}

HRESULT
HOSTFXR_UTILITY::GetPortableHostfxrLocation(
    STRU* struHostfxrPath
)
{
    HRESULT hr = S_OK;

    STRU                        struSystemPathVariable;
    STRU                        strDotnetExeLocation;
    STRU                        strHostFxrSearchExpression;
    STRU                        strHighestDotnetVersion;
    PWSTR                       pwzDelimeterContext = NULL;
    PCWSTR                      pszDotnetLocation = NULL;
    PCWSTR                      pszDotnetExeString(L"dotnet.exe");
    DWORD                       dwCopyLength;
    BOOL                        fFound = FALSE;
    HANDLE                      hFileHandle = INVALID_HANDLE_VALUE;
    SECURITY_ATTRIBUTES         saAttr;
    std::vector<std::wstring>   vVersionFolders;

    if (FAILED(hr))
    {
        goto Finished;
    }

    // Get the System PATH value.
    if (!UTILITY::GetSystemPathVariable(L"PATH", &struSystemPathVariable))
    {
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    // Split on ';', checking to see if dotnet.exe exists in any folders.
    pszDotnetLocation = wcstok_s(struSystemPathVariable.QueryStr(), L";", &pwzDelimeterContext);
    while (pszDotnetLocation != NULL)
    {
        dwCopyLength = (DWORD) wcsnlen_s(pszDotnetLocation, 260);

        // We store both the exe and folder locations as we eventually need to check inside of host\\fxr
        // which doesn't need the dotnet.exe portion of the string
        hr = strDotnetExeLocation.Copy(pszDotnetLocation, dwCopyLength);
        if (FAILED(hr))
        {
            goto Finished;
        }

        if (dwCopyLength > 0 && pszDotnetLocation[dwCopyLength - 1] != L'\\')
        {
            hr = strDotnetExeLocation.Append(L"\\");
            if (FAILED(hr))
            {
                goto Finished;
            }
        }

        hr = struHostfxrPath->Copy(strDotnetExeLocation);
        if (FAILED(hr))
        {
            goto Finished;
        }

        hr = strDotnetExeLocation.Append(pszDotnetExeString);
        if (FAILED(hr))
        {
            goto Finished;
        }

        saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
        saAttr.bInheritHandle = TRUE;
        saAttr.lpSecurityDescriptor = NULL;

        hFileHandle = CreateFile(strDotnetExeLocation.QueryStr(),
            GENERIC_READ,
            FILE_SHARE_READ,
            &saAttr,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            NULL);
        if (hFileHandle != INVALID_HANDLE_VALUE)
        {
            // means we found the folder with a dotnet.exe inside of it.
            fFound = TRUE;
            CloseHandle(hFileHandle);
            break;
        }
        pszDotnetLocation = wcstok_s(NULL, L";", &pwzDelimeterContext);
    }

    if (!fFound)
    {
        // could not find dotnet.exe, error out
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    hr = struHostfxrPath->Append(L"host\\fxr");
    if (FAILED(hr))
    {
        goto Finished;
    }

    if (!UTILITY::DirectoryExists(struHostfxrPath))
    {
        // error, not found the folder
        hr = ERROR_BAD_ENVIRONMENT;
        goto Finished;
    }

    // Find all folders under host\\fxr\\ for version numbers.
    hr = strHostFxrSearchExpression.Copy(struHostfxrPath);
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
    hr = struHostfxrPath->Append(L"\\");
    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = struHostfxrPath->Append(strHighestDotnetVersion.QueryStr());
    if (FAILED(hr))
    {
        goto Finished;

    }

    hr = struHostfxrPath->Append(L"\\hostfxr.dll");
    if (FAILED(hr))
    {
        goto Finished;
    }

Finished:
    return hr;
}