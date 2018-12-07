// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

APPLICATION_MANAGER* APPLICATION_MANAGER::sm_pApplicationManager = NULL;

HRESULT
APPLICATION_MANAGER::GetApplication(
    _In_ IHttpContext*         pContext,
    _Out_ APPLICATION **       ppApplication
)
{
    HRESULT          hr = S_OK;
    APPLICATION     *pApplication = NULL;
    APPLICATION_KEY  key;
    BOOL             fExclusiveLock = FALSE;
    PCWSTR           pszApplicationId = NULL;

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

        pApplication = new APPLICATION();
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

        hr = pApplication->Initialize(this, pszApplicationId, pContext->GetApplication()->GetApplicationPhysicalPath());
        if (FAILED(hr))
        {
            goto Finished;
        }

        hr = m_pApplicationHash->InsertRecord( pApplication );

        if (FAILED(hr))
        {
            goto Finished;
        }
        ReleaseSRWLockExclusive(&m_srwLock);
        fExclusiveLock = FALSE;

        pApplication->StartMonitoringAppOffline();

        *ppApplication = pApplication;
        pApplication = NULL;
    }

Finished:

    if (fExclusiveLock == TRUE)
        ReleaseSRWLockExclusive(&m_srwLock);

    if (FAILED(hr))
    {
        if (pApplication != NULL)
        {
            pApplication->DereferenceApplication();
            pApplication = NULL;
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
