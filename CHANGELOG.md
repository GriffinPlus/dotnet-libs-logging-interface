# Changelog
---

## Release v1.3.0

- Added explicit support for .NET Framework 4.8, .NET 6.0, and .NET 8.0.
- Added support for nullable types.
- Added formatting of runtime metadata in log messages (type, assembly, module, member information, exceptions, objects).

---

## Release v1.2.0

Added the `FailFast` class providing the `TerminateApplication(...)` method to request terminating the application
immediately. This is useful when an unrecoverable error occurs and the application should not continue to run.
When used in conjunction with the `GriffinPlus.Lib.Logging` package (version >= 7.0.5), the method writes a message
to the log, flushes buffered messages and then calls `Environment.FailFast(...)` to terminate the process.

---

## Release v1.1.2

- Added missing support for unwrapping `AggregateException` when using `LogWriter.Write(...)` methods.
- Improved formatting of inner exceptions in general by indenting exceptions by their level in the hierarchy.

---

## Release v1.1.1

Fixed and simplified the generation of log writer name from a runtime type.

---

## Release v1.1.0

Added helper class (`AsyncId`) for tracking asynchronous controls flows using the Task Parallel Library (TPL).

---

## Release v1.0.0

Initial version, released along with Griffin+ Logging 5.1.0.

---
