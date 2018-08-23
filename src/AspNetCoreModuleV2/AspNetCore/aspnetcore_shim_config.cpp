// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "aspnetcore_shim_config.h"

#include "EventLog.h"
#include "config_utility.h"
#include "ahutil.h"

HRESULT
ASPNETCORE_SHIM_CONFIG::Populate(
    IHttpServer      *pHttpServer,
    IHttpApplication *pHttpApplication
)
{
    STACK_STRU(strHostingModel, 12);
    CComPtr<IAppHostElement>        pAspNetCoreElement;

    IAppHostAdminManager *pAdminManager = pHttpServer->GetAdminManager();
    const CComBSTR bstrAspNetCoreSection = CS_ASPNETCORE_SECTION;
    const CComBSTR applicationConfigPath = pHttpApplication->GetAppConfigPath();

    RETURN_IF_FAILED(pAdminManager->GetAdminSection(bstrAspNetCoreSection,
        applicationConfigPath,
        &pAspNetCoreElement));

    CComBSTR struProcessPath;
    RETURN_IF_FAILED(GetElementStringProperty(pAspNetCoreElement,
        CS_ASPNETCORE_PROCESS_EXE_PATH,
        &struProcessPath));
    m_strProcessPath = struProcessPath;

    // Swallow this error for backward compatibility
    // Use default behavior for empty string
    GetElementStringProperty(pAspNetCoreElement,
        CS_ASPNETCORE_HOSTING_MODEL,
        &strHostingModel);

    if (strHostingModel.IsEmpty() || strHostingModel.Equals(L"outofprocess", TRUE))
    {
        m_hostingModel = HOSTING_OUT_PROCESS;
    }
    else if (strHostingModel.Equals(L"inprocess", TRUE))
    {
        m_hostingModel = HOSTING_IN_PROCESS;
    }
    else
    {
        // block unknown hosting value
        EventLog::Error(
            ASPNETCORE_EVENT_UNKNOWN_HOSTING_MODEL_ERROR,
            ASPNETCORE_EVENT_UNKNOWN_HOSTING_MODEL_ERROR_MSG,
            strHostingModel.QueryStr());
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_NOT_SUPPORTED));
    }

    CComBSTR struArguments;
    RETURN_IF_FAILED(GetElementStringProperty(pAspNetCoreElement,
        CS_ASPNETCORE_PROCESS_ARGUMENTS,
        &struArguments));

    m_strArguments = struArguments;

    if (m_hostingModel == HOSTING_OUT_PROCESS)
    {
        STRU struHandlerVersion;
        RETURN_IF_FAILED(ConfigUtility::FindHandlerVersion(pAspNetCoreElement, struHandlerVersion));
        m_strHandlerVersion = struHandlerVersion.QueryStr();
    }


    RETURN_IF_FAILED(GetElementBoolProperty(pAspNetCoreElement,
        CS_ASPNETCORE_STDOUT_LOG_ENABLED,
        &m_fStdoutLogEnabled));
    RETURN_IF_FAILED(GetElementStringProperty(pAspNetCoreElement,
        CS_ASPNETCORE_STDOUT_LOG_FILE,
        &m_struStdoutLogFile));

    return S_OK;
}
