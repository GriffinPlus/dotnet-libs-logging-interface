///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging
{

	partial class DecomposedTypeTestData
	{
		public class GenericTestClass<T>
		{
			public class NestedTestClass { }

			public class NestedGenericTestClass<T2> { }
		}
	}

}
