# Use the same crt library for gtest and signalrclienttests projects (Windows only)
set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)
add_subdirectory (../../../../submodules/googletest gtest)

include_directories(${gtest_SOURCE_DIR}/include ${gtest_SOURCE_DIR})

add_subdirectory (signalrclienttests)
