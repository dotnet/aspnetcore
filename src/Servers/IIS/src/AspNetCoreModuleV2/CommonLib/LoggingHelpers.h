// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "BaseOutputManager.h"

class LoggingHelpers
{

    class Redirection: NonCopyable
    {
    public:

        Redirection(HostFxrErrorRedirector redirector, std::unique_ptr<BaseOutputManager> outputManager)
            : m_redirector(std::move(redirector)),
              m_outputManager(std::move(outputManager))
        {
        }

    private:
        HostFxrErrorRedirector m_redirector;
        std::unique_ptr<BaseOutputManager> m_outputManager;
    };

public:

    static
    Redirection
    StartRedirection(
        RedirectionOutput& output,
        HostFxr& hostFxr,
        const IHttpServer& server,
        bool enableLogging,
        std::wstring outputFileName,
        std::wstring applicationPath
    );

};

