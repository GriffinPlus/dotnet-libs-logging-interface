///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the GriffinPlus common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Globalization;
using System.Text;

// ReSharper disable ExtractCommonBranchingCode
// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable ReplaceSubstringWithRangeIndexer
// ReSharper disable UseVerbatimString

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Provides helper methods that apply line length and truncation rules defined by a <see cref="TextFormatContext"/>.
/// </summary>
/// <remarks>
///     <para>
///     The <see cref="TextPostProcessor"/> centralizes all truncation logic used by the PrettyFormatter subsystem
///     (e.g. <see cref="PrettyObjectEngine"/>, <see cref="PrettyExceptionEngine"/>, <see cref="PrettyAssemblyEngine"/>).
///     It replaces older ad-hoc implementations such as <c>ApplyLineTruncation()</c> in individual engines,
///     ensuring consistent behavior across all formatter components.
///     </para>
///     <para>
///     The helper operates allocation-neutrally in most cases and only creates temporary <see cref="StringBuilder"/>
///     instances when truncation is actually required.
///     </para>
/// </remarks>
static class TextPostProcessor
{
	/// <summary>
	/// Applies truncation to the entire text as a single block.
	/// </summary>
	/// <param name="s">
	/// The text to process. May be <see langword="null"/> or empty.
	/// </param>
	/// <param name="tf">
	/// The formatting context containing the truncation settings.
	/// </param>
	/// <returns>
	/// The input string truncated to <see cref="TextFormatContext.MaxLineLength"/> characters if truncation is enabled;
	/// otherwise the original string.
	/// </returns>
	/// <remarks>
	///     <para>
	///     This method is typically used by engines that produce large multi-line blocks, such as
	///     <see cref="PrettyExceptionEngine"/> or <see cref="PrettyAssemblyEngine"/>,
	///     to ensure that their output remains bounded in size.
	///     </para>
	///     <example>
	///         <code language="csharp">
	/// var tf = new TextFormatContext
	/// {
	///     MaxLineLength = 20,
	///     Truncate = true,
	///     TruncationMarker = "…"
	/// };
	/// string s = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	/// string r = TextPostProcessor.ApplyWhole(s, tf);
	/// // Result: "ABCDEFGHIJKLMNOPQRST…"
	/// </code>
	///     </example>
	/// </remarks>
	public static string ApplyWhole(string s, in TextFormatContext tf)
	{
		if (string.IsNullOrEmpty(s) || tf.MaxLineLength <= 0 || !tf.Truncate)
			return s;

		int cap = tf.MaxLineLength;
		if (s.Length <= cap) return s;
		// If there are <= cap graphemes, do not truncate (even if UTF-16 length > cap)
		if (SafePrefixCharCountByTextElements(s, 0, cap, s.Length) >= s.Length)
			return s;

		int markerElems = new StringInfo(tf.TruncationMarker).LengthInTextElements;
		int limitElements = cap - markerElems;
		if (limitElements <= 0) return tf.TruncationMarker;

		// Compute a grapheme-safe prefix
		int safeChars = SafePrefixCharCountByTextElements(s, 0, limitElements, s.Length);
		return (safeChars <= 0 ? string.Empty : s.Substring(0, safeChars)) + tf.TruncationMarker;
	}

	/// <summary>
	/// Applies per-line truncation using grapheme-safe slicing.
	/// Lines longer than <see cref="TextFormatContext.MaxLineLength"/> are shortened by keeping as many complete
	/// Unicode text elements (grapheme clusters) as fit and then appending the truncation marker. The original
	/// newline sequences (LF, CR, or CRLF) are preserved exactly.
	/// </summary>
	/// <param name="s">
	/// The text to process. Line breaks may be any combination of <c>"\r"</c>, <c>"\n"</c>, or <c>"\r\n"</c>.
	/// </param>
	/// <param name="tf">
	/// The formatting context containing the truncation settings.
	/// </param>
	/// <returns>
	/// A string where each line has been truncated independently to
	/// <see cref="TextFormatContext.MaxLineLength"/> characters if necessary.
	/// </returns>
	/// <remarks>
	///     <para>
	///     This method is typically used by engines that produce multi-line output with one logical entry per line,
	///     such as <see cref="PrettyObjectEngine"/>. Truncation is applied per line to maintain readability
	///     while still bounding line length.
	///     </para>
	///     <example>
	///         <code language="csharp">
	/// var tf = new TextFormatContext
	/// {
	///     MaxLineLength = 15,
	///     Truncate = true,
	///     TruncationMarker = "..."
	/// };
	/// 
	/// string text = "0123456789012345\\nabcdefghi";
	/// string result = TextPostProcessor.ApplyPerLine(text, tf);
	/// 
	/// // Result:
	/// // "0123456789012...\\nabcdefghi"
	/// </code>
	///     </example>
	/// </remarks>
	public static string ApplyPerLine(string s, in TextFormatContext tf)
	{
		// Fast bail-outs: nothing to do if input is empty, truncation is disabled,
		// or there is no maximum line length configured.
		if (string.IsNullOrEmpty(s) || tf.MaxLineLength <= 0 || !tf.Truncate)
			return s;

		int markerElems = new StringInfo(tf.TruncationMarker).LengthInTextElements;
		int cap = tf.MaxLineLength;

		// We build the output lazily: only allocate StringBuilder if we actually need to change something.
		StringBuilder? builder = null;

		int i = 0;
		while (i < s.Length)
		{
			int lineStart = i;

			// Scan to end of the current line (exclusive), do not consume newline characters here.
			while (i < s.Length && s[i] != '\n' && s[i] != '\r') i++;
			int lineEndExclusive = i;
			int lineLen = lineEndExclusive - lineStart;

			// Determine the exact newline sequence length after the line (0, 1, or 2 code units).
			int newlineLen = 0;
			if (i < s.Length)
			{
				if (s[i] == '\r' && i + 1 < s.Length && s[i + 1] == '\n')
					newlineLen = 2; // CRLF
				else
					newlineLen = 1; // LF or CR
			}

			bool needsTrim = false;
			if (lineLen > cap)
			{
				// Only trim if the line has more than 'cap' grapheme clusters
				needsTrim = SafePrefixCharCountByTextElements(s, lineStart, cap, lineEndExclusive) < lineEndExclusive - lineStart;
			}

			// First time we need to modify the output, allocate the builder and copy the prefix unchanged.
			if (needsTrim && builder is null)
			{
				builder = new StringBuilder(s.Length + 16);
				if (lineStart > 0)
					builder.Append(s, 0, lineStart); // copy untouched prefix once
			}

			if (builder is not null)
			{
				if (needsTrim)
				{
					// How many complete grapheme clusters can we keep before appending the marker?
					int limitElements = cap - markerElems;
					if (limitElements <= 0)
					{
						// No room for any text element; output only the truncation marker.
						builder.Append(tf.TruncationMarker);
					}
					else
					{
						// Append as many full grapheme clusters as will fit, then the marker.
						int safeChars = SafePrefixCharCountByTextElements(s, lineStart, limitElements, lineEndExclusive);
						if (safeChars > 0) builder.Append(s, lineStart, safeChars);
						builder.Append(tf.TruncationMarker);
					}
				}
				else
				{
					// Line already fits into the cap → copy unchanged.
					if (lineLen > 0) builder.Append(s, lineStart, lineLen);
				}

				// Preserve the original newline sequence exactly.
				if (newlineLen == 2) builder.Append('\r').Append('\n');
				else if (newlineLen == 1) builder.Append(s[i]);
			}

			// Advance to the start of the next line.
			i = lineEndExclusive + newlineLen;
		}

		// If we never needed to truncate, return the original string to avoid allocations.
		return builder is null ? s : builder.ToString();
	}

	/// <summary>
	/// Truncates an entire string to <see cref="TextFormatContext.MaxLineLength"/> using grapheme-safe slicing,
	/// then appends the truncation marker. If truncation is disabled or no maximum is set, the input is returned unchanged.
	/// </summary>
	/// <param name="s">The input string.</param>
	/// <param name="tf">
	/// The text format context providing the maximum length, truncation toggle, and truncation marker.
	/// The method only inspects <see cref="TextFormatContext.MaxLineLength"/>, <see cref="TextFormatContext.Truncate"/>,
	/// and <see cref="TextFormatContext.TruncationMarker"/>.
	/// </param>
	/// <returns>
	/// The original string if no truncation is required; otherwise a grapheme-safe prefix plus the truncation marker.
	/// </returns>
	/// <remarks>
	/// Uses <see cref="StringInfo"/> to ensure Unicode text elements (grapheme clusters, including ZWJ sequences and
	/// combining marks) are not split. If the truncation marker itself does not fit, only the marker is returned.
	/// </remarks>
	public static string TruncateString(string s, in TextFormatContext tf)
	{
		// Fast bail-outs: nothing to do if empty, truncation disabled, or no maximum configured.
		if (string.IsNullOrEmpty(s) || tf.MaxLineLength <= 0 || !tf.Truncate)
			return s;

		int cap = tf.MaxLineLength;
		if (s.Length <= cap) return s;
		// Avoid over-trim when grapheme count <= cap but UTF-16 length > cap
		if (SafePrefixCharCountByTextElements(s, 0, cap, s.Length) >= s.Length)
			return s;

		// We must reserve space for the truncation marker.
		int markerElems = new StringInfo(tf.TruncationMarker).LengthInTextElements;
		int limitElements = cap - markerElems;
		if (limitElements <= 0)
			return tf.TruncationMarker; // no room for any grapheme → output marker only

		// Compute how many UTF-16 code units we can keep without breaking a grapheme cluster.
		int safeChars = SafePrefixCharCountByTextElements(s, startIndex: 0, maxTextElements: limitElements, endExclusive: s.Length);

		// If nothing fits, we still return just the marker; otherwise take the safe prefix and append the marker.
		return (safeChars <= 0 ? string.Empty : s.Substring(0, safeChars)) + tf.TruncationMarker;
	}

	/// <summary>
	/// Escapes a string segment directly into the builder using C#-like escapes.<br/>
	/// Also neutralizes BiDi control characters by emitting \uXXXX.
	/// </summary>
	/// <param name="builder">The target <see cref="StringBuilder"/>.</param>
	/// <param name="input">The source string.</param>
	/// <param name="startIndex">The index in the source string to start escaping from.</param>
	/// <param name="length">The number of characters to escape.</param>
	public static void AppendEscaped(
		StringBuilder builder,
		string        input,
		int           startIndex,
		int           length)
	{
		int endIndex = startIndex + length;
		if (endIndex > input.Length) endIndex = input.Length;

		for (int i = startIndex; i < endIndex; i++)
		{
			char c = input[i];

			// Surrogates: keep valid pairs as-is, escape unmatched code units.
			if (char.IsHighSurrogate(c))
			{
				// Check for a valid pair *within the specified segment*.
				if (i + 1 < endIndex && char.IsLowSurrogate(input[i + 1]))
				{
					builder.Append(c).Append(input[i + 1]);
					i++;
					continue;
				}
				builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
				continue;
			}
			if (char.IsLowSurrogate(c))
			{
				// Unmatched low surrogate
				builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
				continue;
			}

			// Neutralize BiDi controls (Unicode Cf) explicitly.
			// This ensures data integrity for quoted strings, allowing the user
			// to *see* the control character instead of it being silently removed.
			// Unquoted text should be sanitized via StripBiDiControls() instead.
			if (IsBidiControl(c))
			{
				builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
				continue;
			}

			switch (c)
			{
				case '\\': builder.Append("\\\\"); break;
				case '\"': builder.Append("\\\""); break;
				case '\n': builder.Append("\\n"); break;
				case '\r': builder.Append("\\r"); break;
				case '\t': builder.Append("\\t"); break;

				default:
					if (char.IsControl(c))
						builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
					else
						builder.Append(c);
					break;
			}
		}
	}

	/// <summary>
	/// Appends an escaped version of a character directly to a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="builder">The target <see cref="StringBuilder"/>.</param>
	/// <param name="c">The character to escape and append.</param>
	/// <remarks>
	/// This method handles common escape sequences such as backslashes, single quotes, and control
	/// characters. For control characters not explicitly handled, the method returns a Unicode escape sequence in the
	/// format <c>"\\uXXXX"</c>, where <c>XXXX</c> is the hexadecimal code of the character.
	/// </remarks>
	public static void AppendEscapedChar(StringBuilder builder, char c)
	{
		// Escape unmatched surrogates defensively.
		if (char.IsSurrogate(c))
		{
			builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
			return;
		}

		// Neutralize BiDi controls explicitly.
		if (IsBidiControl(c))
		{
			builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
			return;
		}

		switch (c)
		{
			case '\\': builder.Append("\\\\"); break;
			case '\'': builder.Append("\\\'"); break; // char literals are wrapped in single quotes
			case '\n': builder.Append("\\n"); break;
			case '\r': builder.Append("\\r"); break;
			case '\t': builder.Append("\\t"); break;

			default:
				if (char.IsControl(c))
					builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
				else
					builder.Append(c);
				break;
		}
	}

	/// <summary>
	/// Escapes special characters in a string using common C#-style escapes (\", \\, \n, \r, \t)
	/// and \uXXXX for control characters and BiDi control characters.
	/// This keeps log output single-line and copy/paste friendly.
	/// </summary>
	/// <param name="input">The string to escape. Cannot be <see langword="null"/>.</param>
	/// <returns>
	/// A new string with special characters replaced by their escaped representations. For example, backslashes are
	/// replaced with <c>\\</c>, double quotes with <c>\"</c>, and control characters with their Unicode escape sequences
	/// (e.g., <c>\u000A</c> for a newline).
	/// </returns>
	public static string EscapeString(string input)
	{
		// --- Fast path ---

		// Check for any character that *requires* escaping.
		// If none are found, return the original string to avoid allocation.
		bool needsEscaping = false;
		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];
			// Check for standard escapes, control chars, or any surrogates (which
			// require logic to distinguish matched pairs from unmatched singles).
			if (c is '\\' or '\"' or '\n' or '\r' or '\t' ||
			    char.IsControl(c) ||
			    char.IsSurrogate(c) ||
			    IsBidiControl(c))
			{
				needsEscaping = true;
				break;
			}
		}

		if (!needsEscaping) return input;

		// --- End fast path ---

		var builder = new StringBuilder(input.Length + 8);

		for (int i = 0; i < input.Length; i++)
		{
			char c = input[i];

			// Always neutralize BiDi control characters (prevents visual spoofing / layout effects in logs)
			if (IsBidiControl(c))
			{
				builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
				continue;
			}

			// Surrogates: keep valid pairs as-is, escape unmatched code units.
			if (char.IsHighSurrogate(c))
			{
				if (i + 1 < input.Length && char.IsLowSurrogate(input[i + 1]))
				{
					builder.Append(c).Append(input[i + 1]);
					i++;
					continue;
				}
				builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
				continue;
			}
			if (char.IsLowSurrogate(c))
			{
				builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
				continue;
			}

			switch (c)
			{
				case '\\': builder.Append("\\\\"); break;
				case '\"': builder.Append("\\\""); break;
				case '\n': builder.Append("\\n"); break;
				case '\r': builder.Append("\\r"); break;
				case '\t': builder.Append("\\t"); break;

				default:
					if (char.IsControl(c))
						builder.Append("\\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
					else
						builder.Append(c);
					break;
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Removes BiDi control characters (Cf) from a string. No other changes.
	/// </summary>
	/// <remarks>
	/// This is primarily a security and layout-stability measure for *unquoted* text (e.g., Exception.Message,
	/// object.ToString()) to prevent log viewer layout corruption. For *quoted* strings (e.g., string properties,
	/// dictionary keys), <see cref="AppendEscaped"/> should be used directly on the original string, which will
	/// visualize these characters as \uXXXX instead of removing them.
	/// </remarks>
	internal static string StripBiDiControls(string s)
	{
		if (string.IsNullOrEmpty(s)) return s;
		StringBuilder? builder = null;
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if (!IsBidiControl(c))
			{
				builder?.Append(c);
				continue;
			}
			builder ??= new StringBuilder(s.Length);
			// backfill untouched prefix on first hit
			if (builder.Length == 0 && i > 0) builder.Append(s, 0, i);
			// skip control
		}
		return builder is null ? s : builder.ToString();
	}

	/// <summary>
	/// Computes the number of UTF-16 code units from <paramref name="startIndex"/> that
	/// comprise at most <paramref name="maxTextElements"/> complete Unicode text elements
	/// (grapheme clusters), without crossing <paramref name="endExclusive"/>.
	/// </summary>
	/// <param name="s">The input string.</param>
	/// <param name="startIndex">Start index within <paramref name="s"/> (UTF-16 code units).</param>
	/// <param name="maxTextElements">Maximum number of text elements (grapheme clusters) to include.</param>
	/// <param name="endExclusive">Upper bound (exclusive) that must not be crossed.</param>
	/// <returns>
	/// The number of UTF-16 code units to take from <paramref name="startIndex"/> such that
	/// only full text elements are included and the slice does not cross <paramref name="endExclusive"/>.
	/// </returns>
	internal static int SafePrefixCharCountByTextElements(
		string s,
		int    startIndex,
		int    maxTextElements,
		int    endExclusive)
	{
		if (maxTextElements <= 0 || startIndex >= s.Length || startIndex >= endExclusive) return 0;

		// TextElementEnumerator enumerates Unicode text elements (grapheme clusters).
		// Works on all target TFMs (net461+), correctness > raw Rune counting for ZWJ/combining sequences.
		TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(s, startIndex);

		int takenElements = 0;
		int lastOkEnd = startIndex;

		while (enumerator.MoveNext())
		{
			int elementIndex = enumerator.ElementIndex;
			if (elementIndex >= endExclusive) break;

			string element = enumerator.GetTextElement();
			int elemEnd = elementIndex + element.Length;

			// Do not cross endExclusive; stop if the element would exceed the bound.
			if (elemEnd > endExclusive) break;

			lastOkEnd = elemEnd;
			takenElements++;

			if (takenElements >= maxTextElements) break;
		}

		return lastOkEnd - startIndex;
	}

	/// <summary>
	/// Returns whether the specified character is a Unicode bidirectional control character
	/// that should be escaped to avoid display side effects when logging.
	/// </summary>
	/// <param name="c">The character to check.</param>
	/// <returns>
	/// <see langword="true"/> if the character is a bidirectional control character;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsBidiControl(char c)
	{
		return c is '\u061C'    // ARABIC LETTER MARK (ALM)
			       or '\u200E'  // LEFT-TO-RIGHT MARK (LRM)
			       or '\u200F'  // RIGHT-TO-LEFT MARK (RLM)
			       or '\u202A'  // LEFT-TO-RIGHT EMBEDDING (LRE)
			       or '\u202B'  // RIGHT-TO-LEFT EMBEDDING (RLE)
			       or '\u202C'  // POP DIRECTIONAL FORMATTING (PDF)
			       or '\u202D'  // LEFT-TO-RIGHT OVERRIDE (LRO)
			       or '\u202E'  // RIGHT-TO-LEFT OVERRIDE (RLO)
			       or '\u2066'  // LEFT-TO-RIGHT ISOLATE (LRI)
			       or '\u2067'  // RIGHT-TO-LEFT ISOLATE (RLI)
			       or '\u2068'  // FIRST STRONG ISOLATE (FSI)
			       or '\u2069'; // POP DIRECTIONAL ISOLATE (PDI)
	}
}
