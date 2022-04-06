///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Event handler for the <see cref="LogLevel.NewLogLevelRegistered"/> event.
	/// </summary>
	/// <param name="level">The new log level.</param>
	public delegate void LogLevelRegisteredEventHandler(LogLevel level);

}
