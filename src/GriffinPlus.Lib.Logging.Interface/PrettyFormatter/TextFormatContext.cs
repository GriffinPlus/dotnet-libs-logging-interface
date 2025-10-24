///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Globalization;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Represents a resolved, concrete set of text formatting parameters that are shared across
/// all Pretty formatter engines (newline sequence, indentation, culture, truncation, etc.).
/// </summary>
/// <remarks>
///     <para>
///     This struct is an internal bridge between the high-level <see cref="PrettyOptions"/> façade and the low-level
///     rendering engines. It extracts the effective values from <see cref="PrettyOptions"/>, substituting defaults
///     where <see langword="null"/> values are present.
///     </para>
///     <para>
///     Engines depend only on this struct to remain decoupled from <see cref="PrettyOptions"/> itself.
///     </para>
/// </remarks>
readonly struct TextFormatContext
{
	/// <summary>
	/// The newline sequence to use when writing multi-line output (e.g. <c>"\n"</c> or <c>"\r\n"</c>).
	/// </summary>
	public readonly string NewLine;

	/// <summary>
	/// The string used for one indentation level in multi-line output (e.g. two spaces).
	/// </summary>
	public readonly string Indent;

	/// <summary>
	/// The culture used for formatting numbers, dates, and other culture-sensitive values.
	/// </summary>
	public readonly IFormatProvider Culture;

	/// <summary>
	/// A value indicating whether long strings should be truncated (instead of wrapped) when a line length limit applies.
	/// </summary>
	public readonly bool Truncate;

	/// <summary>
	/// The marker appended to truncated text (e.g. an ellipsis).
	/// </summary>
	public readonly string TruncationMarker;

	/// <summary>
	/// The maximum allowed line length before wrapping or truncation occurs.<br/>
	/// A value of <c>0</c> disables wrapping.
	/// </summary>
	public readonly int MaxLineLength;

	/// <summary>
	/// Initializes a new <see cref="TextFormatContext"/> with the specified parameters.
	/// </summary>
	/// <param name="newLine">The newline sequence to use. Falls back to <see cref="Environment.NewLine"/> if <see langword="null"/>.</param>
	/// <param name="indent">The indentation token to use. Falls back to two spaces if <see langword="null"/>.</param>
	/// <param name="culture">The culture to use for formatting. Falls back to <see cref="CultureInfo.InvariantCulture"/> if <see langword="null"/>.</param>
	/// <param name="truncate">Whether long strings should be truncated instead of wrapped.</param>
	/// <param name="truncationMarker">The marker appended to truncated text. Falls back to an ellipsis if <see langword="null"/>.</param>
	/// <param name="maxLineLength">The maximum line length; <c>0</c> disables wrapping.</param>
	public TextFormatContext(
		string?          newLine,
		string?          indent,
		IFormatProvider? culture,
		bool             truncate,
		string?          truncationMarker,
		int              maxLineLength)
	{
		NewLine = newLine ?? Environment.NewLine;
		Indent = indent ?? "  ";
		Culture = culture ?? CultureInfo.InvariantCulture;
		Truncate = truncate;
		TruncationMarker = truncationMarker ?? "…";
		MaxLineLength = maxLineLength;
	}

	/// <summary>
	/// Creates a <see cref="TextFormatContext"/> from the specified <see cref="PrettyOptions"/> instance,
	/// resolving any <see langword="null"/> properties to their defaults.
	/// </summary>
	/// <param name="root">The root <see cref="PrettyOptions"/> instance, or <see langword="null"/> to use all defaults.</param>
	/// <returns>
	/// A fully resolved <see cref="TextFormatContext"/>.
	/// </returns>
	public static TextFormatContext From(PrettyOptions? root)
	{
		string nl = root?.NewLine ?? Environment.NewLine;
		string indent = root?.Indent ?? "  ";
		IFormatProvider cult = (IFormatProvider?)root?.Culture ?? CultureInfo.InvariantCulture;
		bool truncate = root?.TruncateLongStrings ?? true;
		string mark = root?.TruncationMarker ?? "…";
		int maxLen = root?.MaxLineLength ?? 0;
		return new TextFormatContext(nl, indent, cult, truncate, mark, maxLen);
	}
}
