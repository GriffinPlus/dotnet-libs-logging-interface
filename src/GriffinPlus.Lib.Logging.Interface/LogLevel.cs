///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// A log level (or aspect) that indicates the severity of a log message (immutable).
	/// The value of the <see cref="Id"/> property corresponds to equivalent syslog severity levels.
	/// </summary>
	public sealed class LogLevel
	{
		private static readonly char[]                       sLineSeparators = Unicode.NewLineCharacters.ToCharArray();
		private static          Dictionary<string, LogLevel> sLogLevelsByName;
		private static          LogLevel[]                   sLogLevelsById;
		private static          int                          sNextId;

		/// <summary>
		/// Occurs when a new log writer is registered.
		/// The global logging lock (<see cref="LogGlobals.Sync"/>) is acquired when raising the event.
		/// </summary>
		public static event LogLevelRegisteredEventHandler NewLogLevelRegistered;

		/// <summary>
		/// Emergency: Absolute "panic" condition, the system is unusable.
		/// </summary>
		public static readonly LogLevel Emergency = new LogLevel("Emergency");

		/// <summary>
		/// Alert: Something bad happened, immediate attention is required.
		/// </summary>
		public static readonly LogLevel Alert = new LogLevel("Alert");

		/// <summary>
		/// Critical: Something bad is about to happen, immediate attention is required.
		/// </summary>
		public static readonly LogLevel Critical = new LogLevel("Critical");

		/// <summary>
		/// Error: Non-urgent failure in the system that needs attention.
		/// </summary>
		public static readonly LogLevel Error = new LogLevel("Error");

		/// <summary>
		/// Warning: Something will happen if it is not dealt within a timeframe.
		/// </summary>
		public static readonly LogLevel Warning = new LogLevel("Warning");

		/// <summary>
		/// Notice: Normal but significant condition that might need special handling.
		/// </summary>
		public static readonly LogLevel Notice = new LogLevel("Notice");

		/// <summary>
		/// Informational: Informative but not important.
		/// </summary>
		public static readonly LogLevel Informational = new LogLevel("Informational");

		/// <summary>
		/// Debug: Only relevant for developers.
		/// </summary>
		public static readonly LogLevel Debug = new LogLevel("Debug");

		/// <summary>
		/// Trace: Only relevant for implementers.
		/// </summary>
		public static readonly LogLevel Trace = new LogLevel("Trace");

		/// <summary>
		/// Timing: Aspect that is used when timing is concerned.
		/// </summary>
		public static readonly LogLevel Timing = new LogLevel("Timing");

		/// <summary>
		/// None: Special log level expressing the lowest possible threshold for filtering purposes
		/// (no log level passes the filter). Using this log level to write messages is not allowed.
		/// A message written with this log level will be mapped to log level <see cref="Error"/>,
		/// a notice about this incident will be attached to the message text. The message will then
		/// bypass any filters induced by the configuration.
		/// </summary>
		public static readonly LogLevel None = new LogLevel("None", -1);

		/// <summary>
		/// All: Special log level expressing the highest possible threshold for filtering purposes
		/// (all log levels pass the filter). Using this log level to write messages is not allowed.
		/// A message written with this log level will be mapped to log level <see cref="Error"/>,
		/// a notice about this incident will be attached to the message text. The message will then
		/// bypass any filters induced by the configuration.
		/// </summary>
		public static readonly LogLevel All = new LogLevel("All", int.MaxValue);

		/// <summary>
		/// Gets the maximum id assigned to a log level.
		/// </summary>
		public static int MaxId => sNextId - 1;

		/// <summary>
		/// Gets the first log level id that is assigned to an aspect log level.
		/// </summary>
		public static int FirstAspectId => Timing.Id; // 'Timing' is the only pre-initialized aspect level

		/// <summary>
		/// All predefined log levels (the index corresponds to the id of the log level).
		/// </summary>
		private static readonly LogLevel[] sPredefinedLogLevels =
		{
			Emergency, Alert, Critical, Error, Warning, Notice, Informational, Debug, Trace
		};

		/// <summary>
		/// Initializes the <see cref="LogLevel"/> class.
		/// </summary>
		static LogLevel()
		{
			// populate log level collections with predefined log levels
			sLogLevelsByName = new Dictionary<string, LogLevel>
			{
				{ None.Name, None },
				{ All.Name, All },
				{ Timing.Name, Timing }
			};

			foreach (var level in sPredefinedLogLevels)
			{
				sLogLevelsByName.Add(level.Name, level);
			}

			sLogLevelsById = sLogLevelsByName
				.Where(x => x.Value.Id >= 0 && x.Value.Id < sNextId)
				.OrderBy(x => x.Value.Id)
				.Select(x => x.Value)
				.ToArray();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LogLevel"/> class.
		/// </summary>
		/// <param name="name">Name of the log level.</param>
		private LogLevel(string name)
		{
			// global logging lock is acquired here...
			CheckName(name);
			Name = name;
			Id = sNextId++;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="LogLevel"/> class (assigns a specific log level id).
		/// </summary>
		/// <param name="name">Name of the log level.</param>
		/// <param name="id">Id of the log level.</param>
		private LogLevel(string name, int id)
		{
			CheckName(name);
			Name = name;
			Id = id;
		}

		/// <summary>
		/// Gets the name of the log level.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the id of the log level.
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// Gets predefined log levels (all log levels that are not an aspect).
		/// </summary>
		public static IReadOnlyList<LogLevel> PredefinedLogLevels => sPredefinedLogLevels;

		/// <summary>
		/// Gets all log levels that are currently known (except log level 'None' and 'All').
		/// The index of the log level in the list corresponds to <see cref="LogLevel.Id"/>.
		/// </summary>
		public static IReadOnlyList<LogLevel> KnownLevels => sLogLevelsById;

		/// <summary>
		/// Gets a dictionary containing all known log levels by name.
		/// </summary>
		public static IReadOnlyDictionary<string, LogLevel> KnownLevelsByName => sLogLevelsByName;

		/// <summary>
		/// Checks whether the specified string is a valid log level name
		/// (log level names may consist of all characters except line separators).
		/// </summary>
		/// <param name="name">Name to check.</param>
		/// <exception cref="ArgumentNullException">The specified name is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">The specified name is invalid.</exception>
		public static void CheckName(string name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));

			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentException("The specified name consists of whitespace characters only and is therefore not a valid log level name.");

			if (name.IndexOfAny(sLineSeparators) >= 0)
			{
				string message =
					$"The specified name ({name}) is not a valid log level name.\n" +
					"Valid names may consist of all characters except line separators.\n";
				throw new ArgumentException(message);
			}
		}

		/// <summary>
		/// Gets the aspect log level with the specified name (or creates a new one, if it does not exist, yet).
		/// </summary>
		/// <param name="name">Name of the aspect log level to get.</param>
		/// <returns>The requested aspect log level.</returns>
		/// <exception cref="ArgumentNullException">The specified name is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">The specified name is invalid.</exception>
		public static LogLevel GetAspect(string name)
		{
			CheckName(name);

			sLogLevelsByName.TryGetValue(name, out var level);
			if (level == null)
			{
				lock (LogGlobals.Sync)
				{
					if (!sLogLevelsByName.TryGetValue(name, out level))
					{
						// log level does not exist, yet
						// => add a new one...
						level = new LogLevel(name);
						var newLogLevelsByName = new Dictionary<string, LogLevel>(sLogLevelsByName) { { level.Name, level } };
						var newLogLevelById = new LogLevel[sLogLevelsById.Length + 1];
						Array.Copy(sLogLevelsById, newLogLevelById, sLogLevelsById.Length);
						newLogLevelById[sLogLevelsById.Length] = level;
						Thread.MemoryBarrier(); // ensures everything has been actually written to memory at this point
						sLogLevelsByName = newLogLevelsByName;
						sLogLevelsById = newLogLevelById;

						// notify about the new log level
						OnNewLogLevelRegistered(level);
					}
				}
			}

			return level;
		}

		/// <summary>
		/// Converts a <see cref="LogLevel"/> to its name.
		/// </summary>
		/// <param name="level">Log level to convert.</param>
		public static implicit operator string(LogLevel level)
		{
			return level.Name;
		}

		/// <summary>
		/// Gets the string representation of the current log level.
		/// </summary>
		/// <returns>String representation of the current log level.</returns>
		public override string ToString()
		{
			return $"{Name} ({Id})";
		}

		/// <summary>
		/// Raises the <see cref="NewLogLevelRegistered"/> event.
		/// The global logging lock (<see cref="LogGlobals.Sync"/>) must be acquired when raising the event.
		/// </summary>
		/// <param name="level">The new log level.</param>
		private static void OnNewLogLevelRegistered(LogLevel level)
		{
			System.Diagnostics.Debug.Assert(Monitor.IsEntered(LogGlobals.Sync));
			var handler = NewLogLevelRegistered;
			handler?.Invoke(level);
		}
	}

}
