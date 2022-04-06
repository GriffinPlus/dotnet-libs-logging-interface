///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// Unit tests targeting the <see cref="LogWriterTagSet"/> class.
	/// </summary>
	public class LogWriterTagSetTests
	{
		#region Construction

		public static IEnumerable<object[]> CreateTestData
		{
			get
			{
				// empty tag set
				yield return new object[]
				{
					Array.Empty<string>(),
					Array.Empty<string>()
				};

				// single element in the tag set
				yield return new object[]
				{
					new[] { "Tag" },
					new[] { "Tag" }
				};

				// mixed set of unordered elements
				yield return new object[]
				{
					new[] { "A", "C", "D", "B", "E", "e", "d", "c", "b", "a" },
					new[] { "A", "a", "B", "b", "C", "c", "D", "d", "E", "e" }
				};

				// mixed set with duplicates
				yield return new object[]
				{
					new[] { "A", "B", "C", "D", "E", "A", "B", "C", "D", "E" },
					new[] { "A", "B", "C", "D", "E" }
				};
			}
		}

		/// <summary>
		/// Tests whether the constructor succeeds creating a tag set with valid parameters.
		/// </summary>
		/// <param name="tags">Tags to pass to the constructor.</param>
		/// <param name="expected">The expected tags in the tag set.</param>
		[Theory]
		[MemberData(nameof(CreateTestData))]
		public void Create_Success(string[] tags, string[] expected)
		{
			var logWriterTags = tags.Select(LogWriter.GetTag).ToArray();
			var expectedLogWriterTags = expected.Select(LogWriter.GetTag).ToArray();
			var tagSet = new LogWriterTagSet(logWriterTags);
			Assert.Equal(expectedLogWriterTags.Length, tagSet.Count);
			Assert.Equal(expectedLogWriterTags, tagSet.ToArray<LogWriterTag>());
		}

		/// <summary>
		/// Tests whether the constructor fails when passing a null reference.
		/// </summary>
		[Fact]
		public void Create_TagsIsNull()
		{
			Assert.Throws<ArgumentNullException>(() => new LogWriterTagSet(null));
		}

		#endregion

		#region Indexer

		/// <summary>
		/// Tests whether the indexer is working properly.
		/// </summary>
		[Theory]
		[InlineData("Tag1", "Tag2", "Tag3", "Tag4", "Tag5")] // already ordered
		public void Indexer_Success(params string[] tags)
		{
			var logWriterTags = tags.Select(LogWriter.GetTag).ToArray();
			var tagSet = new LogWriterTagSet(logWriterTags);
			for (int i = 0; i < tags.Length; i++) Assert.Equal(logWriterTags[i], tagSet[i]);
		}

		/// <summary>
		/// Tests whether the indexer fails when passing an index that is out of bounds.
		/// </summary>
		[Theory]
		[InlineData("Tag1", "Tag2", "Tag3", "Tag4", "Tag5")] // already ordered
		public void Indexer_IndexOutOfRange(params string[] tags)
		{
			var logWriterTags = tags.Select(LogWriter.GetTag).ToArray();
			var tagSet = new LogWriterTagSet(logWriterTags);
			Assert.Throws<IndexOutOfRangeException>(() => tagSet[-1]);          // below lower bound
			Assert.Throws<IndexOutOfRangeException>(() => tagSet[tags.Length]); // above upper bound
		}

		#endregion

		#region GetEnumerator()

		/// <summary>
		/// Tests whether the enumerator is working properly.
		/// </summary>
		[Theory]
		[InlineData("Tag1", "Tag2", "Tag3", "Tag4", "Tag5")] // already ordered
		public void GetEnumerator(params string[] tags)
		{
			var logWriterTags = tags.Select(LogWriter.GetTag).ToArray();
			var tagSet = new LogWriterTagSet(logWriterTags);
			int i = 0;
			using (var enumerator = tagSet.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Assert.Equal(logWriterTags[i++], enumerator.Current);
				}
			}
		}

		#endregion

		#region Operator==

		public static IEnumerable<object[]> OperatorEquality_TestData
		{
			get
			{
				var a = LogWriter.GetTag("A");
				var b = LogWriter.GetTag("B");

				// equal
				yield return new object[] { true, null, null };
				yield return new object[] { true, LogWriterTagSet.Empty, LogWriterTagSet.Empty };
				yield return new object[] { true, new LogWriterTagSet(a), new LogWriterTagSet(a) };
				yield return new object[] { true, new LogWriterTagSet(a, b), new LogWriterTagSet(a, b) };

				// not equal
				yield return new object[] { false, new LogWriterTagSet(a), null };
				yield return new object[] { false, new LogWriterTagSet(a), LogWriterTagSet.Empty };
				yield return new object[] { false, null, new LogWriterTagSet(a) };
				yield return new object[] { false, LogWriterTagSet.Empty, new LogWriterTagSet(a) };
				yield return new object[] { false, new LogWriterTagSet(a), new LogWriterTagSet(b) };
				yield return new object[] { false, new LogWriterTagSet(a), new LogWriterTagSet(a, b) };
				yield return new object[] { false, new LogWriterTagSet(a, b), new LogWriterTagSet(b) };
			}
		}

		/// <summary>
		/// Tests whether operator== works properly.
		/// </summary>
		[Theory]
		[MemberData(nameof(OperatorEquality_TestData))]
		public void OperatorEquality(bool expected, LogWriterTagSet left, LogWriterTagSet right)
		{
			bool isEqual = left == right;
			Assert.Equal(expected, isEqual);
		}

		#endregion

		#region Operator!=

		public static IEnumerable<object[]> OperatorInequality_TestData
		{
			get
			{
				foreach (object[] data in OperatorEquality_TestData)
				{
					yield return new[] { !(bool)data[0], data[1], data[2] };
				}
			}
		}

		/// <summary>
		/// Tests whether operator!= works properly.
		/// </summary>
		[Theory]
		[MemberData(nameof(OperatorInequality_TestData))]
		public void OperatorInequality(bool expected, LogWriterTagSet left, LogWriterTagSet right)
		{
			bool isEqual = left != right;
			Assert.Equal(expected, isEqual);
		}

		#endregion
	}

}
