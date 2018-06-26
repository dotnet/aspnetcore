// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "precomp.hxx"
#include "requesthandler.h"
#include "StartupExceptionApplication.h"

class StartupExceptionApplication;

class StartupExceptionHandler : public REQUEST_HANDLER
{
public:
    virtual REQUEST_NOTIFICATION_STATUS OnExecuteRequestHandler() override;

    virtual REQUEST_NOTIFICATION_STATUS OnAsyncCompletion(DWORD cbCompletion, HRESULT hrCompletionStatus) override;

    virtual VOID TerminateRequest(bool fClientInitiated) override;

    StartupExceptionHandler(IHttpContext* pContext, BOOL disableLogs, StartupExceptionApplication* pApplication)
        :
        m_pContext(pContext),
        m_disableLogs(disableLogs),
        m_pApplication(pApplication)
    {
    }

    ~StartupExceptionHandler()
    {
    }

private:
    IHttpContext * m_pContext;
    BOOL        m_disableLogs;
    StartupExceptionApplication* m_pApplication;
};

