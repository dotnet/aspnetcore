// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <Windows.h>
#include <filesystem>

#include "GlobalVersionUtility.h"

namespace fs = std::filesystem;

// throws runtime error if no request handler versions are installed.
// Throw invalid_argument if any argument is null
std::wstring
GlobalVersionUtility::GetGlobalRequestHandlerPath(PCWSTR pwzAspNetCoreFolderPath, PCWSTR pwzHandlerVersion, PCWSTR pwzHandlerName)
{
    if (pwzAspNetCoreFolderPath == nullptr)
    {
        throw new std::invalid_argument("pwzAspNetCoreFolderPath is NULL");
    }

    if (pwzHandlerVersion == nullptr)
    {
        throw new std::invalid_argument("pwzHandlerVersion is NULL");
    }

    if (pwzHandlerName == nullptr)
    {
        throw new std::invalid_argument("pwzHandlerName is NULL");
    }

    std::wstring folderVersion(pwzHandlerVersion);
    fs::path aspNetCoreFolderPath(pwzAspNetCoreFolderPath);

    if (folderVersion.empty())
    {
        folderVersion = FindHighestGlobalVersion(pwzAspNetCoreFolderPath);
    }

    aspNetCoreFolderPath = aspNetCoreFolderPath
        .append(folderVersion)
        .append(pwzHandlerName);
    return aspNetCoreFolderPath;
}

// Throw filesystem_error if directory_iterator can't iterate over the directory
// Throw invalid_argument if any argument is null
std::vector<fx_ver_t>
GlobalVersionUtility::GetRequestHandlerVersions(PCWSTR pwzAspNetCoreFolderPath)
{
    if (pwzAspNetCoreFolderPath == nullptr)
    {
        throw new std::invalid_argument("pwzAspNetCoreFolderPath is NULL");
    }

    std::vector<fx_ver_t> versionsInDirectory;
    for (auto& p : fs::directory_iterator(pwzAspNetCoreFolderPath))
    {
        if (!fs::is_directory(p))
        {
            continue;
        }

        fx_ver_t requested_ver(-1, -1, -1);
        if (fx_ver_t::parse(p.path().filename(), &requested_ver, false))
        {
            versionsInDirectory.push_back(requested_ver);
        }
    }
    return versionsInDirectory;
}

// throws runtime error if no request handler versions are installed.
// Throw invalid_argument if any argument is null
std::wstring
GlobalVersionUtility::FindHighestGlobalVersion(PCWSTR pwzAspNetCoreFolderPath)
{
    if (pwzAspNetCoreFolderPath == nullptr)
    {
        throw std::invalid_argument("pwzAspNetCoreFolderPath is NULL");
    }

    std::vector<fx_ver_t> versionsInDirectory = GetRequestHandlerVersions(pwzAspNetCoreFolderPath);
    if (versionsInDirectory.empty())
    {
        throw std::runtime_error("Cannot find request handler next to aspnetcorev2.dll. Verify a version of the request handler is installed in a version folder.");
    }
    std::sort(versionsInDirectory.begin(), versionsInDirectory.end());

    return versionsInDirectory.back().as_str();
}

// Throws std::out_of_range if there is an index out of range
// Throw invalid_argument if any argument is null
std::wstring
GlobalVersionUtility::RemoveFileNameFromFolderPath(std::wstring fileName)
{
    fs::path path(fileName);
    return path.parent_path();
}

std::wstring
GlobalVersionUtility::GetModuleName(HMODULE hModuleName)
{
    DWORD dwSize = MAX_PATH;
    BOOL  fDone = FALSE;

    // Instead of creating a temporary buffer, use the std::wstring directly as the receive buffer.
    std::wstring retVal;
    retVal.resize(dwSize);

    while (!fDone)
    {
        DWORD dwReturnedSize = GetModuleFileNameW(hModuleName, &retVal[0], dwSize);
        if (dwReturnedSize == 0)
        {
            throw new std::runtime_error("GetModuleFileNameW returned 0.");
        }
        else if ((dwReturnedSize == dwSize) && (GetLastError() == ERROR_INSUFFICIENT_BUFFER))
        {
            dwSize *= 2;
            retVal.resize(dwSize); // smaller buffer. increase the buffer and retry
        }
        else
        {
            // GetModuleFilename will not account for the null terminator
            // std::wstring auto appends one, so we don't need to subtract 1 when resizing.
            retVal.resize(dwReturnedSize);
            fDone = TRUE;
        }
    }

    return retVal;
}
