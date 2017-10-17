// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "precomp.hxx"

IN_PROCESS_STORED_CONTEXT::IN_PROCESS_STORED_CONTEXT(
    IHttpContext* pHttpContext,
    PVOID pMangedHttpContext
)
{
    m_pManagedHttpContext = pMangedHttpContext;
    m_pHttpContext = pHttpContext;
}

IN_PROCESS_STORED_CONTEXT::~IN_PROCESS_STORED_CONTEXT()
{
}

PVOID
IN_PROCESS_STORED_CONTEXT::QueryManagedHttpContext(
    VOID
)
{
    return m_pManagedHttpContext;
}

IHttpContext*
IN_PROCESS_STORED_CONTEXT::QueryHttpContext(
    VOID
)
{
    return m_pHttpContext;
}

HRESULT
IN_PROCESS_STORED_CONTEXT::GetInProcessStoredContext(
    IHttpContext*               pHttpContext,
    IN_PROCESS_STORED_CONTEXT** ppInProcessStoredContext
)
{
    if (pHttpContext == NULL)
    {
        return E_FAIL;
    }

    if (ppInProcessStoredContext == NULL)
    {
        return E_FAIL;
    }

    *ppInProcessStoredContext = (IN_PROCESS_STORED_CONTEXT*)pHttpContext->GetModuleContextContainer()->GetModuleContext(g_pModuleId);
    if (*ppInProcessStoredContext == NULL)
    {
        return E_FAIL;
    }

    return S_OK;
}

HRESULT
IN_PROCESS_STORED_CONTEXT::SetInProcessStoredContext(
    IHttpContext*               pHttpContext,
    IN_PROCESS_STORED_CONTEXT* pInProcessStoredContext
)
{
    if (pHttpContext == NULL)
    {
        return E_FAIL;
    }
    if (pInProcessStoredContext == NULL)
    {
        return E_FAIL;
    }

    return pHttpContext->GetModuleContextContainer()->SetModuleContext(
        pInProcessStoredContext,
        g_pModuleId
    );
}