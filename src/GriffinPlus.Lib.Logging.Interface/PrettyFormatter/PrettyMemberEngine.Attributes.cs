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
	/// <returns>
	/// A string containing attribute blocks (each ending with a space), or an empty string.
	/// </returns>
	private static string FormatAttributes(MemberInfo memberInfo, PrettyMemberOptions options) => FormatAttributesCore(CustomAttributeData.GetCustomAttributes(memberInfo), options);

	/// <summary>
	/// Formats attributes applied to a parameter using the current options.
	/// </summary>
	/// <param name="parameterInfo">The parameter whose attributes are formatted.</param>
	/// <param name="options">Member formatting options.</param>
	/// <returns>
	/// A string containing attribute blocks (each ending with a space), or an empty string.
	/// </returns>
	private static string FormatAttributes(ParameterInfo parameterInfo, PrettyMemberOptions options) => FormatAttributesCore(CustomAttributeData.GetCustomAttributes(parameterInfo), options);

	/// <summary>
	/// Core attribute formatter that avoids allocations from LINQ, <see cref="List{T}"/>, and <see cref="string.Join(string,string[])"/>.
	/// </summary>
	/// <param name="attributes">The attribute list to format.</param>
	/// <param name="options">Member formatting options controlling filtering and argument limits.</param>
	/// <returns>
	/// A string containing concatenated attribute blocks, or an empty string if none are emitted.
	/// </returns>
	private static string FormatAttributesCore(IList<CustomAttributeData>? attributes, PrettyMemberOptions options)
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
					AppendAttributeValue(builder, attributeData.ConstructorArguments[j].Value, options);
					needsComma = true;
				}

				// Named Arguments
				for (int j = 0; j < namedArgsCount && taken < maxElements; j++, taken++)
				{
					if (needsComma) builder.Append(", ");

					CustomAttributeNamedArgument na = attributeData.NamedArguments[j];
					builder.Append(na.MemberName).Append(" = ");
					AppendAttributeValue(builder, na.TypedValue.Value, options);
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
	private static void AppendAttributeValue(StringBuilder builder, object? value, PrettyMemberOptions options)
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
			builder.Append('\"').Append(s).Append('\"');
			return;
		}

		// Char
		if (value is char c)
		{
			builder.Append('\'').Append(c).Append('\'');
			return;
		}

		// Boolean
		if (value is bool b)
		{
			builder.Append(b ? "true" : "false");
			return;
		}

		// Numerics and other IFormattable (before Enum as enum is IFormattable as well)
		// Use InvariantCulture to ensure consistent formatting (e.g., decimal point instead of comma)
		if (value is IFormattable formattable and not Enum and not Type)
		{
			try
			{
				builder.Append(formattable.ToString(format: null, CultureInfo.InvariantCulture));
			}
			catch
			{
				// Defensive: ToString() should not fail, but better safe than sorry...
				builder.Append(value);
			}
			return;
		}

		// Enums
		Type valueType = value.GetType(); // Cache GetType()
		if (valueType.IsEnum)
		{
			// Include Enum name, not just the value.
			// Use AppendType to avoid allocating the enumName string.
			PrettyTypeEngine.AppendType(builder, valueType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes });
			builder.Append('.').Append(value);
			return;
		}

		// Type
		if (value is Type type)
		{
			builder.Append("typeof(");
			PrettyTypeEngine.AppendType(builder, type, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes });
			builder.Append(')');
		}

		// Arrays
		if (value is Array array)
		{
			builder.Append("new[] { ");
			bool first = true;
			foreach (object? o in array)
			{
				if (!first) builder.Append(", ");
				AppendAttributeValue(builder, o, options);
				first = false;
			}
			builder.Append(" }");
		}

		// Fallback for unknown types (should not happen in practice)
		builder.Append(value);
	}
}
