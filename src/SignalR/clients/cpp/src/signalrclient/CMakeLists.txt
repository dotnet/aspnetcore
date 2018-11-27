

set (SOURCES
 callback_manager.cpp
 connection.cpp
 connection_impl.cpp
 default_websocket_client.cpp
 http_sender.cpp
 hub_connection.cpp
 hub_connection_impl.cpp
 hub_proxy.cpp
 internal_hub_proxy.cpp
 logger.cpp
 request_sender.cpp
 signalr_client_config.cpp
 stdafx.cpp
 trace_log_writer.cpp
 transport.cpp
 transport_factory.cpp
 url_builder.cpp
 web_request.cpp
 web_request_factory.cpp
 websocket_transport.cpp
)

add_library (signalrclient SHARED ${SOURCES})

target_link_libraries(signalrclient ${CPPREST_SO})
