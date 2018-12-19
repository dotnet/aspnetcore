// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "HandleWrapper.h"
#include "BaseOutputManager.h"

class FileOutputManager : public BaseOutputManager
{
    #define MAX_FILE_READ_SIZE 30000
public:
    FileOutputManager(RedirectionOutput& output, std::wstring pwzApplicationPath, std::wstring pwzStdOutLogFileName, bool fEnableNativeLogging);

    void Start() override;
    void Stop() override;

private:
    HandleWrapper<InvalidHandleTraits> m_hLogFileHandle;
    std::wstring m_stdOutLogFileName;
    std::filesystem::path m_applicationPath;
    std::filesystem::path m_logFilePath;
};
