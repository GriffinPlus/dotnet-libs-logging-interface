///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Linq;
using System.Text;

using Xunit;

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Unit tests targeting the <see cref="LogWriterTag"/> class.
	/// </summary>
	public class LogWriterTagTests
	{
		private const string ValidCharSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_.,:;+-#()[]{}<>";

		#region CheckTag()

		/// <summary>
		/// Generates a random set of valid tags and tests whether all tags pass the <see cref="LogWriterTag.CheckTag"/> method.
		/// </summary>
		[Fact]
		public void CheckTag_Valid()
		{
			var builder = new StringBuilder();
			var random = new Random(0);
			for (int sample = 0; sample < 10000; sample++)
			{
				builder.Clear();

				// generate a valid tag
				int length = random.Next(1, 50);
				for (int i = 0; i < length; i++)
				{
					int j = random.Next(0, ValidCharSet.Length - 1);
					builder.Append(ValidCharSet[j]);
				}

				// check the tag
				string tag = builder.ToString();
				LogWriterTag.CheckTag(tag);
			}
		}

		/// <summary>
		/// Generates a random set of invalid tags and tests whether all tags let the <see cref="LogWriterTag.CheckTag"/> method
		/// throw an exception.
		/// </summary>
		[Fact]
		public void CheckTag_Invalid()
		{
			var builder = new StringBuilder();
			var random = new Random(0);
			for (int sample = 0; sample < 10000; sample++)
			{
				builder.Clear();

				// generate a valid tag
				int length = random.Next(1, 50);
				for (int i = 0; i < length; i++)
				{
					int j = random.Next(0, ValidCharSet.Length - 1);
					builder.Append(ValidCharSet[j]);
				}

				// find a character that is not valid and inject it into the tag
				char c;
				do { c = (char)random.Next(0, 65535); } while (ValidCharSet.Contains(c));

				int index = random.Next(0, builder.Length - 1);
				builder.Insert(index, c);

				// check the tag
				string tag = builder.ToString();
				Assert.Throws<ArgumentException>(() => LogWriterTag.CheckTag(tag));
			}
		}

		#endregion
	}

}
