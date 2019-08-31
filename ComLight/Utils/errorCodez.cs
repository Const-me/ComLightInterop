using System.Collections.Generic;

namespace ComLight
{
	static partial class ErrorCodes
	{
		static readonly Dictionary<int, string> codes = new Dictionary<int, string>()
		{
			{ unchecked( (int)0x80070026 ), "Reached the end of the file." },
			{ unchecked( (int)0x80040007 ), "Uninitialized object" },
		};
	}
}