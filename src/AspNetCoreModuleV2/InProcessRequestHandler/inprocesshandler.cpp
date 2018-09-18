// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "inprocesshandler.h"
#include "inprocessapplication.h"
#include "aspnetcore_event.h"
#include "IOutputManager.h"
#include "ShuttingDownApplication.h"
#include "ntassert.h"

ALLOC_CACHE_HANDLER * IN_PROCESS_HANDLER::sm_pAlloc = NULL;

IN_PROCESS_HANDLER::IN_PROCESS_HANDLER(
    _In_ std::unique_ptr<IN_PROCESS_APPLICATION, IAPPLICATION_DELETER> pApplication,
    _In_ IHttpContext   *pW3Context,
    _In_ PFN_REQUEST_HANDLER pRequestHandler,
    _In_ void * pRequestHandlerContext,
    _In_ PFN_DISCONNECT_HANDLER pDisconnectHandler,
    _In_ PFN_ASYNC_COMPLETION_HANDLER pAsyncCompletion
): m_pManagedHttpContext(nullptr),
   m_requestNotificationStatus(RQ_NOTIFICATION_PENDING),
   m_fManagedRequestComplete(FALSE),
   m_pW3Context(pW3Context),
   m_pApplication(std::move(pApplication)),
   m_pRequestHandler(pRequestHandler),
   m_pRequestHandlerContext(pRequestHandlerContext),
   m_pAsyncCompletionHandler(pAsyncCompletion),
   m_pDisconnectHandler(pDisconnectHandler)
{
}

__override
REQUEST_NOTIFICATION_STATUS
IN_PROCESS_HANDLER::OnExecuteRequestHandler()
{
    // FREB log

    if (ANCMEvents::ANCM_START_APPLICATION_SUCCESS::IsEnabled(m_pW3Context->GetTraceContext()))
    {
        ANCMEvents::ANCM_START_APPLICATION_SUCCESS::RaiseEvent(
            m_pW3Context->GetTraceContext(),
            NULL,
            L"InProcess Application");
    }

    if (m_pRequestHandler == NULL)
    {
        //
        // return error as the application did not register callback
        //
        if (ANCMEvents::ANCM_EXECUTE_REQUEST_FAIL::IsEnabled(m_pW3Context->GetTraceContext()))
        {
            ANCMEvents::ANCM_EXECUTE_REQUEST_FAIL::RaiseEvent(m_pW3Context->GetTraceContext(),
                                                              NULL,
                                                              (ULONG)E_APPLICATION_ACTIVATION_EXEC_FAILURE);
        }

        m_pW3Context->GetResponse()->SetStatus(500,
                                               "Internal Server Error",
                                               0,
                                               (ULONG)E_APPLICATION_ACTIVATION_EXEC_FAILURE);

        return RQ_NOTIFICATION_FINISH_REQUEST;
    }
    else if (m_pApplication->QueryBlockCallbacksIntoManaged())
    {
        return ServerShutdownMessage();
    }

    return m_pRequestHandler(this, m_pRequestHandlerContext);
}

__override
REQUEST_NOTIFICATION_STATUS
IN_PROCESS_HANDLER::OnAsyncCompletion(
    DWORD       cbCompletion,
    HRESULT     hrCompletionStatus
)
{
    if (m_fManagedRequestComplete)
    {
        // means PostCompletion has been called and this is the associated callback.
        return m_requestNotificationStatus;
    }
    if (m_pApplication->QueryBlockCallbacksIntoManaged())
    {
        // this can potentially happen in ungraceful shutdown.
        // Or something really wrong happening with async completions
        // At this point, managed is in a shutting down state and we cannot send a request to it.
        return ServerShutdownMessage();
    }

    assert(m_pManagedHttpContext != nullptr);
    // Call the managed handler for async completion.
    return m_pAsyncCompletionHandler(m_pManagedHttpContext, hrCompletionStatus, cbCompletion);
}

REQUEST_NOTIFICATION_STATUS IN_PROCESS_HANDLER::ServerShutdownMessage() const
{
    return ShuttingDownHandler::ServerShutdownMessage(m_pW3Context);
}

VOID
IN_PROCESS_HANDLER::NotifyDisconnect()
{
    if (m_pApplication->QueryBlockCallbacksIntoManaged() ||
        m_fManagedRequestComplete)
    {
        return;
    }

    assert(m_pManagedHttpContext != nullptr);
    m_pDisconnectHandler(m_pManagedHttpContext);
}

VOID
IN_PROCESS_HANDLER::IndicateManagedRequestComplete(
    VOID
)
{
    m_fManagedRequestComplete = TRUE;
    m_pManagedHttpContext = nullptr;
}

VOID
IN_PROCESS_HANDLER::SetAsyncCompletionStatus(
    REQUEST_NOTIFICATION_STATUS requestNotificationStatus
)
{
    m_requestNotificationStatus = requestNotificationStatus;
}

VOID
IN_PROCESS_HANDLER::SetManagedHttpContext(
    PVOID pManagedHttpContext
)
{
    m_pManagedHttpContext = pManagedHttpContext;
}

// static
void * IN_PROCESS_HANDLER::operator new(size_t)
{
    DBG_ASSERT(sm_pAlloc != NULL);
    if (sm_pAlloc == NULL)
    {
        return NULL;
    }
    return sm_pAlloc->Alloc();
}

// static
void IN_PROCESS_HANDLER::operator delete(void * pMemory)
{
    DBG_ASSERT(sm_pAlloc != NULL);
    if (sm_pAlloc != NULL)
    {
        sm_pAlloc->Free(pMemory);
    }
}

// static
HRESULT
IN_PROCESS_HANDLER::StaticInitialize(VOID)
/*++

Routine Description:

Global initialization routine for IN_PROCESS_HANDLER

Return Value:

HRESULT

--*/
{
    HRESULT                         hr = S_OK;

    sm_pAlloc = new ALLOC_CACHE_HANDLER;
    if (sm_pAlloc == NULL)
    {
        hr = E_OUTOFMEMORY;
        goto Finished;
    }

    hr = sm_pAlloc->Initialize(sizeof(IN_PROCESS_HANDLER),
                               64); // nThreshold

Finished:
    if (FAILED(hr))
    {
        StaticTerminate();
    }
    return hr;
}

// static
void
IN_PROCESS_HANDLER::StaticTerminate(VOID)
{
    if (sm_pAlloc != NULL)
    {
        delete sm_pAlloc;
        sm_pAlloc = NULL;
    }
}
