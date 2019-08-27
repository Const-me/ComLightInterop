using ComLight;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PortableClient
{
	// The generated assembly contains code like this one.
	static class TestProxyDelegates
	{
		[UnmanagedFunctionPointer( RuntimeClass.defaultCallingConvention )]
		public delegate int add( IntPtr pThis, int a, int b, [Out] out int result );
	}

	class TestProxy: RuntimeClass, ITest
	{
		readonly TestProxyDelegates.add m_add;
		readonly Func<int, int> m_addMarshaller;

		public TestProxy( IntPtr ptr, IntPtr[] vtbl, Guid id ) :
			base( ptr, vtbl, id )
		{
			m_add = Marshal.GetDelegateForFunctionPointer<TestProxyDelegates.add>( vtbl[ 3 ] );
		}

		int ITest.add( int a, int b, out int result )
		{
			return m_add( nativePointer, m_addMarshaller( a ), b, out result );
		}

		void ITest.addManaged( ITest managed, int a, int b, out int result )
		{
			throw new NotImplementedException();
		}
		int ITest.testPerformance( ITest managed, out int xor, out double seconds )
		{
			throw new NotImplementedException();
		}
		void ITest.testReadStream( Stream stm )
		{
			throw new NotImplementedException();
		}
	}
}