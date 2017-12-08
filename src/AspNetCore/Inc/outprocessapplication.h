// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "application.h"

class OUT_OF_PROCESS_APPLICATION : public APPLICATION
{
public:
    OUT_OF_PROCESS_APPLICATION();

    ~OUT_OF_PROCESS_APPLICATION();

    __override
    HRESULT Initialize(_In_ APPLICATION_MANAGER* pApplicationManager,
                       _In_ ASPNETCORE_CONFIG*   pConfiguration);

    __override
    VOID OnAppOfflineHandleChange();

    __override
   REQUEST_NOTIFICATION_STATUS
   ExecuteRequest(
       _In_ IHttpContext* pHttpContext
   );

    HRESULT
    GetProcess(
        _In_    IHttpContext          *context,
        _Out_   SERVER_PROCESS       **ppServerProcess
    )
    {
        return m_pProcessManager->GetProcess(context, m_pConfiguration, ppServerProcess);
    }

private:

    PROCESS_MANAGER*        m_pProcessManager;
};
