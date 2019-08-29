#pragma once
#include "ITest.h"
#include "../ComLightLib/comLightServer.h"

class Test: public ComLight::ObjectRoot<ITest>, public ITest2
{
	HRESULT COMLIGHTCALL add( int a, int b, int& result ) override;

	HRESULT COMLIGHTCALL addManaged( ITest* pManaged, int a, int b, int& result ) override;

	HRESULT COMLIGHTCALL testPerformance( ITest* pManaged, int& result, double& elapsedSeconds ) override;

	HRESULT COMLIGHTCALL testStreams( ComLight::iReadStream* stmRead, ComLight::iWriteStream* stmWrite ) override;

	HRESULT COMLIGHTCALL createFile( LPCTSTR path, ComLight::iWriteStream** pp ) override;

	HRESULT COMLIGHTCALL testMarshalBack( LPCTSTR path, ITest * pManaged ) override;

	// This line is only required if you want to consume the object from desktop .NET framework, and call it from multiple threads. See this for more info: https://stackoverflow.com/a/34978626/126995
	DECLARE_FREE_THREADED_MARSHALLER()

	// Unlike ATL, the interface map is optional for ComLight.
	// If you won't declare a map, the object will support 2 interfaces: IUnknown, and whatever template argument was passed to ObjectRoot class.
	// Interface map is only required to support multiple COM interfaces on the same object.

	/* BEGIN_COM_MAP()
		COM_INTERFACE_ENTRY( ITest )
		COM_INTERFACE_ENTRY( ITest2 )
	END_COM_MAP() */
};

DLLEXPORT HRESULT COMLIGHTCALL createTest( ITest **pp );