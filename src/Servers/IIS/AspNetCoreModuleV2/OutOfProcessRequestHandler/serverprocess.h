// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <random>
#include <map>

// Minimum port number that can be used.
// This is lower than 'MIN_PORT_RANDOM' since we allow people to choose
// low-numbered ports when they are not using random assignment.
#define MIN_PORT                                    1025
// Minimum number used for automatic random port assignment.
// This value is somewhat arbitrary. Fixed-port services tend to
// use ports near the minimum (1025) so this number is much higher
// to reduce the chance of collisions.
#define MIN_PORT_RANDOM                             10000
#define MAX_PORT                                    48000
#define MAX_ACTIVE_CHILD_PROCESSES                  16
#define PIPE_OUTPUT_THREAD_TIMEOUT                  2000
#define LOCALHOST                                   "127.0.0.1"
#define ASPNETCORE_PORT_STR                         L"ASPNETCORE_PORT"
#define ASPNETCORE_PORT_ENV_STR                     L"ASPNETCORE_PORT="
#define ASPNETCORE_APP_PATH_ENV_STR                 L"ASPNETCORE_APPL_PATH="
#define ASPNETCORE_APP_TOKEN_ENV_STR                L"ASPNETCORE_TOKEN="

class PROCESS_MANAGER;

class SERVER_PROCESS
{
public:
    SERVER_PROCESS();

    HRESULT
        Initialize(
        _In_ PROCESS_MANAGER      *pProcessManager,
        _In_ STRU                 *pszProcessExePath,
        _In_ STRU                 *pszArguments,
        _In_ DWORD                 dwStartupTimeLimitInMS,
        _In_ DWORD                 dwShtudownTimeLimitInMS,
        _In_ BOOL                  fWindowsAuthEnabled,
        _In_ BOOL                  fBasicAuthEnabled,
        _In_ BOOL                  fAnonymousAuthEnabled,
        _In_ std::map<std::wstring, std::wstring, ignore_case_comparer>& pEnvironmentVariables,
        _In_ BOOL                  fStdoutLogEnabled,
        _In_ BOOL                  fDisableRedirection,
        _In_ BOOL                  fWebSocketSupported,
        _In_ STRU                 *pstruStdoutLogFile,
        _In_ STRU                 *pszAppPhysicalPath,
        _In_ STRU                 *pszAppPath,
        _In_ STRU                 *pszAppVirtualPath,
        _In_ STRU                 *pszHttpsPort
        );

    HRESULT
    StartProcess( VOID );

    HRESULT
    SetWindowsAuthToken(
        _In_ HANDLE hToken,
        _Out_ LPHANDLE pTargeTokenHandle
    );

    BOOL
    IsReady(
        VOID
    )
    {
        return m_fReady;
    }

    BOOL
    IsDebuggerAttached(
            VOID
    )
    {
        return m_fDebuggerAttached;
    }

    VOID
    StopProcess(
        VOID
    );

    DWORD
    GetPort()
    {
        return m_dwPort;
    }

    VOID
    ReferenceServerProcess(
        VOID
    )
    {
        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceServerProcess(
        VOID
    )
    {
        _ASSERT(m_cRefs != 0 );
        if (InterlockedDecrement(&m_cRefs) == 0)
        {
            delete this;
        }
    }

    virtual
    ~SERVER_PROCESS();

    static
    VOID
    CALLBACK
    ProcessHandleCallback(
        _In_ PVOID  pContext,
        _In_ BOOL
    );

    VOID
    HandleProcessExit(
        VOID
    );

    FORWARDER_CONNECTION*
    QueryWinHttpConnection(
        VOID
    )
    {
        return m_pForwarderConnection;
    }

    LPCSTR
    QueryGuid()
    {
        return m_straGuid.QueryStr();
    };

    VOID
    SendSignal(
        VOID
    );

    static
        void
        ReadStdErrHandle(
            LPVOID pContext
        );

    void
        ReadStdErrHandleInternal();

private:
    VOID
    CleanUp();

    HRESULT
    SetupJobObject(
       VOID
    );

    BOOL
    IsDebuggerIsAttached(
        VOID
    );

    HRESULT
    StopAllProcessesInJobObject(
        VOID
    );

    HRESULT
    SetupStdHandles(
        _Inout_ LPSTARTUPINFOW pStartupInfo
    );

    HRESULT
    CheckIfServerIsUp(
        _In_  DWORD       dwPort,
        _Out_ DWORD     * pdwProcessId,
        _Out_ BOOL      * pfReady
    );

    HRESULT
    RegisterProcessWait(
        _In_ PHANDLE phWaitHandle,
        _In_ HANDLE  hProcessToWaitOn
    );

    HRESULT
    GetChildProcessHandles(
        VOID
    );

    HRESULT
    SetupListenPort(
        ENVIRONMENT_VAR_HASH    *pEnvironmentVarTable,
        BOOL                    *pfCriticalError
    );

    HRESULT
    SetupAppPath(
        ENVIRONMENT_VAR_HASH*   pEnvironmentVarTable
    );

    HRESULT
    SetupAppToken(
        ENVIRONMENT_VAR_HASH*   pEnvironmentVarTable
    );

    HRESULT
    OutputEnvironmentVariables(
        MULTISZ*                pmszOutput,
        ENVIRONMENT_VAR_HASH*   pEnvironmentVarTable
    );

    HRESULT
    SetupCommandLine(
        STRU*    pstrCommandLine
    );

    HRESULT
    PostStartCheck(
        VOID
    );

    HRESULT
    GetRandomPort(
        DWORD*    pdwPickedPort,
        DWORD     dwExcludedPort
    );

    static
    VOID
    SendShutDownSignal(
        LPVOID lpParam
        );

    VOID
    SendShutDownSignalInternal(
        VOID
    );

    HRESULT
    SendShutdownHttpMessage(
        VOID
    );

    VOID
    TerminateBackendProcess(
        VOID
    );

    FORWARDER_CONNECTION   *m_pForwarderConnection;
    BOOL                    m_fStdoutLogEnabled;
    BOOL                    m_fWebSocketSupported;
    BOOL                    m_fWindowsAuthEnabled;
    BOOL                    m_fBasicAuthEnabled;
    BOOL                    m_fAnonymousAuthEnabled;
    BOOL                    m_fDebuggerAttached;
    BOOL                    m_fEnableOutOfProcessConsoleRedirection;

    STTIMER                 m_Timer;
    SOCKET                  m_socket;

    STRU                    m_struLogFile;
    STRU                    m_struFullLogFile;
    STRU                    m_ProcessPath;
    STRU                    m_Arguments;
    STRU                    m_struAppVirtualPath;  // e.g., '/' for site
    STRU                    m_struAppFullPath;     // e.g.,  /LM/W3SVC/4/ROOT/Inproc
    STRU                    m_struPhysicalPath;    // e.g., c:/test/mysite
    STRU                    m_struHttpsPort;     // e.g.,  /LM/W3SVC/4/ROOT/Inproc
    STRU                    m_struPort;
    STRU                    m_struCommandLine;

    volatile LONG           m_lStopping;
    volatile BOOL           m_fReady;
    mutable LONG            m_cRefs;

    std::mt19937            m_randomGenerator;

    DWORD                   m_dwPort;
    DWORD                   m_dwStartupTimeLimitInMS;
    DWORD                   m_dwShutdownTimeLimitInMS;
    DWORD                   m_cChildProcess;
    DWORD                   m_dwChildProcessIds[MAX_ACTIVE_CHILD_PROCESSES];
    DWORD                   m_dwProcessId;
    DWORD                   m_dwListeningProcessId;

    STRA                    m_straGuid;

    HANDLE                  m_hJobObject;
    HANDLE                  m_hStdoutHandle;
    //
    // m_hProcessHandle is the handle to process this object creates.
    //
    HANDLE                  m_hProcessHandle;
    HANDLE                  m_hListeningProcessHandle;
    HANDLE                  m_hProcessWaitHandle;
    HANDLE                  m_hShutdownHandle;
    HANDLE                  m_hReadThread;
    HANDLE                  m_hStdErrWritePipe;
    std::wstringstream      m_output;
    //
    // m_hChildProcessHandle is the handle to process created by
    // m_hProcessHandle process if it does.
    //
    HANDLE                  m_hChildProcessHandles[MAX_ACTIVE_CHILD_PROCESSES];
    HANDLE                  m_hChildProcessWaitHandles[MAX_ACTIVE_CHILD_PROCESSES];

    PROCESS_MANAGER         *m_pProcessManager;
    std::map<std::wstring, std::wstring, ignore_case_comparer> m_pEnvironmentVarTable;
};
