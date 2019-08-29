#pragma once
#include "../../ComLightLib/comLightServer.h"
#include "../../ComLightLib/streams.h"
using namespace ComLight;

struct DECLSPEC_NOVTABLE iFileSystem : public ComLight::IUnknown
{
	DEFINE_INTERFACE_ID( "{d29d85bf-d6d1-4c4c-8989-ce9260debc60}" );

	virtual HRESULT COMLIGHTCALL openFile( LPCTSTR path, iReadStream** stm ) = 0;
	virtual HRESULT COMLIGHTCALL createFile( LPCTSTR path, iWriteStream** stm ) = 0;
};

// You probably don't want to implement file streams in C++ in production code, especially not with <stdio.h>, I/O is way better in .NET.
// But for a demo, <stdio.h> streams are trivially easy to use, and are cross platform.
class NativeFileSystem : public ObjectRoot<iFileSystem>
{
	class ReadStream;
	class WriteStream;
	HRESULT COMLIGHTCALL openFile( LPCTSTR path, iReadStream** stm ) override;
	HRESULT COMLIGHTCALL createFile( LPCTSTR path, iWriteStream** stm ) override;
};