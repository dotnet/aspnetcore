// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "RedirectionOutput.h"
#include <filesystem>
#include "exceptions.h"
#include "EventLog.h"
#include <chrono>
#include <time.h>
#include <cwchar>

std::wstring GetDateTime()
{
    std::chrono::milliseconds milliseconds =
        std::chrono::duration_cast<std::chrono::milliseconds>(std::chrono::system_clock::now().time_since_epoch());
    std::chrono::seconds seconds = std::chrono::duration_cast<std::chrono::seconds>(milliseconds);
    milliseconds = std::chrono::duration_cast<std::chrono::milliseconds>(milliseconds - seconds);

    time_t t = seconds.count();
    tm time;
    // convert time to utc
    gmtime_s(&time, &t);

    wchar_t timeString[sizeof("2019-11-23T13:23:02.000Z")];

    // format string to ISO8601 with additional space for 3 digits of millisecond precision
    std::wcsftime(timeString, sizeof(timeString) / sizeof(wchar_t), L"%FT%T.000Z", &time);

    // add millisecond part
    // 5 = 3 digits of millisecond precision + 'Z' + null character ending
    swprintf(timeString + (sizeof(timeString) / sizeof(wchar_t)) - 5, 5, L"%03dZ", (int)milliseconds.count());

    return std::wstring(timeString);
}

AggregateRedirectionOutput::AggregateRedirectionOutput(std::shared_ptr<RedirectionOutput> outputA, std::shared_ptr<RedirectionOutput> outputB, std::shared_ptr<RedirectionOutput> outputC) noexcept(true):
    m_outputA(std::move(outputA)), m_outputB(std::move(outputB)), m_outputC(std::move(outputC))
{
}

void AggregateRedirectionOutput::Append(const std::wstring& text)
{
    std::wstring out = GetDateTime() + L": " + text;
    if (m_outputA != nullptr)
    {
        m_outputA->Append(out);
    }
    if (m_outputB != nullptr)
    {
        m_outputB->Append(out);
    }
    if (m_outputC != nullptr)
    {
        m_outputC->Append(out);
    }
}

FileRedirectionOutput::FileRedirectionOutput(const std::wstring& applicationPath, const std::wstring& fileName)
{
    try
    {
        SYSTEMTIME systemTime{};
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

        m_file.exceptions(std::ifstream::failbit);
        m_file.open(m_fileName, std::wofstream::out | std::wofstream::app);
    }
    catch (...)
    {
        OBSERVE_CAUGHT_EXCEPTION();
        EventLog::Warn(
            ASPNETCORE_EVENT_GENERAL_WARNING,
            L"Could not start stdout file redirection to '%s' with application base '%s'. %s.",
            fileName.c_str(),
            applicationPath.c_str(),
            CaughtExceptionToString().c_str());
    }
}

void FileRedirectionOutput::Append(const std::wstring& text)
{
    if (m_file.is_open())
    {
        auto multiByte = to_multi_byte_string(text, CP_UTF8);

        // Writing \r\n to an ostream will cause two new lines to be written rather
        // than one. Change all \r\n to \n.
        std::string slashRslashN = "\r\n";
        std::string slashN = "\n";
        size_t start_pos = 0;
        while ((start_pos = multiByte.find(slashRslashN, start_pos)) != std::string::npos) {
            multiByte.replace(start_pos, slashRslashN.length(), slashN);
            start_pos += slashN.length();
        }

        m_file << multiByte;
    }
}

FileRedirectionOutput::~FileRedirectionOutput()
{
    if (m_file.is_open())
    {
        m_file.close();
        std::error_code ec;
        if (std::filesystem::file_size(m_fileName, ec) == 0 && SUCCEEDED_LOG(ec))
        {
            std::filesystem::remove(m_fileName, ec);
            LOG_IF_FAILED(ec);
        }
    }
}

StandardOutputRedirectionOutput::StandardOutputRedirectionOutput(): m_handle(GetStdHandle(STD_OUTPUT_HANDLE))
{
    HANDLE stdOutHandle;
    DuplicateHandle(
      /* hSourceProcessHandle*/ GetCurrentProcess(),
      /* hSourceHandle */ GetStdHandle(STD_OUTPUT_HANDLE),
      /* hTargetProcessHandle */ GetCurrentProcess(),
      /* lpTargetHandle */&stdOutHandle,
      /* dwDesiredAccess */ 0, // dwDesired is ignored if DUPLICATE_SAME_ACCESS is specified
      /* bInheritHandle */ FALSE,
      /* dwOptions  */ DUPLICATE_SAME_ACCESS);
    m_handle = stdOutHandle;
}

void StandardOutputRedirectionOutput::Append(const std::wstring& text)
{
    DWORD nBytesWritten = 0;
    auto encodedBytes = to_multi_byte_string(text, GetConsoleOutputCP());
    WriteFile(m_handle, encodedBytes.data(), static_cast<DWORD>(encodedBytes.size()), &nBytesWritten, nullptr);
}

StringStreamRedirectionOutput::StringStreamRedirectionOutput()
{
    InitializeSRWLock(&m_srwLock);
}

void StringStreamRedirectionOutput::Append(const std::wstring& text)
{
    SRWExclusiveLock lock(m_srwLock);
    auto const writeSize = min(m_charactersLeft, text.size());
    m_output.write(text.c_str(), writeSize);
    m_charactersLeft -= writeSize;
}
