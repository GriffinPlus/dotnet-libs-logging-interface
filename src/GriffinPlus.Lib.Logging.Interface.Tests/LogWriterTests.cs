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
using System.Text;
using System.Threading;

using Xunit;
using Xunit.Priority;

// ReSharper disable CanSimplifyStringEscapeSequence
// ReSharper disable ParameterOnlyUsedForPreconditionCheck.Local

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Unit tests targeting the <see cref="LogWriter"/> class.
/// </summary>
[Collection(TestOrder.TestsCollectionName)]
[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class LogWriterTests
{
	#region Get(string name)

	/// <summary>
	/// Tests creating a <see cref="LogWriter"/> via the <see cref="LogWriter.Get(string)"/> method.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase)]
	public void Get_NameAsStringParameter()
	{
		string name = Guid.NewGuid().ToString("D");
		Assert.DoesNotContain(LogWriter.KnownWriters, x => x.Name == name);
		LogWriter writer = LogWriter.Get(name);
		Assert.Equal(name, writer.Name);
		Assert.Contains(LogWriter.KnownWriters, x => x.Name == name);
	}

	/// <summary>
	/// Tests whether calling <see cref="LogWriter.Get(string)"/> twice returns the same <see cref="LogWriter"/> instance.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase)]
	public void Get_NameAsStringParameter_SameNameShouldReturnSameInstance()
	{
		string name = Guid.NewGuid().ToString("D");
		LogWriter writer1 = LogWriter.Get(name);
		LogWriter writer2 = LogWriter.Get(name);
		Assert.Same(writer1, writer2);
	}

	#endregion

	#region Get<T>()

	public static IEnumerable<object[]> LogWriterCreationTestData1
	{
		get
		{
			// NOTE: The generic GetWriter<>() method does not support generic type definitions
			yield return [typeof(int), "System.Int32"];
			yield return [typeof(List<int>), "System.Collections.Generic.List<System.Int32>"];
			yield return [typeof(Dictionary<int, string>), "System.Collections.Generic.Dictionary<System.Int32,System.String>"];
		}
	}

	/// <summary>
	/// Tests creating a <see cref="LogWriter"/> via the <see cref="LogWriter.Get{T}"/> method.
	/// </summary>
	/// <param name="type">Type to derive the name of the log writer from.</param>
	/// <param name="expectedName">The expected name of the created log writer.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase)]
	[MemberData(nameof(LogWriterCreationTestData1))]
	public void GetT_NameAsTypeByGenericParameter(Type type, string expectedName)
	{
		MethodInfo method = typeof(LogWriter)
			.GetMethods()
			.Single(x => x.Name == nameof(LogWriter.Get) && x.IsGenericMethod && x.GetGenericArguments().Length == 1)
			.MakeGenericMethod(type);

		var writer = (LogWriter)method.Invoke(null, null)!;
		Assert.NotNull(writer);
		Assert.Equal(expectedName, writer.Name);
		Assert.Contains(LogWriter.KnownWriters, x => x.Name == expectedName);
	}

	/// <summary>
	/// Tests whether calling <see cref="LogWriter.Get{T}"/> twice returns the same <see cref="LogWriter"/> instance.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase)]
	public void Get_NameAsTypeByGenericParameter_SameTypeShouldReturnSameInstance()
	{
		MethodInfo method = typeof(LogWriter)
			.GetMethods()
			.Single(x => x.Name == nameof(LogWriter.Get) && x.IsGenericMethod && x.GetGenericArguments().Length == 1)
			.MakeGenericMethod(typeof(LogWriterTests));

		var writer1 = (LogWriter)method.Invoke(null, null)!;
		var writer2 = (LogWriter)method.Invoke(null, null)!;
		Assert.Same(writer1, writer2);
	}

	#endregion

	#region Get(Type type)

	public static IEnumerable<object[]> LogWriterCreationTestData2
	{
		get
		{
			yield return [typeof(uint), "System.UInt32"];
			yield return [typeof(List<uint>), "System.Collections.Generic.List<System.UInt32>"];
			yield return [typeof(Dictionary<uint, string>), "System.Collections.Generic.Dictionary<System.UInt32,System.String>"];
			yield return [typeof(List<>), "System.Collections.Generic.List<T>"];
			yield return [typeof(Dictionary<,>), "System.Collections.Generic.Dictionary<TKey,TValue>"];
		}
	}

	/// <summary>
	/// Tests creating a <see cref="LogWriter"/> via the <see cref="LogWriter.Get(Type)"/> method.
	/// </summary>
	/// <param name="type">Type to derive the name of the log writer from.</param>
	/// <param name="expectedName">The expected name of the created log writer.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase)]
	[MemberData(nameof(LogWriterCreationTestData2))]
	public void Get_NameAsTypeByParameter(Type type, string expectedName)
	{
		Assert.DoesNotContain(LogWriter.KnownWriters, x => x.Name == expectedName);
		LogWriter writer = LogWriter.Get(type);
		Assert.Equal(expectedName, writer.Name);
		Assert.Contains(LogWriter.KnownWriters, x => x.Name == expectedName);
	}

	/// <summary>
	/// Tests whether calling <see cref="LogWriter.Get(Type)"/> twice returns the same <see cref="LogWriter"/> instance.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase)]
	public void Get_NameAsTypeByParameter_SameTypeShouldReturnSameInstance()
	{
		Type type = typeof(LogWriterTests);
		LogWriter writer1 = LogWriter.Get(type);
		LogWriter writer2 = LogWriter.Get(type);
		Assert.Same(writer1, writer2);
	}

	#endregion

	#region WithTag(string tag)

	/// <summary>
	/// Tests creating a tagging <see cref="LogWriter"/> via <see cref="LogWriter.WithTag"/> and checks its integrity.
	/// The tag was not assigned before.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase)]
	public void WithTag_TagWasNotAssignedBefore()
	{
		string name = Guid.NewGuid().ToString("D");
		string tag = Guid.NewGuid().ToString("D");
		LogWriter writer1 = LogWriter.Get(name);
		Assert.Empty(writer1.Tags);
		Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag);
		LogWriter writer2 = writer1.WithTag(tag);
		Assert.NotSame(writer1, writer2);
		Assert.Equal([tag], writer2.Tags);
		Assert.Contains(LogWriter.KnownTags, x => x.Name == tag);
	}

	/// <summary>
	/// Tests creating a tagging <see cref="LogWriter"/> via <see cref="LogWriter.WithTag"/> and checks its integrity.
	/// The tag was assigned before.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase)]
	public void WithTag_TagWasAssignedBefore()
	{
		string name = Guid.NewGuid().ToString("D");
		string tag = Guid.NewGuid().ToString("D");
		Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag);
		LogWriter writer1 = LogWriter.Get(name).WithTag(tag);
		Assert.Contains(LogWriter.KnownTags, x => x.Name == tag);
		Assert.Equal([tag], writer1.Tags);
		LogWriter writer2 = writer1.WithTag(tag);
		Assert.Same(writer1, writer2);
		Assert.Equal([tag], writer2.Tags);
		Assert.Contains(LogWriter.KnownTags, x => x.Name == tag);
	}

	/// <summary>
	/// Tests whether <see cref="LogWriter.WithTag"/> returns the same <see cref="LogWriter"/> instance, if no tags are specified.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase)]
	public void WithTag_TagIsNull()
	{
		string name = Guid.NewGuid().ToString("D");
		const string? tag = null;
		LogWriter writer1 = LogWriter.Get(name);
		Assert.Empty(writer1.Tags);
		LogWriter writer2 = writer1.WithTag(tag);
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
	[Priority(TestOrder.ModifyingBase)]
	public void WithTags_TagsWereNotAssignedBefore()
	{
		string name = Guid.NewGuid().ToString("D");
		string tag1 = Guid.NewGuid().ToString("D");
		string tag2 = Guid.NewGuid().ToString("D");
		LogWriter writer1 = LogWriter.Get(name);
		Assert.Empty(writer1.Tags);
		Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag1);
		Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag2);
		LogWriter writer2 = writer1.WithTags(tag1, tag2);
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
	[Priority(TestOrder.ModifyingBase)]
	public void WithTags_TagsWereAssignedBefore()
	{
		string name = Guid.NewGuid().ToString("D");
		string tag1 = Guid.NewGuid().ToString("D");
		string tag2 = Guid.NewGuid().ToString("D");
		Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag1);
		Assert.DoesNotContain(LogWriter.KnownTags, x => x.Name == tag2);
		LogWriter writer1 = LogWriter.Get(name).WithTags(tag1, tag2);
		Assert.Contains(LogWriter.KnownTags, x => x.Name == tag1);
		Assert.Contains(LogWriter.KnownTags, x => x.Name == tag2);
		Assert.Equal(new[] { tag1, tag2 }.OrderBy(x => x), writer1.Tags);
		LogWriter writer2 = writer1.WithTags(tag1, tag2);
		Assert.Same(writer1, writer2);
		Assert.Equal(new[] { tag1, tag2 }.OrderBy(x => x), writer2.Tags);
		Assert.Contains(LogWriter.KnownTags, x => x.Name == tag1);
		Assert.Contains(LogWriter.KnownTags, x => x.Name == tag2);
	}

	/// <summary>
	/// Tests whether <see cref="LogWriter.WithTags"/> returns the same <see cref="LogWriter"/> instance, if no tags (<see langword="null"/>) are specified.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase)]
	public void WithTags_TagsIsNull()
	{
		string name = Guid.NewGuid().ToString("D");
		LogWriter writer1 = LogWriter.Get(name);
		Assert.Empty(writer1.Tags);
		LogWriter writer2 = writer1.WithTags(null);
		Assert.Same(writer1, writer2);
		Assert.Empty(writer2.Tags);
	}

	#endregion

	#region GetTimestamp()

	/// <summary>
	/// Tests the <see cref="LogWriter.GetTimestamp"/> method.
	/// </summary>
	[Fact]
	[Priority(TestOrder.NonModifying)]
	public void GetTimestamp()
	{
		DateTimeOffset actual = LogWriter.GetTimestamp();
		DateTimeOffset expected = DateTimeOffset.Now;
		TimeSpan difference = expected - actual;
		Assert.True(difference < TimeSpan.FromSeconds(1));
	}

	#endregion

	#region GetHighPrecisionTimestamp()

	/// <summary>
	/// Tests the <see cref="LogWriter.GetHighPrecisionTimestamp"/> method.
	/// </summary>
	[Fact]
	[Priority(TestOrder.NonModifying)]
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
	[Priority(TestOrder.NonModifying)]
	public void OperatorString()
	{
		foreach (LogWriter writer in LogWriter.KnownWriters)
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
	[Priority(TestOrder.ModifyingBase)]
	public void IsLogLevelActive()
	{
		// create a log writer to test with
		LogWriter writer = LogWriter.Get("LogWriterToTest");

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
	/// <param name="ok">
	/// <see langword="true"/> if the name is valid;<br/>
	/// otherwise, <see langword="false"/>.
	/// </param>
	[Theory]
	[Priority(TestOrder.NonModifying)]
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
			Assert.Throws<ArgumentException>(() => LogWriter.CheckName(name));
		}
	}

	/// <summary>
	/// Tests the <see cref="LogWriter.CheckName"/> method passing <see langword="null"/>.
	/// The method should throw an <see cref="ArgumentNullException"/> in this case.
	/// </summary>
	[Fact]
	[Priority(TestOrder.NonModifying)]
	public void CheckName_NameIsNull()
	{
		var exception = Assert.Throws<ArgumentNullException>(() => LogWriter.CheckName(null!));
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
		LogWriter? writerInEvent = null;
		LogLevel? levelInEvent = null;
		string? messageInEvent = null;
		ManualResetEventSlim messageWrittenEvent = new(false);

		LogWriter writer = LogWriter.Get("MyWriter");
		LogLevel level = LogLevel.Notice;
		Assert.True(writer.IsLogLevelActive(level));
		try
		{
			LogWriter.LogMessageWritten += MessageWrittenHandler;
			write(writer, level);
			messageWrittenEvent.Wait();
			Thread.MemoryBarrier();
			Assert.Same(writer, writerInEvent);
			Assert.Same(level, levelInEvent);
			Assert.Equal(expectedMessage, messageInEvent);
		}
		finally
		{
			LogWriter.LogMessageWritten -= MessageWrittenHandler;
		}
		return;

		void MessageWrittenHandler(LogWriter logWriter, LogLevel logLevel, string message)
		{
			writerInEvent = logWriter;
			levelInEvent = logLevel;
			messageInEvent = message;
			Thread.MemoryBarrier();
			messageWrittenEvent.Set();
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
				try
				{
					throw new Exception($"inner exception {i} (single-line)");
				}
				catch (Exception ex)
				{
					throw new Exception($"outer exception {i}\r\n(multi-line)", ex);
				}
			}
			catch (Exception ex)
			{
				throw new AggregateException(
					ex,
					new Exception("exception without stack-trace"));
			}
		}
		catch (Exception ex)
		{
			return ex;
		}
	}

	/// <summary>
	/// Produces the specified number of <see cref="Exception"/> objects with <see cref="Exception"/> objects as inner exceptions.
	/// </summary>
	/// <returns>The requested number of exception objects.</returns>
	private static Exception[] ProduceNestedExceptions(int count)
	{
		var exceptions = new Exception[count];
		for (int i = 0; i < count; i++) exceptions[i] = ProduceNestedException(i);
		return exceptions;
	}

	/// <summary>
	/// Formats the specified <see cref="Exception"/> using the <see cref="PrettyFormatter"/> with the standard preset.
	/// </summary>
	/// <param name="exception">Exception to format.</param>
	/// <returns>The formatted exception string.</returns>
	private static string UnwrapException(Exception exception) => PrettyFormatter.Format(exception, PrettyPresets.Standard);

	/// <summary>
	/// Formats the specified runtime metadata object into a human-readable string representation.
	/// </summary>
	/// <param name="value">
	/// The runtime metadata object to format. This can be a <see cref="Type"/>, an array of <see cref="Type"/>, an
	/// <see cref="IEnumerable{T}"/> of <see cref="Type"/>, a <see cref="MemberInfo"/>, an <see cref="Exception"/>, an
	/// <see cref="Assembly"/>, a <see cref="Module"/>, or any other object.
	/// </param>
	/// <returns>
	/// A string representation of the specified runtime metadata object. If the object is not a recognized metadata type,
	/// the method returns the result of <see cref="Convert.ToString(object, IFormatProvider)"/> or an empty string if the
	/// conversion yields <see langword="null"/>.
	/// </returns>
	/// <remarks>
	/// This method uses a standard formatting preset for recognized metadata types to produce a consistent
	/// and human-readable output. For unrecognized types, the method falls back to a culture-invariant string
	/// conversion.
	/// </remarks>
	private static string FormatRuntimeMetadata(object? value)
	{
		return value switch
		{
			Type type                      => PrettyFormatter.Format(type, PrettyPresets.Standard),
			Type[] types                   => PrettyFormatter.Format(types, PrettyPresets.Standard),
			IEnumerable<Type> typeSequence => PrettyFormatter.Format(typeSequence, PrettyPresets.Standard),
			ParameterInfo parameterInfo    => PrettyFormatter.Format(parameterInfo, PrettyPresets.Standard),
			MemberInfo memberInfo          => PrettyFormatter.Format(memberInfo, PrettyPresets.Standard),
			Exception exception            => PrettyFormatter.Format(exception, PrettyPresets.Standard),
			Assembly assembly              => PrettyFormatter.Format(assembly, PrettyPresets.Standard),
			AssemblyName assemblyName      => PrettyFormatter.Format(assemblyName, PrettyPresets.Standard),
			Module module                  => PrettyFormatter.Format(module, PrettyPresets.Standard),
			var _                          => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
		};
	}

	/// <summary>
	/// Tests the behavior of a logging action with generic arguments,
	/// ensuring proper formatting of runtime metadata at specific argument positions.
	/// </summary>
	/// <param name="argumentCount">The number of arguments to include in the format string.</param>
	/// <param name="argument">The test argument to be formatted and inserted at specific positions.</param>
	/// <param name="action">
	/// The logging action to test. The action takes three parameters: the format string, the array of arguments,
	/// and the expected formatted string.
	/// </param>
	/// <remarks>
	/// This method dynamically constructs a format string based on the specified number of arguments and tests the
	/// logging action by inserting the provided test argument at each position. It verifies that the action correctly
	/// formats the runtime metadata of the argument at the intended position.
	/// </remarks>
	private void TestWriteWithGenericArgumentsUsingPrettyFormatter(
		int                              argumentCount,
		object                           argument,
		Action<string, object[], string> action)
	{
		// Build the format string dynamically.
		StringBuilder formatBuilder = new();
		formatBuilder.Append('|');
		for (int i = 0; i < argumentCount; i++)
		{
			formatBuilder.Append('{').Append(i).Append("}|");
		}
		string format = formatBuilder.ToString();

		// Test each argument position individually.
		for (int i = 0; i < argumentCount; i++)
		{
			// Prepare the arguments with the test argument at position i.
			object[] args = new object[argumentCount];
			for (int j = 0; j < argumentCount; j++)
			{
				args[j] = i == j ? FormatRuntimeMetadata(argument) : 'X';
			}

			// Prepare the expected formatted string.
			string expected = string.Format(format, args);

			// Write the log message and check whether it performs proper pretty formatting of runtime metadata
			// at the correct argument position.
			action(format, args, expected);
		}
	}

	/// <summary>
	/// Gets a collection of metadata-related objects, including types, members, exceptions, and assemblies.
	/// </summary>
	/// <remarks>
	/// The collection includes examples of generic types, type arrays, lists of types, method members,
	/// exceptions, assemblies, and modules. This property is useful for testing or demonstrating functionality that
	/// processes or inspects metadata.
	/// </remarks>
	public static IEnumerable<object[]> PrettyMetadataArguments
	{
		get
		{
			Type genericType = typeof(Dictionary<int, string>);
			MethodInfo method = genericType.GetMethod(
				nameof(Dictionary<int, string>.TryGetValue),
				[typeof(int), typeof(string).MakeByRefType()])!;

			// Type
			yield return [genericType];

			// Type Array
			Type[] typeArray = [typeof(int), genericType];
			yield return [typeArray];

			// Type Sequence
			var typeList = new List<Type> { typeof(int), genericType };
			yield return [typeList];

			// Parameter
			ParameterInfo parameter = method.GetParameters()[1]; // the 'value' parameter
			yield return [parameter];

			// Member
			MemberInfo member = method;
			yield return [member];

			// Exception
			var exception = new InvalidOperationException("Boom!");
			yield return [exception];

			// Assembly
			Assembly assembly = genericType.Assembly;
			yield return [assembly];

			// AssemblyName
			yield return [assembly.GetName()];

			// Module
			Module module = genericType.Module;
			yield return [module];
		}
	}

	/// <summary>
	/// Prepares an array of runtime metadata objects and generates a formatted string representation of their values.
	/// </summary>
	/// <param name="format">
	/// When this method returns, contains the format string used to generate the expected output.
	/// This parameter is passed uninitialized.
	/// </param>
	/// <param name="expected">
	/// When this method returns, contains the formatted string representation of the runtime metadata objects.
	/// This parameter is passed uninitialized.
	/// </param>
	/// <returns>
	/// An array of runtime metadata objects, including types, methods, parameters, exceptions, and assemblies, in the
	/// order they are used in the format string.
	/// </returns>
	private static object[] PrepareWriteWithVArgs(out string format, out string expected)
	{
		// Create the various runtime metadata objects (one of each kind).
		Type metadataType = typeof(Dictionary<int, string>);
		MethodInfo method = metadataType.GetMethod(nameof(Dictionary<int, string>.TryGetValue), [typeof(int), typeof(string).MakeByRefType()])!;
		Type[] typeArray = [typeof(int), typeof(string)];
		var typeSequence = new List<Type> { typeof(int), typeof(Dictionary<int, string>) };
		ParameterInfo parameter = method.GetParameters()[1]; // the 'value' parameter
		MemberInfo member = method;
		var exception = new InvalidOperationException("Boom!");
		Assembly assembly = metadataType.Assembly;
		AssemblyName assemblyName = metadataType.Assembly.GetName();
		Module module = metadataType.Module;

		// Create the argument array.
		object[] args = [metadataType, typeArray, typeSequence, parameter, member, exception, assembly, assemblyName, module];

		// Create the expected formatted string.
		format = "type={0}; types={1}; seq={2}; parameter={3}; member={4}; exception={5}; assembly={6}; assemblyName={7}; module={8}";
		expected = string.Format(
			CultureInfo.InvariantCulture,
			format,
			FormatRuntimeMetadata(metadataType),
			FormatRuntimeMetadata(typeArray),
			FormatRuntimeMetadata(typeSequence),
			FormatRuntimeMetadata(parameter),
			FormatRuntimeMetadata(member),
			FormatRuntimeMetadata(exception),
			FormatRuntimeMetadata(assembly),
			FormatRuntimeMetadata(assemblyName),
			FormatRuntimeMetadata(module));
		return args;
	}

	#endregion

	#region Write(LogLevel level, string message)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write(LogLevel,string)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 1)]
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
	[Priority(TestOrder.ModifyingBase + 2)]
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
	[Priority(TestOrder.ModifyingBase + 3)]
	public void Write_Formatting_GenericArguments_1_WithExceptionToUnwrap()
	{
		Exception[] exceptions = ProduceNestedExceptions(1);
		Write_Common(
			(writer, level) => writer.Write(level, "{0}", exceptions[0]),
			UnwrapException(exceptions[0]));
	}

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0}(LogLevel,string,T0)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 4)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_1_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			1,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1>(LogLevel level, string message, T0 arg0, T1 arg1)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1}(LogLevel,string,T0,T1)"/> with objects to format regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1}(IFormatProvider,LogLevel,string,T0,T1)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 5)]
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
	[Priority(TestOrder.ModifyingBase + 6)]
	public void Write_Formatting_GenericArguments_2_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(2);
		Write_Common(
			(writer, level) => writer.Write(level, "{0} {1}", e[0], e[1]),
			UnwrapException(e[0]) + " " +
			UnwrapException(e[1]));
	}

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1}(LogLevel,string,T0,T1)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 7)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_2_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			2,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1,T2>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2}(LogLevel,string,T0,T1,T2)"/> with objects to format regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2}(IFormatProvider,LogLevel,string,T0,T1,T2)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 8)]
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
	[Priority(TestOrder.ModifyingBase + 9)]
	public void Write_Formatting_GenericArguments_3_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(3);
		Write_Common(
			(writer, level) => writer.Write(level, "{0} {1} {2}", e[0], e[1], e[2]),
			UnwrapException(e[0]) + " " +
			UnwrapException(e[1]) + " " +
			UnwrapException(e[2]));
	}

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2}(LogLevel,string,T0,T1,T2)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 10)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_3_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			3,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1,T2,T3>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3}(LogLevel,string,T0,T1,T2,T3)"/> with objects to format regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3}(IFormatProvider,LogLevel,string,T0,T1,T2,T3)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 11)]
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
	[Priority(TestOrder.ModifyingBase + 12)]
	public void Write_Formatting_GenericArguments_4_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(4);
		Write_Common(
			(writer, level) => writer.Write(level, "{0} {1} {2} {3}", e[0], e[1], e[2], e[3]),
			UnwrapException(e[0]) + " " +
			UnwrapException(e[1]) + " " +
			UnwrapException(e[2]) + " " +
			UnwrapException(e[3]));
	}

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3}(LogLevel,string,T0,T1,T2,T3)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 13)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_4_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			4,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1,T2,T3,T4>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4}(LogLevel,string,T0,T1,T2,T3,T4)"/> with objects to format regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 14)]
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
	[Priority(TestOrder.ModifyingBase + 15)]
	public void Write_Formatting_GenericArguments_5_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(5);
		Write_Common(
			(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4}", e[0], e[1], e[2], e[3], e[4]),
			UnwrapException(e[0]) + " " +
			UnwrapException(e[1]) + " " +
			UnwrapException(e[2]) + " " +
			UnwrapException(e[3]) + " " +
			UnwrapException(e[4]));
	}

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4}(LogLevel,string,T0,T1,T2,T3,T4)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 16)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_5_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			5,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1,T2,T3,T4,T5>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5}(LogLevel,string,T0,T1,T2,T3,T4,T5)"/> with objects to format
	/// regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 17)]
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
	[Priority(TestOrder.ModifyingBase + 18)]
	public void Write_Formatting_GenericArguments_6_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(6);
		Write_Common(
			(writer, level) => writer.Write(level, "{0} {1} {2} {3} {4} {5}", e[0], e[1], e[2], e[3], e[4], e[5]),
			UnwrapException(e[0]) + " " +
			UnwrapException(e[1]) + " " +
			UnwrapException(e[2]) + " " +
			UnwrapException(e[3]) + " " +
			UnwrapException(e[4]) + " " +
			UnwrapException(e[5]));
	}

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5}(LogLevel,string,T0,T1,T2,T3,T4,T5)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 19)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_6_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			6,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1,T2,T3,T4,T5,T6>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6)"/> with objects to format
	/// regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 20)]
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
	[Priority(TestOrder.ModifyingBase + 21)]
	public void Write_Formatting_GenericArguments_7_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(7);
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

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 22)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_7_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			7,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5],
							args[6]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1,T2,T3,T4,T5,T6,T7>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7)"/> with objects to
	/// format regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 23)]
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
	[Priority(TestOrder.ModifyingBase + 24)]
	public void Write_Formatting_GenericArguments_8_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(8);
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

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 25)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_8_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			8,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5],
							args[6],
							args[7]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8)"/> with objects
	/// to format regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 26)]
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
	[Priority(TestOrder.ModifyingBase + 27)]
	public void Write_Formatting_GenericArguments_9_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(9);
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

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 28)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_9_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			9,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5],
							args[6],
							args[7],
							args[8]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/> with
	/// objects to format regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 29)]
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
	[Priority(TestOrder.ModifyingBase + 30)]
	public void Write_Formatting_GenericArguments_10_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(10);
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

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 31)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_10_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			10,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5],
							args[6],
							args[7],
							args[8],
							args[9]);
					},
					expected);
			});
	}

	#endregion

	#region Write<T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10>(LogLevel level, string message, T0 arg0, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8, T9 arg9, T10 arg10)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10)"/>
	/// with objects to format regularly.
	/// Implicitly tests <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10}(IFormatProvider,LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10)"/>.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 32)]
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
	[Priority(TestOrder.ModifyingBase + 33)]
	public void Write_Formatting_GenericArguments_11_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(11);
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

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 34)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_11_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			11,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5],
							args[6],
							args[7],
							args[8],
							args[9],
							args[10]);
					},
					expected);
			});
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
	[Priority(TestOrder.ModifyingBase + 35)]
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
	[Priority(TestOrder.ModifyingBase + 36)]
	public void Write_Formatting_GenericArguments_12_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(12);
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

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 37)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_12_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			12,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5],
							args[6],
							args[7],
							args[8],
							args[9],
							args[10],
							args[11]);
					},
					expected);
			});
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
	[Priority(TestOrder.ModifyingBase + 38)]
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
	[Priority(TestOrder.ModifyingBase + 39)]
	public void Write_Formatting_GenericArguments_13_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(13);
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

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 40)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_13_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			13,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5],
							args[6],
							args[7],
							args[8],
							args[9],
							args[10],
							args[11],
							args[12]);
					},
					expected);
			});
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
	[Priority(TestOrder.ModifyingBase + 41)]
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
	[Priority(TestOrder.ModifyingBase + 42)]
	public void Write_Formatting_GenericArguments_14_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(14);
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

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 43)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_14_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			14,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5],
							args[6],
							args[7],
							args[8],
							args[9],
							args[10],
							args[11],
							args[12],
							args[13]);
					},
					expected);
			});
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
	[Priority(TestOrder.ModifyingBase + 44)]
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
	[Priority(TestOrder.ModifyingBase + 45)]
	public void Write_Formatting_GenericArguments_15_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(15);
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

	/// <summary>
	/// Tests writing a log message using
	/// <see cref="LogWriter.Write{T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14}(LogLevel,string,T0,T1,T2,T3,T4,T5,T6,T7,T8,T9,T10,T11,T12,T13,T14)"/>
	/// with runtime metadata that is pretty formatted.
	/// </summary>
	/// <param name="argument">The metadata instance to format.</param>
	[Theory]
	[Priority(TestOrder.ModifyingBase + 46)]
	[MemberData(nameof(PrettyMetadataArguments))]
	public void Write_Formatting_GenericArguments_15_PrettyFormatsRuntimeMetadata(object argument)
	{
		TestWriteWithGenericArgumentsUsingPrettyFormatter(
			15,
			argument,
			(format, args, expected) =>
			{
				Write_Common(
					(writer, level) =>
					{
						writer.Write(
							level,
							format,
							args[0],
							args[1],
							args[2],
							args[3],
							args[4],
							args[5],
							args[6],
							args[7],
							args[8],
							args[9],
							args[10],
							args[11],
							args[12],
							args[13],
							args[14]);
					},
					expected);
			});
	}

	#endregion

	#region Write(LogLevel level, string message, object[] args)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write(LogLevel,string,object[])"/>
	/// with objects to format regularly.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 47)]
	public void Write_Formatting_WithVArgs_Regular()
	{
		Write_Common(
			(writer, level) => writer.Write(level, "text: {0} {1} {2}", [1, 2, 3]),
			"text: 1 2 3");
	}

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write(LogLevel,string,object[])"/>
	/// with exceptions that should be unwrapped.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 48)]
	public void Write_Formatting_WithVArgs_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(2);
		Write_Common(
			(writer, level) => writer.Write(level, "{0} {1}", [e[0], e[1]]),
			UnwrapException(e[0]) + " " + UnwrapException(e[1]));
	}

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write(LogLevel,string,object[])"/> with runtime metadata.
	/// Ensures arguments are formatted with the <see cref="PrettyFormatter"/> and the original argument array remains unchanged.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 49)]
	public void Write_Formatting_WithVArgs_PrettyFormatsRuntimeMetadata()
	{
		// Prepare the arguments and expected formatted string.
		object[] args = PrepareWriteWithVArgs(out string format, out string expected);

		// Take a snapshot of the original args to verify they remain unchanged.
		object[] snapshot = (object[])args.Clone();

		// Write the log message performing pretty formatting of runtime metadata.
		Write_Common(
			(writer, level) => writer.Write(level, format, args),
			expected);

		// Verify original args remain unchanged.
		for (int i = 0; i < args.Length; i++)
		{
			Assert.Same(snapshot[i], args[i]);
		}
	}

	#endregion

	#region ForceWrite(LogLevel level, string message, object[] args)

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.ForceWrite(LogLevel,string,object[])"/>
	/// with objects to format regularly.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 50)]
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
	[Priority(TestOrder.ModifyingBase + 51)]
	public void ForceWrite_Formatting_WithVArgs_WithExceptionsToUnwrap()
	{
		Exception[] e = ProduceNestedExceptions(2);
		Write_Common(
			(writer, level) => writer.ForceWrite(level, "{0} {1}", e[0], e[1]),
			UnwrapException(e[0]) + " " + UnwrapException(e[1]));
	}

	/// <summary>
	/// Tests writing a log message using <see cref="LogWriter.Write(LogLevel,string,object[])"/> with runtime metadata.
	/// Ensures arguments are formatted with the <see cref="PrettyFormatter"/> and the original argument array remains unchanged.
	/// </summary>
	[Fact]
	[Priority(TestOrder.ModifyingBase + 52)]
	public void ForceWrite_Formatting_WithVArgs_PrettyFormatsRuntimeMetadata()
	{
		// Prepare the arguments and expected formatted string.
		object[] args = PrepareWriteWithVArgs(out string format, out string expected);

		// Take a snapshot of the original args to verify they remain unchanged.
		object[] snapshot = (object[])args.Clone();

		// Write the log message performing pretty formatting of runtime metadata.
		Write_Common(
			(writer, level) => writer.Write(level, format, args),
			expected);

		// Verify original args remain unchanged.
		for (int i = 0; i < args.Length; i++)
		{
			Assert.Same(snapshot[i], args[i]);
		}
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
		foreach (LogWriter writer in LogWriter.KnownWriters)
		{
			string expected = $"{writer.Name} ({writer.Id})";
			Assert.Equal(expected, writer.ToString());
		}
	}

	#endregion
}
