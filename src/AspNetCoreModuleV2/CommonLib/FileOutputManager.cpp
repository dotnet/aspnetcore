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

extern HINSTANCE    g_hModule;

FileOutputManager::FileOutputManager() :
    FileOutputManager(/* fEnableNativeLogging */ true) { }

FileOutputManager::FileOutputManager(bool fEnableNativeLogging) :
    m_hLogFileHandle(INVALID_HANDLE_VALUE),
    m_disposed(false),
    stdoutWrapper(nullptr),
    stderrWrapper(nullptr),
    m_fEnableNativeRedirection(fEnableNativeLogging)
{
    InitializeSRWLock(&m_srwLock);
}

FileOutputManager::~FileOutputManager()
{
    FileOutputManager::Stop();
}

HRESULT
FileOutputManager::Initialize(PCWSTR pwzStdOutLogFileName, PCWSTR pwzApplicationPath)
{
    RETURN_IF_FAILED(m_wsApplicationPath.Copy(pwzApplicationPath));
    RETURN_IF_FAILED(m_wsStdOutLogFileName.Copy(pwzStdOutLogFileName));

    return S_OK;
}

// Start redirecting stdout and stderr into the file handle.
// Uses sttimer to continuously flush output into the file.
HRESULT
FileOutputManager::Start()
{
    SYSTEMTIME systemTime;
    SECURITY_ATTRIBUTES saAttr = { 0 };
    STRU struPath;
    FILETIME processCreationTime;
    FILETIME dummyFileTime;

    // Concatenate the log file name and application path
    RETURN_IF_FAILED(FILE_UTILITY::ConvertPathToFullPath(
        m_wsStdOutLogFileName.QueryStr(),
        m_wsApplicationPath.QueryStr(),
        &struPath));

    RETURN_IF_FAILED(FILE_UTILITY::EnsureDirectoryPathExist(struPath.QueryStr()));


    // TODO fix string as it is incorrect
    RETURN_LAST_ERROR_IF(!GetProcessTimes(
        GetCurrentProcess(), 
        &processCreationTime, 
        &dummyFileTime, 
        &dummyFileTime, 
        &dummyFileTime));
    RETURN_LAST_ERROR_IF(!FileTimeToSystemTime(&processCreationTime, &systemTime));

    RETURN_IF_FAILED(
        m_struLogFilePath.SafeSnwprintf(L"%s_%d%02d%02d%02d%02d%02d_%d.log",
            struPath.QueryStr(),
            systemTime.wYear,
            systemTime.wMonth,
            systemTime.wDay,
            systemTime.wHour,
            systemTime.wMinute,
            systemTime.wSecond,
            GetCurrentProcessId()));

    saAttr.nLength = sizeof(SECURITY_ATTRIBUTES);
    saAttr.bInheritHandle = TRUE;
    saAttr.lpSecurityDescriptor = NULL;

    // Create the file with both READ and WRITE.
    m_hLogFileHandle = CreateFileW(m_struLogFilePath.QueryStr(),
        FILE_READ_DATA | FILE_WRITE_DATA,
        FILE_SHARE_READ,
        &saAttr,
        CREATE_ALWAYS,
        FILE_ATTRIBUTE_NORMAL,
        nullptr);

    RETURN_LAST_ERROR_IF(m_hLogFileHandle == INVALID_HANDLE_VALUE);

    stdoutWrapper = std::make_unique<StdWrapper>(stdout, STD_OUTPUT_HANDLE, m_hLogFileHandle, m_fEnableNativeRedirection);
    stderrWrapper = std::make_unique<StdWrapper>(stderr, STD_ERROR_HANDLE, m_hLogFileHandle, m_fEnableNativeRedirection);

    stdoutWrapper->StartRedirection();
    stderrWrapper->StartRedirection();

    return S_OK;
}


HRESULT
FileOutputManager::Stop()
{
    STRA     straStdOutput;
    CHAR            pzFileContents[MAX_FILE_READ_SIZE] = { 0 };
    DWORD           dwNumBytesRead;
    LARGE_INTEGER   li = { 0 };
    DWORD           dwFilePointer = 0;
    HANDLE   handle = NULL;
    WIN32_FIND_DATA fileData;

    if (m_disposed)
    {
        return S_OK;
    }

    SRWExclusiveLock lock(m_srwLock);

    if (m_disposed)
    {
        return S_OK;
    }

    m_disposed = true;

    if (m_hLogFileHandle == INVALID_HANDLE_VALUE)
    {
        return HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND);
    }

    FlushFileBuffers(m_hLogFileHandle);

    if (stdoutWrapper != nullptr)
    {
        RETURN_IF_FAILED(stdoutWrapper->StopRedirection());
    }

    if (stderrWrapper != nullptr)
    {
        RETURN_IF_FAILED(stderrWrapper->StopRedirection());
    }

    // delete empty log file
    handle = FindFirstFile(m_struLogFilePath.QueryStr(), &fileData);
    if (handle != INVALID_HANDLE_VALUE &&
        handle != NULL &&
        fileData.nFileSizeHigh == 0 &&
        fileData.nFileSizeLow == 0) // skip check of nFileSizeHigh
    {
        FindClose(handle);
        LOG_LAST_ERROR_IF(!DeleteFile(m_struLogFilePath.QueryStr()));
    }

    // Read the first 30Kb from the file and store it in a buffer.
    // By doing this, we can close the handle to the file and be done with it.
    RETURN_LAST_ERROR_IF(!GetFileSizeEx(m_hLogFileHandle, &li));

    if (li.LowPart == 0 || li.HighPart > 0)
    {
        RETURN_IF_FAILED(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    dwFilePointer = SetFilePointer(m_hLogFileHandle, 0, NULL, FILE_BEGIN);

    RETURN_LAST_ERROR_IF(dwFilePointer == INVALID_SET_FILE_POINTER);

    RETURN_LAST_ERROR_IF(!ReadFile(m_hLogFileHandle, pzFileContents, MAX_FILE_READ_SIZE, &dwNumBytesRead, NULL));

    m_straFileContent.Copy(pzFileContents, dwNumBytesRead);

    // printf will fail in in full IIS
    if (printf(m_straFileContent.QueryStr()) != -1)
    {
        // Need to flush contents for the new stdout and stderr
        _flushall();
    }

    return S_OK;
}

bool FileOutputManager::GetStdOutContent(STRA* struStdOutput)
{
    struStdOutput->Copy(m_straFileContent);
    return m_straFileContent.QueryCCH() > 0;
}
