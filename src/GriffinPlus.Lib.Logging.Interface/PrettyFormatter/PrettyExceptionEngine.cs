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
// ReSharper disable TailRecursiveCall

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
	/// Static, frozen member options for formatting TargetSite WITH namespace. (Optimized)
	/// </summary>
	private static readonly PrettyMemberOptions sTargetSiteMemberOptionsWithNamespace = new PrettyMemberOptions
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
		UseNamespaceForTypes = true
	}.Freeze();

	/// <summary>
	/// Static, frozen member options for formatting TargetSite WITHOUT namespace. (Optimized)
	/// </summary>
	private static readonly PrettyMemberOptions sTargetSiteMemberOptionsWithoutNamespace = new PrettyMemberOptions
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
		UseNamespaceForTypes = false
	}.Freeze();

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
	/// Text-formatting context providing deterministic newline/indent/culture handling.
	/// </param>
	/// <returns>
	/// A string suitable for logging and diagnostics.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="exception"/> or <paramref name="options"/> is <see langword="null"/>.
	/// </exception>
	public static string Format(Exception exception, PrettyExceptionOptions options, TextFormatContext tfc)
	{
		if (exception == null) throw new ArgumentNullException(nameof(exception));
		if (options == null) throw new ArgumentNullException(nameof(options));

		var builder = new StringBuilder(512);
		var typeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };

		AppendException(builder, exception, options, depth: 0, typeOptions, tfc);
		return TextPostProcessor.ApplyWhole(builder.ToString(), tfc);
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
			PrettyTypeEngine.AppendType(header, exception.GetType(), typeOptions, tfc);
			if (!string.IsNullOrEmpty(exception.Message)) header.Append(": ");
		}
		if (!string.IsNullOrEmpty(exception.Message))
			header.Append(TextPostProcessor.StripBiDiControls(exception.Message));

		AppendRepeat(builder, tfc.Indent, depth);
		builder.Append(header).Append(tfc.NewLine);

		// Meta (HResult / Source / HelpLink / TargetSite)
		AppendMeta(builder, exception, options, depth, typeOptions, tfc);

		// Data
		if (options.IncludeData) AppendData(builder, exception, options, depth, tfc);

		// Stack trace
		if (options.IncludeStackTrace) AppendStackTrace(builder, exception, options, depth, tfc);

		// Inner exceptions
		if (depth < options.MaxInnerExceptionDepth)
		{
			Exception? singleInner = null;                        // For non-Aggregate exceptions
			IReadOnlyCollection<Exception>? multipleInner = null; // For Aggregate exceptions
			bool usedFlattenedInners = false;                     // Flag to indicate if Flatten() was used

			// Determine which inner exceptions to process based on type and options
			if (options.FlattenAggregates && exception is AggregateException aggExForFlatten)
			{
				// Flattening is enabled and it's an AggregateException
				try
				{
					AggregateException flattened = aggExForFlatten.Flatten();
					// Use the inner exceptions from the *flattened* instance
					multipleInner = flattened.InnerExceptions;
					usedFlattenedInners = true; // Mark that we used the flattened set
				}
				catch
				{
					// Flatten() failed, fallback to using the original InnerExceptions
					try { multipleInner = aggExForFlatten.InnerExceptions; }
					catch { multipleInner = null; }
				}
			}
			else // Flattening is disabled or it's not an AggregateException
			{
				if (exception is AggregateException aggExOriginal)
				{
					// It's an AggregateException, use its InnerExceptions directly
					try { multipleInner = aggExOriginal.InnerExceptions; }
					catch { multipleInner = null; }
				}
				else
				{
					// It's a regular exception, use its InnerException
					try { singleInner = exception.InnerException; }
					catch { singleInner = null; }
				}
			}

			// Recursively append the determined inner exceptions
			if (multipleInner?.Count > 0)
			{
				int i = 0;
				foreach (Exception inner in multipleInner)
				{
					// Safety check
					// ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
					if (inner == null) continue;

					AppendRepeat(builder, tfc.Indent, depth); // Indent the "Inner[x]:" line
					// Add index only if it was originally an Aggregate or if flattened
					bool isOriginalAggregate = exception is AggregateException;
					if (isOriginalAggregate || usedFlattenedInners)
						builder.Append("Inner[").Append(i++).Append("]:").Append(tfc.NewLine);
					else // Should not happen with current logic, but defensive
						builder.Append("Inner:").Append(tfc.NewLine);

					// Recursive call for the inner exception
					AppendException(builder, inner, options, depth + 1, typeOptions, tfc);
				}
			}
			else if (singleInner != null)
			{
				AppendRepeat(builder, tfc.Indent, depth); // Indent the "Inner:" line
				builder.Append("Inner:").Append(tfc.NewLine);
				// Recursive call for the single inner exception
				AppendException(builder, singleInner, options, depth + 1, typeOptions, tfc);
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

		if (options.IncludeSource && !string.IsNullOrWhiteSpace(exception.Source))
		{
			AppendRepeat(builder, tfc.Indent, depth);
			builder
				.Append("  Source: ")
				.Append(TextPostProcessor.StripBiDiControls(exception.Source))
				.Append(tfc.NewLine);
		}

		if (options.IncludeHelpLink && !string.IsNullOrWhiteSpace(exception.HelpLink))
		{
			AppendRepeat(builder, tfc.Indent, depth);
			builder
				.Append("  HelpLink: ")
				.Append(TextPostProcessor.StripBiDiControls(exception.HelpLink))
				.Append(tfc.NewLine);
		}

		if (options.IncludeTargetSite && exception.TargetSite != null)
		{
			MethodBase? methodBase = exception.TargetSite;
			string memberText = PrettyMemberEngine.Format(
				methodBase,
				typeOptions.UseNamespace
					? sTargetSiteMemberOptionsWithNamespace
					: sTargetSiteMemberOptionsWithoutNamespace,
				tfc);
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
	/// <param name="tfc">The text-formatting context (newline/indent/culture).</param>
	private static void AppendData(
		StringBuilder          builder,
		Exception              exception,
		PrettyExceptionOptions options,
		int                    depth,
		TextFormatContext      tfc)
	{
		// Early out: No data to render
		if (!options.IncludeData || exception.Data.Count == 0)
			return;

		// Resolve object formatting options for Data.
		PrettyObjectOptions objectOptions = options.ResolveDataObjectOptions();

		// If the profile limits to "0 items", do not render anything
		if (objectOptions.MaxCollectionItems == 0)
			return;

		// Consistent heading "Data:"
		AppendRepeat(builder, tfc.Indent, depth);
		builder.Append("  Data:");

		// Render uniformly with PrettyObjectEngine to respect options (e.g., empty dictionary).
		string rendered = PrettyObjectEngine.Format(exception.Data, objectOptions, tfc);

		// Render: Inline vs. Block
		if (IsMultiline(rendered))
		{
			// Block rendering
			builder.Append(tfc.NewLine);
			AppendIndentedBlock(builder, rendered, tfc, depth + 1);
			builder.Append(tfc.NewLine);
		}
		else
		{
			// Inline rendering
			builder.Append(' ').Append(rendered).Append(tfc.NewLine);
		}
	}

	/// <summary>
	/// Determines whether the specified string contains multiple lines.
	/// </summary>
	/// <param name="s">
	/// The string to evaluate. Cannot be <see langword="null"/>.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the string contains at least one newline character ('\n');<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool IsMultiline(string s)
	{
		foreach (char ch in s)
		{
			if (Unicode.IsLineBreak(ch))
				return true;
		}
		return false;
	}

	/// <summary>
	/// Appends a block of text to the specified <see cref="StringBuilder"/>, applying indentation to the first line
	/// and each subsequent line after a newline character.
	/// </summary>
	/// <param name="builder">The <see cref="StringBuilder"/> to which the indented block will be appended.</param>
	/// <param name="block">The block of text to append. This text may contain newline characters.</param>
	/// <param name="tfc">The <see cref="TextFormatContext"/> that specifies the indentation string to use.</param>
	/// <param name="depth">The depth of indentation, determining how many times the indentation string is repeated.</param>
	/// <remarks>
	/// Each line in the block, including the first line and any lines following a newline character, is
	/// prefixed with the specified number of repetitions of the indentation string.
	/// </remarks>
	private static void AppendIndentedBlock(
		StringBuilder     builder,
		string            block,
		TextFormatContext tfc,
		int               depth)
	{
		// Indent first line.
		AppendRepeat(builder, tfc.Indent, depth);

		// Append the block, indenting after each newline.
		for (int i = 0; i < block.Length;)
		{
			char c = block[i];

			// Handle CRLF as a single newline
			if (c == '\r')
			{
				if (i + 1 < block.Length && block[i + 1] == '\n')
				{
					builder.Append('\r').Append('\n');
					i += 2;
				}
				else
				{
					builder.Append('\r');
					i++;
				}
				AppendRepeat(builder, tfc.Indent, depth);
				continue;
			}

			// LF
			if (c == '\n')
			{
				builder.Append('\n');
				i++;
				AppendRepeat(builder, tfc.Indent, depth);
				continue;
			}

			// Unicode line separators
			if (c is '\u000B' or '\u000C' or '\u0085' or '\u2028' or '\u2029')
			{
				builder.Append(c);
				i++;
				AppendRepeat(builder, tfc.Indent, depth);
				continue;
			}

			builder.Append(c);
			i++;
		}
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
		if (string.IsNullOrEmpty(stackTrace) || options.StackFrameLimit == 0)
			return;

		AppendRepeat(builder, tfc.Indent, depth);
		builder.Append("  StackTrace:").Append(tfc.NewLine);

		int lineCount = 0;
		int startIndex = 0;
		int len = stackTrace.Length;
		int limit = options.StackFrameLimit < 0 ? int.MaxValue : options.StackFrameLimit;

		for (int i = 0; i < len; i++)
		{
			char c = stackTrace[i];

			// Find newline (LF or CRLF)
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
				}
				else
				{
					// It is only \n or only \r
					AppendTrimmedLine(builder, stackTrace, startIndex, lineLength, depth, tfc);
				}
				startIndex = i + 1;
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
			builder.Append("    ").Append(tfc.TruncationMarker).Append(tfc.NewLine);
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
		if (trimmedLength <= 0) return;
		AppendRepeat(builder, tfc.Indent, depth);
		// Stacktrace is an unquoted text => remove BiDi controls
		string line = TextPostProcessor.StripBiDiControls(text.Substring(start, trimmedLength));
		builder.Append("    ").Append(line).Append(tfc.NewLine);
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
