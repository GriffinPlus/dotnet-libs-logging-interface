///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;

using Xunit;
using Xunit.Priority;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Unit tests targeting the <see cref="LogWriter"/> class.
/// This class contains tests that influence the configuration of log writers that might interfere with other tests.
/// </summary>
[Collection(TestOrder.TestsCollectionName)]
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class LogWriterTests_Configuration
{
	/// <summary>
	/// Tests the <see cref="LogWriter.UpdateLogWriters"/> method.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 1)]
	public void UpdateLogWriters()
	{
		// create a log writer to test with
		LogWriter writer = LogWriter.Get("LogWriterToTest");

		// configure log writers to let messages with log level 'Informational' pass only
		var configuration = new LogWriterTestConfiguration(LogLevel.Informational);
		ILogWriterConfiguration? oldConfiguration = null;
		try
		{
			oldConfiguration = LogWriter.UpdateLogWriters(configuration);

			// only the 'Informational' log level should be active now
			Assert.False(writer.IsLogLevelActive(LogLevel.Emergency));
			Assert.False(writer.IsLogLevelActive(LogLevel.Alert));
			Assert.False(writer.IsLogLevelActive(LogLevel.Critical));
			Assert.False(writer.IsLogLevelActive(LogLevel.Error));
			Assert.False(writer.IsLogLevelActive(LogLevel.Warning));
			Assert.False(writer.IsLogLevelActive(LogLevel.Notice));
			Assert.True(writer.IsLogLevelActive(LogLevel.Informational));
			Assert.False(writer.IsLogLevelActive(LogLevel.Debug));
			Assert.False(writer.IsLogLevelActive(LogLevel.Trace));
			Assert.False(writer.IsLogLevelActive(LogLevel.Timing));

			// the special log levels are always active
			// (although they should not be used to write messages)
			Assert.True(writer.IsLogLevelActive(LogLevel.None));
			Assert.True(writer.IsLogLevelActive(LogLevel.All));
		}
		finally
		{
			if (oldConfiguration != null)
				LogWriter.UpdateLogWriters(oldConfiguration);
		}
	}

	/// <summary>
	/// Tests the <see cref="LogWriter.UpdateLogWriters"/> method passing <see langword="null"/> as configuration.
	/// The method should throw a <see cref="ArgumentNullException"/> in this case.
	/// </summary>
	[Fact]
	[Priority(TestOrder.NonModifying)]
	public void UpdateLogWriters_ConfigurationIsNull()
	{
		Assert.Throws<ArgumentNullException>(() => LogWriter.UpdateLogWriters(null!));
	}
}
