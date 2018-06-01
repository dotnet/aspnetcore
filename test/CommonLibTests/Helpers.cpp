// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

std::wstring
Helpers::CreateRandomValue()
{
    int randomValue = rand();
    return std::to_wstring(randomValue);
}

std::wstring
Helpers::CreateRandomTempDirectory()
{
    PWSTR tempPath = new WCHAR[256];
    GetTempPath(256, tempPath);
    std::wstring wstringPath(tempPath);

    return wstringPath.append(Helpers::CreateRandomValue()).append(L"\\");
}

void
Helpers::DeleteDirectory(std::wstring directory)
{
    std::experimental::filesystem::remove_all(directory);
}

std::wstring
Helpers::ReadFileContent(std::wstring file)
{
    std::wcout << file << std::endl;

    std::fstream t(file);
    std::stringstream buffer;
    buffer << t.rdbuf();

    int nChars = MultiByteToWideChar(CP_ACP,  0, buffer.str().c_str(), -1, NULL, 0);

    LPWSTR pwzName = new WCHAR[nChars];
    MultiByteToWideChar(CP_UTF8, 0, buffer.str().c_str(), -1, pwzName, nChars);

    std::wstring retVal(pwzName);

    delete pwzName;

    return retVal;
}
