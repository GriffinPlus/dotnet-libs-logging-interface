///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// ReSharper disable CanSimplifyStringEscapeSequence

using System.Runtime.CompilerServices;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Some stuff that can become in handy when working with Unicode strings.
/// </summary>
static class Unicode
{
	/// <summary>
	/// A string containing characters that are usually used to represent line breaks.
	/// The string contains the following characters:
	/// line feed (U+000A), form feed (U+000C), carriage return (U+000D), next line (U+0085), line separator (U+2028), paragraph separator (U+2029).
	/// </summary>
	public static readonly string NewLineCharacters =
		"\u000A" + // line feed
		"\u000B" + // vertical tab
		"\u000C" + // form feed
		"\u000D" + // carriage return
		"\u0085" + // next line
		"\u2028" + // line separator
		"\u2029";  // paragraph separator

	/// <summary>
	/// Determines whether the specified character represents a line break.
	/// </summary>
	/// <param name="ch">The character to evaluate.</param>
	/// <returns>
	/// <see langword="true"/> if the character is a line break character;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsLineBreak(char ch)
	{
		return ch is
			       '\u000A' or // line feed ('\n')
			       '\u000B' or // vertical tab
				   '\u000C' or // form feed
			       '\u000D' or // carriage return ('\r')
			       '\u0085' or // next line
			       '\u2028' or // line separator
			       '\u2029';   // paragraph separator
	}
}
