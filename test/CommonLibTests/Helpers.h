// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
class Helpers
{
public:
    static
        std::wstring
        ReadFileContent(std::wstring file);
};

class TempDirectory
{
public:

    TempDirectory();

    ~TempDirectory();

    std::experimental::filesystem::path path() const
    {
        return m_path;
    }

private:
    std::experimental::filesystem::path m_path;
};
