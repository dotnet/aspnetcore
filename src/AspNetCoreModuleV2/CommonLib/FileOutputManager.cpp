// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "sttimer.h"

FileOutputManager::FileOutputManager()
{
}

FileOutputManager::~FileOutputManager()
{
    HANDLE   handle = NULL;
    WIN32_FIND_DATA fileData;

    if (m_hLogFileHandle != INVALID_HANDLE_VALUE)
    {
        m_Timer.CancelTimer();
        CloseHandle(m_hLogFileHandle);
        m_hLogFileHandle = INVALID_HANDLE_VALUE;
    }

    // delete empty log file
    handle = FindFirstFile(m_struLogFilePath.QueryStr(), &fileData);
    if (handle != INVALID_HANDLE_VALUE &&
        handle != NULL &&
        fileData.nFileSizeHigh == 0 &&
        fileData.nFileSizeLow == 0) // skip check of nFileSizeHigh
    {
        FindClose(handle);
        // no need to check whether the deletion succeeds
        // as nothing can be done
        DeleteFile(m_struLogFilePath.QueryStr());
    }

    if (m_fdPreviousStdOut >= 0)
    {
        _dup2(m_fdPreviousStdOut, _fileno(stdout));
    }

    if (m_fdPreviousStdErr >= 0)
    {
        _dup2(m_fdPreviousStdErr, _fileno(stderr));
    }
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
        return hr;
    }

    hr = UTILITY::EnsureDirectoryPathExist(struPath.QueryStr());
    if (FAILED(hr))
    {
        return hr;
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
        return hr;
    }

    m_fdPreviousStdOut = _dup(_fileno(stdout));
    m_fdPreviousStdErr = _dup(_fileno(stderr));

    m_hLogFileHandle = CreateFileW(m_struLogFilePath.QueryStr(),
        FILE_READ_DATA | FILE_WRITE_DATA,
        FILE_SHARE_READ,
        &saAttr,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        NULL);

    if (m_hLogFileHandle == INVALID_HANDLE_VALUE)
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        return hr;
    }

    // There are a few options for redirecting stdout/stderr,
    // but there are issues with most of them.
    // AllocConsole() 
    // *stdout = *m_pStdFile; 
    // *stderr = *m_pStdFile;
    // Calling _dup2 on stderr fails on IIS. IIS sets stderr to -2
    // _dup2(_fileno(m_pStdFile), _fileno(stdout));
    // _dup2(_fileno(m_pStdFile), _fileno(stderr));
    // If we were okay setting stdout and stderr to separate files, we could use:
    // _wfreopen_s(&m_pStdFile, struLogFileName.QueryStr(), L"w+", stdout);
    // _wfreopen_s(&m_pStdFile, struLogFileName.QueryStr(), L"w+", stderr);
    // Calling SetStdHandle works for redirecting managed logs, however you cannot
    // capture native logs (including hostfxr failures).

    if (!SetStdHandle(STD_OUTPUT_HANDLE, m_hLogFileHandle))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        return hr;
    }

    if (!SetStdHandle(STD_ERROR_HANDLE, m_hLogFileHandle))
    {
        hr = HRESULT_FROM_WIN32(GetLastError());
        return hr;
    }


    // Periodically flush the log content to file
    m_Timer.InitializeTimer(STTIMER::TimerCallback, &m_struLogFilePath, 3000, 3000);

    return hr;
}
