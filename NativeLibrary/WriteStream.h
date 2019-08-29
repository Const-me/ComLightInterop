#pragma once
#include "../ComLightLib/comLightServer.h"
#include "../ComLightLib/streams.h"
#include <stdio.h>

// iWriteStream implementation over <stdio.h> file handle.
class WriteStream : public ComLight::ObjectRoot<ComLight::iWriteStream>
{
	HRESULT write( const void* lpBuffer, int nNumberOfBytesToWrite ) override;

	HRESULT flush() override;

	FILE* m_file = nullptr;

public:

	WriteStream() = default;

	HRESULT createFile( LPCTSTR path );
};