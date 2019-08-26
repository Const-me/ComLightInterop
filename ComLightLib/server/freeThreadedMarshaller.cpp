#include "freeThreadedMarshaller.h"
#ifdef _MSC_VER
#include <combaseapi.h>

HRESULT ComLight::details::createFreeThreadedMarshaller( IUnknown* pUnkOuter, IUnknown** ppUnkMarshal )
{
	return ::CoCreateFreeThreadedMarshaler( (LPUNKNOWN)pUnkOuter, (LPUNKNOWN *)ppUnkMarshal );
}

bool ComLight::details::queryMarshallerInterface( REFIID riid, void **ppvObject, IUnknown* marshaller )
{
	if( riid != IID_IMarshal || nullptr == marshaller )
		return false;
	marshaller->AddRef();
	*ppvObject = marshaller;
	return true;
}
#endif