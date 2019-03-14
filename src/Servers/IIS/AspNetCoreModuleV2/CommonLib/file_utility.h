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
    EnsureDirectoryPathExist(
        _In_  LPCWSTR pszPath
    );

    static
    std::string
    GetHtml(HMODULE module, int page);

private:
    static
    HRESULT
    IsPathUnc(
        __in  LPCWSTR       pszPath,
        __out BOOL *        pfIsUnc
    );

};

