// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "url_builder.h"

using namespace signalr;

TEST(url_builder_negotiate, url_correct_if_query_string_empty)
{
    ASSERT_EQ(
        "http://fake/negotiate",
        url_builder::build_negotiate("http://fake/"));
}

TEST(url_builder_negotiate, url_correct_if_query_string_not_empty)
{
    ASSERT_EQ(
        "http://fake/negotiate?q1=1&q2=2",
        url_builder::build_negotiate("http://fake/?q1=1&q2=2"));
}

TEST(url_builder_connect_webSockets, url_correct_if_query_string_not_empty)
{
    ASSERT_EQ(
        "ws://fake/?q1=1&q2=2",
        url_builder::build_connect("http://fake/", transport_type::websockets, "q1=1&q2=2"));
}
