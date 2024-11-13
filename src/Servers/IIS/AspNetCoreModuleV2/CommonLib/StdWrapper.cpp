// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "stdafx.h"
#include "StdWrapper.h"
#include "exceptions.h"
#include "LoggingHelpers.h"
#include <corecrt_io.h>
#include <fcntl.h>

StdWrapper::StdWrapper(FILE* stdStream, DWORD stdHandleNumber, HANDLE handleToRedirectTo, BOOL fEnableNativeRedirection)
    : m_previousFileDescriptor(0),
    m_stdStream(stdStream),
    m_stdHandleNumber(stdHandleNumber),
    m_enableNativeRedirection(fEnableNativeRedirection),
    m_handleToRedirectTo(handleToRedirectTo),
    m_redirectedFile(nullptr)
{
}

StdWrapper::~StdWrapper() = default;

// Redirects stdout/stderr to the provided handle.
// Example:
// If the handleToRedirecTo = 0x24
// Before:
// _fileno(stdout) = 1
// GetStdHandle(STD_OUTPUT_HANDLE) = 0x20
// After:
// _fileno(stdout) = 3
// GetStdHandle(STD_OUTPUT_HANDLE) = 0x28 <- Duplicated from 0x24
HRESULT
StdWrapper::StartRedirection()
{
    HANDLE stdHandle = nullptr;

    // In IIS, stdout and stderr are set to null as w3wp is created with DETACHED_PROCESS,
    // so fileno(m_stdStream) returns -2.
    // Open a null file such that undoing the redirection succeeds and _dup2 works.
    // m_previousFileDescriptor will be used for restoring stdout/stderr
    if (_fileno(m_stdStream) == -2)
    {
        freopen_s((FILE**)&m_stdStream, "nul", "w", m_stdStream);
        m_previousFileDescriptor = _dup(_fileno(m_stdStream));
    }
    else
    {
        m_previousFileDescriptor = _dup(_fileno(m_stdStream));
    }

    if (!m_enableNativeRedirection)
    {
        RETURN_LAST_ERROR_IF(!SetStdHandle(m_stdHandleNumber, m_handleToRedirectTo));

        return S_OK;
    }
    // After setting the std handle, we need to set stdout/stderr to the current
    // output/error handle.

    // Duplicate the handle before opening the handle and associating a file pointer.
    // If we don't by calling close on the file, the same handle value will be closed
    // multiple times.
    // Note, by calling duplicate handle, the new handle returned will have a different value
    // than the original, but point to the same underlying file object.
    RETURN_LAST_ERROR_IF(!DuplicateHandle(
        /* hSourceProcessHandle*/ GetCurrentProcess(),
        /* hSourceHandle */ m_handleToRedirectTo,
        /* hTargetProcessHandle */ GetCurrentProcess(),
        /* lpTargetHandle */&stdHandle,
        /* dwDesiredAccess */ 0, // dwDesired is ignored if DUPLICATE_SAME_ACCESS is specified
        /* bInheritHandle */ TRUE,
        /* dwOptions  */ DUPLICATE_SAME_ACCESS));

    RETURN_LAST_ERROR_IF(!SetStdHandle(m_stdHandleNumber, stdHandle));

    // _open_osfhandle will associate a filedescriptor with the handle.
    const auto fileDescriptor = _open_osfhandle(reinterpret_cast<intptr_t>(stdHandle), _O_WTEXT | _O_TEXT);

    if (fileDescriptor == -1)
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    m_redirectedFile = _fdopen(fileDescriptor, "w");

    if (m_redirectedFile == nullptr)
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    // Set stdout/stderr to the newly created file.
    const auto dup2Result = _dup2(_fileno(m_redirectedFile), _fileno(m_stdStream));

    if (dup2Result != 0)
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    // Removes buffering from the output
    if (setvbuf(m_stdStream, nullptr, _IONBF, 0) != 0)
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    return S_OK;
}

// Redirects stdout/stderr back to the original stdout/stderr.
// Note, this will not restore the original handle values returned by GetStdHandle,
// rather a duplicated number. This is because the original handle value is invalid
// due to dup2 closing the file originally in stdout/stderr
HRESULT
StdWrapper::StopRedirection() const
{
    // After setting the std handle, we need to set stdout/stderr to the current
    // output/error handle.
    FILE * file = _fdopen(m_previousFileDescriptor, "w");
    if (file == nullptr)
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    RETURN_LAST_ERROR_IF(!SetStdHandle(m_stdHandleNumber, reinterpret_cast<HANDLE>(_get_osfhandle(m_previousFileDescriptor))));

    if (!m_enableNativeRedirection)
    {
        return S_OK;
    }

    // Set stdout/stderr to the newly created file output.
    const auto dup2Result = _dup2(_fileno(file), _fileno(m_stdStream));
    if (dup2Result != 0)
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    if (setvbuf(m_stdStream, nullptr, _IONBF, 0) != 0)
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    if (fclose(m_redirectedFile) != 0)
    {
        RETURN_HR(HRESULT_FROM_WIN32(ERROR_FILE_INVALID));
    }

    return S_OK;
}
