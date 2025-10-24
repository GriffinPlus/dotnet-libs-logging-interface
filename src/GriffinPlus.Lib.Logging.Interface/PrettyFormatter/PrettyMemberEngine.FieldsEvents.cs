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
	/// <returns>
	/// A single-line representation of the field.
	/// </returns>
	private static string FormatField(FieldInfo fieldInfo, PrettyMemberOptions options)
	{
		var builder = new StringBuilder();

		if (options.ShowAttributes) builder.Append(FormatAttributes(fieldInfo, options));
		if (options.ShowAccessibility) builder.Append(GetAccessibility(fieldInfo)).Append(' ');
		if (options.ShowMemberModifiers) builder.Append(GetMemberModifiers(fieldInfo));

		if (options.IncludeDeclaringType && fieldInfo.DeclaringType != null)
			builder.Append(PrettyTypeEngine.Format(fieldInfo.DeclaringType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes })).Append('.');

		string typeText = PrettyTypeEngine.Format(fieldInfo.FieldType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes });

		if (options.ShowNullabilityAnnotations && IsNullableReference(fieldInfo, fieldInfo.FieldType) && !typeText.EndsWith("?", StringComparison.Ordinal))
			typeText += "?";

		builder.Append(fieldInfo.Name).Append(" : ").Append(typeText);
		return NormalizeSpaces(builder.ToString());
	}

	/// <summary>
	/// Formats an event with its handler type.
	/// </summary>
	/// <param name="e">The event to format.</param>
	/// <param name="options">Member formatting options.</param>
	/// <returns>
	/// A single-line representation of the event declaration.
	/// </returns>
	private static string FormatEvent(EventInfo e, PrettyMemberOptions options)
	{
		var builder = new StringBuilder();

		if (options.ShowAttributes) builder.Append(FormatAttributes(e, options));
		MethodInfo? acc = e.AddMethod ?? e.RemoveMethod;
		if (options.ShowAccessibility && acc != null) builder.Append(GetAccessibility(acc)).Append(' ');
		if (options.ShowMemberModifiers && acc != null) builder.Append(GetMemberModifiers(acc));

		if (options.IncludeDeclaringType && e.DeclaringType != null)
			builder.Append(PrettyTypeEngine.Format(e.DeclaringType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes })).Append('.');

		builder.Append(e.Name)
			.Append(" : ")
			.Append(PrettyTypeEngine.Format(e.EventHandlerType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes }));

		return NormalizeSpaces(builder.ToString());
	}
}
