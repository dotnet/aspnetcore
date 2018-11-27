// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "trace_log_writer.h"

namespace signalr
{
    void trace_log_writer::write(const utility::string_t &entry)
    {
#ifdef _WIN32
        // OutputDebugString is thread safe
        OutputDebugString(entry.c_str());
#else
        // Note: there is no data race for standard output streams in C++ 11 but the results
        // might be garbled when the method is called concurrently from multiple threads
#ifdef _UTF16_STRINGS
        std::wclog << entry;
#else
        std::clog << entry;
#endif  // _UTF16_STRINGS

#endif  // _MS_WINDOWS
    }
}
