///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Xunit;

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Unit tests targeting the <see cref="LogWriter"/> class.
	/// </summary>
	[Collection("LogWriterTests")]
	public class LogWriterTests
	{
		#region Get(string name)

		/// <summary>
		/// Tests creating a <see cref="LogWriter"/> via the <see cref="LogWriter.Get(string)"/> method.
		/// </summary>
		[Fact]
		public void Get_NameAsStringParameter()
		{
			string name = Guid.NewGuid().ToString("D");
			Assert.DoesNotContain(LogWriter.KnownWriters, x => x.Name == name);
			var writer = LogWriter.Get(name);
			Assert.Equal(name, writer.Name);
			Assert.Contains(LogWriter.KnownWriters, x => x.Name == name);
		}

		/// <summary>
		/// Tests whether calling <see cref="LogWriter.Get(string)"/> twice returns the same <see cref="LogWriter"/> instance.
		/// </summary>
		[Fact]
		public void Get_NameAsStringParameter_SameNameShouldReturnSameInstance()
		{
			string name = Guid.NewGuid().ToString("D");
			var writer1 = LogWriter.Get(name);
			var writer2 = LogWriter.Get(name);
			Assert.Same(writer1, writer2);
		}

		#endregion

		#region Get<T>()

		public static IEnumerable<object[]> LogWriterCreationTestData1
		{
			get
			{
				// NOTE: The generic GetWriter<>() method does not support generic type definitions
				yield return new object[] { typeof(int), "System.Int32" };
				yield return new object[] { typeof(List<int>), "System.Collections.Generic.List<System.Int32>" };
				yield return new object[] { typeof(Dictionary<int, string>), "System.Collections.Generic.Dictionary<System.Int32,System.String>" };
			}
		}

		/// <summary>
		/// Tests creating a <see cref="LogWriter"/> via the <see cref="LogWriter.Get{T}"/> method.
		/// </summary>
		/// <param name="type">Type to derive the name of the log writer from.</param>
		/// <param name="expectedName">The expected name of the created log writer.</param>
		[Theory]
		[MemberData(nameof(LogWriterCreationTestData1))]
		public void GetT_NameAsTypeByGenericParameter(Type type, string expectedName)
		{
			var method = typeof(LogWriter)
				.GetMethods()
				.Single(x => x.Name == nameof(LogWriter.Get) && x.IsGenericMethod && x.GetGenericArguments().Length == 1)
				.MakeGenericMethod(type);

			var writer = (LogWriter)method.Invoke(null, null);
			Assert.NotNull(writer);
			Assert.Equal(expectedName, writer.Name);
			Assert.Contains(LogWriter.KnownWriters, x => x.Name == expectedName);
		}

		/// <summary>
		/// Tests whether calling <see cref="LogWriter.Get{T}"/> twice returns the same <see cref="LogWriter"/> instance.
		/// </summary>
		[Fact]
		public void Get_NameAsTypeByGenericParameter_SameTypeShouldReturnSameInstance()
		{
			var method = typeof(LogWriter)
				.GetMethods()
				.Single(x => x.Name == nameof(LogWriter.Get) && x.IsGenericMethod && x.GetGenericArguments().Length == 1)
				.MakeGenericMethod(typeof(LogWriterTests));

			var writer1 = (LogWriter)method.Invoke(null, null);
			var writer2 = (LogWriter)method.Invoke(null, null);
			Assert.Same(writer1, writer2);
		}

		#endregion

		#region Get(Type type)

		public static IEnumerable<object[]> LogWriterCreationTestData2
		{
			get
			{
				yield return new object[] { typeof(uint), "System.UInt32" };
				yield return new object[] { typeof(List<uint>), "System.Collections.Generic.List<System.UInt32>" };
				yield return new object[] { typeof(Dictionary<uint, string>), "System.Collections.Generic.Dictionary<System.UInt32,System.String>" };
				yield return new object[] { typeof(List<>), "System.Collections.Generic.List<T>" };
				yield return new object[] { typeof(Dictionary<,>), "System.Collections.Generic.Dictionary<TKey,TValue>" };
			}
		}

		/// <summary>
		/// Tests creating a <see cref="LogWriter"/> via the <see cref="LogWriter.Get(Type)"/> method.
		/// </summary>
		/// <param name="type">Type to derive the name of the log writer from.</param>
		/// <param name="expectedName">The expected name of the created log writer.</param>
		[Theory]
		[MemberData(nameof(LogWriterCreationTestData2))]
		public void Get_NameAsTypeByParameter(Type type, string expectedName)
		{
			Assert.DoesNotContain(LogWriter.KnownWriters, x => x.Name == expectedName);
			var writer = LogWriter.Get(type);
			Assert.Equal(expectedName, writer.Name);
			Assert.Contains(LogWriter.KnownWriters, x => x.Name == expectedName);
		}

		/// <summary>
		/// Tests whether calling <see cref="LogWriter.Get(Type)"/> twice returns the same <see cref="LogWriter"/> instance.
		/// </summary>
		[Fact]
		public void Get_NameAsTypeByParameter_SameTypeShouldReturnSameInstance()
		{
			Type type = typeof(LogWriterTests);
			var writer1 = LogWriter.Get(type);
			var writer2 = LogWriter.Get(type);
			Assert.Same(writer1, writer2);
		}

		#endregion

		#region WithTag(string tag)

		/// <summary>
		/// Tests creating a tagging <see cref="LogWriter"/> via <see cref="LogWriter.WithTag"/> and checks its integrity.
		/// The tag was not assigned before.
		/// </summary>
		[Fact]
		public void WithTag_TagWasNotAssignedBefore()
		{
			string name = Guid.NewGuid().ToString("D");
			string tag = Guid.NewGuid().ToString("D");
			var writer1 = LogWriter.Get(name);
			Assert.Empty(writer1.Tags);
			Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag);
			var writer2 = writer1.WithTag(tag);
			Assert.NotSame(writer1, writer2);
			Assert.Equal(new[] { tag }, writer2.Tags);
			Assert.Contains(LogWriter.KnownTags, x => x.Name == tag);
		}

		/// <summary>
		/// Tests creating a tagging <see cref="LogWriter"/> via <see cref="LogWriter.WithTag"/> and checks its integrity.
		/// The tag was assigned before.
		/// </summary>
		[Fact]
		public void WithTag_TagWasAssignedBefore()
		{
			string name = Guid.NewGuid().ToString("D");
			string tag = Guid.NewGuid().ToString("D");
			Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag);
			var writer1 = LogWriter.Get(name).WithTag(tag);
			Assert.Contains(LogWriter.KnownTags, x => x.Name == tag);
			Assert.Equal(new[] { tag }, writer1.Tags);
			var writer2 = writer1.WithTag(tag);
			Assert.Same(writer1, writer2);
			Assert.Equal(new[] { tag }, writer2.Tags);
			Assert.Contains(LogWriter.KnownTags, x => x.Name == tag);
		}

		/// <summary>
		/// Tests whether <see cref="LogWriter.WithTag"/> returns the same <see cref="LogWriter"/> instance, if no tags are specified.
		/// </summary>
		[Fact]
		public void WithTag_TagIsNull()
		{
			string name = Guid.NewGuid().ToString("D");
			const string tag = null;
			var writer1 = LogWriter.Get(name);
			Assert.Empty(writer1.Tags);
			var writer2 = writer1.WithTag(tag);
			Assert.Same(writer1, writer2);
			Assert.Empty(writer2.Tags);
		}

		#endregion

		#region WithTags(params string[] tags)

		/// <summary>
		/// Tests creating a tagging <see cref="LogWriter"/> via <see cref="LogWriter.WithTags"/> and checks its integrity.
		/// The tags were not assigned before.
		/// </summary>
		[Fact]
		public void WithTags_TagsWereNotAssignedBefore()
		{
			string name = Guid.NewGuid().ToString("D");
			string tag1 = Guid.NewGuid().ToString("D");
			string tag2 = Guid.NewGuid().ToString("D");
			var writer1 = LogWriter.Get(name);
			Assert.Empty(writer1.Tags);
			Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag1);
			Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag2);
			var writer2 = writer1.WithTags(tag1, tag2);
			Assert.NotSame(writer1, writer2);
			Assert.Equal(new[] { tag1, tag2 }.OrderBy(x => x), writer2.Tags);
			Assert.Contains(LogWriter.KnownTags, x => x.Name == tag1);
			Assert.Contains(LogWriter.KnownTags, x => x.Name == tag2);
		}

		/// <summary>
		/// Tests creating a tagging <see cref="LogWriter"/> via <see cref="LogWriter.WithTags"/> and checks its integrity.
		/// The tags were assigned before.
		/// </summary>
		[Fact]
		public void WithTags_TagsWereAssignedBefore()
		{
			string name = Guid.NewGuid().ToString("D");
			string tag1 = Guid.NewGuid().ToString("D");
			string tag2 = Guid.NewGuid().ToString("D");
			Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag1);
			Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag2);
			var writer1 = LogWriter.Get(name).WithTags(tag1, tag2);
			Assert.Contains(LogWriter.KnownTags, x => x.Name == tag1);
			Assert.Contains(LogWriter.KnownTags, x => x.Name == tag2);
			Assert.Equal(new[] { tag1, tag2 }.OrderBy(x => x), writer1.Tags);
			var writer2 = writer1.WithTags(tag1, tag2);
			Assert.Same(writer1, writer2);
			Assert.Equal(new[] { tag1, tag2 }.OrderBy(x => x), writer2.Tags);
			Assert.Contains(LogWriter.KnownTags, x => x.Name == tag1);
			Assert.Contains(LogWriter.KnownTags, x => x.Name == tag2);
		}

		/// <summary>
		/// Tests whether <see cref="LogWriter.WithTags"/> returns the same <see cref="LogWriter"/> instance, if no tags (<c>null</c>) are specified.
		/// </summary>
		[Fact]
		public void WithTags_TagsIsNull()
		{
			string name = Guid.NewGuid().ToString("D");
			var writer1 = LogWriter.Get(name);
			Assert.Empty(writer1.Tags);
			var writer2 = writer1.WithTags(null);
			Assert.Same(writer1, writer2);
			Assert.Empty(writer2.Tags);
		}

		#endregion

		#region GetTimestamp()

		/// <summary>
		/// Tests the <see cref="LogWriter.GetTimestamp"/> method.
		/// </summary>
		[Fact]
		public void GetTimestamp()
		{
			var actual = LogWriter.GetTimestamp();
			var expected = DateTimeOffset.Now;
			var difference = expected - actual;
			Assert.True(difference < TimeSpan.FromSeconds(1));
		}

		#endregion

		#region GetHighPrecisionTimestamp()

		/// <summary>
		/// Tests the <see cref="LogWriter.GetHighPrecisionTimestamp"/> method.
		/// </summary>
		[Fact]
		public void GetHighPrecisionTimestamp()
		{
			long actual = LogWriter.GetHighPrecisionTimestamp();
			long expected = (long)((decimal)Stopwatch.GetTimestamp() * 1000000000L / Stopwatch.Frequency); // in ns
			long difference = expected - actual;
			Assert.True(difference < 1000000000); // 1 second
		}

		#endregion

		#region implicit operator string(LogWriter writer)

		/// <summary>
		/// Tests the <see cref="LogWriter.op_Implicit"/> conversion operator.
		/// </summary>
		[Fact]
		public void OperatorString()
		{
			foreach (var writer in LogWriter.KnownWriters)
			{
				string expected = writer.Name;
				Assert.Equal(expected, writer);
			}
		}

		#endregion

		#region IsLogLevelActive(LogLevel level)

		/// <summary>
		/// Tests the <see cref="LogWriter.IsLogLevelActive"/> method.
		/// </summary>
		[Fact]
		public void IsLogLevelActive()
		{
			// use the first known log writer to avoid mixing up the collection of
			// known log writers to avoid confusing other tests
			var writer = LogWriter.KnownWriters[0];

			// the default configuration activates all log levels above and including 'Notice'
			Assert.True(writer.IsLogLevelActive(LogLevel.Emergency));
			Assert.True(writer.IsLogLevelActive(LogLevel.Alert));
			Assert.True(writer.IsLogLevelActive(LogLevel.Critical));
			Assert.True(writer.IsLogLevelActive(LogLevel.Error));
			Assert.True(writer.IsLogLevelActive(LogLevel.Warning));
			Assert.True(writer.IsLogLevelActive(LogLevel.Notice));
			Assert.False(writer.IsLogLevelActive(LogLevel.Informational));
			Assert.False(writer.IsLogLevelActive(LogLevel.Debug));
			Assert.False(writer.IsLogLevelActive(LogLevel.Trace));
			Assert.False(writer.IsLogLevelActive(LogLevel.Timing));

			// the special log levels are always active
			// (although they should not be used to write messages)
			Assert.True(writer.IsLogLevelActive(LogLevel.None));
			Assert.True(writer.IsLogLevelActive(LogLevel.All));
		}

		#endregion

		#region CheckName(string name)

		/// <summary>
		/// Tests the <see cref="LogWriter.CheckName"/> method.
		/// </summary>
		/// <param name="name">Name to check.</param>
		/// <param name="ok"><c>true</c> if the name is valid; otherwise <c>false</c>.</param>
		[Theory]
		[InlineData("A", true)]         // a letter
		[InlineData("0", true)]         // a digit
		[InlineData("", false)]         // empty name
		[InlineData(" ", false)]        // whitespace only
		[InlineData("A\u000AB", false)] // line feed
		[InlineData("A\u000CB", false)] // form feed
		[InlineData("A\u000DB", false)] // carriage return
		[InlineData("A\u2028B", false)] // line separator
		[InlineData("A\u2029B", false)] // paragraph separator
		public void CheckName(string name, bool ok)
		{
			if (ok)
			{
				LogWriter.CheckName(name);
			}
			else
			{
				var exception = Assert.Throws<ArgumentException>(() => LogWriter.CheckName(name));
			}
		}

		/// <summary>
		/// Tests the <see cref="LogWriter.CheckName"/> method passing <c>null</c>.
		/// The method should throw an <see cref="ArgumentNullException"/> in this case.
		/// </summary>
		[Fact]
		public void CheckName_NameIsNull()
		{
			var exception = Assert.Throws<ArgumentNullException>(() => LogWriter.CheckName(null));
			Assert.Equal("name", exception.ParamName);
		}

		#endregion

		#region Write(...)

		#region Common Test Code

		/// <summary>
		/// Common frame for testing the various Write() methods.
		/// </summary>
		/// <param name="write">Invokes the Write() method to test.</param>
		/// <param name="expectedMessage">The expected message.</param>
		private static void Write_Common(Action<LogWriter, LogLevel> write, string expectedMessage)
		{
			LogWriter writerInEvent = null;
			LogLevel levelInEvent = null;
			string messageInEvent = null;

			void MessageWrittenHandler(LogWriter logWriter, LogLevel logLevel, string message)
			{
				writerInEvent = logWriter;
				levelInEvent = logLevel;
				messageInEvent = message;
			}

			var writer = LogWriter.Get("MyWriter");
			var level = LogLevel.Notice;
			try
			{
				LogWriter.LogMessageWritten += MessageWrittenHandler;
				write(writer, level);
				Assert.Same(writer, writerInEvent);
				Assert.Same(level, levelInEvent);
				Assert.Equal(expectedMessage, messageInEvent);
			}
			finally
			{
				LogWriter.LogMessageWritten -= MessageWrittenHandler;
			}
		}

		/// <summary>
		/// Produces an <see cref="Exception"/> with another <see cref="Exception"/> as inner exception.
		/// </summary>
		/// <param name="i">Some data to make exceptions different from each other.</param>
		/// <returns>An exception with an inner exception.</returns>
		private static Exception ProduceNestedException(int i)
		{
			try
			{
				try
				{
					throw new Exception($"inner exception {i}");
				}
				catch (Exception e)
				{
					throw new Exception($"outer exception {i}", e);
				}
			}
			catch (Exception e)
			{
				return e;
			}
		}

		/// <summary>
		/// Produces the specified number of <see cref="Exception"/> objects with <see cref="Exception"/> objects as inner exceptions.
		/// </summary>
		/// <returns>The requested number of exception objects.</returns>
		private static Exception[] ProduceNestedExceptions(int count)
		{
			Exception[] exceptions = new Exception[count];
			for (int i = 0; i < count; i++) exceptions[i] = ProduceNestedException(i);
			return exceptions;
		}

		/// <summary>
		/// Gets a string containing the type, the message and the stacktrace of the specified exception and all of its inner exception.
		/// </summary>
		/// <param name="exception"></param>
		/// <returns>The exception in a properly formatted fashion.</returns>
		private static string UnwrapException(Exception exception)
		{
			var builder = new StringBuilder();
			builder.AppendLine();

			int innerExceptionLevel = 0;
			var current = exception;
			while (current != null)
			{
				builder.Append(
					innerExceptionLevel == 0
						? "--- Exception ---------------------------------------------------------------------------------------------\r\n"
						: "--- Inner Exception ---------------------------------------------------------------------------------------\r\n");
				builder.AppendFormat("--- Exception Type: {0}\r\n", exception.GetType().FullName);
				builder.AppendFormat("--- Message: {0}\r\n", current.Message);
				builder.AppendFormat("--- Stacktrace:\r\n{0}", current.StackTrace);
				builder.AppendLine();

				current = current.InnerException;
				innerExceptionLevel++;
			}

			return builder.ToString();
		}

		#endregion

		#region Write(LogLevel level, string message)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write(LogLevel,string)"/>.
		/// </summary>
		[Fact]
		public void Write_NotFormatting()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text"),
				"text");
		}

		#endregion

		#region Write<T>(LogLevel level, string message, T arg)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T}(LogLevel,string,T)"/> with an object to format regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T}(IFormatProvider,LogLevel,string,T)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_1_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0}", 1),
				"text: 1");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T}(LogLevel,string,T)"/> with an exception that should be unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T}(IFormatProvider,LogLevel,string,T)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_1_WithExceptionToUnwrap()
		{
			var exceptions = ProduceNestedExceptions(1);
			Write_Common(
				(writer, level) => writer.Write(level, "{0}", exceptions[0]),
				UnwrapException(exceptions[0]));
		}

		#endregion

		#region Write<T0,T1>(LogLevel level, string message, T0 arg0, T1 arg1)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1}(LogLevel,string,T0,T1)"/> with objects to format regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1}(IFormatProvider,LogLevel,string,T0,T1)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_2_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1}", 1, 2),
				"text: 1 2");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1}(LogLevel,string,T0,T1)"/> with exceptions that should be unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1}(IFormatProvider,LogLevel,string,T0,T1)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_2_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(2);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1}", e[0], e[1]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]));
		}

		#endregion

		#region Write<T0,T1,T2>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2}(LogLevel,string,T0,T1,T2)"/> with objects to format regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2}(IFormatProvider,LogLevel,string,T0,T1,T2)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_3_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2}", 1, 2, 3),
				"text: 1 2 3");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2}(LogLevel,string,T0,T1,T2)"/> with exceptions that should be unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2}(IFormatProvider,LogLevel,string,T0,T1,T2)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_3_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(3);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2}", e[0], e[1], e[2]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]));
		}

		#endregion

		#region Write<T0,T1,T2,T3>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3}(LogLevel,string,T0,T1,T2,T3)"/> with objects to format regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3}(IFormatProvider,LogLevel,string,T0,T1,T2,T3)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_4_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3}", 1, 2, 3, 4),
				"text: 1 2 3 4");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3}(LogLevel,string,T0,T1,T2,T3)"/> with exceptions that should be unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3}(IFormatProvider,LogLevel,string,T0,T1,T2,T3)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_4_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(4);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3}", e[0], e[1], e[2], e[3]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4}(LogLevel,string,T0,T1,T2,T3,T4)"/> with objects to format regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_5_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4}", 1, 2, 3, 4, 5),
				"text: 1 2 3 4 5");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4}(LogLevel,string,T0,T1,T2,T3,T4)"/> with exceptions that should be
		/// unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_5_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(5);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4}", e[0], e[1], e[2], e[3], e[4]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5}(LogLevel,string,T0,T1,T2,T3,T4,T5)"/> with objects to format
		/// regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_6_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5}", 1, 2, 3, 4, 5, 6),
				"text: 1 2 3 4 5 6");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5}(LogLevel,string,T0,T1,T2,T3,T4,T5)"/> with exceptions that should be
		/// unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_6_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(6);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5}", e[0], e[1], e[2], e[3], e[4], e[5]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5,T6>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6)"/> with objects to format
		/// regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_7_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5} {6}", 1, 2, 3, 4, 5, 6, 7),
				"text: 1 2 3 4 5 6 7");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6)"/> with exceptions that
		/// should be unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_7_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(7);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5} {6}", e[0], e[1], e[2], e[3], e[4], e[5], e[6]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]) + " " +
				UnwrapException(e[6]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5,T6,T7>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7)"/> with objects to
		/// format regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_8_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5} {6} {7}", 1, 2, 3, 4, 5, 6, 7, 8),
				"text: 1 2 3 4 5 6 7 8");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7)"/> with exceptions
		/// that should be unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_8_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(8);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5} {6} {7}", e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]) + " " +
				UnwrapException(e[6]) + " " +
				UnwrapException(e[7]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8)"/> with objects
		/// to format regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_9_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5} {6} {7} {8}", 1, 2, 3, 4, 5, 6, 7, 8, 9),
				"text: 1 2 3 4 5 6 7 8 9");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8)"/> with
		/// exceptions that should be unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_9_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(9);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5} {6} {7} {8}", e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7], e[8]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]) + " " +
				UnwrapException(e[6]) + " " +
				UnwrapException(e[7]) + " " +
				UnwrapException(e[8]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/> with
		/// objects to format regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_10_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9}", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10),
				"text: 1 2 3 4 5 6 7 8 9 10");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/> with
		/// exceptions that should be unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_10_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(10);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9}", e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7], e[8], e[9]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]) + " " +
				UnwrapException(e[6]) + " " +
				UnwrapException(e[7]) + " " +
				UnwrapException(e[8]) + " " +
				UnwrapException(e[9]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10)"/>
		/// with objects to format regularly.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_11_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11),
				"text: 1 2 3 4 5 6 7 8 9 10 11");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10)"/>
		/// with exceptions that should be unwrapped.
		/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_11_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(11);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10}", e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7], e[8], e[9], e[10]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]) + " " +
				UnwrapException(e[6]) + " " +
				UnwrapException(e[7]) + " " +
				UnwrapException(e[8]) + " " +
				UnwrapException(e[9]) + " " +
				UnwrapException(e[10]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11)

		/// <summary>
		/// Tests writing a log message using
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11)"/> with objects to format
		/// regularly.
		/// Implicitly tests
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_12_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12),
				"text: 1 2 3 4 5 6 7 8 9 10 11 12");
		}

		/// <summary>
		/// Tests writing a log message using
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11)"/> with exceptions that
		/// should be unwrapped.
		/// Implicitly tests
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_12_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(12);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11}", e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7], e[8], e[9], e[10], e[11]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]) + " " +
				UnwrapException(e[6]) + " " +
				UnwrapException(e[7]) + " " +
				UnwrapException(e[8]) + " " +
				UnwrapException(e[9]) + " " +
				UnwrapException(e[10]) + " " +
				UnwrapException(e[11]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12)

		/// <summary>
		/// Tests writing a log message using
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12)"/> with objects to
		/// format regularly.
		/// Implicitly tests
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_13_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12}", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13),
				"text: 1 2 3 4 5 6 7 8 9 10 11 12 13");
		}

		/// <summary>
		/// Tests writing a log message using
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12)"/> with exceptions
		/// that should be unwrapped.
		/// Implicitly tests
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_13_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(13);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12}", e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7], e[8], e[9], e[10], e[11], e[12]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]) + " " +
				UnwrapException(e[6]) + " " +
				UnwrapException(e[7]) + " " +
				UnwrapException(e[8]) + " " +
				UnwrapException(e[9]) + " " +
				UnwrapException(e[10]) + " " +
				UnwrapException(e[11]) + " " +
				UnwrapException(e[12]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13)

		/// <summary>
		/// Tests writing a log message using
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13)"/> with
		/// objects to format regularly.
		/// Implicitly tests
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T13}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T13)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_14_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13}", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14),
				"text: 1 2 3 4 5 6 7 8 9 10 11 12 13 14");
		}

		/// <summary>
		/// Tests writing a log message using
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13)"/> with
		/// exceptions that should be unwrapped.
		/// Implicitly tests
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T13}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T13)"/>.
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_14_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(14);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13}", e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7], e[8], e[9], e[10], e[11], e[12], e[13]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]) + " " +
				UnwrapException(e[6]) + " " +
				UnwrapException(e[7]) + " " +
				UnwrapException(e[8]) + " " +
				UnwrapException(e[9]) + " " +
				UnwrapException(e[10]) + " " +
				UnwrapException(e[11]) + " " +
				UnwrapException(e[12]) + " " +
				UnwrapException(e[13]));
		}

		#endregion

		#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10, T11 arg11, T12 arg12, T13 arg13, T14 arg14)

		/// <summary>
		/// Tests writing a log message using
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14)"/>
		/// with objects to format regularly.
		/// Implicitly tests
		/// <see
		///     cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T13,T14}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T13,T14)"/>
		/// .
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_15_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14}", 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15),
				"text: 1 2 3 4 5 6 7 8 9 10 11 12 13 14 15");
		}

		/// <summary>
		/// Tests writing a log message using
		/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14)"/>
		/// with exceptions that should be unwrapped.
		/// Implicitly tests
		/// <see
		///     cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T13,T14}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T13,T14)"/>
		/// .
		/// </summary>
		[Fact]
		public void Write_Formatting_GenericArguments_15_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(15);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5} {6} {7} {8} {9} {10} {11} {12} {13} {14}", e[0], e[1], e[2], e[3], e[4], e[5], e[6], e[7], e[8], e[9], e[10], e[11], e[12], e[13], e[14]),
				UnwrapException(e[0]) + " " +
				UnwrapException(e[1]) + " " +
				UnwrapException(e[2]) + " " +
				UnwrapException(e[3]) + " " +
				UnwrapException(e[4]) + " " +
				UnwrapException(e[5]) + " " +
				UnwrapException(e[6]) + " " +
				UnwrapException(e[7]) + " " +
				UnwrapException(e[8]) + " " +
				UnwrapException(e[9]) + " " +
				UnwrapException(e[10]) + " " +
				UnwrapException(e[11]) + " " +
				UnwrapException(e[12]) + " " +
				UnwrapException(e[13]) + " " +
				UnwrapException(e[14]));
		}

		#endregion

		#region Write(LogLevel level, string message, object[] args)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write(LogLevel,string,object[])"/>
		/// with objects to format regularly.
		/// </summary>
		[Fact]
		public void Write_Formatting_WithVArgs_Regular()
		{
			Write_Common(
				(writer, level) => writer.Write(level, "text: {0} {1} {2}", new object[] { 1, 2, 3 }),
				"text: 1 2 3");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.Write(LogLevel,string,object[])"/>
		/// with exceptions that should be unwrapped.
		/// </summary>
		[Fact]
		public void Write_Formatting_WithVArgs_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(2);
			Write_Common(
				(writer, level) => writer.Write(level, "{0} {1}", new object[] { e[0], e[1] }),
				UnwrapException(e[0]) + " " + UnwrapException(e[1]));
		}

		#endregion

		#region ForceWrite(LogLevel level, string message, object[] args)

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.ForceWrite(LogLevel,string,object[])"/>
		/// with objects to format regularly.
		/// </summary>
		[Fact]
		public void ForceWrite_Formatting_WithVArgs_Regular()
		{
			Write_Common(
				(writer, level) => writer.ForceWrite(level, "text: {0} {1} {2}", 1, 2, 3),
				"text: 1 2 3");
		}

		/// <summary>
		/// Tests writing a log message using <see cref="LogWriter.ForceWrite(LogLevel,string,object[])"/>
		/// with exceptions that should be unwrapped.
		/// </summary>
		[Fact]
		public void ForceWrite_Formatting_WithVArgs_WithExceptionsToUnwrap()
		{
			var e = ProduceNestedExceptions(2);
			Write_Common(
				(writer, level) => writer.ForceWrite(level, "{0} {1}", e[0], e[1]),
				UnwrapException(e[0]) + " " + UnwrapException(e[1]));
		}

		#endregion

		#endregion

		#region ToString()

		/// <summary>
		/// Tests the <see cref="LogWriter.ToString"/> method.
		/// </summary>
		[Fact]
#pragma warning disable xUnit1024 // Test methods cannot have overloads
		public new void ToString()
#pragma warning restore xUnit1024 // Test methods cannot have overloads
		{
			foreach (var writer in LogWriter.KnownWriters)
			{
				string expected = $"{writer.Name} ({writer.Id})";
				Assert.Equal(expected, writer.ToString());
			}
		}

		#endregion
	}

}
