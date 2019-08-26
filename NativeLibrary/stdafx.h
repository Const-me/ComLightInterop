#pragma once
#ifdef _MSC_VER

#include "targetver.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#endif
#include <limits.h>


#ifdef _MSC_VER
// On Windows, it's controlled by library.def module definition file.
#define DLLEXPORT extern "C"
#else
#define DLLEXPORT extern "C" __attribute__((visibility("default")))
#endif