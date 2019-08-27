#pragma once
#include "comLightCommon.h"

namespace ComLight
{
	enum struct eSeekOrigin : uint8_t
	{
		Begin = 0,
		Current = 1,
		End = 2
	};

	struct DECLSPEC_NOVTABLE iReadStream : public IUnknown
	{
		DEFINE_INTERFACE_ID( "006af6db-734e-4595-8c94-19304b2389ac" );

		virtual HRESULT read( uint8_t* buffer, int bufferLength, int offset, int count ) = 0;
		virtual HRESULT seek( int64_t offset, eSeekOrigin origin ) = 0;
		virtual HRESULT getPosition( int64_t& position ) = 0;
		virtual HRESULT getLength( int64_t& length ) = 0;
	};
}