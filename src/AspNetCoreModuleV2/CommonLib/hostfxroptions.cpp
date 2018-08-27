// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "hostfxroptions.h"

#include "hostfxr_utility.h"
#include "debugutil.h"
#include "exceptions.h"
#include "EventLog.h"

HRESULT HOSTFXR_OPTIONS::Create(
        _In_ PCWSTR         pcwzDotnetExePath,
        _In_ PCWSTR         pcwzProcessPath,
        _In_ PCWSTR         pcwzApplicationPhysicalPath,
        _In_ PCWSTR         pcwzArguments,
        _Out_ std::unique_ptr<HOSTFXR_OPTIONS>& ppWrapper)
{
    std::filesystem::path knownDotnetLocation;

    if (pcwzDotnetExePath != nullptr)
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
    catch (HOSTFXR_UTILITY::StartupParametersResolutionException &resolutionException)
    {
        OBSERVE_CAUGHT_EXCEPTION();

        EventLog::Error(
            ASPNETCORE_EVENT_INPROCESS_START_ERROR,
            ASPNETCORE_EVENT_INPROCESS_START_ERROR_MSG,
            pcwzApplicationPhysicalPath,
            resolutionException.get_message().c_str());

        return E_FAIL;
    }
    CATCH_RETURN();

    return S_OK;
}
