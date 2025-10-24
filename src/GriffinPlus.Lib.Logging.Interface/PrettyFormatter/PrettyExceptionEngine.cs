///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

// ReSharper disable LoopCanBeConvertedToQuery

namespace GriffinPlus.Lib.Logging;


/// <summary>
/// Internal engine that formats <see cref="Exception"/> instances into compact, readable text suitable for logs.
/// </summary>
/// <remarks>
///     <para>
///     The engine is stateless and thread-safe. It renders a root exception with optional metadata, followed by an
///     optional stack trace and a recursive rendering of inner exceptions up to a configurable depth.
///     </para>
///     <para>
///     The engine intentionally avoids throwing while formatting: all reflection reads are performed defensively,
///     and failures to access metadata are tolerated and omitted from the output.
///     </para>
/// </remarks>
static class PrettyExceptionEngine
{
	/// <summary>
	/// Formats an <see cref="Exception"/> according to the specified <paramref name="options"/>.
	/// </summary>
	/// <param name="exception">
	/// The exception to format.<br/>
	/// If <see langword="null"/>, the literal string <c>"&lt;null&gt;"</c> is returned.
	/// </param>
	/// <param name="options">
	/// Formatting options controlling type name, metadata, stack trace and inner traversal.<br/>
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <param name="tfc">
	/// Optional text-formatting context providing deterministic newline/indent/culture handling.<br/>
	/// If <see langword="null"/>, platform defaults are used (equivalent to <see cref="Environment.NewLine"/> and two-space indent).
	/// </param>
	/// <returns>
	/// A string suitable for logging and diagnostics.
	/// </returns>
	/// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is <see langword="null"/>.</exception>
	public static string Format(Exception? exception, PrettyExceptionOptions options, TextFormatContext? tfc = null)
	{
		if (exception == null) return "<null>";
		if (options == null) throw new ArgumentNullException(nameof(options));

		// optional Aggregate flattening (safe: no-op for non-AggregateException)
		if (options.FlattenAggregates)
		{
			if (exception is AggregateException aggregateException)
				exception = aggregateException.Flatten();
		}

		TextFormatContext tf = tfc ?? TextFormatContext.From(null);
		var builder = new StringBuilder(512);
		var typeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };

		AppendException(builder, exception, options, depth: 0, typeOptions, tf);
		return TextPostProcessor.ApplyWhole(builder.ToString(), tf);
	}

	/// <summary>
	/// Appends one exception block (header + optional metadata + optional stack + inner exceptions) with proper indentation.
	/// </summary>
	/// <param name="builder">The target <see cref="StringBuilder"/> receiving the formatted output.</param>
	/// <param name="exception">The exception to render. Must not be <see langword="null"/>.</param>
	/// <param name="options">Formatting options that guide rendering behavior.</param>
	/// <param name="depth">The current recursion depth (root = 0). Used for indentation and depth limiting.</param>
	/// <param name="typeOptions">Type-name rendering options forwarded to <see cref="PrettyTypeEngine"/>.</param>
	/// <param name="tfc">The text-formatting context (newline/indent/culture).</param>
	/// <remarks>
	/// This method never throws; any access errors (e.g., restricted properties) are ignored and omitted.
	/// </remarks>
	private static void AppendException(
		StringBuilder          builder,
		Exception              exception,
		PrettyExceptionOptions options,
		int                    depth,
		PrettyTypeOptions      typeOptions,
		TextFormatContext      tfc)
	{
		// Header line: [Type]: Message
		var header = new StringBuilder();
		if (options.IncludeType)
		{
			header.Append(PrettyTypeEngine.Format(exception.GetType(), typeOptions));
			if (!string.IsNullOrEmpty(exception.Message)) header.Append(": ");
		}
		header.Append(exception.Message);

		AppendRepeat(builder, tfc.Indent, depth);
		builder.Append(header).Append(tfc.NewLine);

		// Meta (HResult / Source / HelpLink / TargetSite)
		AppendMeta(builder, exception, options, depth, typeOptions, tfc);

		// Data
		if (options.IncludeData) AppendData(builder, exception, options, depth, typeOptions, tfc);

		// Stack trace
		if (options.IncludeStackTrace) AppendStackTrace(builder, exception, options, depth, tfc);

		// Inner exceptions
		if (depth < options.MaxInnerExceptionDepth)
		{
			if (exception is AggregateException { InnerExceptions.Count: > 0 } aggregateException)
			{
				int i = 0;
				foreach (Exception inner in aggregateException.InnerExceptions)
				{
					AppendRepeat(builder, tfc.Indent, depth);
					builder.Append("Inner[").Append(i++).Append("]:").Append(tfc.NewLine);
					AppendException(builder, inner, options, depth + 1, typeOptions, tfc);
				}
			}
			else if (exception.InnerException != null)
			{
				AppendRepeat(builder, tfc.Indent, depth);
				builder.Append("Inner:").Append(tfc.NewLine);
				AppendException(builder, exception.InnerException, options, depth + 1, typeOptions, tfc);
			}
		}
	}

	/// <summary>
	/// Appends metadata lines (<see cref="Exception.HResult"/>, <see cref="Exception.Source"/>, <see cref="Exception.HelpLink"/>,
	/// <see cref="Exception.TargetSite"/>) when enabled and available.
	/// </summary>
	/// <param name="builder">The output target builder.</param>
	/// <param name="exception">The exception currently being rendered.</param>
	/// <param name="options">Exception formatting options controlling which metadata fields are included.</param>
	/// <param name="depth">The current recursion depth (used for indentation).</param>
	/// <param name="typeOptions">Type-name rendering options (used for <c>TargetSite</c> formatting).</param>
	/// <param name="tfc">The text-formatting context (newline/indent/culture).</param>
	/// <remarks>
	/// Each section is added only if explicitly enabled and the underlying value is available.
	/// </remarks>
	private static void AppendMeta(
		StringBuilder          builder,
		Exception              exception,
		PrettyExceptionOptions options,
		int                    depth,
		PrettyTypeOptions      typeOptions,
		TextFormatContext      tfc)
	{
		if (options.IncludeHResult)
		{
			// HResult is int; print both decimal and hex for convenience
			int hr = SafeGetHResult(exception);
			AppendRepeat(builder, tfc.Indent, depth);
			builder
				.Append("  HResult: ")
				.Append(hr.ToString(CultureInfo.InvariantCulture))
				.Append(" (0x")
				.Append(hr.ToString("X8", CultureInfo.InvariantCulture))
				.Append(")")
				.Append(tfc.NewLine);
		}

		if (options.IncludeSource && !string.IsNullOrEmpty(exception.Source))
		{
			AppendRepeat(builder, tfc.Indent, depth);
			builder.Append("  Source: ").Append(exception.Source).Append(tfc.NewLine);
		}

		if (options.IncludeHelpLink && !string.IsNullOrEmpty(exception.HelpLink))
		{
			AppendRepeat(builder, tfc.Indent, depth);
			builder.Append("  HelpLink: ").Append(exception.HelpLink).Append(tfc.NewLine);
		}

		if (options.IncludeTargetSite && exception.TargetSite != null)
		{
			MethodBase? mi = exception.TargetSite;
			string memberText = PrettyMemberEngine.Format(
				mi,
				new PrettyMemberOptions
				{
					IncludeDeclaringType = true,
					ShowAccessibility = false,
					ShowMemberModifiers = false,
					ShowAsyncForAsyncMethods = false,
					ShowParameterNames = true,
					ShowNullabilityAnnotations = false,
					ShowAttributes = false,
					ShowParameterAttributes = false,
					AttributeFilter = null,
					AttributeMaxElements = 0,
					ShowGenericConstraintsOnMethods = false,
					ShowGenericConstraintsOnTypes = false,
					UseNamespaceForTypes = typeOptions.UseNamespace
				});
			AppendRepeat(builder, tfc.Indent, depth);
			builder.Append("  TargetSite: ").Append(memberText).Append(tfc.NewLine);
		}
	}

	/// <summary>
	/// Appends the <see cref="Exception.Data"/> dictionary as an indented block, honoring item limits.
	/// </summary>
	/// <param name="builder">The output target builder.</param>
	/// <param name="exception">The exception whose data is rendered.</param>
	/// <param name="options">Formatting options controlling whether to include data and how many items to print.</param>
	/// <param name="depth">The current recursion depth (used for indentation).</param>
	/// <param name="typeOptions">Type-name rendering options used for compact value formatting.</param>
	/// <param name="tfc">The text-formatting context (newline/indent/culture).</param>
	/// <remarks>
	/// Values are formatted with <see cref="FormatDataValue(object, PrettyTypeOptions, TextFormatContext)"/> to avoid deep
	/// recursion or overly verbose output.
	/// </remarks>
	private static void AppendData(
		StringBuilder          builder,
		Exception              exception,
		PrettyExceptionOptions options,
		int                    depth,
		PrettyTypeOptions      typeOptions,
		TextFormatContext      tfc)
	{
		if (exception.Data.Count == 0)
			return;

		AppendRepeat(builder, tfc.Indent, depth);
		builder.Append("  Data:").Append(tfc.NewLine);

		// Sort keys for deterministic output
		var keys = new List<object>();
		foreach (object key in exception.Data.Keys) keys.Add(key);
		try
		{
			// Sort by string representation for stability
			keys.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal));
		}
		catch
		{
			// Fallback to default order if keys are not comparable.
		}

		int shown = 0;
		foreach (object key in keys)
		{
			if (options.DataMaxItems > 0 && shown >= options.DataMaxItems)
			{
				AppendRepeat(builder, tfc.Indent, depth);
				builder.Append("    …").Append(tfc.NewLine);
				break;
			}

			object? value;
			try { value = exception.Data[key]; }
			catch { value = "!(Access Error)"; /* defensive */ }

			string k = FormatDataValue(key, typeOptions, tfc);
			string v = FormatDataValue(value, typeOptions, tfc);
			AppendRepeat(builder, tfc.Indent, depth);
			builder.Append("    ").Append(k).Append(" = ").Append(v).Append(tfc.NewLine);
			shown++;
		}
	}

	/// <summary>
	/// Formats a value from the Exception.Data dictionary using simple, non-recursive rules,
	/// applying truncation rules from the TextFormatContext.
	/// </summary>
	/// <param name="value">The data key or value to format; may be null.</param>
	/// <param name="typeOptions">Type-name rendering options used when falling back to a type name.</param>
	/// <param name="tfc">The text formatting context, providing truncation rules (MaxLineLength, TruncationMarker).</param>
	/// <returns>
	/// A short, truncated string representation. Strings are quoted. Known primitives are rendered invariantly;
	/// complex values fall back to <see cref="object.ToString"/> if concise, otherwise to the value's type name.
	/// </returns>
	private static string FormatDataValue(object? value, PrettyTypeOptions typeOptions, TextFormatContext tfc)
	{
		if (value == null) return "<null>";

		// --- Truncation Setup ---
		// 1. Define a default cap for Data values.
		const int defaultCap = 200;
		int cap = defaultCap;

		// 2. Check if the global TextFormatContext defines a *stricter* limit.
		//    We respect the global limit only if it's enabled (Truncate=true, MaxLineLength > 0)
		//    and *smaller* than our default cap.
		if (tfc is { Truncate: true, MaxLineLength: > 0 } && tfc.MaxLineLength < cap)
		{
			cap = tfc.MaxLineLength;
		}

		// 3. Calculate the actual number of characters to keep, accounting for the marker length.
		//    Ensure 'keep' is not negative if the marker is longer than the cap.
		int keep = cap - tfc.TruncationMarker.Length;
		if (keep < 0) keep = 0;

		// --- Value Formatting ---

		// Case 1: string
		// Apply truncation if the string exceeds the calculated cap.
		if (value is string s)
		{
			if (s.Length > cap)
			{
				// Use Substring(int, int) for efficient slicing.
				s = s.Substring(0, keep) + tfc.TruncationMarker;
			}
			return "\"" + s + "\""; // Always quote strings.
		}

		// Case 2: Known primitive types (bool, char)
		if (value is bool b) return b ? "true" : "false";
		if (value is char c) return "'" + c + "'";

		// Case 3: IFormattable (numbers, dates, Guids, etc.)
		// Use InvariantCulture for a stable, culture-independent representation.
		if (value is IFormattable formattable)
		{
			try { return formattable.ToString(null, CultureInfo.InvariantCulture); }
			catch
			{
				/* Defensive: Swallow exceptions from custom IFormattable implementations */
			}
		}

		// Case 4: Fallback to object.ToString()
		string? sToString;
		try { sToString = value.ToString(); }
		catch { sToString = null; } // Be safe if ToString() throws.

		// If ToString() is useless (null, empty, or just returns the default Type.ToString()),
		// we prefer printing the pretty-formatted type name instead.
		if (string.IsNullOrEmpty(sToString) || sToString == value.GetType().ToString())
		{
			return PrettyTypeEngine.Format(value.GetType(), typeOptions);
		}

		// Apply truncation to the ToString() result if it exceeds the cap.
		if (sToString.Length > cap)
		{
			return sToString.Substring(0, keep) + tfc.TruncationMarker;
		}

		// Return the safe, non-empty, and appropriately-sized ToString() result.
		return sToString;
	}

	/// <summary>
	/// Appends the stack trace lines, optionally truncated to <see cref="PrettyExceptionOptions.StackFrameLimit"/>.
	/// </summary>
	/// <param name="builder">The output target builder.</param>
	/// <param name="exception">The exception whose stack trace is rendered.</param>
	/// <param name="options">Exception formatting options defining inclusion and frame limit.</param>
	/// <param name="depth">The current recursion depth (used for indentation).</param>
	/// <param name="tfc">The text-formatting context (newline/indent/culture).</param>
	/// <remarks>
	/// If the exception has no stack trace, this method writes nothing. When truncated, an ellipsis line is appended.
	/// </remarks>
	private static void AppendStackTrace(
		StringBuilder          builder,
		Exception              exception,
		PrettyExceptionOptions options,
		int                    depth,
		TextFormatContext      tfc)
	{
		string? stackTrace = exception.StackTrace;
		if (string.IsNullOrEmpty(stackTrace)) return;

		AppendRepeat(builder, tfc.Indent, depth);
		builder.Append("  StackTrace:").Append(tfc.NewLine);

		int lineCount = 0;
		int startIndex = 0;
		int len = stackTrace.Length;
		int limit = options.StackFrameLimit > 0 ? options.StackFrameLimit : int.MaxValue;

		for (int i = 0; i < len; i++)
		{
			char c = stackTrace[i];

			// Find newline (LR or CRLF)
			if (c is '\n' or '\r')
			{
				if (lineCount >= limit) break; // limit reached

				int lineLength = i - startIndex;

				// extract line (without \r)
				if (c == '\r' && i + 1 < len && stackTrace[i + 1] == '\n')
				{
					// It is CRLF
					AppendTrimmedLine(builder, stackTrace, startIndex, lineLength, depth, tfc);
					i++; // skip \n
					startIndex = i + 1;
				}
				else
				{
					// It is only \n or only \r
					AppendTrimmedLine(builder, stackTrace, startIndex, lineLength, depth, tfc);
					startIndex = i + 1;
				}
				lineCount++;
			}
		}

		// Prepare last line, if no newline at the end.
		if (lineCount < limit && startIndex < len)
		{
			AppendTrimmedLine(builder, stackTrace, startIndex, len - startIndex, depth, tfc);
			lineCount++;
		}

		// Check whether we have truncated the stack trace.
		if (lineCount == limit && startIndex < len)
		{
			AppendRepeat(builder, tfc.Indent, depth);
			builder.Append("    …").Append(tfc.NewLine);
		}
	}

	/// <summary>
	/// Appends a trimmed line of text to the specified <see cref="StringBuilder"/> with an optional indent
	/// and formatting context.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="StringBuilder"/> to which the trimmed line will be appended. Cannot be <see langword="null"/>.
	/// </param>
	/// <param name="text">
	/// The source text containing the line to append. Cannot be <see langword="null"/>.
	/// </param>
	/// <param name="start">
	/// The zero-based starting index of the line within <paramref name="text"/>.
	/// </param>
	/// <param name="length">
	/// The length of the line to append, starting from <paramref name="start"/>. Must be non-negative.
	/// </param>
	/// <param name="depth">
	/// The current recursion depth (used for indentation).
	/// </param>
	/// <param name="tfc">
	/// The <see cref="TextFormatContext"/> that provides formatting details, such as the newline sequence.
	/// Cannot be <see langword="null"/>.
	/// </param>
	/// <remarks>
	/// This method trims trailing whitespace from the specified line before appending it to the <paramref name="builder"/>.
	/// If the trimmed line is empty, no content is appended. The method also prepends the specified indent
	/// and a fixed four-space padding before appending the line.
	/// </remarks>
	private static void AppendTrimmedLine(
		StringBuilder     builder,
		string            text,
		int               start,
		int               length,
		int               depth,
		TextFormatContext tfc)
	{
		// Remove trailing whitespace.
		int end = start + length - 1;
		while (end >= start && char.IsWhiteSpace(text[end]))
		{
			end--;
		}

		int trimmedLength = end - start + 1;
		if (trimmedLength > 0)
		{
			AppendRepeat(builder, tfc.Indent, depth);
			builder.Append("    ")
				.Append(text, start, trimmedLength) // StringBuilder.Append(string, int, int)
				.Append(tfc.NewLine);
		}
	}

	// ───────────────────────── Low-level helpers ─────────────────────────

	/// <summary>
	/// Appends the specified string to the <see cref="StringBuilder"/> instance a specified number of times.
	/// </summary>
	/// <param name="builder">The <see cref="StringBuilder"/> instance to which the string will be appended. Cannot be <c>null</c>.</param>
	/// <param name="s">The string to append. If <c>null</c> or empty, the method does nothing.</param>
	/// <param name="count">The number of times to append the string. Must be greater than zero; otherwise, the method does nothing.</param>
	/// <remarks>
	/// This method performs no operation if <paramref name="s"/> is <c>null</c>, empty,
	/// or if <paramref name="count"/> is less than or equal to zero.
	/// </remarks>
	private static void AppendRepeat(StringBuilder builder, string s, int count)
	{
		if (string.IsNullOrEmpty(s) || count <= 0) return;
		for (int i = 0; i < count; i++) builder.Append(s);
	}

	/// <summary>
	/// Safely retrieves <see cref="Exception.HResult"/> without throwing on older runtimes.
	/// </summary>
	/// <param name="exception">The exception to inspect; must not be <see langword="null"/>.</param>
	/// <returns>
	/// The <see cref="Exception.HResult"/> value, or <c>0</c> if it cannot be retrieved.
	/// </returns>
	private static int SafeGetHResult(Exception exception)
	{
		try { return exception.HResult; }
		catch { return 0; }
	}
}
