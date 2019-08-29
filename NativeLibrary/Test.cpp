#include "stdafx.h"
#include "Test.h"
#include <vector>
#include <chrono>
#include <random>
#include "WriteStream.h"

HRESULT COMLIGHTCALL Test::add( int a, int b, int& result )
{
	int64_t res = (int64_t)a + (int64_t)b;
	if( res < INT_MIN || res > INT_MAX )
		return DISP_E_OVERFLOW;
	result = (int)res;
	return S_OK;
}

HRESULT COMLIGHTCALL Test::addManaged( ITest* pManaged, int a, int b, int& result )
{
	return pManaged->add( a, b, result );
}

HRESULT COMLIGHTCALL Test::testPerformance( ITest* pManaged, int& result, double& elapsedSeconds )
{
	std::vector<int> values;
	values.resize( 1000000 );

	// https://stackoverflow.com/a/19666713/126995
	std::random_device rd;
	std::mt19937 mt( rd() );
	std::uniform_int_distribution<int> dist( 0, 0x40000000 );
	for( int& v : values )
		v = dist( mt );

	int x = 0;
	const auto start = std::chrono::high_resolution_clock::now();
	for( int i: values )
	{
		int r;
		pManaged->add( i, i, r );
		x ^= r;
	}
	const auto finish = std::chrono::high_resolution_clock::now();
	std::chrono::duration<double> elapsed = finish - start;
	result = x;
	elapsedSeconds = elapsed.count();
	return S_OK;
}

HRESULT COMLIGHTCALL Test::testStreams( ComLight::iReadStream* stmRead, ComLight::iWriteStream* stmWrite )
{
	int64_t len;
	CHECK( stmRead->getLength( len ) );

	std::vector<uint8_t> vec;
	vec.resize( (size_t)len );
	CHECK( stmRead->seek( 0, ComLight::eSeekOrigin::Begin ) );
	CHECK( stmRead->read( vec ) );
	CHECK( stmWrite->write( vec ) );
	return S_OK;
}

HRESULT COMLIGHTCALL Test::createFile( LPCTSTR path, ComLight::iWriteStream** pp )
{
	if( nullptr == pp )
		return E_POINTER;
	using namespace ComLight;
	CComPtr<Object<WriteStream>> stm;
	CHECK( Object<WriteStream>::create( stm ) );
	CHECK( stm->createFile( path ) );
	stm.detach( pp );
	return S_OK;
}

DLLEXPORT HRESULT COMLIGHTCALL createTest( ITest **pp )
{
	return ComLight::Object<Test>::create( pp );
}