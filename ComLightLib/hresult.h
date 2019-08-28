#pragma once
#include <stdint.h>
#ifdef _MSC_VER
#include <winerror.h>
#else
#include "pal/hresult.h"
#endif

#define CHECK( hr ) { const HRESULT __hr = ( hr ); if( FAILED( __hr ) ) return __hr; }

#ifndef _MSC_VER
inline constexpr HRESULT HRESULT_FROM_WIN32( int c )
{
	return c < 0 ? c : ( ( 0xFFFF & c ) | 0x80070000 );
}
constexpr int ERROR_HANDLE_EOF = 38;
#endif

constexpr HRESULT E_EOF = HRESULT_FROM_WIN32( ERROR_HANDLE_EOF );