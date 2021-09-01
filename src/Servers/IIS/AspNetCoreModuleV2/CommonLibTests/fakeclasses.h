// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

#include "gtest/gtest.h"
#include "gmock/gmock.h"
#include "InProcessOptions.h"

class MockProperty : public IAppHostProperty
{
public:
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, QueryInterface, HRESULT(REFIID riid, void ** ppvObject));
    MOCK_METHOD0_WITH_CALLTYPE(__stdcall, AddRef, ULONG());
    MOCK_METHOD0_WITH_CALLTYPE(__stdcall, Release, ULONG());
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Name, HRESULT(BSTR* pbstrValue));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Value, HRESULT(VARIANT * pVariant));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, put_Value, HRESULT(VARIANT value));
    MOCK_METHOD0_WITH_CALLTYPE(__stdcall, Clear, HRESULT());
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_StringValue, HRESULT(BSTR* pbstrValue));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Exception, HRESULT(IAppHostPropertyException ** ppException));
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetMetadata, HRESULT(BSTR bstrMetadataType, VARIANT * pValue));
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, SetMetadata, HRESULT(BSTR bstrMetadataType, VARIANT value));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Schema, HRESULT(IAppHostPropertySchema ** ppSchema));
};

class MockCollection : public IAppHostElementCollection
{
public:
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, QueryInterface, HRESULT(REFIID riid, void ** ppvObject));
    MOCK_METHOD0_WITH_CALLTYPE(__stdcall, AddRef, ULONG());
    MOCK_METHOD0_WITH_CALLTYPE(__stdcall, Release, ULONG());
    MOCK_METHOD0_WITH_CALLTYPE(__stdcall, Clear, HRESULT());
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Schema, HRESULT(IAppHostCollectionSchema** pSchema));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Count, HRESULT(DWORD * dwordElem));
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, get_Item, HRESULT(VARIANT cIndex, IAppHostElement ** ppElement));
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, AddElement, HRESULT(IAppHostElement * pElement, INT cPosition));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, DeleteElement, HRESULT(VARIANT cIndex));
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, CreateNewElement, HRESULT(BSTR bstrElementName, IAppHostElement** ppElement));
};

class MockElement : public IAppHostElement
{
public:
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, QueryInterface, HRESULT(REFIID riid, void ** ppvObject));
    MOCK_METHOD0_WITH_CALLTYPE(__stdcall, AddRef, ULONG());
    MOCK_METHOD0_WITH_CALLTYPE(__stdcall, Release, ULONG());
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Name, HRESULT(BSTR * pbstrName));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Collection, HRESULT(IAppHostElementCollection ** ppCollection));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Properties, HRESULT(IAppHostPropertyCollection ** ppProperties));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_ChildElements, HRESULT(IAppHostChildElementCollection ** ppElements));
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetMetadata, HRESULT(BSTR bstrMetadataType, VARIANT * pValue));
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, SetMetadata, HRESULT(BSTR bstrMetadataType, VARIANT value));
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Schema, HRESULT(IAppHostElementSchema** pSchema));
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetElementByName, HRESULT(BSTR bstrSubName, IAppHostElement ** ppElement));
    MOCK_METHOD2_WITH_CALLTYPE(__stdcall, GetPropertyByName, HRESULT(BSTR bstrSubName, IAppHostProperty ** ppProperty));
    MOCK_METHOD0_WITH_CALLTYPE(__stdcall, Clear, HRESULT());
    MOCK_METHOD1_WITH_CALLTYPE(__stdcall, get_Methods, HRESULT(IAppHostMethodCollection ** ppMethods));
};

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


class MockHttpApplication: public IHttpApplication
{
public:
    MOCK_CONST_METHOD0(GetApplicationPhysicalPath, PCWSTR ());
    MOCK_CONST_METHOD0(GetApplicationId, PCWSTR ());
    MOCK_CONST_METHOD0(GetAppConfigPath, PCWSTR ());
    MOCK_METHOD0(GetModuleContextContainer, IHttpModuleContextContainer* ());
};

class MockInProcessOptions : public InProcessOptions
{
public:
    static
        MockInProcessOptions*
        CreateConfig()
    {
        return new MockInProcessOptions;
    }
};

