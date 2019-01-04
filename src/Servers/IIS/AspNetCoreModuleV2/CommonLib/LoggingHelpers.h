// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "BaseOutputManager.h"

class LoggingHelpers
{
    class Redirection: NonCopyable
    {
    public:

        Redirection(std::unique_ptr<BaseOutputManager> outputManager)
            : m_outputManager(std::move(outputManager))
        {
            m_outputManager->TryStartRedirection();
        }

        ~Redirection() noexcept(false)
        {
            m_outputManager->TryStopRedirection();
        }

    private:
        std::unique_ptr<BaseOutputManager> m_outputManager;
    };

public:

    static
    Redirection
    StartStdOutRedirection(
        RedirectionOutput& output
    );

    static std::shared_ptr<RedirectionOutput>
    CreateOutputs(
        bool enableLogging,
        std::wstring outputFileName,
        std::wstring applicationPath,
        std::shared_ptr<RedirectionOutput> stringStreamOutput
    );
};

