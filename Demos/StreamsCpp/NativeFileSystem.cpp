#include "interfaces.h"

class NativeFileSystem::ReadStream : public ObjectRoot<iReadStream>
{
	HRESULT read( void* lpBuffer, int nNumberOfBytesToRead, int &lpNumberOfBytesRead ) override
	{
		if( nullptr == m_file )
			return OLE_E_BLANK;
		const size_t cb = fread( lpBuffer, 1, (size_t)nNumberOfBytesToRead, m_file );
		lpNumberOfBytesRead = (int)cb;
		return S_OK;
	}

	HRESULT seek( int64_t offset, eSeekOrigin origin ) override { return E_NOTIMPL; }

	HRESULT getPosition( int64_t& position ) override
	{
		if( nullptr == m_file )
			return OLE_E_BLANK;
		position = (int64_t)ftell( m_file );
		return S_OK;
	}

	HRESULT getLength( int64_t& length ) override
	{
		if( nullptr == m_file )
			return OLE_E_BLANK;
		fseek( m_file, 0, SEEK_END );
		length = (int64_t)ftell( m_file );
		fseek( m_file, 0, SEEK_SET );
		return S_OK;
	}

	FILE* m_file = nullptr;

public:

	HRESULT openFile( LPCTSTR path )
	{
#ifdef _MSC_VER
		const auto e = _wfopen_s( &m_file, path, L"rb" );
		return ( 0 == e ) ? S_OK : CTL_E_DEVICEIOERROR;
#else
		m_file = fopen( path, "rb" );
		return ( nullptr != m_file ) ? S_OK : CTL_E_DEVICEIOERROR;
#endif
	}
};

HRESULT COMLIGHTCALL NativeFileSystem::openFile( LPCTSTR path, iReadStream** pp )
{
	CComPtr<Object<ReadStream>> stm;
	CHECK( Object<ReadStream>::create( stm ) );
	CHECK( stm->openFile( path ) );
	stm.detach( pp );
	return S_OK;
}

class NativeFileSystem::WriteStream : public ObjectRoot<iWriteStream>
{
	HRESULT write( const void* lpBuffer, int nNumberOfBytesToWrite ) override
	{
		if( nullptr == m_file )
			return OLE_E_BLANK;
		const size_t cb = (size_t)nNumberOfBytesToWrite;
		const size_t written = fwrite( lpBuffer, 1, cb, m_file );
		if( cb == written )
			return S_OK;
		return CTL_E_DEVICEIOERROR;
	}

	HRESULT flush() override
	{
		if( nullptr == m_file )
			return OLE_E_BLANK;
		if( 0 == fflush( m_file ) )
			return S_OK;
		return CTL_E_DEVICEIOERROR;
	}

	FILE* m_file = nullptr;

public:

	WriteStream() = default;

	HRESULT createFile( LPCTSTR path )
	{
#ifdef _MSC_VER
		const auto e = _wfopen_s( &m_file, path, L"wb" );
		return ( 0 == e ) ? S_OK : CTL_E_DEVICEIOERROR;
#else
		m_file = fopen( path, "wb" );
		return ( nullptr != m_file ) ? S_OK : CTL_E_DEVICEIOERROR;
#endif
	}
};

HRESULT COMLIGHTCALL NativeFileSystem::createFile( LPCTSTR path, iWriteStream** pp )
{
	CComPtr<Object<WriteStream>> stm;
	CHECK( Object<WriteStream>::create( stm ) );
	CHECK( stm->createFile( path ) );
	stm.detach( pp );
	return S_OK;
}