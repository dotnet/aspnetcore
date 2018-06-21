// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stringa.h"
#include "stringu.h"

class APP_OFFLINE_HTM
{
public:
    APP_OFFLINE_HTM(LPCWSTR pszPath) : m_cRefs(1)
    {
        m_Path.Copy(pszPath);
    }

    VOID
    ReferenceAppOfflineHtm() const
    {
        InterlockedIncrement(&m_cRefs);
    }

    VOID
    DereferenceAppOfflineHtm() const
    {
        if (InterlockedDecrement(&m_cRefs) == 0)
        {
            delete this;
        }
    }

    BOOL
    Load(
        VOID
    )
    {
        BOOL            fResult = TRUE;
        LARGE_INTEGER   li = { 0 };
        CHAR           *pszBuff = NULL;
        HANDLE         handle = INVALID_HANDLE_VALUE;

        handle = CreateFile(m_Path.QueryStr(),
            GENERIC_READ,
            FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE,
            NULL,
            OPEN_EXISTING,
            FILE_ATTRIBUTE_NORMAL,
            NULL);

        if (handle == INVALID_HANDLE_VALUE)
        {
            if (HRESULT_FROM_WIN32(GetLastError()) == ERROR_FILE_NOT_FOUND)
            {
                fResult = FALSE;
            }

            // This Load() member function is supposed be called only when the change notification event of file creation or file modification happens.
            // If file is currenlty locked exclusively by other processes, we might get INVALID_HANDLE_VALUE even though the file exists. In that case, we should return TRUE here.
            goto Finished;
        }

        if (!GetFileSizeEx(handle, &li))
        {
            goto Finished;
        }

        if (li.HighPart != 0)
        {
            // > 4gb file size not supported
            // todo: log a warning at event log
            goto Finished;
        }

        DWORD bytesRead = 0;

        if (li.LowPart > 0)
        {
            pszBuff = new CHAR[li.LowPart + 1];

            if (ReadFile(handle, pszBuff, li.LowPart, &bytesRead, NULL))
            {
                m_Contents.Copy(pszBuff, bytesRead);
            }
        }

    Finished:
        if (handle != INVALID_HANDLE_VALUE)
        {
            CloseHandle(handle);
            handle = INVALID_HANDLE_VALUE;
        }

        if (pszBuff != NULL)
        {
            delete[] pszBuff;
            pszBuff = NULL;
        }

        return fResult;
    }

    mutable LONG        m_cRefs;
    STRA                m_Contents;
    STRU                m_Path;
};
