// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#ifndef _HTTPSERV_H_
#define _HTTPSERV_H_

#if (!defined(_WIN64) && !defined(WIN32))
#error httpserv.h is only supported on WIN32 or WIN64 platforms
#endif

#include <ahadmin.h>

#if _WIN32_WINNT >= 0x0600
#include "http.h"
#else
#include "http_xp.h"
#endif

//
// Request deterministic notifications
//

// request is beginning
#define RQ_BEGIN_REQUEST               0x00000001
// request is being authenticated
#define RQ_AUTHENTICATE_REQUEST        0x00000002
// request is being authorized
#define RQ_AUTHORIZE_REQUEST           0x00000004
// satisfy request from cache
#define RQ_RESOLVE_REQUEST_CACHE       0x00000008
// map handler for request
#define RQ_MAP_REQUEST_HANDLER         0x00000010
// acquire request state
#define RQ_ACQUIRE_REQUEST_STATE       0x00000020
// pre-execute handler
#define RQ_PRE_EXECUTE_REQUEST_HANDLER 0x00000040 
// execute handler
#define RQ_EXECUTE_REQUEST_HANDLER     0x00000080
// release request state
#define RQ_RELEASE_REQUEST_STATE       0x00000100
// update cache
#define RQ_UPDATE_REQUEST_CACHE        0x00000200
// log request
#define RQ_LOG_REQUEST                 0x00000400
// end request
#define RQ_END_REQUEST                 0x00000800

//
// Request non-deterministic notifications
//

// custom notification
#define RQ_CUSTOM_NOTIFICATION         0x10000000
// send response
#define RQ_SEND_RESPONSE               0x20000000
// read entity
#define RQ_READ_ENTITY                 0x40000000
// map a url to a physical path
#define RQ_MAP_PATH                    0x80000000

// 
// Global notifications
//

// stop accepting new requests
#define GL_STOP_LISTENING               0x00000002
// cache cleanup before termination
#define GL_CACHE_CLEANUP                0x00000004
// cache operation
#define GL_CACHE_OPERATION              0x00000010
// health check
#define GL_HEALTH_CHECK                 0x00000020
// configuration changed
#define GL_CONFIGURATION_CHANGE         0x00000040
// file changed
#define GL_FILE_CHANGE                  0x00000080
// before request pipeline has started
#define GL_PRE_BEGIN_REQUEST            0x00000100
// application start
#define GL_APPLICATION_START            0x00000200
// resolve modules for an application
#define GL_APPLICATION_RESOLVE_MODULES  0x00000400
// application end
#define GL_APPLICATION_STOP             0x00000800
// RSCA query
#define GL_RSCA_QUERY                   0x00001000
// trace event was raised
#define GL_TRACE_EVENT                  0x00002000
// custom notification
#define GL_CUSTOM_NOTIFICATION          0x00004000
// thread cleanup notification
#define GL_THREAD_CLEANUP               0x00008000
// application preload notification
#define GL_APPLICATION_PRELOAD          0x00010000

//
// Request notification return status
//

typedef enum REQUEST_NOTIFICATION_STATUS
{
    RQ_NOTIFICATION_CONTINUE,                   // continue processing
                                                // for notification
    RQ_NOTIFICATION_PENDING,                    // suspend processing
                                                // for notification
    RQ_NOTIFICATION_FINISH_REQUEST              // finish request
                                                // processing
};

//
// Out of band return codes
//

typedef enum GLOBAL_NOTIFICATION_STATUS
{
    GL_NOTIFICATION_CONTINUE,                  // continue processing
                                               // for notification
    GL_NOTIFICATION_HANDLED                    // finish processing for
                                               // notification
};

// 
// Priority class aliases
//

#define PRIORITY_ALIAS_FIRST              L"FIRST"
#define PRIORITY_ALIAS_HIGH               L"HIGH"
#define PRIORITY_ALIAS_MEDIUM             L"MEDIUM"
#define PRIORITY_ALIAS_LOW                L"LOW"
#define PRIORITY_ALIAS_LAST               L"LAST"

//
// Cache operations
//

typedef enum CACHE_OPERATION
{
    CACHE_OPERATION_RETRIEVE,
    CACHE_OPERATION_ADD,
    CACHE_OPERATION_DELETE,
    CACHE_OPERATION_FLUSH_PREFIX,
    CACHE_OPERATION_ENUM
};

//
// Module identifier
//

typedef VOID*            HTTP_MODULE_ID;

//
// Flags for IHttpContext->CloneContext()
//

#define CLONE_FLAG_BASICS              0x01
#define CLONE_FLAG_HEADERS             0x02
#define CLONE_FLAG_ENTITY              0x04
#define CLONE_FLAG_NO_PRECONDITION     0x08
#define CLONE_FLAG_NO_DAV              0x10

//
// Flags for IHttpContext->ExecuteRequest()
//

#define EXECUTE_FLAG_NO_HEADERS                     0x01
#define EXECUTE_FLAG_IGNORE_CURRENT_INTERCEPTOR     0x02
#define EXECUTE_FLAG_IGNORE_APPPOOL                 0x04
#define EXECUTE_FLAG_DISABLE_CUSTOM_ERROR           0x08
#define EXECUTE_FLAG_SAME_URL                       0x10
// do not flush the child response but copy it back to the parent
#define EXECUTE_FLAG_BUFFER_RESPONSE                0x20
// child response is still eligible for http.sys caching
#define EXECUTE_FLAG_HTTP_CACHE_ELIGIBLE            0x40


//
// forward declarations
//
struct HTTP_TRACE_CONFIGURATION;
struct HTTP_TRACE_EVENT;

class  IWpfSettings;
class  IHttpTraceContext;

//
// Module-specific context descriptor
//
class __declspec(uuid("f1927f76-790e-4ccb-a72e-396bdfdae05d"))
IHttpStoredContext
{
 public:
    virtual
    VOID
    CleanupStoredContext(
        VOID
    ) = 0;
};

//
// Context container
//
class __declspec(uuid("d7fad7c9-aa27-4ab9-bd60-e55ccba3f5dc"))
IHttpModuleContextContainer
{
 public:
    virtual
    IHttpStoredContext *
    GetModuleContext(
        IN HTTP_MODULE_ID       moduleId
    ) = 0;

    virtual
    HRESULT
    SetModuleContext(
        IN IHttpStoredContext * ppStoredContext,
        IN HTTP_MODULE_ID       moduleId
    ) = 0;  
};

//
// Dispensed context container
//
class __declspec(uuid("2ae49359-95dd-4e48-ae20-c0cb9d0bc03a"))
IDispensedHttpModuleContextContainer : public IHttpModuleContextContainer
{
public:
    virtual
    VOID
    ReleaseContainer(
        VOID
    ) = 0;
};

//
// Performance counter descriptor
//
class __declspec(uuid("bdfc4c4a-12a4-4744-87d8-765eb320c59f"))
IHttpPerfCounterInfo
{
 public:
    virtual
    VOID
    IncrementCounter(
        DWORD               dwCounterIndex,
        DWORD               dwValue = 1
    ) = 0;

    virtual
    VOID
    DecrementCounter(
        DWORD               dwCounterIndex,
        DWORD               dwValue = 1
    ) = 0;
};

//
// Application descriptor
//
class __declspec(uuid("3f75d9e6-1075-422c-ad89-93a85f2d7bdc"))
IHttpApplication
{
 public:
    virtual
    PCWSTR
    GetApplicationPhysicalPath(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetApplicationId(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetAppConfigPath(
        VOID
    ) const = 0;

    virtual
    IHttpModuleContextContainer *
    GetModuleContextContainer(
        VOID
    ) = 0;
};

// 
// URI cache entry descriptor
// 
class __declspec(uuid("7e0e6167-0094-49a1-8287-ecf6dc6e73a6"))
IHttpUrlInfo
{
 public:
    virtual
    IHttpModuleContextContainer *
    GetModuleContextContainer(
        VOID
    ) = 0;

    virtual
    BOOL
    IsFrequentlyHit(
        VOID
    ) const = 0;
};

// 
// Script map descriptor
// 
class __declspec(uuid("d7fe3d77-68bc-4d4a-851f-eec9fb68017c"))
IScriptMapInfo
{
 public:
    virtual
    PCWSTR
    GetPath(
        VOID
    ) const = 0;

    virtual
    PCSTR
    GetAllowedVerbs(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetModules(
        OUT DWORD *         pcchModules = NULL
    ) const = 0;

    virtual
    PCWSTR
    GetScriptProcessor(
        OUT DWORD *         pcchScriptProcessor = NULL
    ) const = 0;

    virtual
    PCWSTR
    GetManagedType(
        OUT DWORD *         pcchManagedType = NULL
    ) const = 0;

    virtual
    BOOL
    GetAllowPathInfoForScriptMappings(
        VOID
    ) const = 0;

    virtual
    DWORD
    GetRequiredAccess(
        VOID
    ) const = 0;

    virtual
    DWORD
    GetResourceType(
        VOID
    ) const = 0;

    virtual
    BOOL
    GetIsStarScriptMap(
        VOID
    ) const = 0;

    virtual
    DWORD
    GetResponseBufferLimit(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetName(
        VOID
    ) const = 0;
};

class __declspec(uuid("fd86e6de-fb0e-47dd-820a-e0da12be46e9"))
IHttpTokenEntry;

// 
// Metadata descriptor
// 
class __declspec(uuid("48b10633-825d-495e-93b0-225380053e8e"))
IMetadataInfo
{
 public:
    virtual
    PCWSTR
    GetMetaPath(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetVrPath(
        VOID
    ) const = 0;

    virtual
    IHttpTokenEntry *
    GetVrToken(
        VOID
    ) = 0;

    virtual
    IHttpModuleContextContainer *
    GetModuleContextContainer(
        VOID
    ) = 0;
};

// 
// Provides an interface to an HTTP request object.  The methods on this
// class can be used to inspect and modify request data.
// 
class __declspec(uuid("e8698f7e-576e-4cac-a309-67435355faef"))
IHttpRequest
{
 public:
    virtual
    HTTP_REQUEST *
    GetRawHttpRequest(
        VOID
    ) = 0;

    virtual
    const HTTP_REQUEST *
    GetRawHttpRequest(
        VOID
    ) const = 0;

    virtual
    PCSTR
    GetHeader(
        IN PCSTR                pszHeaderName,
        OUT USHORT *            pcchHeaderValue = NULL
    ) const = 0;

    virtual
    PCSTR
    GetHeader(
        IN  HTTP_HEADER_ID      ulHeaderIndex,
        OUT USHORT *            pcchHeaderValue = NULL
    ) const = 0;

    virtual
    HRESULT
    SetHeader(
        IN PCSTR                pszHeaderName,
        IN PCSTR                pszHeaderValue,
        IN USHORT               cchHeaderValue,
        IN BOOL                 fReplace
    ) = 0;

    virtual
    HRESULT
    SetHeader(
        IN HTTP_HEADER_ID       ulHeaderIndex,
        IN PCSTR                pszHeaderValue,
        IN USHORT               cchHeaderValue,
        IN BOOL                 fReplace
    ) = 0;

    virtual
    HRESULT
    DeleteHeader(
        IN PCSTR                pszHeaderName
    ) = 0;

    virtual
    HRESULT
    DeleteHeader(
        IN HTTP_HEADER_ID       ulHeaderIndex
    ) = 0;

    virtual
    PCSTR
    GetHttpMethod(
        VOID
    ) const = 0;

    virtual
    HRESULT
    SetHttpMethod(
        IN PCSTR                pszHttpMethod
    ) = 0;

    virtual
    HRESULT
    SetUrl(
        IN PCWSTR               pszUrl,
        IN DWORD                cchUrl,
        IN BOOL                 fResetQueryString
    ) = 0;

    virtual
    HRESULT
    SetUrl(
        IN PCSTR                pszUrl,
        IN DWORD                cchUrl,
        IN BOOL                 fResetQueryString
    ) = 0;

    virtual
    BOOL
    GetUrlChanged(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetForwardedUrl(
        VOID
    ) const = 0;

    virtual
    PSOCKADDR
    GetLocalAddress(
        VOID
    ) const = 0;

    virtual
    PSOCKADDR
    GetRemoteAddress(
        VOID
    ) const = 0;

    virtual
    HRESULT
    ReadEntityBody(
        OUT VOID *              pvBuffer,
        IN  DWORD               cbBuffer,
        IN  BOOL                fAsync,
        OUT DWORD *             pcbBytesReceived,
        OUT BOOL *              pfCompletionPending = NULL
    ) = 0;

    virtual
    HRESULT
    InsertEntityBody(
        IN VOID *               pvBuffer,
        IN DWORD                cbBuffer
    ) = 0;

    virtual
    DWORD
    GetRemainingEntityBytes(
        VOID
    ) = 0;

    virtual
    VOID
    GetHttpVersion(
        OUT USHORT *            pMajorVersion,
        OUT USHORT *            pMinorVersion
    ) const = 0;

    virtual
    HRESULT
    GetClientCertificate(
        OUT HTTP_SSL_CLIENT_CERT_INFO **    ppClientCertInfo,
        OUT BOOL *                          pfClientCertNegotiated
    ) = 0;

    virtual
    HRESULT
    NegotiateClientCertificate(
        IN BOOL                 fAsync,
        OUT BOOL *              pfCompletionPending = NULL
    ) = 0;

    virtual
    DWORD
    GetSiteId(
        VOID
    ) const = 0;

    virtual
    HRESULT
    GetHeaderChanges(
        IN      DWORD   dwOldChangeNumber,
        OUT     DWORD * pdwNewChangeNumber,
        IN OUT  PCSTR   knownHeaderSnapshot[HttpHeaderRequestMaximum],
        IN OUT  DWORD * pdwUnknownHeaderSnapshot,
        IN OUT  PCSTR **ppUnknownHeaderNameSnapshot,
        IN OUT  PCSTR **ppUnknownHeaderValueSnapshot,
        __out_ecount(HttpHeaderRequestMaximum+1)
                DWORD   diffedKnownHeaderIndices[HttpHeaderRequestMaximum+1],
        OUT     DWORD * pdwDiffedUnknownHeaders,
        OUT     DWORD **ppDiffedUnknownHeaderIndices
    ) = 0;
};

class __declspec(uuid("d9244ae1-51f8-4aa1-a66d-19277c33e610"))
IHttpRequest2 : public IHttpRequest
{
 public:
    virtual
    HRESULT
    GetChannelBindingToken(
        __deref_out_bcount_part(*pTokenSize, *pTokenSize)
        PBYTE *     ppToken,
        DWORD *     pTokenSize
    ) = 0;
};

class __declspec(uuid("cb1c40ca-70f2-41a0-add2-881f5ef57388"))
IHttpCachePolicy
{
 public:
    virtual
    HTTP_CACHE_POLICY *
    GetKernelCachePolicy(
        VOID
    ) = 0;

    virtual
    VOID
    SetKernelCacheInvalidatorSet(
        VOID
    ) = 0;

    virtual
    HTTP_CACHE_POLICY *
    GetUserCachePolicy(
        VOID
    ) = 0;

    virtual
    HRESULT
    AppendVaryByHeader(
        PCSTR   pszHeader
    ) = 0;

    virtual
    PCSTR
    GetVaryByHeaders(
        VOID
    ) const = 0;

    virtual
    HRESULT
    AppendVaryByQueryString(
        PCSTR   pszParam
    ) = 0;

    virtual
    PCSTR
    GetVaryByQueryStrings(
        VOID
    ) const = 0;

    virtual
    HRESULT
    SetVaryByValue(
        PCSTR   pszValue
    ) = 0;

    virtual
    PCSTR
    GetVaryByValue(
        VOID
    ) const = 0;

    virtual
    BOOL
    IsUserCacheEnabled(
        VOID
    ) const = 0;

    virtual
    VOID
    DisableUserCache(
        VOID
    ) = 0;

    virtual
    BOOL
    IsCached(
        VOID
    ) const = 0;

    virtual
    VOID
    SetIsCached(
        VOID
    ) = 0;

    virtual
    BOOL
    GetKernelCacheInvalidatorSet(
        VOID
    ) const = 0;
};

class __declspec(uuid("9f4ba807-050e-4495-ae55-8870f7e9194a"))
IHttpCachePolicy2 : public IHttpCachePolicy
{
 public:
    virtual
    BOOL
    IsForceUpdateSet(
        VOID
    ) const = 0;

    virtual
    VOID
    SetForceUpdate(
        VOID
    ) = 0;
};

// 
// Response descriptor
// 
class __declspec(uuid("7e1c6b38-628f-4e6c-95dc-41237eb7f95e"))
IHttpResponse
{
 public:
    virtual
    HTTP_RESPONSE *
    GetRawHttpResponse(
        VOID
    ) = 0;

    virtual
    const HTTP_RESPONSE *
    GetRawHttpResponse(
        VOID
    ) const = 0;

    virtual
    IHttpCachePolicy *
    GetCachePolicy(
        VOID
    ) = 0;

    virtual
    HRESULT
    SetStatus(
        IN USHORT                   statusCode,
        IN PCSTR                    pszReason,
        IN USHORT                   uSubStatus = 0,
        IN HRESULT                  hrErrorToReport = S_OK,
        IN IAppHostConfigException *pException = NULL,
        IN BOOL                     fTrySkipCustomErrors = FALSE
    ) = 0;

    virtual
    HRESULT
    SetHeader(
        IN PCSTR                pszHeaderName,
        IN PCSTR                pszHeaderValue,
        IN USHORT               cchHeaderValue,
        IN BOOL                 fReplace
    ) = 0;

    virtual
    HRESULT
    SetHeader(
        IN HTTP_HEADER_ID       ulHeaderIndex,
        IN PCSTR                pszHeaderValue,
        IN USHORT               cchHeaderValue,
        IN BOOL                 fReplace
    ) = 0;

    virtual
    HRESULT
    DeleteHeader(
        IN PCSTR                pszHeaderName
    ) = 0;

    virtual
    HRESULT
    DeleteHeader(
        IN HTTP_HEADER_ID       ulHeaderIndex
    ) = 0;

    virtual
    PCSTR
    GetHeader(
        IN PCSTR                pszHeaderName,
        OUT USHORT *            pcchHeaderValue = NULL
    ) const = 0;

    virtual
    PCSTR
    GetHeader(
        IN  HTTP_HEADER_ID      ulHeaderIndex,
        OUT USHORT *            pcchHeaderValue = NULL
    ) const = 0;

    virtual
    VOID
    Clear(
        VOID
    ) = 0;

    virtual
    VOID
    ClearHeaders(
        VOID
    ) = 0;

    virtual
    VOID
    SetNeedDisconnect(
        VOID
    ) = 0;

    virtual
    VOID
    ResetConnection(
        VOID
    ) = 0;

    virtual
    VOID
    DisableKernelCache(
        ULONG reason = 9
    ) = 0;

    virtual
    BOOL
    GetKernelCacheEnabled(
        VOID
    ) const = 0;

    virtual
    VOID
    SuppressHeaders(
        VOID
    ) = 0;

    virtual
    BOOL
    GetHeadersSuppressed(
        VOID
    ) const = 0;

    virtual
    HRESULT
    Flush(
        IN BOOL                 fAsync,
        IN BOOL                 fMoreData,
        OUT DWORD *             pcbSent,
        OUT BOOL *              pfCompletionExpected = NULL
    ) = 0;

    virtual
    HRESULT
    Redirect(
        IN PCSTR                pszUrl,
        IN BOOL                 fResetStatusCode = TRUE,
        IN BOOL                 fIncludeParameters = FALSE
    ) = 0;

    virtual
    HRESULT
    WriteEntityChunkByReference(
        IN HTTP_DATA_CHUNK *    pDataChunk,
        IN LONG                 lInsertPosition = -1
    ) = 0;

    virtual
    HRESULT
    WriteEntityChunks(
        IN  HTTP_DATA_CHUNK *   pDataChunks,
        IN  DWORD               nChunks,
        IN  BOOL                fAsync,
        IN  BOOL                fMoreData,
        OUT DWORD *             pcbSent,
        OUT BOOL *              pfCompletionExpected = NULL
    ) = 0;

    virtual
    VOID
    DisableBuffering(
        VOID
    ) = 0;

    virtual
    VOID
    GetStatus(
        OUT USHORT *                    pStatusCode,
        OUT USHORT *                    pSubStatus = NULL,
        OUT PCSTR *                     ppszReason = NULL,
        OUT USHORT *                    pcchReason = NULL,
        OUT HRESULT *                   phrErrorToReport = NULL,
        OUT PCWSTR *                    ppszModule = NULL,
        OUT DWORD *                     pdwNotification = NULL,
        OUT IAppHostConfigException **  ppException = NULL,
        OUT BOOL *                      pfTrySkipCustomErrors = NULL
    ) = 0;

    virtual
    HRESULT
    SetErrorDescription(
        IN PCWSTR                       pszDescription,
        IN DWORD                        cchDescription,
        IN BOOL                         fHtmlEncode = TRUE
    ) = 0;

    virtual
    PCWSTR
    GetErrorDescription(
        OUT DWORD *                     pcchDescription = NULL
    ) = 0;

    virtual
    HRESULT
    GetHeaderChanges(
        IN      DWORD   dwOldChangeNumber,
        OUT     DWORD * pdwNewChangeNumber,
        IN OUT  PCSTR   knownHeaderSnapshot[HttpHeaderResponseMaximum],
        IN OUT  DWORD * pdwUnknownHeaderSnapshot,
        IN OUT  PCSTR **ppUnknownHeaderNameSnapshot,
        IN OUT  PCSTR **ppUnknownHeaderValueSnapshot,
        __out_ecount(HttpHeaderResponseMaximum+1)
                DWORD   diffedKnownHeaderIndices[HttpHeaderResponseMaximum+1],
        OUT     DWORD * pdwDiffedUnknownHeaders,
        OUT     DWORD **ppDiffedUnknownHeaderIndices
    ) = 0;

    virtual
    VOID
    CloseConnection(
        VOID
    ) = 0;
};

// 
// User descriptor
// 
class __declspec(uuid("8059e6f8-10ce-4d61-b47e-5a1d8d9a8b67"))
IHttpUser
{
 public:
    virtual
    PCWSTR
    GetRemoteUserName(
        VOID
    ) = 0;

    virtual
    PCWSTR
    GetUserName(
        VOID
    ) = 0;

    virtual 
    PCWSTR
    GetAuthenticationType(
        VOID
    ) = 0;

    virtual
    PCWSTR
    GetPassword(
        VOID
    ) = 0;  

    virtual
    HANDLE
    GetImpersonationToken(
        VOID
    ) = 0;

    virtual
    HANDLE
    GetPrimaryToken(
        VOID
    ) = 0;

    virtual
    VOID
    ReferenceUser(
        VOID
    ) = 0;

    virtual
    VOID
    DereferenceUser(
        VOID
    ) = 0;

    virtual
    BOOL
    SupportsIsInRole(
        VOID
    ) = 0;

    virtual
    HRESULT
    IsInRole(
        IN  PCWSTR  pszRoleName,
        OUT BOOL *  pfInRole
    ) = 0;

    virtual
    PVOID
    GetUserVariable(
        IN PCSTR    pszVariableName
    ) = 0;
};

#define HTTP_USER_VARIABLE_SID              "SID"
#define HTTP_USER_VARIABLE_CTXT_HANDLE      "CtxtHandle"
#define HTTP_USER_VARIABLE_CRED_HANDLE      "CredHandle"

class __declspec(uuid("841d9a71-75f4-4626-8b97-66046ca7e45b"))
IHttpConnectionStoredContext : public IHttpStoredContext
{
 public:
    virtual
    VOID
    NotifyDisconnect(
        VOID
    ) = 0;
};

class __declspec(uuid("f3dd2fb3-4d11-4295-b8ab-4cb667add1fe"))
IHttpConnectionModuleContextContainer : public IHttpModuleContextContainer
{
 public:
    virtual
    IHttpConnectionStoredContext *
    GetConnectionModuleContext(
        IN HTTP_MODULE_ID       moduleId
    ) = 0;

    virtual
    HRESULT
    SetConnectionModuleContext(
        IN IHttpConnectionStoredContext *   ppStoredContext,
        IN HTTP_MODULE_ID                   moduleId
    ) = 0;  
};

// 
// Connection descriptor
// 
class __declspec(uuid("d9a5de00-3346-4599-9826-fe88565e1226"))
IHttpConnection
{
 public:
    virtual
    BOOL
    IsConnected(
        VOID
    ) const = 0;

    virtual
    VOID *
    AllocateMemory(
        DWORD               cbAllocation
    ) = 0;

    virtual
    IHttpConnectionModuleContextContainer *
    GetModuleContextContainer(
        VOID
    ) = 0;
};

// 
// Forward declarations
// 
class __declspec(uuid("71e95595-8c74-44d9-88a9-f5112d5f5900"))
IHttpFileInfo;

class __declspec(uuid("eb16a6ec-ba5d-436f-bf24-3ede13906450"))
IHttpSite;

class __declspec(uuid("671e6d34-9380-4df4-b453-91129df02b24"))
ICustomNotificationProvider;

class __declspec(uuid("6f3f657d-2fb8-43c6-a096-5064b41f0580"))
IHttpEventProvider;

class CHttpModule;

//
// IHttpContext extended interface versions (deprecated)
//
enum HTTP_CONTEXT_INTERFACE_VERSION
{
};

// 
// Context object representing the processing of an HTTP request
// 
class __declspec(uuid("424c1b8c-a1ba-44d7-ac98-9f8f457701a5"))
IHttpContext
{
 public:
    virtual
    IHttpSite *
    GetSite(
        VOID
    ) = 0;

    virtual
    IHttpApplication *
    GetApplication(
        VOID
    ) = 0;

    virtual
    IHttpConnection *
    GetConnection(
        VOID
    ) = 0;

    virtual
    IHttpRequest *
    GetRequest(
        VOID
    ) = 0;

    virtual
    IHttpResponse *
    GetResponse(
        VOID
    ) = 0;

    virtual
    BOOL
    GetResponseHeadersSent(
        VOID
    ) const = 0;

    virtual
    IHttpUser *
    GetUser(
        VOID
    ) const = 0;

    virtual
    IHttpModuleContextContainer *
    GetModuleContextContainer(
        VOID
    ) = 0;

    virtual
    VOID
    IndicateCompletion(
        IN REQUEST_NOTIFICATION_STATUS     notificationStatus
    ) = 0;

    virtual
    HRESULT
    PostCompletion(
        IN DWORD                cbBytes
    ) = 0;

    virtual
    VOID
    DisableNotifications(
        IN DWORD                dwNotifications,
        IN DWORD                dwPostNotifications
    ) = 0;

    virtual
    BOOL
    GetNextNotification(
        IN  REQUEST_NOTIFICATION_STATUS status,
        OUT DWORD *                     pdwNotification,
        OUT BOOL *                      pfIsPostNotification,
        OUT CHttpModule **              ppModuleInfo,
        OUT IHttpEventProvider **       ppRequestOutput
    ) = 0;

    virtual
    BOOL
    GetIsLastNotification(
        IN  REQUEST_NOTIFICATION_STATUS status
    ) = 0;    

    virtual
    HRESULT
    ExecuteRequest(
        IN BOOL                 fAsync,
        IN IHttpContext *       pHttpContext,
        IN DWORD                dwExecuteFlags,
        IN IHttpUser *          pHttpUser,
        OUT BOOL *              pfCompletionExpected = NULL
    ) = 0;                      

    virtual
    DWORD
    GetExecuteFlags(
        VOID
    ) const = 0;

    virtual
    HRESULT
    GetServerVariable(
        PCSTR               pszVariableName,
        __deref_out_ecount(*pcchValueLength) PCWSTR * ppszValue,
        __out DWORD *       pcchValueLength
    ) = 0;

    virtual
    HRESULT
    GetServerVariable(
        PCSTR               pszVariableName,
        __deref_out_ecount(*pcchValueLength) PCSTR * ppszValue,
        __out DWORD *       pcchValueLength
    ) = 0;

    virtual
    HRESULT
    SetServerVariable(
        PCSTR               pszVariableName,
        PCWSTR              pszVariableValue
    ) = 0;

    virtual
    VOID *
    AllocateRequestMemory(
        IN DWORD                cbAllocation
    ) = 0;

    virtual
    IHttpUrlInfo *
    GetUrlInfo(
        VOID
    ) = 0;

    virtual
    IMetadataInfo *
    GetMetadata(
        VOID
    ) = 0;

    virtual
    PCWSTR
    GetPhysicalPath(
        OUT DWORD *         pcchPhysicalPath = NULL
    ) = 0;

    virtual
    PCWSTR
    GetScriptName(
        OUT DWORD *         pcchScriptName = NULL
    ) const = 0;

    virtual
    PCWSTR
    GetScriptTranslated(
        OUT DWORD *         pcchScriptTranslated = NULL
    ) = 0;

    virtual
    IScriptMapInfo *
    GetScriptMap(
        VOID
    ) const = 0;

    virtual
    VOID
    SetRequestHandled(
        VOID
    ) = 0;

    virtual
    IHttpFileInfo *
    GetFileInfo(
        VOID
    ) const = 0;

    virtual
    HRESULT
    MapPath(
                                           PCWSTR   pszUrl,
        __out_bcount_opt(*pcbPhysicalPath) PWSTR    pszPhysicalPath,
                                    IN OUT DWORD *  pcbPhysicalPath
    ) = 0;

    virtual
    HRESULT
    NotifyCustomNotification(
        ICustomNotificationProvider *   pCustomOutput,
        OUT BOOL *                      pfCompletionExpected
    ) = 0;

    virtual
    IHttpContext *
    GetParentContext(
        VOID
    ) const = 0;

    virtual
    IHttpContext *
    GetRootContext(
        VOID
    ) const = 0;

    virtual
    HRESULT
    CloneContext(
        IN DWORD                dwCloneFlags,
        OUT IHttpContext **     ppHttpContext
    ) = 0;

    virtual
    HRESULT
    ReleaseClonedContext(
        VOID
    ) = 0;

    virtual
    HRESULT
    GetCurrentExecutionStats(
        OUT DWORD * pdwNotification,
        OUT DWORD * pdwNotificationStartTickCount = NULL,
        OUT PCWSTR *  ppszModule = NULL,
        OUT DWORD * pdwModuleStartTickCount = NULL,
        OUT DWORD * pdwAsyncNotification = NULL,
        OUT DWORD * pdwAsyncNotificationStartTickCount = NULL
    ) const = 0;

    virtual
    IHttpTraceContext *
    GetTraceContext(
        VOID
    ) const = 0;

    virtual
    HRESULT
    GetServerVarChanges(
        IN      DWORD       dwOldChangeNumber,
        OUT     DWORD *     pdwNewChangeNumber,
        IN OUT  DWORD *     pdwVariableSnapshot,
        IN OUT  PCSTR **    ppVariableNameSnapshot,
        IN OUT  PCWSTR **   ppVariableValueSnapshot,
        OUT     DWORD *     pdwDiffedVariables,
        OUT     DWORD **    ppDiffedVariableIndices
    ) = 0;

    virtual
    HRESULT
    CancelIo(
        VOID
    ) = 0;

    virtual
    HRESULT
    MapHandler(
        IN      DWORD               dwSiteId,
        IN      PCWSTR              pszSiteName,
        IN      PCWSTR              pszUrl,
        IN      PCSTR               pszVerb,
        OUT     IScriptMapInfo **   ppScriptMap,
        IN      BOOL                fIgnoreWildcardMappings = FALSE
    ) = 0;

    __declspec(deprecated("This method is deprecated. Use the HttpGetExtendedInterface helper function instead."))
    virtual
    HRESULT
    GetExtendedInterface(
        IN  HTTP_CONTEXT_INTERFACE_VERSION  version,
        OUT PVOID *                         ppInterface
    ) = 0;
};

class __declspec(uuid("9f9098d5-915c-4294-a52e-66532a232bc9"))
IHttpTraceContext
{
public:
    virtual
    HRESULT
    GetTraceConfiguration(
        IN OUT HTTP_TRACE_CONFIGURATION *  pHttpTraceConfiguration
    ) = 0;
    
    virtual    
    HRESULT
    SetTraceConfiguration(
        IN HTTP_MODULE_ID              moduleId,
        IN HTTP_TRACE_CONFIGURATION *  pHttpTraceConfiguration,
        IN DWORD                       cHttpTraceConfiguration = 1
    ) = 0;

    virtual
    HRESULT
    RaiseTraceEvent(
        IN HTTP_TRACE_EVENT * pTraceEvent 
    ) = 0;

    virtual
    LPCGUID
    GetTraceActivityId(
    ) = 0;

    virtual
    HRESULT
    QuickTrace(
        IN PCWSTR   pszData1,
        IN PCWSTR   pszData2 = NULL,
        IN HRESULT  hrLastError = S_OK,
        //
        // 4 == TRACE_LEVEL_INFORMATION
        //
        IN UCHAR    Level = 4
    ) = 0;
};

class __declspec(uuid("37776aff-852e-4eec-93a5-b85a285a95b8"))
IHttpCacheSpecificData;

//
// Cache helpers
//
class __declspec(uuid("cdef2aad-20b3-4512-b1b1-094b3844aeb2"))
IHttpCacheKey
{
 public:
    virtual
    DWORD
    GetHash(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetCacheName(
        VOID
    ) const = 0;

    virtual
    bool
    GetIsEqual(
        IHttpCacheKey *         pCacheCompareKey
    ) const = 0;

    virtual
    bool
    GetIsPrefix(
        IHttpCacheKey *         pCacheCompareKey
    ) const = 0;

    virtual
    VOID
    Enum(
        IHttpCacheSpecificData *
    ) = 0;
};

class __declspec(uuid("37776aff-852e-4eec-93a5-b85a285a95b8"))
IHttpCacheSpecificData
{
 public:
    virtual
    IHttpCacheKey *
    GetCacheKey(
        VOID
    ) const = 0;

    virtual
    VOID
    ReferenceCacheData(
        VOID
    ) = 0;

    virtual
    VOID
    DereferenceCacheData(
        VOID
    ) = 0;

    virtual
    VOID
    ResetTTL(
        VOID
    ) = 0;

    virtual
    VOID
    DecrementTTL(
        OUT BOOL    *pfTTLExpired
    ) = 0;

    virtual
    VOID
    SetFlushed(
        VOID
    ) = 0;

    virtual
    BOOL
    GetFlushed(
        VOID
    ) const = 0;
};

// 
// Site descriptor
// 
class __declspec(uuid("eb16a6ec-ba5d-436f-bf24-3ede13906450"))
IHttpSite
{
 public:
    virtual
    DWORD
    GetSiteId(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetSiteName(
        VOID
    ) const = 0;

    virtual
    IHttpModuleContextContainer *
    GetModuleContextContainer(
        VOID
    ) = 0;

    virtual
    IHttpPerfCounterInfo *
    GetPerfCounterInfo(
        VOID
    ) = 0;
};

//
// File change monitor
//
//
class __declspec(uuid("985422da-b0cf-473b-ba9e-8148ceb3e240"))
IHttpFileMonitor
{
 public:
    virtual
    IHttpModuleContextContainer *
    GetModuleContextContainer(
        VOID
    ) = 0;

    virtual
    VOID
    DereferenceFileMonitor(
        VOID
    ) = 0;
};

//
// File descriptor
// 
// 
class __declspec(uuid("71e95595-8c74-44d9-88a9-f5112d5f5900"))
IHttpFileInfo : public IHttpCacheSpecificData
{
 public:
    virtual
    DWORD
    GetAttributes(
        VOID
    ) const = 0;

    virtual
    VOID
    GetSize(
        OUT ULARGE_INTEGER *        pliSize
    ) const = 0;

    virtual
    const BYTE *
    GetFileBuffer(
        VOID
    ) const = 0;

    virtual
    HANDLE
    GetFileHandle(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetFilePath(
        VOID
    ) const = 0;

    virtual
    PCSTR
    GetETag(
        OUT USHORT *                pcchETag = NULL
    ) const = 0;

    virtual
    VOID
    GetLastModifiedTime(
        OUT FILETIME *              pFileTime
    ) const = 0;

    virtual
    PCSTR
    GetLastModifiedString(
        VOID
    ) const = 0;

    virtual
    BOOL
    GetHttpCacheAllowed(
        OUT DWORD *     pSecondsToLive
    ) const = 0;

    virtual
    HRESULT
    AccessCheck(
        IN HANDLE                   hUserToken,
        IN PSID                     pUserSid
    ) = 0;

    virtual
    HANDLE
    GetVrToken(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetVrPath(
        VOID
    ) const = 0;

    virtual
    IHttpModuleContextContainer *
    GetModuleContextContainer(
        VOID
    ) = 0;

    virtual
    BOOL
    CheckIfFileHasChanged(
        IN HANDLE                   hUserToken
    ) = 0;
};


// 
// Token-cache entry
// 
class __declspec(uuid("fd86e6de-fb0e-47dd-820a-e0da12be46e9"))
IHttpTokenEntry : public IHttpCacheSpecificData
{
 public:
    virtual
    HANDLE
    GetImpersonationToken(
        VOID
    ) = 0;

    virtual
    HANDLE
    GetPrimaryToken(
        VOID
    ) = 0;

    virtual
    PSID
    GetSid(
        VOID
    ) = 0;
};


//
// IHttpServer extended interface versions
//
enum HTTP_SERVER_INTERFACE_VERSION
{
    HTTP_SERVER_INTERFACE_V2
};


//
// Global utility descriptor
//
class __declspec(uuid("eda2a40f-fb92-4d6d-b52b-c8c207380b4e"))
IHttpServer
{
 public:
    virtual
    BOOL
    IsCommandLineLaunch(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetAppPoolName(
        VOID
    ) const = 0;

    virtual
    HRESULT
    AssociateWithThreadPool(
        IN HANDLE                              hHandle,
        IN LPOVERLAPPED_COMPLETION_ROUTINE     completionRoutine
    ) = 0;

    virtual
    VOID
    IncrementThreadCount(
        VOID
    ) = 0;

    virtual
    VOID
    DecrementThreadCount(
        VOID
    ) = 0;

    virtual
    VOID
    ReportUnhealthy(
        IN PCWSTR               pszReasonString,
        IN HRESULT              hrReason
    ) = 0;

    virtual
    VOID
    RecycleProcess(
        PCWSTR                  pszReason
    ) = 0;

    virtual
    IAppHostAdminManager *
    GetAdminManager(
        VOID
    ) const = 0;

    virtual
    HRESULT
    GetFileInfo(
        IN  PCWSTR               pszPhysicalPath,
        IN  HANDLE               hUserToken,
        IN  PSID                 pSid,
        IN  PCWSTR               pszChangeNotificationPath,
        IN  HANDLE               hChangeNotificationToken,
        IN  BOOL                 fCache,
        OUT IHttpFileInfo **     ppFileInfo,
        IN  IHttpTraceContext *  pHttpTraceContext = NULL
    ) = 0;

    virtual
    HRESULT
    FlushKernelCache(
        IN PCWSTR               pszUrl
    ) = 0;

    virtual
    HRESULT
    DoCacheOperation(
        IN CACHE_OPERATION              cacheOperation,
        IN IHttpCacheKey *              pCacheKey,
        OUT IHttpCacheSpecificData **   ppCacheSpecificData,
        IN  IHttpTraceContext *         pHttpTraceContext = NULL
    ) = 0;

    virtual
    GLOBAL_NOTIFICATION_STATUS
    NotifyCustomNotification(
        ICustomNotificationProvider * pCustomOutput
    ) = 0;

    virtual
    IHttpPerfCounterInfo *
    GetPerfCounterInfo(
        VOID
    ) = 0;

    virtual
    VOID
    RecycleApplication(
        PCWSTR                  pszAppConfigPath
    ) = 0;

    virtual
    VOID
    NotifyConfigurationChange(
        PCWSTR                  pszPath
    ) = 0;

    virtual
    VOID
    NotifyFileChange(
        PCWSTR                  pszFileName
    ) = 0;

    virtual
    IDispensedHttpModuleContextContainer *
    DispenseContainer(
        VOID
    ) = 0;

    virtual
    HRESULT
    AddFragmentToCache(
        IN HTTP_DATA_CHUNK *    pDataChunk,
        PCWSTR                  pszFragmentName
    ) = 0;

    virtual
    HRESULT
    ReadFragmentFromCache(
        PCWSTR          pszFragmentName,
        OUT BYTE *      pvBuffer,
        DWORD           cbSize,
        OUT DWORD *     pcbCopied
    ) = 0;

    virtual
    HRESULT
    RemoveFragmentFromCache(
        PCWSTR          pszFragmentName
    ) = 0;

    virtual
    HRESULT
    GetWorkerProcessSettings(
        OUT IWpfSettings ** ppWorkerProcessSettings
    ) = 0;

    virtual
    HRESULT
    GetProtocolManagerCustomInterface(
        IN PCWSTR       pProtocolManagerDll,
        IN PCWSTR       pProtocolManagerDllInitFunction,
        IN DWORD        dwCustomInterfaceId,
        OUT PVOID*      ppCustomInterface
    ) = 0;

    virtual
    BOOL
    SatisfiesPrecondition(
        PCWSTR          pszPrecondition,
        BOOL *          pfUnknownPrecondition = NULL
    ) const = 0;

    virtual
    IHttpTraceContext *
    GetTraceContext(
        VOID
    ) const = 0;

    virtual
    HRESULT
    RegisterFileChangeMonitor(
        PCWSTR                  pszPath,
        HANDLE                  hToken,
        IHttpFileMonitor **     ppFileMonitor
    ) = 0;

    virtual
    HRESULT
    GetExtendedInterface(
        IN  HTTP_SERVER_INTERFACE_VERSION   version,
        OUT PVOID *                         ppInterface
    ) = 0;
};

class __declspec(uuid("34af637e-afe8-4556-bcc1-767f8e0b4a4e"))
IHttpServer2 : public IHttpServer
{
 public:

    virtual
    HRESULT
    GetToken(
        PCWSTR              pszUserName,
        PCWSTR              pszPassword,
        DWORD               dwLogonMethod,
        IHttpTokenEntry **  ppTokenEntry,
        PCWSTR              pszDefaultDomain = NULL,
        PSOCKADDR           pSockAddr = NULL,
        IHttpTraceContext * pHttpTraceContext = NULL
    ) = 0;

    virtual
    PCWSTR
    GetAppPoolConfigFile(
        __out DWORD * pcchConfigFilePath = NULL
    ) const = 0;

    virtual
    HRESULT
    GetExtendedInterface(
        __in const GUID &       Version1,
        __in PVOID              pInput,
        __in const GUID &       Version2,
        __deref_out PVOID *     ppOutput
    ) = 0;
};

//
// Helper function to get extended HTTP interfaces.
//
// Template parameters (HttpType1 and HttpType2)
// can be deduced from the arguments to the function.
//
// Example:
//
//   IHttpRequest * pHttpRequest = pHttpContext->GetRequest();
//   IHttpRequest2 * pHttpRequest2;
//   HRESULT hr = HttpGetExtendedInterface(g_pHttpServer, pHttpRequest, &pHttpRequest2);
//   if( SUCCEEDED(hr) )
//   {
//      // Use pHttpRequest2.
//   }
//
// Where pHttpContext is an IHttpContext pointer and
// g_pHttpServer is an IHttpServer pointer.
//

template <class HttpType1, class HttpType2>
HRESULT
HttpGetExtendedInterface(
    __in IHttpServer *          pHttpServer,
    __in HttpType1 *            pInput,
    __deref_out HttpType2 **    ppOutput
)
{
    HRESULT         hr;
    IHttpServer2 *  pHttpServer2;
    hr = pHttpServer->GetExtendedInterface(HTTP_SERVER_INTERFACE_V2,
                                           reinterpret_cast<void**>(&pHttpServer2) );
    if (SUCCEEDED(hr))
    {
        hr = pHttpServer2->GetExtendedInterface(__uuidof(HttpType1),
                                                pInput,
                                                __uuidof(HttpType2),
                                                reinterpret_cast<void**>(ppOutput) );
    }
    return hr;
}

//
// Notification specific output for notifications
//
class __declspec(uuid("6f3f657d-2fb8-43c6-a096-5064b41f0580"))
IHttpEventProvider
{
 public:
    virtual
    VOID
    SetErrorStatus(
        HRESULT             hrError
    ) = 0;
};

//
// Completion information for notifications
//
class __declspec(uuid("49dd20e3-d9c0-463c-8821-f3413b55cc00"))
IHttpCompletionInfo
{
 public:
    virtual
    DWORD
    GetCompletionBytes(
        VOID
    ) const = 0;

    virtual
    HRESULT
    GetCompletionStatus(
        VOID
    ) const = 0;
};

//
// RQ_ and GL_ CUSTOM_NOTIFICATION outputs
//
class __declspec(uuid("671e6d34-9380-4df4-b453-91129df02b24"))
ICustomNotificationProvider : public IHttpEventProvider
{
 public:
    virtual
    PCWSTR
    QueryNotificationType(
        VOID
    ) = 0;
};

//
// RQ_REQUEST_AUTHENTICATE descriptor
//
class __declspec(uuid("304d51d0-0307-45ed-83fd-dd3fc032fdfc"))
IAuthenticationProvider : public IHttpEventProvider
{
 public:
    virtual
    VOID
    SetUser(
        IN IHttpUser *          pUser
    ) = 0;
};

//
// RQ_MAP_REQUEST_HANDLER
//
class __declspec(uuid("fea3ce6b-e346-47e7-b2a6-ad265baeff2c"))
IMapHandlerProvider : public IHttpEventProvider
{
 public:
    virtual
    HRESULT
    SetScriptName(
        PCWSTR                  pszScriptName,
        DWORD                   cchScriptName
    ) = 0;

    virtual
    VOID
    SetScriptMap(
        IN IScriptMapInfo *     pScriptMap
    ) = 0;

    virtual
    VOID
    SetFileInfo(
        IN IHttpFileInfo *      pFileInfo
    ) = 0;
};

//
// RQ_MAP_PATH
//
class __declspec(uuid("8efdf557-a8f1-4bc9-b462-6df3b038a59a"))
IMapPathProvider : public IHttpEventProvider
{
 public:
    virtual
    PCWSTR
    GetUrl(
    ) const = 0;

    virtual
    PCWSTR
    GetPhysicalPath(
    ) const = 0;

    virtual
    HRESULT
    SetPhysicalPath(
        PCWSTR pszPhysicalPath,
        DWORD  cchPhysicalPath
    ) = 0;
};

//
// RQ_SEND_RESPONSE
//
class __declspec(uuid("57f2e7bc-0bcf-4a9f-94a4-10e55c6e5b51"))
ISendResponseProvider : public IHttpEventProvider
{
 public:
    virtual
    BOOL
    GetHeadersBeingSent(
        VOID
    ) const = 0;

    virtual
    DWORD
    GetFlags(
        VOID
    ) const = 0;

    virtual
    VOID
    SetFlags(
        DWORD dwFlags
    ) = 0;

    virtual
    HTTP_LOG_DATA *
    GetLogData(
        VOID
    ) const = 0;

    virtual
    HRESULT
    SetLogData(
        IN HTTP_LOG_DATA *pLogData
    ) = 0;

    virtual
    BOOL
    GetReadyToLogData(
        VOID
    ) const = 0;
};

//
// RQ_READ_ENTITY
//
class __declspec(uuid("fe6d905a-99b8-49fd-b389-cfc809562b81"))
IReadEntityProvider : public IHttpEventProvider
{
 public:
    virtual
    VOID
    GetEntity(
        OUT PVOID *             ppBuffer,
        OUT DWORD *             pcbData,
        OUT DWORD *             pcbBuffer
    ) = 0;

    virtual
    VOID
    SetEntity(
        IN PVOID            pBuffer,
        DWORD               cbData,
        DWORD               cbBuffer
    ) = 0;
};

//
// GL_PRE_BEGIN_REQUEST provider
//
class __declspec(uuid("fb715d26-aff9-476a-8fc0-6b1acb3d1098"))
IPreBeginRequestProvider : public IHttpEventProvider
{
 public:
    virtual
    IHttpContext *
    GetHttpContext(
        VOID
    ) = 0;
};

//
// GL_APPLICATION_START provider
//
class __declspec(uuid("1de2c71c-c126-4512-aed3-f4f885e14997"))
IHttpApplicationProvider : public IHttpEventProvider
{
 public:
    virtual
    IHttpApplication *
    GetApplication(
        VOID
    ) = 0;
};

typedef IHttpApplicationProvider    IHttpApplicationStartProvider;

class __declspec(uuid("ba32d330-9ea8-4b9e-89f1-8c76a323277f"))
IHttpModuleFactory;

//
// GL_APPLICATION_RESOLVE_MODULES provider
//
class __declspec(uuid("0617d9b9-e20f-4a9f-94f9-35403b3be01e"))
IHttpApplicationResolveModulesProvider : public IHttpApplicationProvider
{
 public:
    virtual 
    HRESULT
    RegisterModule(
        IN HTTP_MODULE_ID       parentModuleId,
        IN IHttpModuleFactory * pModuleFactory,
        IN PCWSTR               pszModuleName,
        IN PCWSTR               pszModuleType,
        IN PCWSTR               pszModulePreCondition,
        IN DWORD                dwRequestNotifications,
        IN DWORD                dwPostRequestNotifications
    ) = 0;

    virtual
    HRESULT
    SetPriorityForRequestNotification(
        IN PCWSTR               pszModuleName,
        IN DWORD                dwRequestNotification,
        IN PCWSTR               pszPriorityAlias
    ) = 0;
};

//
// GL_APPLICATION_STOP provider
//
typedef IHttpApplicationProvider   IHttpApplicationStopProvider;

//
// GL_RSCA_QUERY provider
//
class __declspec(uuid("63fdc43f-934a-4ee5-bcd8-7e7b50b75605"))
IGlobalRSCAQueryProvider : public IHttpEventProvider
{
 public:
    virtual
    PCWSTR
    GetFunctionName(
        VOID
    ) const = 0;

    virtual
    PCWSTR
    GetFunctionParameters(
        VOID
    ) const = 0;

    virtual
    HRESULT
    GetOutputBuffer(
        DWORD       cbBuffer,
        OUT BYTE ** ppbBuffer
    ) = 0;

    virtual
    HRESULT
    ResizeOutputBuffer(
        DWORD          cbNewBuffer,
        DWORD          cbBytesToCopy,
        IN OUT BYTE ** ppbBuffer
    ) = 0;

    virtual
    VOID
    SetResult(
        DWORD       cbData,
        HRESULT     hr
    ) = 0;
};

//
// GL_STOP_LISTENING
//
class __declspec(uuid("41f9a601-e25d-4ac8-8a1f-635698a30ab9"))
IGlobalStopListeningProvider : public IHttpEventProvider
{
 public:
    virtual
    BOOL
    DrainRequestsGracefully(
        VOID
    ) const = 0;
};

//
// GL_CACHE_OPERATION
//
class __declspec(uuid("58925fb9-7c5e-4684-833b-4a04e1286690"))
ICacheProvider : public IHttpEventProvider
{
 public:
    virtual
    CACHE_OPERATION
    GetCacheOperation(
        VOID
    ) const = 0;

    virtual
    IHttpCacheKey *
    GetCacheKey(
        VOID
    ) const = 0;

    virtual
    IHttpCacheSpecificData *
    GetCacheRecord(
        VOID
    ) const = 0;

    virtual
    VOID
    SetCacheRecord(
        IHttpCacheSpecificData *    pCacheRecord
    ) = 0;

    virtual
    IHttpTraceContext *
    GetTraceContext(
        VOID
    ) const = 0;
};

//
// GL_CONFIGURATION_CHANGE
//
class __declspec(uuid("3405f3b4-b3d6-4b73-b5f5-4d8a3cc642ce"))
IGlobalConfigurationChangeProvider : public IHttpEventProvider
{
 public:
    virtual
    PCWSTR
    GetChangePath(
        VOID
    ) const = 0;
};

//
// GL_FILE_CHANGE
//
class __declspec(uuid("ece31ee5-0486-4fb0-a875-6739a2d7daf5"))
IGlobalFileChangeProvider : public IHttpEventProvider
{
public:
    virtual
    PCWSTR
    GetFileName(
        VOID
    ) const = 0;

    virtual
    IHttpFileMonitor *
    GetFileMonitor(
        VOID
    ) = 0;
};

//
// GL_TRACE_EVENT
//
class __declspec(uuid("7c6bb150-0310-4718-a01f-6faceb62dc1d"))
IGlobalTraceEventProvider : public IHttpEventProvider
{
 public:
    virtual
    HRESULT
    GetTraceEvent(
        OUT HTTP_TRACE_EVENT ** ppTraceEvent
    ) = 0;

    virtual
    BOOL
    CheckSubscription(
        IN HTTP_MODULE_ID   ModuleId    
    ) = 0;     

    virtual
    HRESULT 
    GetCurrentHttpRequestContext(
        OUT IHttpContext ** ppHttpContext
    ) = 0;
};

//
// GL_THREAD_CLEANUP
//
class __declspec(uuid("6b36a149-8620-45a0-8197-00814a706e2e"))
IGlobalThreadCleanupProvider : public IHttpEventProvider
{
public:
    virtual
    IHttpApplication *
    GetApplication(
        VOID
    ) = 0;
};

//
// GL_APPLICATION_PRELOAD
//
class __declspec(uuid("2111f8d6-0c41-4ff7-bd45-5c04c7e91a73"))
IGlobalApplicationPreloadProvider : public IHttpEventProvider
{
public:
    virtual
    HRESULT
    CreateContext(
        OUT IHttpContext **     ppHttpContext
    ) = 0;

    virtual
    HRESULT
    ExecuteRequest(
        IN IHttpContext *       pHttpContext,
        IN IHttpUser *          pHttpUser
    ) = 0;
};

class CHttpModule
{
public:
    // RQ_BEGIN_REQUEST

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnBeginRequest(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostBeginRequest(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    // RQ_AUTHENTICATE_REQUEST

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnAuthenticateRequest(
        IN IHttpContext *                       pHttpContext,
        IN IAuthenticationProvider *            pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostAuthenticateRequest(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    // RQ_AUTHORIZE_REQUEST

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnAuthorizeRequest(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostAuthorizeRequest(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    // RQ_RESOLVE_REQUEST_CACHE

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnResolveRequestCache(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostResolveRequestCache(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    // RQ_MAP_REQUEST_HANDLER

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnMapRequestHandler(
        IN IHttpContext *                       pHttpContext,
        IN IMapHandlerProvider *                pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostMapRequestHandler(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    // RQ_ACQUIRE_REQUEST_STATE

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnAcquireRequestState(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostAcquireRequestState(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    // RQ_PRE_EXECUTE_REQUEST_HANDLER

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPreExecuteRequestHandler(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostPreExecuteRequestHandler(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    // RQ_EXECUTE_REQUEST_HANDLER

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnExecuteRequestHandler(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostExecuteRequestHandler(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )    
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    // RQ_RELEASE_REQUEST_STATE

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnReleaseRequestState(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostReleaseRequestState(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    // RQ_UPDATE_REQUEST_CACHE

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnUpdateRequestCache(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }


    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostUpdateRequestCache(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    // RQ_LOG_REQUEST

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnLogRequest(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostLogRequest(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return RQ_NOTIFICATION_CONTINUE;
    }

    // RQ_END_REQUEST

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnEndRequest(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return RQ_NOTIFICATION_CONTINUE;
    }

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnPostEndRequest(
        IN IHttpContext *                       pHttpContext,
        IN IHttpEventProvider *                 pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    // RQ_SEND_RESPONSE

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnSendResponse(
        IN IHttpContext *                       pHttpContext,
        IN ISendResponseProvider *              pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    // RQ_MAP_PATH

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnMapPath(
        IN IHttpContext *                       pHttpContext,
        IN IMapPathProvider *                   pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    // RQ_READ_ENTITY

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnReadEntity(
        IN IHttpContext *                       pHttpContext,
        IN IReadEntityProvider *                pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    // RQ_CUSTOM_NOTIFICATION

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnCustomRequestNotification(
        IN IHttpContext *                       pHttpContext,
        IN ICustomNotificationProvider *        pProvider
    )
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    // Completion

    virtual 
    REQUEST_NOTIFICATION_STATUS
    OnAsyncCompletion(
        IN IHttpContext *                       pHttpContext,
        IN DWORD                                dwNotification,
        IN BOOL                                 fPostNotification,
        IN IHttpEventProvider *                 pProvider,
        IN IHttpCompletionInfo *                pCompletionInfo        
    )    
    {
        UNREFERENCED_PARAMETER( pHttpContext );
        UNREFERENCED_PARAMETER( dwNotification );
        UNREFERENCED_PARAMETER( fPostNotification );
        UNREFERENCED_PARAMETER( pProvider );
        UNREFERENCED_PARAMETER( pCompletionInfo );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CHttpModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();
        
        return RQ_NOTIFICATION_CONTINUE;
    }

    virtual
    VOID
    Dispose(
        VOID
    )
    {
        delete this;
    }

 protected:

    CHttpModule()
    {}

    virtual
    ~CHttpModule()
    {}
};

class CGlobalModule
{
 public:

    // GL_STOP_LISTENING

    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalStopListening(
        IN IGlobalStopListeningProvider  *  pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_CACHE_CLEANUP
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalCacheCleanup(
        VOID
    )
    {
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_CACHE_OPERATION
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalCacheOperation(
        IN ICacheProvider  *  pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_HEALTH_CHECK
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalHealthCheck(
        VOID
    )
    {
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_CONFIGURATION_CHANGE
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalConfigurationChange(
        IN IGlobalConfigurationChangeProvider  *  pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_FILE_CHANGE 
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalFileChange(
        IN IGlobalFileChangeProvider *  pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_PRE_BEGIN_REQUEST 
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalPreBeginRequest(
        IN IPreBeginRequestProvider  *  pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_APPLICATION_START 
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalApplicationStart(
        IN IHttpApplicationStartProvider  *  pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_APPLICATION_RESOLVE_MODULES
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalApplicationResolveModules(
        IN IHttpApplicationResolveModulesProvider  *  pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_APPLICATION_STOP

    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalApplicationStop(
        IN IHttpApplicationStopProvider *   pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_RSCA_QUERY
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalRSCAQuery(
        IN IGlobalRSCAQueryProvider  *  pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_TRACE_EVENT
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalTraceEvent(
        IN IGlobalTraceEventProvider  *  pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_CUSTOM_NOTIFICATION
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalCustomNotification(
        IN ICustomNotificationProvider *    pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    virtual
    VOID
    Terminate(
        VOID
    ) = 0;

    // GL_THREAD_CLEANUP
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalThreadCleanup(
        IN IGlobalThreadCleanupProvider *    pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

    // GL_APPLICATION_PRELOAD
    
    virtual 
    GLOBAL_NOTIFICATION_STATUS
    OnGlobalApplicationPreload(
        IN IGlobalApplicationPreloadProvider *    pProvider
    )
    {
        UNREFERENCED_PARAMETER( pProvider );
        OutputDebugStringA(
            "This module subscribed to event "
            __FUNCTION__
            " but did not override the method in its CGlobalModule implementation."
            "  Please check the method signature to make sure it matches the corresponding method.\n");
        DebugBreak();

        return GL_NOTIFICATION_CONTINUE;
    }

};

class __declspec(uuid("85c1679c-0b21-491c-afb5-c7b5c86464c4"))
IModuleAllocator
{
 public:
    virtual
    VOID *
    AllocateMemory(
        IN DWORD                    cbAllocation
    ) = 0;
};

class __declspec(uuid("ba32d330-9ea8-4b9e-89f1-8c76a323277f"))
IHttpModuleFactory
{
 public:
    virtual
    HRESULT
    GetHttpModule(
        OUT CHttpModule **          ppModule, 
        IN  IModuleAllocator *      pAllocator
    ) = 0;

    virtual
    VOID
    Terminate(
        VOID
    ) = 0;
};

//
// Register-module descriptor
//
class __declspec(uuid("07e5beb3-b798-459d-a98a-e6c485b2b3bc"))
IHttpModuleRegistrationInfo
{
 public:
    virtual 
    PCWSTR
    GetName(
        VOID
    ) const = 0;

    virtual 
    HTTP_MODULE_ID
    GetId(
        VOID
    ) const = 0;

    virtual 
    HRESULT
    SetRequestNotifications(
        IN IHttpModuleFactory * pModuleFactory,
        IN DWORD                dwRequestNotifications,
        IN DWORD                dwPostRequestNotifications
    ) = 0;

    virtual 
    HRESULT
    SetGlobalNotifications(
        IN CGlobalModule *      pGlobalModule,
        IN DWORD                dwGlobalNotifications
    ) = 0;

    virtual
    HRESULT
    SetPriorityForRequestNotification(
        IN DWORD                dwRequestNotification,
        IN PCWSTR               pszPriority
    ) = 0;

    virtual
    HRESULT
    SetPriorityForGlobalNotification(
        IN DWORD                dwGlobalNotification,
        IN PCWSTR               pszPriority
    ) = 0;
};


//
// Register Module entry point
// 

typedef
HRESULT
(WINAPI * PFN_REGISTERMODULE)(
    DWORD                           dwServerVersion,
    IHttpModuleRegistrationInfo *   pModuleInfo,
    IHttpServer *                   pGlobalInfo
);

#define MODULE_REGISTERMODULE   "RegisterModule"

#endif
