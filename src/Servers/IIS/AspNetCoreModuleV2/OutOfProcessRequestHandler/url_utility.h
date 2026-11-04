// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"

#include <httpserv.h>
#include "stringu.h"

class URL_UTILITY
{
public:

    static
    HRESULT
    SplitUrl(
        PCWSTR pszDestinationUrl,
        BOOL *pfSecure,
        STRU *pstrDestination,
        STRU *pstrUrl
    );

    static HRESULT
    EscapeAbsPath(
        IHttpRequest * pRequest,
        STRU * strEscapedUrl
    );
};

