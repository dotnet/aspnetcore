// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

APPLICATION_MANAGER* APPLICATION_MANAGER::sm_pApplicationManager = NULL;

HRESULT
APPLICATION_MANAGER::GetApplication(
    _In_ IHttpContext*         pContext,
    _In_ ASPNETCORE_CONFIG*    pConfig,
    _Out_ APPLICATION **       ppApplication
)
{
    HRESULT          hr = S_OK;
    APPLICATION     *pApplication = NULL;
    APPLICATION_KEY  key;
    BOOL             fExclusiveLock = FALSE;
    BOOL             fMixedHostingModelError = FALSE;
    BOOL             fDuplicatedInProcessApp = FALSE;
    PCWSTR           pszApplicationId = NULL;
    LPCWSTR          apsz[1];
    STACK_STRU ( strEventMsg, 256 );

    *ppApplication = NULL;
    
    DBG_ASSERT(pContext != NULL);
    DBG_ASSERT(pContext->GetApplication() != NULL);

    pszApplicationId = pContext->GetApplication()->GetApplicationId();

    hr = key.Initialize(pszApplicationId);
    if (FAILED(hr))
    {
        goto Finished;
    }

    m_pApplicationHash->FindKey(&key, ppApplication);

    if (*ppApplication == NULL)
    {
        switch (pConfig->QueryHostingModel())
        {
        case HOSTING_IN_PROCESS:
            if (m_pApplicationHash->Count() > 0)
            {
                // Only one inprocess app is allowed per IIS worker process
                fDuplicatedInProcessApp = TRUE;
                hr = HRESULT_FROM_WIN32(ERROR_APP_INIT_FAILURE);
                goto Finished;
            }
            pApplication = new IN_PROCESS_APPLICATION();
            break;

        case HOSTING_OUT_PROCESS:
            pApplication = new OUT_OF_PROCESS_APPLICATION();
            break;

        default:
            hr = E_UNEXPECTED;
            goto Finished;
        }

        if (pApplication == NULL)
        {
            hr = E_OUTOFMEMORY;
            goto Finished;
        }

        AcquireSRWLockExclusive(&m_srwLock);
        fExclusiveLock = TRUE;
        m_pApplicationHash->FindKey(&key, ppApplication);

        if (*ppApplication != NULL)
        {
            // someone else created the application
            delete pApplication;
            pApplication = NULL;
            goto Finished;
        }

        // hosting model check. We do not allow mixed scenario for now
        // could be changed in the future
        if (m_hostingModel != HOSTING_UNKNOWN)
        {
            if (m_hostingModel != pConfig->QueryHostingModel())
            {
                // hosting model does not match, error out
                fMixedHostingModelError = TRUE;
                hr = HRESULT_FROM_WIN32(ERROR_APP_INIT_FAILURE);
                goto Finished;
            }
        }

        hr = pApplication->Initialize(this, pConfig);
        if (FAILED(hr))
        {
            goto Finished;
        }

        hr = m_pApplicationHash->InsertRecord( pApplication );
        if (FAILED(hr))
        {
            goto Finished;
        }

        //
        // first application will decide which hosting model allowed by this process
        //
        if (m_hostingModel == HOSTING_UNKNOWN)
        {
            m_hostingModel = pConfig->QueryHostingModel();
        }

        ReleaseSRWLockExclusive(&m_srwLock);
        fExclusiveLock = FALSE;

        pApplication->StartMonitoringAppOffline();

        *ppApplication = pApplication;
        pApplication = NULL;
    }

Finished:

    if (fExclusiveLock)
    {
        ReleaseSRWLockExclusive(&m_srwLock);
    }

    if (FAILED(hr))
    {
        if (pApplication != NULL)
        {
            pApplication->DereferenceApplication();
            pApplication = NULL;
        }

        if (fDuplicatedInProcessApp)
        {
            if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP_MSG,
                pszApplicationId)))
            {
                apsz[0] = strEventMsg.QueryStr();
                if (FORWARDING_HANDLER::QueryEventLog() != NULL)
                {
                    ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
                        EVENTLOG_ERROR_TYPE,
                        0,
                        ASPNETCORE_EVENT_DUPLICATED_INPROCESS_APP,
                        NULL,
                        1,
                        0,
                        apsz,
                        NULL);
                }
            }
        }
        else if (fMixedHostingModelError)
        {
            if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR_MSG,
                pszApplicationId,
                pConfig->QueryHostingModelStr())))
            {
                apsz[0] = strEventMsg.QueryStr();
                if (FORWARDING_HANDLER::QueryEventLog() != NULL)
                {
                    ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
                        EVENTLOG_ERROR_TYPE,
                        0,
                        ASPNETCORE_EVENT_MIXED_HOSTING_MODEL_ERROR,
                        NULL,
                        1,
                        0,
                        apsz,
                        NULL);
                }
            }
        }
        else
        {
            if (SUCCEEDED(strEventMsg.SafeSnwprintf(
                ASPNETCORE_EVENT_ADD_APPLICATION_ERROR_MSG,
                pszApplicationId,
                hr)))
            {
                apsz[0] = strEventMsg.QueryStr();
                if (FORWARDING_HANDLER::QueryEventLog() != NULL)
                {
                    ReportEventW(FORWARDING_HANDLER::QueryEventLog(),
                        EVENTLOG_ERROR_TYPE,
                        0,
                        ASPNETCORE_EVENT_ADD_APPLICATION_ERROR,
                        NULL,
                        1,
                        0,
                        apsz,
                        NULL);
                }
            }
        }
    }

    return hr;
}

HRESULT
APPLICATION_MANAGER::RecycleApplication(
    _In_ LPCWSTR pszApplication
)
{
    HRESULT          hr = S_OK;
    APPLICATION_KEY  key;

    hr = key.Initialize(pszApplication);
    if (FAILED(hr))
    {
        goto Finished;
    }
    AcquireSRWLockExclusive(&m_srwLock);
    m_pApplicationHash->DeleteKey(&key);
    ReleaseSRWLockExclusive(&m_srwLock);

Finished:

    return hr;
}

HRESULT
APPLICATION_MANAGER::Get502ErrorPage(
    _Out_ HTTP_DATA_CHUNK**     ppErrorPage
)
{
    HRESULT           hr = S_OK;
    BOOL              fExclusiveLock = FALSE;
    HTTP_DATA_CHUNK  *pHttp502ErrorPage = NULL;

    DBG_ASSERT(ppErrorPage != NULL);

    //on-demand create the error page
    if (m_pHttp502ErrorPage != NULL)
    {
        *ppErrorPage = m_pHttp502ErrorPage;
    }
    else
    {
        AcquireSRWLockExclusive(&m_srwLock);
        fExclusiveLock = TRUE;
        if (m_pHttp502ErrorPage != NULL)
        {
            *ppErrorPage = m_pHttp502ErrorPage;
        }
        else
        {
            size_t maxsize = 5000;
            pHttp502ErrorPage = new HTTP_DATA_CHUNK();
            if (pHttp502ErrorPage == NULL)
            {
                hr = HRESULT_FROM_WIN32(ERROR_NOT_ENOUGH_MEMORY);
                goto Finished;
            }
            pHttp502ErrorPage->DataChunkType = HttpDataChunkFromMemory;
            pHttp502ErrorPage->FromMemory.pBuffer = (PVOID)m_pstrErrorInfo;

            pHttp502ErrorPage->FromMemory.BufferLength = (ULONG)strnlen(m_pstrErrorInfo, maxsize); //(ULONG)(wcslen(m_pstrErrorInfo)); // *sizeof(WCHAR);
            if(m_pHttp502ErrorPage != NULL)
            {
                delete m_pHttp502ErrorPage;
            }
            m_pHttp502ErrorPage = pHttp502ErrorPage;
            *ppErrorPage = m_pHttp502ErrorPage;
        }
    }

Finished:
    if (fExclusiveLock)
    {
        ReleaseSRWLockExclusive(&m_srwLock);
    }

    if (FAILED(hr))
    {
        if (pHttp502ErrorPage != NULL)
        {
            delete pHttp502ErrorPage;
        }
    }

    return hr;
}