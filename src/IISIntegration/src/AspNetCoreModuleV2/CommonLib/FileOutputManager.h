// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "sttimer.h"
#include "HandleWrapper.h"
#include "StdWrapper.h"
#include "stringa.h"
#include "stringu.h"
#include "BaseOutputManager.h"

class FileOutputManager : public BaseOutputManager
{
    #define MAX_FILE_READ_SIZE 30000
public:
    FileOutputManager(std::wstring pwzApplicationPath, std::wstring pwzStdOutLogFileName);
    FileOutputManager(std::wstring pwzApplicationPath, std::wstring pwzStdOutLogFileName, bool fEnableNativeLogging);
    ~FileOutputManager();

    virtual std::wstring GetStdOutContent() override;
    void Start() override;
    void Stop() override;

private:
    HandleWrapper<InvalidHandleTraits> m_hLogFileHandle;
    std::wstring m_stdOutLogFileName;
    std::filesystem::path m_applicationPath;
    std::filesystem::path m_logFilePath;
};
