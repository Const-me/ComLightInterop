#include "interfaces.h"
#include <array>

class StreamsDemo : public ComLight::ObjectRoot<iStreamsDemo>
{
	CComPtr<iFileSystem> m_managed;

	HRESULT COMLIGHTCALL init( iFileSystem* managed, iFileSystem** ppNative ) override
	{
		m_managed = managed;
		return ComLight::Object<NativeFileSystem>::create( ppNative );
	}

	HRESULT COMLIGHTCALL copyWithManaged( LPCTSTR pathFrom, LPCTSTR pathTo ) override
	{
		CComPtr<iReadStream> read;
		CHECK( m_managed->openFile( pathFrom, &read ) );

		CComPtr<iWriteStream> write;
		CHECK( m_managed->createFile( pathTo, &write ) );

		constexpr int cbBuffer = 1024;
		std::array<uint8_t, cbBuffer> buffer;

		while( true )
		{
			int cb;
			CHECK( read->read( buffer.data(), cbBuffer, cb ) );
			if( 0 == cb )
				return write->flush();
			CHECK( write->write( buffer.data(), cb ) );
		}
	}
};

DLLEXPORT HRESULT COMLIGHTCALL createStreams( iStreamsDemo **pp )
{
	return ComLight::Object<StreamsDemo>::create( pp );
}