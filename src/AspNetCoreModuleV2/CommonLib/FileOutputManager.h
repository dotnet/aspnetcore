// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "sttimer.h"
#include "IOutputManager.h"
#include "HandleWrapper.h"
#include "StdWrapper.h"
#include "stringa.h"
#include "stringu.h"

class FileOutputManager : public IOutputManager
{
    #define FILE_FLUSH_TIMEOUT 3000
    #define MAX_FILE_READ_SIZE 30000
public:
    FileOutputManager();
    FileOutputManager(bool fEnableNativeLogging);
    ~FileOutputManager();

    HRESULT
    Initialize(PCWSTR pwzStdOutLogFileName, PCWSTR pwzApplciationpath);

    virtual bool GetStdOutContent(STRA* struStdOutput) override;
    virtual HRESULT Start() override;
    virtual HRESULT Stop() override;

private:
    HandleWrapper<InvalidHandleTraits> m_hLogFileHandle;
    STTIMER m_Timer;
    STRU m_wsStdOutLogFileName;
    STRU m_wsApplicationPath;
    STRU m_struLogFilePath;
    STRA m_straFileContent;
    BOOL m_disposed;
    BOOL m_fEnableNativeRedirection;
    SRWLOCK m_srwLock{};
    std::unique_ptr<StdWrapper>    stdoutWrapper;
    std::unique_ptr<StdWrapper>    stderrWrapper;
};
