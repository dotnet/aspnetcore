cmake_minimum_required (VERSION 2.8.11)
project (signalrclient)

set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -std=c++11 -fPIC -L -lcpprest")

set(CPPREST_INCLUDE_DIR "" CACHE FILEPATH "Path to casablanca include dir")

include_directories (
include
"${CPPREST_INCLUDE_DIR}")

find_library(CPPREST_SO NAMES "cpprest" PATHS ${CPPREST_LIB_DIR} REQUIRED)

set(CMAKE_RUNTIME_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/bin)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/bin)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/bin)

add_subdirectory(src/signalrclient)
add_subdirectory(test)
