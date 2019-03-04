// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#pragma once

#include <vector>
#include <mutex>
#include "signalrclient/log_writer.h"

using namespace signalr;

class memory_log_writer : public log_writer
{
public:
    void __cdecl write(const std::string &entry) override;
    std::vector<std::string> get_log_entries();

private:
    std::vector<std::string> m_log_entries;
    std::mutex m_entries_lock;
};