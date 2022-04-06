///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Event handler for the <see cref="LogWriter.NewLogWriterRegistered"/> event.
	/// </summary>
	/// <param name="writer">The new log writer.</param>
	public delegate void LogWriterRegisteredEventHandler(LogWriter writer);

}
