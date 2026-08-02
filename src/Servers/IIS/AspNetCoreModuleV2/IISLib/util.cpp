// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.h"

HRESULT
MakePathCanonicalizationProof(
    IN PCWSTR               pszName,
    OUT STRU *              pstrPath
)
/*++

Routine Description:

    This functions adds a prefix
    to the string, which is "\\?\UNC\" for a UNC path, and "\\?\" for
    other paths.  This prefix tells Windows not to parse the path.

Arguments:

    IN  pszName     - The path to be converted
    OUT pstrPath    - Output path created

Return Values:

    HRESULT

--*/
{
    HRESULT hr = S_OK;

    if (pszName[0] == L'\\' && pszName[1] == L'\\')
    {
        //
        // If the path is already canonicalized, just return
        //

        if ((pszName[2] == '?' || pszName[2] == '.') &&
            pszName[3] == '\\')
        {
            hr = pstrPath->Copy(pszName);

            if (SUCCEEDED(hr))
            {
                //
                // If the path was in DOS form ("\\.\"),
                // we need to change it to Win32 from ("\\?\")
                //

                // Buffer overrun while writing to 'pstrPath->QueryStr()'
                // We know it's not an overrun because we just copied pszName into pstrPath and pszName is at least 4 in length
#pragma warning(suppress: 6386)
                pstrPath->QueryStr()[2] = L'?';
            }

            return hr;
        }

        pszName += 2;


        if (FAILED(hr = pstrPath->Copy(L"\\\\?\\UNC\\")))
        {
            return hr;
        }
    }
    else if (wcslen(pszName) > MAX_PATH)
    {
        if (FAILED(hr = pstrPath->Copy(L"\\\\?\\")))
        {
            return hr;
        }
    }
    else
    {
        pstrPath->Reset();
    }

    return pstrPath->Append(pszName);
}

