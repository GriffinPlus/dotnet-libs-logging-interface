using System;
using System.Reflection;

namespace GriffinPlus.Lib.Logging;

/// <summary>
/// Internal engine for formatting <see cref="MemberInfo"/> and <see cref="ParameterInfo"/> values.<br/>
/// Handles methods, constructors, properties (incl. indexers), fields, events, attributes (optional),
/// extension method <c>this</c>-parameters, and generic constraints.
/// </summary>
/// <remarks>
///     <para>
///     Nullability annotations for reference types are resolved via a project-provided
///     <c>NullabilityInspector</c> to keep cross-target behavior consistent (e.g., .NET 4.6.1 and .NET 6+).
///     The engine itself remains pure and thread-safe, it holds no mutable state.
///     </para>
///     <para>
///     The member formatter emits full C#-style signatures, including parameter modifiers
///     (<c>ref</c>, <c>out</c>, <c>in</c>, <c>params</c>) and nullability suffixes when metadata allows.
///     The underlying <see cref="PrettyTypeEngine"/> may represent ByRef types using the CLR suffix <c>&</c>,
///     but does not itself add C# keywords.
///     </para>
/// </remarks>
static partial class PrettyMemberEngine
{
	/// <summary>
	/// Formats a <see cref="MemberInfo"/> into a documentation-friendly, C#-like string according to <paramref name="options"/>.
	/// </summary>
	/// <param name="methodInfo">The member to format. If <see langword="null"/>, the literal string <c>"&lt;null&gt;"</c> is returned.</param>
	/// <param name="options">Formatting options controlling visibility, modifiers, attributes, constraints, and type rendering.</param>
	/// <returns>
	/// A single-line, human-readable signature for the member.
	/// </returns>
	public static string Format(MemberInfo? methodInfo, PrettyMemberOptions options)
	{
		if (methodInfo == null)
			return "<null>";

		if (methodInfo is MethodInfo info) return FormatMethod(info, options);
		if (methodInfo is ConstructorInfo constructorInfo) return FormatConstructor(constructorInfo, options);
		if (methodInfo is PropertyInfo propertyInfo) return FormatProperty(propertyInfo, options);
		if (methodInfo is FieldInfo fieldInfo) return FormatField(fieldInfo, options);
		if (methodInfo is EventInfo eventInfo) return FormatEvent(eventInfo, options);

		// If a Type arrives here, allow formatting it (including generic constraints if configured).
		if (methodInfo is Type type)
		{
			var typeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };
			string head = PrettyTypeEngine.Format(type, typeOptions);

			// Abort if generic constraints on types are not requested or the type is not a generic type definition.
			if (!options.ShowGenericConstraintsOnTypes || !type.IsGenericTypeDefinition)
				return head;

			// Append generic constraints.
			string where = BuildWhereClauses(type.GetGenericArguments(), options);
			if (!string.IsNullOrEmpty(where)) head += " " + where;

			return head;
		}

		// Fallback: just prefix with declaring type (if requested) and the raw name.
		string prefix = options.IncludeDeclaringType && methodInfo.DeclaringType != null
			                ? PrettyTypeEngine.Format(methodInfo.DeclaringType, new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes }) + "."
			                : "";

		return prefix + methodInfo.Name;
	}

	/// <summary>
	/// Formats a <see cref="ParameterInfo"/> in the context of its owning method/constructor.
	/// </summary>
	/// <param name="parameterInfo">The parameter to format. If <see langword="null"/>, the literal string <c>"&lt;null&gt;"</c> is returned.</param>
	/// <param name="options">Member formatting options (controls attributes, names, type namespace mode, etc.).</param>
	/// <param name="owner">The owning <see cref="MethodBase"/>. May be <see langword="null"/>.</param>
	/// <returns>
	/// A formatted parameter segment, including modifiers and (optionally) the parameter name.
	/// </returns>
	/// <remarks>
	/// The member formatter emits full C#-style signatures, including parameter modifiers
	/// (<c>ref</c>, <c>out</c>, <c>in</c>, <c>params</c>) and nullability suffixes when metadata allows.
	/// The underlying <see cref="PrettyTypeEngine"/> may represent ByRef types using the CLR suffix <c>&</c>,
	/// but does not itself add C# keywords.
	/// </remarks>
	public static string Format(ParameterInfo? parameterInfo, PrettyMemberOptions options, MethodBase? owner)
	{
		if (parameterInfo == null) return "<null>";
		if (options == null) throw new ArgumentNullException(nameof(options));

		GetParameterModifierAndType(parameterInfo, out string modifier, out Type coreType);

		var typeOptions = new PrettyTypeOptions { UseNamespace = options.UseNamespaceForTypes };
		string typeText = PrettyTypeEngine.Format(coreType, typeOptions);

		if (options.ShowNullabilityAnnotations && IsNullableReference(parameterInfo, coreType) && !typeText.EndsWith("?", StringComparison.Ordinal))
			typeText += "?";

		string namePart = options.ShowParameterNames && !string.IsNullOrEmpty(parameterInfo.Name) ? " " + parameterInfo.Name : "";
		string attributes = options is { ShowAttributes: true, ShowParameterAttributes: true } ? FormatAttributes(parameterInfo, options) : "";
		return (attributes + modifier + typeText + namePart).TrimStart();
	}
}
