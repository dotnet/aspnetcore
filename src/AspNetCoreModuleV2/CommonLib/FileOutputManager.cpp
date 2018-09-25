// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "FileOutputManager.h"
#include "sttimer.h"
#include "exceptions.h"
#include "debugutil.h"
#include "SRWExclusiveLock.h"
#include "file_utility.h"
#include "StdWrapper.h"
#include "StringHelpers.h"

FileOutputManager::FileOutputManager(std::wstring pwzStdOutLogFileName, std::wstring  pwzApplicationPath) :
    FileOutputManager(pwzStdOutLogFileName, pwzApplicationPath, /* fEnableNativeLogging */ true) { }

FileOutputManager::FileOutputManager(std::wstring  pwzStdOutLogFileName, std::wstring  pwzApplicationPath, bool fEnableNativeLogging) :
    BaseOutputManager(fEnableNativeLogging),
    m_hLogFileHandle(INVALID_HANDLE_VALUE),
    m_applicationPath(pwzApplicationPath),
    m_stdOutLogFileName(pwzStdOutLogFileName)
{
    InitializeSRWLock(&m_srwLock);
}

FileOutputManager::~FileOutputManager()
{
    FileOutputManager::Stop();
}

// Start redirecting stdout and stderr into the file handle.
// Uses sttimer to continuously flush output into the file.
void
FileOutputManager::Start()
{
    SYSTEMTIME systemTime;
    SECURITY_ATTRIBUTES saAttr = { 0 };
    FILETIME processCreationTime;
    FILETIME dummyFileTime;
    
    // To make Console.* functions work, allocate a console
    // in the current process.
    if (!AllocConsole())
    {
        THROW_LAST_ERROR_IF(GetLastError() != ERROR_ACCESS_DENIED);
    }

    // Concatenate the log file name and application path
    auto logPath = m_applicationPath / m_stdOutLogFileName;
    create_directories(logPath.parent_path());

    THROW_LAST_ERROR_IF(!GetProcessTimes(
        GetCurrentProcess(), 
        &processCreationTime, 
        &dummyFileTime, 
        &dummyFileTime, 
        &dummyFileTime));

    THROW_LAST_ERROR_IF(!FileTimeToSystemTime(&processCreationTime, &systemTime));

    m_logFilePath = format(L"%s_%d%02d%02d%02d%02d%02d_%d.log",
        logPath.c_str(),
        systemTime.wYear,
        systemTime.wMonth,
        systemTime.wDay,
        systemTime.wHour,
        systemTime.wMinute,
        systemTime.wSecond,
        GetCurrentProcessId());

    saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
    saAttr.bInheritHandle = TRUE;
    saAttr.lpSecurityDescriptor = NULL;

    // Create the file with both READ and WRITE.
    m_hLogFileHandle = CreateFileW(m_logFilePath.c_str(),
        FILE_READ_DATA | FILE_WRITE_DATA,
        FILE_SHARE_READ,
        &saAttr,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        nullptr);

    THROW_LAST_ERROR_IF(m_hLogFileHandle == INVALID_HANDLE_VALUE);

    stdoutWrapper = std::make_unique<StdWrapper>(stdout, STD_OUTPUT_HANDLE, m_hLogFileHandle, m_enableNativeRedirection);
    stderrWrapper = std::make_unique<StdWrapper>(stderr, STD_ERROR_HANDLE, m_hLogFileHandle, m_enableNativeRedirection);

    stdoutWrapper->StartRedirection();
    stderrWrapper->StartRedirection();
}

void
FileOutputManager::Stop()
{
    CHAR            pzFileContents[MAX_FILE_READ_SIZE] = { 0 };
    DWORD           dwNumBytesRead;
    LARGE_INTEGER   li = { 0 };
    DWORD           dwFilePointer = 0;
    HANDLE   handle = NULL;
    WIN32_FIND_DATA fileData;

    if (m_disposed)
    {
        return;
    }

    SRWExclusiveLock lock(m_srwLock);

    if (m_disposed)
    {
        return;
    }

    m_disposed = true;

    if (m_hLogFileHandle == INVALID_HANDLE_VALUE)
    {
        THROW_HR(HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND));
    }

    FlushFileBuffers(m_hLogFileHandle);

    if (stdoutWrapper != nullptr)
    {
        THROW_IF_FAILED(stdoutWrapper->StopRedirection());
    }

    if (stderrWrapper != nullptr)
    {
        THROW_IF_FAILED(stderrWrapper->StopRedirection());
    }

    // delete empty log file
    handle = FindFirstFile(m_logFilePath.c_str(), &fileData);
    if (handle != INVALID_HANDLE_VALUE &&
        handle != NULL &&
        fileData.nFileSizeHigh == 0 &&
        fileData.nFileSizeLow == 0) // skip check of nFileSizeHigh
    {
        FindClose(handle);
        LOG_LAST_ERROR_IF(!DeleteFile(m_logFilePath.c_str()));
        return;
    }

    // Read the first 30Kb from the file and store it in a buffer.
    // By doing this, we can close the handle to the file and be done with it.
    THROW_LAST_ERROR_IF(!GetFileSizeEx(m_hLogFileHandle, &li));

    if (li.HighPart > 0)
    {
        THROW_HR(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    dwFilePointer = SetFilePointer(m_hLogFileHandle, 0, NULL, FILE_BEGIN);

    THROW_LAST_ERROR_IF(dwFilePointer == INVALID_SET_FILE_POINTER);

    THROW_LAST_ERROR_IF(!ReadFile(m_hLogFileHandle, pzFileContents, MAX_FILE_READ_SIZE, &dwNumBytesRead, NULL));

    m_stdOutContent = to_wide_string(std::string(pzFileContents, dwNumBytesRead), GetConsoleOutputCP());

    auto content = GetStdOutContent();
    if (!content.empty())
    {
        // printf will fail in in full IIS
        if (wprintf(content.c_str()) != -1)
        {
            // Need to flush contents for the new stdout and stderr
            _flushall();
        }
    }
}

std::wstring FileOutputManager::GetStdOutContent()
{
    return m_stdOutContent;
}
