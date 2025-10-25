///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// A log writer (thread-safe).
/// </summary>
public sealed class LogWriter
{
	private static          List<LogWriter>                  sLogWritersById                  = [];
	private static          Dictionary<string, LogWriter>    sLogWritersByName                = new();
	private static          List<LogWriterTag>               sLogWriterTagsById               = [];
	private static          Dictionary<string, LogWriterTag> sLogWriterTagsByName             = new();
	private static          ILogWriterConfiguration?         sLogWriterConfiguration          = null;
	private static readonly IFormatProvider                  sDefaultFormatProvider           = CultureInfo.InvariantCulture;
	private static readonly ThreadLocal<StringBuilder>       sBuilder                         = new(() => new StringBuilder());
	private static readonly char[]                           sLineSeparators                  = Unicode.NewLineCharacters.ToCharArray();
	private static readonly string[]                         sNewlineTokens                   = ["\r\n", "\r", "\n"];
	private static readonly Regex                            sExtractGenericArgumentTypeRegex = new("^([^`]+)`\\d+$", RegexOptions.Compiled);
	private static          int                              sNextId;
	private readonly        List<WeakReference<LogWriter>>?  mSecondaryWriters;

	/// <summary>
	/// Occurs when a new log writer is registered.
	/// The global logging lock (<see cref="LogGlobals.Sync"/>) is acquired when raising the event.
	/// </summary>
	public static event LogWriterRegisteredEventHandler? NewLogWriterRegistered;

	/// <summary>
	/// Occurs when a new log writer tag is registered.
	/// The global logging lock (<see cref="LogGlobals.Sync"/>) is acquired when raising the event.
	/// </summary>
	public static event LogWriterTagRegisteredEventHandler? NewLogWriterTagRegistered;

	/// <summary>
	/// Occurs when a log writer writes a log message.
	/// The global logging lock (<see cref="LogGlobals.Sync"/>) is not acquired when raising the event.
	/// </summary>
	public static event LogMessageWrittenEventHandler? LogMessageWritten;

	/// <summary>
	/// Initializes the <see cref="LogWriter"/> class.
	/// </summary>
	static LogWriter()
	{
		// configure log writers with the standard set of enabled log levels, i.e. 'Notice' and up
		// (does not differentiate between source names, takes the level into account only)
		UpdateLogWriters(new LogWriterDefaultConfiguration());
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LogWriter"/> class (for primary writers).
	/// </summary>
	/// <param name="name">Name of the log writer.</param>
	private LogWriter(string name)
	{
		// global logging lock is acquired here...
		CheckName(name);
		mSecondaryWriters = [];
		PrimaryWriter = this;
		Id = sNextId++;
		Name = name;
		ActiveLogLevelMask = LogLevelBitMask.Zeros;
		Tags = LogWriterTagSet.Empty;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LogWriter"/> class (for secondary writers).
	/// </summary>
	/// <param name="writer">The original (non-tagging) log writer.</param>
	/// <param name="tags">Tags the tagging log writer should attach to written messages.</param>
	private LogWriter(LogWriter writer, LogWriterTagSet tags)
	{
		// global logging lock is acquired here...
		PrimaryWriter = writer.PrimaryWriter;
		Id = PrimaryWriter.Id;
		Name = PrimaryWriter.Name;
		ActiveLogLevelMask = LogLevelBitMask.Zeros;
		Tags = tags;
	}

	/// <summary>
	/// Gets all log writers that have been registered using <see cref="Get{T}"/>, <see cref="Get(Type)"/> or <see cref="Get(string)"/>.
	/// The index of the log writer in the list corresponds to <see cref="LogWriter.Id"/>.
	/// </summary>
	public static IReadOnlyList<LogWriter> KnownWriters => sLogWritersById;

	/// <summary>
	/// Gets all log writer tags that have been registered using <see cref="WithTag"/> or <see cref="WithTags"/>.
	/// The index of the log writer tag in the list corresponds to <see cref="LogWriterTag.Id"/>.
	/// </summary>
	public static IReadOnlyList<LogWriterTag> KnownTags => sLogWriterTagsById;

	/// <summary>
	/// Converts a <see cref="LogWriter"/> to its name.
	/// </summary>
	/// <param name="writer">Log writer to convert.</param>
	public static implicit operator string(LogWriter writer)
	{
		return writer.Name;
	}

	/// <summary>
	/// Gets the current timestamp as used by the logging subsystem.
	/// </summary>
	/// <returns>The current timestamp.</returns>
	public static DateTimeOffset GetTimestamp()
	{
		return DateTimeOffset.Now;
	}

	/// <summary>
	/// Gets the current high precision timestamp as used by the logging subsystem (in ns).
	/// </summary>
	/// <returns>The current high precision timestamp.</returns>
	public static long GetHighPrecisionTimestamp()
	{
		return (long)((decimal)Stopwatch.GetTimestamp() * 1000000000L / Stopwatch.Frequency); // in ns
	}

	/// <summary>
	/// Gets the id of the log writer.
	/// </summary>
	public int Id { get; }

	/// <summary>
	/// Gets the name of the log writer.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the tags the log writer attaches to a written message.
	/// </summary>
	public LogWriterTagSet Tags { get; }

	/// <summary>
	/// Gets the primary log writer
	/// (the initial log writer with the same name that does not modify messages when writing them).
	/// </summary>
	public LogWriter PrimaryWriter { get; }

	/// <summary>
	/// Gets or sets the bit mask indicating which log levels are active for the log writer.
	/// </summary>
	internal LogLevelBitMask ActiveLogLevelMask { get; set; }

	/// <summary>
	/// Gets a log writer with the specified name that can be used to write to the log.
	/// </summary>
	/// <param name="name">Name of the log writer to get.</param>
	/// <returns>The requested log writer.</returns>
	/// <exception cref="ArgumentNullException">The specified name is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">The specified name is invalid.</exception>
	public static LogWriter Get(string name)
	{
		CheckName(name);

		sLogWritersByName.TryGetValue(name, out LogWriter? writer);
		if (writer != null) return writer;

		lock (LogGlobals.Sync)
		{
			if (sLogWritersByName.TryGetValue(name, out writer))
				return writer;

			writer = new LogWriter(name);

			// the id of the writer should correspond to the index in the list and the
			// number of elements in the dictionary.
			Debug.Assert(writer.Id == sLogWritersById.Count);
			Debug.Assert(writer.Id == sLogWritersByName.Count);

			// set active log level mask, if the configuration is already initialized
			if (sLogWriterConfiguration != null)
			{
				writer.ActiveLogLevelMask = sLogWriterConfiguration.GetActiveLogLevelMask(writer);
			}

			// replace log writer list
			var newLogWritersById = new List<LogWriter>(sLogWritersById) { writer };
			Thread.MemoryBarrier(); // ensures everything has been actually written to memory at this point
			sLogWritersById = newLogWritersById;

			// replace log writer collection dictionary
			var newLogWritersByName = new Dictionary<string, LogWriter>(sLogWritersByName) { { writer.Name, writer } };
			Thread.MemoryBarrier(); // ensures everything has been actually written to memory at this point
			sLogWritersByName = newLogWritersByName;

			// notify about the new log writer
			OnNewLogWriterRegistered(writer);
		}

		return writer;
	}

	/// <summary>
	/// Gets a log writer for the specified type that can be used to write to the log
	/// (the full name of the type becomes the name of the log writer).
	/// </summary>
	/// <param name="type">The type whose full name is to use as the log writer name.</param>
	/// <returns>The requested log writer.</returns>
	public static LogWriter Get(Type type)
	{
		var builder = new StringBuilder();
		AppendName(builder, type);
		return Get(builder.ToString());

		static void AppendName(StringBuilder sb, Type t)
		{
			TypeInfo typeInfo = t.GetTypeInfo();
			if (typeInfo.IsGenericParameter)
			{
				sb.Append(typeInfo.Name);
			}
			else if (typeInfo.IsGenericType)
			{
				sb.Append(typeInfo.Namespace);
				sb.Append('.');
				Match match = sExtractGenericArgumentTypeRegex.Match(typeInfo.Name);
				sb.Append(match.Groups[1].Value);
				sb.Append('<');
				if (typeInfo.IsConstructedGenericType)
				{
					// constructed generic type
					for (int i = 0; i < typeInfo.GenericTypeArguments.Length; i++)
					{
						if (i > 0) sb.Append(',');
						AppendName(sb, typeInfo.GenericTypeArguments[i]);
					}
				}
				else
				{
					// generic type definition
					for (int i = 0; i < typeInfo.GenericTypeParameters.Length; i++)
					{
						if (i > 0) sb.Append(',');
						AppendName(sb, typeInfo.GenericTypeParameters[i]);
					}
				}

				sb.Append('>');
			}
			else
			{
				sb.Append(typeInfo.Namespace);
				sb.Append('.');
				sb.Append(typeInfo.Name);
			}
		}
	}

	/// <summary>
	/// Gets a log writer for the specified type that can be used to write to the log
	/// (the full name of the type becomes the name of the log writer).
	/// </summary>
	/// <typeparam name="T">The type whose full name is to use as the log writer name.</typeparam>
	/// <returns>The requested log writer.</returns>
	public static LogWriter Get<T>()
	{
		return Get(typeof(T));
	}

	/// <summary>
	/// Gets a log writer tag with the specified name (for internal use only).
	/// </summary>
	/// <param name="name">Name of the log writer to get.</param>
	/// <returns>The requested log writer.</returns>
	internal static LogWriterTag GetTag(string name)
	{
		sLogWriterTagsByName.TryGetValue(name, out LogWriterTag? tag);
		if (tag != null) return tag;

		lock (LogGlobals.Sync)
		{
			if (sLogWriterTagsByName.TryGetValue(name, out tag))
				return tag;

			tag = new LogWriterTag(name);

			// the id of the writer tag should correspond to the index in the list and the
			// number of elements in the dictionary.
			Debug.Assert(tag.Id == sLogWriterTagsById.Count);
			Debug.Assert(tag.Id == sLogWriterTagsByName.Count);

			// replace log writer tag list
			var newLogWriterTagsById = new List<LogWriterTag>(sLogWriterTagsById) { tag };
			Thread.MemoryBarrier(); // ensures everything has been actually written to memory at this point
			sLogWriterTagsById = newLogWriterTagsById;

			// replace log writer tag collection dictionary
			var newLogWriterTagsByName = new Dictionary<string, LogWriterTag>(sLogWriterTagsByName) { { tag.Name, tag } };
			Thread.MemoryBarrier(); // ensures everything has been actually written to memory at this point
			sLogWriterTagsByName = newLogWriterTagsByName;

			// notify about the new log writer tag
			OnNewLogWriterTagRegistered(tag);
		}

		return tag;
	}

	/// <summary>
	/// Updates the active log level mask of all log writers according to the specified configuration.<br/>
	/// This directly influences source filtering in the log writer.<br/>
	/// Do not call this method when using Griffin+ Logging as it takes care of configuring log writers!
	/// </summary>
	/// <param name="configuration">The log writer Configuration to use.</param>
	/// <exception cref="NullReferenceException"><paramref name="configuration"/> is <see langword="null"/>.</exception>
	public static void UpdateLogWriters(ILogWriterConfiguration configuration)
	{
		if (configuration == null) throw new ArgumentNullException(nameof(configuration));

		lock (LogGlobals.Sync)
		{
			sLogWriterConfiguration = configuration;
			foreach (LogWriter writer in sLogWritersById) writer.Update(configuration);
		}
	}

	/// <summary>
	/// Checks whether the specified string is a valid log writer name
	/// (log writer names may consist of all characters except line separators).
	/// </summary>
	/// <param name="name">Name to check.</param>
	/// <exception cref="ArgumentNullException">The specified name is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">The specified name is invalid.</exception>
	public static void CheckName(string name)
	{
		if (name == null) throw new ArgumentNullException(nameof(name));

		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("The specified name consists of whitespace characters only and is therefore not a valid log writer name.");

		if (name.IndexOfAny(sLineSeparators) < 0)
			return;

		string message =
			$"The specified name ({name}) is not a valid log writer name.\n" +
			"Valid names may consist of all characters except line separators.\n";
		throw new ArgumentException(message);
	}

	/// <summary>
	/// Checks whether the specified log level is active, so a message written using that level
	/// really gets into the log. The special log levels <see cref="LogLevel.None"/> and <see cref="LogLevel.All"/>
	/// are always active, but must not be used write log messages. These log levels are mapped to
	/// <see cref="LogLevel.Error"/> and get always logged - regardless of the configuration.
	/// </summary>
	/// <param name="level">Log level to check.</param>
	/// <returns>
	/// <see langword="true"/>, if the specified log level is active;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	public bool IsLogLevelActive(LogLevel level)
	{
		return level.Id is < 0 or int.MaxValue || ActiveLogLevelMask.IsBitSet(level.Id);
	}

	/// <summary>
	/// Creates a new log writer that attaches the specified tag to written log messages.
	/// </summary>
	/// <param name="tag">
	/// Tag the new log writer should attach to written log messages
	/// (<see langword="null"/> to return the current log writer).
	/// </param>
	/// <returns>A log writer that attaches the specified tag to written log messages.</returns>
	public LogWriter WithTag(string? tag)
	{
		if (tag == null) return this;
		var newTags = new LogWriterTagSet(new List<LogWriterTag>(Tags) { GetTag(tag) });
		if (newTags.Count == Tags.Count) return this;

		lock (LogGlobals.Sync)
		{
			RemoveCollectedSecondaryWriters();
			var newWriter = new LogWriter(PrimaryWriter, newTags);

			// set active log level mask, if the configuration is already initialized
			if (sLogWriterConfiguration != null) newWriter.ActiveLogLevelMask = sLogWriterConfiguration.GetActiveLogLevelMask(newWriter);

			// link new secondary log writer with its primary log writer
			Debug.Assert(PrimaryWriter.mSecondaryWriters != null, "PrimaryWriter.mSecondaryWriters != null");
			PrimaryWriter.mSecondaryWriters!.Add(new WeakReference<LogWriter>(newWriter));
			return newWriter;
		}
	}

	/// <summary>
	/// Creates a new log writer that attaches the specified tags to written log messages.
	/// </summary>
	/// <param name="tags">
	/// Tags the new log writer should attach to written log messages
	/// (<see langword="null"/> to return the current log writer).
	/// </param>
	/// <returns>A log writer that attaches the specified tags to written log messages.</returns>
	public LogWriter WithTags(params string[]? tags)
	{
		if (tags == null) return this;
		var tagList = new List<LogWriterTag>(Tags);
		tagList.AddRange(tags.Select(GetTag));
		var newTags = new LogWriterTagSet(tagList);
		if (newTags.Count == Tags.Count) return this;

		lock (LogGlobals.Sync)
		{
			RemoveCollectedSecondaryWriters();
			var newWriter = new LogWriter(PrimaryWriter, newTags);

			// set active log level mask, if the configuration is already initialized
			if (sLogWriterConfiguration != null) newWriter.ActiveLogLevelMask = sLogWriterConfiguration.GetActiveLogLevelMask(newWriter);

			// link new secondary log writer with its primary log writer
			Debug.Assert(PrimaryWriter.mSecondaryWriters != null, "PrimaryWriter.mSecondaryWriters != null");
			PrimaryWriter.mSecondaryWriters!.Add(new WeakReference<LogWriter>(newWriter));
			return newWriter;
		}
	}

	/// <summary>
	/// Updates the log writer and associated secondary log writers.
	/// </summary>
	/// <param name="configuration">The log configuration.</param>
	private void Update(ILogWriterConfiguration configuration)
	{
		Debug.Assert(Monitor.IsEntered(LogGlobals.Sync));
		ActiveLogLevelMask = configuration.GetActiveLogLevelMask(this);

		if (mSecondaryWriters == null)
			return;

		for (int i = mSecondaryWriters.Count - 1; i >= 0; i--)
		{
			if (mSecondaryWriters[i].TryGetTarget(out LogWriter? writer))
			{
				writer.Update(configuration);
			}
			else
			{
				// the secondary log writer was collected
				// => remove it from the list
				mSecondaryWriters.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// Removes secondary log writers that have been collected meanwhile.
	/// </summary>
	private void RemoveCollectedSecondaryWriters()
	{
		if (mSecondaryWriters == null)
			return;

		for (int i = mSecondaryWriters.Count - 1; i >= 0; i--)
		{
			if (!mSecondaryWriters[i].TryGetTarget(out LogWriter? _))
			{
				mSecondaryWriters.RemoveAt(i);
			}
		}
	}

	/// <summary>
	/// Writes a message to the log.
	/// </summary>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="message">Message to write to the log.</param>
	public void Write(LogLevel level, string message)
	{
		if (IsLogLevelActive(level))
		{
			OnLogMessageWritten(this, level, message);
		}
	}

	/// <summary>
	/// Writes a formatted message with a variable number of placeholders to the log.
	/// </summary>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="args">Arguments to put into the placeholders.</param>
	public void Write(LogLevel level, string format, params object[] args)
	{
		Write(sDefaultFormatProvider, level, format, args);
	}

	/// <summary>
	/// Writes a formatted message with a variable number of placeholders to the log
	/// (bypasses filters induced by the log configuration).
	/// </summary>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="args">Arguments to put into the placeholders.</param>
	public void ForceWrite(LogLevel level, string format, params object[] args)
	{
		ForceWrite(sDefaultFormatProvider, level, format, args);
	}

	/// <summary>
	/// Writes a formatted message with one placeholder to the log.
	/// </summary>
	/// <typeparam name="T">Type of the placeholder.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg">Argument to put into placeholder {0}.</param>
	public void Write<T>(LogLevel level, string format, T arg)
	{
		Write(sDefaultFormatProvider, level, format, arg);
	}

	/// <summary>
	/// Writes a formatted message with two placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	public void Write<T0, T1>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1);
	}

	/// <summary>
	/// Writes a formatted message with three placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	public void Write<T0, T1, T2>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2);
	}

	/// <summary>
	/// Writes a formatted message with four placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	public void Write<T0, T1, T2, T3>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3);
	}

	/// <summary>
	/// Writes a formatted message with five placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	public void Write<T0, T1, T2, T3, T4>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4);
	}

	/// <summary>
	/// Writes a formatted message with six placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	public void Write<T0, T1, T2, T3, T4, T5>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4, arg5);
	}

	/// <summary>
	/// Writes a formatted message with seven placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5,
		T6       arg6)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6);
	}

	/// <summary>
	/// Writes a formatted message with eight placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5,
		T6       arg6,
		T7       arg7)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
	}

	/// <summary>
	/// Writes a formatted message with nine placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5,
		T6       arg6,
		T7       arg7,
		T8       arg8)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
	}

	/// <summary>
	/// Writes a formatted message with ten placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5,
		T6       arg6,
		T7       arg7,
		T8       arg8,
		T9       arg9)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9);
	}

	/// <summary>
	/// Writes a formatted message with eleven placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5,
		T6       arg6,
		T7       arg7,
		T8       arg8,
		T9       arg9,
		T10      arg10)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10);
	}

	/// <summary>
	/// Writes a formatted message with twelve placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <typeparam name="T11">Type of placeholder 11.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	/// <param name="arg11">Argument to put into placeholder {11}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5,
		T6       arg6,
		T7       arg7,
		T8       arg8,
		T9       arg9,
		T10      arg10,
		T11      arg11)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11);
	}

	/// <summary>
	/// Writes a formatted message with 13 placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <typeparam name="T11">Type of placeholder 11.</typeparam>
	/// <typeparam name="T12">Type of placeholder 12.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	/// <param name="arg11">Argument to put into placeholder {11}.</param>
	/// <param name="arg12">Argument to put into placeholder {12}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5,
		T6       arg6,
		T7       arg7,
		T8       arg8,
		T9       arg9,
		T10      arg10,
		T11      arg11,
		T12      arg12)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12);
	}

	/// <summary>
	/// Writes a formatted message with 14 placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <typeparam name="T11">Type of placeholder 11.</typeparam>
	/// <typeparam name="T12">Type of placeholder 12.</typeparam>
	/// <typeparam name="T13">Type of placeholder 13.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	/// <param name="arg11">Argument to put into placeholder {11}.</param>
	/// <param name="arg12">Argument to put into placeholder {12}.</param>
	/// <param name="arg13">Argument to put into placeholder {13}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5,
		T6       arg6,
		T7       arg7,
		T8       arg8,
		T9       arg9,
		T10      arg10,
		T11      arg11,
		T12      arg12,
		T13      arg13)
	{
		Write(sDefaultFormatProvider, level, format, arg0, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13);
	}

	/// <summary>
	/// Writes a formatted message with 15 placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <typeparam name="T11">Type of placeholder 11.</typeparam>
	/// <typeparam name="T12">Type of placeholder 12.</typeparam>
	/// <typeparam name="T13">Type of placeholder 13.</typeparam>
	/// <typeparam name="T14">Type of placeholder 14.</typeparam>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	/// <param name="arg11">Argument to put into placeholder {11}.</param>
	/// <param name="arg12">Argument to put into placeholder {12}.</param>
	/// <param name="arg13">Argument to put into placeholder {13}.</param>
	/// <param name="arg14">Argument to put into placeholder {14}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
		LogLevel level,
		string   format,
		T0       arg0,
		T1       arg1,
		T2       arg2,
		T3       arg3,
		T4       arg4,
		T5       arg5,
		T6       arg6,
		T7       arg7,
		T8       arg8,
		T9       arg9,
		T10      arg10,
		T11      arg11,
		T12      arg12,
		T13      arg13,
		T14      arg14)
	{
		Write(
			sDefaultFormatProvider,
			level,
			format,
			arg0,
			arg1,
			arg2,
			arg3,
			arg4,
			arg5,
			arg6,
			arg7,
			arg8,
			arg9,
			arg10,
			arg11,
			arg12,
			arg13,
			arg14);
	}

	/// <summary>
	/// Writes a formatted message with a variable number of placeholders to the log.
	/// </summary>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="args">Arguments to put into the placeholders.</param>
	public void Write(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		params object[] args)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object[] modifiedArgs = args;
		for (int i = 0; i < modifiedArgs.Length; i++)
		{
			object obj = modifiedArgs[i];
			if (obj is not Exception exception) continue;
			if (modifiedArgs == args) modifiedArgs = (object[])args.Clone();
			modifiedArgs[i] = UnwrapException(exception);
		}

		args = modifiedArgs;

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, args);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with a variable number of placeholders to the log
	/// (bypasses filters induced by the log configuration).
	/// </summary>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="args">Arguments to put into the placeholders.</param>
	public void ForceWrite(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		params object[] args)
	{
		// unwrap exceptions to ensure inner exceptions are logged as well
		object[] modifiedArgs = args;
		for (int i = 0; i < modifiedArgs.Length; i++)
		{
			object obj = modifiedArgs[i];
			if (obj is not Exception exception) continue;
			if (modifiedArgs == args) modifiedArgs = (object[])args.Clone();
			modifiedArgs[i] = UnwrapException(exception);
		}

		args = modifiedArgs;

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, args);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with one placeholder to the log.
	/// </summary>
	/// <typeparam name="T">Type of the placeholder.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg">Argument to put into placeholder {0}.</param>
	public void Write<T>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T               arg)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg = PrepareArgument(arg);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with two placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	public void Write<T0, T1>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with three placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	public void Write<T0, T1, T2>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with four placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	public void Write<T0, T1, T2, T3>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with five placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	public void Write<T0, T1, T2, T3, T4>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3, carg4);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with six placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	public void Write<T0, T1, T2, T3, T4, T5>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3, carg4, carg5);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with seven placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5,
		T6              arg6)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);
		object? carg6 = PrepareArgument(arg6);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3, carg4, carg5, carg6);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with eight placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5,
		T6              arg6,
		T7              arg7)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);
		object? carg6 = PrepareArgument(arg6);
		object? carg7 = PrepareArgument(arg7);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3, carg4, carg5, carg6, carg7);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with nine placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5,
		T6              arg6,
		T7              arg7,
		T8              arg8)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);
		object? carg6 = PrepareArgument(arg6);
		object? carg7 = PrepareArgument(arg7);
		object? carg8 = PrepareArgument(arg8);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3, carg4, carg5, carg6, carg7, carg8);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with ten placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5,
		T6              arg6,
		T7              arg7,
		T8              arg8,
		T9              arg9)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);
		object? carg6 = PrepareArgument(arg6);
		object? carg7 = PrepareArgument(arg7);
		object? carg8 = PrepareArgument(arg8);
		object? carg9 = PrepareArgument(arg9);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3, carg4, carg5, carg6, carg7, carg8, carg9);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with eleven placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5,
		T6              arg6,
		T7              arg7,
		T8              arg8,
		T9              arg9,
		T10             arg10)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);
		object? carg6 = PrepareArgument(arg6);
		object? carg7 = PrepareArgument(arg7);
		object? carg8 = PrepareArgument(arg8);
		object? carg9 = PrepareArgument(arg9);
		object? carg10 = PrepareArgument(arg10);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3, carg4, carg5, carg6, carg7, carg8, carg9, carg10);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with twelve placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <typeparam name="T11">Type of placeholder 11.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	/// <param name="arg11">Argument to put into placeholder {11}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5,
		T6              arg6,
		T7              arg7,
		T8              arg8,
		T9              arg9,
		T10             arg10,
		T11             arg11)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);
		object? carg6 = PrepareArgument(arg6);
		object? carg7 = PrepareArgument(arg7);
		object? carg8 = PrepareArgument(arg8);
		object? carg9 = PrepareArgument(arg9);
		object? carg10 = PrepareArgument(arg10);
		object? carg11 = PrepareArgument(arg11);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3, carg4, carg5, carg6, carg7, carg8, carg9, carg10, carg11);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with 13 placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <typeparam name="T11">Type of placeholder 11.</typeparam>
	/// <typeparam name="T12">Type of placeholder 12.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	/// <param name="arg11">Argument to put into placeholder {11}.</param>
	/// <param name="arg12">Argument to put into placeholder {12}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5,
		T6              arg6,
		T7              arg7,
		T8              arg8,
		T9              arg9,
		T10             arg10,
		T11             arg11,
		T12             arg12)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);
		object? carg6 = PrepareArgument(arg6);
		object? carg7 = PrepareArgument(arg7);
		object? carg8 = PrepareArgument(arg8);
		object? carg9 = PrepareArgument(arg9);
		object? carg10 = PrepareArgument(arg10);
		object? carg11 = PrepareArgument(arg11);
		object? carg12 = PrepareArgument(arg12);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(provider, format, carg0, carg1, carg2, carg3, carg4, carg5, carg6, carg7, carg8, carg9, carg10, carg11, carg12);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with 14 placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <typeparam name="T11">Type of placeholder 11.</typeparam>
	/// <typeparam name="T12">Type of placeholder 12.</typeparam>
	/// <typeparam name="T13">Type of placeholder 13.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	/// <param name="arg11">Argument to put into placeholder {11}.</param>
	/// <param name="arg12">Argument to put into placeholder {12}.</param>
	/// <param name="arg13">Argument to put into placeholder {13}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5,
		T6              arg6,
		T7              arg7,
		T8              arg8,
		T9              arg9,
		T10             arg10,
		T11             arg11,
		T12             arg12,
		T13             arg13)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);
		object? carg6 = PrepareArgument(arg6);
		object? carg7 = PrepareArgument(arg7);
		object? carg8 = PrepareArgument(arg8);
		object? carg9 = PrepareArgument(arg9);
		object? carg10 = PrepareArgument(arg10);
		object? carg11 = PrepareArgument(arg11);
		object? carg12 = PrepareArgument(arg12);
		object? carg13 = PrepareArgument(arg13);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(
			provider,
			format,
			carg0,
			carg1,
			carg2,
			carg3,
			carg4,
			carg5,
			carg6,
			carg7,
			carg8,
			carg9,
			carg10,
			carg11,
			carg12,
			carg13);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Writes a formatted message with 15 placeholders to the log.
	/// </summary>
	/// <typeparam name="T0">Type of placeholder 0.</typeparam>
	/// <typeparam name="T1">Type of placeholder 1.</typeparam>
	/// <typeparam name="T2">Type of placeholder 2.</typeparam>
	/// <typeparam name="T3">Type of placeholder 3.</typeparam>
	/// <typeparam name="T4">Type of placeholder 4.</typeparam>
	/// <typeparam name="T5">Type of placeholder 5.</typeparam>
	/// <typeparam name="T6">Type of placeholder 6.</typeparam>
	/// <typeparam name="T7">Type of placeholder 7.</typeparam>
	/// <typeparam name="T8">Type of placeholder 8.</typeparam>
	/// <typeparam name="T9">Type of placeholder 9.</typeparam>
	/// <typeparam name="T10">Type of placeholder 10.</typeparam>
	/// <typeparam name="T11">Type of placeholder 11.</typeparam>
	/// <typeparam name="T12">Type of placeholder 12.</typeparam>
	/// <typeparam name="T13">Type of placeholder 13.</typeparam>
	/// <typeparam name="T14">Type of placeholder 14.</typeparam>
	/// <param name="provider">Format provider to use when formatting the message.</param>
	/// <param name="level">Log level to write the message to.</param>
	/// <param name="format">A composite format string containing placeholders (formatting as usual in .NET).</param>
	/// <param name="arg0">Argument to put into placeholder {0}.</param>
	/// <param name="arg1">Argument to put into placeholder {1}.</param>
	/// <param name="arg2">Argument to put into placeholder {2}.</param>
	/// <param name="arg3">Argument to put into placeholder {3}.</param>
	/// <param name="arg4">Argument to put into placeholder {4}.</param>
	/// <param name="arg5">Argument to put into placeholder {5}.</param>
	/// <param name="arg6">Argument to put into placeholder {6}.</param>
	/// <param name="arg7">Argument to put into placeholder {7}.</param>
	/// <param name="arg8">Argument to put into placeholder {8}.</param>
	/// <param name="arg9">Argument to put into placeholder {9}.</param>
	/// <param name="arg10">Argument to put into placeholder {10}.</param>
	/// <param name="arg11">Argument to put into placeholder {11}.</param>
	/// <param name="arg12">Argument to put into placeholder {12}.</param>
	/// <param name="arg13">Argument to put into placeholder {13}.</param>
	/// <param name="arg14">Argument to put into placeholder {14}.</param>
	public void Write<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(
		IFormatProvider provider,
		LogLevel        level,
		string          format,
		T0              arg0,
		T1              arg1,
		T2              arg2,
		T3              arg3,
		T4              arg4,
		T5              arg5,
		T6              arg6,
		T7              arg7,
		T8              arg8,
		T9              arg9,
		T10             arg10,
		T11             arg11,
		T12             arg12,
		T13             arg13,
		T14             arg14)
	{
		if (!IsLogLevelActive(level))
			return;

		// unwrap exceptions to ensure inner exceptions are logged as well
		object? carg0 = PrepareArgument(arg0);
		object? carg1 = PrepareArgument(arg1);
		object? carg2 = PrepareArgument(arg2);
		object? carg3 = PrepareArgument(arg3);
		object? carg4 = PrepareArgument(arg4);
		object? carg5 = PrepareArgument(arg5);
		object? carg6 = PrepareArgument(arg6);
		object? carg7 = PrepareArgument(arg7);
		object? carg8 = PrepareArgument(arg8);
		object? carg9 = PrepareArgument(arg9);
		object? carg10 = PrepareArgument(arg10);
		object? carg11 = PrepareArgument(arg11);
		object? carg12 = PrepareArgument(arg12);
		object? carg13 = PrepareArgument(arg13);
		object? carg14 = PrepareArgument(arg14);

		// format message and raise the event to notify clients of the written message
		StringBuilder builder = sBuilder.Value!;
		builder.Clear();
		builder.AppendFormat(
			provider,
			format,
			carg0,
			carg1,
			carg2,
			carg3,
			carg4,
			carg5,
			carg6,
			carg7,
			carg8,
			carg9,
			carg10,
			carg11,
			carg12,
			carg13,
			carg14);
		OnLogMessageWritten(this, level, builder.ToString());
	}

	/// <summary>
	/// Converts the specified argument into a form that can be fed into the logging subsystem (e.g. unwraps exceptions).
	/// </summary>
	/// <typeparam name="T">Type of the argument.</typeparam>
	/// <param name="arg">Argument to prepare.</param>
	/// <returns>Object to feed into the logging subsystem.</returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static object? PrepareArgument<T>(T arg)
	{
		if (arg is Exception exception) return UnwrapException(exception);
		return arg;
	}

	/// <summary>
	/// Gets a string containing the type, the message and the stacktrace of the specified exception and all of its inner exception.
	/// </summary>
	/// <param name="exception"></param>
	/// <returns>The exception in a properly formatted fashion.</returns>
	public static string UnwrapException(Exception exception)
	{
		var builder = new StringBuilder();
		Build(builder, exception, indent: 0);
		return builder.ToString();

		static void Build(StringBuilder builder, Exception exception, int indent)
		{
			// start of exception
			AppendIndentation(builder, indent);
			builder.AppendLine(
				indent == 0
					? "--- Exception ---------------------------------------------------------------------------------------------"
					: "--- Inner Exception ---------------------------------------------------------------------------------------");

			// exception type
			AppendIndentation(builder, indent);
			builder.Append("--- Exception Type: ");
			builder.AppendLine(exception.GetType().FullName);

			string[] lines = exception.Message.Split(sNewlineTokens, StringSplitOptions.None);
			if (lines.Length > 1)
			{
				// multi-line message
				AppendIndentation(builder, indent);
				builder.AppendLine("--- Message:");
				foreach (string line in exception.Message.Split(sNewlineTokens, StringSplitOptions.None))
				{
					AppendIndentation(builder, indent);
					builder.Append(' ', 4);
					builder.AppendLine(line);
				}
			}
			else
			{
				// single-line message
				AppendIndentation(builder, indent);
				builder.Append("--- Message: ");
				builder.AppendLine(lines.Length > 0 ? lines[0] : "");
			}

			// stack trace
			if (exception.StackTrace != null)
			{
				AppendIndentation(builder, indent);
				builder.AppendLine("--- Stacktrace:");
				foreach (string line in exception.StackTrace.Split(sNewlineTokens, StringSplitOptions.None))
				{
					AppendIndentation(builder, indent);
					builder.Append(' ', 4);
					builder.AppendLine(line);
				}
			}

			// inner exceptions
			if (exception is AggregateException aggregateException)
			{
				foreach (Exception innerException in aggregateException.InnerExceptions)
				{
					builder.AppendLine();
					Build(builder, innerException, indent: indent + 1);
				}
			}
			else if (exception.InnerException != null)
			{
				builder.AppendLine();
				// ReSharper disable once TailRecursiveCall
				Build(builder, exception.InnerException, indent: indent + 1);
			}
		}

		static void AppendIndentation(StringBuilder builder, int indent)
		{
			for (int i = 0; i < indent; i++) builder.Append("> ");
		}
	}

	/// <summary>
	/// Gets the string representation of the log writer.
	/// </summary>
	/// <returns>String representation of the log writer.</returns>
	public override string ToString()
	{
		return $"{Name} ({Id})";
	}

	/// <summary>
	/// Raises the <see cref="NewLogWriterRegistered"/> event.
	/// The global logging lock (<see cref="LogGlobals.Sync"/>) must be acquired when raising the event.
	/// </summary>
	/// <param name="writer">The new log writer.</param>
	private static void OnNewLogWriterRegistered(LogWriter writer)
	{
		Debug.Assert(Monitor.IsEntered(LogGlobals.Sync));
		LogWriterRegisteredEventHandler? handler = NewLogWriterRegistered;
		handler?.Invoke(writer);
	}

	/// <summary>
	/// Raises the <see cref="NewLogWriterTagRegistered"/> event.
	/// The global logging lock (<see cref="LogGlobals.Sync"/>) must be acquired when raising the event.
	/// </summary>
	/// <param name="tag">The new log writer tag.</param>
	private static void OnNewLogWriterTagRegistered(LogWriterTag tag)
	{
		Debug.Assert(Monitor.IsEntered(LogGlobals.Sync));
		LogWriterTagRegisteredEventHandler? handler = NewLogWriterTagRegistered;
		handler?.Invoke(tag);
	}

	/// <summary>
	/// Raises the <see cref="LogMessageWritten"/> event.
	/// The global logging lock (<see cref="LogGlobals.Sync"/>) must _not_ be acquired when raising the event.
	/// </summary>
	/// <param name="writer">The log writer that writes the message.</param>
	/// <param name="level">The log level that is associated with the message.</param>
	/// <param name="message">The message text.</param>
	private static void OnLogMessageWritten(LogWriter writer, LogLevel level, string message)
	{
		Debug.Assert(!Monitor.IsEntered(LogGlobals.Sync));
		LogMessageWrittenEventHandler? handler = LogMessageWritten;
		handler?.Invoke(writer, level, message);
	}
}
