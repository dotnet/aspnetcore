// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <cstdio>

// Wraps stdout/stderr stream, modifying them to redirect to the given handle
class StdWrapper
{
public:
    StdWrapper(FILE* stdStream, DWORD stdHandleNumber, HANDLE handleToRedirectTo, BOOL fEnableNativeRedirection);
    ~StdWrapper();
    HRESULT StartRedirection();
    HRESULT StopRedirection() const;

private:
    int m_previousFileDescriptor;
    FILE* m_stdStream;
    DWORD m_stdHandleNumber;
    BOOL m_enableNativeRedirection;
    HANDLE m_handleToRedirectTo;
    FILE* m_redirectedFile;
};

