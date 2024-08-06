///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

using Xunit;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Unit tests targeting the <see cref="LogLevel"/> class.
/// </summary>
[Collection("LogLevelTests")]
public class LogLevelTests_Modifying
{
	#region GetAspect(string name)

	/// <summary>
	/// Tests the <see cref="LogLevel.GetAspect"/> method.
	/// </summary>
	[Fact]
	public void GetAspect()
	{
		LogLevel eventLogLevel = null;

		try
		{
			LogLevel.NewLogLevelRegistered += Handler;
			string name = Guid.NewGuid().ToString("D");
			LogLevel level = LogLevel.GetAspect(name);
			Assert.NotNull(level);
			Assert.Equal(LogLevel.MaxId, level.Id);
			Assert.Equal(name, level.Name);
			Assert.Same(level, eventLogLevel);
		}
		finally
		{
			LogLevel.NewLogLevelRegistered -= Handler;
		}
		return;

		void Handler(LogLevel level)
		{
			eventLogLevel = level;
		}
	}

	#endregion
}
