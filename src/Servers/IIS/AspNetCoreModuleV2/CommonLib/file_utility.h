// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"

#include <httpserv.h>
#include "stringu.h"

class FILE_UTILITY
{
public:

    static
    HRESULT
    ConvertPathToFullPath(
        _In_  LPCWSTR   pszPath,
        _In_  LPCWSTR   pszRootPath,
        _Out_ STRU*     pStrFullPath
    );

    static
    HRESULT
    EnsureDirectoryPathExists(
        _In_  LPCWSTR pszPath
    );

    static
    std::string
    GetHtml(HMODULE module, int page, USHORT statusCode, USHORT subStatusCode, const std::string& specificReasonPhrase, const std::string& solution);

    static
    std::string
    GetHtml(HMODULE module, int page, USHORT statusCode, USHORT subStatusCode, const std::string& specificReasonPhrase, const std::string& solution, const std::string& error);

private:

    static
    HRESULT
    IsPathUnc(
        __in  LPCWSTR       pszPath,
        __out bool *        pfIsUnc
    );
};

