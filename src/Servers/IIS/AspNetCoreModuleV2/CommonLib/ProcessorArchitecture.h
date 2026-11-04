// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

enum class ProcessorArchitecture
{
    Unknown,
    x86,
    AMD64,
    ARM64
};

inline const wchar_t* ProcessorArchitectureToString(ProcessorArchitecture arch)
{
    switch (arch)
    {
        case ProcessorArchitecture::x86:
            return L"x86";
        case ProcessorArchitecture::AMD64:
            return L"AMD64";
        case ProcessorArchitecture::ARM64:
            return L"ARM64";
        case ProcessorArchitecture::Unknown:
        default:
            return L"Unknown";
    }
}