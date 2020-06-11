// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "HostFxrResolutionResult.h"

#include "HostFxrResolver.h"
#include "debugutil.h"
#include "exceptions.h"
#include "EventLog.h"

void HostFxrResolutionResult::GetArguments(DWORD& hostfxrArgc, std::unique_ptr<PCWSTR[]>& hostfxrArgv) const
{
    hostfxrArgc = static_cast<DWORD>(m_arguments.size());
    hostfxrArgv = std::make_unique<PCWSTR[]>(hostfxrArgc);
    for (DWORD i = 0; i < hostfxrArgc; ++i)
    {
        hostfxrArgv[i] = m_arguments[i].c_str();
    }
}

HRESULT HostFxrResolutionResult::Create(
        _In_ const std::wstring& pcwzDotnetExePath,
        _In_ const std::wstring& pcwzProcessPath,
        _In_ const std::wstring& pcwzApplicationPhysicalPath,
        _In_ const std::wstring& pcwzArguments,
        _In_ ErrorContext& errorContext,
        _Out_ std::unique_ptr<HostFxrResolutionResult>& ppWrapper)
{
    std::filesystem::path knownDotnetLocation;

    if (!pcwzDotnetExePath.empty())
    {
        knownDotnetLocation = pcwzDotnetExePath;
    }

    try
    {
        std::filesystem::path hostFxrDllPath;
        std::vector<std::wstring> arguments;
        HostFxrResolver::GetHostFxrParameters(
                pcwzProcessPath,
                pcwzApplicationPhysicalPath,
                pcwzArguments,
                hostFxrDllPath,
                knownDotnetLocation,
                arguments,
                errorContext);

        LOG_INFOF(L"Parsed hostfxr options: dotnet location: '%ls' hostfxr path: '%ls' arguments:", knownDotnetLocation.c_str(), hostFxrDllPath.c_str());
        for (size_t i = 0; i < arguments.size(); i++)
        {
            LOG_INFOF(L"Argument[%d] = '%ls'", i, arguments[i].c_str());
        }
        ppWrapper = std::make_unique<HostFxrResolutionResult>(knownDotnetLocation, hostFxrDllPath, arguments);
    }
    catch (InvalidOperationException &ex)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_INPROCESS_START_ERROR,
            ASPNETCORE_EVENT_INPROCESS_START_ERROR_MSG,
            pcwzApplicationPhysicalPath.c_str(),
            ex.as_wstring().c_str());

        RETURN_CAUGHT_EXCEPTION();
    }
    catch (std::runtime_error &ex)
    {
        EventLog::Error(
            ASPNETCORE_EVENT_INPROCESS_START_ERROR,
            ASPNETCORE_EVENT_INPROCESS_START_ERROR_MSG,
            pcwzApplicationPhysicalPath.c_str(),
            GetUnexpectedExceptionMessage(ex).c_str());

        RETURN_CAUGHT_EXCEPTION();
    }
    CATCH_RETURN();

    return S_OK;
}
