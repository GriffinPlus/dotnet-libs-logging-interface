///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable UnusedMember.Global
// ReSharper disable PossibleMultipleEnumeration

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// A sorted collection of log writer tags (immutable, thread-safe).<br/>
/// <br/>
/// Valid tags may consist of the following characters only:<br/>
/// - alphanumeric characters: <c>[a-z]</c>, <c>[A-Z]</c>, <c>[0-9]</c><br/>
/// - extra characters: [<c>_</c>, <c>.</c>, <c>,</c>, <c>:</c>, <c>;</c>, <c>+</c>, <c>-</c>, <c>#</c>]<br/>
/// - brackets: <c>()</c>, <c>[]</c>, <c>{}</c>, <c>&lt;&gt;</c><br/>
/// <br/>
/// Asterisk(<c>*</c>) and quotation mark (<c>?</c>) are not supported as these characters are used to implement
/// pattern matching with wildcards. Caret(<c>^</c>) and dollar sign (<c>$</c>) are not supported as these characters
/// are used to implement the detection of regex strings.
/// </summary>
public sealed class LogWriterTagSet : ITagSet, IReadOnlyList<LogWriterTag>, IEquatable<LogWriterTagSet>
{
	/// <summary>
	/// A comparer for <see cref="LogWriterTag"/> that compares by name.
	/// </summary>
	private sealed class TagComparer : IComparer<LogWriterTag>, IEqualityComparer<LogWriterTag>
	{
		private readonly StringComparison mComparison;

		/// <summary>
		/// Initializes a new instance of the <see cref="TagComparer"/> class.
		/// </summary>
		/// <param name="comparison">String comparison to use when comparing tag names.</param>
		public TagComparer(StringComparison comparison)
		{
			mComparison = comparison;
		}

		/// <summary>
		/// Compares the specified tags.
		/// </summary>
		/// <param name="x">Tag to compare.</param>
		/// <param name="y">Tag to compare with.</param>
		/// <returns>
		/// -1, if <paramref name="x"/> is less than <paramref name="y"/>;
		/// 1, if <paramref name="x"/> is greater than <paramref name="y"/>;
		/// 0, if both tags are equal.
		/// </returns>
		public int Compare(LogWriterTag x, LogWriterTag y)
		{
			if (ReferenceEquals(x, y)) return 0;
			if (ReferenceEquals(null, y)) return 1;
			if (ReferenceEquals(null, x)) return -1;
			return string.Compare(x.Name, y.Name, mComparison);
		}

		/// <summary>
		/// Checks whether the specified tags are equal.
		/// </summary>
		/// <param name="x">Tag to compare.</param>
		/// <param name="y">Tag to compare with.</param>
		/// <returns>
		/// <c>true</c> if both tags are equal;<br/>
		/// otherwise <c>false</c>.
		/// </returns>
		public bool Equals(LogWriterTag x, LogWriterTag y)
		{
			if (ReferenceEquals(x, y)) return true;
			if (ReferenceEquals(x, null)) return false;
			if (ReferenceEquals(y, null)) return false;
			return x.Name == y.Name;
		}

		/// <summary>
		/// Gets the hash code of the specified tag.
		/// </summary>
		/// <param name="tag">Tag to get the hash code for.</param>
		/// <returns>Hash code of the tag.</returns>
		public int GetHashCode(LogWriterTag tag)
		{
			return tag.Name != null ? tag.Name.GetHashCode() : 0;
		}
	}

	private static readonly TagComparer        sOrdinalComparer           = new(StringComparison.Ordinal);
	private static readonly TagComparer        sOrdinalIgnoreCaseComparer = new(StringComparison.OrdinalIgnoreCase);
	private static readonly List<LogWriterTag> sEmpty                     = [];
	private readonly        List<LogWriterTag> mTags;
	private readonly        int                mHashCode;

	/// <summary>
	/// Gets an empty tag set.
	/// </summary>
	public static LogWriterTagSet Empty { get; } = new();

	/// <summary>
	/// Initializes a new empty instance of the <see cref="LogWriterTagSet"/> class.
	/// </summary>
	public LogWriterTagSet()
	{
		mTags = sEmpty;
		mHashCode = CalculateHashCode(mTags);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="LogWriterTagSet"/> class with the specified tags.
	/// </summary>
	/// <param name="tags">Tags to keep in the collection.</param>
	public LogWriterTagSet(params LogWriterTag[] tags) :
		this((IEnumerable<LogWriterTag>)tags) { }

	/// <summary>
	/// Initializes a new instance of the <see cref="LogWriterTagSet"/> class with the specified tags.
	/// </summary>
	/// <param name="tags">Tags to keep in the collection.</param>
	public LogWriterTagSet(IEnumerable<LogWriterTag> tags)
	{
		if (tags == null) throw new ArgumentNullException(nameof(tags));
		mTags = [..new HashSet<LogWriterTag>(tags, sOrdinalComparer)];
		mTags.Sort(sOrdinalIgnoreCaseComparer);
		mHashCode = CalculateHashCode(mTags);
	}

	/// <summary>
	/// Gets the tag at the specified index.
	/// </summary>
	/// <param name="index">Index of the tag to get.</param>
	/// <returns>The tag at the specified index.</returns>
	public LogWriterTag this[int index]
	{
		get
		{
			if (index < 0 || index >= mTags.Count) throw new IndexOutOfRangeException("The specified index is out of bounds.");
			return mTags[index];
		}
	}

	/// <summary>
	/// Gets the tag at the specified index.
	/// </summary>
	/// <param name="index">Index of the tag to get.</param>
	/// <returns>The tag at the specified index.</returns>
	string IReadOnlyList<string>.this[int index] => this[index].Name;

	/// <summary>
	/// Gets the number of tags in the collection.
	/// </summary>
	public int Count => mTags.Count;

	/// <summary>
	/// Determines whether the left tag set and the right tag set are equal.
	/// </summary>
	/// <param name="left">Left tag set.</param>
	/// <param name="right">Right tag set.</param>
	/// <returns>
	/// <c>true</c>, if the specified tag sets are equal;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public static bool operator ==(LogWriterTagSet left, ITagSet right)
	{
		if (ReferenceEquals(left, null) && ReferenceEquals(right, null)) return true;
		return !ReferenceEquals(left, null) && left.Equals(right);
	}

	/// <summary>
	/// Determines whether the left tag set and the right tag set are not equal.
	/// </summary>
	/// <param name="left">Left tag set.</param>
	/// <param name="right">Right tag set.</param>
	/// <returns>
	/// <c>true</c>, if the specified tag sets are not equal;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public static bool operator !=(LogWriterTagSet left, ITagSet right)
	{
		if (ReferenceEquals(left, null) && ReferenceEquals(right, null)) return false;
		if (ReferenceEquals(left, null)) return true;
		return !left.Equals(right);
	}

	/// <summary>
	/// Gets an enumerator iterating over the tags.
	/// </summary>
	/// <returns>An enumerator iterating over the tags.</returns>
	public IEnumerator<LogWriterTag> GetEnumerator()
	{
		return mTags.GetEnumerator();
	}

	/// <summary>
	/// Gets an enumerator iterating over the tags.
	/// </summary>
	/// <returns>An enumerator iterating over the tags.</returns>
	IEnumerator<string> IEnumerable<string>.GetEnumerator()
	{
		foreach (LogWriterTag tag in mTags)
		{
			yield return tag.Name;
		}
	}

	/// <summary>
	/// Gets an enumerator iterating over the tags.
	/// </summary>
	/// <returns>An enumerator iterating over the tags.</returns>
	IEnumerator IEnumerable.GetEnumerator()
	{
		return mTags.Select(tag => tag.Name).GetEnumerator();
	}

	/// <summary>
	/// Checks whether the specified tag set equals the current one.
	/// </summary>
	/// <param name="other">Tag set to compare with.</param>
	/// <returns>
	/// <c>true</c>, if the specified tag set equals the current one;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public bool Equals(LogWriterTagSet other)
	{
		return other != null && mTags.SequenceEqual(other.mTags, sOrdinalComparer);
	}

	/// <summary>
	/// Checks whether the specified tag set equals the current one.
	/// </summary>
	/// <param name="other">Tag set to compare with.</param>
	/// <returns>
	/// <c>true</c>, if the specified tag set equals the current one;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public bool Equals(ITagSet other)
	{
		if (other == null) return false;
		if (other is LogWriterTagSet set) return Equals(set);
		return mTags
			.Select(x => x.Name)
			.SequenceEqual(other, StringComparer.Ordinal);
	}

	/// <summary>
	/// Checks whether the specified tag set equals the current one.
	/// </summary>
	/// <param name="obj">Tag set to compare with.</param>
	/// <returns>
	/// <c>true</c>, if the specified tag set equals the current one;<br/>
	/// otherwise <c>false</c>.
	/// </returns>
	public override bool Equals(object obj)
	{
		return ReferenceEquals(this, obj) || (obj is ITagSet other && Equals(other));
	}

	/// <summary>
	/// Gets the hash code of the tag set.
	/// </summary>
	/// <returns>Hash code of the tag set.</returns>
	public override int GetHashCode()
	{
		return mHashCode;
	}

	/// <summary>
	/// Calculates the hash code of the specified tags.
	/// </summary>
	/// <param name="tags">Tags to hash.</param>
	/// <returns>The hash code of the tags.</returns>
	private static int CalculateHashCode(IEnumerable<LogWriterTag> tags)
	{
		unchecked
		{
			int hash = 17;
			foreach (LogWriterTag tag in tags) hash = hash * 23 + tag.GetHashCode();
			return hash;
		}
	}
}
