// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "stdafx.h"
#include "SRWExclusiveLock.h"
#include "NonCopyable.h"

class RedirectionOutput
{
public:
    virtual void Append(const std::wstring& text) = 0;
};

class ForwardingRedirectionOutput: NonCopyable, public RedirectionOutput
{
public:
    ForwardingRedirectionOutput(RedirectionOutput ** target)
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

    RedirectionOutput** m_target;
};

class StringStreamRedirectionOutput: public RedirectionOutput
{
public:
    StringStreamRedirectionOutput()
    {
        InitializeSRWLock(&m_srwLock);
    }

    void Append(const std::wstring& text) override
    {
        SRWExclusiveLock lock(m_srwLock);
        m_output << text;
    }

    std::wstring GetOutput() const
    {
        return m_output.str();
    }

private:
    std::wstringstream m_output;
    SRWLOCK m_srwLock{};
};


