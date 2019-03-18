cmake_minimum_required (VERSION 2.8.11)
project (signalrclient)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -fPIC")
#set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -fPIC -lcrypto -lssl -L/usr/local/Cellar/openssl/1.0.2q/lib")
set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} ${CMAKE_LDFLAGS}")

set(CPPREST_INCLUDE_DIR "" CACHE FILEPATH "Path to casablanca include dir")

include_directories (
include
"${CPPREST_INCLUDE_DIR}")

find_library(CPPREST_SO NAMES "cpprest" PATHS ${CPPREST_LIB_DIR} REQUIRED)

if(NOT WIN32)
  if(APPLE)
    file(GLOB OPENSSL_ROOT_DIR /usr/local/Cellar/openssl/*)
    list(REVERSE OPENSSL_ROOT_DIR)
    set(CMAKE_REQUIRED_INCLUDES "${OPENSSL_INCLUDE_DIR}")
    set(CMAKE_REQUIRED_LIBRARIES "${OPENSSL_LIBRARIES}")
  endif()

  find_package(OpenSSL 1.0.0 REQUIRED)
  find_package(Boost REQUIRED COMPONENTS system thread chrono)

  include_directories ("${OPENSSL_INCLUDE_DIR}")
endif()

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/bin)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/bin)

add_subdirectory(src/signalrclient)
add_subdirectory(test)
