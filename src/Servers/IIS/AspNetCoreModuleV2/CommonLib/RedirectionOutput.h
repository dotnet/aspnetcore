// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "SRWExclusiveLock.h"
#include "NonCopyable.h"
#include "HandleWrapper.h"
#include <fstream>

class RedirectionOutput
{
public:
    virtual ~RedirectionOutput() = default;
    virtual void Append(const std::wstring& text) = 0;
};

class AggregateRedirectionOutput: NonCopyable, public RedirectionOutput
{
public:
    AggregateRedirectionOutput(std::shared_ptr<RedirectionOutput> outputA, std::shared_ptr<RedirectionOutput> outputB, std::shared_ptr<RedirectionOutput> outputC) noexcept(true);

    void Append(const std::wstring& text) override;

private:
    std::shared_ptr<RedirectionOutput> m_outputA;
    std::shared_ptr<RedirectionOutput> m_outputB;
    std::shared_ptr<RedirectionOutput> m_outputC;
};

class FileRedirectionOutput: NonCopyable, public RedirectionOutput
{
public:
    FileRedirectionOutput(const std::wstring& applicationPath, const std::wstring& fileName);

    void Append(const std::wstring& text) override;

    ~FileRedirectionOutput() override;

private:
    std::wstring m_fileName;
    std::ofstream m_file;
};

class StandardOutputRedirectionOutput: NonCopyable, public RedirectionOutput
{
public:
    StandardOutputRedirectionOutput();

    void Append(const std::wstring& text) override;

private:
    HandleWrapper<InvalidHandleTraits> m_handle;
};

class ForwardingRedirectionOutput: NonCopyable, public RedirectionOutput
{
public:
    ForwardingRedirectionOutput(RedirectionOutput ** target) noexcept
        : m_target(target)
    {
    }

    void Append(const std::wstring& text) override
    {
        auto const target = *m_target;
        if (target)
        {
            target->Append(text);
        }
    }

private:
    RedirectionOutput** m_target;
};

class StringStreamRedirectionOutput: NonCopyable, public RedirectionOutput
{
public:
    StringStreamRedirectionOutput();

    void Append(const std::wstring& text) override;

    std::wstring GetOutput() const
    {
        return m_output.str();
    }

private:
    // Logs collected by this output are mostly used for Event Log messages where size limit is 32K
    std::size_t m_charactersLeft = 30000;
    std::wstringstream m_output;
    SRWLOCK m_srwLock{};
};


