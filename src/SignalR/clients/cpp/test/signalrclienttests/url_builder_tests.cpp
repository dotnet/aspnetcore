// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "url_builder.h"

using namespace signalr;

TEST(url_builder_negotiate, url_correct_if_query_string_empty)
{
    ASSERT_EQ(
        _XPLATSTR("http://fake/negotiate"),
        url_builder::build_negotiate(_XPLATSTR("http://fake/")));
}

TEST(url_builder_negotiate, url_correct_if_query_string_not_empty)
{
    ASSERT_EQ(
        _XPLATSTR("http://fake/negotiate?q1=1&q2=2"),
        url_builder::build_negotiate(_XPLATSTR("http://fake/?q1=1&q2=2")));
}

TEST(url_builder_connect_webSockets, url_correct_if_query_string_not_empty)
{
    ASSERT_EQ(
        _XPLATSTR("ws://fake/?q1=1&q2=2"),
        url_builder::build_connect(_XPLATSTR("http://fake/"), transport_type::websockets, _XPLATSTR("q1=1&q2=2")));
}
