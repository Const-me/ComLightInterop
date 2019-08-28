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

	namespace details
	{
		template<class E>
		inline size_t sizeofVector( const std::vector<E>& vec )
		{
			return sizeof( E ) * vec.size();
		}
	}

	struct DECLSPEC_NOVTABLE iReadStream : public IUnknown
	{
		DEFINE_INTERFACE_ID( "006af6db-734e-4595-8c94-19304b2389ac" );

		virtual HRESULT read( void* lpBuffer, int nNumberOfBytesToRead, int &lpNumberOfBytesRead ) = 0;
		virtual HRESULT seek( int64_t offset, eSeekOrigin origin ) = 0;
		virtual HRESULT getPosition( int64_t& position ) = 0;
		virtual HRESULT getLength( int64_t& length ) = 0;

		template<class E>
		inline HRESULT read( std::vector<E>& vec )
		{
			const int cb = (int)details::sizeofVector( vec );
			int cbRead = 0;
			CHECK( read( vec.data(), cb, cbRead ) );
			if( cbRead >= cb )
				return S_OK;
			return E_EOF;
		}
	};

	struct DECLSPEC_NOVTABLE iWriteStream : public IUnknown
	{
		DEFINE_INTERFACE_ID( "d7c3eb39-9170-43b9-ba98-2ea1f2fed8a8" );

		virtual HRESULT write( const void* lpBuffer, int nNumberOfBytesToWrite ) = 0;
		virtual HRESULT flush() = 0;

		template<class E>
		inline HRESULT write( const std::vector<E>& vec )
		{
			const int cb = (int)details::sizeofVector( vec );
			return write( vec.data(), cb );
		}
	};
}