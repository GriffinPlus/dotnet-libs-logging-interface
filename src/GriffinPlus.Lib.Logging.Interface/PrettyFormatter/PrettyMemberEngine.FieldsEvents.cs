using System;
using System.Reflection;
using System.Text;

namespace GriffinPlus.Lib.Logging;

static partial class PrettyMemberEngine
{
	/// <summary>
	/// Formats a field with accessibility/modifiers and its field type.
	/// </summary>
	/// <param name="fieldInfo">The field to format.</param>
	/// <param name="options">Member formatting options.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A single-line representation of the field.
	/// </returns>
	private static string FormatField(FieldInfo fieldInfo, PrettyMemberOptions options, TextFormatContext tfc)
	{
		var builder = new StringBuilder();

		if (options.ShowAttributes) builder.Append(FormatAttributes(fieldInfo, options, tfc));
		if (options.ShowAccessibility) builder.Append(GetAccessibility(fieldInfo)).Append(' ');
		if (options.ShowMemberModifiers) builder.Append(GetMemberModifiers(fieldInfo));

		PrettyTypeOptions typeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;
		if (options.IncludeDeclaringType && fieldInfo.DeclaringType != null)
		{
			PrettyTypeEngine.AppendType(builder, fieldInfo.DeclaringType, typeOptions, tfc);
			builder.Append('.');
		}

		string typeText = PrettyTypeEngine.Format(fieldInfo.FieldType, typeOptions, tfc);

		builder.Append(fieldInfo.Name).Append(" : ").Append(typeText);
		if (options.ShowNullabilityAnnotations && IsNullableReference(fieldInfo, fieldInfo.FieldType) && !typeText.EndsWith("?", StringComparison.Ordinal))
			builder.Append('?');

		return NormalizeSpaces(builder.ToString());
	}

	/// <summary>
	/// Formats an event with its handler type.
	/// </summary>
	/// <param name="e">The event to format.</param>
	/// <param name="options">Member formatting options.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A single-line representation of the event declaration.
	/// </returns>
	private static string FormatEvent(EventInfo e, PrettyMemberOptions options, TextFormatContext tfc)
	{
		var builder = new StringBuilder();

		if (options.ShowAttributes) builder.Append(FormatAttributes(e, options, tfc));
		MethodInfo? acc = e.AddMethod ?? e.RemoveMethod;
		if (options.ShowAccessibility && acc != null) builder.Append(GetAccessibility(acc)).Append(' ');
		if (options.ShowMemberModifiers && acc != null) AppendMemberModifiers(builder, acc);

		PrettyTypeOptions typeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;
		if (options.IncludeDeclaringType && e.DeclaringType != null)
		{
			PrettyTypeEngine.AppendType(builder, e.DeclaringType, typeOptions, tfc);
			builder.Append('.');
		}

		builder.Append(e.Name).Append(" : ");
		PrettyTypeEngine.AppendType(builder, e.EventHandlerType, typeOptions, tfc);

		return NormalizeSpaces(builder.ToString());
	}
}
