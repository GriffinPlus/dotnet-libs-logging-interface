///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the GriffinPlus common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System.Text;

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
	///     <para>
	///     A defensive minimum of 40 characters is enforced to prevent pathological configurations
	///     that would otherwise remove all meaningful content.
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

		const int minCap = 40;
		int cap = tf.MaxLineLength < minCap ? minCap : tf.MaxLineLength;
		if (s.Length <= cap) return s;

		int keep = cap - tf.TruncationMarker.Length;
		return keep <= 0 ? tf.TruncationMarker : s.Substring(0, keep) + tf.TruncationMarker;
	}

	/// <summary>
	/// Applies truncation individually to each line of text.
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
	///     <para>
	///     A defensive minimum of 40 characters is enforced to avoid pathological configurations.
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
		if (string.IsNullOrEmpty(s) || tf.MaxLineLength <= 0 || !tf.Truncate)
			return s;

		const int minCap = 40;
		int cap = tf.MaxLineLength < minCap ? minCap : tf.MaxLineLength;

		// Optimization: Check if any truncation is needed at all.
		// If no line exceeds the cap, return the original string.
		if (!NeedsPerLineTruncation(s, cap))
			return s;

		int keep = cap - tf.TruncationMarker.Length;
		if (keep < 0) keep = 0; // Ensure 'keep' is not negative

		var builder = new StringBuilder(s.Length + 16);
		int lineStart = 0;

		for (int i = 0; i < s.Length; i++)
		{
			bool isAtEnd = i == s.Length - 1;
			char c = s[i];

			// Check for newline characters or end of string
			if (c == '\n' || c == '\r' || isAtEnd)
			{
				int lineEnd = i;

				// Adjust lineEnd for end-of-string case
				if (isAtEnd && c != '\n' && c != '\r')
				{
					lineEnd = i + 1; // Include the last character
				}

				// Handle CRLF sequence
				if (c == '\r' && i + 1 < s.Length && s[i + 1] == '\n')
				{
					i++; // Consume the '\n' as part of the newline sequence
				}

				int lineLength = lineEnd - lineStart;

				// Apply truncation if this line is too long
				if (lineLength > cap)
				{
					if (keep > 0) builder.Append(s, lineStart, keep);
					builder.Append(tf.TruncationMarker);
				}
				else
				{
					// Append the line content as-is
					builder.Append(s, lineStart, lineLength);
				}

				// Append the newline sequence that we found
				if (c is '\r' or '\n')
				{
					builder.Append(s, lineEnd, i - lineEnd + 1);
				}

				// Set start for the next line
				lineStart = i + 1;
			}
		}

		return builder.ToString();
	}

	/// <summary>
	/// Quickly checks if any line in the string exceeds the specified capability,
	/// without allocating a <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="s">The string to check.</param>
	/// <param name="cap">The maximum allowed line length (capability).</param>
	/// <returns>
	/// <see langword="true"/> if at least one line exceeds the cap;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool NeedsPerLineTruncation(string s, int cap)
	{
		int lineLength = 0;
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];

			if (c is '\n' or '\r')
			{
				if (lineLength > cap) return true;
				lineLength = 0; // Reset for next line

				// Handle CRLF
				if (c == '\r' && i + 1 < s.Length && s[i + 1] == '\n')
				{
					i++; // Skip the '\n'
				}
			}
			else
			{
				lineLength++;
			}
		}

		// Check the last line (or the only line)
		return lineLength > cap;
	}
}
