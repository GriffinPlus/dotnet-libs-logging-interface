using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	/// <summary>
	/// Formats attributes applied to a member using the current options.
	/// </summary>
	/// <param name="memberInfo">The member whose attributes are formatted.</param>
	/// <param name="options">Member formatting options.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A string containing attribute blocks (each ending with a space), or an empty string.
	/// </returns>
	private static string FormatAttributes(MemberInfo memberInfo, PrettyMemberOptions options, TextFormatContext tfc)
	{
		return FormatAttributesCore(CustomAttributeData.GetCustomAttributes(memberInfo), options, tfc);
	}

	/// <summary>
	/// Formats attributes applied to a parameter using the current options.
	/// </summary>
	/// <param name="parameterInfo">The parameter whose attributes are formatted.</param>
	/// <param name="options">Member formatting options.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A string containing attribute blocks (each ending with a space), or an empty string.
	/// </returns>
	private static string FormatAttributes(ParameterInfo parameterInfo, PrettyMemberOptions options, TextFormatContext tfc)
	{
		return FormatAttributesCore(CustomAttributeData.GetCustomAttributes(parameterInfo), options, tfc);
	}

	/// <summary>
	/// Core attribute formatter that avoids allocations from LINQ, <see cref="List{T}"/>, and <see cref="string.Join(string,string[])"/>.
	/// </summary>
	/// <param name="attributes">The attribute list to format.</param>
	/// <param name="options">Member formatting options controlling filtering and argument limits.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A string containing concatenated attribute blocks, or an empty string if none are emitted.
	/// </returns>
	private static string FormatAttributesCore(IList<CustomAttributeData>? attributes, PrettyMemberOptions options, TextFormatContext tfc)
	{
		// Guard clause for null or empty input.
		if (attributes == null || attributes.Count == 0) return "";

		// 1. --- Setup ---
		// Initialize builder lazily (as null) to avoid allocation if all attributes are filtered out.
		StringBuilder? builder = null;
		Func<CustomAttributeData, bool>? filter = options.AttributeFilter;
		int maxElements = options.AttributeMaxElements;

		// 2. --- Main Loop ---
		// Iterate with a 'for' loop for performance (avoids IEnumerator allocation).
		// ReSharper disable once ForCanBeConvertedToForeach
		for (int i = 0; i < attributes.Count; i++)
		{
			CustomAttributeData attributeData = attributes[i];

			// 2a. Apply the filter, if one exists.
			if (filter != null && !filter(attributeData))
			{
				continue; // Skip this attribute.
			}

			// 2b. Lazy-initialize the StringBuilder on the first *visible* attribute.
			// 128 is a reasonable starting capacity for attribute blocks.
			builder ??= new StringBuilder(128);

			// 2c. Append the attribute name.
			builder.Append('[');
			string name = attributeData.AttributeType.Name;

			// Check for the "Attribute" suffix and remove it *without* allocating a new substring.
			if (name.EndsWith("Attribute", StringComparison.Ordinal))
			{
				// Use the StringBuilder.Append(string, int, int) overload.
				builder.Append(name, 0, name.Length - 9);
			}
			else
			{
				builder.Append(name);
			}

			// 2d. Append arguments (if any exist and we are allowed to show them).
			int ctorArgsCount = attributeData.ConstructorArguments.Count;
			// ReSharper disable once PossibleNullReferenceException
			int namedArgsCount = attributeData.NamedArguments.Count;
			int totalArgs = ctorArgsCount + namedArgsCount;

			if (totalArgs > 0 && maxElements > 0)
			{
				builder.Append('(');
				int taken = 0;           // Counter for maxElements
				bool needsComma = false; // State flag to manage commas

				// Constructor Arguments
				for (int j = 0; j < ctorArgsCount && taken < maxElements; j++, taken++)
				{
					if (needsComma) builder.Append(", ");
					AppendAttributeValue(builder, attributeData.ConstructorArguments[j].Value, options, tfc);
					needsComma = true;
				}

				// Named Arguments
				for (int j = 0; j < namedArgsCount && taken < maxElements; j++, taken++)
				{
					if (needsComma) builder.Append(", ");

					CustomAttributeNamedArgument na = attributeData.NamedArguments[j];
					builder.Append(na.MemberName).Append(" = ");
					AppendAttributeValue(builder, na.TypedValue.Value, options, tfc);
					needsComma = true;
				}

				builder.Append(')');
			}

			// 2e. Close the attribute block with a trailing space.
			builder.Append("] ");
		}

		// 3. --- Result ---
		// If builder is still null, no attributes were rendered.
		// Otherwise, return the final string.
		return builder?.ToString() ?? "";
	}

	/// <summary>
	/// Appends an attribute constructor/named argument value (string quoting, chars, booleans, arrays, and typeof)
	/// to the specified <see cref="StringBuilder"/>.
	/// </summary>
	/// <param name="builder">The <see cref="StringBuilder"/> to append the C#-like literal representation of the value to.</param>
	/// <param name="value">The attribute argument value to format.</param>
	/// <param name="options">Member formatting options (used for type-name formatting).</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	private static void AppendAttributeValue(
		StringBuilder       builder,
		object?             value,
		PrettyMemberOptions options,
		TextFormatContext   tfc)
	{
		// Null
		if (value == null)
		{
			builder.Append("null");
			return;
		}

		// String
		if (value is string s)
		{
			builder.Append('\"');

			// DO NOT strip BiDi controls; AppendEscaped will handle them.
			// string sanitized = TextPostProcessor.StripBidiControls(s);

			if (!tfc.Truncate || tfc.MaxLineLength <= 0)
			{
				TextPostProcessor.AppendEscaped(builder, s, 0, s.Length);
				builder.Append('\"');
				return;
			}

			int cap = tfc.MaxLineLength;
			if (s.Length <= cap &&
			    TextPostProcessor.SafePrefixCharCountByTextElements(s, 0, cap, s.Length) >= s.Length)
			{
				TextPostProcessor.AppendEscaped(builder, s, 0, s.Length);
				builder.Append('\"');
				return;
			}

			int markerElems = new StringInfo(tfc.TruncationMarker).LengthInTextElements;
			int limit = cap - markerElems;
			if (limit <= 0)
			{
				builder.Append(tfc.TruncationMarker);
				builder.Append('\"');
				return;
			}

			int safeChars = TextPostProcessor.SafePrefixCharCountByTextElements(s, 0, limit, s.Length);
			if (safeChars > 0)
				TextPostProcessor.AppendEscaped(builder, s, 0, safeChars);
			builder.Append(tfc.TruncationMarker);

			builder.Append('\"');
			return;
		}

		// Char
		if (value is char c)
		{
			builder.Append('\'');
			TextPostProcessor.AppendEscapedChar(builder, c);
			builder.Append('\'');
			return;
		}

		// Boolean
		if (value is bool b)
		{
			builder.Append(b ? "true" : "false");
			return;
		}

		// Numerics and other IFormattable (before Enum as enum is IFormattable as well)
		if (value is IFormattable formattable and not Enum and not Type)
		{
			try
			{
				builder.Append(formattable.ToString(format: null, tfc.Culture));
			}
			catch
			{
				// Defensive: ToString() should not fail, but better safe than sorry...
				builder.Append(value);
			}
			return;
		}

		// Get type formatting options based on member options
		PrettyTypeOptions typeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;

		// Enums
		Type valueType = value.GetType(); // Cache GetType()
		if (valueType.IsEnum)
		{
			// Include Enum name, not just the value.
			// Use AppendType to avoid allocating the enumName string.
			PrettyTypeEngine.AppendType(builder, valueType, typeOptions, tfc);
			builder.Append('.').Append(value);
			return;
		}

		// Type
		if (value is Type type)
		{
			builder.Append("typeof(");
			PrettyTypeEngine.AppendType(builder, type, typeOptions, tfc);
			builder.Append(')');
			return;
		}

		// Arrays
		if (value is Array array)
		{
			builder.Append("new[] { ");
			bool first = true;
			foreach (object? o in array)
			{
				if (!first) builder.Append(", ");
				AppendAttributeValue(builder, o, options, tfc);
				first = false;
			}
			builder.Append(" }");
			return;
		}

		// Fallback for unknown types (should not happen in practice)
		builder.Append(value);
	}
}
