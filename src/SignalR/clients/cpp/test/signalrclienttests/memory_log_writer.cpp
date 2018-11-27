// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in t

#include "stdafx.h"
#include "memory_log_writer.h"

void memory_log_writer::write(const utility::string_t& entry)
{
    std::lock_guard<std::mutex> lock(m_entries_lock);
    m_log_entries.push_back(entry);
}

std::vector<utility::string_t> memory_log_writer::get_log_entries()
{
    std::lock_guard<std::mutex> lock(m_entries_lock);
    return m_log_entries;
}