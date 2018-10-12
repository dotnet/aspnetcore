// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

#pragma once
//-------------------------------------------------------------------------------------------------
// <summary>
//    Windows Installer XML CustomAction utility library.
// </summary>
//-------------------------------------------------------------------------------------------------

#ifdef __cplusplus
extern "C" {
#endif

#define WIXAPI __stdcall
#define ExitTrace WcaLogError
#define ExitTrace1 WcaLogError
#define ExitTrace2 WcaLogError
#define ExitTrace3 WcaLogError

#include "dutil.h"

#define MessageExitOnLastError(x, e, s)      { x = ::GetLastError(); x = HRESULT_FROM_WIN32(x); if (FAILED(x)) { ExitTrace(x, "%s", s); WcaErrorMessage(e, x, MB_OK, 0);  goto LExit; } }
#define MessageExitOnLastError1(x, e, f, s)  { x = ::GetLastError(); x = HRESULT_FROM_WIN32(x); if (FAILED(x)) { ExitTrace1(x, f, s); WcaErrorMessage(e, x, MB_OK, 1, s);  goto LExit; } }

#define MessageExitOnFailure(x, e, s)           if (FAILED(x)) { ExitTrace(x, "%s", s); WcaErrorMessage(e, x, INSTALLMESSAGE_ERROR | MB_OK, 0);  goto LExit; }
#define MessageExitOnFailure1(x, e, f, s)       if (FAILED(x)) { ExitTrace1(x, f, s); WcaErrorMessage(e, x, INSTALLMESSAGE_ERROR | MB_OK, 1, s);  goto LExit; }
#define MessageExitOnFailure2(x, e, f, s, t)    if (FAILED(x)) { ExitTrace2(x, f, s, t); WcaErrorMessage(e, x, INSTALLMESSAGE_ERROR | MB_OK, 2, s, t);  goto LExit; }
#define MessageExitOnFailure3(x, e, f, s, t, u) if (FAILED(x)) { ExitTrace2(x, f, s, t, u); WcaErrorMessage(e, x, INSTALLMESSAGE_ERROR | MB_OK, 3, s, t, u);  goto LExit; }

#define MessageExitOnNullWithLastError(p, x, e, s) if (NULL == p) { x = ::GetLastError(); x = HRESULT_FROM_WIN32(x); if (!FAILED(x)) { x = E_FAIL; } ExitTrace(x, "%s", s); WcaErrorMessage(e, x, MB_OK, 0);  goto LExit; }
#define MessageExitOnNullWithLastError1(p, x, e, f, s) if (NULL == p) { x = ::GetLastError(); x = HRESULT_FROM_WIN32(x); if (!FAILED(x)) { x = E_FAIL; } ExitTrace(x, f, s); WcaErrorMessage(e, x, MB_OK, 1, s);  goto LExit; }
#define MessageExitOnNullWithLastError2(p, x, e, f, s, t) if (NULL == p) { x = ::GetLastError(); x = HRESULT_FROM_WIN32(x); if (!FAILED(x)) { x = E_FAIL; } ExitTrace(x, f, s, t); WcaErrorMessage(e, x, MB_OK, 2, s, t);  goto LExit; }

#define MAX_DARWIN_KEY 73
#define MAX_DARWIN_COLUMN 255

// Generic action enum.
enum WCA_ACTION
{
    WCA_ACTION_NONE,
    WCA_ACTION_INSTALL,
    WCA_ACTION_UNINSTALL,
};

enum WCA_CASCRIPT
{
    WCA_CASCRIPT_SCHEDULED,
    WCA_CASCRIPT_ROLLBACK,
};

enum WCA_CASCRIPT_CLOSE
{
    WCA_CASCRIPT_CLOSE_PRESERVE,
    WCA_CASCRIPT_CLOSE_DELETE,
};

typedef struct WCA_CASCRIPT_STRUCT
{
    LPWSTR pwzScriptPath;
    HANDLE hScriptFile;
} *WCA_CASCRIPT_HANDLE;

void WIXAPI WcaGlobalInitialize(
    __in HINSTANCE hInst
    );
void WIXAPI WcaGlobalFinalize();

HRESULT WIXAPI WcaInitialize(
    __in MSIHANDLE hInstall,
    __in const char* szCustomActionLogName
    );
UINT WIXAPI WcaFinalize(
    __in UINT iReturnValue
    );
BOOL WIXAPI WcaIsInitialized();

MSIHANDLE WIXAPI WcaGetInstallHandle();
MSIHANDLE WIXAPI WcaGetDatabaseHandle();

const char* WIXAPI WcaGetLogName();

void WIXAPI WcaSetReturnValue(
    __in UINT iReturnValue
    );
BOOL WIXAPI WcaCancelDetected();

const int LOG_BUFFER = 2048;
enum LOGLEVEL
{ 
    LOGMSG_TRACEONLY,  // Never written to the log file (except in DEBUG builds)
    LOGMSG_VERBOSE,    // Written to log when LOGVERBOSE
    LOGMSG_STANDARD    // Written to log whenever informational logging is enabled
};

void __cdecl WcaLog(
    __in LOGLEVEL llv,
    __in const char* fmt, ...
    );
BOOL WIXAPI WcaDisplayAssert(
    __in LPCSTR sz
    );
void __cdecl WcaLogError(
    __in HRESULT hr,
    __in LPCSTR szMessage,
    ...
    );


UINT WIXAPI WcaProcessMessage(
    __in INSTALLMESSAGE eMessageType,
    __in MSIHANDLE hRecord
    );
UINT __cdecl WcaErrorMessage(
    __in int iError, 
    __in HRESULT hrError, 
    __in UINT uiType, 
    __in DWORD cArgs, 
    ...
    );
HRESULT WIXAPI WcaProgressMessage(
    __in UINT uiCost,
    __in BOOL fExtendProgressBar
    );

BOOL WIXAPI WcaIsInstalling(
    __in INSTALLSTATE isInstalled,
    __in INSTALLSTATE isAction
    );
BOOL WIXAPI WcaIsReInstalling(
    __in INSTALLSTATE isInstalled,
    __in INSTALLSTATE isAction
    );
BOOL WIXAPI WcaIsUninstalling(
    __in INSTALLSTATE isInstalled,
    __in INSTALLSTATE isAction
    );

HRESULT WIXAPI WcaSetComponentState(
    __in LPCWSTR wzComponent,
    __in INSTALLSTATE isState
    );

HRESULT WIXAPI WcaTableExists(
    __in LPCWSTR wzTable
    );

HRESULT WIXAPI WcaOpenView(
    __in LPCWSTR wzSql,
    __out MSIHANDLE* phView
    );
HRESULT WIXAPI WcaExecuteView(
    __in MSIHANDLE hView,
    __in MSIHANDLE hRec
    );
HRESULT WIXAPI WcaOpenExecuteView(
    __in LPCWSTR wzSql,
    __out MSIHANDLE* phView
    );
HRESULT WIXAPI WcaFetchRecord(
    __in MSIHANDLE hView,
    __out MSIHANDLE* phRec
    );
HRESULT WIXAPI WcaFetchSingleRecord(
    __in MSIHANDLE hView,
    __out MSIHANDLE* phRec
    );

HRESULT WIXAPI WcaGetProperty(
    __in LPCWSTR wzProperty,
    __inout LPWSTR* ppwzData
    );
HRESULT WIXAPI WcaGetFormattedProperty(
    __in LPCWSTR wzProperty,
    __out LPWSTR* ppwzData
    );
HRESULT WIXAPI WcaGetFormattedString(
    __in LPCWSTR wzString,
    __out LPWSTR* ppwzData
    );
HRESULT WIXAPI WcaGetIntProperty(
    __in LPCWSTR wzProperty,
    __inout int* piData
    );
HRESULT WIXAPI WcaGetTargetPath(
    __in LPCWSTR wzFolder,
    __out LPWSTR* ppwzData
    );
HRESULT WIXAPI WcaSetProperty(
    __in LPCWSTR wzPropertyName,
    __in LPCWSTR wzPropertyValue
    );
HRESULT WIXAPI WcaSetIntProperty(
    __in LPCWSTR wzPropertyName,
    __in int nPropertyValue
    );
BOOL WIXAPI WcaIsPropertySet(
    __in LPCSTR szProperty
    );

HRESULT WIXAPI WcaGetRecordInteger(
    __in MSIHANDLE hRec,
    __in UINT uiField,
    __inout int* piData
    );
HRESULT WIXAPI WcaGetRecordString(
    __in MSIHANDLE hRec,
    __in UINT uiField,
    __inout LPWSTR* ppwzData
    );
HRESULT WIXAPI WcaGetRecordFormattedString(
    __in MSIHANDLE hRec,
    __in UINT uiField,
    __inout LPWSTR* ppwzData
    );

HRESULT WIXAPI WcaAllocStream(__inout BYTE** ppbData, __in DWORD cbData);
HRESULT WIXAPI WcaFreeStream(__in BYTE* pbData);

HRESULT WIXAPI WcaGetRecordStream(
    __in MSIHANDLE hRecBinary,
    __in UINT uiField, 
    __inout BYTE** ppbData,
    __inout DWORD* pcbData
    );
HRESULT WIXAPI WcaSetRecordString(
    __in MSIHANDLE hRec,
    __in UINT uiField,
    __in LPCWSTR wzData
    );
HRESULT WIXAPI WcaSetRecordInteger(
    __in MSIHANDLE hRec,
    __in UINT uiField,
    __in int iValue
    );

HRESULT WIXAPI WcaDoDeferredAction(
    __in LPCWSTR wzAction,
    __in LPCWSTR wzCustomActionData,
    __in UINT uiCost
    );
DWORD WIXAPI WcaCountOfCustomActionDataRecords(
    __in LPCWSTR wzData
    );
HRESULT WIXAPI WcaReadStringFromCaData(
    __inout LPWSTR* ppwzCustomActionData,
    __inout LPWSTR* ppwzString
    );
HRESULT WIXAPI WcaReadIntegerFromCaData(
    __inout LPWSTR* ppwzCustomActionData,
    __inout int* piResult
    );
HRESULT WIXAPI WcaReadStreamFromCaData(
    __inout LPWSTR* ppwzCustomActionData,
    __out BYTE** ppbData,
    __out DWORD_PTR* pcbData
    );
HRESULT WIXAPI WcaWriteStringToCaData(
    __in LPCWSTR wzString,
    __inout LPWSTR* ppwzCustomActionData
    );
HRESULT WIXAPI WcaWriteIntegerToCaData(
    __in int i, 
    __inout LPWSTR* ppwzCustomActionData
    );
HRESULT WIXAPI WcaWriteStreamToCaData(
    __in_bcount(cbData) const BYTE* pbData,
    __in DWORD cbData,
    __inout LPWSTR* ppwzCustomActionData
    );

HRESULT __cdecl WcaAddTempRecord(
    __inout MSIHANDLE* phTableView,
    __inout MSIHANDLE* phColumns,
    __in LPCWSTR wzTable,
    __in UINT uiUniquifyColumn,
    __in UINT cColumns,
    ...
    );

HRESULT WIXAPI WcaDumpTable(
    __in LPCWSTR wzTable
    );


HRESULT WIXAPI WcaCaScriptCreateKey(
    __out LPWSTR* ppwzScriptKey
    );

HRESULT WIXAPI WcaCaScriptCreate(
    __in WCA_ACTION action,
    __in WCA_CASCRIPT script,
    __in BOOL fImpersonated,
    __in LPCWSTR wzScriptKey,
    __in BOOL fAppend,
    __out WCA_CASCRIPT_HANDLE* phScript
    );

HRESULT WIXAPI WcaCaScriptOpen(
    __in WCA_ACTION action,
    __in WCA_CASCRIPT script,
    __in BOOL fImpersonated,
    __in LPCWSTR wzScriptKey,
    __out WCA_CASCRIPT_HANDLE* phScript
    );

void WIXAPI WcaCaScriptClose(
    __in WCA_CASCRIPT_HANDLE hScript,
    __in WCA_CASCRIPT_CLOSE closeOperation
    );

HRESULT WIXAPI WcaCaScriptReadAsCustomActionData(
    __in WCA_CASCRIPT_HANDLE hScript,
    __out LPWSTR* ppwzCustomActionData
    );

HRESULT WIXAPI WcaCaScriptWriteString(
    __in WCA_CASCRIPT_HANDLE hScript,
    __in LPCWSTR wzValue
    );

HRESULT WIXAPI WcaCaScriptWriteNumber(
    __in WCA_CASCRIPT_HANDLE hScript,
    __in DWORD dwValue
    );

void WIXAPI WcaCaScriptFlush(
    __in WCA_CASCRIPT_HANDLE hScript
    );

void WIXAPI WcaCaScriptCleanup(
    __in LPCWSTR wzProductCode,
    __in BOOL fImpersonated
    );


HRESULT QuietExec(
    __in LPWSTR wzCommand,
    __in DWORD dwTimeout
    );

#ifdef __cplusplus
}
#endif
