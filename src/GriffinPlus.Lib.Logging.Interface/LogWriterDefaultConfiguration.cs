///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// A configuration with defaults for log writers.
	/// It simply enables all log levels above and including 'Notice', independent of the log writer name.
	/// </summary>
	class LogWriterDefaultConfiguration : ILogWriterConfiguration
	{
		private readonly LogLevelBitMask mMask = new LogLevelBitMask(32, false, false);

		/// <summary>
		/// Initializes a new instance of the <see cref="LogWriterDefaultConfiguration"/> class.
		/// </summary>
		public LogWriterDefaultConfiguration()
		{
			// enable the standard log levels ('Notice' and higher)
			mMask.SetBit(LogLevel.Emergency.Id);
			mMask.SetBit(LogLevel.Alert.Id);
			mMask.SetBit(LogLevel.Critical.Id);
			mMask.SetBit(LogLevel.Error.Id);
			mMask.SetBit(LogLevel.Warning.Id);
			mMask.SetBit(LogLevel.Notice.Id);
		}

		/// <summary>
		/// Gets a bit mask in which each bit is associated with a log level with the same id
		/// and expresses whether the corresponding log level is active for the specified writer.
		/// </summary>
		/// <param name="writer">Log writer to get the active log level mask for.</param>
		/// <returns>The requested active log level mask.</returns>
		public LogLevelBitMask GetActiveLogLevelMask(LogWriter writer) => mMask;
	}

}
