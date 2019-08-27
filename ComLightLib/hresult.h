#pragma once
#include <stdint.h>
#ifdef _MSC_VER
#include <winerror.h>
#else
#include "pal/hresult.h"
#endif

#define CHECK( hr ) { const HRESULT __hr = ( hr ); if( FAILED( __hr ) ) return __hr; }