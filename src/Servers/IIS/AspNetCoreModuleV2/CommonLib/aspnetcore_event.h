
#ifndef __ASPNETCOREEVENT_H__
#define __ASPNETCOREEVENT_H__
/*++

    Copyright (c) 2005 Microsoft Corporation

    Module Name:

        aspnetcore_event.h

    Abstract:

        Header file has been generated from mof file containing 
        IIS trace event descriptions

--*/



//
// Start of the new provider class WWWServerTraceProvider,
// GUID: {3a2a4e84-4c21-4981-ae10-3fda0d9b0f83}
// Description: IIS: WWW Server
//

class WWWServerTraceProvider
{
public:
    static
    LPCGUID
    GetProviderGuid( VOID )
    // return GUID for the current event class
    {
        static const GUID ProviderGuid = 
          {0x3a2a4e84,0x4c21,0x4981,{0xae,0x10,0x3f,0xda,0x0d,0x9b,0x0f,0x83}};
        return &ProviderGuid;
    };
    enum enumAreaFlags
    {
        // AspNetCore module events
        ANCM = 0x10000
    };
    static
    LPCWSTR
    TranslateEnumAreaFlagsToString( enum enumAreaFlags EnumValue)
    {
        switch( (DWORD) EnumValue )
        {
        case 0x10000: return L"ANCM";
        }
        return nullptr;
    };

    static
    BOOL
    CheckTracingEnabled(
        IHttpTraceContext * pHttpTraceContext,
        enumAreaFlags       AreaFlags,
        DWORD               dwVerbosity )
    {
        HTTP_TRACE_CONFIGURATION TraceConfig;
        TraceConfig.pProviderGuid = GetProviderGuid();
        HRESULT hr = pHttpTraceContext->GetTraceConfiguration( &TraceConfig );
        if ( FAILED( hr )  || !TraceConfig.fProviderEnabled )
        {
            return FALSE;
        }
        if ( TraceConfig.dwVerbosity >= dwVerbosity && 
             (  TraceConfig.dwAreas == (DWORD) AreaFlags || 
               ( TraceConfig.dwAreas & (DWORD)AreaFlags ) == (DWORD)AreaFlags ) ) 
        { 
            return TRUE;
        } 
        return FALSE;
    };
};

//
// Start of the new event class ANCMEvents,
// GUID: {82ADEAD7-12B2-4781-BDCA-5A4B6C757191}
// Description: ANCM runtime events
//

class ANCMEvents
{
public:
    static
    LPCGUID
    GetAreaGuid( VOID )
    // return GUID for the current event class
    {
        static const GUID AreaGuid = 
          {0x82adead7,0x12b2,0x4781,{0xbd,0xca,0x5a,0x4b,0x6c,0x75,0x71,0x91}};
        return &AreaGuid;
    };

    //
    // Event: mof class name ANCMAppStart,
    // Description: Start application success
    // EventTypeName: ANCM_START_APPLICATION_SUCCESS
    // EventType: 1
    // EventLevel: 4
    //
    
    class ANCM_START_APPLICATION_SUCCESS
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId,
            LPCWSTR     pAppDescription
        )
        //
        // Raise ANCM_START_APPLICATION_SUCCESS Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 1;
            Event.pszEventName = L"ANCM_START_APPLICATION_SUCCESS";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 4;
            Event.cEventItems = 2;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 2 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Items[ 1 ].pszName = L"AppDescription";
            Items[ 1 ].dwDataType = HTTP_TRACE_TYPE_LPCWSTR; // mof type (string)
            Items[ 1 ].pbData = (PBYTE) pAppDescription;
            Items[ 1 ].cbData  = 
                 ( Items[ 1 ].pbData == nullptr )? 0 : ( sizeof(WCHAR) * (1 + (DWORD) wcslen( (PWSTR) Items[ 1 ].pbData  ) ) );
            Items[ 1 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 4 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMAppStartFail,
    // Description: Start application failed
    // EventTypeName: ANCM_START_APPLICATION_FAIL
    // EventType: 2
    // EventLevel: 2
    //
    
    class ANCM_START_APPLICATION_FAIL
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId,
            LPCWSTR     pFailureDescription
        )
        //
        // Raise ANCM_START_APPLICATION_FAIL Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 2;
            Event.pszEventName = L"ANCM_START_APPLICATION_FAIL";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 2;
            Event.cEventItems = 2;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 2 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Items[ 1 ].pszName = L"FailureDescription";
            Items[ 1 ].dwDataType = HTTP_TRACE_TYPE_LPCWSTR; // mof type (string)
            Items[ 1 ].pbData = (PBYTE) pFailureDescription;
            Items[ 1 ].cbData  = 
                 ( Items[ 1 ].pbData == nullptr)? 0 : ( sizeof(WCHAR) * (1 + (DWORD) wcslen( (PWSTR) Items[ 1 ].pbData  ) ) );
            Items[ 1 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 2 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMForwardStart,
    // Description: Start forwarding request
    // EventTypeName: ANCM_REQUEST_FORWARD_START
    // EventType: 3
    // EventLevel: 4
    //
    
    class ANCM_REQUEST_FORWARD_START
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId
        )
        //
        // Raise ANCM_REQUEST_FORWARD_START Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 3;
            Event.pszEventName = L"ANCM_REQUEST_FORWARD_START";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 4;
            Event.cEventItems = 1;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 1 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 4 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMForwardEnd,
    // Description: Finish forwarding request
    // EventTypeName: ANCM_REQUEST_FORWARD_END
    // EventType: 4
    // EventLevel: 4
    //
    
    class ANCM_REQUEST_FORWARD_END
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId
        )
        //
        // Raise ANCM_REQUEST_FORWARD_END Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 4;
            Event.pszEventName = L"ANCM_REQUEST_FORWARD_END";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 4;
            Event.cEventItems = 1;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 1 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 4 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMForwardFail,
    // Description: Forwarding request failure
    // EventTypeName: ANCM_REQUEST_FORWARD_FAIL
    // EventType: 5
    // EventLevel: 2
    //
    
    class ANCM_REQUEST_FORWARD_FAIL
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId,
            ULONG      ErrorCode
        )
        //
        // Raise ANCM_REQUEST_FORWARD_FAIL Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 5;
            Event.pszEventName = L"ANCM_REQUEST_FORWARD_FAIL";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 2;
            Event.cEventItems = 2;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 2 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Items[ 1 ].pszName = L"ErrorCode";
            Items[ 1 ].dwDataType = HTTP_TRACE_TYPE_ULONG; // mof type (uint32)
            Items[ 1 ].pbData = (PBYTE) &ErrorCode;
            Items[ 1 ].cbData = 4;
            Items[ 1 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 2 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMWinHttpCallBack,
    // Description: Receiving callback from WinHttp
    // EventTypeName: ANCM_WINHTTP_CALLBACK
    // EventType: 6
    // EventLevel: 4
    //
    
    class ANCM_WINHTTP_CALLBACK
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId,
            ULONG      InternetStatus
        )
        //
        // Raise ANCM_WINHTTP_CALLBACK Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 6;
            Event.pszEventName = L"ANCM_WINHTTP_CALLBACK";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 4;
            Event.cEventItems = 2;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 2 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Items[ 1 ].pszName = L"InternetStatus";
            Items[ 1 ].dwDataType = HTTP_TRACE_TYPE_ULONG; // mof type (uint32)
            Items[ 1 ].pbData = (PBYTE) &InternetStatus;
            Items[ 1 ].cbData = 4;
            Items[ 1 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 4 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMExecuteStart,
    // Description: Starting inprocess execute request
    // EventTypeName: ANCM_INPROC_EXECUTE_REQUEST_START
    // EventType: 7
    // EventLevel: 4
    //
    
    class ANCM_INPROC_EXECUTE_REQUEST_START
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId
        )
        //
        // Raise ANCM_INPROC_EXECUTE_REQUEST_START Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 7;
            Event.pszEventName = L"ANCM_INPROC_EXECUTE_REQUEST_START";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 4;
            Event.cEventItems = 1;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 1 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 4 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMExecuteEnd,
    // Description: Ending inprocess execute request
    // EventTypeName: ANCM_INPROC_EXECUTE_REQUEST_COMPLETION
    // EventType: 8
    // EventLevel: 5
    //
    
    class ANCM_INPROC_EXECUTE_REQUEST_COMPLETION
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId,
            ULONG      requestStatus
        )
        //
        // Raise ANCM_INPROC_EXECUTE_REQUEST_COMPLETION Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 8;
            Event.pszEventName = L"ANCM_INPROC_EXECUTE_REQUEST_COMPLETION";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 5;
            Event.cEventItems = 2;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 2 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Items[ 1 ].pszName = L"requestStatus";
            Items[ 1 ].dwDataType = HTTP_TRACE_TYPE_ULONG; // mof type (uint32)
            Items[ 1 ].pbData = (PBYTE) &requestStatus;
            Items[ 1 ].cbData = 4;
            Items[ 1 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 5 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMAsyncStart,
    // Description: Starting inprocess async completion
    // EventTypeName: ANCM_INPROC_ASYNC_COMPLETION_START
    // EventType: 9
    // EventLevel: 5
    //
    
    class ANCM_INPROC_ASYNC_COMPLETION_START
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId
        )
        //
        // Raise ANCM_INPROC_ASYNC_COMPLETION_START Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 9;
            Event.pszEventName = L"ANCM_INPROC_ASYNC_COMPLETION_START";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 5;
            Event.cEventItems = 1;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 1 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 5 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMAsyncEnd,
    // Description: Ending inprocess async completion
    // EventTypeName: ANCM_INPROC_ASYNC_COMPLETION_COMPLETION
    // EventType: 10
    // EventLevel: 5
    //
    
    class ANCM_INPROC_ASYNC_COMPLETION_COMPLETION
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId,
            ULONG      requestStatus
        )
        //
        // Raise ANCM_INPROC_ASYNC_COMPLETION_COMPLETION Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 10;
            Event.pszEventName = L"ANCM_INPROC_ASYNC_COMPLETION_COMPLETION";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 5;
            Event.cEventItems = 2;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 2 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Items[ 1 ].pszName = L"requestStatus";
            Items[ 1 ].dwDataType = HTTP_TRACE_TYPE_ULONG; // mof type (uint32)
            Items[ 1 ].pbData = (PBYTE) &requestStatus;
            Items[ 1 ].cbData = 4;
            Items[ 1 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 5 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMRequestShutdown,
    // Description: Inprocess app shutdown
    // EventTypeName: ANCM_INPROC_REQUEST_SHUTDOWN
    // EventType: 11
    // EventLevel: 4
    //
    
    class ANCM_INPROC_REQUEST_SHUTDOWN
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId
        )
        //
        // Raise ANCM_INPROC_REQUEST_SHUTDOWN Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 11;
            Event.pszEventName = L"ANCM_INPROC_REQUEST_SHUTDOWN";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 4;
            Event.cEventItems = 1;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 1 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 4 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMRequestDisconnect,
    // Description: Inprocess request disconnect
    // EventTypeName: ANCM_INPROC_REQUEST_DISCONNECT
    // EventType: 12
    // EventLevel: 4
    //
    
    class ANCM_INPROC_REQUEST_DISCONNECT
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId
        )
        //
        // Raise ANCM_INPROC_REQUEST_DISCONNECT Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 12;
            Event.pszEventName = L"ANCM_INPROC_REQUEST_DISCONNECT";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 4;
            Event.cEventItems = 1;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 1 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 4 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMManagedRequestCompletion,
    // Description: Indicate managed request complete
    // EventTypeName: ANCM_INPROC_MANAGED_REQUEST_COMPLETION
    // EventType: 13
    // EventLevel: 4
    //
    
    class ANCM_INPROC_MANAGED_REQUEST_COMPLETION
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId
        )
        //
        // Raise ANCM_INPROC_MANAGED_REQUEST_COMPLETION Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 13;
            Event.pszEventName = L"ANCM_INPROC_MANAGED_REQUEST_COMPLETION";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 4;
            Event.cEventItems = 1;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 1 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 4 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMHRESULTFailed,
    // Description: Failed HRESULT
    // EventTypeName: ANCM_HRESULT_FAILED
    // EventType: 14
    // EventLevel: 3
    //
    
    class ANCM_HRESULT_FAILED
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId,
            LPCSTR      pFile,
            ULONG      Line,
            ULONG      HResult
        )
        //
        // Raise ANCM_HRESULT_FAILED Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 14;
            Event.pszEventName = L"ANCM_HRESULT_FAILED";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 3;
            Event.cEventItems = 4;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 4 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Items[ 1 ].pszName = L"File";
            Items[ 1 ].dwDataType = HTTP_TRACE_TYPE_LPCSTR; // mof type (string)
            Items[ 1 ].pbData = (PBYTE) pFile;
            Items[ 1 ].cbData  = 
                 ( Items[ 1 ].pbData == nullptr )? 0 : (1 + (DWORD) strlen( (PSTR) Items[ 1 ].pbData ) );
            Items[ 1 ].pszDataDescription = nullptr;
            Items[ 2 ].pszName = L"Line";
            Items[ 2 ].dwDataType = HTTP_TRACE_TYPE_ULONG; // mof type (uint32)
            Items[ 2 ].pbData = (PBYTE) &Line;
            Items[ 2 ].cbData = 4;
            Items[ 2 ].pszDataDescription = nullptr;
            Items[ 3 ].pszName = L"HResult";
            Items[ 3 ].dwDataType = HTTP_TRACE_TYPE_ULONG; // mof type (uint32)
            Items[ 3 ].pbData = (PBYTE) &HResult;
            Items[ 3 ].cbData = 4;
            Items[ 3 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 3 ); //Verbosity
        };
    };
    //
    // Event: mof class name ANCMExceptionCaughFailed,
    // Description: Caught exception
    // EventTypeName: ANCM_EXCEPTION_CAUGHT
    // EventType: 15
    // EventLevel: 3
    //
    
    class ANCM_EXCEPTION_CAUGHT
    {
    public:
        static
        HRESULT
        RaiseEvent(
            IHttpTraceContext * pHttpTraceContext,
            LPCGUID    pContextId,
            LPCSTR      pFile,
            ULONG      Line,
            LPCSTR      pDescription
        )
        //
        // Raise ANCM_EXCEPTION_CAUGHT Event
        //
        {
            HTTP_TRACE_EVENT Event;
            Event.pProviderGuid = WWWServerTraceProvider::GetProviderGuid();
            Event.dwArea =  WWWServerTraceProvider::ANCM;
            Event.pAreaGuid = ANCMEvents::GetAreaGuid();
            Event.dwEvent = 15;
            Event.pszEventName = L"ANCM_EXCEPTION_CAUGHT";
            Event.dwEventVersion = 1;
            Event.dwVerbosity = 3;
            Event.cEventItems = 4;
            Event.pActivityGuid = nullptr;
            Event.pRelatedActivityGuid = nullptr;
            Event.dwTimeStamp = 0;
            Event.dwFlags = HTTP_TRACE_EVENT_FLAG_STATIC_DESCRIPTIVE_FIELDS;
    
            // pActivityGuid, pRelatedActivityGuid, Timestamp to be filled in by IIS
    
            HTTP_TRACE_EVENT_ITEM Items[ 4 ];
            Items[ 0 ].pszName = L"ContextId";
            Items[ 0 ].dwDataType = HTTP_TRACE_TYPE_LPCGUID; // mof type (object)
            Items[ 0 ].pbData = (PBYTE) pContextId;
            Items[ 0 ].cbData = 16;
            Items[ 0 ].pszDataDescription = nullptr;
            Items[ 1 ].pszName = L"File";
            Items[ 1 ].dwDataType = HTTP_TRACE_TYPE_LPCSTR; // mof type (string)
            Items[ 1 ].pbData = (PBYTE) pFile;
            Items[ 1 ].cbData  = 
                 ( Items[ 1 ].pbData == nullptr )? 0 : (1 + (DWORD) strlen( (PSTR) Items[ 1 ].pbData ) );
            Items[ 1 ].pszDataDescription = nullptr;
            Items[ 2 ].pszName = L"Line";
            Items[ 2 ].dwDataType = HTTP_TRACE_TYPE_ULONG; // mof type (uint32)
            Items[ 2 ].pbData = (PBYTE) &Line;
            Items[ 2 ].cbData = 4;
            Items[ 2 ].pszDataDescription = nullptr;
            Items[ 3 ].pszName = L"Description";
            Items[ 3 ].dwDataType = HTTP_TRACE_TYPE_LPCSTR; // mof type (string)
            Items[ 3 ].pbData = (PBYTE) pDescription;
            Items[ 3 ].cbData  = 
                 ( Items[ 3 ].pbData == nullptr )? 0 : (1 + (DWORD) strlen( (PSTR) Items[ 3 ].pbData ) );
            Items[ 3 ].pszDataDescription = nullptr;
            Event.pEventItems = Items;
            pHttpTraceContext->RaiseTraceEvent( &Event );
            return S_OK;
        };
    
        static
        BOOL
        IsEnabled( 
            IHttpTraceContext *  pHttpTraceContext )
        // Check if tracing for this event is enabled
        {
            return WWWServerTraceProvider::CheckTracingEnabled( 
                                 pHttpTraceContext,
                                 WWWServerTraceProvider::ANCM,
                                 3 ); //Verbosity
        };
    };
};
#endif
