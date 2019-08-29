#pragma once
#include "../ComLightLib/streams.h"

struct DECLSPEC_NOVTABLE ITest : public ComLight::IUnknown
{
	DEFINE_INTERFACE_ID( "{a3ccc418-1565-47dc-88ab-78dcfb5cc800}" );

	virtual HRESULT COMLIGHTCALL add( int a, int b, int& result ) = 0;

	virtual HRESULT COMLIGHTCALL addManaged( ITest* pManaged, int a, int b, int& result ) = 0;

	virtual HRESULT COMLIGHTCALL testPerformance( ITest* pManaged, int& result, double& elapsedSeconds ) = 0;

	virtual HRESULT COMLIGHTCALL testStreams( ComLight::iReadStream* stmRead, ComLight::iWriteStream* stmWrite ) = 0;

	virtual HRESULT COMLIGHTCALL createFile( LPCTSTR path, ComLight::iWriteStream** pp ) = 0;

	virtual HRESULT COMLIGHTCALL testMarshalBack( LPCTSTR path, ITest* pManaged ) = 0;
};

// Another interface, just for testing the library
struct DECLSPEC_NOVTABLE ITest2 : public ComLight::IUnknown
{
	DEFINE_INTERFACE_ID( "{ffbed1a8-cd1a-4586-8916-3dc61dc7701e}" );
};