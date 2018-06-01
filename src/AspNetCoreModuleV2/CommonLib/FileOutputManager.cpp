// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"


FileOutputManager::FileOutputManager()
    : m_pStdOutFile(NULL)
{
}

FileOutputManager::~FileOutputManager()
{
    HANDLE   handle = NULL;
    WIN32_FIND_DATA fileData;

    if (m_pStdOutFile != NULL)
    {
        m_Timer.CancelTimer();
        _close(_fileno(m_pStdOutFile));
        m_pStdOutFile = NULL;
    }

    // delete empty log file
    handle = FindFirstFile(m_struLogFilePath.QueryStr(), &fileData);
    if (handle != INVALID_HANDLE_VALUE &&
        fileData.nFileSizeHigh == 0 &&
        fileData.nFileSizeLow == 0) // skip check of nFileSizeHigh
    {
        FindClose(handle);
        // no need to check whether the deletion succeeds
        // as nothing can be done
        DeleteFile(m_struLogFilePath.QueryStr());
    }

    _dup2(m_fdStdOut, _fileno(stdout));
    _dup2(m_fdStdErr, _fileno(stderr));
}

HRESULT
FileOutputManager::Initialize(PCWSTR pwzStdOutLogFileName, PCWSTR pwzApplicationPath)
{
    HRESULT hr = S_OK;

    if (SUCCEEDED(hr = m_wsApplicationPath.Copy(pwzApplicationPath)))
    {
        hr = m_wsStdOutLogFileName.Copy(pwzStdOutLogFileName);
    }

    return hr;
}

void FileOutputManager::NotifyStartupComplete()
{
}

bool FileOutputManager::GetStdOutContent(STRA* struStdOutput)
{
    //
    // Ungraceful shutdown, try to log an error message.
    // This will be a common place for errors as it means the hostfxr_main returned
    // or there was an exception.
    //
    CHAR            pzFileContents[4096] = { 0 };
    DWORD           dwNumBytesRead;
    LARGE_INTEGER   li = { 0 };
    BOOL            fLogged = FALSE;
    DWORD           dwFilePointer = 0;

    if (m_hLogFileHandle != INVALID_HANDLE_VALUE)
    {
        if (GetFileSizeEx(m_hLogFileHandle, &li) && li.LowPart > 0 && li.HighPart == 0)
        {
            if (li.LowPart > 4096)
            {
                dwFilePointer = SetFilePointer(m_hLogFileHandle, -4096, NULL, FILE_END);
            }
            else
            {
                dwFilePointer = SetFilePointer(m_hLogFileHandle, 0, NULL, FILE_BEGIN);
            }

            if (dwFilePointer != INVALID_SET_FILE_POINTER)
            {
                // Read file fails.
                if (ReadFile(m_hLogFileHandle, pzFileContents, 4096, &dwNumBytesRead, NULL))
                {
                    if (SUCCEEDED(struStdOutput->Copy(pzFileContents, dwNumBytesRead)))
                    {
                        fLogged = TRUE;
                    }
                }
            }
        }
    }

    return fLogged;
}

HRESULT
FileOutputManager::Start()
{
    HRESULT hr;
    SYSTEMTIME systemTime;
    SECURITY_ATTRIBUTES saAttr = { 0 };
    STRU struPath;

    hr = UTILITY::ConvertPathToFullPath(
        m_wsStdOutLogFileName.QueryStr(),
        m_wsApplicationPath.QueryStr(),
        &struPath);

    if (FAILED(hr))
    {
        goto Finished;
    }

    hr = UTILITY::EnsureDirectoryPathExist(struPath.QueryStr());
    if (FAILED(hr))
    {
        goto Finished;
    }

    m_fdStdOut = _dup(_fileno(stdout));
    if (m_fdStdOut == -1)
    {
        hr = E_HANDLE;
        goto Finished;
    }

    m_fdStdErr = _dup(_fileno(stderr));
    if (m_fdStdErr == -1)
    {
        hr = E_HANDLE;
        goto Finished;
    }

    GetSystemTime(&systemTime);
    hr = m_struLogFilePath.SafeSnwprintf(L"%s_%d%02d%02d%02d%02d%02d_%d.log",
        struPath.QueryStr(),
        systemTime.wYear,
        systemTime.wMonth,
        systemTime.wDay,
        systemTime.wHour,
        systemTime.wMinute,
        systemTime.wSecond,
        GetCurrentProcessId());
    if (FAILED(hr))
    {
        goto Finished;
    }
    saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
    saAttr.bInheritHandle = TRUE;
    saAttr.lpSecurityDescriptor = NULL;

    if (_wfreopen_s(&m_pStdOutFile, m_struLogFilePath.QueryStr(), L"w+", stdout) == 0)
    {
        setvbuf(m_pStdOutFile, NULL, _IONBF, 0);
        if (_dup2(_fileno(m_pStdOutFile), _fileno(stdout)) == -1)
        {
            hr = E_HANDLE;
            goto Finished;
        }
        if (_dup2(_fileno(m_pStdOutFile), _fileno(stderr)) == -1)
        {
            hr = E_HANDLE;
            goto Finished;
        }
    }

    m_hLogFileHandle = (HANDLE)_get_osfhandle(_fileno(m_pStdOutFile));
    if (m_hLogFileHandle == INVALID_HANDLE_VALUE)
    {
        hr = E_HANDLE;
        goto Finished;
    }

    // Periodically flush the log content to file
    m_Timer.InitializeTimer(STTIMER::TimerCallback, &m_struLogFilePath, FILE_FLUSH_TIMEOUT, FILE_FLUSH_TIMEOUT);

Finished:

    return hr;
}
