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
			builder.Append("…(cycle)");
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
	/// Appends a dictionary value like <c>{ key1 = value1, key2 = value2, … } (Count = N)</c>.
	/// Limits output per <paramref name="options"/>.
	/// </summary>
	/// <param name="builder">Target <see cref="StringBuilder"/>.</param>
	/// <param name="adapter">The dictionary adapter to access entries.</param>
	/// <param name="instance">The dictionary instance.</param>
	/// <param name="options">Formatting options (item limits, depth, type-name mode).</param>
	/// <param name="depth">Current recursion depth.</param>
	/// <param name="visited">Reference set used to detect and avoid cycles.</param>
	/// <param name="typeOptions">Type-name rendering options for nested values.</param>
	/// <param name="tfc">
	/// An optional <see cref="TextFormatContext"/> providing newline, indentation, and culture information.<br/>
	/// If <see langword="null"/>, the engine uses platform defaults.
	/// </param>
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
		int? knownCount = adapter.TryGetCount(instance);
		int max = options.MaxCollectionItems >= 0 ? options.MaxCollectionItems : int.MaxValue;

		builder.Append("{ ");
		int i = 0;
		bool hasMore = false;

		// Enumerate key-value pairs, sorting by key string representation if requested
		IEnumerable<(object? Key, object? Value)> items = adapter.Enumerate(instance);
		if (options.SortMembers) items = items.OrderBy(kv => KeyToInvariantOrdinalString(kv.Key), StringComparer.Ordinal);
		bool tuple = options.DictionaryFormat == DictionaryFormat.Tuples;
		foreach ((object? key, object? value) in items)
		{
			if (i > 0 && i < max) builder.Append(", ");
			if (i >= max)
			{
				hasMore = true;
				break;
			}

			if (tuple) builder.Append('(');
			AppendObject(builder, key, options, depth + 1, visited, typeOptions, tfc);
			builder.Append(tuple ? ", " : " = ");
			AppendObject(builder, value, options, depth + 1, visited, typeOptions, tfc);
			if (tuple) builder.Append(')');

			i++;
		}

		if (hasMore)
		{
			if (i > 0) builder.Append(", ");
			builder.Append('…');
		}

		builder.Append(" }");

		// Append count information
		if (knownCount.HasValue)
			builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
		else if (hasMore)
			builder.Append(" (Count ≥ ").Append((i + 1).ToString(tfc.Culture)).Append(')');
		else
			builder.Append(" (Count = ").Append(i.ToString(tfc.Culture)).Append(')');
	}

	/// <summary>
	/// Converts the specified key to its invariant string representation using ordinal formatting.
	/// </summary>
	/// <param name="key">The object to convert. Can be <see langword="null"/>.</param>
	/// <returns>
	/// A string representation of the key formatted using the invariant culture.
	/// Returns an empty string if <paramref name="key"/> is <see langword="null"/> or if an error occurs during conversion.
	/// </returns>
	/// <remarks>
	/// If the key implements <see cref="IFormattable"/>, its <see cref="IFormattable.ToString(string?, IFormatProvider?)"/>
	/// method is used with the invariant culture. Otherwise, the key's <see cref="object.ToString"/> method is used.
	/// Any bidirectional control characters are stripped from the resulting string.
	/// </remarks>
	private static string KeyToInvariantOrdinalString(object? key)
	{
		if (key == null) return string.Empty;
		try
		{
			if (key is IFormattable f)
			{
				// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
				return TextPostProcessor.StripBiDiControls(f.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty);
			}

			// ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
			return TextPostProcessor.StripBiDiControls(key.ToString() ?? string.Empty);
		}
		catch
		{
			return string.Empty;
		}
	}

	/// <summary>
	/// Appends an <see cref="Array"/> value to the output builder in a concise,
	/// human-readable form including its element type and dimensional sizes.
	/// </summary>
	/// <param name="builder">
	/// The <see cref="StringBuilder"/> that receives the formatted output.
	/// </param>
	/// <param name="array">
	/// The array instance to format. May be multidimensional; must not be <see langword="null"/>.
	/// </param>
	/// <param name="options">
	/// Object formatting options controlling maximum collection items and recursion depth.
	/// </param>
	/// <param name="depth">
	/// Current recursion depth (root = 0). Used to enforce <see cref="PrettyObjectOptions.MaxDepth"/>.
	/// </param>
	/// <param name="visited">
	/// Set of already-visited reference objects used for cycle detection. Arrays are tracked by reference identity.
	/// </param>
	/// <param name="typeOptions">
	/// Type-name rendering options forwarded to <see cref="PrettyTypeEngine"/> when printing the element type.
	/// </param>
	/// <param name="tfc">
	/// An optional <see cref="TextFormatContext"/> providing newline, indentation, and culture information.<br/>
	/// If <see langword="null"/>, the engine uses platform defaults.
	/// </param>
	/// <remarks>
	///     <para>
	///     For one-dimensional arrays the method prints a flat initializer-like list of elements, limited
	///     by <see cref="PrettyObjectOptions.MaxCollectionItems"/> if configured (e.g., <c>Int32[3] { 1, 2, 3 }</c>).
	///     </para>
	///     <para>
	///     Multidimensional arrays are rendered as <c>TypeName[Dim1×Dim2×…] { … }</c> without enumerating
	///     individual elements (e.g., <c>String[2×4] { … }</c>).
	///     </para>
	///     <para>
	///     <b>Note on <c>byte[]</c>:</b>
	///     Byte arrays are handled as a dedicated special case. Instead of formatting each <c>byte</c>
	///     as a number, they are printed in a compact hexadecimal form with a length prefix
	///     (e.g., <c>byte[5] { 0x0A, 0x1F, 0x2C, … }</c>).
	///     This diverges from the generic 1D array path for performance and readability.
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
		// Special (and fast) handling for byte arrays.
		if (array is byte[] bytes)
		{
			int count = bytes.Length;
			int show = options.MaxCollectionItems >= 0 ? Math.Min(count, options.MaxCollectionItems) : count;
			builder.Append("byte[").Append(count).Append("] { ");
			for (int i = 0; i < show; i++)
			{
				if (i > 0) builder.Append(", ");
				builder.Append("0x").Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
			}
			if (show < count) builder.Append(", …");
			builder.Append(" }");
			return;
		}

		// --- Existing logic for all other array types ---

		Type elementType = array.GetType().GetElementType() ?? typeof(object);
		PrettyTypeEngine.AppendType(builder, elementType, typeOptions, tfc);
		builder.Append("[");

		// Ranks like 3×4×2
		for (int d = 0; d < array.Rank; d++)
		{
			if (d > 0) builder.Append('×');
			builder.Append(array.GetLength(d));
		}
		builder.Append("] ");

		// Flatten for 1D arrays only
		if (array.Rank == 1)
		{
			builder.Append("{ ");
			int max = options.MaxCollectionItems >= 0 ? options.MaxCollectionItems : int.MaxValue;
			int shown = 0;
			foreach (object? elem in array)
			{
				if (shown++ > 0) builder.Append(", ");
				if (shown > max)
				{
					builder.Append('…');
					break;
				}
				AppendObject(builder, elem, options, depth + 1, visited, typeOptions, tfc);
			}
			builder.Append(" }");
		}
		else
		{
			// For multidimensional arrays, just show element type and dimensions
			builder.Append("{ … }");
		}
	}

	/// <summary>
	/// Appends an enumerable value like <c>[ item1, item2, … ] (Count = N)</c>.
	/// Honors maximum item limits.
	/// </summary>
	/// <param name="builder">Target <see cref="StringBuilder"/>.</param>
	/// <param name="enumerable">The enumerable to render.</param>
	/// <param name="options">Formatting options (item limits, depth, type-name mode).</param>
	/// <param name="depth">Current recursion depth.</param>
	/// <param name="visited">Reference set used to detect and avoid cycles.</param>
	/// <param name="typeOptions">Type-name rendering options for nested values.</param>
	/// <param name="tfc">
	/// An optional <see cref="TextFormatContext"/> providing newline, indentation, and culture information.<br/>
	/// If <see langword="null"/>, the engine uses platform defaults.
	/// </param>
	private static void AppendEnumerable(
		StringBuilder       builder,
		IEnumerable         enumerable,
		PrettyObjectOptions options,
		int                 depth,
		HashSet<object>     visited,
		PrettyTypeOptions   typeOptions,
		TextFormatContext   tfc)
	{
		// ReSharper disable once PossibleMultipleEnumeration
		int? knownCount = TryGetKnownCount(enumerable);
		int max = options.MaxCollectionItems >= 0 ? options.MaxCollectionItems : int.MaxValue;
		builder.Append("[ ");

		int i = 0;
		bool hasMore = false;
		// ReSharper disable once PossibleMultipleEnumeration
		foreach (object? item in enumerable)
		{
			if (i > 0 && i < max) builder.Append(", ");
			if (i >= max)
			{
				hasMore = true;
				break;
			}

			AppendObject(builder, item, options, depth + 1, visited, typeOptions, tfc);
			i++;
		}

		if (hasMore)
		{
			if (i > 0) builder.Append(", ");
			builder.Append('…');
		}

		builder.Append(" ]");

		if (knownCount.HasValue)
			builder.Append(" (Count = ").Append(knownCount.Value.ToString(tfc.Culture)).Append(')');
		else if (hasMore)
			builder.Append(" (Count ≥ ").Append((i + 1).ToString(tfc.Culture)).Append(')');
		else
			builder.Append(" (Count = ").Append(i.ToString(tfc.Culture)).Append(')');
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
		internal static readonly CountAccessorHolder None = new(null);
		internal readonly        Func<object, int>?  Getter;

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
