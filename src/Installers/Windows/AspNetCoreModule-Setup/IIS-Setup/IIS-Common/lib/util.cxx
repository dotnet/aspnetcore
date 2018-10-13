// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

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
    HRESULT hr;

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
    else
    {
        if (FAILED(hr = pstrPath->Copy(L"\\\\?\\")))
        {
            return hr;
        }
    }

    return pstrPath->Append(pszName);
}

