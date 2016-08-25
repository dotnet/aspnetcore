// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#define MIN_PORT                                    1025
#define MAX_PORT                                    48000
#define MAX_RETRY                                   10
#define LOCALHOST                                   "127.0.0.1"
#define ASPNETCORE_PORT_STR                         L"ASPNETCORE_PORT"
#define ASPNETCORE_PORT_PLACEHOLDER                 L"%ASPNETCORE_PORT%"
#define ASPNETCORE_PORT_PLACEHOLDER_CCH             17        
#define ASPNETCORE_DEBUG_PORT_STR                   L"ASPNETCORE_DEBUG_PORT"
#define ASPNETCORE_DEBUG_PORT_PLACEHOLDER           L"%ASPNETCORE_DEBUG_PORT%"
#define ASPNETCORE_DEBUG_PORT_PLACEHOLDER_CCH       23
#define MAX_ACTIVE_CHILD_PROCESSES                  16

class PROCESS_MANAGER;
class FORWARDER_CONNECTION;

class SERVER_PROCESS
{
public:
    SERVER_PROCESS();

    HRESULT
        Initialize(
        _In_ PROCESS_MANAGER    *pProcessManager,
        _In_ STRU               *pszProcessExePath,
        _In_ STRU               *pszArguments,
        _In_ DWORD               dwStartupTimeLimitInMS,
        _In_ DWORD               dwShtudownTimeLimitInMS,
        _In_ MULTISZ            *pszEnvironment,
        _In_ BOOL                fStdoutLogEnabled,
        _In_ STRU               *pstruStdoutLogFile
        );


    HRESULT
    StartProcess(
        _In_ IHttpContext *context
    );

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

    VOID
    StopProcess(
        VOID
    );

    DWORD 
    GetPort()
    {
        return m_dwPort;
    }

    DWORD 
    GetDebugPort()
    {
        return m_dwDebugPort;
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

    HRESULT 
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

    static
    VOID
    CALLBACK
    TimerCallback(
        _In_ PTP_CALLBACK_INSTANCE Instance,
        _In_ PVOID Context,
        _In_ PTP_TIMER Timer
    );

    LPCWSTR
    QueryFullLogPath()
    {
        return m_struFullLogFile.QueryStr();
    }

    LPCSTR
    QueryGuid()
    {
        return m_straGuid.QueryStr();
    }

    DWORD
    QueryProcessGroupId()
    {
        return m_dwProcessId;
    }

    VOID
    SendSignal( 
        VOID
    );

private:

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
        _In_ IHttpContext *context,
        _In_ LPSTARTUPINFOW pStartupInfo
    );

    HRESULT
    CheckIfServerIsUp(
        _In_  DWORD      dwPort,
        _Out_ BOOL      *pfReady
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
    );

    DWORD 
    GenerateRandomPort(
        VOID
    )
    {
        return (rand() % (MAX_PORT - MIN_PORT)) + MIN_PORT + 1;
    }

    DWORD
    GetNumberOfDigits( 
        _In_ DWORD dwNumber 
    )
    {
        DWORD digits = 0;
        
        if( dwNumber == 0 )
        {
            digits = 1;
            goto Finished;
        }

        while( dwNumber > 0)
        {
            dwNumber = dwNumber / 10;
            digits ++;
        }
    Finished:
        return digits;
    }

    FORWARDER_CONNECTION   *m_pForwarderConnection;
    HANDLE                  m_hJobObject;
    BOOL                    m_fStdoutLogEnabled;
    STRU                    m_struLogFile;
    STRU                    m_struFullLogFile;
    STTIMER                 m_Timer;
    HANDLE                  m_hStdoutHandle;
    volatile BOOL           m_fStopping;
    volatile BOOL           m_fReady;
    CRITICAL_SECTION        m_csLock;
    SOCKET                  m_socket;
    DWORD                   m_dwPort;
    DWORD                   m_dwDebugPort;
    STRU                    m_ProcessPath;
    STRU                    m_Arguments;
    DWORD                   m_dwStartupTimeLimitInMS;
    DWORD                   m_dwShutdownTimeLimitInMS;
    MULTISZ                 m_Environment;
    mutable LONG            m_cRefs;
    HANDLE                  m_hProcessWaitHandle;
    DWORD                   m_cChildProcess;
    HANDLE                  m_hChildProcessWaitHandles[MAX_ACTIVE_CHILD_PROCESSES];
    DWORD                   m_dwProcessId;
    DWORD                   m_dwListeningProcessId;
    STRA                    m_straGuid;
    HANDLE                  m_CancelEvent;

    //
    // m_hProcessHandle is the handle to process this object creates.
    //

    HANDLE                  m_hProcessHandle;
    HANDLE                  m_hListeningProcessHandle;

    //
    // m_hChildProcessHandle is the handle to process created by 
    // m_hProcessHandle process if it does.
    //

    HANDLE                  m_hChildProcessHandles[MAX_ACTIVE_CHILD_PROCESSES];
    DWORD                   m_dwChildProcessIds[MAX_ACTIVE_CHILD_PROCESSES];
    PROCESS_MANAGER         *m_pProcessManager;
};