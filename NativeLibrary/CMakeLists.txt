cmake_minimum_required( VERSION 3.5 )
project( cpp-demo LANGUAGES CXX )

set( CMAKE_CXX_STANDARD 14 )
set( CMAKE_CXX_STANDARD_REQUIRED ON )
set( CMAKE_CXX_EXTENSIONS OFF )
set( CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fvisibility=hidden -fvisibility-inlines-hidden -Wall -Wno-psabi" )

# https://stackoverflow.com/q/46724267/126995
include( CheckCXXCompilerFlag )
CHECK_CXX_COMPILER_FLAG( "-march=native" COMPILER_SUPPORTS_MARCH_NATIVE )
if( COMPILER_SUPPORTS_MARCH_NATIVE )
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -march=native")
endif()

CHECK_CXX_COMPILER_FLAG( "-O3" COMPILER_SUPPORTS_O3 )
if( COMPILER_SUPPORTS_O3 )
    set( CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -O3" )
endif()

add_library( comtest SHARED Test.cpp )