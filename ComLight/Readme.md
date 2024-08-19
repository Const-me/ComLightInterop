This package implements a lightweight cross-platform COM interop library for Windows and Linux.
Specifically, it allows to expose C++ objects to .NET, and .NET objects to C++.
The current version targets 3 platforms: .NET framework 4.7.2, .NET 8.0, and VC++.

The library is designed under the assumption the entry point of your program is a .NET application which needs to use unmanaged C++ code compiled into a native DLL.

By 2024, I have used the library extensively in multiple projects which target Win32, Win64, also Linux running on AMD64, ARMv7, and ARM64 CPUs.
Some of these Linuxes were Alpine, the C++ side of the interop is very simple, does not even depend on `glibc`.

The library only uses good part of the COM, which is the `IUnknown` ABI.
It does not implement type libraries.
It’s your responsibility to write both C++ and C# language projections of the COM interfaces you use. The included C++ headers, and C# custom attributes, make it easy to do so.

The interop is limited to COM objects implemented by DLLs running within the current process.
The library only supports `IUnknown`-based COM interfaces, it doesn’t understand `IDispatch`.
You can only use simple types in your interfaces: primitives, structures, strings, pointers, arrays, function pointers, but not VARIANT or SAFEARRAY.

It doesn’t prevent you from calling the same COM object by multiple threads concurrently i.e. it doesn’t have a concept of apartments and treats all objects as free threaded.
If you call your objects concurrently, it’s your responsibility to make sure your objects are thread safe and reentrant.

It does not implement class IDs or type registration.
A class factory for C++ implemented objects is merely a C function which returns `IUnknown`-derived interface in an output parameter.