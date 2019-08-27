#pragma once
#include <vector>
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

		virtual HRESULT read( void* buffer, int bufferLength, int offset, int count ) = 0;
		virtual HRESULT seek( int64_t offset, eSeekOrigin origin ) = 0;
		virtual HRESULT getPosition( int64_t& position ) = 0;
		virtual HRESULT getLength( int64_t& length ) = 0;
	};

	struct DECLSPEC_NOVTABLE iWriteStream : public IUnknown
	{
		DEFINE_INTERFACE_ID( "d7c3eb39-9170-43b9-ba98-2ea1f2fed8a8" );

		virtual HRESULT write( const void* buffer, int count ) = 0;
		virtual HRESULT flush() = 0;

		template<class E>
		inline HRESULT write( const std::vector<E>& vec )
		{
			const size_t cb = sizeof( E ) * vec.size();
			return write( vec.data(), (int)cb );
		}
	};
}