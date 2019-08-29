#include "interfaces.h"

struct DECLSPEC_NOVTABLE iStreamsDemo : public ComLight::IUnknown
{
	DEFINE_INTERFACE_ID( "0d30d69c-c9f5-40f1-b16b-77f54de38805" );

	virtual HRESULT COMLIGHTCALL init( iFileSystem* managed, iFileSystem** ppNative ) = 0;
};

class StreamsDemo : public ComLight::ObjectRoot<iStreamsDemo>
{
	CComPtr<iFileSystem> m_managed;

	HRESULT COMLIGHTCALL init( iFileSystem* managed, iFileSystem** ppNative ) override
	{
		m_managed = managed;
		return ComLight::Object<NativeFileSystem>::create( ppNative );
	}
};

DLLEXPORT HRESULT COMLIGHTCALL createStreams( iStreamsDemo **pp )
{
	return ComLight::Object<StreamsDemo>::create( pp );
}