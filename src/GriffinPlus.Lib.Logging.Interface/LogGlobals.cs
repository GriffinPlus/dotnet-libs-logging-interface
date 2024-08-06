///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Some global data (for internal use only).
/// </summary>
static class LogGlobals
{
	/// <summary>
	/// Object that is used to synchronize access to shared resources in the logging subsystem.
	/// Used in conjunction with monitor synchronization.
	/// </summary>
	public static readonly object Sync = new();
}
