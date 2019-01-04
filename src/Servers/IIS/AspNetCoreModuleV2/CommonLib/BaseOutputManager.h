// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once
#include "StdWrapper.h"
#include "EventLog.h"
#include "exceptions.h"
#include "StringHelpers.h"
#include "HostFxr.h"
#include "debugutil.h"
#include "RedirectionOutput.h"

class BaseOutputManager
{
public:
    BaseOutputManager(RedirectionOutput& output, bool enableNativeLogging) :
        m_disposed(false),
        m_output(output),
        stdoutWrapper(nullptr),
        stderrWrapper(nullptr),
        m_enableNativeRedirection(enableNativeLogging)
    {
        InitializeSRWLock(&m_srwLock);
    }

    ~BaseOutputManager()
    {
    }

    void
    TryStartRedirection()
    {
        const auto startLambda = [&]() { this->Start(); };
        TryOperation(startLambda, L"Could not start stdout redirection in %s. Exception message: %s.");
    }

    void
    TryStopRedirection()
    {
        const auto stopLambda = [&]() { this->Stop(); };
        TryOperation(stopLambda, L"Could not stop stdout redirection in %s. Exception message: %s.");
    }

protected:

    virtual
    void
    Start() = 0;

    virtual
    void
    Stop() = 0;

    bool m_disposed;
    bool m_enableNativeRedirection;
    SRWLOCK m_srwLock{};
    std::unique_ptr<StdWrapper> stdoutWrapper;
    std::unique_ptr<StdWrapper> stderrWrapper;
    RedirectionOutput& m_output;

    template<typename Functor>
    static
    void
    TryOperation(Functor func,
        std::wstring exceptionMessage)
    {
        try
        {
            func();
        }
        catch (const std::runtime_error& exception)
        {
            EventLog::Warn(ASPNETCORE_EVENT_GENERAL_WARNING, exceptionMessage.c_str(), GetModuleName().c_str(), to_wide_string(exception.what(), GetConsoleOutputCP()).c_str());
        }
        catch (...)
        {
            OBSERVE_CAUGHT_EXCEPTION();
        }
    }

private:
    std::wstring m_stdOutContent;
};

