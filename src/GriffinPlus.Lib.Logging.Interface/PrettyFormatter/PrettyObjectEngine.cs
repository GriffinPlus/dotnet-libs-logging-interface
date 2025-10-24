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
	/// An optional <see cref="TextFormatContext"/> providing newline, indentation, and culture information.<br/>
	/// If <see langword="null"/>, the engine uses platform defaults.
	/// </param>
	/// <returns>
	/// A single-line or multi-line string describing <paramref name="obj"/>.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="options"/> is <see langword="null"/>.
	/// </exception>
	public static string Format(object? obj, PrettyObjectOptions options, TextFormatContext? tfc = null)
	{
		if (obj == null) return "<null>";
		if (options == null) throw new ArgumentNullException(nameof(options));

		var builder = new StringBuilder(512);
		TextFormatContext tf = tfc ?? TextFormatContext.From(null);
		var visited = new HashSet<object>(ReferenceComparer.Instance);
		var typeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };

		if (options.ShowTypeHeader)
		{
			builder.Append(PrettyTypeEngine.Format(obj.GetType(), typeOptions));
			if (options.MaxDepth <= 0) return builder.ToString();
			builder.Append(": ");
		}

		AppendObject(builder, obj, options, depth: 0, visited, typeOptions, tf);
		return TextPostProcessor.ApplyPerLine(builder.ToString(), tf);
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
		if (TryAppendSimple(builder, value, t, options, tfc))
			return;

		// IDictionary first (covers most map-like types)
		if (value is IDictionary dict)
		{
			AppendDictionary(builder, dict, options, depth, visited, typeOptions, tfc);
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
	/// Appends a compact rendering for primitive values, strings and enums.
	/// </summary>
	/// <param name="builder">Target builder.</param>
	/// <param name="value">Value to render.</param>
	/// <param name="type">Runtime type of <paramref name="value"/>.</param>
	/// <param name="options">Formatting options.</param>
	/// <param name="tfc">
	/// An optional <see cref="TextFormatContext"/> providing newline, indentation, and culture information.<br/>
	/// If <see langword="null"/>, the engine uses platform defaults.
	/// </param>
	/// <returns>
	/// <see langword="true"/> if the value has been written; otherwise <see langword="false"/>.
	/// </returns>
	/// <remarks>
	///     <para>
	///     <b>Note on <c>byte[]</c> formatting:</b>
	///     Byte arrays are handled as a dedicated special case. Instead of recursively formatting
	///     each element, they are printed in a compact hexadecimal form with a length prefix,
	///     for example <c>byte[5] { 0A, 1F, 2C, … }</c>.
	///     This diverges from the generic array path for performance and readability reasons.
	///     </para>
	/// </remarks>
	private static bool TryAppendSimple(
		StringBuilder       builder,
		object?             value,
		Type                type,
		PrettyObjectOptions options,
		TextFormatContext   tfc)
	{
		switch (value)
		{
			// String
			case string s:
				builder.Append('"');
				if (options.MaxStringLength >= 0 && s.Length > options.MaxStringLength)
					builder.Append(EscapeString(s.Substring(0, options.MaxStringLength))).Append('…');
				else
					builder.Append(EscapeString(s));
				builder.Append('"');
				return true;

			// Boolean
			case bool b:
				builder.Append(b ? "true" : "false");
				return true;

			// Char
			case char c:
				builder.Append('\'').Append(EscapeChar(c)).Append('\'');
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
	/// Escapes a character for use in string literals by converting it to its escaped representation.
	/// </summary>
	/// <remarks>
	/// This method handles common escape sequences such as backslashes, single quotes, and control
	/// characters. For control characters not explicitly handled, the method returns a Unicode escape sequence in the
	/// format <c>"\\uXXXX"</c>, where <c>XXXX</c> is the hexadecimal code of the character.
	/// </remarks>
	/// <param name="c">The character to escape.</param>
	/// <returns>
	/// A string containing the escaped representation of the character. For example, control characters are converted to
	/// their escape sequences (e.g., <c>'\n'</c> becomes <c>"\\n"</c>), and other characters are returned as-is unless
	/// they require escaping.
	/// </returns>
	private static string EscapeChar(char c)
	{
		// Escape unmatched surrogate code units to avoid ill-formed UTF-16 in output.
		if (char.IsSurrogate(c))
			return "\\u" + ((int)c).ToString("X4", CultureInfo.InvariantCulture);

		return c switch
		{
			'\\' => "\\\\",
			'\'' => "\\\'",
			'\n' => "\\n",
			'\r' => "\\r",
			'\t' => "\\t",
			var _ => char.IsControl(c)
				         ? "\\u" + ((int)c).ToString("X4", CultureInfo.InvariantCulture)
				         : c.ToString()
		};
	}

	/// <summary>
	/// Escapes special characters in a string using common C#-style escapes (\", \\, \n, \r, \t)
	/// and \uXXXX for control characters. This keeps log output single-line and copy/paste friendly.
	/// </summary>
	/// <param name="input">The string to escape. Cannot be <see langword="null"/>.</param>
	/// <returns>
	/// A new string with special characters replaced by their escaped representations. For example, backslashes are
	/// replaced with <c>\</c>, double quotes with <c>"</c>, and control characters with their Unicode escape sequences
	/// (e.g., <c>\u000A</c> for a newline).
	/// </returns>
	private static string EscapeString(string input)
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
			if (c is '\\' or '\"' or '\n' or '\r' or '\t' || char.IsControl(c) || char.IsSurrogate(c))
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
	/// Appends a dictionary value like <c>{ key1 = value1, key2 = value2, … } (Count = N)</c>.
	/// Limits output per <paramref name="options"/>.
	/// </summary>
	/// <param name="builder">Target <see cref="StringBuilder"/>.</param>
	/// <param name="dict">The dictionary to render.</param>
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
		IDictionary         dict,
		PrettyObjectOptions options,
		int                 depth,
		HashSet<object>     visited,
		PrettyTypeOptions   typeOptions,
		TextFormatContext   tfc)
	{
		int count = dict.Count;
		int max = options.MaxCollectionItems >= 0 ? Math.Min(count, options.MaxCollectionItems) : count;

		builder.Append("{ ");
		int i = 0;
		foreach (DictionaryEntry entry in dict)
		{
			if (i > 0) builder.Append(", ");
			if (i >= max)
			{
				builder.Append('…');
				break;
			}

			AppendObject(builder, entry.Key, options, depth + 1, visited, typeOptions, tfc);
			builder.Append(" = ");
			AppendObject(builder, entry.Value, options, depth + 1, visited, typeOptions, tfc);
			i++;
		}
		builder.Append(" } (Count = ").Append(count).Append(")");
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
	///     by <see cref="PrettyObjectOptions.MaxCollectionItems"/> if configured.
	///     </para>
	///     <para>
	///     Multidimensional arrays are rendered as <c>TypeName[Dim1×Dim2×…] { … }</c> without enumerating
	///     individual elements, to avoid excessive output and complexity.
	///     </para>
	///     <para>
	///     Example outputs:
	///     <list type="bullet">
	///         <item>
	///             <description>
	///                 <c>Int32[3] { 1, 2, 3 }</c>
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 <c>String[2×4] { … }</c>
	///             </description>
	///         </item>
	///         <item>
	///             <description>
	///                 <c>Byte[5] { 10, 20, 30, … }</c>
	///             </description>
	///         </item>
	///     </list>
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
				builder.Append(bytes[i].ToString("X2", CultureInfo.InvariantCulture));
			}
			if (show < count) builder.Append(", …");
			builder.Append(" }");
			return;
		}

		// --- Existing logic for all other array types ---

		Type elementType = array.GetType().GetElementType() ?? typeof(object);
		builder.Append(PrettyTypeEngine.Format(elementType, typeOptions));
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
	private readonly struct MemberCacheKey(Type type, bool includeNonPublic, bool sortMembers) :
		IEquatable<MemberCacheKey>
	{
		public readonly Type Type             = type;
		public readonly bool IncludeNonPublic = includeNonPublic;
		public readonly bool SortMembers      = sortMembers;

		public bool Equals(MemberCacheKey other) => Type == other.Type &&
		                                            IncludeNonPublic == other.IncludeNonPublic &&
		                                            SortMembers == other.SortMembers;

		public override bool Equals(object? obj) => obj is MemberCacheKey other && Equals(other);

		public override int GetHashCode()
		{
			unchecked // Overflow is fine
			{
				int hash = Type.GetHashCode();
				hash = (hash * 397) ^ IncludeNonPublic.GetHashCode();
				hash = (hash * 397) ^ SortMembers.GetHashCode();
				return hash;
			}
		}
	}

	private static readonly ConcurrentDictionary<MemberCacheKey, CachedTypeMembers> sMemberCache = new();

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
			builder.Append(PrettyTypeEngine.Format(type, typeOptions));
			return;
		}

		// Create the composite key based on type and relevant options.
		var cacheKey = new MemberCacheKey(type, options.IncludeNonPublic, options.SortMembers);

		// Get members from cache or load them via GetOrAdd().
		CachedTypeMembers members = sMemberCache.GetOrAdd(
			cacheKey,
			key =>
			{
				BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
				if (key.IncludeNonPublic) flags |= BindingFlags.NonPublic; // Use key. not options.

				PropertyInfo[] properties = key.Type.GetProperties(flags); // Use key.Type not type
				FieldInfo[] fields = key.Type.GetFields(flags);            // Use key.Type not type

				if (key.SortMembers) // Use key. not options.
				{
					// Use Array.Sort for in-place sorting, avoiding LINQ allocations
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

				object? propertyValue;
				try
				{
					propertyValue = property.GetValue(value, index: null);
				}
				catch (TargetInvocationException ex) when (ex.InnerException != null)
				{
					propertyValue = "!" + ex.InnerException.GetType().Name;
				}
				catch (Exception ex) { propertyValue = "!" + ex.GetType().Name; }

				AppendObject(builder, propertyValue, options, depth + 1, visited, typeOptions, tfc);
				isFirst = false;
			}
		}

		if (options.IncludeFields)
		{
			// Iterate over the cached members.Fields
			foreach (FieldInfo f in members.Fields)
			{
				if (!isFirst) builder.Append(", ");
				builder.Append(f.Name).Append(" = ");

				object? propertyValue;
				try
				{
					propertyValue = f.GetValue(value);
				}
				catch (TargetInvocationException ex) when (ex.InnerException != null)
				{
					propertyValue = "!" + ex.InnerException.GetType().Name;
				}
				catch (Exception ex) { propertyValue = "!" + ex.GetType().Name; }

				AppendObject(builder, propertyValue, options, depth + 1, visited, typeOptions, tfc);
				isFirst = false;
			}
		}

		builder.Append(" }");
	}

	// ───────────────────────── TryGetKnownCount() Cache ─────────────────────────

	private static readonly ConcurrentDictionary<Type, Func<object, int>?> sReadOnlyCountGetterCache = new();

	private static int? TryGetKnownCount(IEnumerable enumerable)
	{
		// Non-generic ICollection is the cheapest path.
		if (enumerable is ICollection c)
			return c.Count;

		// IReadOnlyCollection<T>:
		// Get accessor delegate from cache, create new one, if necessary.
		Type type = enumerable.GetType();
		Func<object, int>? getter = sReadOnlyCountGetterCache.GetOrAdd(type, BuildReadOnlyCountGetter);
		if (getter != null)
		{
			try { return getter(enumerable); }
			catch
			{
				/* defensive */
			}
		}

		// No count available.
		return null;
	}

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
			// extremely rare case (e.g. security/trimming edge cases)
			// => proceed without getter...
			return null;
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
