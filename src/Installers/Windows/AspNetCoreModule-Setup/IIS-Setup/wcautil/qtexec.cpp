// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

//-------------------------------------------------------------------------------------------------
// <summary>
//    Executes command line instructions without popping up a shell.
// </summary>
//-------------------------------------------------------------------------------------------------

#include "precomp.h"

#define OUTPUT_BUFFER 1024


#define ONEMINUTE 60000

static HRESULT CreatePipes(
    __out HANDLE *phOutRead,
    __out HANDLE *phOutWrite,
    __out HANDLE *phErrWrite,
    __out HANDLE *phInRead,
    __out HANDLE *phInWrite
    )
{
    Assert(phOutRead);
    Assert(phOutWrite);
    Assert(phErrWrite);
    Assert(phInRead);
    Assert(phInWrite);

    HRESULT hr = S_OK;
    SECURITY_ATTRIBUTES sa;
    HANDLE hOutTemp = INVALID_HANDLE_VALUE;
    HANDLE hInTemp = INVALID_HANDLE_VALUE;

    HANDLE hOutRead = INVALID_HANDLE_VALUE;
    HANDLE hOutWrite = INVALID_HANDLE_VALUE;
    HANDLE hErrWrite = INVALID_HANDLE_VALUE;
    HANDLE hInRead = INVALID_HANDLE_VALUE;
    HANDLE hInWrite = INVALID_HANDLE_VALUE;

    // Fill out security structure so we can inherit handles
    ::ZeroMemory(&sa, sizeof(SECURITY_ATTRIBUTES));
    sa.nLength = sizeof(SECURITY_ATTRIBUTES);
    sa.bInheritHandle = TRUE;
    sa.lpSecurityDescriptor = NULL;

    // Create pipes
    if (!::CreatePipe(&hOutTemp, &hOutWrite, &sa, 0))
        ExitOnLastError(hr, "failed to create output pipe");

    if (!::CreatePipe(&hInRead, &hInTemp, &sa, 0))
        ExitOnLastError(hr, "failed to create input pipe");


    // Duplicate output pipe so standard error and standard output write to
    // the same pipe
    if (!::DuplicateHandle(::GetCurrentProcess(), hOutWrite, ::GetCurrentProcess(), &hErrWrite, 0, TRUE, DUPLICATE_SAME_ACCESS))
        ExitOnLastError(hr, "failed to duplicate write handle");

    // We need to create new output read and input write handles that are
    // non inheritable.  Otherwise it creates handles that can't be closed.
    if (!::DuplicateHandle(::GetCurrentProcess(), hOutTemp, ::GetCurrentProcess(), &hOutRead, 0, FALSE, DUPLICATE_SAME_ACCESS))
        ExitOnLastError(hr, "failed to duplicate output pipe");

    if (!::DuplicateHandle(::GetCurrentProcess(), hInTemp, ::GetCurrentProcess(), &hInWrite, 0, FALSE, DUPLICATE_SAME_ACCESS))
        ExitOnLastError(hr, "failed to duplicate input pipe");

    // now that everything has succeeded, assign to the outputs
    *phOutRead = hOutRead;
    hOutRead = INVALID_HANDLE_VALUE;

    *phOutWrite = hOutWrite;
    hOutWrite = INVALID_HANDLE_VALUE;

    *phErrWrite = hErrWrite;
    hErrWrite = INVALID_HANDLE_VALUE;

    *phInRead = hInRead;
    hInRead = INVALID_HANDLE_VALUE;

    *phInWrite = hInWrite;
    hInWrite = INVALID_HANDLE_VALUE;

LExit:
    if (INVALID_HANDLE_VALUE != hOutRead)
        ::CloseHandle(hOutRead);
    if (INVALID_HANDLE_VALUE != hOutWrite)
        ::CloseHandle(hOutWrite);
    if (INVALID_HANDLE_VALUE != hErrWrite)
        ::CloseHandle(hErrWrite);
    if (INVALID_HANDLE_VALUE != hInRead)
        ::CloseHandle(hInRead);
    if (INVALID_HANDLE_VALUE != hInWrite)
        ::CloseHandle(hInWrite);
    if (INVALID_HANDLE_VALUE != hOutTemp)
        ::CloseHandle(hOutTemp);
    if (INVALID_HANDLE_VALUE != hInTemp)
        ::CloseHandle(hInTemp);

    return hr;
}

static HRESULT LogOutput(
    __in HANDLE hRead
    )
{
    BYTE *pBuffer = NULL;
    LPWSTR szLog = NULL;
    LPWSTR szTemp = NULL;
    LPWSTR pEnd = NULL;
    LPWSTR pNext = NULL;
    LPSTR szWrite = NULL;
    DWORD dwBytes = OUTPUT_BUFFER;
    BOOL bFirst = TRUE;
    BOOL bUnicode = TRUE;
    HRESULT hr = S_OK;

    // Get buffer for output
    pBuffer = (BYTE *)MemAlloc(OUTPUT_BUFFER, FALSE);
    ExitOnNull(pBuffer, hr, E_OUTOFMEMORY, "Failed to allocate buffer for output.");

    while (0 != dwBytes)
    {
        ::ZeroMemory(pBuffer, OUTPUT_BUFFER);
        if(!::ReadFile(hRead, pBuffer, OUTPUT_BUFFER - 1, &dwBytes, NULL) && GetLastError() != ERROR_BROKEN_PIPE)
        {
            ExitOnLastError(hr, "Failed to read from handle.");
        }

        // Check for UNICODE or ANSI output
        if (bFirst)
        {
            if ((isgraph(pBuffer[0]) && isgraph(pBuffer[1])) ||
                (isgraph(pBuffer[0]) && isspace(pBuffer[1])) ||
                (isspace(pBuffer[0]) && isgraph(pBuffer[1])) ||
                (isspace(pBuffer[0]) && isspace(pBuffer[1])))
                bUnicode = FALSE;
            bFirst = FALSE;
        }

        // Keep track of output
        if (bUnicode)
        {
            hr = StrAllocConcat(&szLog, (LPWSTR)pBuffer, 0);
            ExitOnFailure(hr, "failed to concatenate output strings");
        }
        else
        {
            hr = StrAllocStringAnsi(&szTemp, (LPSTR)pBuffer, 0, CP_OEMCP);
            ExitOnFailure(hr, "failed to allocate output string");
            hr = StrAllocConcat(&szLog, szTemp, 0);
            ExitOnFailure(hr, "failed to concatenate output strings");
        }

        // Log each line of the output
        pNext = szLog;
        pEnd = wcschr(szLog, L'\r');
        if (NULL == pEnd)
            pEnd = wcschr(szLog, L'\n');
        while (pEnd && *pEnd)
        {
            // Find beginning of next line
            pEnd[0] = 0;
            pEnd++;
            if ((pEnd[0] == L'\r') || (pEnd[0] == L'\n'))
                pEnd++;

            // Log output
            hr = StrAnsiAllocString(&szWrite, pNext, 0, CP_OEMCP);
            ExitOnFailure(hr, "failed to convert output to ANSI");
            WcaLog(LOGMSG_STANDARD, szWrite);

            // Next line
            pNext = pEnd;
            pEnd = wcschr(pNext, L'\r');
            if (NULL == pEnd)
                pEnd = wcschr(pNext, L'\n');
        }

        hr = StrAllocString(&szTemp, pNext, 0);
        ExitOnFailure(hr, "failed to allocate string");

        hr = StrAllocString(&szLog, szTemp, 0);
        ExitOnFailure(hr, "failed to allocate string");
    }

    // Print any text that didn't end with a new line
    if (szLog && *szLog)
    {
        hr = StrAnsiAllocString(&szWrite, szLog, 0, CP_OEMCP);
        ExitOnFailure(hr, "failed to convert output to ANSI");
        WcaLog(LOGMSG_VERBOSE, szWrite);
    }

LExit:
    if (NULL != pBuffer)
        MemFree(pBuffer);

    ReleaseNullStr(szLog);
    ReleaseNullStr(szTemp);
    ReleaseNullStr(szWrite);

    return hr;
}

HRESULT QuietExec(
    __in LPWSTR wzCommand,
    __in DWORD dwTimeout
    )
{
    HRESULT hr = S_OK;
    PROCESS_INFORMATION oProcInfo;
    STARTUPINFOW oStartInfo;
    DWORD dwExitCode = ERROR_SUCCESS;
    HANDLE hOutRead = INVALID_HANDLE_VALUE;
    HANDLE hOutWrite = INVALID_HANDLE_VALUE;
    HANDLE hErrWrite = INVALID_HANDLE_VALUE;
    HANDLE hInRead = INVALID_HANDLE_VALUE;
    HANDLE hInWrite = INVALID_HANDLE_VALUE;

    memset(&oProcInfo, 0, sizeof(oProcInfo));
    memset(&oStartInfo, 0, sizeof(oStartInfo));

    // Create output redirect pipes
    hr = CreatePipes(&hOutRead, &hOutWrite, &hErrWrite, &hInRead, &hInWrite);
    ExitOnFailure(hr, "failed to create output pipes");

    // Set up startup structure
    oStartInfo.cb = sizeof(STARTUPINFOW);
    oStartInfo.dwFlags = STARTF_USESTDHANDLES;
    oStartInfo.hStdInput = hInRead;
    oStartInfo.hStdOutput = hOutWrite;
    oStartInfo.hStdError = hErrWrite;

    WcaLog(LOGMSG_VERBOSE, "%S", wzCommand);

    if (::CreateProcessW(NULL,
        wzCommand, // command line
        NULL, // security info
        NULL, // thread info
        TRUE, // inherit handles
        ::GetPriorityClass(::GetCurrentProcess()) | CREATE_NO_WINDOW, // creation flags
        NULL, // environment
        NULL, // cur dir
        &oStartInfo,
        &oProcInfo))
    {
        ::CloseHandle(oProcInfo.hThread);

        // Close child output/input handles so it doesn't hang
        ::CloseHandle(hOutWrite);
        ::CloseHandle(hErrWrite);
        ::CloseHandle(hInRead);

        // Log output
        LogOutput(hOutRead);

        // Wait for everything to finish
        ::WaitForSingleObject(oProcInfo.hProcess, dwTimeout);
        if (!::GetExitCodeProcess(oProcInfo.hProcess, &dwExitCode))
            dwExitCode = ERROR_SEM_IS_SET;
        ::CloseHandle(hOutRead);
        ::CloseHandle(hInWrite);
        ::CloseHandle(oProcInfo.hProcess);
    }
    else
        ExitOnLastError(hr, "Command failed to execute.");

    hr = HRESULT_FROM_WIN32(dwExitCode);
    ExitOnFailure(hr, "Command line returned an error.");

LExit:
    return hr;
}

