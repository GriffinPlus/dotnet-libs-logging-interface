///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Event handler for the <see cref="LogWriter.LogMessageWritten"/> event.
	/// </summary>
	/// <param name="writer">The log writer that writes the message.</param>
	/// <param name="level">The log level that is associated with the message.</param>
	/// <param name="message">The message text.</param>
	public delegate void LogMessageWrittenEventHandler(LogWriter writer, LogLevel level, string message);

}
