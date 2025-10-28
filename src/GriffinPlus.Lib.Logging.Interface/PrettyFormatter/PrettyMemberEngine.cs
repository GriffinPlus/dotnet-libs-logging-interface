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
///     The underlying <see cref="PrettyTypeEngine"/> may represent ByRef types using the CLR suffix <c>&amp;</c>,
///     but does not itself add C# keywords.
///     </para>
/// </remarks>
static partial class PrettyMemberEngine
{
	/// <summary>
	/// A boxed representation of the boolean value <see langword="true"/>.
	/// </summary>
	/// <remarks>
	/// This field is used to avoid allocating multiple boxed instances of the <see langword="true"/>
	/// value. It is intended for internal use where boxing of boolean values is required.
	/// </remarks>
	private static readonly object sTrueBox = true;

	/// <summary>
	/// A boxed representation of the boolean value <see langword="false"/>.
	/// </summary>
	/// <remarks>
	/// This field is used to avoid allocating multiple boxed instances of the <see langword="false"/>
	/// value. It is intended for internal use where boxing of boolean values is required.
	/// </remarks>
	private static readonly object sFalseBox = false;

	/// <summary>
	/// Formats a <see cref="MemberInfo"/> into a documentation-friendly, C#-like string according to <paramref name="options"/>.
	/// </summary>
	/// <param name="memberInfo">The member to format. If <see langword="null"/>, the literal string <c>"&lt;null&gt;"</c> is returned.</param>
	/// <param name="options">Formatting options controlling visibility, modifiers, attributes, constraints, and type rendering.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A single-line, human-readable signature for the member.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="memberInfo"/> or <paramref name="options"/> is <see langword="null"/>.
	/// </exception>
	public static string Format(MemberInfo memberInfo, PrettyMemberOptions options, TextFormatContext tfc)
	{
		if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));
		if (options == null) throw new ArgumentNullException(nameof(options));

		if (memberInfo is MethodInfo info) return FormatMethod(info, options, tfc);
		if (memberInfo is ConstructorInfo constructorInfo) return FormatConstructor(constructorInfo, options, tfc);
		if (memberInfo is PropertyInfo propertyInfo) return FormatProperty(propertyInfo, options, tfc);
		if (memberInfo is FieldInfo fieldInfo) return FormatField(fieldInfo, options, tfc);
		if (memberInfo is EventInfo eventInfo) return FormatEvent(eventInfo, options, tfc);

		// If a Type arrives here, allow formatting it (including generic constraints if configured).
		PrettyTypeOptions typeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;
		if (memberInfo is Type type)
		{
			string head = PrettyTypeEngine.Format(type, typeOptions, tfc);

			// Abort if generic constraints on types are not requested or the type is not a generic type definition.
			if (!options.ShowGenericConstraintsOnTypes || !type.IsGenericTypeDefinition)
				return head;

			// Append generic constraints.
			string where = BuildWhereClauses(type.GetGenericArguments(), options, tfc);
			if (!string.IsNullOrEmpty(where)) head += " " + where;

			return head;
		}

		// Fallback: just prefix with declaring type (if requested) and the raw name.
		string prefix = options.IncludeDeclaringType && memberInfo.DeclaringType != null
			                ? PrettyTypeEngine.Format(memberInfo.DeclaringType, typeOptions, tfc) + "."
			                : "";

		return prefix + memberInfo.Name;
	}

	/// <summary>
	/// Formats a <see cref="ParameterInfo"/> in the context of its owning method/constructor.
	/// </summary>
	/// <param name="parameterInfo">The parameter to format. If <see langword="null"/>, the literal string <c>"&lt;null&gt;"</c> is returned.</param>
	/// <param name="options">Member formatting options (controls attributes, names, type namespace mode, etc.).</param>
	/// <param name="owner">The owning <see cref="MethodBase"/>. May be <see langword="null"/>.</param>
	/// <param name="tfc">A <see cref="TextFormatContext"/> providing newline, indentation, and culture information.</param>
	/// <returns>
	/// A formatted parameter segment, including modifiers and (optionally) the parameter name.
	/// </returns>
	/// <remarks>
	/// The member formatter emits full C#-style signatures, including parameter modifiers
	/// (<c>ref</c>, <c>out</c>, <c>in</c>, <c>params</c>) and nullability suffixes when metadata allows.
	/// The underlying <see cref="PrettyTypeEngine"/> may represent ByRef types using the CLR suffix <c>&amp;</c>,
	/// but does not itself add C# keywords.
	/// </remarks>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="parameterInfo"/> or <paramref name="options"/> is <see langword="null"/>.
	/// </exception>
	public static string Format(
		ParameterInfo       parameterInfo,
		PrettyMemberOptions options,
		MethodBase?         owner,
		TextFormatContext   tfc)
	{
		if (parameterInfo == null) throw new ArgumentNullException(nameof(parameterInfo));
		if (options == null) throw new ArgumentNullException(nameof(options));

		GetParameterModifierAndType(parameterInfo, out string modifier, out Type coreType);

		PrettyTypeOptions typeOptions = options.UseNamespaceForTypes ? PrettyTypePresets.Full : PrettyTypePresets.Compact;
		string typeText = PrettyTypeEngine.Format(coreType, typeOptions, tfc);

		if (options.ShowNullabilityAnnotations && IsNullableReference(parameterInfo, coreType) && !typeText.EndsWith("?", StringComparison.Ordinal))
			typeText += "?";

		string namePart = options.ShowParameterNames && !string.IsNullOrEmpty(parameterInfo.Name) ? " " + parameterInfo.Name : "";
		string attributes = options is { ShowAttributes: true, ShowParameterAttributes: true } ? FormatAttributes(parameterInfo, options, tfc) : "";
		return (attributes + modifier + typeText + namePart).TrimStart();
	}
}
