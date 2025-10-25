///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Threading;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Provides a "safe" way to flush logging queues and terminate the application without losing any log messages.
/// </summary>
public static class FailFast
{
	/// <summary>
	/// Occurs when the <see cref="TerminateApplication(string)"/> method is called.<br/>
	/// The GriffinPlus.Lib.Logging package hooks up to this event to flush all log writers before the application is terminated.
	/// </summary>
	internal static event Action<string>? TerminationRequestedWithMessage;

	/// <summary>
	/// Occurs when the <see cref="TerminateApplication(Exception)"/> method is called.<br/>
	/// The GriffinPlus.Lib.Logging package hooks up to this event to flush all log writers before the application is terminated.
	/// </summary>
	internal static event Action<Exception>? TerminationRequestedWithException;

	/// <summary>
	/// Requests terminating the application specifying a message describing the reason that led to this incident.<br/>
	/// The process is terminated after buffered log messages have been processed by the logging subsystem.
	/// </summary>
	/// <param name="message">The message text describing the reason why application termination is requested.</param>
	public static void TerminateApplication(string message)
	{
		// global logging lock should not be held here...
		Debug.Assert(!Monitor.IsEntered(LogGlobals.Sync));

		Action<string>? handler = TerminationRequestedWithMessage;

		if (handler == null)
		{
			throw new InvalidOperationException(
				"No termination handler registered.\n" +
				"The GriffinPlus.Lib.Logging package should register a termination handler that flushes all log writers and terminates the application.");
		}

		handler(message);
	}

	/// <summary>
	/// Requests terminating the application specifying the exception that led to this incident.<br/>
	/// The process is terminated after buffered log messages have been processed by the logging subsystem.
	/// </summary>
	/// <param name="exception">The exception that is the reason why application termination is requested.</param>
	public static void TerminateApplication(Exception exception)
	{
		// global logging lock should not be held here...
		Debug.Assert(!Monitor.IsEntered(LogGlobals.Sync));

		Action<Exception>? handler = TerminationRequestedWithException;

		if (handler == null)
		{
			throw new InvalidOperationException(
				"No termination handler registered.\n" +
				"The GriffinPlus.Lib.Logging package should register a termination handler that flushes all log writers and terminates the application.");
		}

		handler(exception);
	}
}
