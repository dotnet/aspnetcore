// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
class Helpers
{
public:

    static
        std::wstring
        CreateRandomValue();

    static
        std::wstring
        CreateRandomTempDirectory();

    static
        void
        DeleteDirectory(std::wstring directory);

    static
        std::wstring
        ReadFileContent(std::wstring file);
};

