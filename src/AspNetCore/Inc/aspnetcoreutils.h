// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

class ASPNETCORE_UTILS
{
public:

    static
    HRESULT 
    ReplacePlaceHolderWithValue(
        _Inout_ LPWSTR  pszStr, 
        _In_    LPWSTR  pszPlaceholder, 
        _In_    DWORD   cchPlaceholder,
        _In_    DWORD   dwValue,
        _In_    DWORD   dwNumDigitsInValue,
        _Out_   BOOL   *pfReplaced
    );

private:
    ASPNETCORE_UTILS()
    {
    }
};