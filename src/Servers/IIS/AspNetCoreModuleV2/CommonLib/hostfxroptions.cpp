// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "hostfxroptions.h"

#include "hostfxr_utility.h"
#include "debugutil.h"
#include "exceptions.h"
#include "EventLog.h"

HRESULT HOSTFXR_OPTIONS::Create(
        _In_ const std::wstring& pcwzDotnetExePath,
        _In_ const std::wstring& pcwzProcessPath,
        _In_ const std::wstring& pcwzApplicationPhysicalPath,
        _In_ const std::wstring& pcwzArguments,
        _Out_ std::unique_ptr<HOSTFXR_OPTIONS>& ppWrapper)
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
        HOSTFXR_UTILITY::GetHostFxrParameters(
                pcwzProcessPath,
                pcwzApplicationPhysicalPath,
                pcwzArguments,
                hostFxrDllPath,
                knownDotnetLocation,
                arguments);

        LOG_INFOF(L"Parsed hostfxr options: dotnet location: '%ls' hostfxr path: '%ls' arguments:", knownDotnetLocation.c_str(), hostFxrDllPath.c_str());
        for (size_t i = 0; i < arguments.size(); i++)
        {
            LOG_INFOF(L"Argument[%d] = '%ls'", i, arguments[i].c_str());
        }
        ppWrapper = std::make_unique<HOSTFXR_OPTIONS>(knownDotnetLocation, hostFxrDllPath, arguments);
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
