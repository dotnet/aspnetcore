// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "file_utility.h"

#include <Shlwapi.h>
#include "debugutil.h"
#include "exceptions.h"
#include "Environment.h"

HRESULT
FILE_UTILITY::IsPathUnc(
    __in  LPCWSTR       pszPath,
    __out bool*         pfIsUnc
)
{
    if (pszPath == nullptr || pfIsUnc == nullptr)
    {
        return E_INVALIDARG;
    }

    STRU strTempPath;
    HRESULT hr = MakePathCanonicalizationProof(pszPath, &strTempPath );
    if (FAILED(hr))
    {
        return hr;
    }

    // MakePathCanonicalizationProof will map \\?\UNC, \\.\UNC and \\ to \\?\UNC (which is 8 characters)
    *pfIsUnc = (_wcsnicmp(strTempPath.QueryStr(), L"\\\\?\\UNC\\", 8) == 0);

    return hr;
}

HRESULT
FILE_UTILITY::ConvertPathToFullPath(
    _In_  LPCWSTR   pszPath,
    _In_  LPCWSTR   pszRootPath,
    _Out_ STRU*     pStruFullPath
)
{
    HRESULT hr = S_OK;
    STRU strFileFullPath;
    LPWSTR pszFullPath = NULL;

    // if relative path, prefix with root path and then convert to absolute path.
    if ( PathIsRelative(pszPath) )
    {
        hr = strFileFullPath.Copy(pszRootPath);
        if(FAILED(hr))
        {
            goto Finished;
        }

        if(!strFileFullPath.EndsWith(L"\\"))
        {
            hr = strFileFullPath.Append(L"\\");
            if(FAILED(hr))
            {
                goto Finished;
            }
        }
    }

    hr = strFileFullPath.Append( pszPath );
    if (FAILED(hr))
    {
        goto Finished;
    }

    pszFullPath = new WCHAR[ strFileFullPath.QueryCCH() + 1];

    if(_wfullpath( pszFullPath,
                   strFileFullPath.QueryStr(),
                   strFileFullPath.QueryCCH() + 1 ) == NULL )
    {
        hr = HRESULT_FROM_WIN32( ERROR_INVALID_PARAMETER );
        goto Finished;
    }

    // convert to canonical path
    hr = MakePathCanonicalizationProof( pszFullPath, pStruFullPath );
    if (FAILED(hr))
    {
        goto Finished;
    }

Finished:

    if ( pszFullPath != NULL )
    {
        delete[] pszFullPath;
        pszFullPath = NULL;
    }

    return hr;
}

// Ensures that the specified directory path exists, creating it if necessary. If any part of the directory
// creation fails and the directory does not already exist, it returns an error obtained from GetLastError.
// If all directories are successfully created or already exist, it returns S_OK.
HRESULT
FILE_UTILITY::EnsureDirectoryPathExists(
    _In_ const LPCWSTR pszPath
)
{
    STRU struPath;
    struPath.Copy(pszPath);

    bool isUnc = false;
    HRESULT hr = IsPathUnc(pszPath, &isUnc);
    if (FAILED(hr))
    {
        return hr;
    }

    // Initialize position based on the type of the path.
    DWORD position = isUnc ? 8 // Skip "\\?\UNC\"
                   : (struPath.IndexOf(L'?', 0) != -1) ? 4 // Skip "\\?\"
                   : 0; // Skip nothing

    // Traverse the path string, creating directories as we go.
    while (true)
    {
        position = struPath.IndexOf(L'\\', position + 1);
        if (position == -1)
        {
            // Didn't find a path separator, so we're done.
            return S_OK;
        }

        // position is >= 1 here since we started searching at position + 1
        if (struPath.QueryStr()[position - 1] == L':')
        {
            // Skip the volume case
            continue;
        }

        // Temporarily terminate the string at the current path separator
        struPath.QueryStr()[position] = L'\0';
        if (!CreateDirectory(struPath.QueryStr(), nullptr) && GetLastError() != ERROR_ALREADY_EXISTS)
        {
            // Unable to create directory and it doesn't already exist
            return HRESULT_FROM_WIN32(GetLastError());
        }

        // Restore the path separator
        struPath.QueryStr()[position] = L'\\';
    }
}

std::string FILE_UTILITY::GetHtml(HMODULE module, int page, USHORT statusCode, USHORT subStatusCode, const std::string& specificReasonPhrase, const std::string& solution)
{
    return GetHtml(module, page, statusCode, subStatusCode, specificReasonPhrase, solution, std::string());
}

std::string
FILE_UTILITY::GetHtml(HMODULE module, int page, USHORT statusCode, USHORT subStatusCode, const std::string& specificReasonPhrase, const std::string& errorReason, const std::string& specificError)
{
    try
    {
        HRSRC rc = nullptr;
        HGLOBAL rcData = nullptr;
        std::string data;
        const char* pTempData = nullptr;

        THROW_LAST_ERROR_IF_NULL(rc = FindResource(module, MAKEINTRESOURCE(page), RT_HTML));
        THROW_LAST_ERROR_IF_NULL(rcData = LoadResource(module, rc));
        auto const size = SizeofResource(module, rc);
        THROW_LAST_ERROR_IF(size == 0);
        THROW_LAST_ERROR_IF_NULL(pTempData = static_cast<const char*>(LockResource(rcData)));
        data = std::string(pTempData, size);

        auto additionalErrorLink = Environment::GetEnvironmentVariableValue(L"ANCM_ADDITIONAL_ERROR_PAGE_LINK");
        std::string additionalHtml;

        if (additionalErrorLink.has_value())
        {
            additionalHtml = format("<a href=\"%S\"> <cite> %S </cite></a> and ", additionalErrorLink->c_str(), additionalErrorLink->c_str());
        }

        std::string formattedError;
        if (!specificError.empty())
        {
            formattedError = format("<h2>Specific error detected by ANCM:</h2><h3>%s</h3>", specificError.c_str());
        }

        std::string formattedErrorReason;
        if (!errorReason.empty())
        {
            formattedErrorReason = format("<h2> Common solutions to this issue: </h2>%s", errorReason.c_str());
        }

        return format(data, statusCode, subStatusCode, specificReasonPhrase.c_str(), statusCode, subStatusCode, specificReasonPhrase.c_str(), formattedErrorReason.c_str(), formattedError.c_str(), additionalHtml.c_str());
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
        return "";
    }
}
