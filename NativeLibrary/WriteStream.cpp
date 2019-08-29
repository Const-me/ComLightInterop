#include "stdafx.h"
#include "WriteStream.h"

HRESULT WriteStream::write( const void* lpBuffer, int nNumberOfBytesToWrite )
{
	if( nullptr == m_file )
		return OLE_E_BLANK;
	if( nNumberOfBytesToWrite < 0 )
		return E_INVALIDARG;
	const size_t cb = (size_t)nNumberOfBytesToWrite;
	const size_t written = fwrite( lpBuffer, 1, cb, m_file );
	if( cb == written )
		return S_OK;
	return CTL_E_DEVICEIOERROR;
}

HRESULT WriteStream::flush()
{
	if( nullptr == m_file )
		return OLE_E_BLANK;
	if( 0 == fflush( m_file ) )
		return S_OK;
	return CTL_E_DEVICEIOERROR;
}

HRESULT WriteStream::createFile( LPCTSTR path )
{
	if( nullptr != m_file )
	{
		fclose( m_file );
		m_file = nullptr;
	}
#ifdef _MSC_VER
	const auto e = _wfopen_s( &m_file, path, L"wb" );
	return ( 0 == e ) ? S_OK : CTL_E_DEVICEIOERROR;
#else
	m_file = fopen( path, "wb" );
	return ( nullptr != m_file ) ? S_OK : CTL_E_DEVICEIOERROR;
#endif
}