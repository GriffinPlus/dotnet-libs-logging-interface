///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using Xunit.Priority;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Provides constants that can be used to define the order of test execution.
/// </summary>
static class TestOrder
{
	/// <summary>
	/// Represents the name of the test collection used for sequential test execution.
	/// </summary>
	/// <remarks>
	/// This constant is typically used to group tests that must be executed sequentially to avoid
	/// interference or shared resource conflicts. All tests within this collection will run one after
	/// the other. The <see cref="PriorityAttribute"/> can be used within this collection to further
	/// control the order of test execution.
	/// </remarks>
	public const string TestsCollectionName = "Sequential Tests";

	/// <summary>
	/// Represents the value indicating that no modifications are applied.
	/// </summary>
	/// <remarks>
	/// This constant is typically used to specify or identify operations or states where no changes or
	/// modifications are performed.
	/// </remarks>
	public const int NonModifying = 0;

	/// <summary>
	/// Represents the base value used for modification calculations.
	/// </summary>
	/// <remarks>
	/// This constant can be used as a reference point or starting value in scenarios where a base modifier
	/// is required.
	/// </remarks>
	public const int ModifyingBase = 20;
}
