///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// This file is part of the Griffin+ common library suite (https://github.com/griffinplus/dotnet-libs-logging-interface)
// The source code is licensed under the MIT license.
///////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable LoopCanBeConvertedToQuery
// ReSharper disable PossibleMultipleEnumeration

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Internal engine that renders arbitrary objects into concise, human-readable strings.
/// Supports primitive types, strings (with truncation), enums, tuples, dictionaries,
/// enumerables, and arbitrary POCOs (fields/properties), while guarding against cycles.
/// </summary>
/// <remarks>
/// The engine is stateless and thread-safe; it allocates only per-call state.
/// </remarks>
static class PrettyObjectEngine
{
	/// <summary>
	/// Formats an arbitrary object according to <paramref name="options"/>.
	/// </summary>
	/// <param name="obj">
	/// The object to format.<br/>
	/// If <see langword="null"/>, returns <c>"&lt;null&gt;"</c>.
	/// </param>
	/// <param name="options">
	/// Formatting options controlling depth, truncation, member selection and type-name rendering.<br/>
	/// Must not be <see langword="null"/>.
	/// </param>
	/// <param name="tfc">
	/// A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.
	/// </param>
	/// <returns>
	/// A single-line or multi-line string describing <paramref name="obj"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="obj"/> or <paramref name="options"/> is <see langword="null"/>.
	/// </exception>
	public static string Format(object obj, PrettyObjectOptions options, TextFormatContext tfc)
	{
		if (obj == null) throw new ArgumentNullException(nameof(obj));
		if (options == null) throw new ArgumentNullException(nameof(options));

		var builder = new StringBuilder(512);
		var visited = new HashSet<object>(ReferenceComparer.Instance);
		var typeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };

		if (options.ShowTypeHeader)
		{
			PrettyTypeEngine.AppendType(builder, obj.GetType(), typeOptions, tfc);
			if (options.MaxDepth <= 0) return builder.ToString();
			builder.Append(": ");
		}

		AppendObject(builder, obj, options, depth: 0, visited, typeOptions, tfc);
		return TextPostProcessor.ApplyPerLine(builder.ToString(), tfc);
	}

	// ───────────────────────── Core Rendering ─────────────────────────

	/// <summary>
	/// Appends a representation of <paramref name="value"/> to <paramref name="builder"/>.
	/// </summary>
	/// <param name="builder">Target string builder receiving the output.</param>
	/// <param name="value">The current value to render.</param>
	/// <param name="options">Formatting options.</param>
	/// <param name="depth">Current recursion depth (root = 0).</param>
	/// <param name="visited">Set of already-seen reference objects to prevent cycles.</param>
	/// <param name="typeOptions">Options passed through to <see cref="PrettyTypeEngine"/>.</param>
	/// <param name="tfc">
	/// An optional <see cref="TextFormatContext"/> providing newline, indentation, and culture information.<br/>
	/// If <see langword="null"/>, the engine uses platform defaults.
	/// </param>
	/// <remarks>
	/// The <paramref name="visited"/> set tracks reference identity globally for the entire formatting run.
	/// This prevents both true cycles and repeated re-traversal of the same instance across siblings,
	/// trading exact duplication for predictable, bounded output.
	/// </remarks>
	private static void AppendObject(
		StringBuilder       builder,
		object?             value,
		PrettyObjectOptions options,
		int                 depth,
		HashSet<object>     visited,
		PrettyTypeOptions   typeOptions,
		TextFormatContext   tfc)
	{
		if (value == null)
		{
			builder.Append("<null>");
			return;
		}

		// Prevent cycles for reference types other than string
		if (value is not string && IsRefType(value) && !visited.Add(value))
		{
			builder.Append(tfc.TruncationMarker).Append("(cycle)");
			return;
		}

		Type t = value.GetType();

		// Primitives / simple values
		if (TryAppendSimple(builder, value, t, tfc))
			return;

		// Dictionary-like first (covers IDictionary, IDictionary<TKey,TValue>, IReadOnlyDictionary<,>, …)
		if (DictionaryAdapter.TryCreate(value) is { } dictAdapter)
		{
			AppendDictionary(builder, dictAdapter, value, options, depth, visited, typeOptions, tfc);
			return;
		}

		// Array
		if (value is Array array)
		{
			AppendArray(builder, array, options, depth, visited, typeOptions, tfc);
			return;
		}

		// IEnumerable (but avoid string which is IEnumerable<char>)
		if (value is IEnumerable en and not string)
		{
			AppendEnumerable(builder, en, options, depth, visited, typeOptions, tfc);
			return;
		}

		// Fallback: object with members
		AppendObjectMembers(builder, value, t, options, depth, visited, typeOptions, tfc);
	}

	/// <summary>
	/// Appends a compact, culture-aware rendering for primitives, strings, enums, and other known simple value types
	/// (like <see cref="Guid"/>, <see cref="DateTime"/>, <see cref="TimeSpan"/>).
	/// </summary>
	/// <param name="builder">Target builder.</param>
	/// <param name="value">Value to render.</param>
	/// <param name="type">Runtime type of <paramref name="value"/>.</param>
	/// <param name="tfc">
	/// An optional <see cref="TextFormatContext"/> providing newline, indentation, and culture information.<br/>
	/// If <see langword="null"/>, the engine uses platform defaults.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the value has been written; otherwise <see langword="false"/>.
	/// </returns>
	private static bool TryAppendSimple(
		StringBuilder     builder,
		object?           value,
		Type              type,
		TextFormatContext tfc)
	{
		switch (value)
		{
			// String
			case string s:
				builder.Append('"');

				// DO NOT strip BiDi controls here.
				// As this is a *quoted* string, we pass the original string 's'
				// to AppendEscaped(), which will correctly visualize BiDi characters
				// as \uXXXX, preserving data integrity.

				if (!tfc.Truncate || tfc.MaxLineLength <= 0)
				{
					// No truncation path (0 allocs in AppendEscaped)
					TextPostProcessor.AppendEscaped(builder, s, 0, s.Length);
					builder.Append('"');
					return true;
				}

				int cap = tfc.MaxLineLength;
				// If grapheme count <= cap, write whole string
				if (s.Length <= cap &&
				    TextPostProcessor.SafePrefixCharCountByTextElements(s, 0, cap, s.Length) >= s.Length)
				{
					TextPostProcessor.AppendEscaped(builder, s, 0, s.Length);
					builder.Append('"');
					return true;
				}

				// Need to truncate: reserve space for marker (grapheme elements)
				int markerElems = new StringInfo(tfc.TruncationMarker).LengthInTextElements;
				int limit = cap - markerElems;
				if (limit <= 0)
				{
					builder.Append(tfc.TruncationMarker);
					builder.Append('"');
					return true;
				}

				int safeChars = TextPostProcessor.SafePrefixCharCountByTextElements(s, 0, limit, s.Length);
				if (safeChars > 0)
					TextPostProcessor.AppendEscaped(builder, s, 0, safeChars);
				builder.Append(tfc.TruncationMarker);

				builder.Append('"');
				return true;

			// Boolean
			case bool b:
				builder.Append(b ? "true" : "false");
				return true;

			// Char
			case char c:
				builder.Append('\'');
				TextPostProcessor.AppendEscapedChar(builder, c);
				builder.Append('\'');
				return true;

			// Numerics
			case sbyte:
			case byte:
			case short:
			case ushort:
			case int:
			case uint:
			case long:
			case ulong:
			case float:
			case double:
			case decimal:
				builder.Append(Convert.ToString(value, tfc.Culture));
				return true;

			// Guid
			case Guid guid:
				builder.Append(guid.ToString("D"));
				return true;

			// DateTime
			case DateTime dateTime:
				builder.Append(dateTime.ToString("o", tfc.Culture));
				return true;

			// DateTimeOffset
			case DateTimeOffset dateTimeOffset:
				builder.Append(dateTimeOffset.ToString("o", tfc.Culture));
				return true;

			// TimeSpan
			case TimeSpan timeSpan:
				builder.Append(timeSpan.ToString("c", tfc.Culture));
				return true;
		}

		// IsEnum must be separate because IsEnum is not a type pattern.
		if (type.IsEnum)
		{
			builder.Append(value);
			return true;
		}

		return false;
	}

	/// <summary>
	/// Appends a formatted representation of a dictionary value to <paramref name="builder"/>.
	/// Chooses between a compact single-line layout and a multi-line block depending on
	/// <see cref="PrettyObjectOptions.AllowMultiline"/> and the rendered content.
	/// </summary>
	/// <param name="builder">Target <see cref="StringBuilder"/> receiving the formatted text.</param>
	/// <param name="adapter">Unified adapter providing enumeration and optional count for the dictionary-like instance.</param>
	/// <param name="instance">The dictionary-like object to render.</param>
	/// <param name="options">Object-formatting options controlling layout, limits and style.</param>
	/// <param name="depth">Current indentation depth (used for multi-line layout).</param>
	/// <param name="visited">Cycle detection set used by the engine (passed through to <see cref="AppendObject"/>).</param>
	/// <param name="typeOptions">Type-formatting options (passed through to <see cref="AppendObject"/>).</param>
	/// <param name="tfc">Text formatting context (indent token, newline string, culture).</param>
	/// <remarks>
	///     <para>
	///     <b>Determinism:</b> When <see cref="PrettyObjectOptions.SortMembers"/> is <see langword="true"/>, items are
	///     collected up to <see cref="PrettyObjectOptions.MaxCollectionItems"/> and sorted by their <em>rendered key</em>
	///     using ordinal comparison to produce a stable order. When <see langword="false"/>, items are streamed in
	///     enumeration order with O(1) additional memory.
	///     </para>
	///     <para>
	///     <b>Multi-line layout:</b> Enabled if <see cref="PrettyObjectOptions.AllowMultiline"/> is <see langword="true"/>
	///     and either (a) more than one entry is emitted or (b) any rendered key/value contains a line break
	///     (LF/CR/VT/FF/NEL/LS/PS). The block is indented by one level relative to <paramref name="depth"/>.
	///     </para>
	///     <para>
	///     <b>Count suffix:</b> A trailing <c>(Count = n)</c> is appended when the adapter can provide an exact count.
	///     If the count is unknown and truncation occurred, the suffix falls back to <c>(Count ≥ m)</c>, where <c>m</c>
	///     is the number of items emitted plus one.
	///     </para>
	/// </remarks>
	private static void AppendDictionary(
		StringBuilder       builder,
		IDictionaryAdapter  adapter,
		object              instance,
		PrettyObjectOptions options,
		int                 depth,
		HashSet<object>     visited,
		PrettyTypeOptions   typeOptions,
		TextFormatContext   tfc)
	{
		// Get known count (if any) and determine item limit
		int? knownCount = adapter.TryGetCount(instance);
		int limit = options.MaxCollectionItems >= 0 ? options.MaxCollectionItems : int.MaxValue;
		bool tuples = options.DictionaryFormat == DictionaryFormat.Tuples;

		// Nothing to emit? Show empty braces and the most accurate count we have.
		if (limit == 0)
		{
			builder.Append("{ }");
			if (knownCount.HasValue)
				builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
			else
				builder.Append(" (Count ≥ 0)");
			return;
		}

		// ───────────────────────────── Sorted (buffered) path ─────────────────────────────

		// Deterministic output: collect up to 'limit' items, sort by rendered key string (ordinal).

		if (options.SortMembers)
		{
			using IEnumerator<(object? Key, object? Value)> e = adapter.Enumerate(instance).GetEnumerator();

			// 1) Collect up to 'limit' items; render the KEY once (visible form).
			var items = new List<(string KeyRendered, object? Value)>(Math.Min(limit, 16));
			bool hasMore = false;
			int taken = 0;

			while (e.MoveNext())
			{
				if (taken >= limit)
				{
					hasMore = true;
					break;
				}

				// Render key using the same recursion/path as output (quotes, escapes, culture, namespaces, …)
				var keyBuilder = new StringBuilder(64);
				AppendObject(keyBuilder, e.Current.Key, options, depth + 1, visited, typeOptions, tfc);
				items.Add((keyBuilder.ToString(), e.Current.Value));
				taken++;
			}

			// 2) Sort by the rendered KEY string (ordinal). This is stable wrt Options + Culture.
			items.Sort(static (a, b) => string.Compare(a.KeyRendered, b.KeyRendered, StringComparison.Ordinal));

			// 3) Render VALUES and decide multiline (line-breaks in key/value or multiple entries).
			var rendered = new List<(string KR, string VR)>(items.Count);
			bool anyMultiline = false;
			for (int i = 0; i < items.Count; i++)
			{
				var valueBuilder = new StringBuilder(128);
				AppendObject(valueBuilder, items[i].Value, options, depth + 1, visited, typeOptions, tfc);
				string valueString = valueBuilder.ToString();

				if (!anyMultiline && (ContainsLineBreak(items[i].KeyRendered) || ContainsLineBreak(valueString)))
					anyMultiline = true;

				rendered.Add((items[i].KeyRendered, valueString));
			}

			// Inline-first: if it fits, prefer single-line even if thresholds would push to block.
			var inlineTokens = new List<string>(rendered.Count);
			for (int i = 0; i < rendered.Count; i++)
			{
				inlineTokens.Add(
					tuples
						? "( " + rendered[i].KR + ", " + rendered[i].VR + " )"
						: rendered[i].KR + " = " + rendered[i].VR);
			}
			int inlineOverhead = hasMore ? 2 + tfc.TruncationMarker.Length : 0;
			bool fitsInline = !anyMultiline &&
			                  !ExceedsInlineWidth(inlineTokens, options.MaxLineContentWidth, separatorLength: 2, overhead: inlineOverhead);

			if (!options.AllowMultiline || fitsInline)
			{
				// Single-line
				builder.Append("{ ");
				for (int i = 0; i < rendered.Count; i++)
				{
					if (i > 0) builder.Append(", ");
					if (tuples)
						builder.Append("( ").Append(rendered[i].KR).Append(", ").Append(rendered[i].VR).Append(" )");
					else
						builder.Append(rendered[i].KR).Append(" = ").Append(rendered[i].VR);
				}
				if (hasMore)
				{
					if (rendered.Count > 0) builder.Append(", ");
					builder.Append(tfc.TruncationMarker);
				}
				builder.Append(" }");

				if (knownCount.HasValue)
					builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
				else if (hasMore)
					builder.Append(" (Count ≥ ").Append((rendered.Count + 1).ToString(tfc.Culture)).Append(')');
				else
					builder.Append(" (Count = ").Append(rendered.Count.ToString(tfc.Culture)).Append(')');
				return;
			}

			// Multi-line block (flow-wrapped when enabled)
			builder.Append('{').Append(tfc.NewLine);

			if (options.FlowItemsInMultiline)
			{
				// Build entry tokens "key = value" or "( key, value )"
				var tokens = new List<string>(rendered.Count + (hasMore ? 1 : 0));
				for (int i = 0; i < rendered.Count; i++)
				{
					tokens.Add(
						tuples
							? "( " + rendered[i].KR + ", " + rendered[i].VR + " )"
							: rendered[i].KR + " = " + rendered[i].VR);
				}
				if (hasMore) tokens.Add(tfc.TruncationMarker);
				AppendFlowWrappedSequence(
					builder,
					tokens,
					depth + 1,
					tfc,
					options.MaxLineContentWidth,
					hasMoreAfter: false);
			}
			else
			{
				for (int i = 0; i < rendered.Count; i++)
				{
					AppendRepeat(builder, tfc.Indent, depth + 1);
					if (tuples)
						builder.Append("( ").Append(rendered[i].KR).Append(", ").Append(rendered[i].VR).Append(" )");
					else
						builder.Append(rendered[i].KR).Append(" = ").Append(rendered[i].VR);
					if (i < rendered.Count - 1 || hasMore) builder.Append(',');
					builder.Append(tfc.NewLine);
				}
				if (hasMore)
				{
					AppendRepeat(builder, tfc.Indent, depth + 1);
					builder.Append(tfc.TruncationMarker).Append(tfc.NewLine);
				}
			}

			AppendRepeat(builder, tfc.Indent, depth);
			builder.Append('}');

			if (knownCount.HasValue)
				builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
			else if (hasMore)
				builder.Append(" (Count ≥ ").Append((rendered.Count + 1).ToString(tfc.Culture)).Append(')');
			else
				builder.Append(" (Count = ").Append(rendered.Count.ToString(tfc.Culture)).Append(')');
			return;
		}


		// ───────────────────────────── Streaming (unsorted) path ─────────────────────────────

		// O(1) memory: render first pair to decide layout; then stream until limit.
		using (IEnumerator<(object? Key, object? Value)> e2 = adapter.Enumerate(instance).GetEnumerator())
		{
			// Buffer up to 'limit' entries to decide layout and to support flow-wrapped multi-line.
			var renderedPairs = new List<(string KR, string VR)>(Math.Min(limit, 16));
			bool truncated = false;

			while (renderedPairs.Count < limit && e2.MoveNext())
			{
				var ksb2 = new StringBuilder(64);
				var vsb2 = new StringBuilder(128);
				AppendObject(ksb2, e2.Current.Key, options, depth + 1, visited, typeOptions, tfc);
				AppendObject(vsb2, e2.Current.Value, options, depth + 1, visited, typeOptions, tfc);
				renderedPairs.Add((ksb2.ToString(), vsb2.ToString()));
			}
			if (e2.MoveNext()) truncated = true;

			// If no entries were printed
			if (renderedPairs.Count == 0)
			{
				builder.Append("{ }");
				if (knownCount.HasValue)
					builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
				else
					builder.Append(" (Count = 0)");
				return;
			}

			bool anyMultiline2 = false;
			for (int i2 = 0; i2 < renderedPairs.Count; i2++)
			{
				if (ContainsLineBreak(renderedPairs[i2].KR) || ContainsLineBreak(renderedPairs[i2].VR))
				{
					anyMultiline2 = true;
					break;
				}
			}

			// Build the exact inline tokens (visible text) to measure the real line width.
			// This mirrors the sorted path and properly accounts for quotes/escapes/culture.
			var inlineTokens2 = new List<string>(renderedPairs.Count);
			for (int i2 = 0; i2 < renderedPairs.Count; i2++)
			{
				inlineTokens2.Add(
					tuples
						? "( " + renderedPairs[i2].KR + ", " + renderedPairs[i2].VR + " )"
						: renderedPairs[i2].KR + " = " + renderedPairs[i2].VR);
			}

			// If we append an ellipsis inline, include the width of ", " + truncation marker
			int inlineOverhead2 = truncated ? 2 + tfc.TruncationMarker.Length : 0;

			// Inline-first decision: prefer single-line if width allows and no item contains line breaks.
			bool fitsInline2 = !anyMultiline2 &&
			                   !ExceedsInlineWidth(inlineTokens2, options.MaxLineContentWidth, separatorLength: 2, overhead: inlineOverhead2);

			if (!options.AllowMultiline || fitsInline2)
			{
				builder.Append("{ ");
				for (int i2 = 0; i2 < renderedPairs.Count; i2++)
				{
					if (i2 > 0) builder.Append(", ");
					if (tuples)
						builder.Append("( ").Append(renderedPairs[i2].KR).Append(", ").Append(renderedPairs[i2].VR).Append(" )");
					else
						builder.Append(renderedPairs[i2].KR).Append(" = ").Append(renderedPairs[i2].VR);
				}
				if (truncated)
				{
					if (renderedPairs.Count > 0) builder.Append(", ");
					builder.Append(tfc.TruncationMarker);
				}
				builder.Append(" }");

				if (knownCount.HasValue)
					builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
				else if (truncated)
					builder.Append(" (Count ≥ ").Append((renderedPairs.Count + 1).ToString(tfc.Culture)).Append(')');
				else
					builder.Append(" (Count = ").Append(renderedPairs.Count.ToString(tfc.Culture)).Append(')');
				return;
			}

			// Multi-line, flow-wrapped
			builder.Append('{').Append(tfc.NewLine);
			if (options.FlowItemsInMultiline)
			{
				var tokens2 = new List<string>(renderedPairs.Count + (truncated ? 1 : 0));
				for (int i2 = 0; i2 < renderedPairs.Count; i2++)
				{
					tokens2.Add(
						tuples
							? "( " + renderedPairs[i2].KR + ", " + renderedPairs[i2].VR + " )"
							: renderedPairs[i2].KR + " = " + renderedPairs[i2].VR);
				}
				if (truncated) tokens2.Add(tfc.TruncationMarker);
				AppendFlowWrappedSequence(
					builder,
					tokens2,
					depth + 1,
					tfc,
					options.MaxLineContentWidth,
					hasMoreAfter: false);
			}
			else
			{
				for (int i2 = 0; i2 < renderedPairs.Count; i2++)
				{
					AppendRepeat(builder, tfc.Indent, depth + 1);
					if (tuples)
						builder.Append("( ").Append(renderedPairs[i2].KR).Append(", ").Append(renderedPairs[i2].VR).Append(" )");
					else
						builder.Append(renderedPairs[i2].KR).Append(" = ").Append(renderedPairs[i2].VR);
					if (i2 < renderedPairs.Count - 1 || truncated) builder.Append(',');
					builder.Append(tfc.NewLine);
				}
				if (truncated)
				{
					AppendRepeat(builder, tfc.Indent, depth + 1);
					builder.Append(tfc.TruncationMarker).Append(tfc.NewLine);
				}
			}

			AppendRepeat(builder, tfc.Indent, depth);
			builder.Append('}');

			if (knownCount.HasValue)
				builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
			else if (truncated)
				builder.Append(" (Count ≥ ").Append((renderedPairs.Count + 1).ToString(tfc.Culture)).Append(')');
			else
				builder.Append(" (Count = ").Append(renderedPairs.Count.ToString(tfc.Culture)).Append(')');
		}
	}

	/// <summary>
	/// Appends an <see cref="Array"/> value using either a compact single-line layout
	/// (e.g., <c>Int32[3] { 1, 2, 3 }</c>) or a multi-line, indented block when
	/// <see cref="PrettyObjectOptions.AllowMultiline"/> is enabled and beneficial.
	/// </summary>
	/// <param name="builder">Target <see cref="StringBuilder"/>.</param>
	/// <param name="array">The array to render.</param>
	/// <param name="options">Formatting options (limits, layout flags, recursion depth).</param>
	/// <param name="depth">Current recursion depth used for indentation in multi-line layout.</param>
	/// <param name="visited">Cycle-detection set passed through to <see cref="AppendObject"/>.</param>
	/// <param name="typeOptions">Type-name rendering options (forwarded to <see cref="PrettyTypeEngine"/>).</param>
	/// <param name="tfc">Text formatting context (newline, indent token, culture).</param>
	/// <remarks>
	///     <para>
	///     <b>1D arrays:</b> Rendered as <c>ElementType[Length] { … }</c>. Multi-line layout is chosen if
	///     <see cref="PrettyObjectOptions.AllowMultiline"/> is <see langword="true"/> and either more than one
	///     element is to be emitted or the first rendered element already contains a line break (LF/CR/VT/FF/NEL/LS/PS).
	///     Item limiting via <see cref="PrettyObjectOptions.MaxCollectionItems"/> is respected; truncation is shown
	///     with an ellipsis <c>…</c>.
	///     </para>
	///     <para>
	///     <b><c>byte[]</c>:</b> Printed in compact hexadecimal (e.g., <c>byte[5] { 0x0A, 0x1F, … }</c>) for readability.
	///     This special case remains for performance; it also supports multi-line if enabled.
	///     </para>
	///     <para>
	///     <b>Multidimensional arrays:</b> Printed as <c>ElementType[Dim1×Dim2×…] { … }</c> without enumerating all elements,
	///     mirroring the existing compact behavior.
	///     </para>
	///     <para>
	///     Unlike dictionaries/enumerables, arrays already carry their length in the header (e.g., <c>[Length]</c>),
	///     therefore no extra <c>(Count = n)</c> suffix is appended.
	///     </para>
	/// </remarks>
	private static void AppendArray(
		StringBuilder       builder,
		Array               array,
		PrettyObjectOptions options,
		int                 depth,
		HashSet<object>     visited,
		PrettyTypeOptions   typeOptions,
		TextFormatContext   tfc)
	{
		// ---- Fast special case: byte[] as hex ------------------------------------------------------
		bool fitsInline;
		int inlineOverhead;

		if (array is byte[] bytes)
		{
			int total = bytes.Length;
			int limit = options.MaxCollectionItems >= 0 ? Math.Min(total, options.MaxCollectionItems) : total;

			// Header: "byte[Length] "
			builder.Append("byte[").Append(total.ToString(tfc.Culture)).Append("] ");

			// Materialize rendered tokens once (respecting 'limit').
			var tokens = new List<string>(limit);
			for (int i = 0; i < limit; i++)
			{
				tokens.Add("0x" + bytes[i].ToString("X2", tfc.Culture));
			}

			// Inline-first: prefer single-line if the whole content fits, irrespective of thresholds.
			inlineOverhead = limit < total ? 2 + tfc.TruncationMarker.Length : 0;
			fitsInline = !ExceedsInlineWidth(tokens, options.MaxLineContentWidth, separatorLength: 2, inlineOverhead);

			if (!options.AllowMultiline || fitsInline)
			{
				builder.Append("{ ");
				for (int i = 0; i < tokens.Count; i++)
				{
					if (i > 0) builder.Append(", ");
					builder.Append(tokens[i]);
				}
				if (limit < total) builder.Append(", ").Append(tfc.TruncationMarker);
				builder.Append(" }");
				return;
			}

			// Multi-line (flow-wrapped if enabled)
			builder.Append('{').Append(tfc.NewLine);
			if (options.FlowItemsInMultiline)
			{
				AppendFlowWrappedSequence(
					builder,
					tokens,
					depth + 1,
					tfc,
					options.MaxLineContentWidth,
					hasMoreAfter: limit < total);
			}
			else
			{
				for (int i = 0; i < tokens.Count; i++)
				{
					AppendRepeat(builder, tfc.Indent, depth + 1);
					builder.Append(tokens[i]);
					if (i < tokens.Count - 1 || limit < total) builder.Append(',');
					builder.Append(tfc.NewLine);
				}
			}

			if (limit < total)
			{
				if (options.FlowItemsInMultiline)
				{
					AppendFlowWrappedSequence(
						builder,
						[tfc.TruncationMarker],
						depth + 1,
						tfc,
						options.MaxLineContentWidth,
						hasMoreAfter: false);
				}
				else
				{
					AppendRepeat(builder, tfc.Indent, depth + 1);
					builder.Append(tfc.TruncationMarker).Append(tfc.NewLine);
				}
			}

			AppendRepeat(builder, tfc.Indent, depth);
			builder.Append('}');
			return;
		}


		// ---- Multi-dimensional arrays: keep compact summary -----------------------------------------
		if (array.Rank > 1)
		{
			// "ElementType[Dim1×Dim2×…] { … }"
			Type? elementType = array.GetType().GetElementType();
			if (elementType != null)
				PrettyTypeEngine.AppendType(builder, elementType, typeOptions, tfc);
			else
				builder.Append("Array");

			builder.Append('[');
			for (int d = 0; d < array.Rank; d++)
			{
				if (d > 0) builder.Append('×');
				builder.Append(array.GetLength(d).ToString(tfc.Culture));
			}
			builder.Append("] { ").Append(tfc.TruncationMarker).Append(" }");
			return;
		}


		// ---- 1D array (general case) ----------------------------------------------------------------
		// Header: "ElementType[Length] "
		int length = array.Length;
		Type? elemType = array.GetType().GetElementType();
		if (elemType != null)
			PrettyTypeEngine.AppendType(builder, elemType, typeOptions, tfc);
		else
			builder.Append("Array");

		builder.Append('[').Append(length.ToString(tfc.Culture)).Append("] ");

		// How many items to show?
		int max = options.MaxCollectionItems >= 0 ? Math.Min(length, options.MaxCollectionItems) : length;

		// Empty representation
		if (max == 0)
		{
			builder.Append("{ }");
			return;
		}

		// Build tokens for the visible slice (up to 'max') to decide inline vs. multiline.
		var items = new List<string>(max);
		for (int i = 0; i < max; i++)
		{
			var sb = new StringBuilder(128);
			object? element = array.GetValue(i);
			AppendObject(sb, element, options, depth + 1, visited, typeOptions, tfc);
			items.Add(sb.ToString());
		}

		bool anyBreaks = false;
		for (int i = 0; i < items.Count; i++)
		{
			if (ContainsLineBreak(items[i]))
			{
				anyBreaks = true;
				break;
			}
		}

		// Inline-first decision
		inlineOverhead = max < length ? 2 + tfc.TruncationMarker.Length : 0;
		fitsInline = !anyBreaks && !ExceedsInlineWidth(items, options.MaxLineContentWidth, separatorLength: 2, inlineOverhead);

		if (!options.AllowMultiline || fitsInline)
		{
			builder.Append("{ ");
			for (int i = 0; i < items.Count; i++)
			{
				if (i > 0) builder.Append(", ");
				builder.Append(items[i]);
			}
			if (max < length) builder.Append(", ").Append(tfc.TruncationMarker);
			builder.Append(" }");
			return;
		}

		// Multi-line layout
		builder.Append('{').Append(tfc.NewLine);

		if (options.FlowItemsInMultiline)
		{
			AppendFlowWrappedSequence(
				builder,
				items,
				depth + 1,
				tfc,
				options.MaxLineContentWidth,
				hasMoreAfter: max < length);

			if (max < length)
			{
				AppendFlowWrappedSequence(
					builder,
					[tfc.TruncationMarker],
					depth + 1,
					tfc,
					options.MaxLineContentWidth,
					hasMoreAfter: false);
			}
		}
		else
		{
			for (int i = 0; i < items.Count; i++)
			{
				AppendRepeat(builder, tfc.Indent, depth + 1);
				builder.Append(items[i]);
				if (i < items.Count - 1 || max < length) builder.Append(',');
				builder.Append(tfc.NewLine);
			}
			if (max < length)
			{
				AppendRepeat(builder, tfc.Indent, depth + 1);
				builder.Append(tfc.TruncationMarker).Append(tfc.NewLine);
			}
		}

		AppendRepeat(builder, tfc.Indent, depth);
		builder.Append('}');
	}

	/// <summary>
	/// Appends an enumerable value to <paramref name="builder"/> in a compact single-line form
	/// (e.g., <c>[ a, b, c ]</c>) or a multi-line indented block when permitted and beneficial.
	/// </summary>
	/// <param name="builder">Target <see cref="StringBuilder"/>.</param>
	/// <param name="enumerable">The enumerable to render.</param>
	/// <param name="options">Formatting options (limits, depth, layout flags).</param>
	/// <param name="depth">Current recursion depth, used for indentation in multi-line layout.</param>
	/// <param name="visited">Cycle-detection set, passed through to <see cref="AppendObject"/>.</param>
	/// <param name="typeOptions">Type-name rendering options forwarded to nested object formatting.</param>
	/// <param name="tfc">Text formatting context (newline, indentation token, culture).</param>
	/// <remarks>
	///     <para>
	///     <b>Layout</b> – Multi-line layout is selected iff <see cref="PrettyObjectOptions.AllowMultiline"/> is
	///     <see langword="true"/> and either more than one item is expected or the first rendered item already contains a
	///     line break (LF, CR, VT, FF, NEL, LS, PS). Otherwise, a single-line representation is used.
	///     </para>
	///     <para>
	///     <b>Limits</b> – Respects <see cref="PrettyObjectOptions.MaxCollectionItems"/> (0 =&gt; no items; negative =&gt; no limit).
	///     When the limit truncates output and more items are present, an ellipsis <c>…</c> is appended.
	///     </para>
	///     <para>
	///     <b>Count suffix</b> – If the total item count can be obtained cheaply via <c>TryGetKnownCount()</c>, the method prints
	///     <c>(Count = n)</c>. If the count is unknown but truncation occurred, the method prints <c>(Count ≥ m)</c>, where
	///     <c>m</c> is the number of printed items plus one.
	///     </para>
	///     <para>
	///     <b>Note:</b> The method does <em>not</em> sort enumerables; order is preserved as provided by the source.
	///     </para>
	/// </remarks>
	private static void AppendEnumerable(
		StringBuilder       builder,
		IEnumerable         enumerable,
		PrettyObjectOptions options,
		int                 depth,
		HashSet<object>     visited,
		PrettyTypeOptions   typeOptions,
		TextFormatContext   tfc)
	{
		// Try to get a cheap count (arrays, lists, IReadOnlyCollection, …)
		int? knownCount = TryGetKnownCount(enumerable);

		// Determine item limit
		int limit = options.MaxCollectionItems >= 0 ? options.MaxCollectionItems : int.MaxValue;

		// Cap only the *preview* used to decide inline vs. multiline.
		// This prevents buffering enormous sequences when Unlimited is selected.
		const int decisionPreviewCap = 256;
		int decisionLimit = Math.Min(limit, decisionPreviewCap);

		// If zero items should be shown, emit an empty bracket pair and the most accurate count we know.
		if (limit == 0)
		{
			builder.Append("[ ]");
			if (knownCount.HasValue)
				builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
			else
				builder.Append(" (Count ≥ 0)");
			return;
		}

		// Enumerate and render up to 'decisionLimit' items for layout decisions + output.
		var items = new List<string>(decisionLimit);
		int emitted = 0;
		IEnumerator e = enumerable.GetEnumerator();
		bool hasMore = false;

		try
		{
			while (emitted < decisionLimit && e.MoveNext())
			{
				var sb = new StringBuilder(128);
				AppendObject(sb, e.Current, options, depth + 1, visited, typeOptions, tfc);
				items.Add(sb.ToString());
				emitted++;
			}
			// Peek one more to know if there are more beyond the preview
			if (e.MoveNext()) hasMore = true;
		}
		finally
		{
			(e as IDisposable)?.Dispose();
		}

		// Empty sequence
		if (items.Count == 0)
		{
			builder.Append("[ ]");
			if (knownCount.HasValue)
				builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
			else
				builder.Append(" (Count = 0)");
			return;
		}

		// Check for line breaks
		bool anyBreaks = false;
		for (int index = 0; index < items.Count; index++)
		{
			if (ContainsLineBreak(items[index]))
			{
				anyBreaks = true;
				break;
			}
		}

		// Inline-first: prefer single line if width permits and there are no line breaks.
		// (include ", " + truncation marker length when judging inline width)
		int inlineOverhead = hasMore ? 2 + tfc.TruncationMarker.Length : 0;
		bool fitsInline = !anyBreaks && !ExceedsInlineWidth(items, options.MaxLineContentWidth, separatorLength: 2, overhead: inlineOverhead);

		if (!options.AllowMultiline || fitsInline)
		{
			builder.Append("[ ");
			for (int idx = 0; idx < items.Count; idx++)
			{
				if (idx > 0) builder.Append(", ");
				builder.Append(items[idx]);
			}
			if (hasMore) builder.Append(", ").Append(tfc.TruncationMarker);
			builder.Append(" ]");

			// Count suffix
			if (knownCount.HasValue)
				builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
			else if (hasMore)
				builder.Append(" (Count ≥ ").Append((items.Count + 1).ToString(tfc.Culture)).Append(')');
			else
				builder.Append(" (Count = ").Append(items.Count.ToString(tfc.Culture)).Append(')');
			return;
		}

		// Multi-line (flow-wrapped if enabled)
		builder.Append('[').Append(tfc.NewLine);

		if (options.FlowItemsInMultiline)
		{
			AppendFlowWrappedSequence(
				builder,
				items,
				depth + 1,
				tfc,
				options.MaxLineContentWidth,
				hasMoreAfter: hasMore);

			if (hasMore)
			{
				AppendFlowWrappedSequence(
					builder,
					[tfc.TruncationMarker],
					depth + 1,
					tfc,
					options.MaxLineContentWidth,
					hasMoreAfter: false);
			}
		}
		else
		{
			for (int idx = 0; idx < items.Count; idx++)
			{
				AppendRepeat(builder, tfc.Indent, depth + 1);
				builder.Append(items[idx]);
				if (idx < items.Count - 1 || hasMore) builder.Append(',');
				builder.Append(tfc.NewLine);
			}
			if (hasMore)
			{
				AppendRepeat(builder, tfc.Indent, depth + 1);
				builder.Append(tfc.TruncationMarker).Append(tfc.NewLine);
			}
		}

		AppendRepeat(builder, tfc.Indent, depth);
		builder.Append(']');

		// Count suffix mirrors dictionary/enumerable semantics
		if (knownCount.HasValue)
			builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
		else if (hasMore)
			builder.Append(" (Count ≥ ").Append((items.Count + 1).ToString(tfc.Culture)).Append(')');
		else
			builder.Append(" (Count = ").Append(items.Count.ToString(tfc.Culture)).Append(')');
	}

	// ───────────────────────── AppendObjectMembers() Cache ─────────────────────────

	/// <summary>
	/// Holds cached type members (properties and fields) for a specific type.
	/// </summary>
	private readonly struct CachedTypeMembers
	{
		public readonly PropertyInfo[] Properties;
		public readonly FieldInfo[]    Fields;

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedTypeMembers"/> class,
		/// storing the specified properties and fields for a type.
		/// </summary>
		/// <param name="properties">
		/// An array of <see cref="PropertyInfo"/> objects representing the properties of the type.<br/>
		/// This parameter cannot be <see langword="null"/>.
		/// </param>
		/// <param name="fields">
		/// An array of <see cref="FieldInfo"/> objects representing the fields of the type.
		/// This parameter cannot be <see langword="null"/>.
		/// </param>
		public CachedTypeMembers(PropertyInfo[] properties, FieldInfo[] fields)
		{
			Properties = properties;
			Fields = fields;
		}
	}

	/// <summary>
	/// Composite cache key for <see cref="sMemberCache"/>.
	/// Includes Type and the options that change reflection results.
	/// </summary>
	private readonly struct InnerMemberCacheKey(bool includeNonPublic, bool sortMembers) :
		IEquatable<InnerMemberCacheKey>
	{
		/// <summary>
		/// Indicates whether non-public members should be included.
		/// </summary>
		public readonly bool IncludeNonPublic = includeNonPublic;

		/// <summary>
		/// Indicates whether members should be sorted.
		/// </summary>
		public readonly bool SortMembers = sortMembers;

		/// <summary>
		/// Determines whether the current instance is equal to another instance of <see cref="InnerMemberCacheKey"/>.
		/// </summary>
		/// <param name="other">The <see cref="InnerMemberCacheKey"/> instance to compare with the current instance.</param>
		/// <returns>
		/// <see langword="true"/> if the specified instance has the same values for <see cref="IncludeNonPublic"/>
		/// and <see cref="SortMembers"/> as the current instance; otherwise, <see langword="false"/>.
		/// </returns>
		public bool Equals(InnerMemberCacheKey other)
		{
			return IncludeNonPublic == other.IncludeNonPublic &&
			       SortMembers == other.SortMembers;
		}

		/// <summary>
		/// Determines whether the specified object is equal to the current instance.
		/// </summary>
		/// <param name="obj">The object to compare with the current instance. Can be <see langword="null"/>.</param>
		/// <returns>
		/// <see langword="true"/> if the specified object is of the same type and represents the same value as the current
		/// instance; otherwise, <see langword="false"/>.
		/// </returns>
		public override bool Equals(object? obj)
		{
			return obj is InnerMemberCacheKey other && Equals(other);
		}

		/// <summary>
		/// Computes a hash code for the current object.
		/// </summary>
		/// <remarks>
		/// The hash code is calculated using the values of the <see cref="IncludeNonPublic"/> and <see cref="SortMembers"/>
		/// properties. This ensures that objects with the same configuration produce the same hash code.
		/// </remarks>
		/// <returns>
		/// An integer representing the hash code of the current object.
		/// </returns>
		public override int GetHashCode()
		{
			unchecked // Overflow is fine
			{
				int hash = IncludeNonPublic.GetHashCode();
				hash = (hash * 397) ^ SortMembers.GetHashCode();
				return hash;
			}
		}
	}

	/// <summary>
	/// A thread-safe cache that maps a <see cref="Type"/> to a <see cref="ConcurrentDictionary{TKey,TValue}"/>
	/// containing cached member information for that type.
	/// </summary>
	/// <remarks>
	/// This cache uses a <see cref="ConditionalWeakTable{TKey,TValue}"/> to ensure that the cached data
	/// is automatically removed when the associated <see cref="Type"/> is no longer referenced. The inner dictionary
	/// stores cached member data, keyed by <see cref="InnerMemberCacheKey"/>.
	/// </remarks>
	private static readonly ConditionalWeakTable<Type, ConcurrentDictionary<InnerMemberCacheKey, CachedTypeMembers>> sMemberCache = new();

	/// <summary>
	/// Appends a POCO-style object by rendering its selected properties and fields in a compact initializer-like form.
	/// </summary>
	/// <param name="builder">Target <see cref="StringBuilder"/>.</param>
	/// <param name="value">The object instance to inspect.</param>
	/// <param name="type">Runtime type of <paramref name="value"/>.</param>
	/// <param name="options">Formatting options (depth, visibility, sorting, member inclusion).</param>
	/// <param name="depth">Current recursion depth.</param>
	/// <param name="visited">Reference set used to detect and avoid cycles.</param>
	/// <param name="typeOptions">Type-name rendering options for headers/abbreviations.</param>
	/// <param name="tfc">
	/// An optional <see cref="TextFormatContext"/> providing newline, indentation, and culture information.<br/>
	/// If <see langword="null"/>, the engine uses platform defaults.
	/// </param>
	/// <remarks>
	/// Stops at <see cref="PrettyObjectOptions.MaxDepth"/> and prints only the type name when the limit is reached.
	/// Members are ordered as configured; access exceptions are swallowed and shown as <c>!{ExceptionType}</c>.
	/// Indexer properties are skipped as they require parameters and typically throw when accessed without indices.
	/// </remarks>
	private static void AppendObjectMembers(
		StringBuilder       builder,
		object              value,
		Type                type,
		PrettyObjectOptions options,
		int                 depth,
		HashSet<object>     visited,
		PrettyTypeOptions   typeOptions,
		TextFormatContext   tfc)
	{
		if (depth >= options.MaxDepth)
		{
			// Just show the type name at depth limit.
			PrettyTypeEngine.AppendType(builder, type, typeOptions, tfc);
			return;
		}

		ConcurrentDictionary<InnerMemberCacheKey, CachedTypeMembers> nestedDict = sMemberCache.GetValue(
			type,
			static _ => new ConcurrentDictionary<InnerMemberCacheKey, CachedTypeMembers>());

		var innerKey = new InnerMemberCacheKey(options.IncludeNonPublic, options.SortMembers);

		// Get members from cache or load them via GetOrAdd().
		CachedTypeMembers members = nestedDict.GetOrAdd(
			innerKey,
			key =>
			{
				BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
				if (key.IncludeNonPublic) flags |= BindingFlags.NonPublic;
				PropertyInfo[] properties = type.GetProperties(flags);
				FieldInfo[] fields = type.GetFields(flags);

				if (key.SortMembers)
				{
					Array.Sort(properties, (a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
					Array.Sort(fields, (a,     b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
				}

				return new CachedTypeMembers(properties, fields);
			});

		builder.Append("{ ");
		bool isFirst = true;

		if (options.IncludeProperties)
		{
			// Iterate over the cached members.Properties
			foreach (PropertyInfo property in members.Properties)
			{
				if (!property.CanRead) continue;                        // Skip write-only properties.
				if (property.GetIndexParameters().Length > 0) continue; // Skip indexers (need parameters).

				if (!isFirst) builder.Append(", ");
				builder.Append(property.Name).Append(" = ");
				object? propertyValue = GetMemberValue(value, property);
				AppendObject(builder, propertyValue, options, depth + 1, visited, typeOptions, tfc);
				isFirst = false;
			}
		}

		if (options.IncludeFields)
		{
			// Iterate over the cached members.Fields
			foreach (FieldInfo fieldInfo in members.Fields)
			{
				if (!isFirst) builder.Append(", ");
				builder.Append(fieldInfo.Name).Append(" = ");
				object? fieldValue = GetMemberValue(value, fieldInfo);
				AppendObject(builder, fieldValue, options, depth + 1, visited, typeOptions, tfc);
				isFirst = false;
			}
		}

		builder.Append(" }");
		return;

		// Safely retrieves the value of a property or field, returning an error message string on failure.
		static object? GetMemberValue(object value, MemberInfo member)
		{
			try
			{
				if (member is PropertyInfo pi) return pi.GetValue(value, index: null);
				if (member is FieldInfo fi) return fi.GetValue(value);
			}
			catch (TargetInvocationException ex) when (ex.InnerException != null)
			{
				return "!" + ex.InnerException.GetType().Name;
			}
			catch (Exception ex)
			{
				return "!" + ex.GetType().Name;
			}

			// Should not happen if member is PropertyInfo or FieldInfo
			return "!(Unsupported MemberType)";
		}
	}

	// ───────────────────────── TryGetKnownCount() Cache ─────────────────────────

	/// <summary>
	/// Represents a container for a function that retrieves an integer value based on an input object.
	/// </summary>
	/// <remarks>
	/// This class is used to encapsulate a getter function that can be invoked to retrieve an integer
	/// value. If no getter function is provided, the <see cref="None"/> singleton instance is used to
	/// represent the absence of a getter.
	/// </remarks>
	private sealed class CountAccessorHolder
	{
		/// <summary>
		/// Represents a static instance of <see cref="CountAccessorHolder"/> with no associated value.
		/// </summary>
		/// <remarks>
		/// This instance is initialized with a <see langword="null"/> value and can be used as a default
		/// or placeholder where no valid <see cref="CountAccessorHolder"/> is required.
		/// </remarks>
		internal static readonly CountAccessorHolder None = new(null);

		/// <summary>
		/// Represents a delegate that retrieves an integer value from an object.
		/// </summary>
		/// <remarks>
		/// The delegate takes an object as input and returns an integer. This can be used to extract or
		/// compute  a value based on the provided object. If the delegate is null, no retrieval operation is
		/// defined.
		/// </remarks>
		internal readonly Func<object, int>? Getter;

		/// <summary>
		/// Initializes a new instance of the <see cref="CountAccessorHolder"/> class with the specified getter function.
		/// </summary>
		/// <param name="getter">
		/// A function that retrieves an integer value based on an input object,
		/// or <see langword="null"/> if no getter is provided.
		/// </param>
		private CountAccessorHolder(Func<object, int>? getter)
		{
			Getter = getter;
		}

		/// <summary>
		/// Creates a holder for a getter or returns the <see cref="None"/> singleton
		/// to represent the absence of a getter. Never returns null.
		/// </summary>
		internal static CountAccessorHolder From(Func<object, int>? getter) => getter is null ? None : new CountAccessorHolder(getter);
	}

	/// <summary>
	/// A thread-safe cache that associates a <see cref="Type"/> with its corresponding  <see cref="CountAccessorHolder"/>
	/// instance.
	/// </summary>
	/// <remarks>
	/// This cache is used to store and retrieve precomputed accessors for types, enabling efficient
	/// access to count-related metadata. The use of <see cref="ConditionalWeakTable{TKey, TValue}"/>
	/// ensures that entries are automatically removed when the associated key is no longer referenced.
	/// </remarks>
	private static readonly ConditionalWeakTable<Type, CountAccessorHolder> sReadOnlyCountGetterCache = new();

	private static int? TryGetKnownCount(IEnumerable enumerable)
	{
		// Non-generic ICollection is the cheapest path.
		if (enumerable is ICollection c)
			return c.Count;

		// IReadOnlyCollection<T>:
		// Get accessor delegate from cache, create new one, if necessary.
		Type type = enumerable.GetType();

		// Get or create Count-accessor holder for the type.
		CountAccessorHolder holder = sReadOnlyCountGetterCache.GetValue(
			type,
			static t => CountAccessorHolder.From(BuildReadOnlyCountGetter(t)));

		// Invoke the getter, if available.
		if (holder.Getter is null) return null;
		try { return holder.Getter(enumerable); }
		catch { return null; } // defensive
	}

	/// <summary>
	/// Creates a delegate that retrieves the <see cref="IReadOnlyCollection{T}.Count"/> property of an
	/// <see cref="IReadOnlyCollection{T}"/> implemented by the specified type.
	/// </summary>
	/// <param name="concreteType">The type to inspect for an implementation of <see cref="IReadOnlyCollection{T}"/>.</param>
	/// <returns>
	/// A compiled delegate that takes an object and returns the value of its <c>Count</c> property if the object
	/// implements <see cref="IReadOnlyCollection{T}"/>; otherwise, <see langword="null"/>.
	/// </returns>
	/// <remarks>
	///     <para>
	///     This method inspects the provided type to determine if it implements <see cref="IReadOnlyCollection{T}"/>.
	///     If such an interface is found, it generates and compiles a lambda expression to access the
	///     <see cref="IReadOnlyCollection{T}.Count"/> property. The resulting delegate can be used to retrieve the count
	///     of elements in an object implementing <see cref="IReadOnlyCollection{T}"/> without requiring compile-time
	///     knowledge of the collection's generic type.
	///     </para>
	///     <para>
	///     If the type does not implement <see cref="IReadOnlyCollection{T}"/> or if the delegate cannot
	///     be compiled (e.g., due to security restrictions or trimming scenarios), the method returns <see langword="null"/>.
	///     </para>
	/// </remarks>
	private static Func<object, int>? BuildReadOnlyCountGetter(Type concreteType)
	{
		// find the concrete IReadOnlyCollection<T>-Interface of the type
		Type? iroc = concreteType
			.GetInterfaces()
			.FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>));
		if (iroc == null) return null;

		// (object o) => ((IReadOnlyCollection<T>)o).Count
		ParameterExpression param = Expression.Parameter(typeof(object), "o");
		UnaryExpression cast = Expression.Convert(param, iroc);
		MemberExpression prop = Expression.Property(cast, "Count"); // int Count { get; }
		Expression<Func<object, int>> lambda = Expression.Lambda<Func<object, int>>(prop, param);

		try
		{
			return lambda.Compile(); // compiles only once per type.
		}
		catch
		{
			// Extremely rare case (e.g. security/trimming edge cases)...
			// Fallback: Return a delegate that uses reflection to get the value.
			var countProperty = (PropertyInfo)prop.Member;
			return obj => (int)countProperty.GetValue(obj)!;
		}
	}

	// ───────────────────────── Helpers ─────────────────────────

	/// <summary>
	/// Determines whether the specified object is a reference-type instance (excluding <see cref="string"/>).
	/// </summary>
	/// <param name="obj">Object to test.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="obj"/> is a reference type (not a value type) and not a string;<br/>
	/// otherwise <see langword="false"/>.
	/// </returns>
	private static bool IsRefType(object obj)
	{
		Type type = obj.GetType();
		return !type.IsValueType && obj is not string;
	}

	// ───────────────────────── Multiline helpers ─────────────────────────

	/// <summary>
	/// Determines whether the specified string contains a line break (LF, CR, VT, FF, NEL, LS, or PS).
	/// </summary>
	/// <param name="s">The string to check for line breaks.</param>
	/// <returns>
	/// <see langword="true"/> if <paramref name="s"/> contains at least one line break character;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ContainsLineBreak(string s)
	{
		// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
		foreach (char ch in s)
		{
			if (Unicode.IsLineBreak(ch))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Returns <see langword="true"/> if the inline content composed from the given
	/// segments would exceed the specified maximum width. The <paramref name="separatorLength"/>
	/// parameter represents the length of the delimiter inserted between segments (e.g., ", " = 2).
	/// A non-positive <paramref name="maxWidth"/> disables the check.
	/// </summary>
	private static bool ExceedsInlineWidth(
		IReadOnlyList<string> segments,
		int                   maxWidth,
		int                   separatorLength,
		int                   overhead = 0)
	{
		if (maxWidth <= 0) return false;
		int total = overhead;
		for (int i = 0; i < segments.Count; i++)
		{
			if (i > 0) total += separatorLength;
			total += segments[i].Length;
			if (total > maxWidth) return true;
		}
		return false;
	}

	/*
	/// <summary>
	/// Determines whether the inline content for a dictionary would exceed the specified maximum width.
	/// This variant avoids allocating per-entry strings by combining key/value lengths with a fixed
	/// glue length (e.g., " = " or tuple glue).
	/// </summary>
	/// <returns>
	/// <see langword="true"/> if the combined length of all entries exceeds;<br/>
	/// otherwise, <see langword="false"/>.
	/// </returns>
	private static bool ExceedsInlineWidthForDictionary(
		IReadOnlyList<(string KR, string VR)> entries,
		int                                   tupleGlueLength,
		int                                   maxWidth,
		int                                   separatorLength)
	{
		if (maxWidth <= 0) return false;
		int total = 0;
		for (int i = 0; i < entries.Count; i++)
		{
			if (i > 0) total += separatorLength;
			total += entries[i].KR.Length + entries[i].VR.Length + tupleGlueLength;
			if (total > maxWidth) return true;
		}
		return false;
	}
	*/

	/// <summary>
	/// Appends a sequence of already-rendered item strings using a flow layout:
	/// multiple items per line up to a maximum line width; inserts new lines and
	/// indentation as needed. Items are separated by ", " like in inline output.
	/// <para>
	/// If <paramref name="hasMoreAfter"/> is <see langword="true"/>, the method will
	/// also append a trailing comma at the end of the last printed line to indicate
	/// that more content follows (e.g., an ellipsis "…" rendered by the caller on a
	/// subsequent line). This keeps the comma semantics consistent: commas separate
	/// items, regardless of line wrapping.
	/// </para>
	/// </summary>
	/// <param name="builder">
	/// Target <see cref="System.Text.StringBuilder"/> to receive the formatted output.
	/// </param>
	/// <param name="items">
	/// The already-rendered item strings to print (each string is assumed to be exactly the
	/// visible token for a single item, including quotes/escapes as required).
	/// </param>
	/// <param name="depth">
	/// Indentation depth in the current block; each visual line begins with <c>depth</c>
	/// repetitions of <see cref="TextFormatContext.Indent"/>.
	/// </param>
	/// <param name="tfc">
	/// Text formatting context providing indentation string and line break sequence.
	/// </param>
	/// <param name="maxLineWidth">
	/// Maximum number of characters per visual line for the content portion. The width is
	/// applied to the text inside the braces/brackets only (i.e., between "{ … }" or "[ … ]").
	/// A value &lt;= 0 disables wrapping by width (all items go onto one line).
	/// </param>
	/// <param name="hasMoreAfter">
	/// When <see langword="true"/>, a trailing comma is emitted at the end of the last
	/// printed line because additional content will follow on the next line (for example
	/// an ellipsis "…").
	/// </param>
	private static void AppendFlowWrappedSequence(
		StringBuilder         builder,
		IReadOnlyList<string> items,
		int                   depth,
		TextFormatContext     tfc,
		int                   maxLineWidth,
		bool                  hasMoreAfter)
	{
		// Trivial case: nothing to print.
		if (items.Count == 0)
			return;

		int i = 0;
		while (i < items.Count)
		{
			// Start a new visual line with the current indentation.
			AppendRepeat(builder, tfc.Indent, depth);

			int lineWidth = 0; // accumulated content width on this visual line
			bool firstOnLine = true;

			// Greedily pack items onto the current line until adding the next token
			// would exceed the configured line width (if enabled).
			while (i < items.Count)
			{
				string token = items[i];

				// If it's the first token on the line, we need only its length;
				// otherwise we need ", " (2 chars) plus the token length.
				int needed = firstOnLine ? token.Length : 2 + token.Length;

				// If a width is configured and adding the next token would exceed it,
				// we stop here and wrap to the next visual line.
				if (maxLineWidth > 0 && !firstOnLine && lineWidth + needed > maxLineWidth)
					break;

				// Add separator before the token if we already printed something on this line.
				if (!firstOnLine)
				{
					builder.Append(", ");
					lineWidth += 2;
				}

				// Append the token itself and update the accumulated width.
				builder.Append(token);
				lineWidth += token.Length;

				firstOnLine = false;
				i++;
			}

			// If more items remain, or the caller announced that additional content
			// follows (e.g., an ellipsis on the next line), append a trailing comma.
			if (i < items.Count || hasMoreAfter)
				builder.Append(',');

			// Terminate this visual line.
			builder.Append(tfc.NewLine);
		}
	}

	/// <summary>
	/// Appends <paramref name="indent"/> exactly <paramref name="count"/> times.
	/// </summary>
	/// <param name="builder">The <see cref="StringBuilder"/> that receives the indentation.</param>
	/// <param name="indent">The indentation string to append.</param>
	/// <param name="count">The number of times to append <paramref name="indent"/>.</param>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void AppendRepeat(StringBuilder builder, string indent, int count)
	{
		for (int i = 0; i < count; i++) builder.Append(indent);
	}

	/// <summary>
	/// Reference equality comparer usable on all TFMs (compares by object identity).
	/// </summary>
	private sealed class ReferenceComparer : IEqualityComparer<object>
	{
		/// <summary>
		/// Singleton instance to avoid per-call allocations.
		/// </summary>
		internal static readonly ReferenceComparer Instance = new();

		/// <summary>
		/// Returns <see langword="true"/> if <paramref name="x"/> and <paramref name="y"/> refer to the same object instance.
		/// </summary>
		// ReSharper disable once MemberHidesStaticFromOuterClass
		public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

		/// <summary>
		/// Returns a hash code based on object identity (runtime-provided), not value semantics.
		/// </summary>
		public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
	}
}
