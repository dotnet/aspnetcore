// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <Windows.h>
#include <memory>

#include "stringu.h"

class HOSTFXR_OPTIONS
{
public:
    HOSTFXR_OPTIONS() {}

    ~HOSTFXR_OPTIONS()
    {
        delete[] m_argv;
    }

    DWORD
    GetArgc() const
    {
        return m_argc;
    }

    BSTR*
    GetArgv() const
    {
        return m_argv;
    }

    PCWSTR
    GetHostFxrLocation() const
    {
        return m_hostFxrLocation.QueryStr();
    }

    PCWSTR
    GetExeLocation() const
    {
        return m_exeLocation.QueryStr();
    }

    static
    HRESULT Create(
         _In_  PCWSTR pcwzExeLocation,
         _In_  PCWSTR pcwzProcessPath,
         _In_  PCWSTR pcwzApplicationPhysicalPath,
         _In_  PCWSTR pcwzArguments,
         _Out_ std::unique_ptr<HOSTFXR_OPTIONS>& ppWrapper);

private:

    HRESULT Populate(PCWSTR hostFxrLocation, PCWSTR struExeLocation, DWORD argc, BSTR argv[]);

    STRU m_exeLocation;
    STRU m_hostFxrLocation;

    DWORD m_argc;
    BSTR* m_argv;
};
