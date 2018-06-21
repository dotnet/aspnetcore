// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "hostfxroptions.h"

#include "hostfxr_utility.h"
#include "debugutil.h"
#include "exceptions.h"

HRESULT HOSTFXR_OPTIONS::Create(
        _In_ PCWSTR         pcwzExeLocation,
        _In_ PCWSTR         pcwzProcessPath,
        _In_ PCWSTR         pcwzApplicationPhysicalPath,
        _In_ PCWSTR         pcwzArguments,
        _In_ HANDLE         hEventLog,
        _Out_ std::unique_ptr<HOSTFXR_OPTIONS>& ppWrapper)
{
    STRU struHostFxrDllLocation;
    STRU struExeAbsolutePath;
    STRU struExeLocation;
    BSTR* pwzArgv;
    DWORD dwArgCount;

    if (pcwzExeLocation != NULL)
    {
        RETURN_IF_FAILED(struExeLocation.Copy(pcwzExeLocation));
    }

    // If the exe was not provided by the shim, reobtain the hostfxr parameters (which finds dotnet).
    if (struExeLocation.IsEmpty())
    {
        RETURN_IF_FAILED(HOSTFXR_UTILITY::GetHostFxrParameters(
            hEventLog,
            pcwzProcessPath,
            pcwzApplicationPhysicalPath,
            pcwzArguments,
            &struHostFxrDllLocation,
            &struExeAbsolutePath,
            &dwArgCount,
            &pwzArgv));
    }
    else if (HOSTFXR_UTILITY::IsDotnetExecutable(struExeLocation.QueryStr()))
    {
        RETURN_IF_FAILED(HOSTFXR_UTILITY::ParseHostfxrArguments(
            pcwzArguments,
            pcwzExeLocation,
            pcwzApplicationPhysicalPath,
            hEventLog,
            &dwArgCount,
            &pwzArgv));
    }
    else
    {
        RETURN_IF_FAILED(HOSTFXR_UTILITY::GetStandaloneHostfxrParameters(
            pcwzExeLocation,
            pcwzApplicationPhysicalPath,
            pcwzArguments,
            hEventLog,
            &struHostFxrDllLocation,
            &dwArgCount,
            &pwzArgv));
    }

    ppWrapper = std::make_unique<HOSTFXR_OPTIONS>();
    RETURN_IF_FAILED(ppWrapper->Populate(struHostFxrDllLocation.QueryStr(), struExeAbsolutePath.QueryStr(), dwArgCount, pwzArgv));

    return S_OK;
}


HRESULT HOSTFXR_OPTIONS::Populate(PCWSTR hostFxrLocation, PCWSTR struExeLocation, DWORD argc, BSTR argv[])
{
    HRESULT hr;

    m_argc = argc;
    m_argv = argv;

    if (FAILED(hr = m_hostFxrLocation.Copy(hostFxrLocation)))
    {
        goto Finished;
    }

    if (FAILED(hr = m_exeLocation.Copy(struExeLocation)))
    {
        goto Finished;
    }

    Finished:

    return hr;
}
