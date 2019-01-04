// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#pragma once

#include "SRWExclusiveLock.h"
#include "NonCopyable.h"
#include "exceptions.h"
#include <fstream>
#include <filesystem>

class RedirectionOutput
{
public:
    virtual ~RedirectionOutput() = default;
    virtual void Append(const std::wstring& text) = 0;
};

class AggregateRedirectionOutput: NonCopyable, public RedirectionOutput
{
public:
    AggregateRedirectionOutput(std::shared_ptr<RedirectionOutput> outputA, std::shared_ptr<RedirectionOutput> outputB, std::shared_ptr<RedirectionOutput> outputC)
        : m_outputA(std::move(outputA)), m_outputB(std::move(outputB)), m_outputC(std::move(outputC))
    {
    }

    void Append(const std::wstring& text) override
    {
        if (m_outputA != nullptr)
        {
            m_outputA->Append(text);
        }
        if (m_outputB != nullptr)
        {
            m_outputB->Append(text);
        }
        if (m_outputC != nullptr)
        {
            m_outputC->Append(text);
        }
    }

private:
    std::shared_ptr<RedirectionOutput> m_outputA;
    std::shared_ptr<RedirectionOutput> m_outputB;
    std::shared_ptr<RedirectionOutput> m_outputC;
};

class FileRedirectionOutput: NonCopyable, public RedirectionOutput
{
public:
    FileRedirectionOutput(const std::wstring& applicationPath, const std::wstring& fileName)
    {
        try
        {
            SYSTEMTIME systemTime;
            FILETIME processCreationTime;
            FILETIME dummyFileTime;

            // Concatenate the log file name and application path
            auto logPath = std::filesystem::path(applicationPath) / fileName;
            create_directories(logPath.parent_path());

            THROW_LAST_ERROR_IF(!GetProcessTimes(
                GetCurrentProcess(),
                &processCreationTime,
                &dummyFileTime,
                &dummyFileTime,
                &dummyFileTime));

            THROW_LAST_ERROR_IF(!FileTimeToSystemTime(&processCreationTime, &systemTime));

            m_fileName = format(L"%s_%d%02d%02d%02d%02d%02d_%d.log",
                logPath.c_str(),
                systemTime.wYear,
                systemTime.wMonth,
                systemTime.wDay,
                systemTime.wHour,
                systemTime.wMinute,
                systemTime.wSecond,
                GetCurrentProcessId());

            m_file.open(m_fileName, std::wofstream::out | std::wofstream::app);
        }
        catch (...)
        {
            OBSERVE_CAUGHT_EXCEPTION();
        }
    }

    void Append(const std::wstring& text)
    {
        m_file << text;
    }

    ~FileRedirectionOutput()
    {
        m_file.close();
        std::error_code ec;
        if (std::filesystem::file_size(m_fileName, ec) == 0 && SUCCEEDED_LOG(ec))
        {
            std::filesystem::remove(m_fileName, ec) && LOG_IF_FAILED(ec);
        }
    }

private:
    std::wstring m_fileName;
    std::wofstream m_file;
};

class StandardOutputRedirectionOutput: NonCopyable, public RedirectionOutput
{
public:
    StandardOutputRedirectionOutput(): m_handle(GetStdHandle(STD_OUTPUT_HANDLE))
    {
    }

    void Append(const std::wstring& text) override
    {
        DWORD nBytesWritten = 0;
        auto const codePage = GetConsoleOutputCP();
        auto const encodedByteCount = WideCharToMultiByte(codePage, 0, text.data(), -1, nullptr, 0, nullptr, nullptr);
        auto encodedBytes = std::shared_ptr<CHAR[]>(new CHAR[encodedByteCount]);
        WideCharToMultiByte(codePage, 0, text.data(), -1, encodedBytes.get(), encodedByteCount, nullptr, nullptr);
        WriteFile(m_handle, encodedBytes.get(), encodedByteCount - 1, &nBytesWritten, nullptr);
    }

private:
    HANDLE m_handle;
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


