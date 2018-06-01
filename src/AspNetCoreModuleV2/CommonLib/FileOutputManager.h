// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once


class FileOutputManager : public IOutputManager
{
    #define FILE_FLUSH_TIMEOUT 3000
public:
    FileOutputManager();
    ~FileOutputManager();

    HRESULT
    Initialize(PCWSTR pwzStdOutLogFileName, PCWSTR pwzApplciationpath);

    virtual bool GetStdOutContent(STRA* struStdOutput) override;
    virtual HRESULT Start() override;
    virtual void NotifyStartupComplete() override;

private:
    HANDLE m_hLogFileHandle;
    STTIMER m_Timer;
    STRU m_wsStdOutLogFileName;
    STRU m_wsApplicationPath;
    STRU m_struLogFilePath;
    int m_fdStdOut;
    int m_fdStdErr;
    FILE* m_pStdOutFile;
};

