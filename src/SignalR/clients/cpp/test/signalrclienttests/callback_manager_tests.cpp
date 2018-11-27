// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "callback_manager.h"

using namespace signalr;
using namespace web;

TEST(callback_manager_register_callback, register_returns_unique_callback_ids)
{
    callback_manager callback_mgr{ json::value::object() };
    auto callback_id1 = callback_mgr.register_callback([](const json::value&){});
    auto callback_id2 = callback_mgr.register_callback([](const web::json::value&){});

    ASSERT_NE(callback_id1, callback_id2);
}

TEST(callback_manager_invoke_callback, invoke_callback_invokes_and_removes_callback_if_remove_callback_true)
{
    callback_manager callback_mgr{ json::value::object() };

    utility::string_t callback_argument{_XPLATSTR("")};

    auto callback_id = callback_mgr.register_callback(
        [&callback_argument](const json::value& argument)
        {
            callback_argument = argument.serialize();
        });

    auto callback_found = callback_mgr.invoke_callback(callback_id, json::value::number(42), true);

    ASSERT_TRUE(callback_found);
    ASSERT_EQ(_XPLATSTR("42"), callback_argument);
    ASSERT_FALSE(callback_mgr.remove_callback(callback_id));
}

TEST(callback_manager_invoke_callback, invoke_callback_invokes_and_does_not_remove_callback_if_remove_callback_false)
{
    callback_manager callback_mgr{ json::value::object() };

    utility::string_t callback_argument{ _XPLATSTR("") };

    auto callback_id = callback_mgr.register_callback(
        [&callback_argument](const json::value& argument)
    {
        callback_argument = argument.serialize();
    });

    auto callback_found = callback_mgr.invoke_callback(callback_id, json::value::number(42), false);

    ASSERT_TRUE(callback_found);
    ASSERT_EQ(_XPLATSTR("42"), callback_argument);
    ASSERT_TRUE(callback_mgr.remove_callback(callback_id));
}

TEST(callback_manager_ivoke_callback, invoke_callback_returns_false_for_invalid_callback_id)
{
    callback_manager callback_mgr{ json::value::object() };
    auto callback_found = callback_mgr.invoke_callback(_XPLATSTR("42"), json::value::object(), true);

    ASSERT_FALSE(callback_found);
}

TEST(callback_manager_remove, remove_removes_callback_and_returns_true_for_valid_callback_id)
{
    auto callback_called = false;

    {
        callback_manager callback_mgr{ json::value::object() };

        auto callback_id = callback_mgr.register_callback(
            [&callback_called](const json::value&)
        {
            callback_called = true;
        });

        ASSERT_TRUE(callback_mgr.remove_callback(callback_id));
    }

    ASSERT_FALSE(callback_called);
}

TEST(callback_manager_remove, remove_returns_false_for_invalid_callback_id)
{
    callback_manager callback_mgr{ json::value::object() };
    ASSERT_FALSE(callback_mgr.remove_callback(_XPLATSTR("42")));
}

TEST(callback_manager_clear, clear_invokes_all_callbacks)
{
    callback_manager callback_mgr{ json::value::object() };
    auto invocation_count = 0;

    for (auto i = 0; i < 10; i++)
    {
        callback_mgr.register_callback(
            [&invocation_count](const json::value& argument)
        {
            invocation_count++;
            ASSERT_EQ(_XPLATSTR("42"), argument.serialize());
        });
    }

    callback_mgr.clear(json::value::number(42));

    ASSERT_EQ(10, invocation_count);
}

TEST(callback_manager_dtor, clear_invokes_all_callbacks)
{
    auto invocation_count = 0;
    bool parameter_correct = true;

    {
        callback_manager callback_mgr{ json::value::number(42) };
        for (auto i = 0; i < 10; i++)
        {
            callback_mgr.register_callback(
                [&invocation_count, &parameter_correct](const json::value& argument)
            {
                invocation_count++;
                parameter_correct &= argument.serialize() == _XPLATSTR("42");
            });
        }
    }

    ASSERT_EQ(10, invocation_count);
    ASSERT_TRUE(parameter_correct);
}
