#add_subdirectory (gtest)
add_subdirectory (../../../../submodules/googletest gtest)
enable_testing()
include_directories(${gtest_SOURCE_DIR}/include ${gtest_SOURCE_DIR})

add_subdirectory (signalrclienttests)
