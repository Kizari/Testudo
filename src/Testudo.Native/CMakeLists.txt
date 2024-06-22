# Declare project
cmake_minimum_required(VERSION 3.10)
project(Testudo.Native LANGUAGES CXX VERSION 1.0)
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED True)

# Library includes
find_package(PkgConfig REQUIRED)
pkg_check_modules(WEBKIT2GTK REQUIRED webkit2gtk-4.1)
include_directories(${WEBKIT2GTK_INCLUDE_DIRS})
link_directories(${WEBKIT2GTK_LIBRARY_DIRS})

# Declare output library
add_library(Testudo.Native SHARED 
    TestudoApplicationExports.cpp
    TestudoWindowExports.cpp
    Linux/TestudoApplication.cpp
    Linux/TestudoWindow.cpp)

# Link dependencies
target_link_libraries(${PROJECT_NAME} ${WEBKIT2GTK_LIBRARIES})

# Remove "lib" prefix from output library
set_target_properties(${PROJECT_NAME} PROPERTIES PREFIX "")

