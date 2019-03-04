// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "test_utils.h"
#include "cpprest/asyncrt_utils.h"
#include "trace_log_writer.h"
#include "logger.h"
#include "memory_log_writer.h"

using namespace signalr;
TEST(logger_write, entry_added_if_trace_level_set)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    logger l(writer, trace_level::messages);
    l.log(trace_level::messages, "message");

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_EQ(1U, log_entries.size());
}

TEST(logger_write, entry_not_added_if_trace_level_not_set)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    logger l(writer, trace_level::messages);
    l.log(trace_level::events, "event");

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_TRUE(log_entries.empty());
}

TEST(logger_write, entries_added_for_combined_trace_level)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    logger l(writer, trace_level::messages | trace_level::state_changes | trace_level::events | trace_level::errors | trace_level::info);
    l.log(trace_level::messages, "message");
    l.log(trace_level::events, "event");
    l.log(trace_level::state_changes, "state_change");
    l.log(trace_level::errors, "error");
    l.log(trace_level::info, "info");

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();

    ASSERT_EQ(5U, log_entries.size());
}

TEST(logger_write, entries_formatted_correctly)
{
    std::shared_ptr<log_writer> writer(std::make_shared<memory_log_writer>());

    logger l(writer, trace_level::all);
    l.log(trace_level::messages, "message");

    auto log_entries = std::dynamic_pointer_cast<memory_log_writer>(writer)->get_log_entries();
    ASSERT_FALSE(log_entries.empty());

    auto entry = log_entries[0];

    auto date_str = entry.substr(0, entry.find_first_of("Z") + 1);
    auto date = utility::datetime::from_string(utility::conversions::to_string_t(date_str), utility::datetime::ISO_8601);
    ASSERT_EQ(date_str, utility::conversions::to_utf8string(date.to_string(utility::datetime::ISO_8601)));

    ASSERT_EQ("[message     ] message\n", remove_date_from_log_entry(entry));
}
