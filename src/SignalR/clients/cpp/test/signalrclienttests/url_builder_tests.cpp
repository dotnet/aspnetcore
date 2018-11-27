// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

#include "stdafx.h"
#include "url_builder.h"

using namespace signalr;

//TEST(url_builder_negotiate, url_correct_if_query_string_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/negotiate")),
//        url_builder::build_negotiate(web::uri{ _XPLATSTR("http://fake/") }, _XPLATSTR("")));
//}
//
//TEST(url_builder_negotiate, url_correct_if_query_string_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/negotiate?q1=1&q2=2")),
//        url_builder::build_negotiate(web::uri{ _XPLATSTR("http://fake/signalr/") }, _XPLATSTR("q1=1&q2=2")));
//}
//
//TEST(url_builder_negotiate, url_correct_if_connection_data_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/negotiate?clientProtocol=1.4&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D")),
//        url_builder::build_negotiate(web::uri{ _XPLATSTR("http://fake/signalr/") }, _XPLATSTR("")));
//}
//
//TEST(url_builder_connect_webSockets, url_correct_if_query_string_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("ws://fake/?q1=1&q2=2")),
//        url_builder::build_connect(web::uri{ _XPLATSTR("http://fake/") }, transport_type::websockets, _XPLATSTR("q1=1&q2=2")));
//}
//
//TEST(url_builder_reconnect_longPolling, url_correct_if_query_string_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/reconnect?transport=longPolling&clientProtocol=1.4&connectionToken=connection%20token")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("")));
//}
//
//TEST(url_builder_reconnect_longPolling, url_correct_if_query_string_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/reconnect?transport=longPolling&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("q1=1&q2=2")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/reconnect?transport=longPolling&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("&q1=1&q2=2")));
//}
//
//TEST(url_builder_reconnect_webSockets, url_correct_if_query_string_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("ws://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection%20token")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("wss://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection%20token")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("https://fake/signalr/") }, transport_type::websockets, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("")));
//}
//
//TEST(url_builder_reconnect_webSockets, url_correct_if_query_string_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("ws://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("q1=1&q2=2")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("ws://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("&q1=1&q2=2")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("wss://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("https://fake/signalr/") }, transport_type::websockets, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("q1=1&q2=2")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("wss://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("https://fake/signalr/") }, transport_type::websockets, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("&q1=1&q2=2")));
//}
//
//TEST(url_builder_reconnect, url_correct_if_connection_data_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/reconnect?transport=longPolling&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("ws://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets, _XPLATSTR(""), _XPLATSTR(""), _XPLATSTR("")));
//}
//
//TEST(url_builder_reconnect, url_correct_if_last_message_id_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/reconnect?transport=longPolling&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D&messageId=L45T%20M355463_1D")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling,
//        _XPLATSTR("connection token"), _XPLATSTR("L45T M355463_1D"), _XPLATSTR(""), _XPLATSTR("")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("ws://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D&messageId=L45T%20M355463_1D")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets,
//        _XPLATSTR("connection token"), _XPLATSTR("L45T M355463_1D"), _XPLATSTR(""), _XPLATSTR("")));
//}
//
//TEST(url_builder_reconnect, url_correct_if_groups_token_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/reconnect?transport=longPolling&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D&groupsToken=G%207")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling,
//        _XPLATSTR("connection token"), _XPLATSTR(""), _XPLATSTR("G 7"), _XPLATSTR("")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("ws://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D&groupsToken=G%207")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets,
//        _XPLATSTR("connection token"), _XPLATSTR(""), _XPLATSTR("G 7"), _XPLATSTR("")));
//}
//
//TEST(url_builder_reconnect, query_string_added_after_message_id_and_groups_token)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/reconnect?transport=longPolling&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D&messageId=L45T_M355463_1D&groupsToken=G7&X")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling,
//        _XPLATSTR("connection token"), _XPLATSTR("L45T_M355463_1D"), _XPLATSTR("G7"), _XPLATSTR("X")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("ws://fake/signalr/reconnect?transport=webSockets&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D&messageId=L45T_M355463_1D&groupsToken=G7&X")),
//        url_builder::build_reconnect(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets,
//        _XPLATSTR("connection token"), _XPLATSTR("L45T_M355463_1D"), _XPLATSTR("G7"), _XPLATSTR("X")));
//}
//
//TEST(url_builder_start, url_correct_if_query_string_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/start?transport=longPolling&clientProtocol=1.4&connectionToken=connection%20token")),
//        url_builder::build_start(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling,
//            _XPLATSTR("connection token"), _XPLATSTR("")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/start?transport=webSockets&clientProtocol=1.4&connectionToken=connection%20token")),
//        url_builder::build_start(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets,
//            _XPLATSTR("connection token"), _XPLATSTR("")));
//}
//
//TEST(url_builder_start, url_correct_if_query_string_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/start?transport=longPolling&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_start(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling,
//            _XPLATSTR("connection-token"), _XPLATSTR("q1=1&q2=2")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/start?transport=webSockets&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_start(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets,
//            _XPLATSTR("connection-token"), _XPLATSTR("&q1=1&q2=2")));
//}
//
//TEST(url_builder_start, url_correct_if_connection_data_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/start?transport=longPolling&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D")),
//        url_builder::build_start(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling,
//        _XPLATSTR("connection token"), _XPLATSTR("")));
//}
//
//TEST(url_builder_abort, url_correct_if_query_string_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/abort?transport=longPolling&clientProtocol=1.4&connectionToken=connection%20token")),
//        url_builder::build_abort(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling,
//        _XPLATSTR("connection token"), _XPLATSTR("")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/abort?transport=webSockets&clientProtocol=1.4&connectionToken=connection%20token")),
//        url_builder::build_abort(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets,
//        _XPLATSTR("connection token"), _XPLATSTR("")));
//}
//
//TEST(url_builder_abort, url_correct_if_query_string_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/abort?transport=longPolling&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_abort(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling,
//            _XPLATSTR("connection-token"), _XPLATSTR("q1=1&q2=2")));
//
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/abort?transport=webSockets&clientProtocol=1.4&connectionToken=connection-token&q1=1&q2=2")),
//        url_builder::build_abort(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::websockets,
//            _XPLATSTR("connection-token"), _XPLATSTR("&q1=1&q2=2")));
//}
//
//TEST(url_builder_abort, url_correct_if_connection_data_not_empty)
//{
//    ASSERT_EQ(
//        web::uri(_XPLATSTR("http://fake/signalr/abort?transport=longPolling&clientProtocol=1.4&connectionToken=connection%20token&connectionData=%5B%7B%22Name%22:%22ChatHub%22%7D%5D")),
//        url_builder::build_abort(web::uri{ _XPLATSTR("http://fake/signalr/") }, transport_type::long_polling,
//        _XPLATSTR("connection token"), _XPLATSTR("")));
//}