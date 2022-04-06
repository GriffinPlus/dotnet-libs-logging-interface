///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Text.RegularExpressions;

namespace GriffinPlus.Lib.Logging
{

	/// <summary>
	/// A log writer tag (thread-safe).
	///
	/// Valid tags may consist of the following characters only:
	/// - alphanumeric characters: [a-z], [A-Z], [0-9]
	/// - extra characters: [_ . , : ; + - #]
	/// - brackets: (), [], {}, &lt;&gt;
	///
	/// Asterisk(*) and quotation mark (?) are not supported as these characters are used to implement pattern matching with wildcards.
	/// Caret(^) and dollar sign ($) are not supported as these characters are used to implement the detection of regex strings.
	/// </summary>
	public sealed class LogWriterTag
	{
		internal static readonly Regex ValidNameRegex = new Regex(
			@"^[a-zA-Z0-9_\.\,\:\;\+\-\#\(\)\[\]\{\}\<\>]+$",
			RegexOptions.Compiled | RegexOptions.Singleline);

		private static   int sNextId;
		private readonly int mHashCode;

		/// <summary>
		/// Initializes a new instance of the <see cref="LogWriterTag"/> class.
		/// </summary>
		/// <param name="name">Name of the log writer tag.</param>
		internal LogWriterTag(string name)
		{
			// global logging lock is acquired here...
			CheckTag(name);
			Id = sNextId++;
			Name = name;
			mHashCode = CalculateHashCode();
		}

		/// <summary>
		/// Gets the id of the log writer tag.
		/// </summary>
		public int Id { get; }

		/// <summary>
		/// Gets the name of the log writer tag.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Checks whether the specified string is a valid tag.
		/// </summary>
		/// <param name="tag">Tag to check.</param>
		/// <exception cref="ArgumentNullException">The specified tag is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">The specified tag is invalid.</exception>
		public static void CheckTag(string tag)
		{
			if (tag == null) throw new ArgumentNullException(nameof(tag));
			if (!ValidNameRegex.IsMatch(tag))
			{
				string message =
					$"The specified tag ({tag}) is not a valid log writer tag.\n" +
					"Valid tags may consist of the following characters only:\n" +
					"- alphanumeric characters: [a-z], [A-Z], [0-9]\n" +
					"- extra characters: [_ . , : ; + - #]\n" +
					"- brackets: (), [], {}, <>";
				throw new ArgumentException(message);
			}
		}

		/// <summary>
		/// Checks whether the specified tag equals the current one.
		/// </summary>
		/// <param name="other">Tag to compare with.</param>
		/// <returns>
		/// <c>true</c> if the specified tag equals the current one;
		/// otherwise <c>false</c>.
		/// </returns>
		private bool Equals(LogWriterTag other)
		{
			return Id == other.Id && Name == other.Name;
		}

		/// <summary>
		/// Checks whether the specified object equals the current one.
		/// </summary>
		/// <param name="obj">Object to compare with.</param>
		/// <returns>
		/// <c>true</c> if the specified object equals the current one;
		/// otherwise <c>false</c>.
		/// </returns>
		public override bool Equals(object obj)
		{
			return ReferenceEquals(this, obj) || obj is LogWriterTag other && Equals(other);
		}

		/// <summary>
		/// Gets the hash code of the tag.
		/// </summary>
		/// <returns>Hash code of the tag.</returns>
		public override int GetHashCode()
		{
			return mHashCode;
		}

		/// <summary>
		/// Gets the string representation of the tag.
		/// </summary>
		/// <returns>String representation of the tag.</returns>
		public override string ToString()
		{
			return $"{Name} ({Id})";
		}

		/// <summary>
		/// Calculates the hash code of the tag.
		/// </summary>
		/// <returns>The hash code of the tag.</returns>
		private int CalculateHashCode()
		{
			unchecked
			{
				return (Id * 397) ^ (Name?.GetHashCode() ?? 0);
			}
		}
	}

}
