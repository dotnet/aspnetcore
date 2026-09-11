// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include "stdafx.h"

#include "ServerErrorApplication.h"

using ::testing::_;
using ::testing::NiceMock;
using ::testing::Return;

namespace
{
class MockHttpResponse : public IHttpResponse
{
public:
    MOCK_METHOD(HTTP_RESPONSE*, GetRawHttpResponse, (), (override));
    MOCK_METHOD(const HTTP_RESPONSE*, GetRawHttpResponse, (), (const, override));
    MOCK_METHOD(IHttpCachePolicy*, GetCachePolicy, (), (override));
    MOCK_METHOD(HRESULT, SetStatus, (USHORT, PCSTR, USHORT, HRESULT, IAppHostConfigException*, BOOL), (override));
    MOCK_METHOD(HRESULT, SetHeader, (PCSTR, PCSTR, USHORT, BOOL), (override));
    MOCK_METHOD(HRESULT, SetHeader, (HTTP_HEADER_ID, PCSTR, USHORT, BOOL), (override));
    MOCK_METHOD(HRESULT, DeleteHeader, (PCSTR), (override));
    MOCK_METHOD(HRESULT, DeleteHeader, (HTTP_HEADER_ID), (override));
    MOCK_METHOD(PCSTR, GetHeader, (PCSTR, USHORT*), (const, override));
    MOCK_METHOD(PCSTR, GetHeader, (HTTP_HEADER_ID, USHORT*), (const, override));
    MOCK_METHOD(void, Clear, (), (override));
    MOCK_METHOD(void, ClearHeaders, (), (override));
    MOCK_METHOD(void, SetNeedDisconnect, (), (override));
    MOCK_METHOD(void, ResetConnection, (), (override));
    MOCK_METHOD(void, DisableKernelCache, (ULONG), (override));
    MOCK_METHOD(BOOL, GetKernelCacheEnabled, (), (const, override));
    MOCK_METHOD(void, SuppressHeaders, (), (override));
    MOCK_METHOD(BOOL, GetHeadersSuppressed, (), (const, override));
    MOCK_METHOD(HRESULT, Flush, (BOOL, BOOL, DWORD*, BOOL*), (override));
    MOCK_METHOD(HRESULT, Redirect, (PCSTR, BOOL, BOOL), (override));
    MOCK_METHOD(HRESULT, WriteEntityChunkByReference, (HTTP_DATA_CHUNK*, LONG), (override));
    MOCK_METHOD(HRESULT, WriteEntityChunks, (HTTP_DATA_CHUNK*, DWORD, BOOL, BOOL, DWORD*, BOOL*), (override));
    MOCK_METHOD(void, DisableBuffering, (), (override));
    MOCK_METHOD(void, GetStatus, (USHORT*, USHORT*, PCSTR*, USHORT*, HRESULT*, PCWSTR*, DWORD*, IAppHostConfigException**, BOOL*), (override));
    MOCK_METHOD(HRESULT, SetErrorDescription, (PCWSTR, DWORD, BOOL), (override));
    MOCK_METHOD(PCWSTR, GetErrorDescription, (DWORD*), (override));
    MOCK_METHOD(HRESULT, GetHeaderChanges, (DWORD, DWORD*, PCSTR*, DWORD*, PCSTR**, PCSTR**, DWORD*, DWORD*, DWORD**), (override));
    MOCK_METHOD(void, CloseConnection, (), (override));
};

class MockHttpContext : public IHttpContext
{
public:
    MOCK_METHOD(IHttpSite*, GetSite, (), (override));
    MOCK_METHOD(IHttpApplication*, GetApplication, (), (override));
    MOCK_METHOD(IHttpConnection*, GetConnection, (), (override));
    MOCK_METHOD(IHttpRequest*, GetRequest, (), (override));
    MOCK_METHOD(IHttpResponse*, GetResponse, (), (override));
    MOCK_METHOD(BOOL, GetResponseHeadersSent, (), (const, override));
    MOCK_METHOD(IHttpUser*, GetUser, (), (const, override));
    MOCK_METHOD(IHttpModuleContextContainer*, GetModuleContextContainer, (), (override));
    MOCK_METHOD(void, IndicateCompletion, (REQUEST_NOTIFICATION_STATUS), (override));
    MOCK_METHOD(HRESULT, PostCompletion, (DWORD), (override));
    MOCK_METHOD(void, DisableNotifications, (DWORD, DWORD), (override));
    MOCK_METHOD(BOOL, GetNextNotification, (REQUEST_NOTIFICATION_STATUS, DWORD*, BOOL*, CHttpModule**, IHttpEventProvider**), (override));
    MOCK_METHOD(BOOL, GetIsLastNotification, (REQUEST_NOTIFICATION_STATUS), (override));
    MOCK_METHOD(HRESULT, ExecuteRequest, (BOOL, IHttpContext*, DWORD, IHttpUser*, BOOL*), (override));
    MOCK_METHOD(DWORD, GetExecuteFlags, (), (const, override));
    MOCK_METHOD(HRESULT, GetServerVariable, (PCSTR, PCWSTR*, DWORD*), (override));
    MOCK_METHOD(HRESULT, GetServerVariable, (PCSTR, PCSTR*, DWORD*), (override));
    MOCK_METHOD(HRESULT, SetServerVariable, (PCSTR, PCWSTR), (override));
    MOCK_METHOD(void*, AllocateRequestMemory, (DWORD), (override));
    MOCK_METHOD(IHttpUrlInfo*, GetUrlInfo, (), (override));
    MOCK_METHOD(IMetadataInfo*, GetMetadata, (), (override));
    MOCK_METHOD(PCWSTR, GetPhysicalPath, (DWORD*), (override));
    MOCK_METHOD(PCWSTR, GetScriptName, (DWORD*), (const, override));
    MOCK_METHOD(PCWSTR, GetScriptTranslated, (DWORD*), (override));
    MOCK_METHOD(IScriptMapInfo*, GetScriptMap, (), (const, override));
    MOCK_METHOD(void, SetRequestHandled, (), (override));
    MOCK_METHOD(IHttpFileInfo*, GetFileInfo, (), (const, override));
    MOCK_METHOD(HRESULT, MapPath, (PCWSTR, PWSTR, DWORD*), (override));
    MOCK_METHOD(HRESULT, NotifyCustomNotification, (ICustomNotificationProvider*, BOOL*), (override));
    MOCK_METHOD(IHttpContext*, GetParentContext, (), (const, override));
    MOCK_METHOD(IHttpContext*, GetRootContext, (), (const, override));
    MOCK_METHOD(HRESULT, CloneContext, (DWORD, IHttpContext**), (override));
    MOCK_METHOD(HRESULT, ReleaseClonedContext, (), (override));
    MOCK_METHOD(HRESULT, GetCurrentExecutionStats, (DWORD*, DWORD*, PCWSTR*, DWORD*, DWORD*, DWORD*), (const, override));
    MOCK_METHOD(IHttpTraceContext*, GetTraceContext, (), (const, override));
    MOCK_METHOD(HRESULT, GetServerVarChanges, (DWORD, DWORD*, DWORD*, PCSTR**, PCWSTR**, DWORD*, DWORD**), (override));
    MOCK_METHOD(HRESULT, CancelIo, (), (override));
    MOCK_METHOD(HRESULT, MapHandler, (DWORD, PCWSTR, PCWSTR, PCSTR, IScriptMapInfo**, BOOL), (override));
    MOCK_METHOD(HRESULT, GetExtendedInterface, (HTTP_CONTEXT_INTERFACE_VERSION, PVOID*), (override));
};

TEST(ServerErrorHandlerTest, PreservesResponseContentAfterApplicationDestroyed)
{
    NiceMock<MockHttpResponse> response;
    NiceMock<MockHttpContext> context;
    NiceMock<MockHttpApplication> httpApplication;
    IREQUEST_HANDLER* handler = nullptr;
    std::string responseContent;
    USHORT statusCode = 0;
    USHORT subStatusCode = 0;

    ON_CALL(httpApplication, GetApplicationPhysicalPath())
        .WillByDefault(Return(L"C:\\TestApp"));
    ON_CALL(httpApplication, GetAppConfigPath())
        .WillByDefault(Return(L"MACHINE/WEBROOT/APPHOST/TestApp"));
    ON_CALL(httpApplication, GetApplicationId())
        .WillByDefault(Return(L"/TestApp"));
    EXPECT_CALL(context, GetResponse())
        .WillOnce(Return(&response));
    EXPECT_CALL(response, SetStatus(_, _, _, _, _, _))
        .WillOnce([&](USHORT status, PCSTR, USHORT subStatus, HRESULT, IAppHostConfigException*, BOOL)
        {
            statusCode = status;
            subStatusCode = subStatus;
            return S_OK;
        });
    EXPECT_CALL(response, WriteEntityChunkByReference(_, -1))
        .WillOnce([&](HTTP_DATA_CHUNK* dataChunk, LONG)
        {
            responseContent.assign(
                static_cast<const char*>(dataChunk->FromMemory.pBuffer),
                dataChunk->FromMemory.BufferLength);
            return S_OK;
        });

    const std::string ownerContent = "original page content that is not stored inline";
    auto* application = new ServerErrorApplication(
        httpApplication,
        E_FAIL,
        false,
        ownerContent,
        503,
        7,
        "Service Unavailable");

    ASSERT_EQ(S_OK, application->CreateHandler(&context, &handler));
    application->DereferenceApplication();

    EXPECT_EQ(RQ_NOTIFICATION_FINISH_REQUEST, handler->OnExecuteRequestHandler());
    EXPECT_EQ(503, statusCode);
    EXPECT_EQ(7, subStatusCode);
    EXPECT_EQ(ownerContent, responseContent);

    handler->DereferenceRequestHandler();
}
}
