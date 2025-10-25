///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Reflection;
using System.Threading.Tasks;

using Xunit;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

// ReSharper disable ReplaceAsyncWithTaskReturn
// ReSharper disable UnusedVariable

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Unit tests targeting the <see cref="AsyncId"/> class.
/// </summary>
public class AsyncIdTests
{
	/// <summary>
	/// Tests the <see cref="AsyncId.Current"/> property.
	/// </summary>
	[Fact]
	public async Task Current()
	{
		await Current_AssignNewIdAsync(1);
		await Current_AssignNewIdAsync(2);
		Current_TestWrapAround();
	}

	private static async Task Current_AssignNewIdAsync(uint expectedId)
	{
		// first call to AsyncId.Current => assign a new id
		uint id = AsyncId.Current;
		Assert.Equal(expectedId, AsyncId.Current);
		await Current_ReuseIdAsync(expectedId);
	}

	private static async Task Current_ReuseIdAsync(uint expectedId)
	{
		// subsequent call to AsyncId.Current => return assigned id
		uint id = AsyncId.Current;
		Assert.Equal(expectedId, AsyncId.Current);
	}

	private static void Current_TestWrapAround()
	{
		// set the internal id counter to the greatest possible value
		unchecked
		{
			FieldInfo? field = typeof(AsyncId).GetField("sAsyncIdCounter", BindingFlags.NonPublic | BindingFlags.Static);
			Assert.NotNull(field);
			field.SetValue(null, (int)uint.MaxValue);
		}

		// requesting the next id should return 1 skipping 0
		Assert.Equal(1u, AsyncId.Current);
	}
}
