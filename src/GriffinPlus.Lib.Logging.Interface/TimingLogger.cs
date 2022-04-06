///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

// ReSharper disable UnusedMember.Global

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// A timing logger that can be used to measure the time that elapses between its construction and its disposal.
	/// </summary>
	public class TimingLogger : IDisposable
	{
		private static readonly ConcurrentBag<TimingLogger> sPool             = new ConcurrentBag<TimingLogger>();
		private static readonly LogWriter                   sDefaultLogWriter = LogWriter.Get("Timing");
		private static readonly LogLevel                    sDefaultLogLevel  = LogLevel.Timing;
		private static          int                         sNextTimingLoggerId;
		private                 long                        mTimestamp;
		private                 LogWriter                   mLogWriter;
		private                 LogLevel                    mLogLevel;
		private                 string                      mOperation;
		private                 string                      mThreadName;
		private                 int                         mTimingLoggerId;
		private                 int                         mManagedThreadId;
		private                 bool                        mActive;

		/// <summary>
		/// Initializes a new instance of the <see cref="TimingLogger"/> class.
		/// </summary>
		private TimingLogger() { }

		/// <summary>
		/// Disposes the timing logger emitting a log message that notifies about the time since the timing logger was created.
		/// </summary>
		public void Dispose()
		{
			if (mActive)
			{
				double elapsed = (double)(Stopwatch.GetTimestamp() - mTimestamp) / Stopwatch.Frequency;
				WriteEndMessage(elapsed);
				mActive = false;
				sPool.Add(this);
			}
		}

		/// <summary>
		/// Gets a timing logger using the default log writer 'Timing' and the default log level 'Timing'.
		/// </summary>
		/// <param name="operation">Name of the operation that is being measured.</param>
		public static TimingLogger Measure(string operation = null)
		{
			if (!sPool.TryTake(out var logger)) logger = new TimingLogger();
			logger.mLogWriter = sDefaultLogWriter;
			logger.mLogLevel = sDefaultLogLevel;
			logger.mOperation = operation;
			logger.mTimingLoggerId = Interlocked.Increment(ref sNextTimingLoggerId);
			logger.mThreadName = Thread.CurrentThread.Name;
			logger.mManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			logger.mActive = true;
			logger.WriteStartMessage();
			logger.mTimestamp = Stopwatch.GetTimestamp();
			return logger;
		}

		/// <summary>
		/// Gets a timing logger using the specified log level and the default log writer 'Timing' to emit timing related log messages.
		/// </summary>
		/// <param name="level">Log level to use.</param>
		/// <param name="operation">Name of the operation that is being measured.</param>
		public static TimingLogger Measure(LogLevel level, string operation = null)
		{
			if (!sPool.TryTake(out var logger)) logger = new TimingLogger();
			logger.mLogWriter = sDefaultLogWriter;
			logger.mLogLevel = level;
			logger.mOperation = operation;
			logger.mTimingLoggerId = Interlocked.Increment(ref sNextTimingLoggerId);
			logger.mThreadName = Thread.CurrentThread.Name;
			logger.mManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			logger.mActive = true;
			logger.WriteStartMessage();
			logger.mTimestamp = Stopwatch.GetTimestamp();
			return logger;
		}

		/// <summary>
		/// Gets a timing logger using the specified log writer and the default aspect log level 'Timing' to emit timing related log messages.
		/// </summary>
		/// <param name="writer">Log writer to use.</param>
		/// <param name="operation">Name of the operation that is being measured.</param>
		public static TimingLogger Measure(LogWriter writer, string operation = null)
		{
			if (!sPool.TryTake(out var logger)) logger = new TimingLogger();
			logger.mLogWriter = writer;
			logger.mLogLevel = sDefaultLogLevel;
			logger.mOperation = operation;
			logger.mTimingLoggerId = Interlocked.Increment(ref sNextTimingLoggerId);
			logger.mThreadName = Thread.CurrentThread.Name;
			logger.mManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			logger.mActive = true;
			logger.WriteStartMessage();
			logger.mTimestamp = Stopwatch.GetTimestamp();
			return logger;
		}

		/// <summary>
		/// Gets a timing logger using the specified log writer and log level to emit timing related log messages.
		/// </summary>
		/// <param name="writer">Log writer to use.</param>
		/// <param name="level">Log level to use.</param>
		/// <param name="operation">Name of the operation that is being measured.</param>
		public static TimingLogger Measure(LogWriter writer, LogLevel level, string operation = null)
		{
			if (!sPool.TryTake(out var logger)) logger = new TimingLogger();
			logger.mLogWriter = writer;
			logger.mLogLevel = level;
			logger.mOperation = operation;
			logger.mTimingLoggerId = Interlocked.Increment(ref sNextTimingLoggerId);
			logger.mThreadName = Thread.CurrentThread.Name;
			logger.mManagedThreadId = Thread.CurrentThread.ManagedThreadId;
			logger.mActive = true;
			logger.WriteStartMessage();
			logger.mTimestamp = Stopwatch.GetTimestamp();
			return logger;
		}

		/// <summary>
		/// Writes a log message indicating that the measured operation is starting.
		/// </summary>
		private void WriteStartMessage()
		{
			if (mOperation != null)
			{
				if (!string.IsNullOrWhiteSpace(mThreadName))
				{
					mLogWriter.Write(
						mLogLevel,
						"Timing ({0}|{1}|{2}): Starting operation ({3}).",
						mTimingLoggerId,
						mManagedThreadId,
						mThreadName,
						mOperation);
				}
				else
				{
					mLogWriter.Write(
						mLogLevel,
						"Timing ({0}|{1}): Starting operation ({2}).",
						mTimingLoggerId,
						mManagedThreadId,
						mOperation);
				}
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(mThreadName))
				{
					mLogWriter.Write(
						mLogLevel,
						"Timing ({0}|{1}|{2}): Starting operation.",
						mTimingLoggerId,
						mManagedThreadId,
						mThreadName);
				}
				else
				{
					mLogWriter.Write(
						mLogLevel,
						"Timing ({0}|{1}): Starting operation.",
						mTimingLoggerId,
						mManagedThreadId);
				}
			}
		}

		/// <summary>
		/// Writes a log message indicating that the measured operation has finished.
		/// </summary>
		/// <param name="elapsed">Duration the measured operation took (in seconds).</param>
		private void WriteEndMessage(double elapsed)
		{
			elapsed *= 1000.0; // convert to ms

			if (mOperation != null)
			{
				if (!string.IsNullOrWhiteSpace(mThreadName))
				{
					mLogWriter.Write(
						mLogLevel,
						"Timing ({0}|{1}|{2}): Operation ({3}) completed [{4:0.0000} ms].",
						mTimingLoggerId,
						mManagedThreadId,
						mThreadName,
						mOperation,
						elapsed);
				}
				else
				{
					mLogWriter.Write(
						mLogLevel,
						"Timing ({0}|{1}): Operation ({2}) completed [{3:0.0000} ms].",
						mTimingLoggerId,
						mManagedThreadId,
						mOperation,
						elapsed);
				}
			}
			else
			{
				if (!string.IsNullOrWhiteSpace(mThreadName))
				{
					mLogWriter.Write(
						mLogLevel,
						"Timing ({0}|{1}|{2}): Operation completed [{3:0.0000} ms].",
						mTimingLoggerId,
						mManagedThreadId,
						mThreadName,
						elapsed);
				}
				else
				{
					mLogWriter.Write(
						mLogLevel,
						"Timing ({0}|{1}): Operation completed [{2:0.0000} ms].",
						mTimingLoggerId,
						mManagedThreadId,
						elapsed);
				}
			}
		}
	}

}
