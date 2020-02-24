#include <stdio.h>
#include "../../ComLightLib/comLightServer.h"

// Declare an interface
struct DECLSPEC_NOVTABLE IHelloWorld : public ComLight::IUnknown
{
	DEFINE_INTERFACE_ID( "{cdc9e3c6-b300-4138-b006-c61e7c2bfe48}" );

	virtual HRESULT COMLIGHTCALL print( const char* msg ) = 0;
};

// Implement the interface
class HelloWorld : public ComLight::ObjectRoot<IHelloWorld>
{
	HRESULT COMLIGHTCALL print( const char* msg ) override
	{
		printf( "%s\n", msg );
		return S_FALSE;
	}
};

// Create class factory function. Registration parts of COM is not great, also not portable. Using C API instead.
DLLEXPORT HRESULT COMLIGHTCALL createHelloWorld( IHelloWorld **pp )
{
	return ComLight::Object<HelloWorld>::create( pp );
}