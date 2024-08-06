///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// A test configuration for log writers.
/// </summary>
public class LogWriterTestConfiguration : ILogWriterConfiguration
{
	private readonly LogLevelBitMask mMask = new(32, false, false);

	/// <summary>
	/// Initializes a new instance of the <see cref="LogWriterTestConfiguration"/> class.
	/// </summary>
	public LogWriterTestConfiguration(params LogLevel[] levelsToEnable)
	{
		foreach (LogLevel level in levelsToEnable)
		{
			mMask.SetBit(level.Id);
		}
	}

	/// <summary>
	/// Gets a bit mask in which each bit is associated with a log level with the same id
	/// and expresses whether the corresponding log level is active for the specified writer.
	/// </summary>
	/// <param name="writer">Log writer to get the active log level mask for.</param>
	/// <returns>The requested active log level mask.</returns>
	public LogLevelBitMask GetActiveLogLevelMask(LogWriter writer)
	{
		return mMask;
	}
}
