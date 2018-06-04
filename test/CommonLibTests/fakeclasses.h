// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"

class MockHttpServer : public IHttpServer
{
    // Inherited via IHttpServer
    virtual BOOL IsCommandLineLaunch(VOID) const override
    {
        return 0;
    }
    virtual PCWSTR GetAppPoolName(VOID) const override
    {
        return PCWSTR();
    }
    virtual HRESULT AssociateWithThreadPool(HANDLE hHandle, LPOVERLAPPED_COMPLETION_ROUTINE completionRoutine) override
    {
        return E_NOTIMPL;
    }
    virtual VOID IncrementThreadCount(VOID) override
    {
        return VOID();
    }
    virtual VOID DecrementThreadCount(VOID) override
    {
        return VOID();
    }
    virtual VOID ReportUnhealthy(PCWSTR pszReasonString, HRESULT hrReason) override
    {
        return VOID();
    }
    virtual VOID RecycleProcess(PCWSTR pszReason) override
    {
        return VOID();
    }
    virtual IAppHostAdminManager * GetAdminManager(VOID) const override
    {
        return nullptr;
    }
    virtual HRESULT GetFileInfo(PCWSTR pszPhysicalPath, HANDLE hUserToken, PSID pSid, PCWSTR pszChangeNotificationPath, HANDLE hChangeNotificationToken, BOOL fCache, IHttpFileInfo ** ppFileInfo, IHttpTraceContext * pHttpTraceContext = NULL) override
    {
        return E_NOTIMPL;
    }
    virtual HRESULT FlushKernelCache(PCWSTR pszUrl) override
    {
        return E_NOTIMPL;
    }
    virtual HRESULT DoCacheOperation(CACHE_OPERATION cacheOperation, IHttpCacheKey * pCacheKey, IHttpCacheSpecificData ** ppCacheSpecificData, IHttpTraceContext * pHttpTraceContext = NULL) override
    {
        return E_NOTIMPL;
    }
    virtual GLOBAL_NOTIFICATION_STATUS NotifyCustomNotification(ICustomNotificationProvider * pCustomOutput) override
    {
        return GLOBAL_NOTIFICATION_STATUS();
    }
    virtual IHttpPerfCounterInfo * GetPerfCounterInfo(VOID) override
    {
        return nullptr;
    }
    virtual VOID RecycleApplication(PCWSTR pszAppConfigPath) override
    {
        return VOID();
    }
    virtual VOID NotifyConfigurationChange(PCWSTR pszPath) override
    {
        return VOID();
    }
    virtual VOID NotifyFileChange(PCWSTR pszFileName) override
    {
        return VOID();
    }
    virtual IDispensedHttpModuleContextContainer * DispenseContainer(VOID) override
    {
        return nullptr;
    }
    virtual HRESULT AddFragmentToCache(HTTP_DATA_CHUNK * pDataChunk, PCWSTR pszFragmentName) override
    {
        return E_NOTIMPL;
    }
    virtual HRESULT ReadFragmentFromCache(PCWSTR pszFragmentName, BYTE * pvBuffer, DWORD cbSize, DWORD * pcbCopied) override
    {
        return E_NOTIMPL;
    }
    virtual HRESULT RemoveFragmentFromCache(PCWSTR pszFragmentName) override
    {
        return E_NOTIMPL;
    }
    virtual HRESULT GetWorkerProcessSettings(IWpfSettings ** ppWorkerProcessSettings) override
    {
        return E_NOTIMPL;
    }
    virtual HRESULT GetProtocolManagerCustomInterface(PCWSTR pProtocolManagerDll, PCWSTR pProtocolManagerDllInitFunction, DWORD dwCustomInterfaceId, PVOID * ppCustomInterface) override
    {
        return E_NOTIMPL;
    }
    virtual BOOL SatisfiesPrecondition(PCWSTR pszPrecondition, BOOL * pfUnknownPrecondition = NULL) const override
    {
        return 0;
    }
    virtual IHttpTraceContext * GetTraceContext(VOID) const override
    {
        return nullptr;
    }
    virtual HRESULT RegisterFileChangeMonitor(PCWSTR pszPath, HANDLE hToken, IHttpFileMonitor ** ppFileMonitor) override
    {
        return E_NOTIMPL;
    }
    virtual HRESULT GetExtendedInterface(HTTP_SERVER_INTERFACE_VERSION version, PVOID * ppInterface) override
    {
        return E_NOTIMPL;
    }
};

class MockRequestHandlerConfig : public REQUESTHANDLER_CONFIG
{
public:
    static
        MockRequestHandlerConfig*
        CreateConfig()
    {
        return new MockRequestHandlerConfig;
    }

private:
    MockRequestHandlerConfig()
    {

    }
};

