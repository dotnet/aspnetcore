// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include <string>
#include "ConfigurationSource.h"
#include "exceptions.h"

enum APP_HOSTING_MODEL
{
    HOSTING_UNKNOWN = 0,
    HOSTING_IN_PROCESS,
    HOSTING_OUT_PROCESS
};

class ShimOptions: NonCopyable
{
public:
    const std::wstring&
    QueryProcessPath() const
    {
        return m_strProcessPath;
    }

    const std::wstring&
    QueryArguments() const
    {
        return m_strArguments;
    }

    APP_HOSTING_MODEL
    QueryHostingModel() const
    {
        return m_hostingModel;
    }

    const std::wstring&
    QueryHandlerVersion() const
    {
        return m_strHandlerVersion;
    }

    BOOL
    QueryStdoutLogEnabled() const
    {
        return m_fStdoutLogEnabled;
    }

    const std::wstring&
    QueryStdoutLogFile() const
    {
        return m_struStdoutLogFile;
    }

    ShimOptions(const ConfigurationSource &configurationSource);

private:
    std::wstring                   m_strArguments;
    std::wstring                   m_strProcessPath;
    APP_HOSTING_MODEL              m_hostingModel;
    std::wstring                   m_strHandlerVersion;
    std::wstring                   m_struStdoutLogFile;
    bool                           m_fStdoutLogEnabled;
};
