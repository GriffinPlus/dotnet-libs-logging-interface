# Griffin + Logging Interface

[![Azure DevOps builds (branch)](https://img.shields.io/azure-devops/build/griffinplus/2f589a5e-e2ab-4c08-bee5-5356db2b2aeb/37/master?label=Build)](https://dev.azure.com/griffinplus/DotNET%20Libraries/_build/latest?definitionId=37&branchName=master)
[![Tests (master)](https://img.shields.io/azure-devops/tests/griffinplus/DotNET%20Libraries/37/master?label=Tests)](https://dev.azure.com/griffinplus/DotNET%20Libraries/_build/latest?definitionId=37&branchName=master)

| NuGet Package                                                                                                             |                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
|---------------------------------------------------------------------------------------------------------------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [GriffinPlus.Lib.Logging.Interface](README.md)                                                                            | [![NuGet Version](https://img.shields.io/nuget/v/GriffinPlus.Lib.Logging.Interface.svg?label=Version)](https://www.nuget.org/packages/GriffinPlus.Lib.Logging.Interface) [![NuGet Downloads](https://img.shields.io/nuget/dt/GriffinPlus.Lib.Logging.Interface.svg?label=Downloads)](https://www.nuget.org/packages/GriffinPlus.Lib.Logging.Interface)

## Overview

*Griffin+ Logging* is a simple, but modular extensible logging facility that focusses on applications built on the .NET framework. It addresses many issues that have arised during multiple years of developing .NET libraries and applications. *Griffin+ Logging* is part of the *Griffin+* library suite and used in other *Griffin+* projects to channelize and process log message streams. This repository contains the interface of *Griffin+ Logging* only. The interface is very stable and should be used when writing libraries. Applications should use the full-featured [Griffin+ Logging](https://github.com/GriffinPlus/dotnet-libs-logging) instead. It integrates seamlessly with the interface and does the heavy lifting. While the interface is very stable, the full-featured [Griffin+ Logging](https://github.com/GriffinPlus/dotnet-libs-logging) receives more updates over time.

## Supported Platforms

The library is entirely written in C# using .NET Standard 2.0.

Therefore it should work on the following platforms (or higher):
- .NET Framework 4.6.1
- .NET Core 2.0
- .NET 5.0
- Mono 5.4
- Xamarin iOS 10.14
- Xamarin Mac 3.8
- Xamarin Android 8.0
- Universal Windows Platform (UWP) 10.0.16299

The library is tested automatically on the following frameworks and operating systems:
- .NET Framework 4.6.1 (Windows Server 2019)
- .NET Core 3.1 (Windows Server 2019 and Ubuntu 20.04)
- .NET 5.0 (Windows Server 2019 and Ubuntu 20.04)

## Coarse Overview and Terminology

The *Griffin+ Logging Interface* consists of a couple of classes defined in the `GriffinPlus.Lib.Logging` namespace. The main pillars are the following classes:

- `LogWriter`
- `LogLevel`

A log writer - represented by the `LogWriter` class - corresponds to a certain piece of code emitting log messages. Log writers are identified by their name. Usually this is the full name of a class, but it can be an arbitrary name as well. The `LogWriter` class offers a large set of overloads for writing simple and formatted messages. Generic overloads avoid boxing of value types when passing arguments to the `Write(...)` methods, thus reducing the load on the garbage collection.

Each and every log message is associated with a log level represented by the `LogLevel` class. A log level indicates how severe the issue is the message is about. A log level has an integer id that expresses the severity: the lower the log level id the more severe the issue. The log levels correspond to log levels known from *syslog*. Their log level id is the same as the corresponding severity level used by *syslog*, so it is easy to integrate *Griffin+ Logging* into an existing logging infrastructure. The `LogLevel` class contains a few commonly used predefined log levels and a recommendation when these log levels should be used:


| Id  | Log Level       | Description
|:---:|:----------------|:----------------------------------------------------------------------------------------------
|   0 | `Emergency`     | Absolute "panic" condition, the system is unusable.
|   1 | `Alert`         | Something bad happened, immediate attention is required.
|   2 | `Critical`      | Something bad is about to happen, immediate attention is required.
|   3 | `Error`         | Non-urgent failure in the system that needs attention.
|   4 | `Warning`       | Something will happen if it is not dealt within a timeframe.
|   5 | `Notice`        | Normal but significant condition that might need special handling.
|   6 | `Informational` | Informative but not important.
|   7 | `Debug`         | Only relevant for developers.
|   8 | `Trace`         | Only relevant for implementers.

In addition to the predefined log levels an aspect log level can be defined. Aspect log levels are primarily useful when tracking an issue that effects multiple classes. The ids of aspect log levels directly follow the predefined log levels.

Another option is to categorize log messages using *tagging*. Log writers can be configured to attach tags to written log messages. In addition to just matching the name of a log writer tags provide a flexible way to group log writers, e.g. to express their membership to a subsystem. This enables writing applications that can configure logging on a per-subsystem basis without losing the original name of the log writer (which is almost always the name of the class the log writer belongs to).

## Using

### Requesting a Log Writer

If you want to write a message to the log, you first have to request a `LogWriter`. A log writer provides various ways of formatting log messages. It is perfectly fine to keep a single `LogWriter` instance in a static member variable as log writers are thread-safe, so you only need one instance for multiple threads. A log writer has a unique name usually identifying the piece of code that emits log messages. This is often the name of the class, although the name can be chosen freely. In this case you can simply pass the type of the corresponding class when requesting a log writer. The name will automatically be set to the full name of the specified type. A positive side effect of using a type is that the name of the log writer changes, if the name of the type changes or even the namespace the type is defined in. It is refactoring-safe.

You can obtain a `LogWriter` by calling one of its static factory methods:

```csharp
public static LogWriter Get<T>();
public static LogWriter Get(Type type);
public static LogWriter Get(string name);
```

Log writers can be configured to attach custom tags to written log messages. This enables writing applications that can configure logging on a per-subsystem basis without losing the original name of the log writer (which is almost always the name of the class the log writer belongs to). To let an existing log writer add tags to written messages a new log writer with tagging behavior can be derived from the log writer using one of the following `LogWriter` methods:

```csharp
public LogWriter WithTag(string tag);
public LogWriter WithTags(params string[] tags);
```

Tags may consist of the following characters only:
- alphanumeric characters: `[a-z]`, `[A-Z]`, `[0-9]`
- extra characters: `_`, `.`, `,`, `:`, `;`, `+`, `-`, `#`
- brackets: `(`, `)`, `[`, `]`, `{`, `}`, `<`, `>`

### Choosing a Log Level

Each message written to the log is associated with a certain log level, represented by the `LogLevel` class. The log level indicates the severity of the log message. *Griffin+ Logging* comes with a set of predefined log levels as described above. In addition to the predefined log levels an aspect log level can be used. Aspect log levels are primarily useful when tracking an issue that effects multiple classes. The following `LogLevel` method creates an aspect log level:

```csharp
public GetAspect(string name);
```

### Writing a Message

Once you've a `LogWriter` and a `LogLevel` you can use one of the following `LogWriter` methods to write a message:

```csharp
// without formatting
public void Write(LogLevel level, string message);

// formatting with default format provider (invariant culture), bypasses filters applied by the log configuration
public void ForceWrite(LogLevel level, string format, params object[] args);

// formatting with default format provider (invariant culture), up to 15 parameters
public void Write<T>(LogLevel level, string format, T arg);
public void Write<T0,T1>(LogLevel level, string format, T0 arg0, T1 arg1);
public void Write<T0,T1,T2>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2);
public void Write<T0,T1,T2,T3>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3);
public void Write<T0,T1,T2,T3,T4>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
public void Write<T0,T1,T2,T3,T4,T5>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
public void Write<T0,T1,T2,T3,T4,T5,T6>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14>(LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);

// formatting with default format provider (invariant culture), for more than 15 parameters
public void Write(LogLevel level, string format, params object[] args);

// formatting with custom format provider, bypasses filters applied by the log configuration
public void ForceWrite(IFormatProvider provider, LogLevel level, string format, params object[] args);

// formatting with custom format provider, up to 15 parameters
public void Write<T>(IFormatProvider provider, LogLevel level, string format, T arg);
public void Write<T0,T1>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1);
public void Write<T0,T1,T2>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2);
public void Write<T0,T1,T2,T3>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3);
public void Write<T0,T1,T2,T3,T4>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4);
public void Write<T0,T1,T2,T3,T4,T5>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5);
public void Write<T0,T1,T2,T3,T4,T5,T6>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13);
public void Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14>(IFormatProvider provider, LogLevel level, string format, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14);

// formatting with custom format provider, for more than 15 parameters
public void Write(IFormatProvider provider, LogLevel level, string format, params object[] args);
```

### Helpers

#### AsyncId

The `AsyncId` class comes in handy when tracking asynchronous control flows using .NET's Task Parallel Library (TPL). Its `Current` property provides an integer value that remains constant all along the entire control flow starting from the point of its first invocation.

#### GetTimestamp()

The `GetTimestamp()` method of the `LogWriter` class returns an absolute timestamp (with timezone offset) as used by Griffin+ logging.

#### GetHighPrecisionTimestamp()

The `GetHighPrecisionTimestamp()` method of the `LogWriter` class returns a timestamp with nanosecond precision. The actual resolution of the timestamp depends on the system's clock, but on most modern systems the resolution is finer than one nanosecond. This timestamp can be used to measure timespans very accurately.

### Complete Example

The following example shows how the *Griffin+ Logging Interface* can be used. For illustration purposes written log messages are printed to the console. When using the full-featured *Griffin+ Logging* this is not necessary as *Griffin+ Logging* provides a more powerful and modular *log message processing pipeline*. By default log messages associated with log level `Notice` or higher pass the filter in log writers. This filter can be configured using the static `LogWriter.UpdateLogWriters(...)` method. Same here, when using *Griffin+ Logging* this is not necessary as it ships with more powerful configuration mechanisms that take care of parameterizing log writers.

 The source code is available in the demo project contained in the repository as well:

```csharp
using System;
using System.Threading;

namespace GriffinPlus.Lib.Logging.Demo
{

    class MyClass1 { }

    class MyClass2 { }

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
            var aspect = LogLevel.GetAspect("Demo Aspect");

            // Write messages to all known log levels (predefined log levels + aspects).
            foreach (var level in LogLevel.KnownLevels)
            {
                sLog1.Write(level, "This is sLog1 writing using level '{0}'.", level.Name);
                sLog2.Write(level, "This is sLog2 writing using level '{0}'.", level.Name);
                sLog3.Write(level, "This is sLog3 writing using level '{0}'.", level.Name);
                sLog_TagA.Write(level, "This is sLog_TagA writing using level '{0}'.", level.Name);
                sLog_TagB.Write(level, "This is sLog_TagB writing using level '{0}'.", level.Name);
                sLog_TagBC.Write(level, "This is sLog_TagBC writing using level '{0}'.", level.Name);
            }

            // Use a timing logger to determine how long an operation takes. It uses log level 'Timing' and log writer
            // 'Timing' by default, so you need to ensure that the configuration lets these messages pass).
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

}
```
