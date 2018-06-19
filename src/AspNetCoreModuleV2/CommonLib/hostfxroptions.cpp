// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

HRESULT HOSTFXR_OPTIONS::Create(
        _In_ PCWSTR         pcwzExeLocation,
        _In_ PCWSTR         pcwzProcessPath,
        _In_ PCWSTR         pcwzApplicationPhysicalPath,
        _In_ PCWSTR         pcwzArguments,
        _In_ HANDLE         hEventLog,
        _Out_ std::unique_ptr<HOSTFXR_OPTIONS>& ppWrapper)
{
    HRESULT hr = S_OK;
    STRU struHostFxrDllLocation;
    STRU struExeAbsolutePath;
    STRU struExeLocation;
    BSTR* pwzArgv;
    DWORD dwArgCount;

    if (pcwzExeLocation != NULL && FAILED(struExeLocation.Copy(pcwzExeLocation)))
    {
        goto Finished;
    }

    // If the exe was not provided by the shim, reobtain the hostfxr parameters (which finds dotnet).
    if (struExeLocation.IsEmpty())
    {
        if (FAILED(hr = HOSTFXR_UTILITY::GetHostFxrParameters(
            hEventLog,
            pcwzProcessPath,
            pcwzApplicationPhysicalPath,
            pcwzArguments,
            &struHostFxrDllLocation,
            &struExeAbsolutePath,
            &dwArgCount,
            &pwzArgv)))
        {
            goto Finished;
        }
    }
    else if (HOSTFXR_UTILITY::IsDotnetExecutable(struExeLocation.QueryStr()))
    {
        if (FAILED(hr = HOSTFXR_UTILITY::ParseHostfxrArguments(
            pcwzArguments,
            pcwzExeLocation,
            pcwzApplicationPhysicalPath,
            hEventLog,
            &dwArgCount,
            &pwzArgv)))
        {
            goto Finished;
        }
    }
    else
    {
        if (FAILED(hr = HOSTFXR_UTILITY::GetStandaloneHostfxrParameters(
            pcwzExeLocation,
            pcwzApplicationPhysicalPath,
            pcwzArguments,
            hEventLog,
            &struHostFxrDllLocation,
            &dwArgCount,
            &pwzArgv)))
        {
            goto Finished;
        }
    }

    ppWrapper = std::make_unique<HOSTFXR_OPTIONS>();
    if (FAILED(hr = ppWrapper->Populate(struHostFxrDllLocation.QueryStr(), struExeAbsolutePath.QueryStr(), dwArgCount, pwzArgv)))
    {
        goto Finished;
    }

Finished:

    return hr;
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
