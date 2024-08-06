///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Interface of a log writer configuration.
/// Must be implemented thread-safe.
/// </summary>
public interface ILogWriterConfiguration
{
	/// <summary>
	/// Gets a bit mask in which each bit is associated with a log level with the same id
	/// and expresses whether the corresponding log level is active for the specified writer.
	/// </summary>
	/// <param name="writer">Log writer to get the active log level mask for.</param>
	/// <returns>The requested active log level mask.</returns>
	LogLevelBitMask GetActiveLogLevelMask(LogWriter writer);
}
