// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include <optional>

class Environment
{
public:
    Environment() = delete;
    ~Environment() = delete;

    static
    std::wstring ExpandEnvironmentVariables(const std::wstring & str);
    static
    std::optional<std::wstring> GetEnvironmentVariableValue(const std::wstring & str);
    static
    std::wstring GetCurrentDirectoryValue();
    static
    std::wstring GetDllDirectoryValue();
    static
    bool IsRunning64BitProcess();
    static
    HRESULT CopyToDirectory(const std::wstring& source, const std::filesystem::path& destination, bool cleanDest, const std::filesystem::path& directoryToIgnore, int& copiedFileCount);
    static
    bool CheckUpToDate(const std::wstring& source, const std::filesystem::path& destination, const std::wstring& extension, const std::filesystem::path& directoryToIgnore);
private:
    static
    void CopyToDirectoryInner(const std::filesystem::path& source_folder, const std::filesystem::path& target_folder, const std::filesystem::path& directoryToIgnore, int& copiedFileCount);
};

