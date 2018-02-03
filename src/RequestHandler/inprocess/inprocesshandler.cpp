#include "..\precomp.hxx"

IN_PROCESS_HANDLER::IN_PROCESS_HANDLER(
    _In_ IHttpContext   *pW3Context,
    _In_ HTTP_MODULE_ID *pModuleId,
    _In_ APPLICATION    *pApplication
): REQUEST_HANDLER(pW3Context, pModuleId, pApplication)
{
    m_fManagedRequestComplete = FALSE;
}

IN_PROCESS_HANDLER::~IN_PROCESS_HANDLER()
{
    //todo
}

__override
REQUEST_NOTIFICATION_STATUS
IN_PROCESS_HANDLER::OnExecuteRequestHandler()
{
    // First get the in process Application
    HRESULT hr;
    hr = ((IN_PROCESS_APPLICATION*)m_pApplication)->LoadManagedApplication();
    if (FAILED(hr))
    {
        // TODO remove com_error?
        /*_com_error err(hr);
        if (ANCMEvents::ANCM_START_APPLICATION_FAIL::IsEnabled(m_pW3Context->GetTraceContext()))
        {
            ANCMEvents::ANCM_START_APPLICATION_FAIL::RaiseEvent(
                m_pW3Context->GetTraceContext(),
                NULL,
                err.ErrorMessage());
        }
        */
        //fInternalError = TRUE;
        m_pW3Context->GetResponse()->SetStatus(500, "Internal Server Error", 0, hr);
        return REQUEST_NOTIFICATION_STATUS::RQ_NOTIFICATION_FINISH_REQUEST;
    }

    // FREB log
    
    if (ANCMEvents::ANCM_START_APPLICATION_SUCCESS::IsEnabled(m_pW3Context->GetTraceContext()))
    {
        ANCMEvents::ANCM_START_APPLICATION_SUCCESS::RaiseEvent(
            m_pW3Context->GetTraceContext(),
            NULL,
            L"InProcess Application");
    }

    //SetHttpSysDisconnectCallback();
    return ((IN_PROCESS_APPLICATION*)m_pApplication)->OnExecuteRequest(m_pW3Context, this);
}

__override
REQUEST_NOTIFICATION_STATUS
IN_PROCESS_HANDLER::OnAsyncCompletion(
    DWORD       cbCompletion,
    HRESULT     hrCompletionStatus
)
{
    HRESULT hr;
    if (FAILED(hrCompletionStatus))
    {
        return RQ_NOTIFICATION_FINISH_REQUEST;
    }
    else
    {
        // For now we are assuming we are in our own self contained box. 
        // TODO refactor Finished and Failure sections to handle in process and out of process failure.
        // TODO verify that websocket's OnAsyncCompletion is not calling this.
        IN_PROCESS_APPLICATION* application = (IN_PROCESS_APPLICATION*)m_pApplication;
        if (application == NULL)
        {
            hr = E_FAIL;
            return RQ_NOTIFICATION_FINISH_REQUEST;
        }

        return application->OnAsyncCompletion(cbCompletion, hrCompletionStatus, this);
    }
}

VOID
IN_PROCESS_HANDLER::TerminateRequest(
    bool    fClientInitiated
)
{
    UNREFERENCED_PARAMETER(fClientInitiated);
    //todo
}

PVOID
IN_PROCESS_HANDLER::QueryManagedHttpContext(
    VOID
)
{
    return m_pManagedHttpContext;
}

BOOL
IN_PROCESS_HANDLER::QueryIsManagedRequestComplete(
    VOID
)
{
    return m_fManagedRequestComplete;
}

IHttpContext*
IN_PROCESS_HANDLER::QueryHttpContext(
    VOID
)
{
    return m_pW3Context;
}

VOID
IN_PROCESS_HANDLER::IndicateManagedRequestComplete(
    VOID
)
{
    m_fManagedRequestComplete = TRUE;
}

REQUEST_NOTIFICATION_STATUS
IN_PROCESS_HANDLER::QueryAsyncCompletionStatus(
    VOID
)
{
    return m_requestNotificationStatus;
}

VOID
IN_PROCESS_HANDLER::SetAsyncCompletionStatus(
    REQUEST_NOTIFICATION_STATUS requestNotificationStatus
)
{
    m_requestNotificationStatus = requestNotificationStatus;
}

VOID
IN_PROCESS_HANDLER::SetManangedHttpContext(
    PVOID pManagedHttpContext
)
{
    m_pManagedHttpContext = pManagedHttpContext;
}
