///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Threading;

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedParameter.Local
// ReSharper disable UnusedVariable

namespace GriffinPlus.Lib.Logging.Demo;

class MyClass1;

class MyClass2;

class Program
{
	// Register log writers using types.
	private static readonly LogWriter sLog1 = LogWriter.Get<MyClass1>();       // actual log writer name: GriffinPlus.Lib.Logging.Demo.MyClass1
	private static readonly LogWriter sLog2 = LogWriter.Get(typeof(MyClass2)); // actual log writer name: GriffinPlus.Lib.Logging.Demo.MyClass2

	// Register a log writer using a custom name.
	private static readonly LogWriter sLog3 = LogWriter.Get("My Fancy Writer");

	// Create tagging log writers
	private static readonly LogWriter sLog_TagA  = sLog3.WithTag("TagA");          // same as sLog7, but tags messages with 'TagA'
	private static readonly LogWriter sLog_TagB  = sLog3.WithTag("TagB");          // same as sLog7, but tags messages with 'TagB'
	private static readonly LogWriter sLog_TagBC = sLog3.WithTags("TagB", "TagC"); // same as sLog7, but tags messages with 'TagB' and 'TagC'

	private static void Main(string[] args)
	{
		// attach handler to call when writing a log message
		// (not needed when running along with the full-featured Griffin+ logging system)
		LogWriter.LogMessageWritten += (writer, level, message) =>
		{
			Console.WriteLine();
			Console.WriteLine("Writer:    {0}", writer.Name);
			if (writer.Tags.Count > 0) Console.WriteLine("Tags:      {0}", string.Join(", ", writer.Tags));
			Console.WriteLine("Level:     {0}", level.Name);
			Console.WriteLine("Message:   {0}", message);
		};

		// Get an aspect log level.
		LogLevel aspect = LogLevel.GetAspect("Demo Aspect");

		// Write messages to all known log levels (predefined log levels + aspects).
		foreach (LogLevel level in LogLevel.KnownLevels)
		{
			sLog1.Write(level, "This is sLog1 writing using level '{0}'.", level.Name);
			sLog2.Write(level, "This is sLog2 writing using level '{0}'.", level.Name);
			sLog3.Write(level, "This is sLog3 writing using level '{0}'.", level.Name);
			sLog_TagA.Write(level, "This is sLog_TagA writing using level '{0}'.", level.Name);
			sLog_TagB.Write(level, "This is sLog_TagB writing using level '{0}'.", level.Name);
			sLog_TagBC.Write(level, "This is sLog_TagBC writing using level '{0}'.", level.Name);
		}

		// Use a timing logger to determine how long an operation takes. It uses log level 'Timing' and log writer
		// 'Timing' by default, so you need to ensure that the configuration lets these messages pass.
		sLog1.Write(LogLevel.Notice, "Presenting a timing logger with default settings...");
		using (TimingLogger.Measure())
		{
			Thread.Sleep(500);
		}

		// Use a timing logger, customize the log writer/level it uses and associate an operation name with the
		// measurement that is printed to the log as well.
		sLog1.Write(LogLevel.Notice, "A timing logger with custom log level/writer and operation name...");
		using (TimingLogger.Measure(sLog1, LogLevel.Notice, "Waiting for 500ms"))
		{
			Thread.Sleep(500);
		}

		Console.WriteLine();
		Console.WriteLine("Press any key...");
		Console.ReadKey();
	}
}
