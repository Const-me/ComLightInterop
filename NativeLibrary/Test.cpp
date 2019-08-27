#include "stdafx.h"
#include "Test.h"
#include <vector>
#include <chrono>
#include <random>

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

HRESULT COMLIGHTCALL Test::testReadStream( ComLight::iReadStream* stm )
{
	int64_t len;
	CHECK( stm->getLength( len ) );

	std::vector<uint8_t> vec;
	vec.resize( (size_t)len );
	CHECK( stm->seek( 0, ComLight::eSeekOrigin::Begin ) );
	CHECK( stm->read( vec.data(), (int)len, 0, (int)len ) );
	vec.resize( (size_t)len + 1 );
	vec[ (size_t)len ] = 0;
	printf( "%s\n", vec.data() );

	return S_OK;
}

DLLEXPORT HRESULT COMLIGHTCALL createTest( ITest **pp )
{
	constexpr HRESULT E_EOF = HRESULT_FROM_WIN32( ERROR_HANDLE_EOF );

	return ComLight::Object<Test>::create( pp );
}