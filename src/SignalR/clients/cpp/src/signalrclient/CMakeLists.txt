

set (SOURCES
  callback_manager.cpp
  connection.cpp
  connection_impl.cpp
  default_http_client.cpp
  default_websocket_client.cpp
  http_sender.cpp
  hub_connection.cpp
  hub_connection_impl.cpp
  logger.cpp
  negotiate.cpp
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

if(NOT WIN32)
  target_link_libraries(signalrclient ${CPPREST_LIB} ${OPENSSL_LIBRARIES} Boost::boost Boost::system Boost::chrono Boost::thread)
else()
  target_link_libraries(signalrclient ${CPPREST_LIB})
endif()

add_custom_command(TARGET signalrclient POST_BUILD COMMAND ${CMAKE_COMMAND} -E copy_directory ${CMAKE_SOURCE_DIR}/include $<TARGET_FILE_DIR:signalrclient>/include)
