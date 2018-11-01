// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"

std::wstring
Helpers::ReadFileContent(std::wstring file)
{
    std::wcout << file << std::endl;

    std::fstream t(file);
    std::stringstream buffer;
    buffer << t.rdbuf();

    int nChars = MultiByteToWideChar(CP_ACP,  0, buffer.str().c_str(), -1, NULL, 0);

    std::wstring retVal(nChars, '\0');

    MultiByteToWideChar(CP_UTF8, 0, buffer.str().c_str(), -1, retVal.data(), nChars);

    return retVal;
}

TempDirectory::TempDirectory()
{
    UUID uuid;
    UuidCreate(&uuid);
    RPC_CSTR szUuid = NULL;
    if (UuidToStringA(&uuid, &szUuid) == RPC_S_OK)
    {
        m_path = std::filesystem::temp_directory_path() / reinterpret_cast<PCHAR>(szUuid);
        RpcStringFreeA(&szUuid);
        return;
    }
    throw std::exception("Cannot create temp directory");
}

TempDirectory::~TempDirectory()
{
    std::filesystem::remove_all(m_path);
}
