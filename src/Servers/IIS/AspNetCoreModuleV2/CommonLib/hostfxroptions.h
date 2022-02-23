// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <Windows.h>
#include <memory>
#include <filesystem>
#include <utility>
#include <vector>
#include <string>

class HOSTFXR_OPTIONS
{
public:
    HOSTFXR_OPTIONS(
        std::filesystem::path dotnetExeLocation,
        std::filesystem::path hostFxrLocation,
        std::vector<std::wstring> arguments
        ) noexcept
    :   m_dotnetExeLocation(std::move(dotnetExeLocation)),
        m_hostFxrLocation(std::move(hostFxrLocation)),
        m_arguments(std::move(arguments))
    {}

    void
    GetArguments(DWORD &hostfxrArgc, std::unique_ptr<PCWSTR[]> &hostfxrArgv) const
    {
        hostfxrArgc = static_cast<DWORD>(m_arguments.size());
        hostfxrArgv = std::make_unique<PCWSTR[]>(hostfxrArgc);
        for (DWORD i = 0; i < hostfxrArgc; ++i)
        {
            hostfxrArgv[i] = m_arguments[i].c_str();
        }
    }

    const std::filesystem::path&
    GetHostFxrLocation() const noexcept
    {
        return m_hostFxrLocation;
    }

    const std::filesystem::path&
    GetDotnetExeLocation() const noexcept
    {
        return m_dotnetExeLocation;
    }

    static
    HRESULT Create(
         _In_  const std::wstring& pcwzExeLocation,
         _In_  const std::wstring& pcwzProcessPath,
         _In_  const std::wstring& pcwzApplicationPhysicalPath,
         _In_  const std::wstring& pcwzArguments,
         _Out_ std::unique_ptr<HOSTFXR_OPTIONS>& ppWrapper);

private:
    const std::filesystem::path m_dotnetExeLocation;
    const std::filesystem::path m_hostFxrLocation;
    const std::vector<std::wstring> m_arguments;
};
