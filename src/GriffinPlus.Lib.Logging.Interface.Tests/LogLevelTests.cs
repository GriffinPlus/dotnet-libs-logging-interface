///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;

using Xunit;

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Unit tests targeting the <see cref="LogLevel"/> class.
	/// </summary>
	[Collection("LogLevelTests")]
	public class LogLevelTests
	{
		#region Expected Results

		private readonly struct LogLevelItem
		{
			public readonly int    Id;
			public readonly string Name;

			public LogLevelItem(int id, string name)
			{
				Id = id;
				Name = name;
			}
		}

		private static readonly LogLevelItem[] sExpectedPredefinedLogLevels =
		{
			new LogLevelItem(0, "Emergency"),
			new LogLevelItem(1, "Alert"),
			new LogLevelItem(2, "Critical"),
			new LogLevelItem(3, "Error"),
			new LogLevelItem(4, "Warning"),
			new LogLevelItem(5, "Notice"),
			new LogLevelItem(6, "Informational"),
			new LogLevelItem(7, "Debug"),
			new LogLevelItem(8, "Trace")
		};

		private static readonly LogLevelItem[] sExpectedKnownLogLevels =
		{
			new LogLevelItem(0, "Emergency"),
			new LogLevelItem(1, "Alert"),
			new LogLevelItem(2, "Critical"),
			new LogLevelItem(3, "Error"),
			new LogLevelItem(4, "Warning"),
			new LogLevelItem(5, "Notice"),
			new LogLevelItem(6, "Informational"),
			new LogLevelItem(7, "Debug"),
			new LogLevelItem(8, "Trace"),
			new LogLevelItem(9, "Timing")
		};

		#endregion

		#region FirstAspectId

		/// <summary>
		/// Tests the <see cref="LogLevel.FirstAspectId"/> property.
		/// </summary>
		[Fact]
		public void FirstAspectId()
		{
			int expected = LogLevel.Timing.Id; // the first predefined aspect level
			Assert.Equal(expected, LogLevel.FirstAspectId);
		}

		#endregion

		#region MaxId

		/// <summary>
		/// Tests the <see cref="LogLevel.MaxId"/> property.
		/// </summary>
		[Fact]
		public void MaxId()
		{
			// the 'Timing' aspect log level is the one and only aspect log level and therefore the
			// log level with the greatest assigned id
			Assert.Equal(LogLevel.Timing.Id, LogLevel.MaxId);
		}

		#endregion

		#region None

		/// <summary>
		/// Tests the <see cref="LogLevel.None"/> property.
		/// Checks whether the special log level 'None' has the expected name and id.
		/// </summary>
		[Fact]
		public void None()
		{
			Assert.Equal(-1, LogLevel.None.Id);
			Assert.Equal("None", LogLevel.None.Name);
		}

		#endregion

		#region All

		/// <summary>
		/// Tests the <see cref="LogLevel.All"/> property.
		/// Checks whether the special log level 'All' has the expected name and id.
		/// </summary>
		[Fact]
		public void All()
		{
			Assert.Equal(int.MaxValue, LogLevel.All.Id);
			Assert.Equal("All", LogLevel.All.Name);
		}

		#endregion

		#region Emergency

		/// <summary>
		/// Tests the <see cref="LogLevel.Emergency"/> property.
		/// </summary>
		[Fact]
		public void Emergency()
		{
			Assert.Equal(sExpectedPredefinedLogLevels[0].Id, LogLevel.Emergency.Id);
			Assert.Equal("Emergency", LogLevel.Emergency.Name);
		}

		#endregion

		#region Alert

		/// <summary>
		/// Tests the <see cref="LogLevel.Alert"/> property.
		/// </summary>
		[Fact]
		public void Alert()
		{
			Assert.Equal(sExpectedPredefinedLogLevels[1].Id, LogLevel.Alert.Id);
			Assert.Equal("Alert", LogLevel.Alert.Name);
		}

		#endregion

		#region Alert

		/// <summary>
		/// Tests the <see cref="LogLevel.Critical"/> property.
		/// </summary>
		[Fact]
		public void Critical()
		{
			Assert.Equal(sExpectedPredefinedLogLevels[2].Id, LogLevel.Critical.Id);
			Assert.Equal("Critical", LogLevel.Critical.Name);
		}

		#endregion

		#region Error

		/// <summary>
		/// Tests the <see cref="LogLevel.Error"/> property.
		/// </summary>
		[Fact]
		public void Error()
		{
			Assert.Equal(sExpectedPredefinedLogLevels[3].Id, LogLevel.Error.Id);
			Assert.Equal("Error", LogLevel.Error.Name);
		}

		#endregion

		#region Warning

		/// <summary>
		/// Tests the <see cref="LogLevel.Warning"/> property.
		/// </summary>
		[Fact]
		public void Warning()
		{
			Assert.Equal(sExpectedPredefinedLogLevels[4].Id, LogLevel.Warning.Id);
			Assert.Equal("Warning", LogLevel.Warning.Name);
		}

		#endregion

		#region Notice

		/// <summary>
		/// Tests the <see cref="LogLevel.Notice"/> property.
		/// </summary>
		[Fact]
		public void Notice()
		{
			Assert.Equal(sExpectedPredefinedLogLevels[5].Id, LogLevel.Notice.Id);
			Assert.Equal("Notice", LogLevel.Notice.Name);
		}

		#endregion

		#region Informational

		/// <summary>
		/// Tests the <see cref="LogLevel.Informational"/> property.
		/// </summary>
		[Fact]
		public void Informational()
		{
			Assert.Equal(sExpectedPredefinedLogLevels[6].Id, LogLevel.Informational.Id);
			Assert.Equal("Informational", LogLevel.Informational.Name);
		}

		#endregion

		#region Debug

		/// <summary>
		/// Tests the <see cref="LogLevel.Debug"/> property.
		/// </summary>
		[Fact]
		public void Debug()
		{
			Assert.Equal(sExpectedPredefinedLogLevels[7].Id, LogLevel.Debug.Id);
			Assert.Equal("Debug", LogLevel.Debug.Name);
		}

		#endregion

		#region Trace

		/// <summary>
		/// Tests the <see cref="LogLevel.Trace"/> property.
		/// </summary>
		[Fact]
		public void Trace()
		{
			Assert.Equal(sExpectedPredefinedLogLevels[8].Id, LogLevel.Trace.Id);
			Assert.Equal("Trace", LogLevel.Trace.Name);
		}

		#endregion

		#region PredefinedLogLevels

		/// <summary>
		/// Tests the <see cref="LogLevel.PredefinedLogLevels"/> property.
		/// </summary>
		[Fact]
		public void PredefinedLogLevels()
		{
			var levels = LogLevel.PredefinedLogLevels.ToArray();
			Assert.Equal(sExpectedPredefinedLogLevels.Length, levels.Length);

			for (int i = 0; i < levels.Length; i++)
			{
				Assert.Equal(sExpectedPredefinedLogLevels[i].Id, levels[i].Id);
				Assert.Equal(sExpectedPredefinedLogLevels[i].Name, levels[i].Name);
			}
		}

		#endregion

		#region KnownLevels

		/// <summary>
		/// Tests the <see cref="LogLevel.KnownLevels"/> property.
		/// </summary>
		[Fact]
		public void KnownLevels()
		{
			var levels = LogLevel.KnownLevels.ToArray();
			Assert.Equal(sExpectedKnownLogLevels.Length, levels.Length);

			for (int i = 0; i < levels.Length; i++)
			{
				Assert.Equal(sExpectedKnownLogLevels[i].Id, levels[i].Id);
				Assert.Equal(sExpectedKnownLogLevels[i].Name, levels[i].Name);
			}
		}

		#endregion

		#region KnownLevelByName

		/// <summary>
		/// Tests the <see cref="LogLevel.KnownLevelsByName"/> property.
		/// </summary>
		[Fact]
		public void KnownLevelsByName()
		{
			// set up a new dictionary with the same elements to work with
			var remaining = LogLevel.KnownLevelsByName.ToDictionary(
				x => x.Key,
				x => x.Value);

			// the dictionary contains the special log levels 'None' and 'All'
			// => remove them before comparing the other log levels
			Assert.True(remaining.Remove("None"));
			Assert.True(remaining.Remove("All"));

			// the number of log levels in the dictionary should now reflect all regular log levels
			Assert.Equal(sExpectedKnownLogLevels.Length, remaining.Count);

			for (int i = 0; i < sExpectedKnownLogLevels.Length; i++)
			{
				int expectedId = sExpectedKnownLogLevels[i].Id;
				string expectedName = sExpectedKnownLogLevels[i].Name;
				var level = LogLevel.KnownLevelsByName[expectedName];
				Assert.Equal(expectedId, level.Id);
				Assert.Equal(expectedName, level.Name);
				remaining.Remove(expectedName);
			}

			// all log levels should have been found
			Assert.Empty(remaining);
		}

		#endregion

		#region implicit operator string(LogLevel level)

		/// <summary>
		/// Tests the <see cref="LogLevel.op_Implicit"/> conversion operator.
		/// </summary>
		[Fact]
		public void OperatorString()
		{
			foreach (var level in LogLevel.KnownLevels)
			{
				string expected = level.Name;
				Assert.Equal(expected, level);
			}
		}

		#endregion

		#region CheckName(string name)

		/// <summary>
		/// Tests the <see cref="LogLevel.CheckName"/> method.
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
				LogLevel.CheckName(name);
			}
			else
			{
				var exception = Assert.Throws<ArgumentException>(() => LogLevel.CheckName(name));
			}
		}

		/// <summary>
		/// Tests the <see cref="LogLevel.CheckName"/> method passing <c>null</c>.
		/// The method should throw an <see cref="ArgumentNullException"/> in this case.
		/// </summary>
		[Fact]
		public void CheckName_NameIsNull()
		{
			var exception = Assert.Throws<ArgumentNullException>(() => LogLevel.CheckName(null));
			Assert.Equal("name", exception.ParamName);
		}

		#endregion

		#region ToString()

		/// <summary>
		/// Tests the <see cref="LogLevel.ToString"/> method.
		/// </summary>
		[Fact]
#pragma warning disable xUnit1024 // Test methods cannot have overloads
		public new void ToString()
#pragma warning restore xUnit1024 // Test methods cannot have overloads
		{
			foreach (var level in LogLevel.KnownLevels)
			{
				string expected = $"{level.Name} ({level.Id})";
				Assert.Equal(expected, level.ToString());
			}
		}

		#endregion
	}

}
